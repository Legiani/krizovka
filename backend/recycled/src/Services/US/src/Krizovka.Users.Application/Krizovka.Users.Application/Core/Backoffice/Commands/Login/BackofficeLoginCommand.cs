using MediatR;

namespace Krizovka.Users.Application.Core.Backoffice.Commands.Login;

public record BackofficeLoginCommand(string Email, string Password) : IRequest<LoginResponse>;

public record LoginResponse(
    bool Success,
    string? Token,
    string? ErrorMessage,
    Guid? UserId,
    Guid? IdentityId,
    string? Roles
);
