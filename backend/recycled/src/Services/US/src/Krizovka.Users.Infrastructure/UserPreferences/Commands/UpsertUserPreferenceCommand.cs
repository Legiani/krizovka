using System.Text.Json;
using Krizovka.Users.Domain.Entities;
using Krizovka.Users.Infrastructure.Persistence;
using Krizovka.Users.Infrastructure.UserPreferences.DTOs;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Krizovka.Users.Infrastructure.UserPreferences.Commands;

/// <summary>
/// Upsert a user preference for a specific city.
/// If a preference for (AccountId, SystemId) already exists, update the provided fields.
/// Otherwise create a new record.
/// </summary>
public record UpsertUserPreferenceCommand(
    Guid AccountId,
    Guid SystemId,
    JsonDocument? Favorites,
    JsonDocument? MenuLayout,
    JsonDocument? NotificationSettings
) : IRequest<UserPreferenceDto>;

public class UpsertUserPreferenceCommandHandler : IRequestHandler<UpsertUserPreferenceCommand, UserPreferenceDto>
{
    private readonly UsDbContext _context;

    public UpsertUserPreferenceCommandHandler(UsDbContext context)
    {
        _context = context;
    }

    public async Task<UserPreferenceDto> Handle(UpsertUserPreferenceCommand request, CancellationToken cancellationToken)
    {
        var existing = await _context.UserPreferences
            .FirstOrDefaultAsync(
                x => x.AccountId == request.AccountId && x.SystemId == request.SystemId,
                cancellationToken);

        if (existing is not null)
        {
            // Partial patch — only update fields that were provided
            if (request.Favorites is not null) existing.Favorites = request.Favorites;
            if (request.MenuLayout is not null) existing.MenuLayout = request.MenuLayout;
            if (request.NotificationSettings is not null) existing.NotificationSettings = request.NotificationSettings;
            existing.UpdatedAt = DateTimeOffset.UtcNow;
        }
        else
        {
            existing = new UserPreference
            {
                Id = Guid.NewGuid(),
                AccountId = request.AccountId,
                SystemId = request.SystemId,
                Favorites = request.Favorites,
                MenuLayout = request.MenuLayout,
                NotificationSettings = request.NotificationSettings,
                CreatedAt = DateTimeOffset.UtcNow
            };
            _context.UserPreferences.Add(existing);
        }

        await _context.SaveChangesAsync(cancellationToken);

        return new UserPreferenceDto
        {
            Id = existing.Id,
            AccountId = existing.AccountId,
            SystemId = existing.SystemId,
            Favorites = existing.Favorites,
            MenuLayout = existing.MenuLayout,
            NotificationSettings = existing.NotificationSettings,
            CreatedAt = existing.CreatedAt,
            UpdatedAt = existing.UpdatedAt
        };
    }
}
