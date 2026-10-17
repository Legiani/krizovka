namespace Krizovka.Shared.Events.Core;

/// <summary>
/// Published by US when a new user account is created.
/// Consumed by: CPS (initialize empty CustomerProfile).
/// </summary>
public record UserCreatedEvent
{
    public Guid   AccountId   { get; init; }
    public string Email       { get; init; } = string.Empty;
    public string SystemId    { get; init; } = string.Empty;
    public DateOnly? DateOfBirth { get; init; }
    public DateTimeOffset CreatedAt { get; init; } = DateTimeOffset.UtcNow;
}

/// <summary>
/// Published by US when a user account is deleted (GDPR right-to-erasure).
/// Consumed by: CPS (scrub PII - profile, evidence documents).
/// </summary>
public record UserDeletedEvent
{
    public Guid   AccountId { get; init; }
    public DateTimeOffset DeletedAt { get; init; } = DateTimeOffset.UtcNow;
}
