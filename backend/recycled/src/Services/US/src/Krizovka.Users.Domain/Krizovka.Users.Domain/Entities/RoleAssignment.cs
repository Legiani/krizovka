using Krizovka.Users.Domain.Enums;

namespace Krizovka.Users.Domain.Entities;

public class RoleAssignment
{
    public Guid AssignmentId { get; set; }
    public Guid AccountId { get; set; }
    public string RoleCode { get; set; } = string.Empty;
    public ScopeType ScopeType { get; set; }
    public string ScopeId { get; set; } = string.Empty;
    public AccountStatus Status { get; set; }
    public DateTimeOffset ValidFrom { get; set; }
    public DateTimeOffset? ValidTo { get; set; }
    public Guid GrantedBy { get; set; }
    public DateTimeOffset CreatedAt { get; set; }

    // Navigation properties
    public UserAccount Account { get; set; } = null!;
}
