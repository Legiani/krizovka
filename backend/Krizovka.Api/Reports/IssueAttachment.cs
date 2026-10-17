namespace Krizovka.Api.Reports;

public class IssueAttachment
{
    public Guid Id { get; set; }
    public Guid IssueId { get; set; }
    public string StorageKey { get; set; } = string.Empty;
    public string FileName { get; set; } = string.Empty;
    public string ContentType { get; set; } = string.Empty;
    public long SizeBytes { get; set; }
    public DateTimeOffset CreatedAt { get; set; }

    public Issue? Issue { get; set; }
}
