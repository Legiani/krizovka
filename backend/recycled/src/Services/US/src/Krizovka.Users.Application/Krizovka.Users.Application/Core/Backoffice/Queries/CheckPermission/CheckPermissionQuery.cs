using MediatR;

namespace Krizovka.Users.Application.Core.Backoffice.Queries.CheckPermission;

public record CheckPermissionQuery(
    Guid IdentityId,
    string Module,
    string Action,
    string? ScopeType = null,
    string? ScopeId = null,
    string? LegacySystemId = null
) : IRequest<CheckPermissionResponse>;

public record CheckPermissionResponse(
    bool Allowed,
    string? Reason = null
);
