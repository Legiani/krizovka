using MediatR;

namespace Krizovka.Users.Application.Core.Backoffice.Commands.RemovePermission;

public record RemovePermissionCommand(
    Guid PermissionId
) : IRequest<RemovePermissionResponse>;

public record RemovePermissionResponse(
    bool Success,
    string? Error = null
);
