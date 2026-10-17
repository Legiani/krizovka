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

public class ReportIdempotencyTests : IClassFixture<WebApplicationFactory<Program>>, IDisposable
{
    private readonly WebApplicationFactory<Program> _factory;
    private readonly SqliteConnection _sqliteConnection;
    private readonly HttpClient _client;

    public ReportIdempotencyTests(WebApplicationFactory<Program> factory)
    {
        _sqliteConnection = new SqliteConnection("DataSource=:memory:");
        _sqliteConnection.Open();

        _factory = factory.WithWebHostBuilder(builder =>
        {
            builder.UseEnvironment("Testing");
            builder.ConfigureServices(services =>
            {
                var descriptor = services.SingleOrDefault(d => d.ServiceType == typeof(DbContextOptions<KrizovkaDbContext>));
                if (descriptor != null)
                {
                    services.Remove(descriptor);
                }

                services.AddDbContext<KrizovkaDbContext>(options =>
                {
                    options.UseSqlite(_sqliteConnection);
                });

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
    public async Task CreateReport_DuplicateClientReportId_Returns200AndSameReport()
    {
        var orgId = Guid.NewGuid();
        var clientReportId = Guid.NewGuid();
        var request = new CreateReportRequest(
            ClientReportId: clientReportId,
            OrganizationId: orgId,
            Description: "POTŘEBUJI POMOC - první pokus",
            Latitude: 50.087811,
            Longitude: 14.420460,
            AccuracyMeters: 8.5,
            LocationMeasuredAt: DateTimeOffset.UtcNow,
            Contact: new ContactSnapshot { FirstName = "Jan", LastName = "Novák", Phone = "+420 777 000 111" }
        );

        // 1. Initial request
        var res1 = await _client.PostAsJsonAsync("/reports", request);
        Assert.True(res1.StatusCode == HttpStatusCode.Created || res1.StatusCode == HttpStatusCode.OK);
        var detail1 = await res1.Content.ReadFromJsonAsync<ReportDetailDto>();
        Assert.NotNull(detail1);

        // 2. Duplicate retry request (panic double click or network retry)
        var res2 = await _client.PostAsJsonAsync("/reports", request);
        Assert.Equal(HttpStatusCode.OK, res2.StatusCode);
        var detail2 = await res2.Content.ReadFromJsonAsync<ReportDetailDto>();
        Assert.NotNull(detail2);

        // Both requests return the same report ID
        Assert.Equal(detail1.Id, detail2.Id);
        Assert.Equal(clientReportId, detail1.ClientReportId);
        Assert.Equal(orgId, detail1.OrganizationId);

        // Verify only 1 record exists in the database
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<KrizovkaDbContext>();
        var count = await db.Reports.CountAsync(r => r.OrganizationId == orgId && r.ClientReportId == clientReportId);
        Assert.Equal(1, count);
    }

    [Fact]
    public async Task CreateReport_SameClientReportId_DifferentOrganization_CreatesTwoSeparateReports()
    {
        var clientReportId = Guid.NewGuid();
        var org1 = Guid.NewGuid();
        var org2 = Guid.NewGuid();

        var req1 = new CreateReportRequest(
            ClientReportId: clientReportId,
            OrganizationId: org1,
            Description: "Hlášení pro organizaci 1",
            Latitude: 50.087,
            Longitude: 14.420,
            AccuracyMeters: 10.0,
            LocationMeasuredAt: DateTimeOffset.UtcNow,
            Contact: null
        );

        var req2 = new CreateReportRequest(
            ClientReportId: clientReportId,
            OrganizationId: org2,
            Description: "Hlášení pro organizaci 2",
            Latitude: 50.087,
            Longitude: 14.420,
            AccuracyMeters: 10.0,
            LocationMeasuredAt: DateTimeOffset.UtcNow,
            Contact: null
        );

        var res1 = await _client.PostAsJsonAsync("/reports", req1);
        var res2 = await _client.PostAsJsonAsync("/reports", req2);

        Assert.True(res1.StatusCode == HttpStatusCode.Created || res1.StatusCode == HttpStatusCode.OK);
        Assert.True(res2.StatusCode == HttpStatusCode.Created || res2.StatusCode == HttpStatusCode.OK);

        var detail1 = await res1.Content.ReadFromJsonAsync<ReportDetailDto>();
        var detail2 = await res2.Content.ReadFromJsonAsync<ReportDetailDto>();

        Assert.NotNull(detail1);
        Assert.NotNull(detail2);
        Assert.NotEqual(detail1.Id, detail2.Id);
        Assert.Equal(org1, detail1.OrganizationId);
        Assert.Equal(org2, detail2.OrganizationId);
        Assert.Equal(clientReportId, detail1.ClientReportId);
        Assert.Equal(clientReportId, detail2.ClientReportId);

        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<KrizovkaDbContext>();
        var reports = await db.Reports.Where(r => r.ClientReportId == clientReportId).ToListAsync();
        Assert.Equal(2, reports.Count);
    }

    [Fact]
    public async Task Database_UniqueIndex_RejectsDuplicateOrganizationAndClientReportId()
    {
        var orgId = Guid.NewGuid();
        var clientReportId = Guid.NewGuid();

        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<KrizovkaDbContext>();
            var report1 = Issue.Create(
                organizationId: orgId,
                reporterAccountId: Guid.Empty,
                clientReportId: clientReportId,
                description: "Záznam 1",
                latitude: null,
                longitude: null,
                accuracyMeters: null,
                locationMeasuredAt: null,
                contact: null
            );
            db.Reports.Add(report1);
            await db.SaveChangesAsync();
        }

        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<KrizovkaDbContext>();
            var report2 = Issue.Create(
                organizationId: orgId,
                reporterAccountId: Guid.Empty,
                clientReportId: clientReportId,
                description: "Záznam 2 – kolize stejného klíče",
                latitude: null,
                longitude: null,
                accuracyMeters: null,
                locationMeasuredAt: null,
                contact: null
            );
            db.Reports.Add(report2);

            await Assert.ThrowsAsync<DbUpdateException>(() => db.SaveChangesAsync());
        }
    }

    [Fact]
    public async Task CreateReport_ParallelRequests_HandlesConcurrency()
    {
        var orgId = Guid.NewGuid();
        var clientReportId = Guid.NewGuid();
        var request = new CreateReportRequest(
            ClientReportId: clientReportId,
            OrganizationId: orgId,
            Description: "Souběžné panické stisknutí tlačítka SOS",
            Latitude: 50.087811,
            Longitude: 14.420460,
            AccuracyMeters: 5.0,
            LocationMeasuredAt: DateTimeOffset.UtcNow,
            Contact: new ContactSnapshot { FirstName = "Petr", LastName = "Svoboda" }
        );

        // 5 concurrent requests with identical clientReportId
        var tasks = Enumerable.Range(0, 5)
            .Select(_ => _client.PostAsJsonAsync("/reports", request))
            .ToList();

        var responses = await Task.WhenAll(tasks);

        foreach (var res in responses)
        {
            Assert.True(res.IsSuccessStatusCode);
            Assert.True(res.StatusCode == HttpStatusCode.OK || res.StatusCode == HttpStatusCode.Created);
        }

        var details = new List<ReportDetailDto>();
        foreach (var res in responses)
        {
            var detail = await res.Content.ReadFromJsonAsync<ReportDetailDto>();
            Assert.NotNull(detail);
            details.Add(detail);
        }

        var expectedReportId = details[0].Id;
        Assert.All(details, d =>
        {
            Assert.Equal(expectedReportId, d.Id);
            Assert.Equal(clientReportId, d.ClientReportId);
            Assert.Equal(orgId, d.OrganizationId);
        });

        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<KrizovkaDbContext>();
        var count = await db.Reports.CountAsync(r => r.OrganizationId == orgId && r.ClientReportId == clientReportId);
        Assert.Equal(1, count);
    }
}
