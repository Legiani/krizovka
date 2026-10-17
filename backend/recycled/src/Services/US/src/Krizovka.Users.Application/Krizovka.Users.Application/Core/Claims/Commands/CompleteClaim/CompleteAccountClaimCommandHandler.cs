using Krizovka.Users.Application.Core.Interfaces.Persistence;
using Krizovka.Users.Application.Core.Interfaces.Services;
using Krizovka.Users.Domain.Enums;
using Krizovka.Users.Domain.Events;
using Krizovka.Shared.Events.Core;
using Krizovka.Shared.ErrorHandling.Exceptions;
using MassTransit;
using MediatR;

namespace Krizovka.Users.Application.Core.Claims.Commands.CompleteClaim;

public class CompleteAccountClaimCommandHandler : IRequestHandler<CompleteAccountClaimCommand>
{
    private readonly IAccountClaimRepository _claimRepository;
    private readonly IUserAccountRepository _accountRepository;
    private readonly IRelationshipRepository _relationshipRepository;
    private readonly IClaimPolicyService _policyService;
    private readonly IPublishEndpoint _publishEndpoint;

    public CompleteAccountClaimCommandHandler(
        IAccountClaimRepository claimRepository,
        IUserAccountRepository accountRepository,
        IRelationshipRepository relationshipRepository,
        IClaimPolicyService policyService,
        IPublishEndpoint publishEndpoint)
    {
        _claimRepository = claimRepository;
        _accountRepository = accountRepository;
        _relationshipRepository = relationshipRepository;
        _policyService = policyService;
        _publishEndpoint = publishEndpoint;
    }

    public async Task Handle(CompleteAccountClaimCommand request, CancellationToken cancellationToken)
    {
        var claim = await _claimRepository.GetByTokenAsync(request.ClaimToken, cancellationToken);
        if (claim == null)
        {
            throw new NotFoundException($"Claim with token not found");
        }

        if (claim.Status != ClaimStatus.PENDING)
        {
            throw new ConflictException($"Claim is not in PENDING status");
        }

        if (claim.ExpiresAt < DateTimeOffset.UtcNow)
        {
            claim.Status = ClaimStatus.EXPIRED;
            await _claimRepository.UpdateAsync(claim, cancellationToken);
            throw new ConflictException($"Claim has expired");
        }

        var account = claim.Account;
        if (account.IdentityId != null)
        {
            throw new ConflictException($"Account already has an identity linked");
        }

        // Link identity
        account.IdentityId = request.IdentityId;

        // Apply policy: determine new account type
        var oldAccountType = account.AccountType;
        account.AccountType = _policyService.DetermineNewAccountType(oldAccountType);
        account.UpdatedAt = DateTimeOffset.UtcNow;

        await _accountRepository.UpdateAsync(account, cancellationToken);

        // Revoke MANAGES relationships if policy dictates
        if (_policyService.ShouldRevokeManagesRelationship(oldAccountType))
        {
            var relationships = await _relationshipRepository.GetByAccountIdAsync(account.AccountId, cancellationToken);
            var managesToRevoke = relationships
                .Where(r => r.ChildAccountId == account.AccountId && r.RelationType == RelationType.MANAGES)
                .ToList();

            foreach (var rel in managesToRevoke)
            {
                await _relationshipRepository.DeleteAsync(rel, cancellationToken);
                await _publishEndpoint.Publish(new UserRelationshipDeleted(rel.RelationshipId), cancellationToken);
            }
        }

        // Update claim status
        claim.Status = ClaimStatus.COMPLETED;
        claim.CompletedAt = DateTimeOffset.UtcNow;
        await _claimRepository.UpdateAsync(claim, cancellationToken);

        // Publish events
        await _publishEndpoint.Publish(new UserIdentityLinked(account.AccountId, request.IdentityId), cancellationToken);
        await _publishEndpoint.Publish(new UserAccountClaimCompleted(claim.ClaimId, account.AccountId, request.IdentityId), cancellationToken);

        if (oldAccountType != account.AccountType)
        {
            await _publishEndpoint.Publish(new UserAccountUpdated(account.AccountId, new Dictionary<string, object>
            {
                { "account_type", new { old = oldAccountType.ToString(), @new = account.AccountType.ToString() } }
            }), cancellationToken);
        }
    }
}
