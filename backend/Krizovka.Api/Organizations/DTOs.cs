namespace Krizovka.Api.Organizations;

public sealed record OrganizationDto(
    Guid Id,
    string Name,
    string Code,
    string Status,
    string? ContactEmail,
    string? ContactPhone,
    bool IsDefaultDispatch,
    DateTimeOffset CreatedAt,
    DateTimeOffset? UpdatedAt,
    List<OrganizationAreaDto> Areas
);

public sealed record OrganizationAreaDto(
    Guid Id,
    Guid OrganizationId,
    string Name,
    string PolygonGeoJson,
    double OffsetMeters,
    string AllocatedVelinId,
    DateTimeOffset CreatedAt,
    DateTimeOffset? UpdatedAt
);

public sealed record CreateOrganizationRequest(
    string Name,
    string Code,
    string? Status,
    string? ContactEmail,
    string? ContactPhone,
    bool IsDefaultDispatch
);

public sealed record UpdateOrganizationRequest(
    string Name,
    string Code,
    string Status,
    string? ContactEmail,
    string? ContactPhone,
    bool IsDefaultDispatch
);

public sealed record UpsertOrganizationAreaRequest(
    string Name,
    string PolygonGeoJson,
    double OffsetMeters,
    string AllocatedVelinId
);

public sealed record ResolveLocationResult(
    Guid OrganizationId,
    string OrganizationName,
    Guid? AreaId,
    string? AreaName,
    string AllocatedVelinId,
    bool IsFallback,
    bool IsWithinCore,
    bool IsWithinOffset,
    double? DistanceToAreaMeters
);
