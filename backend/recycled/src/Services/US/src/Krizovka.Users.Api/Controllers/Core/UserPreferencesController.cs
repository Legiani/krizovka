using Krizovka.Users.Infrastructure.UserPreferences.Commands;
using Krizovka.Users.Infrastructure.UserPreferences.DTOs;
using Krizovka.Users.Infrastructure.UserPreferences.Queries;
using Krizovka.Shared.Authentication;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Krizovka.Users.Api.Controllers.Core;

/// <summary>
/// User Preferences API — per-user per-city settings.
/// Called by the mobile app using authenticated current-user context.
///
/// GET  /preferences                    → all preferences for the user
/// GET  /preferences/{systemId}         → preference for a specific city
/// PUT  /preferences/{systemId}         → upsert preference for a city
/// DELETE /preferences/{systemId}       → remove preference for a city
/// </summary>
[Authorize]
[ApiController]
[Route("preferences")]
public class UserPreferencesController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly ICurrentUserService _currentUser;

    public UserPreferencesController(IMediator mediator, ICurrentUserService currentUser)
    {
        _mediator = mediator;
        _currentUser = currentUser;
    }

    /// <summary>
    /// Get all preferences for the authenticated user.
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var accountId = _currentUser.GetActingAccountId();
        if (accountId is null)
            return Unauthorized("Missing acting account context.");

        var result = await _mediator.Send(new GetUserPreferencesQuery(accountId.Value));
        return Ok(result);
    }

    /// <summary>
    /// Get preference for a specific city/system.
    /// </summary>
    [HttpGet("{systemId}")]
    public async Task<IActionResult> GetBySystem(Guid systemId)
    {
        var accountId = _currentUser.GetActingAccountId();
        if (accountId is null)
            return Unauthorized("Missing acting account context.");

        var result = await _mediator.Send(new GetUserPreferenceBySystemQuery(accountId.Value, systemId));
        if (result is null)
            return NotFound();

        return Ok(result);
    }

    /// <summary>
    /// Create or update preference for a specific city/system (upsert).
    /// </summary>
    [HttpPut("{systemId}")]
    public async Task<IActionResult> Upsert(Guid systemId, [FromBody] UpsertUserPreferenceRequest request)
    {
        var accountId = _currentUser.GetActingAccountId();
        if (accountId is null)
            return Unauthorized("Missing acting account context.");

        var result = await _mediator.Send(new UpsertUserPreferenceCommand(
            accountId.Value,
            systemId,
            request.Favorites,
            request.MenuLayout,
            request.NotificationSettings));

        return Ok(result);
    }

    /// <summary>
    /// Delete preference for a specific city/system.
    /// </summary>
    [HttpDelete("{systemId}")]
    public async Task<IActionResult> Delete(Guid systemId)
    {
        var accountId = _currentUser.GetActingAccountId();
        if (accountId is null)
            return Unauthorized("Missing acting account context.");

        var deleted = await _mediator.Send(new DeleteUserPreferenceCommand(accountId.Value, systemId));
        if (!deleted)
            return NotFound();

        return NoContent();
    }
}
