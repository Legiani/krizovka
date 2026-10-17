using Krizovka.Users.Application.Core.Backoffice.Queries.ListUsers;
using Krizovka.Users.Application.Core.Backoffice.Queries.GetBackofficeUser;
using Krizovka.Users.Application.Core.Backoffice.Commands.CreateBackofficeUser;
using Krizovka.Users.Application.Core.Backoffice.Commands.UpdateBackofficeUser;
using Krizovka.Users.Application.Core.Backoffice.Commands.DeleteBackofficeUser;
using Krizovka.Users.Application.Core.Backoffice.Commands.ResetBackofficeUserPassword;
using Krizovka.Shared.Authentication;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Krizovka.Users.Api.Controllers.Core;

[ApiController]
[Authorize(AuthenticationSchemes = "Backoffice")]
[Route("backoffice/users")]
public class BackofficeUsersController : ControllerBase
{
    private readonly IMediator _mediator;

    public BackofficeUsersController(IMediator mediator)
    {
        _mediator = mediator;
    }

    /// <summary>
    /// List all backoffice users (admins)
    /// </summary>
    [HttpGet]
    [RequirePermission("USERS", "READ")]
    [ProducesResponseType(typeof(List<BackofficeUserDto>), 200)]
    public async Task<IActionResult> List()
    {
        var result = await _mediator.Send(new ListBackofficeUsersQuery());
        return Ok(result);
    }

    /// <summary>
    /// Get backoffice user details
    /// </summary>
    [HttpGet("{id}")]
    [RequirePermission("USERS", "READ")]
    [ProducesResponseType(typeof(BackofficeUserDetailDto), 200)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> Get(Guid id)
    {
        var result = await _mediator.Send(new GetBackofficeUserQuery(id));
        if (result == null) return NotFound();
        return Ok(result);
    }

    /// <summary>
    /// Create a new backoffice user
    /// </summary>
    [HttpPost]
    [RequirePermission("USERS", "WRITE")]
    [ProducesResponseType(typeof(Guid), 201)]
    [ProducesResponseType(400)]
    public async Task<IActionResult> Create([FromBody] CreateBackofficeUserCommand command)
    {
        var id = await _mediator.Send(command);
        return CreatedAtAction(nameof(Get), new { id }, id);
    }

    /// <summary>
    /// Update a backoffice user
    /// </summary>
    [HttpPut("{id}")]
    [RequirePermission("USERS", "WRITE")]
    [ProducesResponseType(204)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateBackofficeUserCommand command)
    {
        if (id != command.Id) return BadRequest("ID mismatch");

        await _mediator.Send(command);
        return NoContent();
    }

    /// <summary>
    /// Delete a backoffice user. Cannot delete the last super admin.
    /// </summary>
    [HttpDelete("{id}")]
    [RequirePermission("USERS", "DELETE")]
    [ProducesResponseType(204)]
    [ProducesResponseType(404)]
    [ProducesResponseType(400)]
    public async Task<IActionResult> Delete(Guid id)
    {
        await _mediator.Send(new DeleteBackofficeUserCommand(id));
        return NoContent();
    }

    /// <summary>
    /// Reset password for a backoffice user.
    /// </summary>
    [HttpPost("{id}/reset-password")]
    [RequirePermission("USERS", "RESET_PASSWORD")]
    [ProducesResponseType(204)]
    [ProducesResponseType(404)]
    [ProducesResponseType(400)]
    public async Task<IActionResult> ResetPassword(Guid id, [FromBody] ResetPasswordRequest request)
    {
        await _mediator.Send(new ResetBackofficeUserPasswordCommand(id, request.NewPassword));
        return NoContent();
    }
}

public record ResetPasswordRequest(string NewPassword);
