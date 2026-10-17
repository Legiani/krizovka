using MediatR;

namespace Krizovka.Users.Application.Core.Identities.Commands.LinkIdentity;

public record LinkIdentityCommand(
    Guid IdentityId,
    Guid AccountId
) : IRequest;
