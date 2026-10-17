using Krizovka.Users.Application.Core.Identities.Commands.LinkIdentity;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Krizovka.Users.Api.Controllers.Core;

[ApiController]
[Route("webhooks")]
[AllowAnonymous]
public class WebhooksController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly IConfiguration _configuration;
    private readonly ILogger<WebhooksController> _logger;

    public WebhooksController(IMediator mediator, IConfiguration configuration, ILogger<WebhooksController> logger)
    {
        _mediator = mediator;
        _configuration = configuration;
        _logger = logger;
    }

    /// <summary>
    /// Webhook called after successful user registration (identity provider → US).
    /// </summary>
    [HttpPost("identity/registration")]
    public async Task<IActionResult> RegistrationHook([FromBody] RegistrationWebhookRequest request)
    {
        if (!VerifySignature())
            return Unauthorized();

        if (request?.Identity == null)
            return BadRequest("Invalid payload");

        var identityId = request.Identity.Id;
        var regSystemId = request.Identity.Traits?.RegistrationSystemId;
        var email = request.Identity.Traits?.Email;
        var firstName = request.Identity.Traits?.Name?.First;
        var lastName = request.Identity.Traits?.Name?.Last;

        await _mediator.Send(new Krizovka.Users.Application.Core.Identities.Commands.CreateAccountFromIdentity.CreateAccountFromIdentityCommand(identityId, regSystemId, email, firstName, lastName));

        return Ok();
    }

    /// <summary>
    /// Webhook called after identity link (SSO / account merge).
    /// </summary>
    [HttpPost("identity/linked")]
    public async Task<IActionResult> IdentityLinked([FromBody] IdentityLinkedWebhookRequest request)
    {
        if (!VerifySignature())
            return Unauthorized();

        if (request.IdentityId != Guid.Empty && request.AccountId != Guid.Empty)
        {
            await _mediator.Send(new LinkIdentityCommand(request.IdentityId, request.AccountId));
        }
        return Ok();
    }

    /// <summary>
    /// Verifies the webhook signature using WEBHOOK_SECRET env variable.
    /// Compares the X-Hook-Signature header value against the configured secret.
    /// For production deployments, consider a middleware-based HMAC-SHA256 body verification.
    /// </summary>
    private bool VerifySignature()
    {
        var secret = _configuration["WEBHOOK_SECRET"];
        if (string.IsNullOrEmpty(secret))
        {
            _logger.LogWarning("WEBHOOK_SECRET is not configured. Webhook access denied.");
            return false;
        }

        if (!Request.Headers.TryGetValue("X-Hook-Signature", out var signatureHeader))
        {
            _logger.LogWarning("Missing X-Hook-Signature header.");
            return false;
        }

        return System.Security.Cryptography.CryptographicOperations.FixedTimeEquals(
            System.Text.Encoding.UTF8.GetBytes(signatureHeader.ToString()),
            System.Text.Encoding.UTF8.GetBytes(secret));
    }
}

public record IdentityLinkedWebhookRequest(Guid IdentityId, Guid AccountId);

public class RegistrationWebhookRequest
{
    [System.Text.Json.Serialization.JsonPropertyName("identity")]
    public IdentityWebhookDto? Identity { get; set; }
}

public class IdentityWebhookDto
{
    [System.Text.Json.Serialization.JsonPropertyName("id")]
    public Guid Id { get; set; }

    [System.Text.Json.Serialization.JsonPropertyName("traits")]
    public IdentityTraitsDto? Traits { get; set; }
}

public class IdentityTraitsDto
{
    [System.Text.Json.Serialization.JsonPropertyName("email")]
    public string? Email { get; set; }

    [System.Text.Json.Serialization.JsonPropertyName("name")]
    public IdentityNameDto? Name { get; set; }

    [System.Text.Json.Serialization.JsonPropertyName("registration_system_id")]
    public string? RegistrationSystemId { get; set; }
}

public class IdentityNameDto
{
    [System.Text.Json.Serialization.JsonPropertyName("first")]
    public string? First { get; set; }

    [System.Text.Json.Serialization.JsonPropertyName("last")]
    public string? Last { get; set; }
}
