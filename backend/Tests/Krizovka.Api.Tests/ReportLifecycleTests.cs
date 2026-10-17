using System.Data.Common;
using Krizovka.Api.Data;
using Krizovka.Api.Reports;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Krizovka.Api.Tests;

public class ReportLifecycleTests : IDisposable
{
    private readonly DbConnection _connection;

    public ReportLifecycleTests()
    {
        _connection = new SqliteConnection("DataSource=:memory:");
        _connection.Open();

        using var db = CreateDb();
        db.Database.EnsureCreated();
    }

    private KrizovkaDbContext CreateDb()
    {
        var options = new DbContextOptionsBuilder<KrizovkaDbContext>()
            .UseSqlite(_connection)
            .LogTo(Console.WriteLine, Microsoft.Extensions.Logging.LogLevel.Information)
            .Options;
        return new KrizovkaDbContext(options);
    }

    public void Dispose()
    {
        _connection.Dispose();
    }

    [Fact]
    public async Task Report_Should_Be_Created_With_New_Status_And_History()
    {
        var orgId = Guid.NewGuid();
        var clientReportId = Guid.NewGuid();
        var reporterAccountId = Guid.NewGuid();
        Guid reportId;

        using (var db = CreateDb())
        {
            var report = Issue.Create(
                organizationId: orgId,
                reporterAccountId: reporterAccountId,
                clientReportId: clientReportId,
                description: "POTŘEBUJI POMOC - požár v laboratoři",
                latitude: 50.087,
                longitude: 14.420,
                accuracyMeters: 10,
                locationMeasuredAt: DateTimeOffset.UtcNow,
                contact: new ContactSnapshot
                {
                    FirstName = "Jan",
                    LastName = "Novák",
                    Phone = "+420 777 123 456",
                    Email = "jan.novak@example.invalid"
                },
                ipAddress: "192.168.1.100",
                userAgent: "KrizovkaFlutter/1.0 (iOS)"
            );

            reportId = report.Id;
            db.Reports.Add(report);
            await db.SaveChangesAsync();
        }

        using (var db = CreateDb())
        {
            var saved = await db.Reports
                .Include(r => r.History)
                .FirstOrDefaultAsync(r => r.Id == reportId);

            Assert.NotNull(saved);
            Assert.Equal(ReportStatus.New, saved.Status);
            Assert.Equal(clientReportId, saved.ClientReportId);
            Assert.Equal("Jan", saved.Contact.FirstName);
            Assert.Equal("192.168.1.100", saved.IpAddress);
            Assert.Equal("KrizovkaFlutter/1.0 (iOS)", saved.UserAgent);
            Assert.Single(saved.History);
            Assert.Equal("Created", saved.History.First().Action);
            Assert.Contains("192.168.1.100", saved.History.First().Metadata);
        }
    }

    [Fact]
    public async Task Report_Can_Be_Created_With_Empty_Contact_And_Captures_Device_Telemetry()
    {
        var orgId = Guid.NewGuid();
        var clientReportId = Guid.NewGuid();
        Guid reportId;

        // Anonymní / nevyplněný profil uživatele nesmí bránit odeslání SOS
        using (var db = CreateDb())
        {
            var report = Issue.Create(
                organizationId: orgId,
                reporterAccountId: Guid.Empty,
                clientReportId: clientReportId,
                description: null,
                latitude: null,
                longitude: null,
                accuracyMeters: null,
                locationMeasuredAt: null,
                contact: null, // Uživatel zatím nevyplnil žádné údaje v Nastavení
                ipAddress: "203.0.113.42",
                userAgent: "Mozilla/5.0 (iPhone; CPU iPhone OS 18_0) Mobile"
            );

            reportId = report.Id;
            db.Reports.Add(report);
            await db.SaveChangesAsync();
        }

        using (var db = CreateDb())
        {
            var saved = await db.Reports
                .Include(r => r.History)
                .FirstOrDefaultAsync(r => r.Id == reportId);

            Assert.NotNull(saved);
            Assert.Equal(ReportStatus.New, saved.Status);
            Assert.Equal("203.0.113.42", saved.IpAddress);
            Assert.Equal("Mozilla/5.0 (iPhone; CPU iPhone OS 18_0) Mobile", saved.UserAgent);
            Assert.NotNull(saved.Contact);
            Assert.Null(saved.Contact.FirstName);
            Assert.Single(saved.History);
        }
    }

