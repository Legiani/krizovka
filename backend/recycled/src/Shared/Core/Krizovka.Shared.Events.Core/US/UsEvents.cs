namespace Krizovka.Shared.Events.Core.US;

public record AccountCreatedEvent(
    Guid EventId,
    DateTimeOffset Timestamp,
    Guid AccountId,
    string AccountType,
    string? Source
) : IEvent
{
    public string EventType => "US.AccountCreated";
}

public record RoleAssignedEvent(
    Guid EventId,
    DateTimeOffset Timestamp,
    Guid AssignmentId,
    Guid AccountId,
    string RoleId,
    Guid AssignedBy
) : IEvent
{
    public string EventType => "US.RoleAssigned";
}

public record AccountDeletedEvent(
    Guid EventId,
    DateTimeOffset Timestamp,
    Guid AccountId
) : IEvent
{
    public string EventType => "US.AccountDeleted";
}
