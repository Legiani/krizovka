using Krizovka.Users.Application.Core.Search.DTOs;
using Krizovka.Users.Domain.Enums;
using MediatR;

namespace Krizovka.Users.Application.Core.Search.Queries.SearchAccounts;

public record SearchAccountsQuery(
    string? Query = null,
    AccountType? AccountType = null,
    AccountStatus? Status = null,
    int Page = 1,
    int PageSize = 100
) : IRequest<SearchPagedResponse>;
