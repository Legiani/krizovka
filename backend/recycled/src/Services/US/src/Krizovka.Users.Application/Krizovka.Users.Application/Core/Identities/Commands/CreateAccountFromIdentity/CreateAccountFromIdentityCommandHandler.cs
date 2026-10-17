using Krizovka.Users.Application.Core.Interfaces.Persistence;
using Krizovka.Users.Domain.Entities;
using Krizovka.Users.Domain.Enums;
using Krizovka.Users.Domain.Events;
using Krizovka.Shared.Events.Core.US;
using MassTransit;
using MediatR;

namespace Krizovka.Users.Application.Core.Identities.Commands.CreateAccountFromIdentity;

public class CreateAccountFromIdentityCommandHandler : IRequestHandler<CreateAccountFromIdentityCommand>
{
    private readonly IUserAccountRepository _repository;
    private readonly IPublishEndpoint _publishEndpoint;

    public CreateAccountFromIdentityCommandHandler(IUserAccountRepository repository, IPublishEndpoint publishEndpoint)
    {
        _repository = repository;
        _publishEndpoint = publishEndpoint;
    }

    public async Task Handle(CreateAccountFromIdentityCommand request, CancellationToken cancellationToken)
    {
        // 1. Check if account already exists for this identity
        var existingAccount = await _repository.GetByIdentityAsync(request.IdentityId, cancellationToken);
        if (existingAccount != null)
        {
            // Idempotency: account exists, update system ID if missing
            if (string.IsNullOrEmpty(existingAccount.RegistrationSystemId) && !string.IsNullOrEmpty(request.RegistrationSystemId))
            {
                existingAccount.RegistrationSystemId = request.RegistrationSystemId;
                await _repository.UpdateAsync(existingAccount, cancellationToken);

                // Optional: Publish updated event if critical
            }
            return;
        }

        // 2. Create new account
        var account = new UserAccount
        {
            AccountId = Guid.NewGuid(),
            IdentityId = request.IdentityId,
            AccountType = AccountType.MAIN, // Default for self-service registration
            Status = AccountStatus.ACTIVE,
            CreatedAt = DateTimeOffset.UtcNow,
            UpdatedAt = DateTimeOffset.UtcNow,
            RegistrationSystemId = request.RegistrationSystemId,
            Email = request.Email,
            FirstName = request.FirstName,
            LastName = request.LastName
        };

        await _repository.AddAsync(account, cancellationToken);

        // 3. Publish Created Event
        await _publishEndpoint.Publish(new UserAccountCreated(
            account.AccountId,
            account.AccountType,
            Guid.Empty // System created
        ), cancellationToken);

        // Publish Integration Event
        await _publishEndpoint.Publish(new AccountCreatedEvent(
            Guid.NewGuid(),
            DateTimeOffset.UtcNow,
            account.AccountId,
            account.AccountType.ToString(),
            account.RegistrationSystemId
        ), cancellationToken);
    }
}
