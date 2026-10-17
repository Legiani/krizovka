using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.DependencyInjection;

namespace Krizovka.Shared.Authentication;

[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = true, Inherited = true)]
public sealed class AuthorizeSystemResourceOwnerScopeAttribute : Attribute, IAsyncActionFilter, IOwnerScopeRequirement
{
    public string ResourceParameterName { get; }
    public Type ResolverType { get; }

    public AuthorizeSystemResourceOwnerScopeAttribute(string resourceParameterName, Type resolverType)
    {
        if (!typeof(ISystemCodeResourceResolver).IsAssignableFrom(resolverType))
        {
            throw new ArgumentException(
                $"{resolverType.FullName} must implement {nameof(ISystemCodeResourceResolver)}",
                nameof(resolverType));
        }

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

        var resourceResolver = context.HttpContext.RequestServices.GetService(ResolverType) as ISystemCodeResourceResolver;
        var scopeResolver = context.HttpContext.RequestServices.GetService<ISystemOwnerScopeResolver>();
        if (resourceResolver == null || scopeResolver == null)
        {
            return new OwnerScopeResolutionResult(
                Array.Empty<ResolvedOwnerScope>(),
                new StatusCodeResult(StatusCodes.Status500InternalServerError));
        }

        var scopes = new List<ResolvedOwnerScope>();

        foreach (var resourceId in resourceIds
                     .Select(OwnerScopeAuthorizationHelper.NormalizeScopeId)
                     .Distinct(StringComparer.OrdinalIgnoreCase))
        {
            var systemCodeResolution = await resourceResolver.ResolveAsync(resourceId, cancellationToken);
            if (!systemCodeResolution.Found || string.IsNullOrWhiteSpace(systemCodeResolution.SystemCode))
            {
                return new OwnerScopeResolutionResult(
                    Array.Empty<ResolvedOwnerScope>(),
                    new NotFoundResult());
            }

            var resolvedScope = await scopeResolver.ResolveSystemScopeAsync(systemCodeResolution.SystemCode, cancellationToken);
            if (resolvedScope == null)
            {
                return new OwnerScopeResolutionResult(
                    Array.Empty<ResolvedOwnerScope>(),
                    new NotFoundResult());
            }

            scopes.Add(resolvedScope);
        }

        return new OwnerScopeResolutionResult(scopes);
    }
}
