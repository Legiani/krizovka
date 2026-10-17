using Krizovka.Api.Data;
using Krizovka.Shared.Authentication;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);
var connection = builder.Configuration.GetConnectionString("DefaultConnection")
    ?? throw new InvalidOperationException("ConnectionStrings__DefaultConnection is required.");
builder.Services.AddDbContext<KrizovkaDbContext>(options => options.UseNpgsql(connection));
builder.Services.AddMestoPlusAuthentication(options =>
{
    options.UsServiceUrl = builder.Configuration["US_URL"]
        ?? throw new InvalidOperationException("US_URL is required.");
});
builder.Services.AddAuthorization();
builder.Services.AddProblemDetails();
var app = builder.Build();
if (builder.Configuration.GetValue<bool>("Database:MigrateOnStartup"))
{
    await using var scope = app.Services.CreateAsyncScope();
    await scope.ServiceProvider.GetRequiredService<KrizovkaDbContext>().Database.MigrateAsync();
}
app.UseExceptionHandler();
app.UseAuthentication();
app.UseAuthorization();
app.MapGet("/health/live", () => Results.Ok(new { status = "ok", service = "krizovka-api" }));
app.MapGet("/health/ready", async (KrizovkaDbContext db, CancellationToken ct) =>
    await db.Database.CanConnectAsync(ct)
        ? Results.Ok(new { status = "ready" })
        : Results.StatusCode(503));
app.MapGet("/api/session", (HttpContext context) => Results.Ok(new
{
    accountId = context.User.FindFirst("AccountId")?.Value,
    message = "Identity integration is available. Report endpoints are not implemented yet."
})).RequireAuthorization();
app.Run();
