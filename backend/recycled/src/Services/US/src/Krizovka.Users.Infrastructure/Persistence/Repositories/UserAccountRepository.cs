using Krizovka.Users.Application.Core.Interfaces.Persistence;
using Krizovka.Users.Domain.Entities;
using Krizovka.Users.Domain.Enums;
using Krizovka.Users.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Krizovka.Users.Infrastructure.Persistence.Repositories;

public class UserAccountRepository : IUserAccountRepository
{
    private readonly UsDbContext _dbContext;

    public UserAccountRepository(UsDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<UserAccount?> GetByIdAsync(Guid accountId, CancellationToken cancellationToken = default)
    {
        return await _dbContext.UserAccounts
            .Include(x => x.RoleAssignments)
            .FirstOrDefaultAsync(x => x.AccountId == accountId, cancellationToken);
    }

    public async Task<UserAccount?> GetSummaryByIdAsync(Guid accountId, CancellationToken cancellationToken = default)
    {
        return await _dbContext.UserAccounts
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.AccountId == accountId, cancellationToken);
    }

    public async Task<UserAccount?> GetByIdentityAsync(Guid identityId, CancellationToken cancellationToken = default)
    {
        return await _dbContext.UserAccounts
            .Include(x => x.RoleAssignments)
            .FirstOrDefaultAsync(x => x.IdentityId == identityId, cancellationToken);
    }

    public async Task<List<UserAccount>> GetByIdentityIdsAsync(IReadOnlyCollection<Guid> identityIds, CancellationToken cancellationToken = default)
    {
        if (identityIds.Count == 0)
        {
            return [];
        }

        return await _dbContext.UserAccounts
            .AsNoTracking()
            .Where(x => x.IdentityId.HasValue && identityIds.Contains(x.IdentityId.Value))
            .ToListAsync(cancellationToken);
    }

    public async Task<List<UserAccount>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        return await _dbContext.UserAccounts
            .AsNoTracking()
            .ToListAsync(cancellationToken);
    }

    public async Task<List<UserAccount>> GetByIdsAsync(IReadOnlyCollection<Guid> accountIds, CancellationToken cancellationToken = default)
    {
        if (accountIds.Count == 0)
        {
            return [];
        }

        return await _dbContext.UserAccounts
            .AsNoTracking()
            .Where(x => accountIds.Contains(x.AccountId))
            .ToListAsync(cancellationToken);
    }

    public async Task<(List<UserAccount> Items, int TotalCount)> SearchPagedAsync(
        string? query,
        AccountType? accountType,
        AccountStatus? status,
        int page,
        int pageSize,
        IReadOnlyCollection<string>? registrationSystems = null,
        CancellationToken cancellationToken = default)
    {
        var queryable = _dbContext.UserAccounts
            .AsNoTracking()
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(query))
        {
            // JOIN/EXISTS optimization in DB instead of memory filtering
            queryable = queryable.Where(a =>
                (a.Email != null && a.Email.Contains(query)) ||
                (a.FirstName != null && a.FirstName.Contains(query)) ||
                (a.LastName != null && a.LastName.Contains(query)) ||
                _dbContext.AccountClaims.Any(c => c.AccountId == a.AccountId && c.Email.Contains(query)) ||
                (a.IdentityId != null && a.IdentityId.Value.ToString().Contains(query)));
        }

        if (accountType.HasValue)
        {
            queryable = queryable.Where(a => a.AccountType == accountType.Value);
        }

        if (status.HasValue)
        {
            queryable = queryable.Where(a => a.Status == status.Value);
        }

        if (registrationSystems is { Count: > 0 })
        {
            queryable = queryable.Where(a =>
                a.RegistrationSystemId != null &&
                registrationSystems.Contains(a.RegistrationSystemId));
        }

        var totalCount = await queryable.CountAsync(cancellationToken);

        var items = await queryable
            .OrderByDescending(x => x.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return (items, totalCount);
    }

    public async Task AddAsync(UserAccount account, CancellationToken cancellationToken = default)
    {
        await _dbContext.UserAccounts.AddAsync(account, cancellationToken);
        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task UpdateAsync(UserAccount account, CancellationToken cancellationToken = default)
    {
        _dbContext.UserAccounts.Update(account);
        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task<bool> ExistsAsync(Guid accountId, CancellationToken cancellationToken = default)
    {
        return await _dbContext.UserAccounts.AnyAsync(x => x.AccountId == accountId, cancellationToken);
    }

    public async Task<UserAccount?> GetByIdentityIdAsync(Guid identityId, CancellationToken cancellationToken = default)
    {
        return await _dbContext.UserAccounts
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.IdentityId == identityId, cancellationToken);
    }
}
