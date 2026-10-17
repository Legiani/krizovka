using Krizovka.Users.Application.Core.Interfaces.Persistence;
using MediatR;
using Microsoft.Extensions.Caching.Memory;

namespace Krizovka.Users.Application.Core.Backoffice.Queries.CheckPermission;

public class CheckPermissionQueryHandler : IRequestHandler<CheckPermissionQuery, CheckPermissionResponse>
{
    private const string RoleCatalogCacheKey = "us::backoffice::role-catalog";
    private static readonly TimeSpan RoleCatalogCacheDuration = TimeSpan.FromMinutes(2);

    private readonly IUserAccountRepository _accountRepository;
    private readonly IBackofficeUserRepository _backofficeUserRepository;
    private readonly IRoleAssignmentRepository _roleAssignmentRepository;
    private readonly IBackofficeRoleRepository _roleRepository;
    private readonly IMemoryCache _cache;

    public CheckPermissionQueryHandler(
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

    public async Task<CheckPermissionResponse> Handle(CheckPermissionQuery request, CancellationToken cancellationToken)
    {
        var user = await _accountRepository.GetByIdentityIdAsync(request.IdentityId, cancellationToken);
        var boUser = await _backofficeUserRepository.GetByIdentityIdAsync(request.IdentityId, cancellationToken)
            ?? await _backofficeUserRepository.GetByIdAsync(request.IdentityId, cancellationToken);

        var userInactive = user != null && user.Status != Krizovka.Users.Domain.Enums.AccountStatus.ACTIVE;
        if (user == null && boUser == null)
        {
            return new CheckPermissionResponse(false, "User not found or inactive");
        }
        if (userInactive && (boUser == null || !boUser.IsActive))
        {
            return new CheckPermissionResponse(false, "User not found or inactive");
        }

        var allRoles = await GetRoleCatalogAsync(cancellationToken);
        var requiresRequestedScope = !string.IsNullOrWhiteSpace(request.ScopeType) && !string.IsNullOrWhiteSpace(request.ScopeId);

        if (HasGlobalBackofficeRolePermission(boUser, request.Module, request.Action))
        {
            return new CheckPermissionResponse(true);
        }

        var activeAssignments = new List<Krizovka.Users.Domain.Entities.RoleAssignment>();
        if (user != null && user.Status == Krizovka.Users.Domain.Enums.AccountStatus.ACTIVE)
        {
            var assignments = await _roleAssignmentRepository.GetByAccountIdAsync(user.AccountId, cancellationToken);
            var now = DateTimeOffset.UtcNow;
            activeAssignments = assignments
                .Where(a => a.Status == Krizovka.Users.Domain.Enums.AccountStatus.ACTIVE
                    && a.ValidFrom <= now
                    && (a.ValidTo == null || a.ValidTo > now))
                .ToList();
        }

        // Super admin — always has full access
        var isSuperAdmin = boUser?.IsSuperAdmin == true
            || string.Equals(boUser?.Role?.Code, Krizovka.Users.Domain.Constants.RoleCodes.BO_SUPER_ADMIN, StringComparison.OrdinalIgnoreCase)
            || activeAssignments.Any(a => a.RoleCode == Krizovka.Users.Domain.Constants.RoleCodes.BO_SUPER_ADMIN);
        if (isSuperAdmin)
        {
            return new CheckPermissionResponse(true);
        }

        if (!activeAssignments.Any())
        {
            return new CheckPermissionResponse(false, "No active role assignment found");
        }

        // Iterate ALL assignments — user may have different roles per owner scope.
        var hasPermission = activeAssignments.Any(assignment =>
        {
            var assignmentScopeMatch = assignment.ScopeType == Krizovka.Users.Domain.Enums.ScopeType.GLOBAL
                || !requiresRequestedScope
                || (assignment.ScopeType.ToString().Equals(request.ScopeType, StringComparison.OrdinalIgnoreCase)
                    && string.Equals(assignment.ScopeId, request.ScopeId, StringComparison.OrdinalIgnoreCase));

            if (!assignmentScopeMatch) return false;

            if (!allRoles.TryGetValue(assignment.RoleCode, out var role) || role.Permissions.Count == 0) return false;

            return role.Permissions.Any(p =>
            {
                var moduleMatch = p.Module == "*" || p.Module.Equals(request.Module, StringComparison.OrdinalIgnoreCase);
                var actionMatch = p.Action == "*" || p.Action.Equals(request.Action, StringComparison.OrdinalIgnoreCase);
                if (!moduleMatch || !actionMatch)
                {
                    return false;
                }

                if (string.Equals(p.ScopeType, "GLOBAL", StringComparison.OrdinalIgnoreCase) &&
                    string.Equals(p.ScopeValue, "*", StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }

                if (!requiresRequestedScope)
                {
                    if (string.IsNullOrEmpty(request.LegacySystemId))
                    {
                        return true;
                    }

                    return p.ScopeType == "SYSTEM" &&
                           string.Equals(p.ScopeValue, request.LegacySystemId, StringComparison.OrdinalIgnoreCase);
                }

                return p.ScopeType.Equals(request.ScopeType, StringComparison.OrdinalIgnoreCase)
                    && string.Equals(p.ScopeValue, request.ScopeId, StringComparison.OrdinalIgnoreCase);
            });
        });

        return hasPermission
            ? new CheckPermissionResponse(true)
            : new CheckPermissionResponse(false, $"Missing permission: {request.Module}.{request.Action}" +
                (request.ScopeType != null && request.ScopeId != null ? $" for {request.ScopeType}:{request.ScopeId}" : "") +
                (request.LegacySystemId != null ? $" on legacy system {request.LegacySystemId}" : ""));
    }

    private static bool HasGlobalBackofficeRolePermission(Krizovka.Users.Domain.Entities.BackofficeUser? boUser, string module, string action)
    {
        if (boUser == null || !boUser.IsActive || boUser.Role?.Permissions == null)
        {
            return false;
        }

        return boUser.Role.Permissions.Any(p =>
        {
            var moduleMatch = p.Module == "*" || p.Module.Equals(module, StringComparison.OrdinalIgnoreCase);
            var actionMatch = p.Action == "*" || p.Action.Equals(action, StringComparison.OrdinalIgnoreCase);
            if (!moduleMatch || !actionMatch)
            {
                return false;
            }

            return string.Equals(p.ScopeType, "GLOBAL", StringComparison.OrdinalIgnoreCase)
                && string.Equals(p.ScopeValue, "*", StringComparison.OrdinalIgnoreCase);
        });
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
