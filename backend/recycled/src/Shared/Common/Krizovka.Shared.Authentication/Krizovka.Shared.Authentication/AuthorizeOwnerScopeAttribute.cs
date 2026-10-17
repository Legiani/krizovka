using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Krizovka.Shared.Authentication;

[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = true, Inherited = true)]
public class AuthorizeOwnerScopeAttribute : Attribute, IAsyncActionFilter, IOwnerScopeRequirement
{
    public string ScopeType { get; }
    public string ScopeParameterName { get; }
    public bool Optional { get; set; }

    public AuthorizeOwnerScopeAttribute(string scopeType, string scopeParameterName)
    {
        ScopeType = scopeType;
        ScopeParameterName = scopeParameterName;
    }

    public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        await OwnerScopeAuthorizationHelper.AuthorizeAsync(this, context, next);
    }

    public Task<OwnerScopeResolutionResult> ResolveScopeAsync(
        ActionExecutingContext context,
        CancellationToken cancellationToken)
    {
        var requestedScopeIds = OwnerScopeAuthorizationHelper.TryGetScopeIdsFromRequest(context, ScopeParameterName);
        if (requestedScopeIds.Count == 0)
        {
            if (Optional)
            {
                return Task.FromResult(new OwnerScopeResolutionResult(
                    Array.Empty<ResolvedOwnerScope>(),
                    null,
                    true));
            }

            return Task.FromResult(new OwnerScopeResolutionResult(
                Array.Empty<ResolvedOwnerScope>(),
                new ForbidResult("MestoPlus")));
        }

        return Task.FromResult(new OwnerScopeResolutionResult(
            requestedScopeIds
                .Select(scopeId => new ResolvedOwnerScope(ScopeType, scopeId))
                .ToList()));
    }
}
