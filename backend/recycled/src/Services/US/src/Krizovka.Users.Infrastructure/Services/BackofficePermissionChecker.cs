using Krizovka.Shared.Authentication;
using Krizovka.Users.Application.Core.Interfaces.Persistence;

namespace Krizovka.Users.Infrastructure.Services;

/// <summary>
/// US-side implementation of IBackofficePermissionChecker.
/// Resolves the backoffice user by identityId and checks their roles/permissions from the database.
/// </summary>
public class BackofficePermissionChecker : IBackofficePermissionChecker
{
    private readonly IBackofficeUserRepository _backofficeUserRepository;

    public BackofficePermissionChecker(IBackofficeUserRepository backofficeUserRepository)
    {
        _backofficeUserRepository = backofficeUserRepository;
    }

    public async Task<bool> HasPermissionAsync(
        string identityId,
        string module,
        string action,
        string? scopeType = null,
        string? scopeId = null)
    {
        if (!Guid.TryParse(identityId, out var identityGuid))
            return false;

        var user = await _backofficeUserRepository.GetByIdentityIdAsync(identityGuid, CancellationToken.None);
        if (user == null || !user.IsActive)
            return false;

        // Super admin — always has full access
        if (user.IsSuperAdmin)
            return true;

        if (user.Role == null || user.Role.Permissions == null || !user.Role.Permissions.Any())
            return false;

        var requiresScopeMatch = !string.IsNullOrWhiteSpace(scopeType) && !string.IsNullOrWhiteSpace(scopeId);

        return user.Role.Permissions.Any(p =>
        {
            var moduleMatch = p.Module == "*" || string.Equals(p.Module, module, StringComparison.OrdinalIgnoreCase);
            var actionMatch = p.Action == "*" || string.Equals(p.Action, action, StringComparison.OrdinalIgnoreCase);
            if (!moduleMatch || !actionMatch)
            {
                return false;
            }

            if (string.Equals(p.ScopeType, "GLOBAL", StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }

            if (!requiresScopeMatch)
            {
                return true;
            }

            return string.Equals(p.ScopeType, scopeType, StringComparison.OrdinalIgnoreCase)
                && string.Equals(p.ScopeValue, scopeId, StringComparison.OrdinalIgnoreCase);
        });
    }
}
