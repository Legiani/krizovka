using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Krizovka.Shared.ServiceRegistry;

public static class CorsExtensions
{
    public static IServiceCollection AddMestoPlusCors(this IServiceCollection services, IConfiguration configuration)
    {
        var environmentName = configuration["ASPNETCORE_ENVIRONMENT"]
            ?? configuration["DOTNET_ENVIRONMENT"]
            ?? Environments.Production;
        var isLocalOriginEnvironment =
            string.Equals(environmentName, Environments.Development, StringComparison.OrdinalIgnoreCase) ||
            string.Equals(environmentName, "Testing", StringComparison.OrdinalIgnoreCase);

        var extraOriginsConfig = configuration["ALLOWED_ORIGINS"];
        var extraOrigins = !string.IsNullOrEmpty(extraOriginsConfig)
            ? extraOriginsConfig.Split(';', StringSplitOptions.RemoveEmptyEntries).ToHashSet()
            : new HashSet<string>();

        // We use SetIsOriginAllowed dynamically instead of hardcoded 100+ strings
        // to cover all future local services and nip.io subdomains.
        services.AddCors(options =>
        {
            var policyBuilder = new Action<Microsoft.AspNetCore.Cors.Infrastructure.CorsPolicyBuilder>(policy =>
            {
                policy.SetIsOriginAllowed(origin =>
                        origin == "https://app.cityplus.qubert.cz" ||
                        origin == "https://cityplus-app.qubert.cz" ||
                        origin == "https://cityplus-backoffice.qubert.cz" ||
                        origin == "capacitor://localhost" ||
                        (isLocalOriginEnvironment && origin.EndsWith(".192.168.1.39.nip.io")) ||
                        (isLocalOriginEnvironment && origin.StartsWith("http://localhost:")) ||
                        (isLocalOriginEnvironment && origin.StartsWith("https://localhost:")) ||
                        extraOrigins.Contains(origin))
                    .AllowAnyMethod()
                    .AllowAnyHeader()
                    .AllowCredentials();
            });

            options.AddDefaultPolicy(policyBuilder);
            options.AddPolicy("GatewayClients", policyBuilder);
        });

        return services;
    }
}
