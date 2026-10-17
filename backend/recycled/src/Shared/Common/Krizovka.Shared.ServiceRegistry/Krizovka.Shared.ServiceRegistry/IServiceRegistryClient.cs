using Krizovka.Shared.ServiceRegistry.Models;

namespace Krizovka.Shared.ServiceRegistry;

public interface IServiceRegistryClient
{
    Task RegisterServiceAsync(ServiceRegistration registration, CancellationToken cancellationToken = default);
    Task UpdateHealthAsync(ServiceHealthUpdate healthUpdate, CancellationToken cancellationToken = default);
    Task<ServiceInfo?> GetServiceAsync(string serviceName, CancellationToken cancellationToken = default);
    Task<IEnumerable<ServiceInfo>> GetAllServicesAsync(CancellationToken cancellationToken = default);
    Task DeregisterServiceAsync(string serviceName, CancellationToken cancellationToken = default);
}
