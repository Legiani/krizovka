namespace Krizovka.Shared.Events.Core;

public interface IBaseEvent
{
    Guid EventId { get; }
    DateTimeOffset OccurredAt { get; }
}

public record AuditEventRecorded(
    Guid EventId,
    DateTimeOffset OccurredAt,
    string SystemId,
    string SourceService,
    string EventName,
    string Actor,
    string Target,
    string Payload,
    string LogLevel,
    string SensitivityLevel
) : IBaseEvent;
