using Krizovka.Api.Data;
using Krizovka.Api.Guides;
using Krizovka.Api.Organizations;
using Krizovka.Api.Reports;
using Krizovka.Shared.Authentication;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);
var connection = builder.Configuration.GetConnectionString("DefaultConnection")
    ?? "Data Source=krizovka.dev.db";

if (connection.StartsWith("Data Source=", StringComparison.OrdinalIgnoreCase) || connection.EndsWith(".db", StringComparison.OrdinalIgnoreCase))
{
    builder.Services.AddDbContext<KrizovkaDbContext>(options => options.UseSqlite(connection));
}
else
{
    builder.Services.AddDbContext<KrizovkaDbContext>(options => options.UseNpgsql(connection));
}

builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        policy.SetIsOriginAllowed(_ => true)
              .AllowAnyMethod()
              .AllowAnyHeader()
              .AllowCredentials();
    });
});

builder.Configuration["INTERNAL_API_KEY"] ??= "dev-internal-key-for-local-testing";
builder.Services.AddMestoPlusAuthentication(options =>
{
    options.UsServiceUrl = builder.Configuration["US_URL"] ?? "http://127.0.0.1:5081";
});
builder.Services.Configure<ForwardedHeadersOptions>(options =>
{
    options.ForwardedHeaders = Microsoft.AspNetCore.HttpOverrides.ForwardedHeaders.XForwardedFor | Microsoft.AspNetCore.HttpOverrides.ForwardedHeaders.XForwardedProto;
    options.KnownIPNetworks.Clear();
    options.KnownProxies.Clear();
});

builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("OperatorPolicy", policy =>
        policy.RequireAuthenticatedUser());
    options.AddPolicy("AdminPolicy", policy =>
        policy.RequireAuthenticatedUser());
});
builder.Services.AddProblemDetails();
builder.Services.ConfigureHttpJsonOptions(options =>
{
    options.SerializerOptions.Converters.Add(new System.Text.Json.Serialization.JsonStringEnumConverter());
});
builder.Services.AddSingleton<IAttachmentStorage, FileSystemAttachmentStorage>();
var app = builder.Build();

app.UseForwardedHeaders();

if (builder.Configuration.GetValue<bool>("Database:MigrateOnStartup") || connection.Contains(".db"))
{
    await using var scope = app.Services.CreateAsyncScope();
    var db = scope.ServiceProvider.GetRequiredService<KrizovkaDbContext>();
    if (db.Database.IsSqlite())
    {
        await db.Database.EnsureCreatedAsync();
    }
    else
    {
        await db.Database.MigrateAsync();
    }
    await KrizovkaDataSeeder.SeedAsync(db);
}

app.UseExceptionHandler();
app.UseCors();
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
    message = "Identity integration is available."
})).RequireAuthorization();
app.MapReportEndpoints();
app.MapOrganizationEndpoints();
app.MapGuideEndpoints();
app.Run();

public partial class Program { }
