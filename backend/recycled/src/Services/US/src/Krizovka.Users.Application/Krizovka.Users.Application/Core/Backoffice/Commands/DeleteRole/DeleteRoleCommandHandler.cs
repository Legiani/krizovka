using Krizovka.Users.Application.Core.Interfaces.Persistence;
using Krizovka.Users.Domain.Constants;
using MediatR;

namespace Krizovka.Users.Application.Core.Backoffice.Commands.DeleteRole;

public class DeleteRoleCommandHandler : IRequestHandler<DeleteRoleCommand, DeleteRoleResponse>
{
    private static readonly HashSet<string> SystemRoles =
    [
        RoleCodes.BO_SUPER_ADMIN,
        RoleCodes.BO_ADMIN,
        RoleCodes.BO_USER
    ];

    private readonly IBackofficeRoleRepository _roleRepository;

    public DeleteRoleCommandHandler(IBackofficeRoleRepository roleRepository)
    {
        _roleRepository = roleRepository;
    }

    public async Task<DeleteRoleResponse> Handle(DeleteRoleCommand request, CancellationToken cancellationToken)
    {
        var role = await _roleRepository.GetByIdAsync(request.RoleId, cancellationToken);
        if (role == null)
            return new DeleteRoleResponse(false, "Role not found");

        if (SystemRoles.Contains(role.Code))
            return new DeleteRoleResponse(false, $"System role '{role.Code}' cannot be deleted.", IsSystemRole: true);

        await _roleRepository.DeleteAsync(request.RoleId, cancellationToken);
        return new DeleteRoleResponse(true);
    }
}
