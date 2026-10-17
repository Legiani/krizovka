using Krizovka.Users.Domain.Entities;
using Krizovka.Users.Domain.Enums;

namespace Krizovka.Users.Application.Core.Interfaces.Persistence;

public interface IUserAccountRepository
{
    Task<UserAccount?> GetByIdAsync(Guid accountId, CancellationToken cancellationToken = default);
    Task<UserAccount?> GetSummaryByIdAsync(Guid accountId, CancellationToken cancellationToken = default);
    Task<UserAccount?> GetByIdentityAsync(Guid identityId, CancellationToken cancellationToken = default);
    Task<List<UserAccount>> GetByIdentityIdsAsync(IReadOnlyCollection<Guid> identityIds, CancellationToken cancellationToken = default);
    Task<List<UserAccount>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<List<UserAccount>> GetByIdsAsync(IReadOnlyCollection<Guid> accountIds, CancellationToken cancellationToken = default);
    Task<(List<UserAccount> Items, int TotalCount)> SearchPagedAsync(
        string? query,
        AccountType? accountType,
        AccountStatus? status,
        int page,
        int pageSize,
        IReadOnlyCollection<string>? registrationSystems = null,
        CancellationToken cancellationToken = default);
    Task AddAsync(UserAccount account, CancellationToken cancellationToken = default);
    Task UpdateAsync(UserAccount account, CancellationToken cancellationToken = default);
    Task<bool> ExistsAsync(Guid accountId, CancellationToken cancellationToken = default);
    Task<UserAccount?> GetByIdentityIdAsync(Guid identityId, CancellationToken cancellationToken = default);
}
