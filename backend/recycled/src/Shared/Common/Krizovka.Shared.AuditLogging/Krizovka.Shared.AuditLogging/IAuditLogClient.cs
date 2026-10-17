namespace Krizovka.Shared.AuditLogging;

public interface IAuditLogClient
{
    Task LogAsync(AuditLogEntry entry, CancellationToken cancellationToken = default);
}

public record AuditLogEntry(
    string ServiceName,
    string Action,
    string ResourceId,
    string Details,
    Guid? ActorId,
    string IpAddress,
    DateTimeOffset Timestamp
);
