using Krizovka.Users.Infrastructure.Persistence;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Krizovka.Users.Infrastructure.UserPreferences.Commands;

/// <summary>
/// Delete a user preference by AccountId + SystemId.
/// Returns true if the record was found and deleted.
/// </summary>
public record DeleteUserPreferenceCommand(Guid AccountId, Guid SystemId) : IRequest<bool>;

public class DeleteUserPreferenceCommandHandler : IRequestHandler<DeleteUserPreferenceCommand, bool>
{
    private readonly UsDbContext _context;

    public DeleteUserPreferenceCommandHandler(UsDbContext context)
    {
        _context = context;
    }

    public async Task<bool> Handle(DeleteUserPreferenceCommand request, CancellationToken cancellationToken)
    {
        var existing = await _context.UserPreferences
            .FirstOrDefaultAsync(
                x => x.AccountId == request.AccountId && x.SystemId == request.SystemId,
                cancellationToken);

        if (existing is null) return false;

        _context.UserPreferences.Remove(existing);
        await _context.SaveChangesAsync(cancellationToken);
        return true;
    }
}
