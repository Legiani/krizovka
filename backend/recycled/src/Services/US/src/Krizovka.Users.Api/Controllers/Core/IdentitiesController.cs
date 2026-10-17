using Microsoft.AspNetCore.Authorization;
using Krizovka.Users.Application.Core.Identities.Commands.LinkIdentity;
using Krizovka.Shared.Authentication;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace Krizovka.Users.Api.Controllers.Core;

[ApiController]
[Route("identities")]
[Authorize(AuthenticationSchemes = "Backoffice")]
public class IdentitiesController : ControllerBase
{
    private readonly IMediator _mediator;

    public IdentitiesController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpPost("{identityId}/link")]
    [RequirePermission("USERS", "WRITE")]
    public async Task<IActionResult> LinkIdentity(Guid identityId, [FromBody] LinkIdentityRequest request)
    {
        await _mediator.Send(new LinkIdentityCommand(identityId, request.AccountId));
        return Ok();
    }
}

public record LinkIdentityRequest(Guid AccountId);
