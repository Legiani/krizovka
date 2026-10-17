using MediatR;

namespace Krizovka.Users.Application.Core.Backoffice.Commands.UpdateBackofficeUser;

public record UpdateBackofficeUserCommand(
    Guid Id,
    string WorkingEmail,
    string FirstName,
    string LastName,
    Guid? RoleId,
    bool IsSuperAdmin
) : IRequest;
