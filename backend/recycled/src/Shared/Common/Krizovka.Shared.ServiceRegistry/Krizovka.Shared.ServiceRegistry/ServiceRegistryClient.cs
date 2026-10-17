using Krizovka.Shared.ServiceRegistry.Models;
using Microsoft.Extensions.Logging;
using System.Text.Json;
using Microsoft.Extensions.Configuration;

namespace Krizovka.Shared.ServiceRegistry;

public class ServiceRegistryClient : IServiceRegistryClient
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<ServiceRegistryClient> _logger;
    private readonly JsonSerializerOptions _jsonOptions;

    public ServiceRegistryClient(HttpClient httpClient, ILogger<ServiceRegistryClient> logger, IConfiguration configuration)
    {
        _httpClient = httpClient;
        _logger = logger;
        _jsonOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };
    }

    public async Task RegisterServiceAsync(ServiceRegistration registration, CancellationToken cancellationToken = default)
    {
        try
        {
            var json = JsonSerializer.Serialize(registration, _jsonOptions);
            var content = new StringContent(json, System.Text.Encoding.UTF8, "application/json");

            var response = await _httpClient.PostAsync("/v1/srs/services/register", content, cancellationToken);
            response.EnsureSuccessStatusCode();

            _logger.LogInformation("Service {ServiceName} registered successfully", registration.ServiceName);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to register service {ServiceName}", registration.ServiceName);
            throw;
        }
    }

    public async Task UpdateHealthAsync(ServiceHealthUpdate healthUpdate, CancellationToken cancellationToken = default)
    {
        try
        {
            var json = JsonSerializer.Serialize(healthUpdate, _jsonOptions);
            var content = new StringContent(json, System.Text.Encoding.UTF8, "application/json");

            var response = await _httpClient.PutAsync($"/v1/srs/services/{healthUpdate.ServiceName}/health", content, cancellationToken);
            response.EnsureSuccessStatusCode();
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to update health for service {ServiceName}", healthUpdate.ServiceName);
            // Don't throw - health check failures shouldn't crash the service
        }
    }

    public async Task<ServiceInfo?> GetServiceAsync(string serviceName, CancellationToken cancellationToken = default)
    {
        try
        {
            var response = await _httpClient.GetAsync($"/v1/srs/services/{serviceName}", cancellationToken);

            if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
                return null;

            response.EnsureSuccessStatusCode();

            var json = await response.Content.ReadAsStringAsync(cancellationToken);
            return JsonSerializer.Deserialize<ServiceInfo>(json, _jsonOptions);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get service {ServiceName}", serviceName);
            return null;
        }
    }

    public async Task<IEnumerable<ServiceInfo>> GetAllServicesAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var response = await _httpClient.GetAsync("/v1/srs/services", cancellationToken);
            response.EnsureSuccessStatusCode();

            var json = await response.Content.ReadAsStringAsync(cancellationToken);
            return JsonSerializer.Deserialize<IEnumerable<ServiceInfo>>(json, _jsonOptions) ?? Enumerable.Empty<ServiceInfo>();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get all services");
            return Enumerable.Empty<ServiceInfo>();
        }
    }

    public async Task DeregisterServiceAsync(string serviceName, CancellationToken cancellationToken = default)
    {
        try
        {
            var response = await _httpClient.DeleteAsync($"/v1/srs/services/{serviceName}", cancellationToken);
            response.EnsureSuccessStatusCode();

            _logger.LogInformation("Service {ServiceName} deregistered successfully", serviceName);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to deregister service {ServiceName}", serviceName);
            // Don't throw - deregistration failures shouldn't crash the service
        }
    }
}
