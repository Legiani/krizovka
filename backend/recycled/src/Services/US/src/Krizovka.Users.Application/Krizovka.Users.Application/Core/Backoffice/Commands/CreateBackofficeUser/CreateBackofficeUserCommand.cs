using MediatR;

namespace Krizovka.Users.Application.Core.Backoffice.Commands.CreateBackofficeUser;

public record CreateBackofficeUserCommand(
    string WorkingEmail,
    string FirstName,
    string LastName,
    Guid? RoleId,
    bool IsSuperAdmin,
    Guid? AccountId = null
) : IRequest<Guid>;
