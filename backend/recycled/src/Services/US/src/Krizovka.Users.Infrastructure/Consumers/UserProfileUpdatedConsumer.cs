using Krizovka.Shared.Events.Core;
using Krizovka.Users.Application.Core.Interfaces.Persistence;
using MassTransit;
using Microsoft.Extensions.Logging;

namespace Krizovka.Users.Infrastructure.Consumers;

/// <summary>
/// Reacts to UserProfileUpdatedEvent published generally by CPS self-service profile page.
/// Updates the source-of-truth fields living in US.UserAccount (Email, FirstName, LastName).
/// </summary>
public class UserProfileUpdatedConsumer : IConsumer<UserProfileUpdatedEvent>
{
    private readonly IUserAccountRepository _repo;
    private readonly ILogger<UserProfileUpdatedConsumer> _logger;

    public UserProfileUpdatedConsumer(
        IUserAccountRepository repo,
        ILogger<UserProfileUpdatedConsumer> logger)
    {
        _repo = repo;
        _logger = logger;
    }

    public async Task Consume(ConsumeContext<UserProfileUpdatedEvent> context)
    {
        var msg = context.Message;
        _logger.LogInformation("US: Updating profile data for account {AccountId}", msg.AccountId);

        var account = await _repo.GetByIdAsync(msg.AccountId, context.CancellationToken);
        if (account == null)
        {
            _logger.LogWarning("US: Cannot update profile data, account {AccountId} not found.", msg.AccountId);
            return;
        }

        account.Email = msg.Email ?? account.Email;
        account.FirstName = msg.FirstName ?? account.FirstName;
        account.LastName = msg.LastName ?? account.LastName;
        account.UpdatedAt = msg.UpdatedAt;

        // PhoneNumber se do logiky UserAccount standardně vrací do custom tables nebo je vynecháno

        await _repo.UpdateAsync(account, context.CancellationToken);
    }
}
