using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace Krizovka.Shared.Authentication;

[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = true, Inherited = true)]
public class AuthorizeResourceOwnerScopeAttribute : Attribute, IAsyncActionFilter, IOwnerScopeRequirement
{
    public string ScopeType { get; }
    public string ResourceParameterName { get; }
    public Type ResolverType { get; }

    public AuthorizeResourceOwnerScopeAttribute(string scopeType, string resourceParameterName, Type resolverType)
    {
        if (!typeof(IResourceOwnerScopeResolver).IsAssignableFrom(resolverType))
        {
            throw new ArgumentException(
                $"{resolverType.FullName} must implement {nameof(IResourceOwnerScopeResolver)}",
                nameof(resolverType));
        }

        ScopeType = scopeType;
        ResourceParameterName = resourceParameterName;
        ResolverType = resolverType;
    }

    public Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        return OwnerScopeAuthorizationHelper.AuthorizeAsync(this, context, next);
    }

    public async Task<OwnerScopeResolutionResult> ResolveScopeAsync(
        ActionExecutingContext context,
        CancellationToken cancellationToken)
    {
        var resourceIds = OwnerScopeAuthorizationHelper.TryGetScopeIdsFromRequest(context, ResourceParameterName);
        if (resourceIds.Count == 0)
        {
            return new OwnerScopeResolutionResult(
                Array.Empty<ResolvedOwnerScope>(),
                new ForbidResult("MestoPlus"));
        }

        var resolver = context.HttpContext.RequestServices.GetService(ResolverType) as IResourceOwnerScopeResolver;
        if (resolver == null)
        {
            return new OwnerScopeResolutionResult(
                Array.Empty<ResolvedOwnerScope>(),
                new StatusCodeResult(StatusCodes.Status500InternalServerError));
        }

        var resolvedScopes = new List<ResolvedOwnerScope>();

        foreach (var resourceId in resourceIds
                     .Select(OwnerScopeAuthorizationHelper.NormalizeScopeId)
                     .Distinct(StringComparer.OrdinalIgnoreCase))
        {
            var resolution = await resolver.ResolveAsync(resourceId, cancellationToken);
            if (!resolution.Found)
            {
                return new OwnerScopeResolutionResult(
                    Array.Empty<ResolvedOwnerScope>(),
                    new NotFoundResult());
            }

            foreach (var scopeId in resolution.ScopeIds
                         .Where(scopeId => !string.IsNullOrWhiteSpace(scopeId))
                         .Select(OwnerScopeAuthorizationHelper.NormalizeScopeId))
            {
                resolvedScopes.Add(new ResolvedOwnerScope(ScopeType, scopeId));
            }
        }

        if (resolvedScopes.Count == 0)
        {
            return new OwnerScopeResolutionResult(
                Array.Empty<ResolvedOwnerScope>(),
                new ForbidResult("MestoPlus"));
        }

        return new OwnerScopeResolutionResult(resolvedScopes);
    }
}
