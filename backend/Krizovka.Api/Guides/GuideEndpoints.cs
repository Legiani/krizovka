using Krizovka.Api.Data;
using Microsoft.EntityFrameworkCore;

namespace Krizovka.Api.Guides;

public static class GuideEndpoints
{
    public static IEndpointRouteBuilder MapGuideEndpoints(this IEndpointRouteBuilder routes)
    {
        // Central Guide Templates
        var templatesGroup = routes.MapGroup("/api/guide-templates");

        templatesGroup.MapGet("/", async (KrizovkaDbContext db, CancellationToken ct) =>
        {
            var templates = await db.GuideTemplates
                .AsNoTracking()
                .OrderBy(x => x.Category)
                .ThenBy(x => x.Title)
                .ToListAsync(ct);

            return Results.Ok(templates.Select(ToTemplateDto));
        });

        templatesGroup.MapGet("/{id}", async (string id, KrizovkaDbContext db, CancellationToken ct) =>
        {
            var template = await db.GuideTemplates.AsNoTracking().FirstOrDefaultAsync(x => x.Id == id, ct);
            return template is null ? Results.NotFound() : Results.Ok(ToTemplateDto(template));
        });

        // Organization Guides
        var guidesGroup = routes.MapGroup("/api/guides");

        guidesGroup.MapGet("/", async (Guid? organizationId, GuideStatus? status, KrizovkaDbContext db, CancellationToken ct) =>
        {
            var query = db.Guides.AsNoTracking().AsQueryable();

            if (organizationId.HasValue)
            {
                query = query.Where(x => x.OrganizationId == organizationId.Value);
            }

            if (status.HasValue)
            {
                query = query.Where(x => x.Status == status.Value);
            }

            var guides = await query
                .OrderBy(x => x.Category)
                .ThenBy(x => x.Title)
                .ToListAsync(ct);

            return Results.Ok(guides.Select(ToGuideDto));
        });

        guidesGroup.MapGet("/{id:guid}", async (Guid id, KrizovkaDbContext db, CancellationToken ct) =>
        {
            var guide = await db.Guides.AsNoTracking().FirstOrDefaultAsync(x => x.Id == id, ct);
            return guide is null ? Results.NotFound() : Results.Ok(ToGuideDto(guide));
        });

        guidesGroup.MapPost("/", async (CreateGuideRequest request, KrizovkaDbContext db, CancellationToken ct) =>
        {
            if (string.IsNullOrWhiteSpace(request.Title) || string.IsNullOrWhiteSpace(request.Category))
            {
                return Results.Problem(
                    title: "Neplatný požadavek",
                    detail: "Název a kategorie jsou povinné.",
                    statusCode: StatusCodes.Status400BadRequest);
            }

            var guide = new Guide
            {
                Id = Guid.NewGuid(),
                OrganizationId = request.OrganizationId,
                Title = request.Title.Trim(),
                Category = request.Category.Trim(),
                Summary = request.Summary?.Trim() ?? string.Empty,
                QuickSummary = request.QuickSummary?.Trim() ?? string.Empty,
                DiagramMermaid = request.DiagramMermaid?.Trim() ?? string.Empty,
                LegalBasisJson = string.IsNullOrWhiteSpace(request.LegalBasisJson) ? "[]" : request.LegalBasisJson.Trim(),
                Status = GuideStatus.Draft,
                StepsJson = string.IsNullOrWhiteSpace(request.StepsJson) ? "[]" : request.StepsJson.Trim(),
                EmergencyContactsJson = string.IsNullOrWhiteSpace(request.EmergencyContactsJson) ? "[]" : request.EmergencyContactsJson.Trim(),
                CreatedAt = DateTimeOffset.UtcNow
            };

            db.Guides.Add(guide);
            await db.SaveChangesAsync(ct);

            return Results.Created($"/api/guides/{guide.Id}", ToGuideDto(guide));
        }).RequireAuthorization("AdminPolicy");

        guidesGroup.MapPut("/{id:guid}", async (Guid id, UpdateGuideRequest request, KrizovkaDbContext db, CancellationToken ct) =>
        {
            var guide = await db.Guides.FirstOrDefaultAsync(x => x.Id == id, ct);
            if (guide is null) return Results.NotFound();

            guide.Title = request.Title.Trim();
            guide.Category = request.Category.Trim();
            guide.Summary = request.Summary?.Trim() ?? string.Empty;
            if (request.QuickSummary is not null) guide.QuickSummary = request.QuickSummary.Trim();
            if (request.DiagramMermaid is not null) guide.DiagramMermaid = request.DiagramMermaid.Trim();
            if (request.LegalBasisJson is not null) guide.LegalBasisJson = request.LegalBasisJson.Trim();
            if (request.Status.HasValue) guide.Status = request.Status.Value;
            if (request.StepsJson is not null) guide.StepsJson = request.StepsJson.Trim();
            if (request.EmergencyContactsJson is not null) guide.EmergencyContactsJson = request.EmergencyContactsJson.Trim();
            guide.UpdatedAt = DateTimeOffset.UtcNow;

            await db.SaveChangesAsync(ct);
            return Results.Ok(ToGuideDto(guide));
        }).RequireAuthorization("AdminPolicy");

        static string InterpolateOrg(string templateText, Organizations.Organization org)
        {
            if (string.IsNullOrEmpty(templateText)) return templateText;
            var velinPhone = !string.IsNullOrWhiteSpace(org.ContactPhone) ? org.ContactPhone : "+420 224 000 112";
            return templateText
                .Replace("{VELIN}", velinPhone)
                .Replace("{ORGANIZATION_NAME}", org.Name)
                .Replace("{CONTACT_EMAIL}", org.ContactEmail ?? "")
                .Replace("{CONTACT_PHONE}", org.ContactPhone ?? "");
        }

        // 1-click clone from Central Template
        guidesGroup.MapPost("/clone", async (CloneGuideRequest request, KrizovkaDbContext db, CancellationToken ct) =>
        {
            var template = await db.GuideTemplates.AsNoTracking().FirstOrDefaultAsync(x => x.Id == request.TemplateId, ct);
            if (template is null)
            {
                return Results.Problem(
                    title: "Nenalezeno",
                    detail: $"Šablona '{request.TemplateId}' nebyla nalezena.",
                    statusCode: StatusCodes.Status404NotFound);
            }

            var org = await db.Organizations.AsNoTracking().FirstOrDefaultAsync(x => x.Id == request.OrganizationId, ct);
            if (org is null)
            {
                return Results.Problem(
                    title: "Nenalezeno",
                    detail: $"Organizace '{request.OrganizationId}' nebyla nalezena.",
                    statusCode: StatusCodes.Status404NotFound);
            }

            var guide = new Guide
            {
                Id = Guid.NewGuid(),
                OrganizationId = request.OrganizationId,
                SourceTemplateId = template.Id,
                SourceVersion = template.Version,
                Title = $"{template.Title} ({org.Name})",
                Category = template.Category,
                Summary = template.Summary,
                QuickSummary = template.QuickSummary,
                DiagramMermaid = template.DiagramMermaid,
                LegalBasisJson = template.LegalBasisJson,
                Status = GuideStatus.Draft,
                StepsJson = InterpolateOrg(template.StepsJson, org),
                EmergencyContactsJson = InterpolateOrg(template.EmergencyContactsJson, org),
                CreatedAt = DateTimeOffset.UtcNow
            };

            db.Guides.Add(guide);
            await db.SaveChangesAsync(ct);

            return Results.Created($"/api/guides/{guide.Id}", ToGuideDto(guide));
        }).RequireAuthorization("AdminPolicy");

        // Batch clone multiple templates
        guidesGroup.MapPost("/clone-batch", async (BatchCloneGuidesRequest request, KrizovkaDbContext db, CancellationToken ct) =>
        {
            var org = await db.Organizations.AsNoTracking().FirstOrDefaultAsync(x => x.Id == request.OrganizationId, ct);
            if (org is null)
            {
                return Results.Problem(
                    title: "Nenalezeno",
                    detail: $"Organizace '{request.OrganizationId}' nebyla nalezena.",
                    statusCode: StatusCodes.Status404NotFound);
            }

            var templates = await db.GuideTemplates
                .AsNoTracking()
                .Where(x => request.TemplateIds.Contains(x.Id))
                .ToListAsync(ct);

            var createdGuides = new List<Guide>();
            foreach (var template in templates)
            {
                var guide = new Guide
                {
                    Id = Guid.NewGuid(),
                    OrganizationId = request.OrganizationId,
                    SourceTemplateId = template.Id,
                    SourceVersion = template.Version,
                    Title = $"{template.Title} ({org.Name})",
                    Category = template.Category,
                    Summary = template.Summary,
                    QuickSummary = template.QuickSummary,
                    DiagramMermaid = template.DiagramMermaid,
                    LegalBasisJson = template.LegalBasisJson,
                    Status = GuideStatus.Draft,
                    StepsJson = InterpolateOrg(template.StepsJson, org),
                    EmergencyContactsJson = InterpolateOrg(template.EmergencyContactsJson, org),
                    CreatedAt = DateTimeOffset.UtcNow
                };
                createdGuides.Add(guide);
            }

            db.Guides.AddRange(createdGuides);
            await db.SaveChangesAsync(ct);

            return Results.Ok(createdGuides.Select(ToGuideDto));
        }).RequireAuthorization("AdminPolicy");

        guidesGroup.MapPost("/{id:guid}/publish", async (Guid id, KrizovkaDbContext db, CancellationToken ct) =>
        {
            var guide = await db.Guides.FirstOrDefaultAsync(x => x.Id == id, ct);
            if (guide is null) return Results.NotFound();

            guide.Status = GuideStatus.Published;
            guide.PublishedAt = DateTimeOffset.UtcNow;
            guide.UpdatedAt = DateTimeOffset.UtcNow;

            await db.SaveChangesAsync(ct);
            return Results.Ok(ToGuideDto(guide));
        }).RequireAuthorization("AdminPolicy");

        guidesGroup.MapPost("/{id:guid}/archive", async (Guid id, KrizovkaDbContext db, CancellationToken ct) =>
        {
            var guide = await db.Guides.FirstOrDefaultAsync(x => x.Id == id, ct);
            if (guide is null) return Results.NotFound();

            guide.Status = GuideStatus.Archived;
            guide.UpdatedAt = DateTimeOffset.UtcNow;

            await db.SaveChangesAsync(ct);
            return Results.Ok(ToGuideDto(guide));
        }).RequireAuthorization("AdminPolicy");

        guidesGroup.MapDelete("/{id:guid}", async (Guid id, KrizovkaDbContext db, CancellationToken ct) =>
        {
            var guide = await db.Guides.FirstOrDefaultAsync(x => x.Id == id, ct);
            if (guide is null) return Results.NotFound();

            db.Guides.Remove(guide);
            await db.SaveChangesAsync(ct);
            return Results.NoContent();
        }).RequireAuthorization("AdminPolicy");

        return routes;
    }

    private static GuideTemplateDto ToTemplateDto(GuideTemplate t) => new(
        t.Id,
        t.Title,
        t.Category,
        t.Summary,
        t.QuickSummary,
        t.DiagramMermaid,
        t.LegalBasisJson,
        t.RecommendedOrgTypesJson,
        t.Version,
        t.StepsJson,
        t.EmergencyContactsJson,
        t.CreatedAt
    );

    private static GuideDto ToGuideDto(Guide g) => new(
        g.Id,
        g.OrganizationId,
        g.SourceTemplateId,
        g.SourceVersion,
        g.Title,
        g.Category,
        g.Summary,
        g.QuickSummary,
        g.DiagramMermaid,
        g.LegalBasisJson,
        g.Status,
        g.StepsJson,
        g.EmergencyContactsJson,
        g.PublishedAt,
        g.CreatedAt,
        g.UpdatedAt
    );
}
