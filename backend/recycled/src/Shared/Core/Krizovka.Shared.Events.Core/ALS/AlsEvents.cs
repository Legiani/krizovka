namespace Krizovka.Shared.Events.Core.ALS;

public record AuditLogCreatedEvent(
    Guid EventId,
    DateTimeOffset Timestamp,
    Guid LogId,
    string ServiceName,
    string Action,
    Guid? ActorId
) : IEvent
{
    public string EventType => "ALS.AuditLogCreated";
}
