namespace Krizovka.Shared.Authentication;

/// <summary>
/// Backwards-compatible city-specific owner-scope guard.
/// Prefer AuthorizeOwnerScopeAttribute for new code.
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = false, Inherited = true)]
public class AuthorizeCityAttribute : AuthorizeOwnerScopeAttribute
{
    public AuthorizeCityAttribute() : base("CITY", "cityId")
    {
    }
}
