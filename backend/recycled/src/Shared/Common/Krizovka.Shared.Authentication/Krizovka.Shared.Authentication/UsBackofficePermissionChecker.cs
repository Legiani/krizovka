using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.Logging;

namespace Krizovka.Shared.Authentication;

/// <summary>
/// Shared fallback permission checker for services outside US.
/// Uses the canonical US backoffice permission endpoint instead of silently skipping enforcement.
/// </summary>
public class UsBackofficePermissionChecker : IBackofficePermissionChecker
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    private readonly HttpClient _httpClient;
    private readonly ILogger<UsBackofficePermissionChecker> _logger;

    public UsBackofficePermissionChecker(HttpClient httpClient, ILogger<UsBackofficePermissionChecker> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
    }

    public async Task<bool> HasPermissionAsync(
        string identityId,
        string module,
        string action,
        string? scopeType = null,
        string? scopeId = null)
    {
        if (!Guid.TryParse(identityId, out var identityGuid))
        {
            return false;
        }

        try
        {
            var response = await _httpClient.PostAsJsonAsync(
                "v1/us/backoffice/auth/check-permission",
                new CheckPermissionRequest(identityGuid, module, action, scopeType, scopeId));

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning(
                    "US backoffice permission check failed. Status: {StatusCode}. BaseUrl: {BaseUrl}",
                    response.StatusCode,
                    _httpClient.BaseAddress);
                return false;
            }

            var result = await response.Content.ReadFromJsonAsync<CheckPermissionResponse>(JsonOptions);
            return result?.Allowed == true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Exception sending permission check to US at {BaseUrl}", _httpClient.BaseAddress);
            return false;
        }
    }

    private sealed record CheckPermissionRequest(
        Guid IdentityId,
        string Module,
        string Action,
        string? ScopeType,
        string? ScopeId);

    private sealed record CheckPermissionResponse(bool Allowed);
}
