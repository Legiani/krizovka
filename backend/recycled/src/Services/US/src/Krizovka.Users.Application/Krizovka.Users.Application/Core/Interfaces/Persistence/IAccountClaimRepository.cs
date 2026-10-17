using Krizovka.Users.Domain.Entities;

namespace Krizovka.Users.Application.Core.Interfaces.Persistence;

public interface IAccountClaimRepository
{
    Task<AccountClaim?> GetByIdAsync(Guid claimId, CancellationToken cancellationToken = default);
    Task<AccountClaim?> GetByTokenAsync(string claimToken, CancellationToken cancellationToken = default);
    Task<List<AccountClaim>> GetByAccountIdAsync(Guid accountId, CancellationToken cancellationToken = default);
    Task AddAsync(AccountClaim claim, CancellationToken cancellationToken = default);
    Task UpdateAsync(AccountClaim claim, CancellationToken cancellationToken = default);
}
