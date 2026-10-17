using Krizovka.Users.Domain.Enums;

namespace Krizovka.Users.Domain.Entities;

public class UserAccount
{
    public Guid AccountId { get; set; }
    public Guid? IdentityId { get; set; }
    public AccountType AccountType { get; set; }
    public AccountStatus Status { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }
    public DateTimeOffset? DeletedAt { get; set; }

    /// <summary>
    /// ID of the system where the user registered (e.g. PID, IDSK)
    /// </summary>
    public string? RegistrationSystemId { get; set; }

    // Unified Profile fields (migrated from BackofficeUser/Kratos)
    public string? Email { get; set; }
    public string? FirstName { get; set; }
    public string? LastName { get; set; }

    // Navigation properties
    public ICollection<Relationship> ParentRelationships { get; set; } = new List<Relationship>();
    public ICollection<Relationship> ChildRelationships { get; set; } = new List<Relationship>();
    public ICollection<RoleAssignment> RoleAssignments { get; set; } = new List<RoleAssignment>();
}
