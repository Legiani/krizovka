using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.Logging;

namespace Krizovka.Shared.Authentication;

public sealed class SrsSystemOwnerScopeResolver : ISystemOwnerScopeResolver
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    private readonly HttpClient _httpClient;
    private readonly ILogger<SrsSystemOwnerScopeResolver> _logger;

    public SrsSystemOwnerScopeResolver(
        HttpClient httpClient,
        ILogger<SrsSystemOwnerScopeResolver> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
    }

    public async Task<ResolvedOwnerScope?> ResolveSystemScopeAsync(
        string systemCode,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(systemCode))
        {
            return null;
        }

        try
        {
            var response = await _httpClient.GetAsync(
                $"v1/srs/cities/by-code/{Uri.EscapeDataString(systemCode)}",
                cancellationToken);

            if (response.StatusCode == HttpStatusCode.NotFound)
            {
                return null;
            }

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning(
                    "SRS municipality lookup failed for systemCode {SystemCode}. Status: {StatusCode}",
                    systemCode,
                    response.StatusCode);
                return null;
            }

            var municipality = await response.Content.ReadFromJsonAsync<MunicipalityLookupResponse>(JsonOptions, cancellationToken);
            var scopeType = MapMunicipalityTypeToScopeType(municipality?.Type);
            if (scopeType == null || string.IsNullOrWhiteSpace(municipality?.Code))
            {
                return null;
            }

            return new ResolvedOwnerScope(scopeType, municipality.Code);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Exception resolving municipality scope for systemCode {SystemCode}", systemCode);
            return null;
        }
    }

    private static string? MapMunicipalityTypeToScopeType(string? municipalityType)
    {
        return municipalityType?.ToUpperInvariant() switch
        {
            "CITY" => "CITY",
            "IDS" => "IDS",
            "PROVIDER" => "PROVIDER",
            "CARRIER" => "CARRIER",
            "ORGANIZATION" => "ORGANIZATION",
            _ => null
        };
    }

    private sealed record MunicipalityLookupResponse(
        string? Code,
        string? Type);
}
