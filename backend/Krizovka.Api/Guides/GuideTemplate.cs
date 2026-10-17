namespace Krizovka.Api.Guides;

public sealed class GuideTemplate
{
    public string Id { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public string Summary { get; set; } = string.Empty;
    public string QuickSummary { get; set; } = string.Empty;
    public string DiagramMermaid { get; set; } = string.Empty;
    public string LegalBasisJson { get; set; } = "[]";
    public string RecommendedOrgTypesJson { get; set; } = "[]";
    public int Version { get; set; } = 1;
    public string StepsJson { get; set; } = "[]";
    public string EmergencyContactsJson { get; set; } = "[]";
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
}
