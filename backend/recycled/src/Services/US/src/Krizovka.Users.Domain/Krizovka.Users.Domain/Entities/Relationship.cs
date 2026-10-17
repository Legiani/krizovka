using Krizovka.Users.Domain.Enums;

namespace Krizovka.Users.Domain.Entities;

public class Relationship
{
    public Guid RelationshipId { get; set; }
    public Guid ParentAccountId { get; set; }
    public Guid ChildAccountId { get; set; }
    public RelationType RelationType { get; set; }
    public AccountStatus Status { get; set; }
    public DateTimeOffset ValidFrom { get; set; }
    public DateTimeOffset? ValidTo { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public string? MetadataJson { get; set; }

    // Navigation properties
    public UserAccount ParentAccount { get; set; } = null!;
    public UserAccount ChildAccount { get; set; } = null!;
}
