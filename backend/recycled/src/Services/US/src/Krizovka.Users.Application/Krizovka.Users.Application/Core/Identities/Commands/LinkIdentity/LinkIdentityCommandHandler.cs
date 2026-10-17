using Krizovka.Users.Application.Core.Interfaces.Persistence;
using Krizovka.Users.Domain.Events;
using Krizovka.Shared.Events.Core;
using Krizovka.Shared.ErrorHandling.Exceptions;
using MassTransit;
using MediatR;

namespace Krizovka.Users.Application.Core.Identities.Commands.LinkIdentity;

public class LinkIdentityCommandHandler : IRequestHandler<LinkIdentityCommand>
{
    private readonly IUserAccountRepository _accountRepository;
    private readonly IPublishEndpoint _publishEndpoint;

    public LinkIdentityCommandHandler(
        IUserAccountRepository accountRepository,
        IPublishEndpoint publishEndpoint)
    {
        _accountRepository = accountRepository;
        _publishEndpoint = publishEndpoint;
    }

    public async Task Handle(LinkIdentityCommand request, CancellationToken cancellationToken)
    {
        var account = await _accountRepository.GetByIdAsync(request.AccountId, cancellationToken);
        if (account == null)
        {
            throw new NotFoundException($"Account {request.AccountId} not found");
        }

        if (account.IdentityId != null)
        {
            throw new ConflictException($"Account {request.AccountId} already has an identity linked");
        }

        account.IdentityId = request.IdentityId;
        account.UpdatedAt = DateTimeOffset.UtcNow;

        await _accountRepository.UpdateAsync(account, cancellationToken);

        // Publish event
        await _publishEndpoint.Publish(new UserIdentityLinked(
            account.AccountId,
            request.IdentityId
        ), cancellationToken);
    }
}