    [Fact]
    public async Task Report_Status_Should_Transition_To_Accepted_And_Closed()
    {
        var orgId = Guid.NewGuid();
        var clientReportId = Guid.NewGuid();
        var actorId = Guid.NewGuid();
        Guid reportId;

        // 1. Initial creation (POST /api/reports)
        using (var db = CreateDb())
        {
            var report = Issue.Create(
                organizationId: orgId,
                reporterAccountId: Guid.Empty,
                clientReportId: clientReportId,
                description: "Nouzové volání",
                latitude: null,
                longitude: null,
                accuracyMeters: null,
                locationMeasuredAt: null,
                contact: new ContactSnapshot { FirstName = "Test", LastName = "User" }
            );

            reportId = report.Id;
            db.Reports.Add(report);
            await db.SaveChangesAsync();
        }

        // 2. Operator takeover (POST /api/reports/{id}/status -> Accepted)
        using (var db = CreateDb())
        {
            var report = await db.Reports.Include(r => r.History).FirstAsync(r => r.Id == reportId);
            report.ChangeStatus(ReportStatus.Accepted, actorId, "Operátor převzal hlášení");
            await db.SaveChangesAsync();
        }

        using (var db = CreateDb())
        {
            var accepted = await db.Reports.FirstAsync(r => r.Id == reportId);
            Assert.Equal(ReportStatus.Accepted, accepted.Status);
            Assert.NotNull(accepted.UpdatedAt);
        }

        // 3. Operator close (POST /api/reports/{id}/status -> Closed)
        using (var db = CreateDb())
        {
            var report = await db.Reports.Include(r => r.History).FirstAsync(r => r.Id == reportId);
            report.ChangeStatus(ReportStatus.Closed, actorId, "Vyřešeno na místě");
            await db.SaveChangesAsync();
        }

        using (var db = CreateDb())
        {
            var closed = await db.Reports
                .Include(r => r.History)
                .FirstAsync(r => r.Id == reportId);

            Assert.Equal(ReportStatus.Closed, closed.Status);
            // 1 Created + 2 StatusChanges
            Assert.Equal(3, closed.History.Count);
        }
    }

    [Fact]
    public async Task Report_Location_Update_Should_Store_New_Coordinates()
    {
        var orgId = Guid.NewGuid();
        var clientReportId = Guid.NewGuid();
        Guid reportId;

        using (var db = CreateDb())
        {
            var report = Issue.Create(
                organizationId: orgId,
                reporterAccountId: Guid.Empty,
                clientReportId: clientReportId,
                description: "Test",
                latitude: null,
                longitude: null,
                accuracyMeters: null,
                locationMeasuredAt: null,
                contact: new ContactSnapshot { FirstName = "Test" }
            );

            reportId = report.Id;
            db.Reports.Add(report);
            await db.SaveChangesAsync();
        }

        // Subsequent location update (POST /api/reports/{id}/location)
        using (var db = CreateDb())
        {
            var report = await db.Reports.FirstAsync(r => r.Id == reportId);
            report.UpdateLocation(50.12345, 14.54321, 8.5, DateTimeOffset.UtcNow);
            await db.SaveChangesAsync();
        }

        using (var db2 = CreateDb())
        {
            var updated = await db2.Reports.FirstAsync(r => r.Id == reportId);
            Assert.Equal(50.12345, updated.Latitude);
            Assert.Equal(14.54321, updated.Longitude);
            Assert.Equal(8.5, updated.AccuracyMeters);
            Assert.NotNull(updated.LocationMeasuredAt);
        }
    }

