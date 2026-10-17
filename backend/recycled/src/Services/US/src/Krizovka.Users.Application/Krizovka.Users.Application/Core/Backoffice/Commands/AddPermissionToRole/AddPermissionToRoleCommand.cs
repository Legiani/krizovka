using MediatR;

namespace Krizovka.Users.Application.Core.Backoffice.Commands.AddPermissionToRole;

public record AddPermissionToRoleCommand(
    Guid RoleId,
    string Module,
    string Action,
    string ScopeType,
    string ScopeValue
) : IRequest<AddPermissionToRoleResponse>;

public record AddPermissionToRoleResponse(
    Guid PermissionId,
    bool Success,
    string? Error = null
);
