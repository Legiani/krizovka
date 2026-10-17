namespace Krizovka.Shared.ServiceRegistry.Models;

public record ServiceRegistration(
    string ServiceName,
    string ServiceUrl,
    string Version,
    Dictionary<string, string> Metadata
);

public record ServiceHealthUpdate(
    string ServiceName,
    string Status,
    Dictionary<string, string> Details
);

public record ServiceInfo(
    string ServiceName,
    string ServiceUrl,
    string Version,
    string Status,
    DateTimeOffset RegisteredAt,
    DateTimeOffset LastHealthCheck,
    Dictionary<string, string> Metadata
);
