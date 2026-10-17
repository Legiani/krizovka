using Krizovka.Users.Application.Core.Interfaces.Services;
using MediatR;

namespace Krizovka.Users.Application.Core.Roles.Queries.GetRoleCatalog;

public class GetRoleCatalogQueryHandler : IRequestHandler<GetRoleCatalogQuery, List<RoleDefinition>>
{
    private readonly IRoleCatalogService _roleCatalogService;

    public GetRoleCatalogQueryHandler(IRoleCatalogService roleCatalogService)
    {
        _roleCatalogService = roleCatalogService;
    }

    public Task<List<RoleDefinition>> Handle(GetRoleCatalogQuery request, CancellationToken cancellationToken)
    {
        return Task.FromResult(_roleCatalogService.GetAllRoles());
    }
}
