using Krizovka.Users.Application.Core.Interfaces.Persistence;
using Krizovka.Users.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Krizovka.Users.Infrastructure.Persistence.Repositories;

public class BackofficeRoleRepository : IBackofficeRoleRepository
{
    private readonly UsDbContext _context;

    public BackofficeRoleRepository(UsDbContext context)
    {
        _context = context;
    }

    public async Task<List<BackofficeRole>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        return await _context.BackofficeRoles
            .Include(r => r.Permissions)
            .ToListAsync(cancellationToken);
    }

    public async Task<BackofficeRole?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _context.BackofficeRoles
            .Include(r => r.Permissions)
            .FirstOrDefaultAsync(r => r.Id == id, cancellationToken);
    }

    public async Task<BackofficeRole> CreateAsync(BackofficeRole role, CancellationToken cancellationToken = default)
    {
        _context.BackofficeRoles.Add(role);
        await _context.SaveChangesAsync(cancellationToken);
        return role;
    }

    public async Task<BackofficeRole> UpdateAsync(BackofficeRole role, CancellationToken cancellationToken = default)
    {
        _context.BackofficeRoles.Update(role);
        await _context.SaveChangesAsync(cancellationToken);
        return role;
    }

    public async Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var role = await GetByIdAsync(id, cancellationToken);
        if (role != null)
        {
            _context.BackofficeRoles.Remove(role);
            await _context.SaveChangesAsync(cancellationToken);
        }
    }
}
