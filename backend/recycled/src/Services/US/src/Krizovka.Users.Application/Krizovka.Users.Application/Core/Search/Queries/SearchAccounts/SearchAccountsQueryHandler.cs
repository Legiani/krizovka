using Krizovka.Users.Application.Core.Interfaces.Persistence;
using Krizovka.Users.Application.Core.Search.DTOs;
using MediatR;

namespace Krizovka.Users.Application.Core.Search.Queries.SearchAccounts;

public class SearchAccountsQueryHandler : IRequestHandler<SearchAccountsQuery, SearchPagedResponse>
{
    private readonly IUserAccountRepository _accountRepository;

    public SearchAccountsQueryHandler(IUserAccountRepository accountRepository)
    {
        _accountRepository = accountRepository;
    }

    public async Task<SearchPagedResponse> Handle(SearchAccountsQuery request, CancellationToken cancellationToken)
    {
        var (accounts, total) = await _accountRepository.SearchPagedAsync(
            request.Query,
            request.AccountType,
            request.Status,
            request.Page,
            request.PageSize,
            null,
            cancellationToken);

        var mapped = accounts.Select(a => new AccountSearchResult
        {
            AccountId = a.AccountId,
            AccountType = a.AccountType,
            Status = a.Status,
            IdentityId = a.IdentityId,
            Email = a.Email,
            FirstName = a.FirstName,
            LastName = a.LastName,
            MatchedBy = !string.IsNullOrWhiteSpace(request.Query) ? "query" : "filter"
        }).ToList();

        return new SearchPagedResponse(mapped, total);
    }
}
