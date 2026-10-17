using Krizovka.Users.Application.Core.Interfaces.Services.Models;
using Krizovka.Users.Application.Core.Interfaces.Persistence;
using Krizovka.Users.Application.Core.Interfaces.Services;
using MediatR;

namespace Krizovka.Users.Application.Core.Admin.Queries.SearchUsers;

public class SearchUsersQueryHandler : IRequestHandler<SearchUsersQuery, SearchUsersResponse>
{
    private readonly IUserAccountRepository _accountRepository;
    private readonly IKratosIdentityService _kratosIdentityService;

    public SearchUsersQueryHandler(IUserAccountRepository accountRepository, IKratosIdentityService kratosIdentityService)
    {
        _accountRepository = accountRepository;
        _kratosIdentityService = kratosIdentityService;
    }

    public async Task<SearchUsersResponse> Handle(SearchUsersQuery request, CancellationToken cancellationToken)
    {
        // 1. Unified Search (DB Level Filtering + Pagination)
        // Note: For Name/Email search by Kratos, we still might need two phases if Kratos is the source of truth for "Name".
        // But if we search by Status or general Query, we use Repo.

        Krizovka.Users.Domain.Enums.AccountStatus? statusEnum = null;
        if (!string.IsNullOrEmpty(request.Status))
        {
            if (Enum.TryParse<Krizovka.Users.Domain.Enums.AccountStatus>(request.Status, true, out var s))
            {
                statusEnum = s;
            }
        }

        var (pagedAccounts, totalCount) = await GetPagedAccountsAsync(request, statusEnum, cancellationToken);

        var shouldHydrateIdentityTraits = ShouldHydrateIdentityTraits(request, pagedAccounts.Count);
        var identityMap = shouldHydrateIdentityTraits
            ? await HydrateIdentityTraitsAsync(pagedAccounts, cancellationToken)
            : new Dictionary<Guid, KratosIdentityDto>();

        // 3. Map Results
        var resultsList = pagedAccounts.Select(a =>
        {
            KratosIdentityDto? iden = a.IdentityId.HasValue && identityMap.TryGetValue(a.IdentityId.Value, out var i) ? i : null;
            return new UserSearchResult(
                a.AccountId,
                a.IdentityId,
                a.Email ?? iden?.Traits.Email,
                shouldHydrateIdentityTraits ? iden?.Traits.Phone : null,
                a.FirstName ?? iden?.Traits.Name?.First,
                a.LastName ?? iden?.Traits.Name?.Last,
                shouldHydrateIdentityTraits ? iden?.Traits.DateOfBirth : null,
                a.RegistrationSystemId ?? iden?.Traits.RegistrationSystemId,
                a.Status.ToString(),
                a.AccountType.ToString(),
                a.CreatedAt,
                null
            );
        }).ToList();

        var totalPages = (int)Math.Ceiling(totalCount / (double)request.PageSize);

        return new SearchUsersResponse(
            resultsList,
            totalCount,
            request.Page,
            request.PageSize,
            totalPages
        );
    }

    private static bool ShouldHydrateIdentityTraits(SearchUsersQuery request, int resultCount)
    {
        if (request.IdentityId.HasValue || !string.IsNullOrWhiteSpace(request.Email) || !string.IsNullOrWhiteSpace(request.Phone))
        {
            return true;
        }

        return request.PageSize == 1 || resultCount <= 1;
    }

    private async Task<(List<Krizovka.Users.Domain.Entities.UserAccount> Items, int TotalCount)> GetPagedAccountsAsync(
        SearchUsersQuery request,
        Krizovka.Users.Domain.Enums.AccountStatus? statusEnum,
        CancellationToken cancellationToken)
    {
        if (!string.IsNullOrWhiteSpace(request.Phone))
        {
            return await GetPhoneMatchedAccountsAsync(request, statusEnum, cancellationToken);
        }

        if (!request.IdentityId.HasValue)
        {
            return await _accountRepository.SearchPagedAsync(
                request.Email ?? request.Name,
                null,
                statusEnum,
                request.Page,
                request.PageSize,
                request.Systems,
                cancellationToken);
        }

        var account = await _accountRepository.GetByIdentityIdAsync(request.IdentityId.Value, cancellationToken);
        if (account == null)
        {
            return (new List<Krizovka.Users.Domain.Entities.UserAccount>(), 0);
        }

        if (statusEnum.HasValue && account.Status != statusEnum.Value)
        {
            return (new List<Krizovka.Users.Domain.Entities.UserAccount>(), 0);
        }

        if (request.Systems is { Count: > 0 } &&
            (string.IsNullOrWhiteSpace(account.RegistrationSystemId) || !request.Systems.Contains(account.RegistrationSystemId)))
        {
            return (new List<Krizovka.Users.Domain.Entities.UserAccount>(), 0);
        }

        var items = request.Page == 1
            ? new List<Krizovka.Users.Domain.Entities.UserAccount> { account }
            : new List<Krizovka.Users.Domain.Entities.UserAccount>();

        return (items, 1);
    }

    private async Task<(List<Krizovka.Users.Domain.Entities.UserAccount> Items, int TotalCount)> GetPhoneMatchedAccountsAsync(
        SearchUsersQuery request,
        Krizovka.Users.Domain.Enums.AccountStatus? statusEnum,
        CancellationToken cancellationToken)
    {
        var identityIds = await _kratosIdentityService.FindIdentityIdsByCredentialAsync(request.Phone!, cancellationToken);
        if (identityIds.Count == 0)
        {
            return ([], 0);
        }

        var accounts = await _accountRepository.GetByIdentityIdsAsync(identityIds, cancellationToken);

        var filteredAccounts = accounts
            .Where(account => !statusEnum.HasValue || account.Status == statusEnum.Value)
            .Where(account => request.Systems is not { Count: > 0 } ||
                              (!string.IsNullOrWhiteSpace(account.RegistrationSystemId) &&
                               request.Systems.Contains(account.RegistrationSystemId)))
            .OrderByDescending(account => account.CreatedAt)
            .ToList();

        var totalCount = filteredAccounts.Count;
        var items = filteredAccounts
            .Skip((request.Page - 1) * request.PageSize)
            .Take(request.PageSize)
            .ToList();

        return (items, totalCount);
    }

    private async Task<Dictionary<Guid, KratosIdentityDto>> HydrateIdentityTraitsAsync(
        IReadOnlyCollection<Krizovka.Users.Domain.Entities.UserAccount> pagedAccounts,
        CancellationToken cancellationToken)
    {
        var identityIds = pagedAccounts
            .Where(a => a.IdentityId.HasValue)
            .Select(a => a.IdentityId!.Value)
            .Distinct()
            .ToList();

        var fetchedResults = await Task.WhenAll(identityIds.Select(async identityId =>
            (identityId, identity: await _kratosIdentityService.GetIdentityAsync(identityId, cancellationToken))));

        return fetchedResults
            .Where(result => result.identity != null)
            .ToDictionary(result => result.identityId, result => result.identity!);
    }
}
