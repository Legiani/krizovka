using Krizovka.Users.Application.Core.Interfaces.Persistence;
using Krizovka.Users.Application.Core.Interfaces.Services;
using Krizovka.Users.Application.Core.Interfaces.Authentication;
using Krizovka.Users.Infrastructure.Authentication;
using Krizovka.Users.Infrastructure.Persistence;
using Krizovka.Users.Infrastructure.Persistence.Repositories;
using Krizovka.Users.Infrastructure.Services;
using Krizovka.Shared.Authentication;
using Krizovka.Shared.Messaging;
using MassTransit;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.AspNetCore.Authentication.JwtBearer;

namespace Krizovka.Users.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        // Database
        var connectionString = Environment.GetEnvironmentVariable("DB_CONNECTION_STRING")
            ?? configuration.GetConnectionString("DefaultConnection");

        if (string.Equals(Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT"), "Production", StringComparison.OrdinalIgnoreCase))
        {
            if (string.IsNullOrEmpty(connectionString))
                throw new InvalidOperationException("A valid DB_CONNECTION_STRING or DefaultConnection must be provided in Production.");
        }
        else
        {
            connectionString ??= "Host=localhost;Database=krizovka_users"; // Fallback for build/swagger generation
        }

        services.AddDbContext<UsDbContext>(options =>
            options.UseNpgsql(connectionString,
                x => x.MigrationsAssembly(typeof(UsDbContext).Assembly.FullName))
                   .UseSnakeCaseNamingConvention()
                   .ConfigureWarnings(w => w.Ignore(Microsoft.EntityFrameworkCore.Diagnostics.RelationalEventId.PendingModelChangesWarning)));

        // MediatR (Register handlers in Infrastructure layer)
        services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(typeof(DependencyInjection).Assembly));

        // Repositories
        services.AddScoped<IUserAccountRepository, UserAccountRepository>();
        // Authentication
        // For US, we use the internal client that calls MediatR directly to avoid loops
        services.AddScoped<IUsAuthClient, InternalUsAuthClient>();

        services.AddScoped<IRelationshipRepository, RelationshipRepository>();
        services.AddScoped<IRoleAssignmentRepository, RoleAssignmentRepository>();
        services.AddScoped<IAccountClaimRepository, AccountClaimRepository>();
        services.AddScoped<IBackofficeUserRepository, BackofficeUserRepository>();
        services.AddScoped<IBackofficeRoleRepository, BackofficeRoleRepository>();

        // Services
        services.AddScoped<IClaimPolicyService, ClaimPolicyService>();
        services.AddScoped<IBackofficeTokenService, BackofficeTokenService>();
        services.AddSingleton<IRoleCatalogService, RoleCatalogService>();
        services.AddScoped<IBackofficePermissionChecker, BackofficePermissionChecker>();

        // MassTransit
        services.AddSharedMessaging(configuration, x =>
        {
            x.SetEndpointNameFormatter(new KebabCaseEndpointNameFormatter("us", false));
            x.AddConsumer<Krizovka.Users.Infrastructure.Consumers.UserProfileUpdatedConsumer>();
        });

        // Register no-op IPublishEndpoint for development (MassTransit disabled) - REMOVED as we are enabling it
        // If we still want a fallback, MassTransit provides unit test helpers, but for now we enforce usage.

        return services;
    }
}
