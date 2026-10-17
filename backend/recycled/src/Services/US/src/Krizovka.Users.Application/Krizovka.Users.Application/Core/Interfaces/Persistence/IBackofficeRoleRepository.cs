using Krizovka.Users.Domain.Entities;

namespace Krizovka.Users.Application.Core.Interfaces.Persistence;

public interface IBackofficeRoleRepository
{
    Task<List<BackofficeRole>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<BackofficeRole?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<BackofficeRole> CreateAsync(BackofficeRole role, CancellationToken cancellationToken = default);
    Task<BackofficeRole> UpdateAsync(BackofficeRole role, CancellationToken cancellationToken = default);
    Task DeleteAsync(Guid id, CancellationToken cancellationToken = default);
}
