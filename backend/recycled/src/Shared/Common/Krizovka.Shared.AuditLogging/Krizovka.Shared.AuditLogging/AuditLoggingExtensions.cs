using Krizovka.Shared.ApiClient.Extensions;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;

namespace Krizovka.Shared.AuditLogging;

public static class AuditLoggingExtensions
{
    public static IServiceCollection AddMestoPlusAuditLogging(this IServiceCollection services, string serviceName, string alsUrl)
    {
        var skipWarnings = Environment.GetEnvironmentVariable("SKIP_DB_INIT") == "true";
        if (string.IsNullOrEmpty(alsUrl) && !skipWarnings)
        {
            Console.WriteLine($"CRITICAL WARNING: ALS_URL is missing for {serviceName}. Audit logging will fail.");
        }

        services.AddServiceHttpClient<IAuditLogClient, HttpAuditLogClient>(client =>
        {
            if (!string.IsNullOrEmpty(alsUrl))
            {
                client.BaseAddress = new Uri(alsUrl);
            }
        });

        services.AddSingleton(serviceName); // Register service name for middleware

        return services;
    }

    public static IApplicationBuilder UseMestoPlusAuditLogging(this IApplicationBuilder app, string serviceName)
    {
        return app.UseMiddleware<AuditLoggingMiddleware>(serviceName);
    }
}
