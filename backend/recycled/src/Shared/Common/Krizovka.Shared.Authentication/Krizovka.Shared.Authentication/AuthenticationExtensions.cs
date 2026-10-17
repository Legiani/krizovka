using Krizovka.Shared.ApiClient.Extensions;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;

namespace Krizovka.Shared.Authentication;

public static class AuthenticationExtensions
{
    public static IServiceCollection AddMestoPlusAuthentication(
        this IServiceCollection services,
        Action<AuthenticationOptions>? configure = null)
    {
        var options = new AuthenticationOptions();
        configure?.Invoke(options);

        // Override with environment variables if present
        var usClusterUrl = Environment.GetEnvironmentVariable("US_CLUSTER_URL");
        var usUrl = Environment.GetEnvironmentVariable("US_URL");
        var srsClusterUrl = Environment.GetEnvironmentVariable("SRS_CLUSTER_URL");
        var srsUrl = Environment.GetEnvironmentVariable("SRS_URL");

        if (!string.IsNullOrEmpty(usClusterUrl))
        {
            options.UsServiceUrl = usClusterUrl;
        }
        else if (!string.IsNullOrEmpty(usUrl))
        {
            options.UsServiceUrl = usUrl;
        }

        if (string.IsNullOrEmpty(options.UsServiceUrl))
        {
            var skipWarnings = Environment.GetEnvironmentVariable("SKIP_DB_INIT") == "true";
            if (!skipWarnings)
            {
                Console.WriteLine("CRITICAL WARNING: US_URL or US_CLUSTER_URL environment variable is missing. Authentication may fail.");
            }
            options.UsServiceUrl = "http://localhost"; // Fallback to prevent startup crash
        }

        var cacheDuration = Environment.GetEnvironmentVariable("AUTH_CACHE_DURATION");
        if (!string.IsNullOrEmpty(cacheDuration) && int.TryParse(cacheDuration, out var duration))
        {
            options.CacheDurationSeconds = duration;
        }

        services.AddSingleton(options);
        services.AddMemoryCache();
        services.AddHttpContextAccessor();
        services.AddScoped<ICurrentUserService, CurrentUserService>();
        services.AddScoped<IBackofficeAccessibleSystemsService, BackofficeAccessibleSystemsService>();

        services.AddServiceHttpClient<IUsAuthClient, UsAuthClient>(client =>
        {
            client.BaseAddress = new Uri(options.UsServiceUrl);
            client.Timeout = TimeSpan.FromSeconds(15);
        });

        services.AddServiceHttpClient<IBackofficePermissionChecker, UsBackofficePermissionChecker>(client =>
        {
            client.BaseAddress = new Uri(options.UsServiceUrl);
            client.Timeout = TimeSpan.FromSeconds(15);
        });

        var effectiveSrsUrl = !string.IsNullOrEmpty(srsClusterUrl)
            ? srsClusterUrl
            : !string.IsNullOrEmpty(srsUrl)
                ? srsUrl
                : "http://localhost";

        services.AddServiceHttpClient<ISystemOwnerScopeResolver, SrsSystemOwnerScopeResolver>(client =>
        {
            client.BaseAddress = new Uri(effectiveSrsUrl);
            client.Timeout = TimeSpan.FromSeconds(15);
        });

        services.AddServiceHttpClient<ISrsMunicipalityDirectoryClient, SrsMunicipalityDirectoryClient>(client =>
        {
            client.BaseAddress = new Uri(effectiveSrsUrl);
            client.Timeout = TimeSpan.FromSeconds(15);
        });

        services.AddAuthentication(auth =>
        {
            auth.DefaultAuthenticateScheme = "MestoPlus";
            auth.DefaultChallengeScheme = "MestoPlus";
            auth.DefaultScheme = "MestoPlus";
        })
        .AddScheme<AuthenticationOptions, MestoPlusAuthenticationHandler>("MestoPlus", o =>
        {
            o.UsServiceUrl = options.UsServiceUrl;
            o.CacheDurationSeconds = options.CacheDurationSeconds;
            o.RequireAuthentication = options.RequireAuthentication;
        });

        return services;
    }

    /// <summary>
    /// Adds the 'Backoffice' authentication scheme as an alias for MestoPlusAuthenticationHandler.
    /// Call this AFTER AddMestoPlusAuthentication() in services that use
    /// [Authorize(AuthenticationSchemes = "Backoffice")] but do NOT have their own JWT Bearer setup.
    /// Do NOT call in US – US registers its own JWT Bearer 'Backoffice' scheme via DependencyInjection.cs.
    /// </summary>
    public static IServiceCollection AddBackofficeScheme(this IServiceCollection services,
        Action<AuthenticationOptions>? configure = null)
    {
        var options = new AuthenticationOptions();
        configure?.Invoke(options);

        // Override with environment variables (same logic as AddMestoPlusAuthentication)
        var usClusterUrl = Environment.GetEnvironmentVariable("US_CLUSTER_URL");
        var usUrl = Environment.GetEnvironmentVariable("US_URL");

        if (!string.IsNullOrEmpty(usClusterUrl))
            options.UsServiceUrl = usClusterUrl;
        else if (!string.IsNullOrEmpty(usUrl))
            options.UsServiceUrl = usUrl;

        services.AddAuthentication()
            .AddScheme<AuthenticationOptions, MestoPlusAuthenticationHandler>("Backoffice", o =>
            {
                o.UsServiceUrl = options.UsServiceUrl;
                o.CacheDurationSeconds = options.CacheDurationSeconds;
                o.RequireAuthentication = true;
            });

        return services;
    }

    public static IApplicationBuilder UseMestoPlusAuthentication(this IApplicationBuilder app)
    {
        return app.UseAuthentication();
    }
}
