using MediatR;

namespace Krizovka.Users.Application.Core.Backoffice.Commands.CreateRole;

public record CreateRoleCommand(
    string Name,
    string Code,
    string? Description
) : IRequest<CreateRoleResponse>;

public record CreateRoleResponse(
    Guid RoleId,
    bool Success,
    string? Error = null
);
