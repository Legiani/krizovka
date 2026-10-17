using MediatR;

namespace Krizovka.Users.Application.Core.Claims.Commands.CompleteClaim;

public record CompleteAccountClaimCommand(
    string ClaimToken,
    Guid IdentityId
) : IRequest;
