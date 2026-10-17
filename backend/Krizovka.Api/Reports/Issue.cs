namespace Krizovka.Api.Reports;

// Adapted from City+ IS Issue; city workflows and mandatory GPS were removed.
public sealed class Issue
{
    public Guid Id { get; set; }
    public Guid OrganizationId { get; set; }
    public Guid ReporterAccountId { get; set; }
    public Guid ClientReportId { get; set; }
    public string? Description { get; set; }
    public ReportStatus Status { get; set; } = ReportStatus.New;
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset? UpdatedAt { get; set; }
    public double? Latitude { get; set; }
    public double? Longitude { get; set; }
    public double? AccuracyMeters { get; set; }
    public DateTimeOffset? LocationMeasuredAt { get; set; }
    public ContactSnapshot Contact { get; set; } = new();
    public ICollection<IssueAttachment> Attachments { get; set; } = new List<IssueAttachment>();
    public ICollection<IssueHistory> History { get; set; } = new List<IssueHistory>();
}

public enum ReportStatus { New, Accepted, Closed }

public sealed class ContactSnapshot
{
    public string? FirstName { get; set; }
    public string? LastName { get; set; }
    public string? PersonnelNumber { get; set; }
    public string? Phone { get; set; }
    public string? Email { get; set; }
}
