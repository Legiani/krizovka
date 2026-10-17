namespace Krizovka.Api.Organizations;

public sealed class OrganizationArea
{
    public Guid Id { get; set; }
    public Guid OrganizationId { get; set; }
    public Organization? Organization { get; set; }
    public string Name { get; set; } = string.Empty;
    public string PolygonGeoJson { get; set; } = string.Empty;
    public double OffsetMeters { get; set; } = 50.0;
    public string AllocatedVelinId { get; set; } = "default-velin";
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset? UpdatedAt { get; set; }
}
