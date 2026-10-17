using Krizovka.Api.Data;
using Microsoft.EntityFrameworkCore;

namespace Krizovka.Api.Organizations;

public static class OrganizationEndpoints
{
    public static IEndpointRouteBuilder MapOrganizationEndpoints(this IEndpointRouteBuilder routes)
    {
        var group = routes.MapGroup("/api/organizations");

        group.MapGet("/", async (KrizovkaDbContext db, CancellationToken ct) =>
        {
            var orgs = await db.Organizations
                .AsNoTracking()
                .Include(x => x.Areas)
                .OrderBy(x => x.Name)
                .ToListAsync(ct);

            return Results.Ok(orgs.Select(ToDto));
        });

        group.MapGet("/{id:guid}", async (Guid id, KrizovkaDbContext db, CancellationToken ct) =>
        {
            var org = await db.Organizations
                .AsNoTracking()
                .Include(x => x.Areas)
                .FirstOrDefaultAsync(x => x.Id == id, ct);

            return org is null ? Results.NotFound() : Results.Ok(ToDto(org));
        });

        group.MapPost("/", async (CreateOrganizationRequest request, KrizovkaDbContext db, CancellationToken ct) =>
        {
            if (string.IsNullOrWhiteSpace(request.Name) || string.IsNullOrWhiteSpace(request.Code))
            {
                return Results.Problem(
                    title: "Neplatný požadavek",
                    detail: "Název a kód organizace jsou povinné.",
                    statusCode: StatusCodes.Status400BadRequest);
            }

            // If new org is default dispatch, unset previous default dispatch
            if (request.IsDefaultDispatch)
            {
                await db.Organizations
                    .Where(x => x.IsDefaultDispatch)
                    .ExecuteUpdateAsync(s => s.SetProperty(x => x.IsDefaultDispatch, false), ct);
            }

            var org = new Organization
            {
                Id = Guid.NewGuid(),
                Name = request.Name.Trim(),
                Code = request.Code.Trim().ToUpperInvariant(),
                Status = string.IsNullOrWhiteSpace(request.Status) ? "Active" : request.Status.Trim(),
                ContactEmail = request.ContactEmail?.Trim(),
                ContactPhone = request.ContactPhone?.Trim(),
                IsDefaultDispatch = request.IsDefaultDispatch,
                CreatedAt = DateTimeOffset.UtcNow
            };

            db.Organizations.Add(org);
            await db.SaveChangesAsync(ct);

            return Results.Created($"/api/organizations/{org.Id}", ToDto(org));
        }).RequireAuthorization("AdminPolicy");

        group.MapPut("/{id:guid}", async (Guid id, UpdateOrganizationRequest request, KrizovkaDbContext db, CancellationToken ct) =>
        {
            var org = await db.Organizations.Include(x => x.Areas).FirstOrDefaultAsync(x => x.Id == id, ct);
            if (org is null) return Results.NotFound();

            if (request.IsDefaultDispatch && !org.IsDefaultDispatch)
            {
                await db.Organizations
                    .Where(x => x.Id != id && x.IsDefaultDispatch)
                    .ExecuteUpdateAsync(s => s.SetProperty(x => x.IsDefaultDispatch, false), ct);
            }

            org.Name = request.Name.Trim();
            org.Code = request.Code.Trim().ToUpperInvariant();
            org.Status = request.Status.Trim();
            org.ContactEmail = request.ContactEmail?.Trim();
            org.ContactPhone = request.ContactPhone?.Trim();
            org.IsDefaultDispatch = request.IsDefaultDispatch;
            org.UpdatedAt = DateTimeOffset.UtcNow;

            await db.SaveChangesAsync(ct);
            return Results.Ok(ToDto(org));
        }).RequireAuthorization("AdminPolicy");

        group.MapPost("/{id:guid}/areas", async (Guid id, UpsertOrganizationAreaRequest request, KrizovkaDbContext db, CancellationToken ct) =>
        {
            var org = await db.Organizations.FirstOrDefaultAsync(x => x.Id == id, ct);
            if (org is null) return Results.NotFound();

            var area = new OrganizationArea
            {
                Id = Guid.NewGuid(),
                OrganizationId = id,
                Name = request.Name.Trim(),
                PolygonGeoJson = request.PolygonGeoJson.Trim(),
                OffsetMeters = request.OffsetMeters >= 0 ? request.OffsetMeters : 0,
                AllocatedVelinId = string.IsNullOrWhiteSpace(request.AllocatedVelinId) ? "default-velin" : request.AllocatedVelinId.Trim(),
                CreatedAt = DateTimeOffset.UtcNow
            };

            db.OrganizationAreas.Add(area);
            await db.SaveChangesAsync(ct);

            return Results.Created($"/api/organizations/{id}/areas/{area.Id}", ToAreaDto(area));
        }).RequireAuthorization("AdminPolicy");

        group.MapDelete("/{id:guid}/areas/{areaId:guid}", async (Guid id, Guid areaId, KrizovkaDbContext db, CancellationToken ct) =>
        {
            var area = await db.OrganizationAreas.FirstOrDefaultAsync(x => x.OrganizationId == id && x.Id == areaId, ct);
            if (area is null) return Results.NotFound();

            db.OrganizationAreas.Remove(area);
            await db.SaveChangesAsync(ct);
            return Results.NoContent();
        }).RequireAuthorization("AdminPolicy");

        group.MapGet("/resolve-location", async (double? lat, double? lon, KrizovkaDbContext db, CancellationToken ct) =>
        {
            if (lat is null || lon is null)
            {
                return Results.BadRequest(new { error = "Parametry 'lat' a 'lon' jsou povinné." });
            }

            var orgs = await db.Organizations
                .AsNoTracking()
                .Include(x => x.Areas)
                .Where(x => x.Status == "Active")
                .ToListAsync(ct);

            // First: Check for core polygon matches
            foreach (var org in orgs)
            {
                foreach (var area in org.Areas)
                {
                    var (isInside, _, _) = LocationResolver.EvaluateArea(lat.Value, lon.Value, area.PolygonGeoJson, area.OffsetMeters);
                    if (isInside)
                    {
                        return Results.Ok(new ResolveLocationResult(
                            OrganizationId: org.Id,
                            OrganizationName: org.Name,
                            AreaId: area.Id,
                            AreaName: area.Name,
                            AllocatedVelinId: area.AllocatedVelinId,
                            IsFallback: false,
                            IsWithinCore: true,
                            IsWithinOffset: false,
                            DistanceToAreaMeters: 0.0
                        ));
                    }
                }
            }

            // Second: Check for offset buffer matches (closest area within offset)
            Organization? bestOrg = null;
            OrganizationArea? bestArea = null;
            double minOffsetDist = double.MaxValue;

            foreach (var org in orgs)
            {
                foreach (var area in org.Areas)
                {
                    var (_, isWithinOffset, distance) = LocationResolver.EvaluateArea(lat.Value, lon.Value, area.PolygonGeoJson, area.OffsetMeters);
                    if (isWithinOffset && distance < minOffsetDist)
                    {
                        minOffsetDist = distance;
                        bestOrg = org;
                        bestArea = area;
                    }
                }
            }

            if (bestOrg is not null && bestArea is not null)
            {
                return Results.Ok(new ResolveLocationResult(
                    OrganizationId: bestOrg.Id,
                    OrganizationName: bestOrg.Name,
                    AreaId: bestArea.Id,
                    AreaName: bestArea.Name,
                    AllocatedVelinId: bestArea.AllocatedVelinId,
                    IsFallback: false,
                    IsWithinCore: false,
                    IsWithinOffset: true,
                    DistanceToAreaMeters: Math.Round(minOffsetDist, 1)
                ));
            }

            // Third: Fallback to default dispatch organization
            var fallbackOrg = orgs.FirstOrDefault(x => x.IsDefaultDispatch) ?? orgs.FirstOrDefault();
            if (fallbackOrg is not null)
            {
                return Results.Ok(new ResolveLocationResult(
                    OrganizationId: fallbackOrg.Id,
                    OrganizationName: fallbackOrg.Name,
                    AreaId: null,
                    AreaName: null,
                    AllocatedVelinId: "default-velin",
                    IsFallback: true,
                    IsWithinCore: false,
                    IsWithinOffset: false,
                    DistanceToAreaMeters: null
                ));
            }

            return Results.NotFound(new { error = "Žádná aktivní organizace nebyla nalezena." });
        });

        return routes;
    }

    private static OrganizationDto ToDto(Organization org) => new(
        org.Id,
        org.Name,
        org.Code,
        org.Status,
        org.ContactEmail,
        org.ContactPhone,
        org.IsDefaultDispatch,
        org.CreatedAt,
        org.UpdatedAt,
        org.Areas.Select(ToAreaDto).ToList()
    );

    private static OrganizationAreaDto ToAreaDto(OrganizationArea area) => new(
        area.Id,
        area.OrganizationId,
        area.Name,
        area.PolygonGeoJson,
        area.OffsetMeters,
        area.AllocatedVelinId,
        area.CreatedAt,
        area.UpdatedAt
    );
}
