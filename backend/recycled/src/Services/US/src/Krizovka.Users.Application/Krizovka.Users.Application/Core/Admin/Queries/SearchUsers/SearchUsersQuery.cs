using Krizovka.Users.Domain.Entities;
using MediatR;

namespace Krizovka.Users.Application.Core.Admin.Queries.SearchUsers;

public record SearchUsersQuery(
    string? Email,
    string? Phone,
    string? Name,
    string? Status,
    Guid? IdentityId,
    int Page = 1,
    int PageSize = 20,
    List<string>? Systems = null  // System-level filtering
) : IRequest<SearchUsersResponse>;

public record SearchUsersResponse(
    List<UserSearchResult> Users,
    int TotalCount,
    int Page,
    int PageSize,
    int TotalPages
);

public record UserSearchResult(
    Guid AccountId,
    Guid? IdentityId,
    string? Email,
    string? Phone,
    string? FirstName,
    string? LastName,
    string? DateOfBirth,
    string? RegistrationSystemId,
    string Status,
    string AccountType,
    DateTimeOffset CreatedAt,
    DateTimeOffset? LastLoginAt
);
