using Krizovka.Users.Application.Core.Interfaces.Persistence;
using MediatR;

namespace Krizovka.Users.Application.Core.Backoffice.Queries.GetRoles;

public class GetRolesQueryHandler : IRequestHandler<GetRolesQuery, GetRolesResponse>
{
    private readonly IBackofficeRoleRepository _roleRepository;

    public GetRolesQueryHandler(IBackofficeRoleRepository roleRepository)
    {
        _roleRepository = roleRepository;
    }

    public async Task<GetRolesResponse> Handle(GetRolesQuery request, CancellationToken cancellationToken)
    {
        var roles = await _roleRepository.GetAllAsync(cancellationToken);

        var roleDtos = roles.Select(r => new RoleDto(
            r.Id,
            r.Name,
            r.Code,
            r.Description,
            r.Permissions.Select(p => new PermissionDto(
                p.Id,
                p.Module,
                p.Action,
                p.ScopeType,
                p.ScopeValue
            )).ToList()
        )).ToList();

        return new GetRolesResponse(roleDtos);
    }
}
