using Krizovka.Users.Domain.Entities;
using MediatR;

namespace Krizovka.Users.Application.Core.Relationships.Queries.GetRelationships;

public record GetRelationshipsQuery(Guid AccountId) : IRequest<List<Relationship>>;
