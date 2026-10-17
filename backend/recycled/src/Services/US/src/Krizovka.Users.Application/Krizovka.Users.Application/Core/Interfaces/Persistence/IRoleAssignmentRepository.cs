using Krizovka.Users.Domain.Entities;

namespace Krizovka.Users.Application.Core.Interfaces.Persistence;

public interface IRoleAssignmentRepository
{
    Task<RoleAssignment?> GetByIdAsync(Guid assignmentId, CancellationToken cancellationToken = default);
    Task<List<RoleAssignment>> GetByAccountIdAsync(Guid accountId, CancellationToken cancellationToken = default);
    Task AddAsync(RoleAssignment assignment, CancellationToken cancellationToken = default);
    Task DeleteAsync(RoleAssignment assignment, CancellationToken cancellationToken = default);
}
