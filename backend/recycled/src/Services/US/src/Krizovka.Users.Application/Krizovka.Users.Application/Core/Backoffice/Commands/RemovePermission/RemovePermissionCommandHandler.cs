using Krizovka.Users.Application.Core.Interfaces.Persistence;
using MediatR;

namespace Krizovka.Users.Application.Core.Backoffice.Commands.RemovePermission;

public class RemovePermissionCommandHandler : IRequestHandler<RemovePermissionCommand, RemovePermissionResponse>
{
    private readonly IBackofficeRoleRepository _roleRepository;

    public RemovePermissionCommandHandler(IBackofficeRoleRepository roleRepository)
    {
        _roleRepository = roleRepository;
    }

    public async Task<RemovePermissionResponse> Handle(RemovePermissionCommand request, CancellationToken cancellationToken)
    {
        var roles = await _roleRepository.GetAllAsync(cancellationToken);
        var role = roles.FirstOrDefault(r => r.Permissions.Any(p => p.Id == request.PermissionId));

        if (role == null)
        {
            return new RemovePermissionResponse(false, "Permission not found");
        }

        var permission = role.Permissions.First(p => p.Id == request.PermissionId);
        role.Permissions.Remove(permission);

        await _roleRepository.UpdateAsync(role, cancellationToken);

        return new RemovePermissionResponse(true);
    }
}
