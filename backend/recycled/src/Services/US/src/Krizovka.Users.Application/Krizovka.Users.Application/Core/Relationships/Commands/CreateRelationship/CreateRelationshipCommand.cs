using Krizovka.Users.Domain.Enums;
using MediatR;

namespace Krizovka.Users.Application.Core.Relationships.Commands.CreateRelationship;

/// <summary>
/// Creates a relationship between two accounts
/// </summary>
/// <param name="ParentAccountId">The parent/guardian account ID</param>
/// <param name="ChildAccountId">The child/managed account ID</param>
/// <param name="RelationType">
/// Type of relationship:
/// 0 = PARENT_OF,
/// 1 = MANAGES,
/// 2 = PARTNER_OF,
/// 3 = GUARDIAN_OF,
/// 4 = MANAGES_ORGANIZATION_STAFF
/// </param>
public record CreateRelationshipCommand(
    Guid ParentAccountId,
    Guid ChildAccountId,
    RelationType RelationType
) : IRequest<Guid>;
