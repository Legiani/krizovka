using Krizovka.Users.Application.Core.Interfaces.Services.Models;
using System.Net.Http.Json;
using Krizovka.Users.Application.Core.Interfaces.Services;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;

namespace Krizovka.Users.Infrastructure.Services;

public class KratosIdentityService : IKratosIdentityService
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IMemoryCache _memoryCache;
    private readonly ILogger<KratosIdentityService> _logger;

    public KratosIdentityService(IHttpClientFactory httpClientFactory, IMemoryCache memoryCache, ILogger<KratosIdentityService> logger)
    {
        _httpClientFactory = httpClientFactory;
        _memoryCache = memoryCache;
        _logger = logger;
    }

    public async Task<KratosIdentityDto?> GetIdentityAsync(Guid identityId, CancellationToken cancellationToken = default)
    {
        var cacheKey = $"KratosIdentity_{identityId}";

        if (_memoryCache.TryGetValue(cacheKey, out KratosIdentityDto? cachedIdentity))
        {
            return cachedIdentity;
        }

        var client = _httpClientFactory.CreateClient("KratosAdmin");

        try
        {
            var response = await client.GetAsync($"/admin/identities/{identityId}", cancellationToken);
            if (response.IsSuccessStatusCode)
            {
                var identity = await response.Content.ReadFromJsonAsync<KratosIdentityDto>(cancellationToken: cancellationToken);
                if (identity != null)
                {
                    var cacheEntryOptions = new MemoryCacheEntryOptions()
                        .SetSlidingExpiration(TimeSpan.FromMinutes(5))
                        .SetAbsoluteExpiration(TimeSpan.FromMinutes(15));

                    _memoryCache.Set(cacheKey, identity, cacheEntryOptions);
                    return identity;
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to fetch identity from Kratos for ID: {IdentityId}", identityId);
        }

        return null;
    }

    public async Task<IReadOnlyCollection<Guid>> FindIdentityIdsByCredentialAsync(string credential, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(credential))
        {
            return [];
        }

        var cacheKey = $"KratosIdentityLookup_{credential.Trim().ToLowerInvariant()}";
        if (_memoryCache.TryGetValue(cacheKey, out IReadOnlyCollection<Guid>? cachedIdentityIds))
        {
            return cachedIdentityIds ?? [];
        }

        var client = _httpClientFactory.CreateClient("KratosAdmin");

        try
        {
            var response = await client.GetAsync($"/admin/identities?credentials_identifier={Uri.EscapeDataString(credential)}", cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                return [];
            }

            var identities = await response.Content.ReadFromJsonAsync<List<KratosIdentityDto>>(cancellationToken: cancellationToken) ?? [];
            var identityIds = identities
                .Select(identity => identity.Id)
                .Distinct()
                .ToArray();

            var cacheEntryOptions = new MemoryCacheEntryOptions()
                .SetSlidingExpiration(TimeSpan.FromMinutes(5))
                .SetAbsoluteExpiration(TimeSpan.FromMinutes(15));

            _memoryCache.Set(cacheKey, identityIds, cacheEntryOptions);
            return identityIds;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to search identities in Kratos for credential lookup");
            return [];
        }
    }
}
