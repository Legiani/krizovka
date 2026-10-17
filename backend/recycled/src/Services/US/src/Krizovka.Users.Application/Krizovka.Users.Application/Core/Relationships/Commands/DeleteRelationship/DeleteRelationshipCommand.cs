using Krizovka.Users.Application.Core.Interfaces.Persistence;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Krizovka.Users.Application.Core.Relationships.Commands.DeleteRelationship;

public record DeleteRelationshipCommand(Guid RelationshipId) : IRequest;

public class DeleteRelationshipCommandHandler : IRequestHandler<DeleteRelationshipCommand>
{
    private readonly IRelationshipRepository _repo;
    private readonly ILogger<DeleteRelationshipCommandHandler> _logger;

    public DeleteRelationshipCommandHandler(
        IRelationshipRepository repo,
        ILogger<DeleteRelationshipCommandHandler> logger)
    {
        _repo   = repo;
        _logger = logger;
    }

    public async Task Handle(DeleteRelationshipCommand request, CancellationToken cancellationToken)
    {
        var relationship = await _repo.GetByIdAsync(request.RelationshipId, cancellationToken);

        if (relationship == null)
        {
            _logger.LogWarning("DeleteRelationship: relationship {Id} not found.", request.RelationshipId);
            return;
        }

        await _repo.DeleteAsync(relationship, cancellationToken);

        _logger.LogInformation(
            "Relationship {Id} deleted (parent={ParentId} managed child account).",
            relationship.RelationshipId, relationship.ParentAccountId);
    }
}
