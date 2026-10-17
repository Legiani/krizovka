using Krizovka.Users.Infrastructure.Persistence;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Krizovka.Users.Infrastructure.Roles.Commands;

/// <summary>
/// Revokes (deletes) a role assignment for a user account.
/// </summary>
public class RevokeRoleCommandHandler : IRequestHandler<Krizovka.Users.Application.Core.Roles.Commands.RevokeRole.RevokeRoleCommand>
{
    private readonly UsDbContext _db;
    private readonly ILogger<RevokeRoleCommandHandler> _logger;

    public RevokeRoleCommandHandler(UsDbContext db, ILogger<RevokeRoleCommandHandler> logger)
    {
        _db = db;
        _logger = logger;
    }

    public async Task Handle(Krizovka.Users.Application.Core.Roles.Commands.RevokeRole.RevokeRoleCommand request, CancellationToken ct)
    {
        var assignment = await _db.RoleAssignments
            .FirstOrDefaultAsync(r => r.AssignmentId == request.AssignmentId, ct)
            ?? throw new KeyNotFoundException($"RoleAssignment {request.AssignmentId} not found");

        _db.RoleAssignments.Remove(assignment);
        await _db.SaveChangesAsync(ct);

        _logger.LogInformation("RoleAssignment {Id} revoked from account {AccountId}",
            request.AssignmentId, assignment.AccountId);
    }
}
