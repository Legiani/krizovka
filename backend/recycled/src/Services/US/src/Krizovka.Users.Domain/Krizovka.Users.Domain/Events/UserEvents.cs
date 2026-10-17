using Krizovka.Users.Domain.Enums;

namespace Krizovka.Users.Domain.Events;

// Account events
public record UserAccountCreated(Guid AccountId, AccountType AccountType, Guid CreatedBy);
public record UserAccountUpdated(Guid AccountId, Dictionary<string, object> Changes);
public record UserIdentityLinked(Guid AccountId, Guid IdentityId);
public record UserAccountSuspended(Guid AccountId);
public record UserAccountDeleted(Guid AccountId);

// Relationship events
public record UserRelationshipCreated(Guid RelationshipId, Guid ParentAccountId, Guid ChildAccountId, RelationType RelationType);
public record UserRelationshipDeleted(Guid RelationshipId);

// Role events
public record UserRoleAssigned(Guid AssignmentId, Guid AccountId, string RoleCode, ScopeType ScopeType, string ScopeId);
public record UserRoleRevoked(Guid AssignmentId);

// Claim events
public record UserAccountClaimRequested(Guid ClaimId, Guid AccountId, string Email, DateTimeOffset ExpiresAt);
public record UserAccountClaimCompleted(Guid ClaimId, Guid AccountId, Guid IdentityId);
