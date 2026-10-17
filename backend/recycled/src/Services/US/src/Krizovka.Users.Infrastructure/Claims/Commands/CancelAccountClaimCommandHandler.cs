using Krizovka.Shared.ErrorHandling.Exceptions;
using Krizovka.Users.Domain.Enums;
using Krizovka.Users.Infrastructure.Persistence;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Krizovka.Users.Infrastructure.Claims.Commands;

/// <summary>
/// Cancels a pending account claim.
/// Sets the claim status to CANCELLED and timestamps the cancellation.
/// </summary>
public class CancelAccountClaimCommandHandler
    : IRequestHandler<Krizovka.Users.Application.Core.Claims.Commands.CancelClaim.CancelAccountClaimCommand>
{
    private readonly UsDbContext _db;
    private readonly ILogger<CancelAccountClaimCommandHandler> _logger;

    public CancelAccountClaimCommandHandler(UsDbContext db, ILogger<CancelAccountClaimCommandHandler> logger)
    {
        _db = db;
        _logger = logger;
    }

    public async Task Handle(
        Krizovka.Users.Application.Core.Claims.Commands.CancelClaim.CancelAccountClaimCommand request,
        CancellationToken ct)
    {
        var claim = await _db.AccountClaims.FirstOrDefaultAsync(c => c.ClaimId == request.ClaimId, ct)
            ?? throw new KeyNotFoundException($"AccountClaim {request.ClaimId} not found");

        if (claim.Status == ClaimStatus.CANCELLED)
        {
            _logger.LogInformation("Claim {Id} already CANCELLED – idempotent skip", request.ClaimId);
            return;
        }

        if (claim.Status != ClaimStatus.PENDING)
            throw new ConflictException($"Cannot cancel claim in status {claim.Status}");

        claim.Status = ClaimStatus.CANCELLED;
        await _db.SaveChangesAsync(ct);

        _logger.LogInformation("AccountClaim {Id} cancelled", request.ClaimId);
    }
}
