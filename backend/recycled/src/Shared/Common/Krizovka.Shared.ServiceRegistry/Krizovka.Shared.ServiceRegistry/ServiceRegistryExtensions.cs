using Krizovka.Shared.ApiClient.Extensions;
using Krizovka.Shared.ServiceRegistry.Models;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Krizovka.Shared.ServiceRegistry;

public static class ServiceRegistryExtensions
{
    public static IServiceCollection AddMestoPlusServiceRegistry(
        this IServiceCollection services,
        string serviceName,
        string serviceUrl,
        string srsUrl)
    {
        var skipWarnings = Environment.GetEnvironmentVariable("SKIP_DB_INIT") == "true";
        if (!skipWarnings)
        {
            if (string.IsNullOrEmpty(serviceName)) Console.WriteLine("CRITICAL WARNING: serviceName is missing in AddKosServiceRegistry");
            if (string.IsNullOrEmpty(serviceUrl)) Console.WriteLine($"CRITICAL WARNING: serviceUrl is missing for {serviceName}");
            if (string.IsNullOrEmpty(srsUrl)) Console.WriteLine($"CRITICAL WARNING: srsUrl is missing for {serviceName}");
        }

        services.AddServiceHttpClient<IServiceRegistryClient, ServiceRegistryClient>(client =>
        {
            if (!string.IsNullOrEmpty(srsUrl))
            {
                client.BaseAddress = new Uri(srsUrl);
            }
            client.Timeout = TimeSpan.FromSeconds(10);
        });

        services.AddSingleton(new ServiceRegistrationOptions
        {
            ServiceName = serviceName ?? "UNKNOWN",
            ServiceUrl = serviceUrl ?? "http://unknown",
            SrsUrl = srsUrl ?? "http://unknown"
        });

        services.AddHostedService<ServiceRegistrationBackgroundService>();

        return services;
    }
}

public class ServiceRegistrationOptions
{
    public string ServiceName { get; set; } = string.Empty;
    public string ServiceUrl { get; set; } = string.Empty;
    public string SrsUrl { get; set; } = string.Empty;
}

public class ServiceRegistrationBackgroundService : BackgroundService
{
    private readonly IServiceRegistryClient _client;
    private readonly ServiceRegistrationOptions _options;
    private readonly ILogger<ServiceRegistrationBackgroundService> _logger;

    public ServiceRegistrationBackgroundService(
        IServiceRegistryClient client,
        ServiceRegistrationOptions options,
        ILogger<ServiceRegistrationBackgroundService> logger)
    {
        _client = client;
        _options = options;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        // Wait a bit for the service to fully start
        await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);

        // Register service
        try
        {
            var registration = new ServiceRegistration(
                _options.ServiceName,
                _options.ServiceUrl,
                "1.0.0",
                new Dictionary<string, string>
                {
                    { "environment", "development" },
                    { "registeredAt", DateTimeOffset.UtcNow.ToString("O") }
                }
            );

            await _client.RegisterServiceAsync(registration, stoppingToken);
            _logger.LogInformation("Service {ServiceName} registered with SRS", _options.ServiceName);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to register service {ServiceName} with SRS", _options.ServiceName);
        }

        // Periodic health check updates (every 30 seconds)
        while (!stoppingToken.IsCancellationRequested)
        {
            await Task.Delay(TimeSpan.FromSeconds(30), stoppingToken);

            try
            {
                var healthUpdate = new ServiceHealthUpdate(
                    _options.ServiceName,
                    "healthy",
                    new Dictionary<string, string>
                    {
                        { "lastCheck", DateTimeOffset.UtcNow.ToString("O") },
                        { "uptime", TimeSpan.FromSeconds(Environment.TickCount64 / 1000).ToString() }
                    }
                );

                await _client.UpdateHealthAsync(healthUpdate, stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to update health for service {ServiceName}", _options.ServiceName);
            }
        }

        // Deregister on shutdown
        try
        {
            await _client.DeregisterServiceAsync(_options.ServiceName, CancellationToken.None);
            _logger.LogInformation("Service {ServiceName} deregistered from SRS", _options.ServiceName);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to deregister service {ServiceName} from SRS", _options.ServiceName);
        }
    }
}
