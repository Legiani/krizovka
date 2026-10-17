using Krizovka.Users.Application.Core.Admin.Queries.SearchUsers;

using MediatR;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Krizovka.Shared.Authentication;

namespace Krizovka.Users.Api.Controllers.Core;

[ApiController]
[Route("admin/users")]
[Authorize(AuthenticationSchemes = "Backoffice")]
public class AdminUsersController : ControllerBase
{
    private readonly IMediator _mediator;

    public AdminUsersController(IMediator mediator)
    {
        _mediator = mediator;
    }

    /// <summary>
    /// Advanced user search for backoffice
    /// </summary>
    [HttpGet("search")]
    [RequirePermission("USERS", "READ")]
    [ProducesResponseType(typeof(SearchUsersResponse), 200)]
    [ProducesResponseType(400)]
    public async Task<IActionResult> SearchUsers(
        [FromQuery] string? email = null,
        [FromQuery] string? phone = null,
        [FromQuery] string? name = null,
        [FromQuery] string? status = null,
        [FromQuery] Guid? identityId = null,
        [FromQuery] string? systems = null,  // Comma-separated system IDs
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20)
    {
        if (pageSize > 100)
        {
            return Problem(detail: "Page size cannot exceed 100", statusCode: 400);
        }

        if (page < 1)
        {
            return Problem(detail: "Page must be greater than 0", statusCode: 400);
        }

        var systemList = string.IsNullOrEmpty(systems)
            ? null
            : systems.Split(',').Select(s => s.Trim()).ToList();

        var query = new SearchUsersQuery(
            email,
            phone,
            name,
            status,
            identityId,
            page,
            pageSize,
            systemList
        );

        var result = await _mediator.Send(query);
        return Ok(result);
    }

    /// <summary>
    /// Batch retrieve users by IDs
    /// </summary>
    [HttpPost("batch")]
    [RequirePermission("USERS", "READ")]
    [ProducesResponseType(typeof(Dictionary<Guid, string>), 200)]
    public async Task<IActionResult> GetUsersByIds([FromBody] List<Guid> ids)
    {
        var result = await _mediator.Send(new Krizovka.Users.Application.Core.Admin.Queries.GetUsersByIds.GetUsersByIdsQuery(ids));
        return Ok(result);
    }


    /// <summary>
    /// Update user Identity (Name, Email)
    /// </summary>
    [HttpPut("{identityId}")]
    [RequirePermission("USERS", "WRITE")]
    public async Task<IActionResult> UpdateUser(Guid identityId, [FromBody] UpdateUserRequest request)
    {
        await _mediator.Send(new Krizovka.Users.Application.Core.Identities.Commands.UpdateIdentity.UpdateIdentityCommand(
            identityId,
            request.Email,
            request.FirstName,
            request.LastName,
            request.Phone,
            request.DateOfBirth
        ));
        return NoContent();
    }
}

public record UpdateUserRequest(string Email, string FirstName, string LastName, string? Phone, string? DateOfBirth);
