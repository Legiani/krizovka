using Krizovka.Users.Infrastructure.Persistence;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Krizovka.Users.Infrastructure.Relationships.Commands;

/// <summary>
/// Deletes (removes) a relationship record.
/// </summary>
public class DeleteRelationshipCommandHandler
    : IRequestHandler<Krizovka.Users.Application.Core.Relationships.Commands.DeleteRelationship.DeleteRelationshipCommand>
{
    private readonly UsDbContext _db;
    private readonly ILogger<DeleteRelationshipCommandHandler> _logger;

    public DeleteRelationshipCommandHandler(UsDbContext db, ILogger<DeleteRelationshipCommandHandler> logger)
    {
        _db = db;
        _logger = logger;
    }

    public async Task Handle(
        Krizovka.Users.Application.Core.Relationships.Commands.DeleteRelationship.DeleteRelationshipCommand request,
        CancellationToken ct)
    {
        var relationship = await _db.Relationships
            .FirstOrDefaultAsync(r => r.RelationshipId == request.RelationshipId, ct)
            ?? throw new KeyNotFoundException($"Relationship {request.RelationshipId} not found");

        _db.Relationships.Remove(relationship);
        await _db.SaveChangesAsync(ct);

        _logger.LogInformation("Relationship {Id} between {ParentId}→{ChildId} deleted",
            request.RelationshipId, relationship.ParentAccountId, relationship.ChildAccountId);
    }
}
