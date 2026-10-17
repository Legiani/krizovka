using Krizovka.Users.Infrastructure.Verifications.DTOs;
using Krizovka.Users.Infrastructure.Persistence;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Krizovka.Users.Infrastructure.Verifications.Queries;

public record GetVerificationsQuery(Guid AccountId) : IRequest<GetVerificationsResponse>;

public class GetVerificationsQueryHandler : IRequestHandler<GetVerificationsQuery, GetVerificationsResponse>
{
    private readonly UsDbContext _context;

    public GetVerificationsQueryHandler(UsDbContext context)
    {
        _context = context;
    }

    public async Task<GetVerificationsResponse> Handle(GetVerificationsQuery request, CancellationToken cancellationToken)
    {
        var verifications = await _context.UserVerifications
            .Where(v => v.AccountId == request.AccountId)
            .Select(v => new VerificationDto
            {
                AttributeName = v.AttributeName,
                Status = v.Status.ToString(),
                VerificationMethod = v.VerificationMethod,
                VerifiedBy = v.VerifiedBy,
                VerifiedAt = v.VerifiedAt,
                ExpiresAt = v.ExpiresAt,
                Metadata = v.Metadata
            })
            .ToListAsync(cancellationToken);

        return new GetVerificationsResponse
        {
            Verifications = verifications
        };
    }
}
