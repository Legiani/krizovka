using Microsoft.AspNetCore.Builder;
 using Microsoft.Extensions.Configuration;
 using Microsoft.Extensions.DependencyInjection;
 using Microsoft.AspNetCore.ResponseCompression;
 using System.IO.Compression;
 using Krizovka.Shared.Authentication;
 using Krizovka.Shared.ErrorHandling;
 using Krizovka.Shared.AuditLogging;
 using Krizovka.Shared.ServiceRegistry;

namespace Krizovka.Shared.AspNetCore;

public static class StandardSetupExtensions
{
    /// <summary>
    /// Registers all standard Krizovka services (Controllers, Error Handling, Auth, Audit, CORS).
    /// </summary>
    public static IServiceCollection AddMestoPlusApiStandard(
        this IServiceCollection services,
        IConfiguration configuration,
        string serviceName,
        string? routePrefix = null,
        Action<AuthenticationOptions>? configureAuth = null,
        bool enableAuditLogging = true,
        bool enableBackofficeScheme = true)
    {
        // 1. Controllers with KOS standards (JSON, route prefix, model validation)
        services.AddKosControllers(routePrefix);

        // 2. Standard Error Handling (including MediatR Validation pipeline)
        services.AddMestoPlusErrorHandling();

        // 3. Authentication (using standard environment variables or config)
        var usUrl = Environment.GetEnvironmentVariable("US_URL") ?? configuration.GetValue<string>("US_URL") ?? "http://krizovka-users:8080";
        services.AddMestoPlusAuthentication(options =>
        {
            options.UsServiceUrl = usUrl;
            options.RequireAuthentication = true;
            configureAuth?.Invoke(options);
        });
        if (enableBackofficeScheme)
        {
            services.AddBackofficeScheme();
        }

        // 4. Audit Logging (optional to avoid self-auditing in ALS)
        if (enableAuditLogging)
        {
            var alsUrl = Environment.GetEnvironmentVariable("ALS_URL") ?? configuration.GetValue<string>("ALS_URL") ?? "http://krizovka-audit-disabled";
            services.AddMestoPlusAuditLogging(serviceName, alsUrl);
        }

        // 5. CORS
        services.AddMestoPlusCors(configuration);

        // 6. Response compression — reduces JSON payload by 60–80 %
        //    Brotli preferred (better ratio), Gzip as fallback for older clients.
        services.AddResponseCompression(options =>
        {
            options.EnableForHttps = true; // Safe: API responses are not web pages susceptible to BREACH
            options.Providers.Add<BrotliCompressionProvider>();
            options.Providers.Add<GzipCompressionProvider>();
            options.MimeTypes = ResponseCompressionDefaults.MimeTypes.Concat(new[]
            {
                "application/json",
                "application/geo+json",
                "application/problem+json",
                "text/plain",
            });
        });
        services.Configure<BrotliCompressionProviderOptions>(options =>
            options.Level = CompressionLevel.Fastest); // Fastest: CPU-efficient (best latency)
        services.Configure<GzipCompressionProviderOptions>(options =>
            options.Level = CompressionLevel.Fastest);

        return services;
    }

    /// <summary>
    /// Configures the standard Krizovka middleware pipeline.
    /// </summary>
    public static IApplicationBuilder UseMestoPlusApiStandard(
        this IApplicationBuilder app,
        string serviceName,
        bool enableAuditLogging = true)
    {
        // Exception handling should be first
        app.UseMestoPlusErrorHandling();

        // Request logging right after error handling so it catches exceptions too
        app.UseMestoPlusRequestLogging();

        // Response compression — must be before routing to compress all responses
        app.UseResponseCompression();

        // Standard ASP.NET Core middleware
        app.UseRouting();
        app.UseCors();

        app.UseMestoPlusAuthentication();
        app.UseAuthorization();

        // Audit Logging after Auth to capture user identity
        if (enableAuditLogging)
        {
            app.UseMestoPlusAuditLogging(serviceName);
        }

        return app;
    }
}
