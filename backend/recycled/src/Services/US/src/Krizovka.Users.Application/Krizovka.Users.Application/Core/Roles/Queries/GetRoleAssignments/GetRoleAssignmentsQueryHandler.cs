using Krizovka.Users.Application.Core.Interfaces.Persistence;
using Krizovka.Users.Domain.Entities;
using MediatR;

namespace Krizovka.Users.Application.Core.Roles.Queries.GetRoleAssignments;

public class GetRoleAssignmentsQueryHandler : IRequestHandler<GetRoleAssignmentsQuery, List<RoleAssignment>>
{
    private readonly IRoleAssignmentRepository _roleAssignmentRepository;

    public GetRoleAssignmentsQueryHandler(IRoleAssignmentRepository roleAssignmentRepository)
    {
        _roleAssignmentRepository = roleAssignmentRepository;
    }

    public async Task<List<RoleAssignment>> Handle(GetRoleAssignmentsQuery request, CancellationToken cancellationToken)
    {
        return await _roleAssignmentRepository.GetByAccountIdAsync(request.AccountId, cancellationToken);
    }
}
