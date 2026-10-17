namespace Krizovka.Api.Reports;

public class IssueHistory
{
    public Guid Id { get; set; }
    public Guid IssueId { get; set; }
    public Guid ActorId { get; set; } // User who performed the action
    public string Action { get; set; } = string.Empty; // e.g. "StatusChange", "Comment", "TagUpdate"
    public string? PreviousValue { get; set; }
    public string? NewValue { get; set; }
    public string? Metadata { get; set; } // JSON for extra details
    public DateTimeOffset CreatedAt { get; set; }

    public Issue Issue { get; set; } = null!;
}
