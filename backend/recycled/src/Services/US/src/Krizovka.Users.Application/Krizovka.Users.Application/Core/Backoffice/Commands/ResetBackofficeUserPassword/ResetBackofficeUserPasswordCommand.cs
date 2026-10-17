using MediatR;

namespace Krizovka.Users.Application.Core.Backoffice.Commands.ResetBackofficeUserPassword;

public record ResetBackofficeUserPasswordCommand(Guid Id, string NewPassword) : IRequest;
