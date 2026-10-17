using Krizovka.Users.Api.Controllers.Core;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace Krizovka.Users.Api.Tests;

public sealed class WebhookSecurityTests
{
    [Theory]
    [InlineData(null, null)]
    [InlineData(null, "attacker-supplied")]
    [InlineData("expected-secret", null)]
    [InlineData("expected-secret", "wrong-secret")]
    public async Task Both_webhooks_reject_missing_or_invalid_configuration(string? configured, string? supplied)
    {
        var configuration = new ConfigurationBuilder().AddInMemoryCollection(
            new Dictionary<string, string?> { ["WEBHOOK_SECRET"] = configured }).Build();
        // No mediator should be called when authentication fails.
        var controller = new WebhooksController(null!, configuration, NullLogger<WebhooksController>.Instance)
        {
            ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext() }
        };
        if (supplied != null) controller.Request.Headers["X-Hook-Signature"] = supplied;
        Assert.IsType<UnauthorizedResult>(await controller.RegistrationHook(new RegistrationWebhookRequest()));
        Assert.IsType<UnauthorizedResult>(await controller.IdentityLinked(new IdentityLinkedWebhookRequest(Guid.NewGuid(), Guid.NewGuid())));
    }
}
