using Krizovka.Users.Domain.Entities;
using MediatR;

namespace Krizovka.Users.Application.Core.Roles.Queries.GetRoleAssignments;

public record GetRoleAssignmentsQuery(Guid AccountId) : IRequest<List<RoleAssignment>>;
