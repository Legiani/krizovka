using MediatR;

namespace Krizovka.Users.Application.Core.Backoffice.Queries.GetRoles;

public record GetRolesQuery() : IRequest<GetRolesResponse>;

public record GetRolesResponse(
    List<RoleDto> Roles
);

public record RoleDto(
    Guid Id,
    string Name,
    string Code,
    string? Description,
    List<PermissionDto> Permissions
);

public record PermissionDto(
    Guid Id,
    string Module,
    string Action,
    string ScopeType,
    string ScopeValue
);
