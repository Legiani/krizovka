using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Security.Claims;
using Krizovka.Api.Data;
using Krizovka.Api.Reports;
using Krizovka.Shared.Authentication;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Krizovka.Api.Tests;

[Collection("ApiSecurityTests")]
public class ReportTenantSecurityTests : IClassFixture<WebApplicationFactory<Program>>, IDisposable
{
    private readonly WebApplicationFactory<Program> _factory;
    private readonly SqliteConnection _sqliteConnection;
    private readonly HttpClient _client;

    public static readonly Guid OrgA = Guid.Parse("11111111-1111-1111-1111-111111111111");
    public static readonly Guid OrgB = Guid.Parse("22222222-2222-2222-2222-222222222222");

    public ReportTenantSecurityTests(WebApplicationFactory<Program> factory)
    {
        _sqliteConnection = new SqliteConnection("DataSource=:memory:");
        _sqliteConnection.Open();

        _factory = factory.WithWebHostBuilder(builder =>
        {
            builder.UseEnvironment("Testing");
            builder.ConfigureServices(services =>
            {
                // Replace DB context with in-memory SQLite using shared connection
                var dbDescriptor = services.SingleOrDefault(d => d.ServiceType == typeof(DbContextOptions<KrizovkaDbContext>));
                if (dbDescriptor != null)
                {
                    services.Remove(dbDescriptor);
                }

                services.AddDbContext<KrizovkaDbContext>(options =>
                {
                    options.UseSqlite(_sqliteConnection);
                });

                // Replace IUsAuthClient with test mock
                var authDescriptors = services.Where(d => d.ServiceType == typeof(IUsAuthClient)).ToList();
                foreach (var desc in authDescriptors)
                {
                    services.Remove(desc);
                }
                services.AddSingleton<IUsAuthClient, TestUsAuthClient>();

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

    private async Task<(Issue reportA, Issue reportB)> SeedReportsAsync()
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<KrizovkaDbContext>();

        var reportA = Issue.Create(
            organizationId: OrgA,
            reporterAccountId: Guid.NewGuid(),
            clientReportId: Guid.NewGuid(),
            description: "SOS v Kampusu A",
            latitude: 50.08,
            longitude: 14.41,
            accuracyMeters: 5,
            locationMeasuredAt: DateTimeOffset.UtcNow,
            contact: new ContactSnapshot { FirstName = "Jan", LastName = "Novak", Phone = "+420 111 222 333" },
            ipAddress: "10.0.0.1",
            userAgent: "TestAgent"
        );

        var attachmentA = new IssueAttachment
        {
            Id = Guid.NewGuid(),
            IssueId = reportA.Id,
            FileName = "evidenceA.jpg",
            ContentType = "image/jpeg",
            SizeBytes = 1024,
            StorageKey = "attachments/evidenceA.jpg",
            CreatedAt = DateTimeOffset.UtcNow
        };
        reportA.AddAttachment(attachmentA, Guid.NewGuid());

        var reportB = Issue.Create(
            organizationId: OrgB,
            reporterAccountId: Guid.NewGuid(),
            clientReportId: Guid.NewGuid(),
            description: "SOS v Kampusu B",
            latitude: 50.09,
            longitude: 14.42,
            accuracyMeters: 10,
            locationMeasuredAt: DateTimeOffset.UtcNow,
            contact: new ContactSnapshot { FirstName = "Karel", LastName = "Dvorak", Phone = "+420 444 555 666" },
            ipAddress: "10.0.0.2",
            userAgent: "TestAgent"
        );

        var attachmentB = new IssueAttachment
        {
            Id = Guid.NewGuid(),
            IssueId = reportB.Id,
            FileName = "evidenceB.jpg",
            ContentType = "image/jpeg",
            SizeBytes = 2048,
            StorageKey = "attachments/evidenceB.jpg",
            CreatedAt = DateTimeOffset.UtcNow
        };
        reportB.AddAttachment(attachmentB, Guid.NewGuid());

        db.Reports.AddRange(reportA, reportB);
        await db.SaveChangesAsync();

        return (reportA, reportB);
    }

    [Fact]
    public async Task Operator_Cannot_List_Reports_From_Another_Organization()
    {
        var (reportA, reportB) = await SeedReportsAsync();

        // Operator of Org A queries with ?organizationId=OrgB
        var request = new HttpRequestMessage(HttpMethod.Get, $"/api/reports?organizationId={OrgB}");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", $"operator-org-{OrgA}");

        var response = await _client.SendAsync(request);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var reports = await response.Content.ReadFromJsonAsync<List<ReportSummaryDto>>();
        Assert.NotNull(reports);

        // Musí vidět pouze hlášení Org A, nikdy Org B
        Assert.Contains(reports, r => r.Id == reportA.Id);
        Assert.DoesNotContain(reports, r => r.Id == reportB.Id);
        Assert.All(reports, r => Assert.Equal(OrgA, r.OrganizationId));
    }

    [Fact]
    public async Task Operator_Cannot_Access_Report_Detail_Of_Another_Organization()
    {
        var (reportA, reportB) = await SeedReportsAsync();

        // Operator of Org A requests detail of reportB
        var request = new HttpRequestMessage(HttpMethod.Get, $"/api/reports/{reportB.Id}");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", $"operator-org-{OrgA}");

        var response = await _client.SendAsync(request);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Operator_Cannot_Download_Attachment_Of_Another_Organization()
    {
        var (reportA, reportB) = await SeedReportsAsync();
        var attachmentB = reportB.Attachments.First();

        // Operator of Org A tries to download attachment belonging to reportB
        var request = new HttpRequestMessage(HttpMethod.Get, $"/api/reports/{reportB.Id}/attachments/{attachmentB.Id}");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", $"operator-org-{OrgA}");

        var response = await _client.SendAsync(request);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Anonymous_Request_To_Report_List_Returns_401()
    {
        await SeedReportsAsync();

        // Listing reports requires OperatorPolicy
        var response = await _client.GetAsync("/api/reports");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Anonymous_Request_To_Report_Detail_By_Id_Succeeds_For_Client_Polling()
    {
        var (reportA, _) = await SeedReportsAsync();

        // Anonymous call with specific report GUID succeeds for citizen client polling
        var response = await _client.GetAsync($"/api/reports/{reportA.Id}");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var detail = await response.Content.ReadFromJsonAsync<ReportDetailDto>();
        Assert.NotNull(detail);
        Assert.Equal(reportA.Id, detail.Id);
    }

    [Fact]
    public async Task Anonymous_Request_To_Attachment_By_Id_Succeeds_For_Client_Preview()
    {
        var (reportA, _) = await SeedReportsAsync();
        var attachmentA = reportA.Attachments.First();

        // Anonymous call with specific report and attachment GUID succeeds
        var response = await _client.GetAsync($"/api/reports/{reportA.Id}/attachments/{attachmentA.Id}");

        // In test environment dummy storage returns 404 file not found or 200 file, but NEVER 401 Unauthorized
        Assert.NotEqual(HttpStatusCode.Unauthorized, response.StatusCode);
        Assert.NotEqual(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task SuperAdmin_Can_Access_Reports_Across_All_Organizations()
    {
        var (reportA, reportB) = await SeedReportsAsync();

        // 1. SuperAdmin can read detail of Org A
        var reqDetailA = new HttpRequestMessage(HttpMethod.Get, $"/api/reports/{reportA.Id}");
        reqDetailA.Headers.Authorization = new AuthenticationHeaderValue("Bearer", "superadmin-token");
        var resDetailA = await _client.SendAsync(reqDetailA);
        Assert.Equal(HttpStatusCode.OK, resDetailA.StatusCode);

        // 2. SuperAdmin can read detail of Org B
        var reqDetailB = new HttpRequestMessage(HttpMethod.Get, $"/api/reports/{reportB.Id}");
        reqDetailB.Headers.Authorization = new AuthenticationHeaderValue("Bearer", "superadmin-token");
        var resDetailB = await _client.SendAsync(reqDetailB);
        Assert.Equal(HttpStatusCode.OK, resDetailB.StatusCode);

        // 3. SuperAdmin can list specific organization Org B
        var reqListB = new HttpRequestMessage(HttpMethod.Get, $"/api/reports?organizationId={OrgB}");
        reqListB.Headers.Authorization = new AuthenticationHeaderValue("Bearer", "superadmin-token");
        var resListB = await _client.SendAsync(reqListB);
        Assert.Equal(HttpStatusCode.OK, resListB.StatusCode);
        var reportsB = await resListB.Content.ReadFromJsonAsync<List<ReportSummaryDto>>();
        Assert.NotNull(reportsB);
        Assert.Contains(reportsB, r => r.Id == reportB.Id);
        Assert.DoesNotContain(reportsB, r => r.Id == reportA.Id);
    }

    [Fact]
    public async Task Operator_Without_Assigned_Organization_Returns_403_On_List()
    {
        await SeedReportsAsync();

        var request = new HttpRequestMessage(HttpMethod.Get, "/api/reports");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", "operator-no-org");

        var response = await _client.SendAsync(request);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }
}

public class TestUsAuthClient : IUsAuthClient
{
    public Task<ValidationResult?> ValidateContextAsync(string token, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrEmpty(token)) return Task.FromResult<ValidationResult?>(null);

        if (token == "superadmin-token")
        {
            return Task.FromResult<ValidationResult?>(new ValidationResult(
                AccountId: Guid.NewGuid(),
                IdentityId: Guid.NewGuid(),
                Claims: new List<string> { "*:*" },
                Roles: new List<string> { "SuperAdmin" },
                ScopeType: "GLOBAL",
                ScopeId: "*",
                IsBackoffice: true
            ));
        }

        if (token.StartsWith("operator-org-"))
        {
            var orgIdStr = token.Substring("operator-org-".Length);
            var orgId = Guid.Parse(orgIdStr);
            return Task.FromResult<ValidationResult?>(new ValidationResult(
                AccountId: Guid.NewGuid(),
                IdentityId: Guid.NewGuid(),
                Claims: new List<string>(),
                Roles: new List<string> { "OPERATOR" },
                ScopeType: "ORGANIZATION",
                ScopeId: orgId.ToString(),
                IsBackoffice: true,
                OrganizationId: orgId
            ));
        }

        if (token == "operator-no-org")
        {
            return Task.FromResult<ValidationResult?>(new ValidationResult(
                AccountId: Guid.NewGuid(),
                IdentityId: Guid.NewGuid(),
                Claims: new List<string>(),
                Roles: new List<string> { "OPERATOR" },
                ScopeType: null,
                ScopeId: null,
                IsBackoffice: true,
                OrganizationId: null
            ));
        }

        return Task.FromResult<ValidationResult?>(null);
    }
}
