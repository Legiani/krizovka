using Microsoft.EntityFrameworkCore;
using Krizovka.Api.Reports;

namespace Krizovka.Api.Data;

public sealed class KrizovkaDbContext(DbContextOptions<KrizovkaDbContext> options) : DbContext(options)
{
    public DbSet<Issue> Reports => Set<Issue>();

    protected override void OnModelCreating(ModelBuilder model)
    {
        var report = model.Entity<Issue>();
        report.ToTable("reports");
        report.HasKey(x => x.Id);
        report.HasIndex(x => new { x.OrganizationId, x.ReporterAccountId, x.ClientReportId }).IsUnique();
        report.HasIndex(x => new { x.OrganizationId, x.CreatedAt });
        report.Property(x => x.Description).HasMaxLength(4000);
        report.Property(x => x.Status).HasConversion<string>().HasMaxLength(20);
        report.OwnsOne(x => x.Contact);
        report.HasMany(x => x.Attachments).WithOne(x => x.Issue).HasForeignKey(x => x.IssueId);
        report.HasMany(x => x.History).WithOne(x => x.Issue).HasForeignKey(x => x.IssueId);
        model.Entity<IssueAttachment>().ToTable("report_attachments");
        model.Entity<IssueHistory>().ToTable("report_history");
    }
}
