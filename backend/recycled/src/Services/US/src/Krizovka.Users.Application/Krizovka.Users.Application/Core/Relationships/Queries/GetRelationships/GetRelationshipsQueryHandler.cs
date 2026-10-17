using Krizovka.Users.Application.Core.Interfaces.Persistence;
using Krizovka.Users.Domain.Entities;
using MediatR;

namespace Krizovka.Users.Application.Core.Relationships.Queries.GetRelationships;

public class GetRelationshipsQueryHandler : IRequestHandler<GetRelationshipsQuery, List<Relationship>>
{
    private readonly IRelationshipRepository _relationshipRepository;

    public GetRelationshipsQueryHandler(IRelationshipRepository relationshipRepository)
    {
        _relationshipRepository = relationshipRepository;
    }

    public async Task<List<Relationship>> Handle(GetRelationshipsQuery request, CancellationToken cancellationToken)
    {
        return await _relationshipRepository.GetByAccountIdAsync(request.AccountId, cancellationToken);
    }
}
