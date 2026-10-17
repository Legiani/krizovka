using MediatR;

namespace Krizovka.Users.Application.Core.Identities.Commands.UpdateIdentity;

public record UpdateIdentityCommand(
    Guid IdentityId,
    string Email,
    string FirstName,
    string LastName,
    string? Phone,
    string? DateOfBirth
) : IRequest;
