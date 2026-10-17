using Krizovka.Users.Application.Core.Interfaces.Persistence;
using Krizovka.Users.Domain.Entities;
using MediatR;

namespace Krizovka.Users.Application.Core.Claims.Queries.GetClaim;

public class GetAccountClaimQueryHandler : IRequestHandler<GetAccountClaimQuery, AccountClaim?>
{
    private readonly IAccountClaimRepository _claimRepository;

    public GetAccountClaimQueryHandler(IAccountClaimRepository claimRepository)
    {
        _claimRepository = claimRepository;
    }

    public async Task<AccountClaim?> Handle(GetAccountClaimQuery request, CancellationToken cancellationToken)
    {
        return await _claimRepository.GetByIdAsync(request.ClaimId, cancellationToken);
    }
}
