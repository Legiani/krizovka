using Krizovka.Users.Domain.Enums;
using MediatR;

namespace Krizovka.Users.Application.Core.Roles.Commands.AssignRole;

public record AssignRoleCommand(
    Guid AccountId,
    string RoleCode,
    ScopeType ScopeType,
    string ScopeId
) : IRequest<Guid>;
