using System.Security.Claims;
using Krizovka.Api.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Krizovka.Api.Reports;

public static class ReportEndpoints
{
    public static IEndpointRouteBuilder MapReportEndpoints(this IEndpointRouteBuilder app)
    {
        RegisterReportEndpoints(app.MapGroup("/api/reports"));
        RegisterReportEndpoints(app.MapGroup("/reports"));
        return app;
    }

    private static readonly System.Collections.Concurrent.ConcurrentDictionary<string, SemaphoreSlim> _reportCreationLocks = new();

    private static void RegisterReportEndpoints(RouteGroupBuilder group)
    {
        // 1. Submit emergency report (Klient POTŘEBUJI POMOC) - Anonymní / nestresující pro uživatele
        group.MapPost("", async (
            [FromBody] CreateReportRequest request,
            KrizovkaDbContext db,
            HttpContext httpContext,
            CancellationToken ct) =>
        {
            if (request.ClientReportId == Guid.Empty)
            {
                return Results.Problem(
                    title: "Neplatný požadavek",
                    detail: "ClientReportId je povinné pro zajištění idempotence.",
                    statusCode: StatusCodes.Status400BadRequest);
            }

            if (request.OrganizationId == Guid.Empty)
            {
                return Results.Problem(
                    title: "Neplatný požadavek",
                    detail: "OrganizationId je povinné.",
                    statusCode: StatusCodes.Status400BadRequest);
            }

            var lockKey = $"{request.OrganizationId}:{request.ClientReportId}";
            var semaphore = _reportCreationLocks.GetOrAdd(lockKey, _ => new SemaphoreSlim(1, 1));
            await semaphore.WaitAsync(ct);

            try
            {
                // Check idempotency: OrganizationId + ClientReportId
                var existing = await db.Reports
                    .Include(r => r.History)
                    .Include(r => r.Attachments)
                    .FirstOrDefaultAsync(r => r.OrganizationId == request.OrganizationId && r.ClientReportId == request.ClientReportId, ct);

                if (existing != null)
                {
                    httpContext.Response.Headers.Location = $"/api/reports/{existing.Id}";
                    return Results.Ok(ToDetailDto(existing));
                }

                // Reporter Account ID from auth context if authenticated, otherwise empty
                var reporterAccountId = Guid.TryParse(httpContext.User.FindFirst("AccountId")?.Value, out var parsedAccountId)
                    ? parsedAccountId
                    : Guid.Empty;

                // Capture as much device and connection telemetry as possible for traceability
                var forwardedFor = httpContext.Request.Headers["X-Forwarded-For"].FirstOrDefault();
                var ipAddress = !string.IsNullOrWhiteSpace(forwardedFor)
                    ? forwardedFor.Split(',')[0].Trim()
                    : httpContext.Connection.RemoteIpAddress?.ToString();

                var userAgent = httpContext.Request.Headers.UserAgent.ToString();
                if (string.IsNullOrWhiteSpace(userAgent))
                {
                    userAgent = httpContext.Request.Headers["X-Device-Info"].FirstOrDefault();
                }

                var report = Issue.Create(
                    organizationId: request.OrganizationId,
                    reporterAccountId: reporterAccountId,
                    clientReportId: request.ClientReportId,
                    description: request.Description,
                    latitude: request.Latitude,
                    longitude: request.Longitude,
                    accuracyMeters: request.AccuracyMeters,
                    locationMeasuredAt: request.LocationMeasuredAt,
                    contact: request.Contact, // Může být neúplný / prázdný, SOS hlášení to nesmí blokovat
                    ipAddress: ipAddress,
                    userAgent: userAgent
                );

                try
                {
                    db.Reports.Add(report);
                    await db.SaveChangesAsync(ct);

                    return Results.Created($"/api/reports/{report.Id}", ToDetailDto(report));
                }
                catch (DbUpdateException)
                {
                    // Concurrency race: parallel request already inserted this (OrganizationId, ClientReportId)
                    db.ChangeTracker.Clear();
                    var concurrentExisting = await db.Reports
                        .Include(r => r.History)
                        .Include(r => r.Attachments)
                        .FirstOrDefaultAsync(r => r.OrganizationId == request.OrganizationId && r.ClientReportId == request.ClientReportId, ct);

                    if (concurrentExisting != null)
                    {
                        httpContext.Response.Headers.Location = $"/api/reports/{concurrentExisting.Id}";
                        return Results.Ok(ToDetailDto(concurrentExisting));
                    }

                    throw;
                }
            }
            finally
            {
                semaphore.Release();
            }
        }).AllowAnonymous();

        // 2. List reports for Velín - vyžaduje autorizaci operátora + tenant scoping
        group.MapGet("", async (
            [FromQuery] Guid? organizationId,
            [FromQuery] string? status,
            KrizovkaDbContext db,
            HttpContext httpContext,
            CancellationToken ct) =>
        {
            var callerOrgClaim = httpContext.User.FindFirst("OrganizationId")?.Value
                ?? httpContext.User.FindFirst("org_id")?.Value;
            Guid.TryParse(callerOrgClaim, out var callerOrgId);

            var isSuperAdmin = httpContext.User.IsInRole("SuperAdmin");

            var query = db.Reports.AsNoTracking().AsQueryable();

            if (!isSuperAdmin)
            {
                if (callerOrgId == Guid.Empty)
                {
                    return Results.Problem(
                        title: "Přístup odepřen",
                        detail: "Operátor nemá přiřazenou platnou organizaci.",
                        statusCode: StatusCodes.Status403Forbidden);
                }

                query = query.Where(r => r.OrganizationId == callerOrgId);
            }
            else
            {
                if (organizationId.HasValue && organizationId.Value != Guid.Empty)
                {
                    query = query.Where(r => r.OrganizationId == organizationId.Value);
                }
            }

            if (!string.IsNullOrWhiteSpace(status) && Enum.TryParse<ReportStatus>(status, true, out var parsedStatus))
            {
                query = query.Where(r => r.Status == parsedStatus);
            }

            // Oprava paměťového memory leaku: řazení a limit se provádí přímo v databázovém dotazu
            var list = await query
                .OrderByDescending(r => r.CreatedAt)
                .Take(100)
                .Select(r => new ReportSummaryDto(
                    r.Id,
                    r.OrganizationId,
                    r.ClientReportId,
                    r.Description,
                    r.Status,
                    r.CreatedAt,
                    r.UpdatedAt,
                    r.Latitude,
                    r.Longitude,
                    r.AccuracyMeters,
                    r.LocationMeasuredAt,
                    r.Contact,
                    r.IpAddress
                ))
                .ToListAsync(ct);

            return Results.Ok(list);
        }).RequireAuthorization("OperatorPolicy");

        // 3. Detail of report
        group.MapGet("{id:guid}", async (
            Guid id,
            KrizovkaDbContext db,
            HttpContext httpContext,
            CancellationToken ct) =>
        {
            var report = await db.Reports
                .AsNoTracking()
                .Include(r => r.History)
                .Include(r => r.Attachments)
                .FirstOrDefaultAsync(r => r.Id == id, ct);

            if (report == null)
            {
                return Results.Problem(
                    title: "Nenalezeno",
                    detail: $"Hlášení {id} nebylo nalezeno.",
                    statusCode: StatusCodes.Status404NotFound);
            }

            if (httpContext.User.Identity?.IsAuthenticated == true)
            {
                var isSuperAdmin = httpContext.User.IsInRole("SuperAdmin");
                if (!isSuperAdmin)
                {
                    var callerOrgClaim = httpContext.User.FindFirst("OrganizationId")?.Value
                        ?? httpContext.User.FindFirst("org_id")?.Value;
                    Guid.TryParse(callerOrgClaim, out var callerOrgId);

                    if (callerOrgId == Guid.Empty || report.OrganizationId != callerOrgId)
                    {
                        return Results.Problem(
                            title: "Přístup odepřen",
                            detail: "Hlášení patří jiné organizaci.",
                            statusCode: StatusCodes.Status403Forbidden);
                    }
                }
            }

            return Results.Ok(ToDetailDto(report));
        }).AllowAnonymous();

        // 4. Update status (Převzít / Uzavřít operátorem) - Vyžaduje autorizaci
        group.MapPost("{id:guid}/status", async (
            Guid id,
            [FromBody] UpdateReportStatusRequest request,
            KrizovkaDbContext db,
            HttpContext httpContext,
            CancellationToken ct) =>
        {
            var report = await db.Reports
                .Include(r => r.History)
                .Include(r => r.Attachments)
                .FirstOrDefaultAsync(r => r.Id == id, ct);

            if (report == null)
            {
                return Results.Problem(
                    title: "Nenalezeno",
                    detail: $"Hlášení {id} nebylo nalezeno.",
                    statusCode: StatusCodes.Status404NotFound);
            }

            var callerOrgClaim = httpContext.User.FindFirst("OrganizationId")?.Value
                ?? httpContext.User.FindFirst("org_id")?.Value;
            Guid.TryParse(callerOrgClaim, out var callerOrgId);
            var isSuperAdmin = httpContext.User.IsInRole("SuperAdmin");

            if (!isSuperAdmin && report.OrganizationId != callerOrgId)
            {
                return Results.Problem(
                    title: "Přístup odepřen",
                    detail: "Hlášení patří jiné organizaci.",
                    statusCode: StatusCodes.Status403Forbidden);
            }

            var actorId = Guid.TryParse(httpContext.User.FindFirst("AccountId")?.Value, out var parsedActor)
                ? parsedActor
                : Guid.Empty;

            report.ChangeStatus(request.Status, actorId, request.Note);
            await db.SaveChangesAsync(ct);

            return Results.Ok(ToDetailDto(report));
        }).RequireAuthorization("OperatorPolicy");

        // 5. Update location (dodatečné zaslání přesné GPS z klienta)
        group.MapPost("{id:guid}/location", async (
            Guid id,
            [FromBody] UpdateReportLocationRequest request,
            KrizovkaDbContext db,
            CancellationToken ct) =>
        {
            var report = await db.Reports
                .Include(r => r.History)
                .FirstOrDefaultAsync(r => r.Id == id, ct);

            if (report == null)
            {
                return Results.Problem(
                    title: "Nenalezeno",
                    detail: $"Hlášení {id} nebylo nalezeno.",
                    statusCode: StatusCodes.Status404NotFound);
            }

            report.UpdateLocation(request.Latitude, request.Longitude, request.AccuracyMeters, request.LocationMeasuredAt);
            await db.SaveChangesAsync(ct);

            return Results.Ok(ToDetailDto(report));
        }).AllowAnonymous();

        // 6. Upload attachment (Fotografie nebo hlasová zpráva)
        group.MapPost("{id:guid}/attachments", async (
            Guid id,
            IFormFile file,
            KrizovkaDbContext db,
            IAttachmentStorage storage,
            HttpContext httpContext,
            CancellationToken ct) =>
        {
            if (file == null || file.Length == 0)
            {
                return Results.Problem(
                    title: "Neplatný soubor",
                    detail: "Soubor nebyl poskytnut nebo je prázdný.",
                    statusCode: StatusCodes.Status400BadRequest);
            }

            const long maxFileSize = 25 * 1024 * 1024; // 25 MB
            if (file.Length > maxFileSize)
            {
                return Results.Problem(
                    title: "Příliš velký soubor",
                    detail: "Velikost souboru přesahuje povolený limit 25 MB.",
                    statusCode: StatusCodes.Status400BadRequest);
            }

            var report = await db.Reports
                .Include(r => r.Attachments)
                .Include(r => r.History)
                .FirstOrDefaultAsync(r => r.Id == id, ct);

            if (report == null)
            {
                return Results.Problem(
                    title: "Nenalezeno",
                    detail: $"Hlášení {id} nebylo nalezeno.",
                    statusCode: StatusCodes.Status404NotFound);
            }

            var callerOrgClaim = httpContext.User.FindFirst("OrganizationId")?.Value
                ?? httpContext.User.FindFirst("org_id")?.Value;
            if (Guid.TryParse(callerOrgClaim, out var callerOrgId) && callerOrgId != Guid.Empty)
            {
                var isSuperAdmin = httpContext.User.IsInRole("SuperAdmin");
                if (!isSuperAdmin && report.OrganizationId != callerOrgId)
                {
                    return Results.Problem(
                        title: "Přístup odepřen",
                        detail: "Nemáte oprávnění nahrávat přílohy k hlášení jiné organizace.",
                        statusCode: StatusCodes.Status403Forbidden);
                }
            }

            var actorId = Guid.TryParse(httpContext.User.FindFirst("AccountId")?.Value, out var parsedActor)
                ? parsedActor
                : Guid.Empty;

            var attachmentId = Guid.NewGuid();
            var now = DateTimeOffset.UtcNow;
            var fileName = Path.GetFileName(file.FileName);
            var contentType = string.IsNullOrWhiteSpace(file.ContentType) ? "application/octet-stream" : file.ContentType;

            await using var stream = file.OpenReadStream();
            var storageKey = await storage.SaveAttachmentAsync(id, attachmentId, fileName, stream, ct);

            try
            {
                var attachment = new IssueAttachment
                {
                    Id = attachmentId,
                    IssueId = id,
                    StorageKey = storageKey,
                    FileName = fileName,
                    ContentType = contentType,
                    SizeBytes = file.Length,
                    CreatedAt = now
                };

                report.AddAttachment(attachment, actorId);
                db.Set<IssueAttachment>().Add(attachment);
                await db.SaveChangesAsync(ct);

                return Results.Created($"/api/reports/{id}/attachments/{attachment.Id}", new ReportAttachmentDto(
                    attachment.Id,
                    attachment.FileName,
                    attachment.ContentType,
                    attachment.SizeBytes,
                    attachment.StorageKey,
                    attachment.CreatedAt
                ));
            }
            catch
            {
                // Ochrana před sirotčími soubory na disku při selhání DB
                await storage.DeleteAttachmentAsync(storageKey, CancellationToken.None);
                throw;
            }
        })
        .DisableAntiforgery()
        .AllowAnonymous();

        // 7. Download / Stream attachment
        group.MapGet("{id:guid}/attachments/{attachmentId:guid}", async (
            Guid id,
            Guid attachmentId,
            KrizovkaDbContext db,
            IAttachmentStorage storage,
            HttpContext httpContext,
            CancellationToken ct) =>
        {
            var report = await db.Reports
                .AsNoTracking()
                .FirstOrDefaultAsync(r => r.Id == id, ct);

            if (report == null)
            {
                return Results.Problem(
                    title: "Nenalezeno",
                    detail: $"Hlášení {id} nebylo nalezeno.",
                    statusCode: StatusCodes.Status404NotFound);
            }

            if (httpContext.User.Identity?.IsAuthenticated == true)
            {
                var isSuperAdmin = httpContext.User.IsInRole("SuperAdmin");
                if (!isSuperAdmin)
                {
                    var callerOrgClaim = httpContext.User.FindFirst("OrganizationId")?.Value
                        ?? httpContext.User.FindFirst("org_id")?.Value;
                    Guid.TryParse(callerOrgClaim, out var callerOrgId);

                    if (callerOrgId == Guid.Empty || report.OrganizationId != callerOrgId)
                    {
                        return Results.Problem(
                            title: "Přístup odepřen",
                            detail: "Hlášení patří jiné organizaci.",
                            statusCode: StatusCodes.Status403Forbidden);
                    }
                }
            }

            var attachment = await db.Set<IssueAttachment>()
                .AsNoTracking()
                .FirstOrDefaultAsync(a => a.IssueId == id && a.Id == attachmentId, ct);

            if (attachment == null)
            {
                return Results.Problem(
                    title: "Nenalezeno",
                    detail: $"Příloha {attachmentId} pro hlášení {id} nebyla nalezena.",
                    statusCode: StatusCodes.Status404NotFound);
            }

            var stream = await storage.OpenAttachmentReadStreamAsync(attachment.StorageKey, ct);
            if (stream == null)
            {
                return Results.Problem(
                    title: "Soubor nenalezen",
                    detail: "Soubor přílohy nebyl na disku nalezen.",
                    statusCode: StatusCodes.Status404NotFound);
            }

            return Results.File(
                fileStream: stream,
                contentType: attachment.ContentType,
                fileDownloadName: attachment.FileName,
                enableRangeProcessing: true
            );
        }).AllowAnonymous();

        // 8. Add supplemental text message
        group.MapPost("{id:guid}/messages", async (
            Guid id,
            [FromBody] AddReportMessageRequest request,
            KrizovkaDbContext db,
            HttpContext httpContext,
            CancellationToken ct) =>
        {
            if (string.IsNullOrWhiteSpace(request.Message))
            {
                return Results.Problem(
                    title: "Neplatný požadavek",
                    detail: "Zpráva nesmí být prázdná.",
                    statusCode: StatusCodes.Status400BadRequest);
            }

            var report = await db.Reports
                .Include(r => r.History)
                .Include(r => r.Attachments)
                .FirstOrDefaultAsync(r => r.Id == id, ct);

            if (report == null)
            {
                return Results.Problem(
                    title: "Nenalezeno",
                    detail: $"Hlášení {id} nebylo nalezeno.",
                    statusCode: StatusCodes.Status404NotFound);
            }

            var actorId = Guid.TryParse(httpContext.User.FindFirst("AccountId")?.Value, out var parsedActor)
                ? parsedActor
                : Guid.Empty;

            report.AddSupplementalMessage(request.Message, actorId);
            await db.SaveChangesAsync(ct);

            return Results.Ok(ToDetailDto(report));
        }).AllowAnonymous();

        // 9. Start live audio/video stream (LiveKit uplink)
        group.MapPost("{id:guid}/stream", async (
            Guid id,
            [FromBody] StartStreamRequest request,
            KrizovkaDbContext db,
            IConfiguration config,
            HttpContext httpContext,
            CancellationToken ct) =>
        {
            var report = await db.Reports
                .Include(r => r.History)
                .FirstOrDefaultAsync(r => r.Id == id, ct);

            if (report == null)
            {
                return Results.Problem(
                    title: "Nenalezeno",
                    detail: $"Hlášení {id} nebylo nalezeno.",
                    statusCode: StatusCodes.Status404NotFound);
            }

            var mode = request.Mode?.ToLowerInvariant() == "video" ? "video" : "audio";
            var actorId = Guid.TryParse(httpContext.User.FindFirst("AccountId")?.Value, out var parsedActor)
                ? parsedActor
                : Guid.Empty;

            report.StartStream(mode, actorId);
            await db.SaveChangesAsync(ct);

            var liveKitUrl = config["LIVEKIT_URL"] ?? "wss://livekit.krizovka.local";
            var roomName = $"room_{id:N}";
            var now = DateTimeOffset.UtcNow;
            var token = $"demo_token_{id:N}_{mode}_{now.ToUnixTimeSeconds()}";

            var session = new StreamSessionDto(
                ReportId: id,
                RoomName: roomName,
                Token: token,
                ServerUrl: liveKitUrl,
                Mode: mode,
                UplinkOnly: true,
                ExpiresAt: now.AddHours(2)
            );

            return Results.Ok(session);
        }).AllowAnonymous();

        // 10. End live stream
        group.MapPost("{id:guid}/stream/end", async (
            Guid id,
            KrizovkaDbContext db,
            HttpContext httpContext,
            CancellationToken ct) =>
        {
            var report = await db.Reports
                .Include(r => r.History)
                .Include(r => r.Attachments)
                .FirstOrDefaultAsync(r => r.Id == id, ct);

            if (report == null)
            {
                return Results.Problem(
                    title: "Nenalezeno",
                    detail: $"Hlášení {id} nebylo nalezeno.",
                    statusCode: StatusCodes.Status404NotFound);
            }

            var actorId = Guid.TryParse(httpContext.User.FindFirst("AccountId")?.Value, out var parsedActor)
                ? parsedActor
                : Guid.Empty;

            report.EndStream(actorId);
            await db.SaveChangesAsync(ct);

            return Results.Ok(ToDetailDto(report));
        }).AllowAnonymous();
    }

    private static ReportDetailDto ToDetailDto(Issue r) =>
        new(
            r.Id,
            r.OrganizationId,
            r.ClientReportId,
            r.ReporterAccountId,
            r.Description,
            r.Status,
            r.CreatedAt,
            r.UpdatedAt,
            r.Latitude,
            r.Longitude,
            r.AccuracyMeters,
            r.LocationMeasuredAt,
            r.Contact,
            r.History.OrderByDescending(h => h.CreatedAt).Select(h => new ReportHistoryDto(
                h.Id,
                h.ActorId,
                h.Action,
                h.PreviousValue,
                h.NewValue,
                h.Metadata,
                h.CreatedAt
            )).ToList(),
            r.Attachments.Select(a => new ReportAttachmentDto(
                a.Id,
                a.FileName,
                a.ContentType,
                a.SizeBytes,
                a.StorageKey,
                a.CreatedAt
            )).ToList(),
            r.IpAddress,
            r.UserAgent
        );
}
