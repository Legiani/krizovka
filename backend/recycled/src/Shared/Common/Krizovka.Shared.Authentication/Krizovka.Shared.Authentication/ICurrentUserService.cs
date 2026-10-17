namespace Krizovka.Shared.Authentication;

/// <summary>
/// Provides access to the current authenticated user's context.
/// </summary>
public interface ICurrentUserService
{
    /// <summary>
    /// Gets the current user's Identity ID (from JWT claims, validated by US).
    /// </summary>
    /// <returns>The Identity ID, or null if not authenticated.</returns>
    Guid? GetCurrentIdentityId();

    /// <summary>
    /// Gets the current user's Account ID (from US service).
    /// </summary>
    /// <returns>The Account ID, or null if not authenticated.</returns>
    Guid? GetCurrentAccountId();

    /// <summary>
    /// Gets the ID of the account the user is currently acting as.
    /// This is either the explicit `ActingAccountId` claim (delegation)
    /// or the `AccountId` claim (acting as self).
    /// </summary>
    /// <returns>The acting Account ID, or null if not authenticated.</returns>
    Guid? GetActingAccountId();

    /// <summary>
    /// Gets the current user's Identity ID, throwing if not authenticated.
    /// </summary>
    /// <returns>The Identity ID.</returns>
    /// <exception cref="UnauthorizedAccessException">Thrown when user is not authenticated.</exception>
    Guid GetRequiredIdentityId();

    /// <summary>
    /// Gets the current user's Account ID, throwing if not authenticated.
    /// </summary>
    /// <returns>The Account ID.</returns>
    /// <exception cref="UnauthorizedAccessException">Thrown when user is not authenticated.</exception>
    Guid GetRequiredAccountId();

    /// <summary>
    /// Gets the ID of the account the user is currently acting as, throwing if not authenticated.
    /// </summary>
    /// <returns>The acting Account ID.</returns>
    /// <exception cref="UnauthorizedAccessException">Thrown when user is not authenticated.</exception>
    Guid GetRequiredActingAccountId();

    /// <summary>
    /// Checks if the current request is authenticated.
    /// </summary>
    /// <returns>True if authenticated, false otherwise.</returns>
    bool IsAuthenticated();

    /// <summary>
    /// Gets all roles assigned to the current user.
    /// </summary>
    /// <returns>List of role names.</returns>
    IReadOnlyList<string> GetRoles();

    /// <summary>
    /// Gets all permissions/claims assigned to the current user.
    /// </summary>
    /// <returns>List of permission/claim values.</returns>
    IReadOnlyList<string> GetClaims();

    /// <summary>
    /// Checks if the current user has a specific role.
    /// </summary>
    /// <param name="role">Role name to check.</param>
    /// <returns>True if user has the role, false otherwise.</returns>
    bool HasRole(string role);

    /// <summary>
    /// Checks if the current user has a specific permission/claim.
    /// </summary>
    /// <param name="claim">Claim value to check.</param>
    /// <returns>True if user has the claim, false otherwise.</returns>
    bool HasClaim(string claim);

    /// <summary>
    /// Gets the authorization scope type (e.g. GLOBAL, CITY) from the current user.
    /// </summary>
    /// <returns>Scope type string.</returns>
    string? GetScopeType();

    /// <summary>
    /// Gets the authorization scope value (e.g. City UUID) from the current user.
    /// </summary>
    /// <returns>Scope definition ID string.</returns>
    string? GetScopeId();

    /// <summary>
    /// Gets all owner-scope assignments attached to the current user context.
    /// </summary>
    /// <returns>List of CITY / IDS / PROVIDER / CARRIER / ORGANIZATION assignments.</returns>
    IReadOnlyList<ScopeAssignment> GetScopeAssignments();
}
