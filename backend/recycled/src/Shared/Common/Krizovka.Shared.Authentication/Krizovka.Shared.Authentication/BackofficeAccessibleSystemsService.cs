using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;

namespace Krizovka.Shared.Authentication;

public interface IBackofficeAccessibleSystemsService
{
    Task<AccessibleSystemsResult> GetAccessibleSystemsAsync(CancellationToken cancellationToken = default);

    Task<EffectiveSystemFilterResult> GetEffectiveSystemFilterAsync(
        string? requestedSystemId,
        CancellationToken cancellationToken = default);
}

public sealed record AccessibleSystemsResult(
    IReadOnlyList<string> SystemIds,
    bool HasGlobalAccess);

public sealed record EffectiveSystemFilterResult(
    IReadOnlyList<string> EffectiveSystemIds,
    bool HasGlobalAccess,
    bool IsForbidden);

public interface ISrsMunicipalityDirectoryClient
{
    Task<SrsMunicipalityRecord?> GetByCodeAsync(string code, CancellationToken cancellationToken);

    Task<SrsMunicipalityRecord?> GetByIdAsync(Guid id, CancellationToken cancellationToken);

    Task<IReadOnlyList<SrsMunicipalityRecord>> ListByTypeAsync(string municipalityType, CancellationToken cancellationToken);
}

public sealed record SrsMunicipalityRecord(
    Guid Id,
    string Code,
    string Type,
    Guid? ParentMunicipalityId);

internal sealed class SrsMunicipalityDirectoryClient : ISrsMunicipalityDirectoryClient
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    private static readonly TimeSpan CacheDuration = TimeSpan.FromMinutes(2);

    private readonly HttpClient _httpClient;
    private readonly IMemoryCache _cache;
    private readonly ILogger<SrsMunicipalityDirectoryClient> _logger;

    public SrsMunicipalityDirectoryClient(
        HttpClient httpClient,
        IMemoryCache cache,
        ILogger<SrsMunicipalityDirectoryClient> logger)
    {
        _httpClient = httpClient;
        _cache = cache;
        _logger = logger;
    }

    public Task<SrsMunicipalityRecord?> GetByCodeAsync(string code, CancellationToken cancellationToken)
    {
        var normalizedCode = code.Trim().ToUpperInvariant();
        return _cache.GetOrCreateAsync(
            $"srs::municipality::code::{normalizedCode}",
            async entry =>
            {
                entry.AbsoluteExpirationRelativeToNow = CacheDuration;
                return await GetAsync($"v1/srs/cities/by-code/{Uri.EscapeDataString(code)}", cancellationToken);
            })!;
    }

    public Task<SrsMunicipalityRecord?> GetByIdAsync(Guid id, CancellationToken cancellationToken)
    {
        return _cache.GetOrCreateAsync(
            $"srs::municipality::id::{id:D}",
            async entry =>
            {
                entry.AbsoluteExpirationRelativeToNow = CacheDuration;
                return await GetAsync($"v1/srs/cities/{id:D}", cancellationToken);
            })!;
    }

    public Task<IReadOnlyList<SrsMunicipalityRecord>> ListByTypeAsync(string municipalityType, CancellationToken cancellationToken)
    {
        var normalizedType = municipalityType.Trim().ToUpperInvariant();
        return _cache.GetOrCreateAsync(
            $"srs::municipality::list::{normalizedType}",
            async entry =>
            {
                entry.AbsoluteExpirationRelativeToNow = CacheDuration;
                return await ListAsync($"v1/srs/cities?type={Uri.EscapeDataString(normalizedType)}", cancellationToken);
            })!;
    }

    private async Task<SrsMunicipalityRecord?> GetAsync(string path, CancellationToken cancellationToken)
    {
        try
        {
            using var response = await _httpClient.GetAsync(path, cancellationToken);
            if (response.StatusCode == HttpStatusCode.NotFound)
            {
                return null;
            }

            response.EnsureSuccessStatusCode();

            var dto = await response.Content.ReadFromJsonAsync<SrsMunicipalityDto>(JsonOptions, cancellationToken);
            return dto?.ToRecord();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "SRS municipality lookup failed for path {Path}", path);
            return null;
        }
    }

    private async Task<IReadOnlyList<SrsMunicipalityRecord>> ListAsync(string path, CancellationToken cancellationToken)
    {
        try
        {
            using var response = await _httpClient.GetAsync(path, cancellationToken);
            response.EnsureSuccessStatusCode();

            var dtos = await response.Content.ReadFromJsonAsync<List<SrsMunicipalityDto>>(JsonOptions, cancellationToken);
            return dtos?.Select(dto => dto.ToRecord()).ToList() ?? [];
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "SRS municipality list failed for path {Path}", path);
            return [];
        }
    }

    private sealed record SrsMunicipalityDto(
        Guid Id,
        string Code,
        string Type,
        Guid? ParentMunicipalityId)
    {
        public SrsMunicipalityRecord ToRecord() => new(Id, Code, Type, ParentMunicipalityId);
    }
}

public sealed class BackofficeAccessibleSystemsService : IBackofficeAccessibleSystemsService
{
    private static readonly TimeSpan CacheDuration = TimeSpan.FromMinutes(2);

    private readonly ICurrentUserService _currentUserService;
    private readonly ISrsMunicipalityDirectoryClient _directoryClient;
    private readonly IMemoryCache _cache;
    private readonly ILogger<BackofficeAccessibleSystemsService> _logger;

