using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using System.Security.Claims;

namespace Krizovka.Shared.AuditLogging;

public class AuditLoggingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly IAuditLogClient _auditClient;
    private readonly string _serviceName;
    private readonly ILogger<AuditLoggingMiddleware> _logger;

    public AuditLoggingMiddleware(
        RequestDelegate next,
        IAuditLogClient auditClient,
        ILogger<AuditLoggingMiddleware> logger,
        string serviceName)
    {
        _next = next;
        _auditClient = auditClient;
        _logger = logger;
        _serviceName = serviceName;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        // Capture request details before processing
        var method = context.Request.Method;
        var path = context.Request.Path;

        // Prevent circular recursion: don't log requests from the audit log client itself
        if (context.Request.Headers.ContainsKey("X-Internal-Audit-Log") ||
            path.StartsWithSegments("/v1/audit-events") ||
            path.StartsWithSegments("/swagger") ||
            path.Value?.Contains("/health", StringComparison.OrdinalIgnoreCase) == true ||
            path.Value?.Contains("/heartbeat", StringComparison.OrdinalIgnoreCase) == true)
        {
            await _next(context);
            return;
        }

        // Only log state-changing operations (POST, PUT, DELETE, PATCH)
        var shouldLog = method == "POST" || method == "PUT" || method == "DELETE" || method == "PATCH";

        await _next(context);

        // Log after the response so we can include the status code
        if (shouldLog)
        {
            // Fire-and-forget with a short timeout to avoid blocking, but capture errors via logger
            _ = LogRequestAsync(context, method, path, context.Response.StatusCode);
        }
    }

    private async Task LogRequestAsync(HttpContext context, string method, string path, int statusCode)
    {
        try
        {
            var actorIdStr = context.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            Guid? actorId = Guid.TryParse(actorIdStr, out var id) ? id : null;
            var ipAddress = context.Connection.RemoteIpAddress?.ToString() ?? "unknown";
            var resourceId = GetResourceIdFromPath(path);
            var actionName = $"{method} {path}";

            _logger.LogDebug("[AuditLogging] Recording audit event: {ActionName} by actor {ActorId}", actionName, actorId);

            var entry = new AuditLogEntry(
                _serviceName,
                actionName,
                resourceId,
                $"HTTP {statusCode} – {method} {path}",
                actorId,
                ipAddress,
                DateTimeOffset.UtcNow
            );

            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));
            await _auditClient.LogAsync(entry, cts.Token);
        }
        catch (OperationCanceledException)
        {
            _logger.LogWarning("[AuditLogging] Audit log write timed out for {Method} {Path}", method, path);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[AuditLogging] Unhandled error writing audit log for {Method} {Path}", method, path);
        }
    }

    private string GetResourceIdFromPath(string path)
    {
        // Search through segments to find the first valid Guid
        var segments = path.Split('/', StringSplitOptions.RemoveEmptyEntries);
        // We reverse because the ID is usually near the end e.g. /v1/cps/.../{id}/photo
        foreach (var segment in segments.Reverse())
        {
            if (Guid.TryParse(segment, out _))
            {
                return segment;
            }
        }
        return string.Empty;
    }
}
