using MediatR;
using Krizovka.Shared.Authentication;
using Krizovka.Users.Application.Core.Context.Queries.ValidateContext;
using Microsoft.Extensions.DependencyInjection;

namespace Krizovka.Users.Infrastructure.Authentication;

/// <summary>
/// Implementation of IUsAuthClient that runs directly within the User Service (US) process.
/// Instead of calling US via HTTP, it dispatches the ValidateTokenQuery using MediatR.
/// This allows US to use the same MestoPlusAuthenticationHandler as other services without
/// creating an infinite HTTP loop.
/// </summary>
public class InternalUsAuthClient : IUsAuthClient
{
    private readonly IServiceProvider _serviceProvider;

    public InternalUsAuthClient(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }

    public async Task<ValidationResult?> ValidateContextAsync(string token, CancellationToken cancellationToken = default)
    {
        // Use a nested scope to resolve IMediator to avoid potential lifetime issues
        using var scope = _serviceProvider.CreateScope();
        var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();

        var query = new ValidateTokenQuery(token);
        var internalResult = await mediator.Send(query, cancellationToken);

        if (internalResult == null || !internalResult.IsValid)
        {
            return null;
        }

        // Map US-internal result to Shared ValidationResult
        return new ValidationResult(
            internalResult.AccountId,
            internalResult.IdentityId,
            internalResult.Claims,
            internalResult.Roles,
            internalResult.Scope?.ScopeType,
            internalResult.Scope?.ScopeId,
            internalResult.CityAssignments?.Select(a => new Krizovka.Shared.Authentication.CityAssignment(a.CityId, a.RoleCode)).ToList(),
            internalResult.ActingAccountId,
            internalResult.IsBackoffice,
            internalResult.ScopeAssignments?.Select(a => new Krizovka.Shared.Authentication.ScopeAssignment(a.ScopeType, a.ScopeId, a.RoleCode)).ToList(),
            internalResult.OrganizationId
        );
    }
}
