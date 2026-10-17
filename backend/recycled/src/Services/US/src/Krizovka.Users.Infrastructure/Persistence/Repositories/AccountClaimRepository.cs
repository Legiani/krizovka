using Krizovka.Users.Application.Core.Interfaces.Persistence;
using Krizovka.Users.Domain.Entities;
using Krizovka.Users.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Krizovka.Users.Infrastructure.Persistence.Repositories;

public class AccountClaimRepository : IAccountClaimRepository
{
    private readonly UsDbContext _dbContext;

    public AccountClaimRepository(UsDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<AccountClaim?> GetByIdAsync(Guid claimId, CancellationToken cancellationToken = default)
    {
        return await _dbContext.AccountClaims
            .Include(x => x.Account)
            .FirstOrDefaultAsync(x => x.ClaimId == claimId, cancellationToken);
    }

    public async Task<AccountClaim?> GetByTokenAsync(string claimToken, CancellationToken cancellationToken = default)
    {
        return await _dbContext.AccountClaims
            .Include(x => x.Account)
            .FirstOrDefaultAsync(x => x.ClaimToken == claimToken, cancellationToken);
    }

    public async Task<List<AccountClaim>> GetByAccountIdAsync(Guid accountId, CancellationToken cancellationToken = default)
    {
        return await _dbContext.AccountClaims
            .Where(x => x.AccountId == accountId)
            .ToListAsync(cancellationToken);
    }

    public async Task AddAsync(AccountClaim claim, CancellationToken cancellationToken = default)
    {
        await _dbContext.AccountClaims.AddAsync(claim, cancellationToken);
        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task UpdateAsync(AccountClaim claim, CancellationToken cancellationToken = default)
    {
        _dbContext.AccountClaims.Update(claim);
        await _dbContext.SaveChangesAsync(cancellationToken);
    }
}
