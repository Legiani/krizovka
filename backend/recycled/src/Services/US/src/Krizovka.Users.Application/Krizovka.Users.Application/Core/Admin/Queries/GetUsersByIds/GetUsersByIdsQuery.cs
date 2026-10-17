using MediatR;

namespace Krizovka.Users.Application.Core.Admin.Queries.GetUsersByIds;

public record GetUsersByIdsQuery(List<Guid> Ids) : IRequest<Dictionary<Guid, string>>;
