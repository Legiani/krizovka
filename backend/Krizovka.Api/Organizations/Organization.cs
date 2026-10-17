namespace Krizovka.Api.Organizations;

public sealed class Organization
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Code { get; set; } = string.Empty;
    public string Status { get; set; } = "Active"; // Active, Planned, Inactive
    public string? ContactEmail { get; set; }
    public string? ContactPhone { get; set; }
    public bool IsDefaultDispatch { get; set; }
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset? UpdatedAt { get; set; }

    public List<OrganizationArea> Areas { get; set; } = [];
}
