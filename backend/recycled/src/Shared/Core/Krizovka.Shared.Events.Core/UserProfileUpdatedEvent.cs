namespace Krizovka.Shared.Events.Core;

/// <summary>
/// Publikováno CPS (nebo jinou službou) při self-service úpravě osobních dat.
/// US (User Service) tento event naslouchá pro aktualizaci hlavního UserAccount objektu.
/// </summary>
public record UserProfileUpdatedEvent
{
    public Guid AccountId { get; init; }
    public string? Email { get; init; }
    public string? FirstName { get; init; }
    public string? LastName { get; init; }
    public string? PhoneNumber { get; init; }
    public DateTimeOffset UpdatedAt { get; init; } = DateTimeOffset.UtcNow;
}
