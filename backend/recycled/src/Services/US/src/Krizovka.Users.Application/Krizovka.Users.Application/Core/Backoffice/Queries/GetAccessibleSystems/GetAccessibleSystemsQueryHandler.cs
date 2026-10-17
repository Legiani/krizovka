using Krizovka.Users.Application.Core.Interfaces.Persistence;
using MediatR;
using Microsoft.Extensions.Caching.Memory;

namespace Krizovka.Users.Application.Core.Backoffice.Queries.GetAccessibleSystems;

public class GetAccessibleSystemsQueryHandler : IRequestHandler<GetAccessibleSystemsQuery, GetAccessibleSystemsResponse>
{
    private const string RoleCatalogCacheKey = "us::backoffice::role-catalog";
    private static readonly TimeSpan RoleCatalogCacheDuration = TimeSpan.FromMinutes(2);

    private readonly IUserAccountRepository _accountRepository;
    private readonly IBackofficeUserRepository _backofficeUserRepository;
    private readonly IRoleAssignmentRepository _roleAssignmentRepository;
    private readonly IBackofficeRoleRepository _roleRepository;
    private readonly IMemoryCache _cache;

    public GetAccessibleSystemsQueryHandler(
        IUserAccountRepository accountRepository,
        IBackofficeUserRepository backofficeUserRepository,
        IRoleAssignmentRepository roleAssignmentRepository,
        IBackofficeRoleRepository roleRepository,
        IMemoryCache cache)
    {
        _accountRepository = accountRepository;
        _backofficeUserRepository = backofficeUserRepository;
        _roleAssignmentRepository = roleAssignmentRepository;
        _roleRepository = roleRepository;
        _cache = cache;
    }

    public async Task<GetAccessibleSystemsResponse> Handle(GetAccessibleSystemsQuery request, CancellationToken cancellationToken)
    {
        // Check if user is a super admin via BackofficeUser.IsSuperAdmin field or role code in assignments
        var boUser = await _backofficeUserRepository.GetByIdentityIdAsync(request.IdentityId, cancellationToken)
            ?? await _backofficeUserRepository.GetByIdAsync(request.IdentityId, cancellationToken);
        var isSuperAdminByFlag = boUser?.IsSuperAdmin == true || boUser?.Role?.Code == Krizovka.Users.Domain.Constants.RoleCodes.BO_SUPER_ADMIN;

        var hasGlobalBackofficeRole = boUser?.Role?.Permissions?.Any(p =>
            p.ScopeType == "GLOBAL" &&
            p.ScopeValue == "*" &&
            p.Module != "*" &&
            p.Action != "*") == true;

        if (isSuperAdminByFlag || hasGlobalBackofficeRole)
        {
            return new GetAccessibleSystemsResponse(new List<string>(), true);
        }

        var user = await _accountRepository.GetByIdentityIdAsync(request.IdentityId, cancellationToken);
        if (user == null || user.Status != Krizovka.Users.Domain.Enums.AccountStatus.ACTIVE)
        {
            return new GetAccessibleSystemsResponse(new List<string>(), false);
        }

        var assignments = await _roleAssignmentRepository.GetByAccountIdAsync(user.AccountId, cancellationToken);
        var now = DateTimeOffset.UtcNow;
        var activeRoles = assignments
            .Where(a => a.Status == Krizovka.Users.Domain.Enums.AccountStatus.ACTIVE
                && a.ValidFrom <= now
                && (a.ValidTo == null || a.ValidTo > now))
            .ToList();

        var isSuperAdmin = activeRoles.Any(a => a.RoleCode == Krizovka.Users.Domain.Constants.RoleCodes.BO_SUPER_ADMIN);
        if (isSuperAdmin)
        {
            return new GetAccessibleSystemsResponse(new List<string>(), true);
        }

        var primaryRoleAssignment = activeRoles.FirstOrDefault();
        if (primaryRoleAssignment == null)
        {
            return new GetAccessibleSystemsResponse(new List<string>(), false);
        }

        var allRoles = await GetRoleCatalogAsync(cancellationToken);
        allRoles.TryGetValue(primaryRoleAssignment.RoleCode, out var role);

        if (role == null || role.Permissions.Count == 0)
        {
            return new GetAccessibleSystemsResponse(new List<string>(), false);
        }

        // Check if user has global access
        var hasGlobalAccess = role.Permissions.Any(p =>
            p.ScopeType == "GLOBAL" && p.ScopeValue == "*"
        );

        if (hasGlobalAccess)
        {
            return new GetAccessibleSystemsResponse(new List<string>(), true);
        }

        // Get specific systems user has access to
        var systems = role.Permissions
            .Where(p => p.ScopeType == "SYSTEM" && !string.IsNullOrEmpty(p.ScopeValue))
            .Select(p => p.ScopeValue)
            .Distinct()
            .ToList();

        return new GetAccessibleSystemsResponse(systems, false);
    }

    private async Task<Dictionary<string, RoleSnapshot>> GetRoleCatalogAsync(CancellationToken cancellationToken)
    {
        if (_cache.TryGetValue(RoleCatalogCacheKey, out Dictionary<string, RoleSnapshot>? cachedCatalog))
        {
            return cachedCatalog ?? [];
        }

        var roles = await _roleRepository.GetAllAsync(cancellationToken);
        var roleCatalog = roles.ToDictionary(
            role => role.Code,
            role => new RoleSnapshot(
                role.Code,
                role.Permissions.Select(permission => new PermissionSnapshot(
                    permission.Module,
                    permission.Action,
                    permission.ScopeType,
                    permission.ScopeValue)).ToArray()),
            StringComparer.OrdinalIgnoreCase);

        _cache.Set(RoleCatalogCacheKey, roleCatalog, RoleCatalogCacheDuration);
        return roleCatalog;
    }

    private sealed record RoleSnapshot(string Code, IReadOnlyCollection<PermissionSnapshot> Permissions);

    private sealed record PermissionSnapshot(
        string Module,
        string Action,
        string ScopeType,
        string ScopeValue);
}
