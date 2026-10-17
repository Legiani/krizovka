using System.Net;
using System.Net.Http.Json;
using Krizovka.Api.Data;
using Krizovka.Api.Reports;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Krizovka.Api.Tests;

[Collection("ApiSecurityTests")]
public class ReportEndpointSecurityTests : IClassFixture<WebApplicationFactory<Program>>, IDisposable
{
    private readonly WebApplicationFactory<Program> _factory;
    private readonly SqliteConnection _sqliteConnection;
    private readonly HttpClient _client;

    public ReportEndpointSecurityTests(WebApplicationFactory<Program> factory)
    {
        _sqliteConnection = new SqliteConnection("DataSource=:memory:");
        _sqliteConnection.Open();

        _factory = factory.WithWebHostBuilder(builder =>
        {
            builder.UseEnvironment("Testing");
            builder.ConfigureServices(services =>
            {
                // Replace DB context with in-memory SQLite using shared connection
                var descriptor = services.SingleOrDefault(d => d.ServiceType == typeof(DbContextOptions<KrizovkaDbContext>));
                if (descriptor != null)
                {
                    services.Remove(descriptor);
                }

                services.AddDbContext<KrizovkaDbContext>(options =>
                {
                    options.UseSqlite(_sqliteConnection);
                });

                // Ensure schema created
                var sp = services.BuildServiceProvider();
                using var scope = sp.CreateScope();
                var db = scope.ServiceProvider.GetRequiredService<KrizovkaDbContext>();
                db.Database.EnsureCreated();
            });
        });

        _client = _factory.CreateClient();
    }

    public void Dispose()
    {
        _client.Dispose();
        _sqliteConnection.Dispose();
    }

    [Fact]
    public async Task Get_Reports_Without_Authorization_Returns_401()
    {
        var response = await _client.GetAsync("/api/reports");
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Post_Organizations_Without_Authorization_Returns_401()
    {
        var response = await _client.PostAsJsonAsync("/api/organizations", new
        {
            Name = "Test Org",
            Code = "TESTORG"
        });

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Post_Guides_Without_Authorization_Returns_401()
    {
        var response = await _client.PostAsJsonAsync("/api/guides", new
        {
            Title = "Evakuace",
            Category = "Požár",
            OrganizationId = Guid.NewGuid()
        });

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Post_Report_Anonymously_Succeeds_And_Captures_Device_Telemetry()
    {
        var orgId = Guid.NewGuid();
        var clientReportId = Guid.NewGuid();

        var request = new HttpRequestMessage(HttpMethod.Post, "/api/reports")
        {
            Content = JsonContent.Create(new
            {
                ClientReportId = clientReportId,
                OrganizationId = orgId,
                Description = "SOS incident v hale B",
                Contact = new
                {
                    FirstName = "Petr",
                    LastName = "Nový",
                    Phone = "+420 601 234 567"
                }
            })
        };

        // Telemetry headers sent by client / proxy
        request.Headers.Add("X-Forwarded-For", "198.51.100.25, 10.0.0.1");
        request.Headers.Add("User-Agent", "KrizovkaApp/2.0 (Android 15; Pixel 9)");

        var response = await _client.SendAsync(request);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var created = await response.Content.ReadFromJsonAsync<ReportDetailDto>();
        Assert.NotNull(created);
        Assert.Equal(orgId, created.OrganizationId);
        Assert.Equal("198.51.100.25", created.IpAddress);
        Assert.Equal("KrizovkaApp/2.0 (Android 15; Pixel 9)", created.UserAgent);
    }

    [Fact]
    public async Task Post_Report_With_Empty_Contact_Succeeds_Without_Blocking()
    {
        var orgId = Guid.NewGuid();
        var clientReportId = Guid.NewGuid();

        // Uživatel ještě nevyplnil žádná data v Nastavení -> SOS přesto projde
        var request = new HttpRequestMessage(HttpMethod.Post, "/api/reports")
        {
            Content = JsonContent.Create(new
            {
                ClientReportId = clientReportId,
                OrganizationId = orgId,
                Description = "Tichý poplach - tlačítko bez profilu"
            })
        };
        request.Headers.Add("X-Forwarded-For", "203.0.113.88");

        var response = await _client.SendAsync(request);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var created = await response.Content.ReadFromJsonAsync<ReportDetailDto>();
        Assert.NotNull(created);
        Assert.Equal("203.0.113.88", created.IpAddress);
        Assert.Null(created.Contact.FirstName);
    }
}
