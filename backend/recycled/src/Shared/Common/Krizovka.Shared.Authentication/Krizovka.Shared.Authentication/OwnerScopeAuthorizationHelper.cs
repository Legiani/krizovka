using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Krizovka.Shared.Authentication;

internal static class OwnerScopeAuthorizationHelper
{
    private static readonly IEqualityComparer<ResolvedOwnerScope> ScopeComparer = new ResolvedOwnerScopeComparer();

    public static async Task AuthorizeAsync(
        IOwnerScopeRequirement requirement,
        ActionExecutingContext context,
        ActionExecutionDelegate next)
    {
        var currentUserService = context.HttpContext.RequestServices.GetService<ICurrentUserService>();
        var loggerFactory = context.HttpContext.RequestServices.GetService<ILoggerFactory>();
        var logger = loggerFactory?.CreateLogger(requirement.GetType().FullName ?? requirement.GetType().Name);

        if (currentUserService == null)
        {
            logger?.LogError("ICurrentUserService is not registered in DI.");
            context.Result = new StatusCodeResult(StatusCodes.Status500InternalServerError);
            return;
        }

        if (HasGlobalAccess(currentUserService))
        {
            await next();
            return;
        }

        var resolution = await requirement.ResolveScopeAsync(context, context.HttpContext.RequestAborted);
        if (resolution.FailureResult != null)
        {
            context.Result = resolution.FailureResult;
            return;
        }

        if (resolution.SkipOwnerScopeCheck)
        {
            await next();
            return;
        }

        var allowedScopes = currentUserService.GetScopeAssignments()
            .Select(a => new ResolvedOwnerScope(a.ScopeType, NormalizeScopeId(a.ScopeId)))
            .ToHashSet(ScopeComparer);

        var fallbackScopeType = currentUserService.GetScopeType();
        var fallbackScopeId = currentUserService.GetScopeId();
        if (!string.IsNullOrWhiteSpace(fallbackScopeType) && !string.IsNullOrWhiteSpace(fallbackScopeId))
        {
            allowedScopes.Add(new ResolvedOwnerScope(fallbackScopeType, NormalizeScopeId(fallbackScopeId)));
        }

        var normalizedRequestedScopes = resolution.Scopes
            .Select(scope => new ResolvedOwnerScope(scope.ScopeType.ToUpperInvariant(), NormalizeScopeId(scope.ScopeId)))
            .Distinct(ScopeComparer)
            .ToList();

        var allAllowed = normalizedRequestedScopes.All(scope => allowedScopes.Contains(scope));
        if (!allAllowed)
        {
            logger?.LogWarning(
                "Owner scope authorization failed: requested scopes {RequestedScopes} are outside allowed assignments {AllowedScopes}.",
                string.Join(", ", normalizedRequestedScopes.Select(FormatScope)),
                string.Join(", ", allowedScopes.Select(FormatScope)));
            context.Result = new ForbidResult("MestoPlus");
            return;
        }

        await next();
    }

