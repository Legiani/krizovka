using System.Text.Json.Serialization;

namespace Krizovka.Api.Guides;

public sealed record GuideTemplateDto(
    string Id,
    string Title,
    string Category,
    string Summary,
    string QuickSummary,
    string DiagramMermaid,
    string LegalBasisJson,
    string RecommendedOrgTypesJson,
    int Version,
    string StepsJson,
    string EmergencyContactsJson,
    DateTimeOffset CreatedAt
);

public sealed record GuideDto(
    Guid Id,
    Guid OrganizationId,
    string? SourceTemplateId,
    int? SourceVersion,
    string Title,
    string Category,
    string Summary,
    string QuickSummary,
    string DiagramMermaid,
    string LegalBasisJson,
    [property: JsonConverter(typeof(JsonStringEnumConverter))]
    GuideStatus Status,
    string StepsJson,
    string EmergencyContactsJson,
    DateTimeOffset? PublishedAt,
    DateTimeOffset CreatedAt,
    DateTimeOffset? UpdatedAt
);

public sealed record CreateGuideRequest(
    Guid OrganizationId,
    string Title,
    string Category,
    string Summary,
    string? QuickSummary,
    string? DiagramMermaid,
    string? LegalBasisJson,
    string? StepsJson,
    string? EmergencyContactsJson
);

public sealed record UpdateGuideRequest(
    string Title,
    string Category,
    string Summary,
    string? QuickSummary,
    string? DiagramMermaid,
    string? LegalBasisJson,
    [property: JsonConverter(typeof(JsonStringEnumConverter))]
    GuideStatus? Status,
    string? StepsJson,
    string? EmergencyContactsJson
);

public sealed record CloneGuideRequest(
    string TemplateId,
    Guid OrganizationId
);

public sealed record BatchCloneGuidesRequest(
    List<string> TemplateIds,
    Guid OrganizationId
);
