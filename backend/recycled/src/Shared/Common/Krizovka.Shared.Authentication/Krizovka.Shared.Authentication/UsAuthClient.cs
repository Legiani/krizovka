using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;

namespace Krizovka.Shared.Authentication;

public interface IUsAuthClient
{
    Task<ValidationResult?> ValidateContextAsync(string token, CancellationToken cancellationToken = default);
}

public class UsAuthClient : IUsAuthClient
{
    private readonly HttpClient _httpClient;
    private readonly IMemoryCache _cache;
    private readonly AuthenticationOptions _options;
    private readonly JsonSerializerOptions _jsonOptions;
    private readonly Microsoft.Extensions.Logging.ILogger<UsAuthClient> _logger;

    public UsAuthClient(
        HttpClient httpClient,
        IMemoryCache cache,
        AuthenticationOptions options,
        Microsoft.Extensions.Logging.ILogger<UsAuthClient> logger)
    {
        _httpClient = httpClient;
        _cache = cache;
        _options = options;
        _logger = logger;
        _jsonOptions = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        };
    }

    public async Task<ValidationResult?> ValidateContextAsync(string token, CancellationToken cancellationToken = default)
    {
        var cacheKey = "auth_token_" + Convert.ToHexString(System.Security.Cryptography.SHA256.HashData(System.Text.Encoding.UTF8.GetBytes(token)));

        if (_cache.TryGetValue(cacheKey, out ValidationResult? cached))
        {
            return cached;
        }

        try
        {
            var request = new { token };
            var response = await _httpClient.PostAsJsonAsync("v1/us/context/validate", request, cancellationToken);


            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("US Context Validation failed. Status: {StatusCode}. BaseUrl: {BaseUrl}",
                    response.StatusCode, _httpClient.BaseAddress);
                return null;
            }

            var result = await response.Content.ReadFromJsonAsync<UsValidationResponse>(_jsonOptions, cancellationToken);

            if (result == null || !result.IsValid)
            {
                _logger.LogWarning("US returned invalid context result");
                return null;
            }

            var validationResult = new ValidationResult(
                result.AccountId,
                result.IdentityId,
                result.Claims ?? new List<string>(),
                result.Roles ?? new List<string>(),
                result.Scope?.ScopeType,
                result.Scope?.ScopeId,
                result.CityAssignments ?? new List<CityAssignment>(),
                result.ActingAccountId,
                result.IsBackoffice,
                result.ScopeAssignments ?? new List<ScopeAssignment>()
            );

            _cache.Set(cacheKey, validationResult, TimeSpan.FromSeconds(_options.CacheDurationSeconds));

            return validationResult;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Exception sending validation request to US at {BaseUrl}", _httpClient.BaseAddress);
            return null;
        }
    }

    private record UsValidationResponse(
        bool IsValid,
        Guid AccountId,
        Guid IdentityId,
        Guid? ActingAccountId,
        List<string>? Claims,
        List<string>? Roles,
        ScopeInfo? Scope,
        List<CityAssignment>? CityAssignments,
        bool IsBackoffice,
        List<ScopeAssignment>? ScopeAssignments
    );

    private record ScopeInfo(
        string ScopeType,
        string ScopeId
    );
}

/// <summary>Represents a single city-role assignment for a backoffice user.</summary>
public record CityAssignment(string CityId, string RoleCode);

public record ValidationResult(
    Guid AccountId,
    Guid IdentityId,
    List<string> Claims,
    List<string> Roles,
    string? ScopeType = null,
    string? ScopeId = null,
    List<CityAssignment>? CityAssignments = null,
    Guid? ActingAccountId = null,
    bool IsBackoffice = true,
    List<ScopeAssignment>? ScopeAssignments = null
);