    public static bool HasGlobalAccess(ICurrentUserService currentUserService)
    {
        if (string.Equals(currentUserService.GetScopeType(), "GLOBAL", StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        return currentUserService.GetScopeAssignments()
            .Any(a => string.Equals(a.ScopeType, "GLOBAL", StringComparison.OrdinalIgnoreCase));
    }

    public static string NormalizeScopeId(string scopeId)
    {
        return Guid.TryParse(scopeId, out var parsedGuid)
            ? parsedGuid.ToString()
            : scopeId.Trim();
    }

    public static string FormatScope(ResolvedOwnerScope scope)
    {
        return $"{scope.ScopeType}:{scope.ScopeId}";
    }

    private sealed class ResolvedOwnerScopeComparer : IEqualityComparer<ResolvedOwnerScope>
    {
        public bool Equals(ResolvedOwnerScope? x, ResolvedOwnerScope? y)
        {
            if (ReferenceEquals(x, y))
            {
                return true;
            }

            if (x is null || y is null)
            {
                return false;
            }

            return string.Equals(x.ScopeType, y.ScopeType, StringComparison.OrdinalIgnoreCase)
                && string.Equals(x.ScopeId, y.ScopeId, StringComparison.OrdinalIgnoreCase);
        }

        public int GetHashCode(ResolvedOwnerScope obj)
        {
            return HashCode.Combine(
                obj.ScopeType.ToUpperInvariant(),
                obj.ScopeId.ToUpperInvariant());
        }
    }

    public static IReadOnlyList<IOwnerScopeRequirement> GetOwnerScopeRequirements(ActionExecutingContext context)
    {
        return context.Filters
            .OfType<IOwnerScopeRequirement>()
            .ToList();
    }

    public static IReadOnlyList<string> TryGetScopeIdsFromRequest(ActionExecutingContext context, string parameterName)
    {
        if (context.HttpContext.Request.Query.TryGetValue(parameterName, out var queryValues))
        {
            var values = queryValues
                .Select(v => v?.Trim())
                .Where(v => !string.IsNullOrWhiteSpace(v))
                .Select(v => v!)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();
            if (values.Count > 0)
            {
                return values;
            }
        }

        if (context.RouteData.Values.TryGetValue(parameterName, out var routeValue) && routeValue != null)
        {
            return new[] { routeValue.ToString()! };
        }

        foreach (var (argumentName, argumentValue) in context.ActionArguments)
        {
            if (argumentValue == null)
            {
                continue;
            }

            if (string.Equals(argumentName, parameterName, StringComparison.OrdinalIgnoreCase))
            {
                var directValues = ExtractScopeIds(argumentValue);
                if (directValues.Count > 0)
                {
                    return directValues;
                }
            }

            var nestedValues = ExtractScopeIdsFromObject(argumentValue, parameterName);
            if (nestedValues.Count > 0)
            {
                return nestedValues;
            }
        }

        return Array.Empty<string>();
    }

    public static IReadOnlyList<string> ExtractScopeIdsFromObject(object argument, string parameterName)
    {
        var type = argument.GetType();
        foreach (var candidateName in GetCandidatePropertyNames(parameterName))
        {
            var property = type.GetProperty(candidateName,
                System.Reflection.BindingFlags.Public |
                System.Reflection.BindingFlags.Instance |
                System.Reflection.BindingFlags.IgnoreCase);
            if (property == null)
            {
                continue;
            }

            var values = ExtractScopeIds(property.GetValue(argument));
            if (values.Count > 0)
            {
                return values;
            }
        }

        return Array.Empty<string>();
    }

    private static IReadOnlyList<string> ExtractScopeIds(object? value)
    {
        if (value == null)
        {
            return Array.Empty<string>();
        }

        if (value is string stringValue)
        {
            return string.IsNullOrWhiteSpace(stringValue) ? Array.Empty<string>() : new[] { stringValue.Trim() };
        }

        if (value is Guid guidValue && guidValue != Guid.Empty)
        {
            return new[] { guidValue.ToString() };
        }

        if (value is IEnumerable<Guid> guidEnumerable)
        {
            return guidEnumerable.Select(g => g.ToString()).Distinct(StringComparer.OrdinalIgnoreCase).ToList();
        }

        if (value is IEnumerable<string> stringEnumerable)
        {
            return stringEnumerable
                .Where(v => !string.IsNullOrWhiteSpace(v))
                .Select(v => v.Trim())
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();
        }

        return Array.Empty<string>();
    }

    private static IEnumerable<string> GetCandidatePropertyNames(string parameterName)
    {
        yield return parameterName;

        if (parameterName.EndsWith("Id", StringComparison.OrdinalIgnoreCase))
        {
            yield return parameterName[..^2] + "Ids";
        }
    }
}
