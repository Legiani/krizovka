using System.Text.Json.Serialization;

namespace Krizovka.Api.Reports;

public sealed record CreateReportRequest(
    Guid ClientReportId,
    Guid OrganizationId,
    string? Description,
    double? Latitude,
    double? Longitude,
    double? AccuracyMeters,
    DateTimeOffset? LocationMeasuredAt,
    ContactSnapshot? Contact
);

public sealed record UpdateReportStatusRequest(
    [property: JsonConverter(typeof(JsonStringEnumConverter))]
    ReportStatus Status,
    string? Note
);

public sealed record UpdateReportLocationRequest(
    double Latitude,
    double Longitude,
    double? AccuracyMeters,
    DateTimeOffset? LocationMeasuredAt
);

public sealed record AddReportMessageRequest(
    string Message
);

public sealed record StartStreamRequest(
    string Mode // "audio" or "video"
);

public sealed record StreamSessionDto(
    Guid ReportId,
    string RoomName,
    string Token,
    string ServerUrl,
    string Mode,
    bool UplinkOnly,
    DateTimeOffset ExpiresAt
);

public sealed record ReportSummaryDto(
    Guid Id,
    Guid OrganizationId,
    Guid ClientReportId,
    string? Description,
    [property: JsonConverter(typeof(JsonStringEnumConverter))]
    ReportStatus Status,
    DateTimeOffset CreatedAt,
    DateTimeOffset? UpdatedAt,
    double? Latitude,
    double? Longitude,
    double? AccuracyMeters,
    DateTimeOffset? LocationMeasuredAt,
    ContactSnapshot Contact,
    string? IpAddress = null
);

public sealed record ReportDetailDto(
    Guid Id,
    Guid OrganizationId,
    Guid ClientReportId,
    Guid ReporterAccountId,
    string? Description,
    [property: JsonConverter(typeof(JsonStringEnumConverter))]
    ReportStatus Status,
    DateTimeOffset CreatedAt,
    DateTimeOffset? UpdatedAt,
    double? Latitude,
    double? Longitude,
    double? AccuracyMeters,
    DateTimeOffset? LocationMeasuredAt,
    ContactSnapshot Contact,
    List<ReportHistoryDto> History,
    List<ReportAttachmentDto> Attachments,
    string? IpAddress = null,
    string? UserAgent = null
);

public sealed record ReportHistoryDto(
    Guid Id,
    Guid ActorId,
    string Action,
    string? PreviousValue,
    string? NewValue,
    string? Metadata,
    DateTimeOffset CreatedAt
);

public sealed record ReportAttachmentDto(
    Guid Id,
    string FileName,
    string ContentType,
    long SizeBytes,
    string StorageKey,
    DateTimeOffset CreatedAt
);
