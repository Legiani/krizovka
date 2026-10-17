using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.DependencyInjection;

namespace Krizovka.Shared.Authentication;

[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = true, Inherited = true)]
public sealed class AuthorizeSystemOwnerScopeAttribute : Attribute, IAsyncActionFilter, IOwnerScopeRequirement
{
    public string SystemParameterName { get; }

    public AuthorizeSystemOwnerScopeAttribute(string systemParameterName)
    {
        SystemParameterName = systemParameterName;
    }

    public Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        return OwnerScopeAuthorizationHelper.AuthorizeAsync(this, context, next);
    }

    public async Task<OwnerScopeResolutionResult> ResolveScopeAsync(
        ActionExecutingContext context,
        CancellationToken cancellationToken)
    {
        var systemCodes = OwnerScopeAuthorizationHelper.TryGetScopeIdsFromRequest(context, SystemParameterName);
        if (systemCodes.Count == 0)
        {
            return new OwnerScopeResolutionResult(
                Array.Empty<ResolvedOwnerScope>(),
                new ForbidResult("MestoPlus"));
        }

        var resolver = context.HttpContext.RequestServices.GetService<ISystemOwnerScopeResolver>();
        if (resolver == null)
        {
            return new OwnerScopeResolutionResult(
                Array.Empty<ResolvedOwnerScope>(),
                new StatusCodeResult(StatusCodes.Status500InternalServerError));
        }

        var scopes = new List<ResolvedOwnerScope>();
        foreach (var systemCode in systemCodes
                     .Select(OwnerScopeAuthorizationHelper.NormalizeScopeId)
                     .Distinct(StringComparer.OrdinalIgnoreCase))
        {
            var resolved = await resolver.ResolveSystemScopeAsync(systemCode, cancellationToken);
            if (resolved == null)
            {
                return new OwnerScopeResolutionResult(
                    Array.Empty<ResolvedOwnerScope>(),
                    new NotFoundResult());
            }

            scopes.Add(resolved);
        }

        return new OwnerScopeResolutionResult(scopes);
    }
}
