using Krizovka.Users.Application.Core.Interfaces.Persistence;
using Krizovka.Users.Domain.Entities;
using Krizovka.Users.Domain.Enums;
using Krizovka.Users.Domain.Events;
using Krizovka.Shared.Events.Core;
using MassTransit;
using MediatR;

namespace Krizovka.Users.Application.Core.Relationships.Commands.CreateRelationship;

public class CreateRelationshipCommandHandler : IRequestHandler<CreateRelationshipCommand, Guid>
{
    private readonly IRelationshipRepository _relationshipRepository;
    private readonly IPublishEndpoint _publishEndpoint;

    public CreateRelationshipCommandHandler(
        IRelationshipRepository relationshipRepository,
        IPublishEndpoint publishEndpoint)
    {
        _relationshipRepository = relationshipRepository;
        _publishEndpoint = publishEndpoint;
    }

    public async Task<Guid> Handle(CreateRelationshipCommand request, CancellationToken cancellationToken)
    {
        var relationship = new Relationship
        {
            RelationshipId = Guid.NewGuid(),
            ParentAccountId = request.ParentAccountId,
            ChildAccountId = request.ChildAccountId,
            RelationType = request.RelationType,
            Status = AccountStatus.ACTIVE,
            ValidFrom = DateTimeOffset.UtcNow,
            CreatedAt = DateTimeOffset.UtcNow
        };

        await _relationshipRepository.AddAsync(relationship, cancellationToken);

        // Publish event
        await _publishEndpoint.Publish(new UserRelationshipCreated(
            relationship.RelationshipId,
            relationship.ParentAccountId,
            relationship.ChildAccountId,
            relationship.RelationType
        ), cancellationToken);

        return relationship.RelationshipId;
    }
}
