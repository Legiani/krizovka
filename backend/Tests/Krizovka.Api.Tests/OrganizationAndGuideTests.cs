using System.Data.Common;
using System.Text.Json;
using Krizovka.Api.Data;
using Krizovka.Api.Guides;
using Krizovka.Api.Organizations;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Krizovka.Api.Tests;

public class OrganizationAndGuideTests : IDisposable
{
    private readonly DbConnection _connection;

    public OrganizationAndGuideTests()
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
            .Options;
        return new KrizovkaDbContext(options);
    }

    public void Dispose()
    {
        _connection.Dispose();
    }

    [Fact]
    public async Task Seeder_Initializes_Default_Orgs_And_GuideTemplates()
    {
        using (var db = CreateDb())
        {
            await KrizovkaDataSeeder.SeedAsync(db);

            var orgs = await db.Organizations.Include(x => x.Areas).ToListAsync();
            Assert.NotEmpty(orgs);
            Assert.Contains(orgs, o => o.IsDefaultDispatch);

            var templates = await db.GuideTemplates.ToListAsync();
            Assert.True(templates.Count >= 3);
            Assert.Contains(templates, t => t.Id == "tmpl-active-shooter-v1");

            var guides = await db.Guides.ToListAsync();
            Assert.NotEmpty(guides);
            Assert.Contains(guides, g => g.Status == GuideStatus.Published);
        }
    }

    [Fact]
    public async Task Guide_Cloning_Creates_Draft_With_Source_Metadata()
    {
        using (var db = CreateDb())
        {
            await KrizovkaDataSeeder.SeedAsync(db);

            var org = await db.Organizations.FirstAsync(x => !x.IsDefaultDispatch);
            var template = await db.GuideTemplates.FirstAsync(x => x.Id == "tmpl-fire-evacuation-v1");

            // Clone template into org
            var cloned = new Guide
            {
                Id = Guid.NewGuid(),
                OrganizationId = org.Id,
                SourceTemplateId = template.Id,
                SourceVersion = template.Version,
                Title = $"{template.Title} ({org.Name})",
                Category = template.Category,
                Summary = template.Summary,
                Status = GuideStatus.Draft,
                StepsJson = template.StepsJson,
                EmergencyContactsJson = template.EmergencyContactsJson,
                CreatedAt = DateTimeOffset.UtcNow
            };

            db.Guides.Add(cloned);
            await db.SaveChangesAsync();

            var fetched = await db.Guides.FindAsync(cloned.Id);
            Assert.NotNull(fetched);
            Assert.Equal(GuideStatus.Draft, fetched.Status);
            Assert.Equal("tmpl-fire-evacuation-v1", fetched.SourceTemplateId);
            Assert.Equal(1, fetched.SourceVersion);
            Assert.Equal(org.Id, fetched.OrganizationId);

            // Publishing changes status and records PublishedAt
            fetched.Status = GuideStatus.Published;
            fetched.PublishedAt = DateTimeOffset.UtcNow;
            await db.SaveChangesAsync();

            var published = await db.Guides.FindAsync(cloned.Id);
            Assert.NotNull(published);
            Assert.Equal(GuideStatus.Published, published.Status);
            Assert.NotNull(published.PublishedAt);
        }
    }

    [Fact]
    public async Task Organization_Area_Resolution_Handles_Core_Offset_And_Fallback()
    {
        using (var db = CreateDb())
        {
            await KrizovkaDataSeeder.SeedAsync(db);

            // 1. Point in core Kampus Sever (50.087, 14.420)
            var orgs = await db.Organizations.Include(x => x.Areas).ToListAsync();
            var coreArea = orgs.First(o => o.Code == "KAMPUS").Areas.First();

            var (insideCore, _, _) = LocationResolver.EvaluateArea(50.087, 14.420, coreArea.PolygonGeoJson, coreArea.OffsetMeters);
            Assert.True(insideCore);

            // 2. Point in offset of Kampus Sever (e.g. 50.0902, 14.420)
            var (insideOffsetCore, withinOffset, dist) = LocationResolver.EvaluateArea(50.0902, 14.420, coreArea.PolygonGeoJson, coreArea.OffsetMeters);
            Assert.False(insideOffsetCore);
            Assert.True(withinOffset);
            Assert.True(dist <= coreArea.OffsetMeters);

            // 3. Point far away (e.g. Brno: 49.195, 16.606)
            var (insideBrno, withinOffsetBrno, _) = LocationResolver.EvaluateArea(49.195, 16.606, coreArea.PolygonGeoJson, coreArea.OffsetMeters);
            Assert.False(insideBrno);
            Assert.False(withinOffsetBrno);

            // Fallback should select default dispatch
            var fallback = orgs.FirstOrDefault(o => o.IsDefaultDispatch);
            Assert.NotNull(fallback);
            Assert.Equal("KAMPUS", fallback.Code);
        }
    }

    [Fact]
    public async Task Catalog_Contains_All_36_Methodologies_With_Diagrams_And_Summaries()
    {
        using (var db = CreateDb())
        {
            await KrizovkaDataSeeder.SeedAsync(db);

            var templates = await db.GuideTemplates.ToListAsync();
            // 36 standard methodologies + 3 legacy compatibility
            Assert.True(templates.Count >= 36);

            var amok = templates.FirstOrDefault(t => t.Id == "tmpl-met-01-amok");
            Assert.NotNull(amok);
            Assert.Contains("UTEČ", amok.QuickSummary);
            Assert.Contains("flowchart TD", amok.DiagramMermaid);
            Assert.Contains("AMOK", amok.LegalBasisJson);

            // Verify all MET-01 to MET-36 have quickSummary and diagramMermaid
            for (int i = 1; i <= 36; i++)
            {
                var id = $"tmpl-met-{i:D2}";
                var tmpl = templates.FirstOrDefault(t => t.Id.StartsWith(id));
                Assert.True(tmpl != null, $"Methodology {id} should exist in catalog");
                Assert.False(string.IsNullOrWhiteSpace(tmpl!.QuickSummary), $"{id} must have QuickSummary");
                Assert.False(string.IsNullOrWhiteSpace(tmpl.DiagramMermaid), $"{id} must have DiagramMermaid");
                Assert.False(string.IsNullOrWhiteSpace(tmpl.LegalBasisJson), $"{id} must have LegalBasisJson");
            }
        }
    }
}
