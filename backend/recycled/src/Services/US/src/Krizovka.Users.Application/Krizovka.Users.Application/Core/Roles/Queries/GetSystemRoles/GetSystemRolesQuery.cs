using Krizovka.Users.Domain.Entities;
using MediatR;

namespace Krizovka.Users.Application.Core.Roles.Queries.GetSystemRoles;

/// <summary>
/// Returns all system-scoped role assignments for the given account.
/// "System-scoped" means ScopeType is IDS, PROVIDER, or CARRIER.
/// Used by the Gateway to inject X-System-Roles header into downstream requests.
/// </summary>
public record GetSystemRolesQuery(
    Guid AccountId,
    /// <summary>Optional: filter by specific ScopeId (e.g. IDS system_id). Null = return all.</summary>
    string? ScopeId = null
) : IRequest<List<RoleAssignment>>;
