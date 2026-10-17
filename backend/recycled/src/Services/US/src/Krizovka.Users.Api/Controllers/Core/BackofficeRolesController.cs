using Krizovka.Users.Application.Core.Backoffice.Queries.GetRoles;
using Krizovka.Users.Application.Core.Backoffice.Queries.GetRoleUsers;
using Krizovka.Users.Application.Core.Backoffice.Commands.CreateRole;
using Krizovka.Users.Application.Core.Backoffice.Commands.AddPermissionToRole;
using Krizovka.Users.Application.Core.Backoffice.Commands.RemovePermission;
using Krizovka.Users.Application.Core.Backoffice.Commands.DeleteRole;
using Krizovka.Shared.Authentication;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;

namespace Krizovka.Users.Api.Controllers.Core;

[Authorize(AuthenticationSchemes = "Backoffice")]
[ApiController]
[Route("backoffice/roles")]
public class BackofficeRolesController : ControllerBase
{
    private readonly IMediator _mediator;

    public BackofficeRolesController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpGet]
    [RequirePermission("ROLES", "READ")]
    [ProducesResponseType(typeof(GetRolesResponse), 200)]
    public async Task<IActionResult> GetRoles()
    {
        var query = new GetRolesQuery();
        var result = await _mediator.Send(query);
        return Ok(result);
    }

    [HttpGet("{roleId}/users")]
    [RequirePermission("ROLES", "READ")]
    [ProducesResponseType(typeof(GetRoleUsersResponse), 200)]
    public async Task<IActionResult> GetRoleUsers(Guid roleId)
    {
        var query = new GetRoleUsersQuery(roleId);
        var result = await _mediator.Send(query);
        return Ok(result);
    }

    [HttpPost]
    [RequirePermission("ROLES", "WRITE")]
    [ProducesResponseType(typeof(CreateRoleResponse), 200)]
    public async Task<IActionResult> CreateRole([FromBody] CreateRoleRequest request)
    {
        var command = new CreateRoleCommand(request.Name, request.Code, request.Description);
        var result = await _mediator.Send(command);
        return Ok(result);
    }

    [HttpPost("{roleId}/permissions")]
    [RequirePermission("ROLES", "WRITE")]
    [ProducesResponseType(typeof(AddPermissionToRoleResponse), 200)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> AddPermission(Guid roleId, [FromBody] AddPermissionRequest request)
    {
        var command = new AddPermissionToRoleCommand(
            roleId, request.Module, request.Action, request.ScopeType, request.ScopeValue);
        var result = await _mediator.Send(command);

        if (!result.Success)
            return Problem(detail: result.Error, statusCode: 404);

        return Ok(result);
    }

    [HttpDelete("permissions/{permissionId}")]
    [RequirePermission("ROLES", "DELETE")]
    [ProducesResponseType(typeof(RemovePermissionResponse), 200)]
    public async Task<IActionResult> RemovePermission(Guid permissionId)
    {
        var command = new RemovePermissionCommand(permissionId);
        var result = await _mediator.Send(command);

        if (!result.Success)
            return Problem(detail: result.Error, statusCode: 404);

        return Ok(result);
    }

    [HttpDelete("{roleId}")]
    [RequirePermission("ROLES", "DELETE")]
    [ProducesResponseType(200)]
    [ProducesResponseType(400)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> DeleteRole(Guid roleId)
    {
        var command = new DeleteRoleCommand(roleId);
        var result = await _mediator.Send(command);

        if (!result.Success)
            return result.IsSystemRole
                ? Problem(detail: result.Error, statusCode: 400)
                : Problem(detail: result.Error, statusCode: 404);

        return Ok(new Krizovka.Shared.AspNetCore.Responses.MessageResponse("Role deleted"));
    }
}  // end of BackofficeRolesController

public record CreateRoleRequest(string Name, string Code, string? Description);
public record AddPermissionRequest(string Module, string Action, string ScopeType, string ScopeValue);
