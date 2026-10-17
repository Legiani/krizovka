using MediatR;

namespace Krizovka.Users.Application.Core.Identities.Commands.CreateAccountFromIdentity;

public record CreateAccountFromIdentityCommand(Guid IdentityId, string? RegistrationSystemId, string? Email = null, string? FirstName = null, string? LastName = null) : IRequest;
