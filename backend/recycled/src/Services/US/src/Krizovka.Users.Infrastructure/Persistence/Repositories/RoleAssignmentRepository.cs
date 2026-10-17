using Krizovka.Users.Application.Core.Interfaces.Persistence;
using Krizovka.Users.Domain.Entities;
using Krizovka.Users.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Krizovka.Users.Infrastructure.Persistence.Repositories;

public class RoleAssignmentRepository : IRoleAssignmentRepository
{
    private readonly UsDbContext _dbContext;

    public RoleAssignmentRepository(UsDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<RoleAssignment?> GetByIdAsync(Guid assignmentId, CancellationToken cancellationToken = default)
    {
        return await _dbContext.RoleAssignments
            .Include(x => x.Account)
            .FirstOrDefaultAsync(x => x.AssignmentId == assignmentId, cancellationToken);
    }

    public async Task<List<RoleAssignment>> GetByAccountIdAsync(Guid accountId, CancellationToken cancellationToken = default)
    {
        return await _dbContext.RoleAssignments
            .Where(x => x.AccountId == accountId)
            .ToListAsync(cancellationToken);
    }

    public async Task AddAsync(RoleAssignment assignment, CancellationToken cancellationToken = default)
    {
        await _dbContext.RoleAssignments.AddAsync(assignment, cancellationToken);
        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task DeleteAsync(RoleAssignment assignment, CancellationToken cancellationToken = default)
    {
        _dbContext.RoleAssignments.Remove(assignment);
        await _dbContext.SaveChangesAsync(cancellationToken);
    }
}
