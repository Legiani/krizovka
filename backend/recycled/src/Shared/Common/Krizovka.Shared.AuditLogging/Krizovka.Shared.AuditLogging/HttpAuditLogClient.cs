using System.Net.Http.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;

namespace Krizovka.Shared.AuditLogging;

public class HttpAuditLogClient : IAuditLogClient
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<HttpAuditLogClient> _logger;

    public HttpAuditLogClient(HttpClient httpClient, ILogger<HttpAuditLogClient> logger, IConfiguration configuration)
    {
        _httpClient = httpClient;
        _logger = logger;

        if (_httpClient.BaseAddress == null)
        {
            _logger.LogCritical(
                "[AuditLogging] HttpAuditLogClient has no BaseAddress. " +
                "ALS_URL environment variable is missing. All audit log writes will fail! " +
                "Set ALS_URL to the URL of the ALS service.");
        }
    }

    public async Task LogAsync(AuditLogEntry entry, CancellationToken cancellationToken = default)
    {
        if (_httpClient.BaseAddress == null)
        {
            _logger.LogWarning("[AuditLogging] Skipping audit log write — ALS BaseAddress not configured.");
            return;
        }

        try
        {
            var request = new HttpRequestMessage(HttpMethod.Post, "/v1/audit-events")
            {
                Content = JsonContent.Create(entry)
            };
            request.Headers.Add("X-Internal-Audit-Log", "true");

            var response = await _httpClient.SendAsync(request, cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning(
                    "[AuditLogging] Failed to send audit log to ALS. Status: {StatusCode}. " +
                    "Check that INTERNAL_API_KEY matches on both sides.",
                    response.StatusCode);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[AuditLogging] Error sending audit log to ALS");
            // We don't throw here to prevent audit logging failure from breaking the main flow
        }
    }
}
