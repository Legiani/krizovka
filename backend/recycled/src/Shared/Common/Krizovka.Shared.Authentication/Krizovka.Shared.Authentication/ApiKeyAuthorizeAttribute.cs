using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Krizovka.Shared.Authentication;

[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
public class ApiKeyAuthorizeAttribute : Attribute, IAsyncAuthorizationFilter
{
    private const string ApiKeyHeaderName = "X-Api-Key";

    public Task OnAuthorizationAsync(AuthorizationFilterContext context)
    {
        var configuration = context.HttpContext.RequestServices.GetRequiredService<IConfiguration>();
        var logger = context.HttpContext.RequestServices.GetService<ILogger<ApiKeyAuthorizeAttribute>>();
        var configuredApiKey = configuration["INTERNAL_API_KEY"] ??
                               configuration["InternalApiKey"] ??
                               Environment.GetEnvironmentVariable("INTERNAL_API_KEY");

        if (string.IsNullOrWhiteSpace(configuredApiKey))
        {
            logger?.LogCritical(
                "INTERNAL_API_KEY is not configured. Rejecting ApiKeyAuthorize request in fail-closed mode.");
            context.Result = new StatusCodeResult(500);
            return Task.CompletedTask;
        }

        if (!context.HttpContext.Request.Headers.TryGetValue(ApiKeyHeaderName, out var extractedApiKey))
        {
            context.Result = new UnauthorizedResult();
            return Task.CompletedTask;
        }

        if (configuredApiKey != extractedApiKey.ToString())
        {
            context.Result = new UnauthorizedResult();
            return Task.CompletedTask;
        }

        return Task.CompletedTask;
    }
}
