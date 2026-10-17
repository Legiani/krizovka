using Krizovka.Users.Application.Core.Interfaces.Persistence;
using Krizovka.Users.Domain.Entities;
using Krizovka.Users.Domain.Enums;
using Krizovka.Users.Domain.Events;
using Krizovka.Shared.Events.Core;
using MassTransit;
using MediatR;
using Krizovka.Shared.Authentication;

namespace Krizovka.Users.Application.Core.Roles.Commands.AssignRole;

public class AssignRoleCommandHandler : IRequestHandler<AssignRoleCommand, Guid>
{
    private readonly IRoleAssignmentRepository _roleAssignmentRepository;
    private readonly IPublishEndpoint? _publishEndpoint;
    private readonly ICurrentUserService _currentUserService;

    public AssignRoleCommandHandler(
        IRoleAssignmentRepository roleAssignmentRepository,
        ICurrentUserService currentUserService,
        IPublishEndpoint? publishEndpoint = null)
    {
        _roleAssignmentRepository = roleAssignmentRepository;
        _currentUserService = currentUserService;
        _publishEndpoint = publishEndpoint;
    }

    public async Task<Guid> Handle(AssignRoleCommand request, CancellationToken cancellationToken)
    {
        var assignment = new RoleAssignment
        {
            AssignmentId = Guid.NewGuid(),
            AccountId = request.AccountId,
            RoleCode = request.RoleCode,
            ScopeType = request.ScopeType,
            ScopeId = request.ScopeId,
            Status = AccountStatus.ACTIVE,
            ValidFrom = DateTimeOffset.UtcNow,
            GrantedBy = _currentUserService.GetCurrentIdentityId() ?? Guid.Empty,
            CreatedAt = DateTimeOffset.UtcNow
        };

        await _roleAssignmentRepository.AddAsync(assignment, cancellationToken);

        // Publish event
        if (_publishEndpoint != null)
        {
            await _publishEndpoint.Publish(new UserRoleAssigned(
                assignment.AssignmentId,
                assignment.AccountId,
                assignment.RoleCode,
                assignment.ScopeType,
                assignment.ScopeId
            ), cancellationToken);
        }

        return assignment.AssignmentId;
    }
}
