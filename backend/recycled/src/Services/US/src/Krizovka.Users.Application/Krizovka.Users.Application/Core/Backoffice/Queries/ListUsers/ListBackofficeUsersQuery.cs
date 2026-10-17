using MediatR;

namespace Krizovka.Users.Application.Core.Backoffice.Queries.ListUsers;

public record ListBackofficeUsersQuery : IRequest<List<BackofficeUserDto>>;

public record BackofficeUserDto(
    Guid Id,
    Guid? IdentityId,
    Guid? AccountId,
    string Email,
    string FirstName,
    string LastName,
    bool IsSuperAdmin,
    string? RoleName,
    string? RoleCode
);
