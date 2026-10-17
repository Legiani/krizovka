using System.Text.Json;
using Krizovka.Api.Guides;
using Krizovka.Api.Organizations;
using Microsoft.EntityFrameworkCore;

namespace Krizovka.Api.Data;

public static class KrizovkaDataSeeder
{
    public static readonly Guid DefaultOrgId = Guid.Parse("11111111-1111-1111-1111-111111111111");
    public static readonly Guid SecondaryOrgId = Guid.Parse("22222222-2222-2222-2222-222222222222");

    public static async Task SeedAsync(KrizovkaDbContext db)
    {
        // 1. Seed Organizations if none
        if (!db.Organizations.Any())
        {
            var org1 = new Organization
            {
                Id = DefaultOrgId,
                Name = "Ukázkový kampus Sever",
                Code = "KAMPUS",
                Status = "Active",
                ContactEmail = "velin@kampus-sever.cz",
                ContactPhone = "+420 224 000 112",
                IsDefaultDispatch = true,
                CreatedAt = DateTimeOffset.UtcNow,
                Areas =
                [
                    new OrganizationArea
                    {
                        Id = Guid.NewGuid(),
                        OrganizationId = DefaultOrgId,
                        Name = "Areál Kampus Sever - Hlavní budova a parkoviště",
                        PolygonGeoJson = JsonSerializer.Serialize(new
                        {
                            type = "Polygon",
                            coordinates = new[]
                            {
                                new[]
                                {
                                    new[] { 14.4150, 50.0850 },
                                    new[] { 14.4250, 50.0850 },
                                    new[] { 14.4250, 50.0900 },
                                    new[] { 14.4150, 50.0900 },
                                    new[] { 14.4150, 50.0850 }
                                }
                            }
                        }),
                        OffsetMeters = 50.0,
                        AllocatedVelinId = "velin-kampus-sever",
                        CreatedAt = DateTimeOffset.UtcNow
                    }
                ]
            };

            var org2 = new Organization
            {
                Id = SecondaryOrgId,
                Name = "Ukázkové centrum",
                Code = "CENTRUM",
                Status = "Active",
                ContactEmail = "dispecink@centrum.cz",
                ContactPhone = "+420 224 000 222",
                IsDefaultDispatch = false,
                CreatedAt = DateTimeOffset.UtcNow,
                Areas =
                [
                    new OrganizationArea
                    {
                        Id = Guid.NewGuid(),
                        OrganizationId = SecondaryOrgId,
                        Name = "Budova Centra",
                        PolygonGeoJson = JsonSerializer.Serialize(new
                        {
                            type = "Polygon",
                            coordinates = new[]
                            {
                                new[]
                                {
                                    new[] { 14.4300, 50.0750 },
                                    new[] { 14.4400, 50.0750 },
                                    new[] { 14.4400, 50.0800 },
                                    new[] { 14.4300, 50.0800 },
                                    new[] { 14.4300, 50.0750 }
                                }
                            }
                        }),
                        OffsetMeters = 30.0,
                        AllocatedVelinId = "velin-centrum",
                        CreatedAt = DateTimeOffset.UtcNow
                    }
                ]
            };

            db.Organizations.AddRange(org1, org2);
            await db.SaveChangesAsync();
        }

        // 2. Seed or update 40 GuideTemplates from JSON catalog
        var catalogPath = FindCatalogFile();
        if (File.Exists(catalogPath))
        {
            var json = await File.ReadAllTextAsync(catalogPath);
            var templates = JsonSerializer.Deserialize<List<GuideTemplate>>(json, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

            if (templates != null && templates.Count > 0)
            {
                var existingTemplates = await db.GuideTemplates.ToDictionaryAsync(t => t.Id);

                foreach (var t in templates)
                {
                    if (existingTemplates.TryGetValue(t.Id, out var existing))
                    {
                        existing.Title = t.Title;
                        existing.Category = t.Category;
                        existing.Summary = t.Summary;
                        existing.QuickSummary = t.QuickSummary;
                        existing.DiagramMermaid = t.DiagramMermaid;
                        existing.LegalBasisJson = t.LegalBasisJson;
                        existing.RecommendedOrgTypesJson = t.RecommendedOrgTypesJson;
                        existing.StepsJson = t.StepsJson;
                        existing.EmergencyContactsJson = t.EmergencyContactsJson;
                        existing.Version = t.Version;
                    }
                    else
                    {
                        t.CreatedAt = DateTimeOffset.UtcNow;
                        db.GuideTemplates.Add(t);
                    }
                }

                await db.SaveChangesAsync();
            }
        }

        // 3. Seed default organization guides if it has none
        if (!await db.Guides.AnyAsync(g => g.OrganizationId == DefaultOrgId))
        {
            var org = await db.Organizations.FirstOrDefaultAsync(o => o.Id == DefaultOrgId);
            if (org != null)
            {
                var starterTemplateIds = new[]
                {
                    "tmpl-met-01-amok",
                    "tmpl-met-15-pozar",
                    "tmpl-met-23-cpr",
                    "tmpl-met-29-ransomware",
                    "tmpl-met-36-reunification"
                };

                var templatesToClone = await db.GuideTemplates
                    .Where(t => starterTemplateIds.Contains(t.Id))
                    .ToListAsync();

                foreach (var tmpl in templatesToClone)
                {
                    var velinPhone = !string.IsNullOrWhiteSpace(org.ContactPhone) ? org.ContactPhone : "+420 224 000 112";
                    var steps = tmpl.StepsJson
                        .Replace("{VELIN}", velinPhone)
                        .Replace("{ORGANIZATION_NAME}", org.Name);
                    var contacts = tmpl.EmergencyContactsJson
                        .Replace("{VELIN}", velinPhone)
                        .Replace("{ORGANIZATION_NAME}", org.Name);

                    db.Guides.Add(new Guide
                    {
                        Id = Guid.NewGuid(),
                        OrganizationId = DefaultOrgId,
                        SourceTemplateId = tmpl.Id,
                        SourceVersion = tmpl.Version,
                        Title = $"{tmpl.Title} ({org.Name})",
                        Category = tmpl.Category,
                        Summary = tmpl.Summary,
                        QuickSummary = tmpl.QuickSummary,
                        DiagramMermaid = tmpl.DiagramMermaid,
                        LegalBasisJson = tmpl.LegalBasisJson,
                        Status = GuideStatus.Published,
                        StepsJson = steps,
                        EmergencyContactsJson = contacts,
                        PublishedAt = DateTimeOffset.UtcNow,
                        CreatedAt = DateTimeOffset.UtcNow
                    });
                }

                await db.SaveChangesAsync();
            }
        }
    }

    private static string FindCatalogFile()
    {
        var paths = new[]
        {
            Path.Combine(AppContext.BaseDirectory, "Data", "Seed", "guide_templates_catalog.json"),
            Path.Combine(AppContext.BaseDirectory, "guide_templates_catalog.json"),
            Path.Combine(Directory.GetCurrentDirectory(), "backend", "Krizovka.Api", "Data", "Seed", "guide_templates_catalog.json"),
            Path.Combine(Directory.GetCurrentDirectory(), "Data", "Seed", "guide_templates_catalog.json")
        };

        foreach (var p in paths)
        {
            if (File.Exists(p)) return p;
        }

        return paths[0];
    }
}
