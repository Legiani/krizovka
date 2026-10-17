using Krizovka.Users.Application.Core.Interfaces.Persistence;
using Krizovka.Users.Domain.Enums;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Krizovka.Users.Application.Core.Roles.Commands.RevokeRole;

public record RevokeRoleCommand(Guid AssignmentId) : IRequest;

public class RevokeRoleCommandHandler : IRequestHandler<RevokeRoleCommand>
{
    private readonly IRoleAssignmentRepository _repo;
    private readonly ILogger<RevokeRoleCommandHandler> _logger;

    public RevokeRoleCommandHandler(
        IRoleAssignmentRepository repo,
        ILogger<RevokeRoleCommandHandler> logger)
    {
        _repo   = repo;
        _logger = logger;
    }

    public async Task Handle(RevokeRoleCommand request, CancellationToken cancellationToken)
    {
        var assignment = await _repo.GetByIdAsync(request.AssignmentId, cancellationToken);

        if (assignment == null)
        {
            _logger.LogWarning("RevokeRole: assignment {Id} not found.", request.AssignmentId);
            return;
        }

        // Soft-revoke: set ValidTo to now (preserves audit trail)
        assignment.ValidTo = DateTimeOffset.UtcNow;
        assignment.Status  = AccountStatus.DELETED;
        await _repo.DeleteAsync(assignment, cancellationToken);

        _logger.LogInformation(
            "Role assignment {Id} (role={Role}, account={AccountId}) revoked.",
            assignment.AssignmentId, assignment.RoleCode, assignment.AccountId);
    }
}
