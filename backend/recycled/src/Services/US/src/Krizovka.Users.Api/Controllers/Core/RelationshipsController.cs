using Krizovka.Users.Application.Core.Relationships.Commands.CreateRelationship;
using Krizovka.Users.Application.Core.Relationships.Commands.DeleteRelationship;
using Krizovka.Users.Application.Core.Relationships.Queries.GetRelationships;
using Microsoft.AspNetCore.Authorization;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace Krizovka.Users.Api.Controllers.Core;

[ApiController]
[Route("relationships")]
[Authorize]
public class RelationshipsController : ControllerBase
{
    private readonly IMediator _mediator;

    public RelationshipsController(IMediator mediator)
    {
        _mediator = mediator;
    }

    /// <summary>
    /// Create a new relationship between two accounts
    /// </summary>
    /// <param name="command">Relationship creation request</param>
    /// <returns>The created relationship ID</returns>
    /// <remarks>
    /// Sample request:
    ///
    ///     POST /v1/relationships
    ///     {
    ///        "parentAccountId": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
    ///        "childAccountId": "3fa85f64-5717-4562-b3fc-2c963f66afa7",
    ///        "relationType": 3
    ///     }
    ///
    /// RelationType values:
    /// - 0 = PARENT_OF
    /// - 1 = MANAGES
    /// - 2 = PARTNER_OF
    /// - 3 = GUARDIAN_OF (most common for parent-child relationships)
    /// - 4 = MANAGES_ORGANIZATION_STAFF
    /// </remarks>
    /// <response code="200">Returns the relationship ID</response>
    /// <response code="400">If the request is invalid</response>
    [HttpPost]
    [ProducesResponseType(typeof(object), 200)]
    [ProducesResponseType(400)]
    public async Task<IActionResult> Create([FromBody] CreateRelationshipCommand command)
    {
        var id = await _mediator.Send(command);
        return Ok(new Krizovka.Shared.AspNetCore.Responses.IdResponse(id));
    }

    [HttpGet("account/{accountId}")]
    public async Task<IActionResult> GetByAccount(Guid accountId)
    {
        var result = await _mediator.Send(new GetRelationshipsQuery(accountId));
        return Ok(result);
    }

    /// <summary>
    /// Delete relationship
    /// </summary>
    /// <summary>
    /// Delete relationship
    /// </summary>
    [HttpDelete("{relationshipId}")]
    public async Task<IActionResult> Delete(Guid relationshipId)
    {
        await _mediator.Send(new DeleteRelationshipCommand(relationshipId));
        return NoContent();
    }
}
