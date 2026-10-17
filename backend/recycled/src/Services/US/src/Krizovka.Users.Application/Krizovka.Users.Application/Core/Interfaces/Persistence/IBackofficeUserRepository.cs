using Krizovka.Users.Domain.Entities;

namespace Krizovka.Users.Application.Core.Interfaces.Persistence;

public interface IBackofficeUserRepository
{
    Task<BackofficeUser?> GetByIdentityIdAsync(Guid identityId, CancellationToken cancellationToken = default);
    Task<BackofficeUser?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<BackofficeUser?> GetByEmailAsync(string email, CancellationToken cancellationToken = default);
    Task<List<BackofficeUser>> GetAllAsync(CancellationToken cancellationToken = default);
    Task AddAsync(BackofficeUser user, CancellationToken cancellationToken = default);
    Task UpdateAsync(BackofficeUser user, CancellationToken cancellationToken = default);
    Task DeleteAsync(Guid id, CancellationToken cancellationToken = default);
    Task<bool> ExistsByEmailAsync(string email, CancellationToken cancellationToken = default);

}
