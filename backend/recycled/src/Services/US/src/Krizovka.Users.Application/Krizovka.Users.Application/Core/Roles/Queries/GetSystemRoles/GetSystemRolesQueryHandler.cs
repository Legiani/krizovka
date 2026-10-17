using Krizovka.Users.Application.Core.Interfaces.Persistence;
using Krizovka.Users.Domain.Entities;
using Krizovka.Users.Domain.Enums;
using MediatR;

namespace Krizovka.Users.Application.Core.Roles.Queries.GetSystemRoles;

public class GetSystemRolesQueryHandler : IRequestHandler<GetSystemRolesQuery, List<RoleAssignment>>
{
    private static readonly ScopeType[] SystemScopes =
    {
        ScopeType.IDS,
        ScopeType.PROVIDER,
        ScopeType.CARRIER
    };

    private readonly IRoleAssignmentRepository _roleAssignmentRepository;

    public GetSystemRolesQueryHandler(IRoleAssignmentRepository roleAssignmentRepository)
    {
        _roleAssignmentRepository = roleAssignmentRepository;
    }

    public async Task<List<RoleAssignment>> Handle(
        GetSystemRolesQuery request,
        CancellationToken cancellationToken)
    {
        var allRoles = await _roleAssignmentRepository.GetByAccountIdAsync(
            request.AccountId, cancellationToken);

        var systemRoles = allRoles
            .Where(r => SystemScopes.Contains(r.ScopeType))
            .Where(r => r.Status == Domain.Enums.AccountStatus.ACTIVE)
            .Where(r => r.ValidFrom <= DateTimeOffset.UtcNow && (r.ValidTo == null || r.ValidTo > DateTimeOffset.UtcNow));

        if (request.ScopeId is not null)
        {
            systemRoles = systemRoles.Where(r => r.ScopeId == request.ScopeId);
        }

        return systemRoles.ToList();
    }
}
