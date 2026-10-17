using MediatR;

namespace Krizovka.Users.Application.Core.Backoffice.Queries.GetBackofficeUser;

public record GetBackofficeUserQuery(Guid Id) : IRequest<BackofficeUserDetailDto>;

public record BackofficeUserDetailDto(
    Guid Id,
    Guid? IdentityId,
    Guid? AccountId,
    string Email,
    string FirstName,
    string LastName,
    bool IsSuperAdmin,
    Guid? RoleId,
    string? RoleName,
    List<string> Permissions
);
