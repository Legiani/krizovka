using Krizovka.Shared.AspNetCore;
using Krizovka.Shared.Authentication;
using Krizovka.Shared.ErrorHandling;
using Krizovka.Users.Application.Core;
using Krizovka.Users.Infrastructure;
using Krizovka.Users.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);
var signingKey = builder.Configuration["BackofficeAuth:GlobalSecret"];
if (string.IsNullOrWhiteSpace(signingKey) || signingKey.Length < 32)
    throw new InvalidOperationException("BackofficeAuth__GlobalSecret must contain at least 32 characters.");
builder.Services.AddKosControllers("v1/us");
builder.Services.AddMestoPlusErrorHandling();
builder.Services.AddMestoPlusAuthentication(o => o.UsServiceUrl = "http://krizovka-users:8080");
builder.Services.AddBackofficeScheme();
builder.Services.AddAuthorization();
builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);
var app = builder.Build();
app.UseMestoPlusErrorHandling();
app.UseAuthentication();
app.UseAuthorization();
// Own empty database only. Seed default operator and admin for local testing if missing.
if (builder.Configuration.GetValue<bool>("Database:MigrateOnStartup"))
{
    await using var scope = app.Services.CreateAsyncScope();
    var db = scope.ServiceProvider.GetRequiredService<UsDbContext>();
    await db.Database.MigrateAsync();

    var operatorRole = await db.BackofficeRoles.FirstOrDefaultAsync(r => r.Code == "OPERATOR");
    if (operatorRole == null)
    {
        operatorRole = new Krizovka.Users.Domain.Entities.BackofficeRole
        {
            Id = Guid.NewGuid(),
            Name = "Operátor velínu",
            Code = "OPERATOR",
            Description = "Dispečer krizového velínu"
        };
        db.BackofficeRoles.Add(operatorRole);
        await db.SaveChangesAsync();
    }

    var superAdminRole = await db.BackofficeRoles.FirstOrDefaultAsync(r => r.Code == "SUPER_ADMIN");
    if (superAdminRole == null)
    {
        superAdminRole = new Krizovka.Users.Domain.Entities.BackofficeRole
        {
            Id = Guid.NewGuid(),
            Name = "Super Administrátor",
            Code = "SUPER_ADMIN",
            Description = "Globální administrátor systému"
        };
        db.BackofficeRoles.Add(superAdminRole);
        await db.SaveChangesAsync();
    }

    var hasOperatorUserPerms = await db.BackofficePermissions.AnyAsync(p => p.RoleId == operatorRole.Id && p.Module == "USERS");
    if (!hasOperatorUserPerms)
    {
        db.BackofficePermissions.AddRange(
            new Krizovka.Users.Domain.Entities.BackofficePermission { Id = Guid.NewGuid(), RoleId = operatorRole.Id, Module = "USERS", Action = "*", ScopeType = "GLOBAL", ScopeValue = "*" },
            new Krizovka.Users.Domain.Entities.BackofficePermission { Id = Guid.NewGuid(), RoleId = operatorRole.Id, Module = "ROLES", Action = "*", ScopeType = "GLOBAL", ScopeValue = "*" }
        );
        await db.SaveChangesAsync();
    }

    var operatorUser = await db.BackofficeUsers.FirstOrDefaultAsync(u => u.Email == "operator@krizovka.cz");
    if (operatorUser == null)
    {
        operatorUser = new Krizovka.Users.Domain.Entities.BackofficeUser
        {
            Id = Guid.NewGuid(),
            IdentityId = Guid.NewGuid(),
            Email = "operator@krizovka.cz",
            FirstName = "Dispečer",
            LastName = "Krizovky",
            IsActive = true,
            IsSuperAdmin = false,
            RoleId = operatorRole.Id,
            OrganizationId = Guid.Parse("11111111-1111-1111-1111-111111111111"), // Kampus Sever
            PasswordHash = BCrypt.Net.BCrypt.HashPassword("Operator123!")
        };
        db.BackofficeUsers.Add(operatorUser);
        await db.SaveChangesAsync();
    }

    var superAdminUser = await db.BackofficeUsers.FirstOrDefaultAsync(u => u.Email == "superadmin@krizovka.cz");
    if (superAdminUser == null)
    {
        superAdminUser = new Krizovka.Users.Domain.Entities.BackofficeUser
        {
            Id = Guid.NewGuid(),
            IdentityId = Guid.NewGuid(),
            Email = "superadmin@krizovka.cz",
            FirstName = "Super",
            LastName = "Admin",
            IsActive = true,
            IsSuperAdmin = true,
            RoleId = superAdminRole.Id,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword("SuperAdmin123!")
        };
        db.BackofficeUsers.Add(superAdminUser);
        await db.SaveChangesAsync();
    }
}
app.MapGet("/health/live", () => Results.Ok(new { status = "ok", service = "krizovka-users" }));
app.MapGet("/health/ready", async (UsDbContext db, CancellationToken ct) =>
    await db.Database.CanConnectAsync(ct)
        ? Results.Ok(new { status = "ready" })
        : Results.StatusCode(503));
app.MapControllers();
app.Run();