    public BackofficeAccessibleSystemsService(
        ICurrentUserService currentUserService,
        ISrsMunicipalityDirectoryClient directoryClient,
        IMemoryCache cache,
        ILogger<BackofficeAccessibleSystemsService> logger)
    {
        _currentUserService = currentUserService;
        _directoryClient = directoryClient;
        _cache = cache;
        _logger = logger;
    }

    public async Task<AccessibleSystemsResult> GetAccessibleSystemsAsync(CancellationToken cancellationToken = default)
    {
        if (OwnerScopeAuthorizationHelper.HasGlobalAccess(_currentUserService))
        {
            return new AccessibleSystemsResult([], true);
        }

        var assignments = _currentUserService.GetScopeAssignments()
            .Where(a => !string.IsNullOrWhiteSpace(a.ScopeType) && !string.IsNullOrWhiteSpace(a.ScopeId))
            .Select(a => new ScopeAssignment(
                a.ScopeType.Trim().ToUpperInvariant(),
                OwnerScopeAuthorizationHelper.NormalizeScopeId(a.ScopeId),
                a.RoleCode))
            .DistinctBy(a => $"{a.ScopeType}:{a.ScopeId}", StringComparer.OrdinalIgnoreCase)
            .OrderBy(a => a.ScopeType, StringComparer.OrdinalIgnoreCase)
            .ThenBy(a => a.ScopeId, StringComparer.OrdinalIgnoreCase)
            .ToList();

        if (assignments.Count == 0)
        {
            return new AccessibleSystemsResult([], false);
        }

        var cacheKey = "auth::accessible-systems::" + string.Join("|", assignments.Select(a => $"{a.ScopeType}:{a.ScopeId}"));
        if (_cache.TryGetValue(cacheKey, out AccessibleSystemsResult? cached) && cached != null)
        {
            return cached;
        }

        var allowedCodes = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var assignment in assignments)
        {
            await AppendAllowedSystemCodesAsync(assignment.ScopeType, assignment.ScopeId, allowedCodes, cancellationToken);
        }

        var result = new AccessibleSystemsResult(
            allowedCodes.OrderBy(code => code, StringComparer.OrdinalIgnoreCase).ToList(),
            false);

        _cache.Set(cacheKey, result, CacheDuration);
        return result;
    }

    public async Task<EffectiveSystemFilterResult> GetEffectiveSystemFilterAsync(
        string? requestedSystemId,
        CancellationToken cancellationToken = default)
    {
        var normalizedRequestedSystemId = string.IsNullOrWhiteSpace(requestedSystemId)
            ? null
            : requestedSystemId.Trim();

        var accessibleSystems = await GetAccessibleSystemsAsync(cancellationToken);
        if (accessibleSystems.HasGlobalAccess)
        {
            return normalizedRequestedSystemId == null
                ? new EffectiveSystemFilterResult([], true, false)
                : new EffectiveSystemFilterResult([normalizedRequestedSystemId], true, false);
        }

        if (normalizedRequestedSystemId == null)
        {
            return new EffectiveSystemFilterResult(accessibleSystems.SystemIds, false, false);
        }

        if (!accessibleSystems.SystemIds.Contains(normalizedRequestedSystemId, StringComparer.OrdinalIgnoreCase))
        {
            _logger.LogWarning(
                "Requested systemId {SystemId} is outside current backoffice assignments {Assignments}",
                normalizedRequestedSystemId,
                string.Join(", ", accessibleSystems.SystemIds));
            return new EffectiveSystemFilterResult([], false, true);
        }

        return new EffectiveSystemFilterResult([normalizedRequestedSystemId], false, false);
    }

    private async Task AppendAllowedSystemCodesAsync(
        string scopeType,
        string scopeId,
        ISet<string> allowedCodes,
        CancellationToken cancellationToken)
    {
        switch (scopeType.ToUpperInvariant())
        {
            case "GLOBAL":
                allowedCodes.Clear();
                break;

            case "SYSTEM":
                allowedCodes.Add(scopeId);
                break;

            case "CITY":
            case "IDS":
            case "PROVIDER":
            case "CARRIER":
            case "ORGANIZATION":
                var municipality = await ResolveMunicipalityAsync(scopeId, cancellationToken);
                if (municipality == null)
                {
                    _logger.LogWarning("Unable to resolve scope assignment {ScopeType}:{ScopeId} into SRS municipality.", scopeType, scopeId);
                    return;
                }

                allowedCodes.Add(municipality.Code);

                if (scopeType.Equals("IDS", StringComparison.OrdinalIgnoreCase))
                {
                    foreach (var childCode in await GetIdsChildrenAsync(municipality.Id, cancellationToken))
                    {
                        allowedCodes.Add(childCode);
                    }
                }

                break;
        }
    }

    private async Task<SrsMunicipalityRecord?> ResolveMunicipalityAsync(string scopeId, CancellationToken cancellationToken)
    {
        return Guid.TryParse(scopeId, out var municipalityId)
            ? await _directoryClient.GetByIdAsync(municipalityId, cancellationToken)
            : await _directoryClient.GetByCodeAsync(scopeId, cancellationToken);
    }

    private async Task<IReadOnlyList<string>> GetIdsChildrenAsync(Guid parentMunicipalityId, CancellationToken cancellationToken)
    {
        var childTypes = new[] { "CITY", "PROVIDER", "CARRIER" };
        var children = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var childType in childTypes)
        {
            var municipalities = await _directoryClient.ListByTypeAsync(childType, cancellationToken);
            foreach (var municipality in municipalities.Where(m => m.ParentMunicipalityId == parentMunicipalityId))
            {
                children.Add(municipality.Code);
            }
        }

        return children.OrderBy(code => code, StringComparer.OrdinalIgnoreCase).ToList();
    }
}
