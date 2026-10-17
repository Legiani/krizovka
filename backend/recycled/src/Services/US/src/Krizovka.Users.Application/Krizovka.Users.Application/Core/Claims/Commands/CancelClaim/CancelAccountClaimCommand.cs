using Krizovka.Users.Application.Core.Interfaces.Persistence;
using Krizovka.Users.Domain.Enums;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Krizovka.Users.Application.Core.Claims.Commands.CancelClaim;

public record CancelAccountClaimCommand(Guid ClaimId) : IRequest;

public class CancelAccountClaimCommandHandler : IRequestHandler<CancelAccountClaimCommand>
{
    private readonly IAccountClaimRepository _claims;
    private readonly ILogger<CancelAccountClaimCommandHandler> _logger;

    public CancelAccountClaimCommandHandler(
        IAccountClaimRepository claims,
        ILogger<CancelAccountClaimCommandHandler> logger)
    {
        _claims = claims;
        _logger = logger;
    }

    public async Task Handle(CancelAccountClaimCommand request, CancellationToken cancellationToken)
    {
        var claim = await _claims.GetByIdAsync(request.ClaimId, cancellationToken);

        if (claim == null)
        {
            _logger.LogWarning("CancelClaim: claim {ClaimId} not found.", request.ClaimId);
            return;
        }

        if (claim.Status != ClaimStatus.PENDING)
        {
            _logger.LogInformation("CancelClaim: claim {ClaimId} is already in terminal state {Status}.",
                request.ClaimId, claim.Status);
            return;
        }

        claim.Status      = ClaimStatus.CANCELLED;
        claim.CompletedAt = DateTimeOffset.UtcNow;
        await _claims.UpdateAsync(claim, cancellationToken);

        _logger.LogInformation("AccountClaim {ClaimId} cancelled for account {AccountId}.",
            claim.ClaimId, claim.AccountId);
    }
}
