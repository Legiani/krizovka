using Krizovka.Users.Application.Core.Backoffice.Queries.GetCurrentUser;
using Krizovka.Users.Application.Core.Backoffice.Queries.CheckPermission;
using Krizovka.Users.Application.Core.Backoffice.Queries.GetAccessibleSystems;
using Krizovka.Users.Application.Core.Backoffice.Commands.Login;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Krizovka.Shared.Authentication;

namespace Krizovka.Users.Api.Controllers.Core;

[Authorize(AuthenticationSchemes = "Backoffice")]
[ApiController]
[Route("backoffice/auth")]
public class BackofficeAuthController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly ICurrentUserService _currentUserService;
    private readonly IBackofficeAccessibleSystemsService _accessibleSystems;
    private readonly bool _isSecureCookie;

    public BackofficeAuthController(
        IMediator mediator,
        ICurrentUserService currentUserService,
        IBackofficeAccessibleSystemsService accessibleSystems,
        IWebHostEnvironment env)
    {
        _mediator = mediator;
        _currentUserService = currentUserService;
        _accessibleSystems = accessibleSystems;
        // Secure=true requires HTTPS — disable in Development to allow HTTP (localhost)
        _isSecureCookie = !env.IsDevelopment();
    }

    /// <summary>
    /// Get current backoffice user profile with permissions
    /// </summary>
    /// <remarks>
    /// Returns the authenticated user's profile, role, and all permissions.
    /// Used by the frontend to determine what UI elements to show/hide.
    ///
    /// **Example Response:**
    ///
    ///     {
    ///       "userId": "guid",
    ///       "identityId": "guid",
    ///       "isSuperAdmin": false,
    ///       "role": {
    ///         "roleId": "guid",
    ///         "name": "ROPID Admin",
    ///         "code": "ROPID_ADMIN"
    ///       },
    ///       "permissions": [
    ///         {
    ///           "module": "USERS",
    ///           "action": "*",
    ///           "scopeType": "SYSTEM",
    ///           "scopeValue": "ROPID"
    ///         }
    ///       ]
    ///     }
    ///   ]
    /// }
    /// </remarks>
    [HttpPost("login")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(LoginResponse), 200)]
    public async Task<IActionResult> Login([FromBody] BackofficeLoginCommand command)
    {
        var result = await _mediator.Send(command);

        if (!result.Success || result.Token == null)
        {
            return Problem(detail: result.ErrorMessage, statusCode: 401);
        }

        var cookieOptions = new CookieOptions
        {
            HttpOnly = true,
            Secure = _isSecureCookie,
            SameSite = SameSiteMode.Lax,
            Expires = DateTime.UtcNow.AddHours(24)
        };
        Response.Cookies.Append("admin_session", result.Token, cookieOptions);

        return Ok(result);
    }

    [HttpPost("logout")]
    [AllowAnonymous]
    public IActionResult Logout()
    {
        var cookieOptions = new CookieOptions
        {
            HttpOnly = true,
            Secure = _isSecureCookie,
            SameSite = SameSiteMode.Lax,
            Expires = DateTime.UtcNow.AddDays(-1)
        };
        Response.Cookies.Append("admin_session", "", cookieOptions);

        return Ok(new Krizovka.Shared.AspNetCore.Responses.MessageResponse("Logged out successfully"));
    }

    /// <summary>
    /// Get current backoffice user profile with permissions
    /// </summary>
    [HttpGet("current-user")]
    [ProducesResponseType(typeof(GetCurrentUserResponse), 200)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> GetCurrentUser()
    {
        var identityId = _currentUserService.GetRequiredIdentityId();
        var query = new GetCurrentUserQuery(identityId);
        var result = await _mediator.Send(query);

        if (result == null)
        {
            return Problem(detail: "Backoffice user not found", statusCode: 404);
        }

        return Ok(result);
    }

    /// <summary>
    /// Check if user has specific permission
    /// </summary>
    /// <remarks>
    /// Checks if the current user has permission to perform an action.
    /// Supports wildcard matching and owner-scope checks across CITY / IDS / PROVIDER / CARRIER / ORGANIZATION scopes.
    /// The legacy SystemId field remains only for backwards compatibility.
    ///
    /// **Example Request:**
    ///
    ///     POST /v1/backoffice/auth/check-permission
    ///     {
    ///       "identityId": "guid",
    ///       "module": "ORDERS",
    ///       "action": "WRITE",
    ///       "scopeType": "IDS",
    ///       "scopeId": "ROPID"
    ///     }
    ///
    /// **Example Response:**
    ///
    ///     {
    ///       "allowed": true
    ///     }
    /// </remarks>
    [HttpPost("check-permission")]
    [ApiKeyAuthorize]
    [ProducesResponseType(typeof(CheckPermissionResponse), 200)]
    public async Task<IActionResult> CheckPermission([FromBody] CheckPermissionRequest request)
    {
        Guid identityGuid;
        if (!string.IsNullOrEmpty(request.IdentityId) && Guid.TryParse(request.IdentityId, out var parsedGuid))
        {
            identityGuid = parsedGuid;
        }
        else
        {
            identityGuid = _currentUserService.GetRequiredIdentityId();
        }

        var scopeType = request.ScopeType;
        var scopeId = request.ScopeId;

        if (string.IsNullOrWhiteSpace(scopeType) &&
            string.IsNullOrWhiteSpace(scopeId) &&
            !string.IsNullOrWhiteSpace(request.CityId))
        {
            scopeType = "CITY";
            scopeId = request.CityId;
        }

        var query = new CheckPermissionQuery(
            identityGuid,
            request.Module,
            request.Action,
            scopeType,
            scopeId,
            request.SystemId
        );

        var result = await _mediator.Send(query);
        return Ok(result);
    }

    /// <summary>
    /// Get list of systems user can access
    /// </summary>
    /// <remarks>
    /// Returns all systems the user has permission to access.
    /// If hasGlobalAccess is true, user can access all systems.
    ///
    /// **Example Response:**
    ///
    ///     {
    ///       "systems": ["ROPID", "PID"],
    ///       "hasGlobalAccess": false
    ///     }
    /// </remarks>
    [HttpGet("accessible-systems")]
    [ProducesResponseType(typeof(GetAccessibleSystemsResponse), 200)]
    public async Task<IActionResult> GetAccessibleSystems(CancellationToken ct)
    {
        var result = await _accessibleSystems.GetAccessibleSystemsAsync(ct);
        return Ok(new GetAccessibleSystemsResponse(result.SystemIds.ToList(), result.HasGlobalAccess));
    }
}

public record CheckPermissionRequest(
    string? IdentityId,
    string Module,
    string Action,
    string? ScopeType = null,
    string? ScopeId = null,
    string? SystemId = null,
    string? CityId = null
);
