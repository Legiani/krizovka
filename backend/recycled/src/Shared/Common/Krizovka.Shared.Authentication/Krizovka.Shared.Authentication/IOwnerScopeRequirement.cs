using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace Krizovka.Shared.Authentication;

public interface IOwnerScopeRequirement
{
    Task<OwnerScopeResolutionResult> ResolveScopeAsync(
        ActionExecutingContext context,
        CancellationToken cancellationToken);
}

public sealed record ResolvedOwnerScope(
    string ScopeType,
    string ScopeId);

public sealed record OwnerScopeResolutionResult(
    IReadOnlyList<ResolvedOwnerScope> Scopes,
    IActionResult? FailureResult = null,
    bool SkipOwnerScopeCheck = false);

public interface IResourceOwnerScopeResolver
{
    Task<ResourceOwnerScopeResolutionResult> ResolveAsync(
        string resourceId,
        CancellationToken cancellationToken);
}

public sealed record ResourceOwnerScopeResolutionResult(
    bool Found,
    IReadOnlyList<string> ScopeIds);

public interface ISystemCodeResourceResolver
{
    Task<SystemCodeResolutionResult> ResolveAsync(
        string resourceId,
        CancellationToken cancellationToken);
}

public sealed record SystemCodeResolutionResult(
    bool Found,
    string? SystemCode);
