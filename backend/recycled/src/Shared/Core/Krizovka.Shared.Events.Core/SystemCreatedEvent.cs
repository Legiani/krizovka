namespace Krizovka.Shared.Events.Core;

public record SystemCreatedEvent
{
    public Guid EventId { get; init; } = Guid.NewGuid();
    public DateTimeOffset OccurredAt { get; init; } = DateTimeOffset.UtcNow;
    public string SystemId { get; init; } = string.Empty;
    public string Name { get; init; } = string.Empty;
    public string Currency { get; init; } = string.Empty;
    public string Timezone { get; init; } = string.Empty;
}
