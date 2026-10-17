using System.Security.Claims;
using Microsoft.AspNetCore.Http;

namespace Krizovka.Shared.Authentication;

/// <summary>
/// Implementation of ICurrentUserService that extracts user context from HttpContext.
/// </summary>
public class CurrentUserService : ICurrentUserService
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    public CurrentUserService(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    private HttpContext? HttpContext => _httpContextAccessor.HttpContext;

    private ClaimsPrincipal? User => HttpContext?.User;

    public Guid? GetCurrentIdentityId()
    {
        var identityIdClaim = User?.FindFirst("IdentityId")?.Value;
        if (identityIdClaim != null && Guid.TryParse(identityIdClaim, out var identityId))
        {
            return identityId;
        }

        // Fallback to NameIdentifier (standard claim type)
        var nameIdentifierClaim = User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (nameIdentifierClaim != null && Guid.TryParse(nameIdentifierClaim, out var nameIdentifier))
        {
            return nameIdentifier;
        }

        return null;
    }

    public Guid? GetCurrentAccountId()
    {
        var accountIdClaim = User?.FindFirst("AccountId")?.Value;
        if (accountIdClaim != null && Guid.TryParse(accountIdClaim, out var accountId))
        {
            return accountId;
        }

        return null;
    }

    public Guid? GetActingAccountId()
    {
        // 1. Try explicit ActingAccountId claim (delegation)
        var actingIdClaim = User?.FindFirst("ActingAccountId")?.Value;
        if (actingIdClaim != null && Guid.TryParse(actingIdClaim, out var actingId))
        {
            return actingId;
        }

        // 2. Fallback to our own AccountId
        return GetCurrentAccountId();
    }

    public Guid GetRequiredIdentityId()
    {
        var identityId = GetCurrentIdentityId();
        if (identityId == null)
        {
            throw new UnauthorizedAccessException("User is not authenticated or IdentityId is missing.");
        }

        return identityId.Value;
    }

    public Guid GetRequiredAccountId()
    {
        var accountId = GetCurrentAccountId();
        if (accountId == null)
        {
            throw new UnauthorizedAccessException("User is not authenticated or AccountId is missing.");
        }

        return accountId.Value;
    }

    public Guid GetRequiredActingAccountId()
    {
        var actingId = GetActingAccountId();
        if (actingId == null)
        {
            throw new UnauthorizedAccessException("User is not authenticated or Acting AccountId is missing.");
        }

        return actingId.Value;
    }

    public bool IsAuthenticated()
    {
        return User?.Identity?.IsAuthenticated ?? false;
    }

    public IReadOnlyList<string> GetRoles()
    {
        if (User == null)
        {
            return Array.Empty<string>();
        }

        return User.FindAll(ClaimTypes.Role)
            .Select(c => c.Value)
            .ToList();
    }

    public IReadOnlyList<string> GetClaims()
    {
        if (User == null)
        {
            return Array.Empty<string>();
        }

        return User.FindAll("Permission")
            .Select(c => c.Value)
            .ToList();
    }

    public bool HasRole(string role)
    {
        return User?.IsInRole(role) ?? false;
    }

    public bool HasClaim(string claim)
    {
        return User?.HasClaim("Permission", claim) ?? false;
    }

    public string? GetScopeType()
    {
        if (HttpContext?.Items.TryGetValue("ScopeType", out var scopeType) == true)
        {
            return scopeType?.ToString();
        }
        return null;
    }

    public string? GetScopeId()
    {
        if (HttpContext?.Items.TryGetValue("ScopeId", out var scopeId) == true)
        {
            return scopeId?.ToString();
        }
        return null;
    }

    public IReadOnlyList<ScopeAssignment> GetScopeAssignments()
    {
        if (HttpContext?.Items.TryGetValue("ScopeAssignments", out var scopeAssignments) == true &&
            scopeAssignments is IEnumerable<ScopeAssignment> typedAssignments)
        {
            return typedAssignments.ToList();
        }

        if (HttpContext?.Items.TryGetValue("CityAssignments", out var cityAssignments) == true &&
            cityAssignments is IEnumerable<CityAssignment> typedCityAssignments)
        {
            return typedCityAssignments
                .Where(a => !string.IsNullOrWhiteSpace(a.CityId))
                .Select(a => new ScopeAssignment("CITY", a.CityId, a.RoleCode))
                .ToList();
        }

        var scopeType = GetScopeType();
        var scopeId = GetScopeId();
        if (!string.IsNullOrWhiteSpace(scopeType) && !string.IsNullOrWhiteSpace(scopeId))
        {
            return new[] { new ScopeAssignment(scopeType, scopeId) };
        }

        return Array.Empty<ScopeAssignment>();
    }
}