    [Fact]
    public async Task Report_Attachments_Should_Be_Saved_And_Listed_In_Detail()
    {
        var orgId = Guid.NewGuid();
        var clientReportId = Guid.NewGuid();
        var attachmentId = Guid.NewGuid();
        var now = DateTimeOffset.UtcNow;
        Guid reportId;

        using (var db = CreateDb())
        {
            var report = Issue.Create(
                organizationId: orgId,
                reporterAccountId: Guid.Empty,
                clientReportId: clientReportId,
                description: "Nouzová situace s fotkou",
                latitude: null,
                longitude: null,
                accuracyMeters: null,
                locationMeasuredAt: null,
                contact: new ContactSnapshot { FirstName = "Petr", LastName = "Dvořák" }
            );

            var attachment = new IssueAttachment
            {
                Id = attachmentId,
                IssueId = report.Id,
                FileName = "photo_scene.jpg",
                ContentType = "image/jpeg",
                SizeBytes = 1024 * 50,
                StorageKey = $"{report.Id}/{attachmentId}_photo_scene.jpg",
                CreatedAt = now
            };

            report.AddAttachment(attachment, Guid.Empty);
            reportId = report.Id;

            db.Reports.Add(report);
            await db.SaveChangesAsync();
        }

        using (var db = CreateDb())
        {
            var saved = await db.Reports
                .Include(r => r.Attachments)
                .Include(r => r.History)
                .FirstAsync(r => r.Id == reportId);

            Assert.Single(saved.Attachments);
            var att = saved.Attachments.First();
            Assert.Equal("photo_scene.jpg", att.FileName);
            Assert.Equal("image/jpeg", att.ContentType);
            Assert.Equal(1024 * 50, att.SizeBytes);
            Assert.Contains(saved.History, h => h.Action == "AttachmentAdded" && h.NewValue == "photo_scene.jpg");
        }
    }

    [Fact]
    public async Task FileSystemAttachmentStorage_Should_Save_And_Open_Stream()
    {
        var tempDir = Path.Combine(Path.GetTempPath(), "krizovka_tests_" + Guid.NewGuid().ToString("N"));
        try
        {
            var config = new Microsoft.Extensions.Configuration.ConfigurationManager();
            config["Storage:AttachmentsDirectory"] = tempDir;

            var env = new TestWebHostEnvironment();
            var storage = new FileSystemAttachmentStorage(config, env);

            var reportId = Guid.NewGuid();
            var attachmentId = Guid.NewGuid();
            var testBytes = new byte[] { 1, 2, 3, 4, 5, 6, 7, 8 };
            using var inputStream = new MemoryStream(testBytes);

            var key = await storage.SaveAttachmentAsync(reportId, attachmentId, "voice_note.m4a", inputStream);
            Assert.False(string.IsNullOrWhiteSpace(key));

            using var readStream = await storage.OpenAttachmentReadStreamAsync(key);
            Assert.NotNull(readStream);
            using var ms = new MemoryStream();
            await readStream.CopyToAsync(ms);
            Assert.Equal(testBytes, ms.ToArray());

            await storage.DeleteAttachmentAsync(key);
            var readStreamAfterDelete = await storage.OpenAttachmentReadStreamAsync(key);
            Assert.Null(readStreamAfterDelete);
        }
        finally
        {
            if (Directory.Exists(tempDir))
                Directory.Delete(tempDir, true);
        }
    }

    [Fact]
    public async Task Reports_Can_Be_Queried_And_Ordered_By_CreatedAt_On_Sqlite()
    {
        var orgId = Guid.NewGuid();
        using (var db = CreateDb())
        {
            var r1 = Issue.Create(
                organizationId: orgId,
                reporterAccountId: Guid.Empty,
                clientReportId: Guid.NewGuid(),
                description: "Report 1",
                latitude: null,
                longitude: null,
                accuracyMeters: null,
                locationMeasuredAt: null,
                contact: new ContactSnapshot { FirstName = "A", LastName = "B" }
            );

            var r2 = Issue.Create(
                organizationId: orgId,
                reporterAccountId: Guid.Empty,
                clientReportId: Guid.NewGuid(),
                description: "Report 2",
                latitude: null,
                longitude: null,
                accuracyMeters: null,
                locationMeasuredAt: null,
                contact: new ContactSnapshot { FirstName = "C", LastName = "D" }
            );

            db.Reports.AddRange(r1, r2);
            await db.SaveChangesAsync();
        }

        using (var db = CreateDb())
        {
            var reports = await db.Reports
                .AsNoTracking()
                .Where(r => r.OrganizationId == orgId)
                .OrderByDescending(r => r.CreatedAt)
                .Take(10)
                .ToListAsync();

            Assert.Equal(2, reports.Count);
        }
    }

