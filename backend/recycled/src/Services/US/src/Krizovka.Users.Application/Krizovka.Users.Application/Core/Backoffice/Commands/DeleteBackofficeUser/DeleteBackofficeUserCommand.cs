using MediatR;

namespace Krizovka.Users.Application.Core.Backoffice.Commands.DeleteBackofficeUser;

public record DeleteBackofficeUserCommand(Guid Id) : IRequest;
