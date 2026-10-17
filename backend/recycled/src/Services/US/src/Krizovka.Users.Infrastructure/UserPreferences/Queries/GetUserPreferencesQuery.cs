using Krizovka.Users.Infrastructure.Persistence;
using Krizovka.Users.Infrastructure.UserPreferences.DTOs;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Krizovka.Users.Infrastructure.UserPreferences.Queries;

/// <summary>
/// Get all preferences for a given account.
/// </summary>
public record GetUserPreferencesQuery(Guid AccountId) : IRequest<List<UserPreferenceDto>>;

public class GetUserPreferencesQueryHandler : IRequestHandler<GetUserPreferencesQuery, List<UserPreferenceDto>>
{
    private readonly UsDbContext _context;

    public GetUserPreferencesQueryHandler(UsDbContext context)
    {
        _context = context;
    }

    public async Task<List<UserPreferenceDto>> Handle(GetUserPreferencesQuery request, CancellationToken cancellationToken)
    {
        return await _context.UserPreferences
            .Where(p => p.AccountId == request.AccountId)
            .Select(p => new UserPreferenceDto
            {
                Id = p.Id,
                AccountId = p.AccountId,
                SystemId = p.SystemId,
                Favorites = p.Favorites,
                MenuLayout = p.MenuLayout,
                NotificationSettings = p.NotificationSettings,
                CreatedAt = p.CreatedAt,
                UpdatedAt = p.UpdatedAt
            })
            .ToListAsync(cancellationToken);
    }
}

/// <summary>
/// Get a single preference for account + system (city).
/// </summary>
public record GetUserPreferenceBySystemQuery(Guid AccountId, Guid SystemId) : IRequest<UserPreferenceDto?>;

public class GetUserPreferenceBySystemQueryHandler : IRequestHandler<GetUserPreferenceBySystemQuery, UserPreferenceDto?>
{
    private readonly UsDbContext _context;

    public GetUserPreferenceBySystemQueryHandler(UsDbContext context)
    {
        _context = context;
    }

    public async Task<UserPreferenceDto?> Handle(GetUserPreferenceBySystemQuery request, CancellationToken cancellationToken)
    {
        var p = await _context.UserPreferences
            .FirstOrDefaultAsync(
                x => x.AccountId == request.AccountId && x.SystemId == request.SystemId,
                cancellationToken);

        if (p is null) return null;

        return new UserPreferenceDto
        {
            Id = p.Id,
            AccountId = p.AccountId,
            SystemId = p.SystemId,
            Favorites = p.Favorites,
            MenuLayout = p.MenuLayout,
            NotificationSettings = p.NotificationSettings,
            CreatedAt = p.CreatedAt,
            UpdatedAt = p.UpdatedAt
        };
    }
}
