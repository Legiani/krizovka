using System.Text.Json;

namespace Krizovka.Users.Infrastructure.UserPreferences.DTOs;

/// <summary>
/// Response DTO for a single UserPreference record.
/// </summary>
public record UserPreferenceDto
{
    public Guid Id { get; init; }
    public Guid AccountId { get; init; }
    public Guid SystemId { get; init; }
    public JsonDocument? Favorites { get; init; }
    public JsonDocument? MenuLayout { get; init; }
    public JsonDocument? NotificationSettings { get; init; }
    public DateTimeOffset CreatedAt { get; init; }
    public DateTimeOffset? UpdatedAt { get; init; }
}

/// <summary>
/// Request DTO for creating or updating user preferences (upsert semantics).
/// All JSON fields are optional — only provided fields are updated.
/// </summary>
public record UpsertUserPreferenceRequest
{
    public JsonDocument? Favorites { get; init; }
    public JsonDocument? MenuLayout { get; init; }
    public JsonDocument? NotificationSettings { get; init; }
}
