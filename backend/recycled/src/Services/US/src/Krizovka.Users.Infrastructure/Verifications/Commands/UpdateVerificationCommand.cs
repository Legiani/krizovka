using Krizovka.Users.Domain.Entities;
using Krizovka.Users.Domain.Enums;
using Krizovka.Users.Infrastructure.Persistence;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Krizovka.Users.Infrastructure.Verifications.Commands;

public record UpdateVerificationCommand(
    Guid AccountId,
    string AttributeName,
    string Status,
    string? VerificationMethod,
    string? Metadata,
    string? ActorId = null  // Authenticated user/system that performs the verification
) : IRequest;

public class UpdateVerificationCommandHandler : IRequestHandler<UpdateVerificationCommand>
{
    private readonly UsDbContext _context;

    public UpdateVerificationCommandHandler(UsDbContext context)
    {
        _context = context;
    }

    public async Task Handle(UpdateVerificationCommand request, CancellationToken cancellationToken)
    {
        // Parse status
        if (!Enum.TryParse<VerificationStatus>(request.Status, out var status))
        {
            throw new ArgumentException($"Invalid verification status: {request.Status}");
        }

        // Find existing verification or create new one
        var verification = await _context.UserVerifications
            .FirstOrDefaultAsync(v => v.AccountId == request.AccountId && v.AttributeName == request.AttributeName, cancellationToken);

        var now = DateTimeOffset.UtcNow;

        if (verification == null)
        {
            // Create new verification record
            verification = new UserVerification
            {
                VerificationId = Guid.NewGuid(),
                AccountId = request.AccountId,
                AttributeName = request.AttributeName,
                Status = status,
                VerificationMethod = request.VerificationMethod,
                VerifiedBy = request.ActorId ?? request.VerificationMethod ?? "SYSTEM",
                VerifiedAt = status == VerificationStatus.VERIFIED ? now : null,
                Metadata = request.Metadata,
                CreatedAt = now,
                UpdatedAt = now
            };
            _context.UserVerifications.Add(verification);
        }
        else
        {
            // Update existing verification
            verification.Status = status;
            verification.VerificationMethod = request.VerificationMethod ?? verification.VerificationMethod;
            verification.VerifiedBy = request.ActorId ?? request.VerificationMethod ?? "SYSTEM";
            verification.VerifiedAt = status == VerificationStatus.VERIFIED ? now : verification.VerifiedAt;
            verification.Metadata = request.Metadata ?? verification.Metadata;
            verification.UpdatedAt = now;
        }

        await _context.SaveChangesAsync(cancellationToken);
    }
}
