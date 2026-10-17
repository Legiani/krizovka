using Krizovka.Users.Application.Core.Claims.Commands.CompleteClaim;
using Krizovka.Users.Application.Core.Claims.Commands.CreateClaim;
using Krizovka.Users.Application.Core.Claims.Commands.CancelClaim;
using Krizovka.Users.Application.Core.Claims.Queries.GetClaim;
using Microsoft.AspNetCore.Authorization;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace Krizovka.Users.Api.Controllers.Core;

[ApiController]
[Route("accounts")]
[Authorize]
public class ClaimsController : ControllerBase
{
    private readonly IMediator _mediator;

    public ClaimsController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpPost("{accountId}/claims")]
    public async Task<IActionResult> CreateClaim(Guid accountId, [FromBody] CreateClaimRequest request)
    {
        var claimId = await _mediator.Send(new CreateAccountClaimCommand(
            accountId,
            request.Email,
            request.ExpireHours ?? 48
        ));
        return Ok(new Krizovka.Shared.AspNetCore.Responses.IdResponse(claimId));
    }

    [HttpPost("claims/complete")]
    public async Task<IActionResult> CompleteClaim([FromBody] CompleteClaimRequest request)
    {
        await _mediator.Send(new CompleteAccountClaimCommand(request.ClaimToken, request.IdentityId));
        return Ok();
    }

    [HttpGet("claims/{claimId}")]
    public async Task<IActionResult> GetClaim(Guid claimId)
    {
        var claim = await _mediator.Send(new GetAccountClaimQuery(claimId));
        if (claim == null)
        {
            return NotFound();
        }
        return Ok(claim);
    }

    /// <summary>
    /// Cancel pending claim
    /// </summary>
    /// <summary>
    /// Cancel pending claim
    /// </summary>
    [HttpPost("claims/cancel")]
    public async Task<IActionResult> CancelClaim([FromBody] CancelClaimRequest request)
    {
        await _mediator.Send(new CancelAccountClaimCommand(request.ClaimId));
        return NoContent();
    }
}

public record CreateClaimRequest(string Email, int? ExpireHours);
public record CompleteClaimRequest(string ClaimToken, Guid IdentityId);
public record CancelClaimRequest(Guid ClaimId);
