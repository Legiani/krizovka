using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using System.Security.Claims;

namespace Krizovka.Shared.Authentication;

/// <summary>
/// Enforces RBAC permission check at the API level.
/// Resolves the current backoffice user's role and verifies they have the required Module:Action permission.
///
/// Usage:
///   [RequirePermission("NOTIFICATIONS", "READ")]
///   [RequirePermission("MAP", "WRITE")]
///
/// The attribute calls the IBackofficePermissionChecker service (injected via DI), allowing
/// each microservice to provide its own implementation (e.g., via US HTTP client, or embedded logic).
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = true)]
public class RequirePermissionAttribute : Attribute, IAsyncActionFilter
{
    public string Module { get; }
    public string Action { get; }

    public RequirePermissionAttribute(string module, string action)
    {
        Module = module;
        Action = action;
    }

    public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        var identityId = context.HttpContext.User.FindFirstValue(ClaimTypes.NameIdentifier)
                      ?? context.HttpContext.User.FindFirstValue("sub");

        if (string.IsNullOrEmpty(identityId))
        {
            context.Result = new UnauthorizedResult();
            return;
        }

        var ownerScopes = OwnerScopeAuthorizationHelper.GetOwnerScopeRequirements(context);

        // Fast-path via principal claims is safe only for unscoped permission checks.
        if (ownerScopes.Count == 0 && HasRequiredPermissionClaim(context.HttpContext.User, Module, Action))
        {
            await next();
            return;
        }

        var checker = context.HttpContext.RequestServices.GetService<IBackofficePermissionChecker>();
        if (checker == null)
        {
            context.Result = new StatusCodeResult(StatusCodes.Status500InternalServerError);
            return;
        }

        var allowed = ownerScopes.Count == 0
            ? await checker.HasPermissionAsync(identityId, Module, Action)
            : await HasScopedPermissionAsync(checker, identityId, context, ownerScopes);

        if (!allowed)
        {
            if (context.Result != null)
            {
                return;
            }

            context.Result = new ObjectResult(new { error = $"Forbidden: missing permission {Module}.{Action}" })
            {
                StatusCode = 403
            };
            return;
        }

        await next();
    }

    private async Task<bool> HasScopedPermissionAsync(
        IBackofficePermissionChecker checker,
        string identityId,
        ActionExecutingContext context,
        IReadOnlyList<IOwnerScopeRequirement> ownerScopes)
    {
        var currentUserService = context.HttpContext.RequestServices.GetService<ICurrentUserService>();
        if (currentUserService != null && OwnerScopeAuthorizationHelper.HasGlobalAccess(currentUserService))
        {
            if (HasRequiredPermissionClaim(context.HttpContext.User, Module, Action))
            {
                return true;
            }
            return await checker.HasPermissionAsync(identityId, Module, Action);
        }

        var requiredScopes = new HashSet<ResolvedOwnerScope>();

        foreach (var ownerScope in ownerScopes)
        {
            var resolution = await ownerScope.ResolveScopeAsync(context, context.HttpContext.RequestAborted);
            if (resolution.FailureResult != null)
            {
                context.Result = resolution.FailureResult;
                return false;
            }

            if (resolution.SkipOwnerScopeCheck)
            {
                continue;
            }

            if (resolution.Scopes.Count == 0)
            {
                context.Result = new ForbidResult("MestoPlus");
                return false;
            }

            foreach (var scope in resolution.Scopes
                         .Select(scope => new ResolvedOwnerScope(scope.ScopeType, OwnerScopeAuthorizationHelper.NormalizeScopeId(scope.ScopeId)))
                         .Distinct())
            {
                requiredScopes.Add(scope);
            }
        }

        if (requiredScopes.Count == 0)
        {
            if (HasRequiredPermissionClaim(context.HttpContext.User, Module, Action))
            {
                return true;
            }
            return await checker.HasPermissionAsync(identityId, Module, Action);
        }

        foreach (var scope in requiredScopes)
        {
            var allowed = await checker.HasPermissionAsync(identityId, Module, Action, scope.ScopeType, scope.ScopeId);
            if (!allowed)
            {
                return false;
            }
        }

        return true;
    }

    private static bool HasRequiredPermissionClaim(ClaimsPrincipal user, string module, string action)
    {
        foreach (var claim in user.FindAll("Permission"))
        {
            var parts = claim.Value.Split(':', 2, StringSplitOptions.TrimEntries);
            if (parts.Length != 2)
            {
                continue;
            }

            var moduleMatch = parts[0] == "*" || string.Equals(parts[0], module, StringComparison.OrdinalIgnoreCase);
            var actionMatch = parts[1] == "*" || string.Equals(parts[1], action, StringComparison.OrdinalIgnoreCase);

            if (moduleMatch && actionMatch)
            {
                return true;
            }
        }

        return false;
    }
}

/// <summary>
/// Abstraction for checking backoffice permissions.
/// Each service can implement this against its own user store or via HTTP to US.
/// </summary>
public interface IBackofficePermissionChecker
{
    Task<bool> HasPermissionAsync(
        string identityId,
        string module,
        string action,
        string? scopeType = null,
        string? scopeId = null);
}
