using Krizovka.Users.Application.Core.Context.Queries.BatchValidateContext;
using Krizovka.Users.Application.Core.Context.Queries.ValidateContext;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

using Krizovka.Shared.Authentication;

namespace Krizovka.Users.Api.Controllers.Core;

[ApiKeyAuthorize]
[ApiController]
[Route("context")]
public class ContextController : ControllerBase
{
    private readonly IMediator _mediator;

    public ContextController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpGet("validate")]
    public async Task<IActionResult> Validate([FromQuery] Guid aid, [FromQuery] string operation)
    {
        var result = await _mediator.Send(new ValidateContextQuery(aid, operation));

        if (!result.Allowed)
        {
            return Problem(detail: "Context validation failed: access denied", statusCode: StatusCodes.Status403Forbidden);
        }

        return Ok(result);
    }

    [HttpPost("validate")]
    public async Task<IActionResult> ValidateToken([FromBody] ValidateTokenRequest request)
    {
        var result = await _mediator.Send(new ValidateTokenQuery(request.Token));

        // Always return 200 — callers (UsAuthClient) check IsValid/Allowed in the body.
        // Returning HTTP 401 here causes UsAuthClient to treat it as a connection error
        // rather than reading the validation result properly.
        return Ok(result);
    }

    /// <summary>
    /// Batch context validation for BFF
    /// </summary>
    [HttpPost("check-batch")]
    public async Task<IActionResult> CheckBatch([FromBody] BatchValidationRequest request)
    {
        var checks = request.Checks.Select(c => new Krizovka.Users.Application.Core.Context.Queries.BatchValidateContext.ValidationCheck(c.Aid, c.Operation)).ToList();
        var results = await _mediator.Send(new BatchValidateContextQuery(checks));
        return Ok(results);
    }
}

public record ValidateTokenRequest(string Token);

public record BatchValidationRequest(List<ValidationCheck> Checks);
public record ValidationCheck(Guid Aid, string Operation);
public record ValidationResult(Guid Aid, string Operation, bool Allowed, string? Scope);
