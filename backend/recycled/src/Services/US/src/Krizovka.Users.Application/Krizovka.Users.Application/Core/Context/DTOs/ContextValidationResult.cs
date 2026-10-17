namespace Krizovka.Users.Application.Core.Context.DTOs;

public class ContextValidationResult
{
    public bool Allowed { get; set; }
    public bool IsValid { get; set; } // For authentication middleware compatibility
    public Guid AccountId { get; set; }
    public Guid IdentityId { get; set; } // For authentication middleware compatibility
    public Guid? ActingAccountId { get; set; }
    public string Operation { get; set; } = string.Empty;
    public bool IsBackoffice { get; set; }
    public List<string> Roles { get; set; } = new();
    public List<string> Claims { get; set; } = new(); // For authentication middleware compatibility
    public ScopeInfo? Scope { get; set; }

    /// <summary>All city-role assignments for this backoffice user. Empty for super-admins (they have global access).</summary>
    public List<CityAssignmentDto> CityAssignments { get; set; } = new();

    /// <summary>All owner-scope assignments attached to this context.</summary>
    public List<ScopeAssignmentDto> ScopeAssignments { get; set; } = new();
}

public class ScopeInfo
{
    public string ScopeType { get; set; } = string.Empty;
    public string ScopeId { get; set; } = string.Empty;
}

/// <summary>Represents one city-scoped role assignment for serialization.</summary>
public class CityAssignmentDto
{
    public string CityId { get; set; } = string.Empty;
    public string RoleCode { get; set; } = string.Empty;
}

public class ScopeAssignmentDto
{
    public string ScopeType { get; set; } = string.Empty;
    public string ScopeId { get; set; } = string.Empty;
    public string RoleCode { get; set; } = string.Empty;
}
