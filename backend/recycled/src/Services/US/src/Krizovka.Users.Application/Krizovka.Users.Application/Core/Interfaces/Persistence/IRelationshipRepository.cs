using Krizovka.Users.Domain.Entities;

namespace Krizovka.Users.Application.Core.Interfaces.Persistence;

public interface IRelationshipRepository
{
    Task<Relationship?> GetByIdAsync(Guid relationshipId, CancellationToken cancellationToken = default);
    Task<List<Relationship>> GetByAccountIdAsync(Guid accountId, CancellationToken cancellationToken = default);
    Task<List<Relationship>> GetChildRelationshipsAsync(Guid parentAccountId, CancellationToken cancellationToken = default);
    Task<List<Relationship>> GetPendingRelationshipsAsync(Guid accountId, CancellationToken cancellationToken = default);
    Task<List<Relationship>> GetManageableRelationshipsAsync(Guid accountId, CancellationToken cancellationToken = default);
    Task AddAsync(Relationship relationship, CancellationToken cancellationToken = default);
    Task DeleteAsync(Relationship relationship, CancellationToken cancellationToken = default);
}
