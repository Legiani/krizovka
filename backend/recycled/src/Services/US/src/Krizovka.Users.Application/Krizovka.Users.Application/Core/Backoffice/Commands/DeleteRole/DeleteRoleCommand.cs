namespace Krizovka.Users.Application.Core.Backoffice.Commands.DeleteRole;

public record DeleteRoleCommand(Guid RoleId) : MediatR.IRequest<DeleteRoleResponse>;

public record DeleteRoleResponse(bool Success, string? Error = null, bool IsSystemRole = false);
