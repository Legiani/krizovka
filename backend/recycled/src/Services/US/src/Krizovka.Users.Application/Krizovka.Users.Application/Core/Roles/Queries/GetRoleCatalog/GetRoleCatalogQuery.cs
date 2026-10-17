using Krizovka.Users.Application.Core.Interfaces.Services;
using MediatR;

namespace Krizovka.Users.Application.Core.Roles.Queries.GetRoleCatalog;

public record GetRoleCatalogQuery : IRequest<List<RoleDefinition>>;
