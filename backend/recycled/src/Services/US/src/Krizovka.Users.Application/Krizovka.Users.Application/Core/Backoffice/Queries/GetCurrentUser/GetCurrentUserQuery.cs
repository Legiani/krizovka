using MediatR;

namespace Krizovka.Users.Application.Core.Backoffice.Queries.GetCurrentUser;

public record GetCurrentUserQuery(
    Guid IdentityId
) : IRequest<GetCurrentUserResponse?>;

public record GetCurrentUserResponse(
    Guid UserId,
    Guid IdentityId,
    Guid? AccountId,
    string Email,
    string FirstName,
    string LastName,
    bool IsSuperAdmin,
    RoleInfo? Role,
    List<PermissionInfo> Permissions,
    List<CityAssignmentInfo> CityAssignments
);

public record RoleInfo(
    Guid RoleId,
    string Name,
    string Code,
    string? Description
);

public record PermissionInfo(
    string Module,
    string Action,
    string ScopeType,
    string? ScopeValue
);

/// <summary>City-role assignment returned to the frontend via current-user endpoint.</summary>
public record CityAssignmentInfo(string CityId, string RoleCode);
