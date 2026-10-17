using MediatR;

namespace Krizovka.Users.Application.Core.Claims.Commands.CreateClaim;

public record CreateAccountClaimCommand(
    Guid AccountId,
    string Email,
    int ExpireHours = 48
) : IRequest<Guid>;
