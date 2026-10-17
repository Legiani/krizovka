using Krizovka.Users.Application.Core.Interfaces.Persistence;
using Krizovka.Users.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Krizovka.Users.Infrastructure.Persistence.Repositories;

public class BackofficeUserRepository : IBackofficeUserRepository
{
    private readonly UsDbContext _context;

    public BackofficeUserRepository(UsDbContext context)
    {
        _context = context;
    }

    public async Task<BackofficeUser?> GetByIdentityIdAsync(Guid identityId, CancellationToken cancellationToken = default)
    {
        return await _context.BackofficeUsers
            .Include(x => x.Role)
                .ThenInclude(r => r!.Permissions)
            .FirstOrDefaultAsync(x => x.IdentityId == identityId, cancellationToken);
    }

    public async Task<BackofficeUser?> GetByEmailAsync(string email, CancellationToken cancellationToken = default)
    {
        var normalizedEmail = email.ToLower();
        return await _context.BackofficeUsers
            .Include(x => x.Role)
                .ThenInclude(r => r!.Permissions)
            .FirstOrDefaultAsync(x => x.Email.ToLower() == normalizedEmail, cancellationToken);
    }

    public async Task<List<BackofficeUser>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        return await _context.BackofficeUsers
            .Include(x => x.Role)
                .ThenInclude(r => r!.Permissions)
            .ToListAsync(cancellationToken);
    }

    public async Task<BackofficeUser?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _context.BackofficeUsers
            .Include(x => x.Role)
                .ThenInclude(r => r!.Permissions)
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
    }

    public async Task AddAsync(BackofficeUser user, CancellationToken cancellationToken = default)
    {
        await _context.BackofficeUsers.AddAsync(user, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task UpdateAsync(BackofficeUser user, CancellationToken cancellationToken = default)
    {
        _context.BackofficeUsers.Update(user);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task<bool> ExistsByEmailAsync(string email, CancellationToken cancellationToken = default)
    {
        var normalizedEmail = email.ToLower();
        return await _context.BackofficeUsers
            .AnyAsync(x => x.Email.ToLower() == normalizedEmail, cancellationToken);
    }

    public async Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var user = await _context.BackofficeUsers.FindAsync([id], cancellationToken);
        if (user != null)
        {
            _context.BackofficeUsers.Remove(user);
            await _context.SaveChangesAsync(cancellationToken);
        }
    }
}
