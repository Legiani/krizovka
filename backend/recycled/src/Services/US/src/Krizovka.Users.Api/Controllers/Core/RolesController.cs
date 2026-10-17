using Microsoft.AspNetCore.Authorization;
using Krizovka.Users.Application.Core.Roles.Commands.AssignRole;
using Krizovka.Users.Application.Core.Roles.Commands.RevokeRole;
using Krizovka.Users.Application.Core.Roles.Queries.GetRoleAssignments;
using Krizovka.Users.Application.Core.Roles.Queries.GetRoleCatalog;
using Krizovka.Users.Application.Core.Roles.Queries.GetSystemRoles;
using Krizovka.Shared.Authentication;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace Krizovka.Users.Api.Controllers.Core;

[ApiController]
[Route("roles")]
[Authorize(AuthenticationSchemes = "Backoffice")]
public class RolesController : ControllerBase
{
    private readonly IMediator _mediator;

    public RolesController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpPost("assignments")]
    [RequirePermission("ROLES", "WRITE")]
    public async Task<IActionResult> AssignRole([FromBody] AssignRoleCommand command)
    {
        var id = await _mediator.Send(command);
        return Ok(new Krizovka.Shared.AspNetCore.Responses.IdResponse(id));
    }

    [HttpGet("assignments")]
    [RequirePermission("ROLES", "READ")]
    public async Task<IActionResult> GetAssignments([FromQuery] Guid accountId)
    {
        var result = await _mediator.Send(new GetRoleAssignmentsQuery(accountId));
        return Ok(result);
    }

    [HttpGet("catalog")]
    [RequirePermission("ROLES", "READ")]
    public async Task<IActionResult> GetCatalog()
    {
        var result = await _mediator.Send(new GetRoleCatalogQuery());
        return Ok(result);
    }

    /// <summary>
    /// Revoke role assignment
    /// </summary>
    /// <summary>
    /// Revoke role assignment
    /// </summary>
    [HttpDelete("assignments/{assignmentId}")]
    [RequirePermission("ROLES", "DELETE")]
    public async Task<IActionResult> RevokeRole(Guid assignmentId)
    {
        await _mediator.Send(new RevokeRoleCommand(assignmentId));
        return NoContent();
    }
}

/// <summary>
/// Internal S2S controller for Gateway claims enrichment.
/// Protected by InternalApiKey – must NOT be exposed via public ingress.
/// </summary>
[ApiController]
[Route("internal/accounts")]
[ApiKeyAuthorize]
public class InternalSystemRolesController : ControllerBase
{
    private readonly IMediator _mediator;

    public InternalSystemRolesController(IMediator mediator)
    {
        _mediator = mediator;
    }

    /// <summary>
    /// Returns system-scoped role assignments for a given account.
    /// Used by the Gateway ClaimsMiddleware to inject X-System-Roles header.
    /// </summary>
    /// <param name="accountId">Account UUID</param>
    /// <param name="scopeId">Optional: IDS/PROVIDER/CARRIER system_id to filter by</param>
    [HttpGet("{accountId}/system-roles")]
    [ProducesResponseType(typeof(IEnumerable<object>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetSystemRoles(
        Guid accountId,
        [FromQuery] string? scopeId = null)
    {
        var roles = await _mediator.Send(new GetSystemRolesQuery(accountId, scopeId));

        var result = roles.Select(r => new
        {
            r.AssignmentId,
            r.RoleCode,
            ScopeType = r.ScopeType.ToString(),
            r.ScopeId,
            r.ValidFrom,
            r.ValidTo
        });

        return Ok(result);
    }
}
