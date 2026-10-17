using Krizovka.Users.Application.Core.Interfaces.Services.Models;
using MediatR;
using Krizovka.Users.Application.Core.Interfaces.Persistence;
using Krizovka.Users.Application.Core.Interfaces.Services;

namespace Krizovka.Users.Application.Core.Admin.Queries.GetUsersByIds;

public class GetUsersByIdsQueryHandler : IRequestHandler<GetUsersByIdsQuery, Dictionary<Guid, string>>
{
    private readonly IUserAccountRepository _accountRepository;
    private readonly IKratosIdentityService _kratosIdentityService;

    public GetUsersByIdsQueryHandler(IUserAccountRepository accountRepository, IKratosIdentityService kratosIdentityService)
    {
        _accountRepository = accountRepository;
        _kratosIdentityService = kratosIdentityService;
    }

    public async Task<Dictionary<Guid, string>> Handle(GetUsersByIdsQuery request, CancellationToken cancellationToken)
    {
        if (request.Ids == null || !request.Ids.Any())
        {
            return new Dictionary<Guid, string>();
        }

        var distinctIds = request.Ids.Distinct().ToHashSet();

        var accounts = await _accountRepository.GetByIdsAsync(distinctIds.ToArray(), cancellationToken);
        var fallbackIdentities = await HydrateFallbackIdentitiesAsync(accounts, cancellationToken);

        var result = new Dictionary<Guid, string>();

        foreach (var account in accounts)
        {
            var displayName = BuildDisplayName(account.FirstName, account.LastName, account.Email);

            if (string.IsNullOrWhiteSpace(displayName) &&
                account.IdentityId.HasValue &&
                fallbackIdentities.TryGetValue(account.IdentityId.Value, out var identity))
            {
                displayName = BuildDisplayName(identity.Traits.Name?.First, identity.Traits.Name?.Last, identity.Traits.Email);
            }

            result[account.AccountId] = string.IsNullOrWhiteSpace(displayName) ? "Unknown" : displayName;
        }

        return result;
    }

    private async Task<Dictionary<Guid, KratosIdentityDto>> HydrateFallbackIdentitiesAsync(
        IReadOnlyCollection<Krizovka.Users.Domain.Entities.UserAccount> accounts,
        CancellationToken cancellationToken)
    {
        var missingIdentityIds = accounts
            .Where(account => string.IsNullOrWhiteSpace(BuildDisplayName(account.FirstName, account.LastName, account.Email)) && account.IdentityId.HasValue)
            .Select(account => account.IdentityId!.Value)
            .Distinct()
            .ToList();

        var identities = await Task.WhenAll(missingIdentityIds.Select(async identityId =>
            (identityId, identity: await _kratosIdentityService.GetIdentityAsync(identityId, cancellationToken))));

        return identities
            .Where(result => result.identity != null)
            .ToDictionary(result => result.identityId, result => result.identity!);
    }

    private static string? BuildDisplayName(string? firstName, string? lastName, string? email)
    {
        var fullName = $"{firstName} {lastName}".Trim();
        if (!string.IsNullOrWhiteSpace(fullName))
        {
            return fullName;
        }

        return string.IsNullOrWhiteSpace(email) ? null : email;
    }
}
