using System.Text.Json;

namespace Krizovka.Users.Domain.Entities;

/// <summary>
/// Stores per-user preferences for the Město+ mobile app.
/// Includes favorite items, custom menu order, and notification settings.
/// One record per user per city (SystemId).
/// </summary>
public class UserPreference
{
    public Guid Id { get; set; }
    public Guid AccountId { get; set; }

    /// <summary>
    /// The city/municipality this preference applies to.
    /// A user may have preferences for multiple municipalities.
    /// </summary>
    public Guid SystemId { get; set; }

    /// <summary>
    /// Favorited items across the app.
    /// e.g. [{"type":"spolek","id":"...","name":"SDH Tišice"}, {"type":"sekce","id":"akce","name":"Akce"}]
    /// </summary>
    public JsonDocument? Favorites { get; set; }

    /// <summary>
    /// Custom menu configuration (max 5 items, with sort order).
    /// e.g. [{"key":"novinky","order":0}, {"key":"akce","order":1}, {"key":"svoz","order":2}]
    /// </summary>
    public JsonDocument? MenuLayout { get; set; }

    /// <summary>
    /// Notification preferences per category.
    /// e.g. {"waste_collection":true, "news":true, "crisis":true, "events":false}
    /// </summary>
    public JsonDocument? NotificationSettings { get; set; }

    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset? UpdatedAt { get; set; }

    // Navigation
    public UserAccount UserAccount { get; set; } = null!;
}
