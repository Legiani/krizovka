using Krizovka.Users.Application.Core.Interfaces.Persistence;
using Krizovka.Users.Domain.Entities;
using MediatR;

namespace Krizovka.Users.Application.Core.Backoffice.Commands.AddPermissionToRole;

public class AddPermissionToRoleCommandHandler : IRequestHandler<AddPermissionToRoleCommand, AddPermissionToRoleResponse>
{
    private readonly IBackofficeRoleRepository _roleRepository;

    public AddPermissionToRoleCommandHandler(IBackofficeRoleRepository roleRepository)
    {
        _roleRepository = roleRepository;
    }

    public async Task<AddPermissionToRoleResponse> Handle(AddPermissionToRoleCommand request, CancellationToken cancellationToken)
    {
        var role = await _roleRepository.GetByIdAsync(request.RoleId, cancellationToken);

        if (role == null)
        {
            return new AddPermissionToRoleResponse(Guid.Empty, false, "Role not found");
        }

        var permission = new BackofficePermission
        {
            Id = Guid.NewGuid(),
            RoleId = request.RoleId,
            Module = request.Module,
            Action = request.Action,
            ScopeType = request.ScopeType,
            ScopeValue = request.ScopeValue
        };

        role.Permissions.Add(permission);
        await _roleRepository.UpdateAsync(role, cancellationToken);

        return new AddPermissionToRoleResponse(permission.Id, true);
    }
}
