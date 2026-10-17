namespace Krizovka.Api.Reports;

public sealed class Issue
{
    public Guid Id { get; private set; }
    public Guid OrganizationId { get; private set; }
    public Guid ReporterAccountId { get; private set; }
    public Guid ClientReportId { get; private set; }
    public string? Description { get; private set; }
    public ReportStatus Status { get; private set; } = ReportStatus.New;
    public DateTimeOffset CreatedAt { get; private set; }
    public DateTimeOffset? UpdatedAt { get; private set; }
    public double? Latitude { get; private set; }
    public double? Longitude { get; private set; }
    public double? AccuracyMeters { get; private set; }
    public DateTimeOffset? LocationMeasuredAt { get; private set; }
    public string? IpAddress { get; private set; }
    public string? UserAgent { get; private set; }
    public ContactSnapshot Contact { get; private set; } = new();

    private readonly List<IssueAttachment> _attachments = [];
    public IReadOnlyCollection<IssueAttachment> Attachments => _attachments.AsReadOnly();

    private readonly List<IssueHistory> _history = [];
    public IReadOnlyCollection<IssueHistory> History => _history.AsReadOnly();

    private Issue() { } // Required by EF Core

    public static Issue Create(
        Guid organizationId,
        Guid reporterAccountId,
        Guid clientReportId,
        string? description,
        double? latitude,
        double? longitude,
        double? accuracyMeters,
        DateTimeOffset? locationMeasuredAt,
        ContactSnapshot? contact,
        string? ipAddress = null,
        string? userAgent = null)
    {
        if (organizationId == Guid.Empty)
            throw new ArgumentException("OrganizationId is required.", nameof(organizationId));
        if (clientReportId == Guid.Empty)
            throw new ArgumentException("ClientReportId is required.", nameof(clientReportId));

        var now = DateTimeOffset.UtcNow;
        var issue = new Issue
        {
            Id = Guid.NewGuid(),
            OrganizationId = organizationId,
            ReporterAccountId = reporterAccountId,
            ClientReportId = clientReportId,
            Description = description?.Trim(),
            Status = ReportStatus.New,
            CreatedAt = now,
            Latitude = latitude,
            Longitude = longitude,
            AccuracyMeters = accuracyMeters,
            LocationMeasuredAt = locationMeasuredAt ?? (latitude.HasValue ? now : null),
            IpAddress = ipAddress,
            UserAgent = userAgent,
            Contact = contact ?? new ContactSnapshot()
        };

        issue.AddHistoryEntry(
            actorId: reporterAccountId,
            action: "Created",
            previousValue: null,
            newValue: ReportStatus.New.ToString(),
            metadata: !string.IsNullOrWhiteSpace(ipAddress) ? $"IP: {ipAddress}" : null,
            timestamp: now
        );

        return issue;
    }

    public void ChangeStatus(ReportStatus newStatus, Guid actorId, string? note)
    {
        var prev = Status;
        Status = newStatus;
        var now = DateTimeOffset.UtcNow;
        UpdatedAt = now;

        AddHistoryEntry(
            actorId: actorId,
            action: "StatusChange",
            previousValue: prev.ToString(),
            newValue: newStatus.ToString(),
            metadata: note,
            timestamp: now
        );
    }

    public void UpdateLocation(double latitude, double longitude, double? accuracyMeters, DateTimeOffset? measuredAt)
    {
        var now = DateTimeOffset.UtcNow;
        Latitude = latitude;
        Longitude = longitude;
        AccuracyMeters = accuracyMeters;
        LocationMeasuredAt = measuredAt ?? now;
        UpdatedAt = now;

        AddHistoryEntry(
            actorId: Guid.Empty,
            action: "LocationUpdated",
            previousValue: null,
            newValue: $"{latitude}, {longitude}",
            metadata: null,
            timestamp: now
        );
    }

    public void AddAttachment(IssueAttachment attachment, Guid actorId)
    {
        ArgumentNullException.ThrowIfNull(attachment);
        _attachments.Add(attachment);
        var now = DateTimeOffset.UtcNow;
        UpdatedAt = now;

        AddHistoryEntry(
            actorId: actorId,
            action: "AttachmentAdded",
            previousValue: null,
            newValue: attachment.FileName,
            metadata: attachment.ContentType,
            timestamp: now
        );
    }

    public void AddSupplementalMessage(string message, Guid actorId)
    {
        if (string.IsNullOrWhiteSpace(message))
            throw new ArgumentException("Message cannot be empty.", nameof(message));

        var now = DateTimeOffset.UtcNow;
        UpdatedAt = now;

        AddHistoryEntry(
            actorId: actorId,
            action: "MessageAdded",
            previousValue: null,
            newValue: message.Trim(),
            metadata: null,
            timestamp: now
        );
    }

    public void StartStream(string mode, Guid actorId)
    {
        var now = DateTimeOffset.UtcNow;
        UpdatedAt = now;

        AddHistoryEntry(
            actorId: actorId,
            action: "LiveStreamStarted",
            previousValue: null,
            newValue: mode,
            metadata: "UplinkOnly",
            timestamp: now
        );
    }

    public void EndStream(Guid actorId)
    {
        var now = DateTimeOffset.UtcNow;
        UpdatedAt = now;

        AddHistoryEntry(
            actorId: actorId,
            action: "LiveStreamEnded",
            previousValue: null,
            newValue: "Ukončeno uživatelem",
            metadata: null,
            timestamp: now
        );
    }

    private void AddHistoryEntry(Guid actorId, string action, string? previousValue, string? newValue, string? metadata, DateTimeOffset timestamp)
    {
        _history.Add(new IssueHistory
        {
            IssueId = Id,
            ActorId = actorId,
            Action = action,
            PreviousValue = previousValue,
            NewValue = newValue,
            Metadata = metadata,
            CreatedAt = timestamp
        });
    }
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
