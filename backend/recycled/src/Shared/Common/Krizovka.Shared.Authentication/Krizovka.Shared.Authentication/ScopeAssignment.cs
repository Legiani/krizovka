namespace Krizovka.Shared.Authentication;

/// <summary>
/// One owner-scope assignment attached to the authenticated principal.
/// Used by owner-scope authorization guards for CITY / IDS / PROVIDER / CARRIER / ORGANIZATION access.
/// </summary>
public record ScopeAssignment(
    string ScopeType,
    string ScopeId,
    string? RoleCode = null
);
