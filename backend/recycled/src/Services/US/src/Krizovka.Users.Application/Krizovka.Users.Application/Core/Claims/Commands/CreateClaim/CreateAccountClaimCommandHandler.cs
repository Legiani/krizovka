using Krizovka.Users.Application.Core.Interfaces.Persistence;
using Krizovka.Users.Domain.Entities;
using Krizovka.Users.Domain.Enums;
using Krizovka.Users.Domain.Events;
using Krizovka.Shared.Events.Core;
using Krizovka.Shared.ErrorHandling.Exceptions;
using MassTransit;
using MediatR;

namespace Krizovka.Users.Application.Core.Claims.Commands.CreateClaim;

public class CreateAccountClaimCommandHandler : IRequestHandler<CreateAccountClaimCommand, Guid>
{
    private readonly IAccountClaimRepository _claimRepository;
    private readonly IUserAccountRepository _accountRepository;
    private readonly IPublishEndpoint _publishEndpoint;

    public CreateAccountClaimCommandHandler(
        IAccountClaimRepository claimRepository,
        IUserAccountRepository accountRepository,
        IPublishEndpoint publishEndpoint)
    {
        _claimRepository = claimRepository;
        _accountRepository = accountRepository;
        _publishEndpoint = publishEndpoint;
    }

    public async Task<Guid> Handle(CreateAccountClaimCommand request, CancellationToken cancellationToken)
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

        var claim = new AccountClaim
        {
            ClaimId = Guid.NewGuid(),
            AccountId = request.AccountId,
            Email = request.Email,
            ClaimToken = Guid.NewGuid().ToString("N"), // Simple UUID token
            ExpiresAt = DateTimeOffset.UtcNow.AddHours(request.ExpireHours),
            Status = ClaimStatus.PENDING,
            CreatedAt = DateTimeOffset.UtcNow
        };

        await _claimRepository.AddAsync(claim, cancellationToken);

        // Publish event - NFS will handle email sending
        await _publishEndpoint.Publish(new UserAccountClaimRequested(
            claim.ClaimId,
            claim.AccountId,
            claim.Email,
            claim.ExpiresAt
        ), cancellationToken);

        return claim.ClaimId;
    }
}
