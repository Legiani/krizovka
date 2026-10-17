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
// Own empty database only. No demo users, external seeders, sidecars or audit service.
if (builder.Configuration.GetValue<bool>("Database:MigrateOnStartup"))
{
    await using var scope = app.Services.CreateAsyncScope();
    await scope.ServiceProvider.GetRequiredService<UsDbContext>().Database.MigrateAsync();
}
app.MapGet("/health/live", () => Results.Ok(new { status = "ok", service = "krizovka-users" }));
app.MapGet("/health/ready", async (UsDbContext db, CancellationToken ct) =>
    await db.Database.CanConnectAsync(ct)
        ? Results.Ok(new { status = "ready" })
        : Results.StatusCode(503));
app.MapControllers();
app.Run();
