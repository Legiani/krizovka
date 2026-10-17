namespace Krizovka.Api.Guides;

public enum GuideStatus
{
    Draft,
    Published,
    Archived
}

public sealed class Guide
{
    public Guid Id { get; set; }
    public Guid OrganizationId { get; set; }
    public string? SourceTemplateId { get; set; }
    public int? SourceVersion { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public string Summary { get; set; } = string.Empty;
    public string QuickSummary { get; set; } = string.Empty;
    public string DiagramMermaid { get; set; } = string.Empty;
    public string LegalBasisJson { get; set; } = "[]";
    public GuideStatus Status { get; set; } = GuideStatus.Draft;
    public string StepsJson { get; set; } = "[]";
    public string EmergencyContactsJson { get; set; } = "[]";
    public DateTimeOffset? PublishedAt { get; set; }
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset? UpdatedAt { get; set; }
}