    [Fact]
    public async Task Report_Should_Support_Supplemental_Messages()
    {
        var orgId = Guid.NewGuid();
        Guid reportId;

        using (var db = CreateDb())
        {
            var report = Issue.Create(
                organizationId: orgId,
                reporterAccountId: Guid.Empty,
                clientReportId: Guid.NewGuid(),
                description: "Požár",
                latitude: null,
                longitude: null,
                accuracyMeters: null,
                locationMeasuredAt: null,
                contact: new ContactSnapshot { FirstName = "Jan", LastName = "Novák" }
            );
            reportId = report.Id;
            db.Reports.Add(report);
            await db.SaveChangesAsync();
        }

        // Add supplemental message
        using (var db = CreateDb())
        {
            var report = await db.Reports.Include(r => r.History).FirstAsync(r => r.Id == reportId);
            report.AddSupplementalMessage("Kouř se šíří do 2. patra.", Guid.Empty);
            await db.SaveChangesAsync();
        }

        using (var db = CreateDb())
        {
            var report = await db.Reports.Include(r => r.History).FirstAsync(r => r.Id == reportId);
            var msgHistory = report.History.FirstOrDefault(h => h.Action == "MessageAdded");
            Assert.NotNull(msgHistory);
            Assert.Equal("Kouř se šíří do 2. patra.", msgHistory.NewValue);
        }
    }

    [Fact]
    public async Task Report_Should_Support_LiveStream_Start_And_End_Without_Closing_Report()
    {
        var orgId = Guid.NewGuid();
        Guid reportId;

        using (var db = CreateDb())
        {
            var report = Issue.Create(
                organizationId: orgId,
                reporterAccountId: Guid.Empty,
                clientReportId: Guid.NewGuid(),
                description: "Střelec v areálu",
                latitude: null,
                longitude: null,
                accuracyMeters: null,
                locationMeasuredAt: null,
                contact: new ContactSnapshot { FirstName = "Petr", LastName = "Svoboda" }
            );
            reportId = report.Id;
            db.Reports.Add(report);
            await db.SaveChangesAsync();
        }

        // Start stream
        using (var db = CreateDb())
        {
            var report = await db.Reports.Include(r => r.History).FirstAsync(r => r.Id == reportId);
            report.StartStream("audio", Guid.Empty);
            await db.SaveChangesAsync();
        }

        // End stream
        using (var db = CreateDb())
        {
            var report = await db.Reports.Include(r => r.History).FirstAsync(r => r.Id == reportId);
            report.EndStream(Guid.Empty);
            await db.SaveChangesAsync();
        }

        using (var db = CreateDb())
        {
            var report = await db.Reports.Include(r => r.History).FirstAsync(r => r.Id == reportId);
            Assert.Equal(ReportStatus.New, report.Status); // Stream ended does NOT close SOS report
            Assert.Equal(3, report.History.Count); // Created + LiveStreamStarted + LiveStreamEnded
            Assert.Contains(report.History, h => h.Action == "LiveStreamStarted" && h.Metadata == "UplinkOnly");
            Assert.Contains(report.History, h => h.Action == "LiveStreamEnded");
        }
    }
}

file class TestWebHostEnvironment : Microsoft.AspNetCore.Hosting.IWebHostEnvironment
{
    public string WebRootPath { get; set; } = "";
    public Microsoft.Extensions.FileProviders.IFileProvider WebRootFileProvider { get; set; } = null!;
    public string ApplicationName { get; set; } = "Krizovka.Api.Tests";
    public Microsoft.Extensions.FileProviders.IFileProvider ContentRootFileProvider { get; set; } = null!;
    public string ContentRootPath { get; set; } = AppContext.BaseDirectory;
    public string EnvironmentName { get; set; } = "Testing";
}
