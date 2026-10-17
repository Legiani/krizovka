using Krizovka.Users.Application.Core.Interfaces.Persistence;
using Krizovka.Users.Domain.Entities;
using Krizovka.Users.Domain.Enums;
using Krizovka.Users.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Krizovka.Users.Infrastructure.Persistence.Repositories;

public class RelationshipRepository : IRelationshipRepository
{
    private readonly UsDbContext _dbContext;

    public RelationshipRepository(UsDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<Relationship?> GetByIdAsync(Guid relationshipId, CancellationToken cancellationToken = default)
    {
        return await _dbContext.Relationships
            .Include(x => x.ParentAccount)
            .Include(x => x.ChildAccount)
            .FirstOrDefaultAsync(x => x.RelationshipId == relationshipId, cancellationToken);
    }

    public async Task<List<Relationship>> GetByAccountIdAsync(Guid accountId, CancellationToken cancellationToken = default)
    {
        return await _dbContext.Relationships
            .Where(x => x.ParentAccountId == accountId || x.ChildAccountId == accountId)
            .Include(x => x.ParentAccount)
            .Include(x => x.ChildAccount)
            .ToListAsync(cancellationToken);
    }

    public async Task<List<Relationship>> GetChildRelationshipsAsync(Guid parentAccountId, CancellationToken cancellationToken = default)
    {
        return await _dbContext.Relationships
            .Where(x => x.ParentAccountId == parentAccountId && x.Status == AccountStatus.ACTIVE)
            .Include(x => x.ChildAccount)
            .Include(x => x.ChildAccount)
            .ToListAsync(cancellationToken);
    }

    public async Task<List<Relationship>> GetPendingRelationshipsAsync(Guid accountId, CancellationToken cancellationToken = default)
    {
        return await _dbContext.Relationships
            .Where(x => (x.ParentAccountId == accountId || x.ChildAccountId == accountId) && x.Status == AccountStatus.PENDING)
            .Include(x => x.ParentAccount)
            .Include(x => x.ChildAccount)
            .ToListAsync(cancellationToken);
    }

    public async Task<List<Relationship>> GetManageableRelationshipsAsync(Guid accountId, CancellationToken cancellationToken = default)
    {
        var now = DateTimeOffset.UtcNow;
        return await _dbContext.Relationships
            .Where(x => x.ParentAccountId == accountId
                && x.Status == AccountStatus.ACTIVE
                && (x.RelationType == RelationType.MANAGES
                    || x.RelationType == RelationType.PARENT_OF
                    || x.RelationType == RelationType.MANAGES_ORGANIZATION_STAFF)
                && x.ValidFrom <= now
                && (x.ValidTo == null || x.ValidTo > now))
            .Include(x => x.ChildAccount)
            .ToListAsync(cancellationToken);
    }

    public async Task AddAsync(Relationship relationship, CancellationToken cancellationToken = default)
    {
        await _dbContext.Relationships.AddAsync(relationship, cancellationToken);
        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task DeleteAsync(Relationship relationship, CancellationToken cancellationToken = default)
    {
        _dbContext.Relationships.Remove(relationship);
        await _dbContext.SaveChangesAsync(cancellationToken);
    }
}
