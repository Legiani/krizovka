namespace Krizovka.Shared.Events.Core;

public interface IEvent
{
    Guid EventId { get; }
    DateTimeOffset Timestamp { get; }
    string EventType { get; }
}
