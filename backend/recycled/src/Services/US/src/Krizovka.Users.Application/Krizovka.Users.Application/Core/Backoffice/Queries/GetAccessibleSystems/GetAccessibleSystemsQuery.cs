using MediatR;

namespace Krizovka.Users.Application.Core.Backoffice.Queries.GetAccessibleSystems;

public record GetAccessibleSystemsQuery(
    Guid IdentityId
) : IRequest<GetAccessibleSystemsResponse>;

public record GetAccessibleSystemsResponse(
    List<string> Systems,
    bool HasGlobalAccess
);
