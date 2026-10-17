namespace Krizovka.Shared.Events.Core.NFS;

public record NotificationSentEvent(
    Guid EventId,
    DateTimeOffset Timestamp,
    Guid NotificationId,
    Guid RecipientId,
    string Channel,
    string Status
) : IEvent
{
    public string EventType => "NFS.NotificationSent";
}
