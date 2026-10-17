using Microsoft.EntityFrameworkCore;
using Krizovka.Api.Reports;

namespace Krizovka.Api.Data;

public sealed class KrizovkaDbContext(DbContextOptions<KrizovkaDbContext> options) : DbContext(options)
{
    public DbSet<Issue> Reports => Set<Issue>();
    public DbSet<Organizations.Organization> Organizations => Set<Organizations.Organization>();
    public DbSet<Organizations.OrganizationArea> OrganizationAreas => Set<Organizations.OrganizationArea>();
    public DbSet<Guides.GuideTemplate> GuideTemplates => Set<Guides.GuideTemplate>();
    public DbSet<Guides.Guide> Guides => Set<Guides.Guide>();

    protected override void ConfigureConventions(ModelConfigurationBuilder configurationBuilder)
    {
        base.ConfigureConventions(configurationBuilder);
        configurationBuilder.Properties<DateTimeOffset>().HaveConversion<Microsoft.EntityFrameworkCore.Storage.ValueConversion.DateTimeOffsetToStringConverter>();
        configurationBuilder.Properties<DateTimeOffset?>().HaveConversion<Microsoft.EntityFrameworkCore.Storage.ValueConversion.DateTimeOffsetToStringConverter>();
    }

    protected override void OnModelCreating(ModelBuilder model)
    {
        var report = model.Entity<Issue>();
        report.ToTable("reports");
        report.HasKey(x => x.Id);
        report.HasIndex(x => new { x.OrganizationId, x.ClientReportId }).IsUnique();
        report.HasIndex(x => new { x.OrganizationId, x.CreatedAt });
        report.Property(x => x.Description).HasMaxLength(4000);
        report.Property(x => x.Status).HasConversion<string>().HasMaxLength(20);
        report.Property(x => x.IpAddress).HasMaxLength(45);
        report.Property(x => x.UserAgent).HasMaxLength(500);
        report.OwnsOne(x => x.Contact);
        report.HasMany(x => x.Attachments).WithOne(x => x.Issue).HasForeignKey(x => x.IssueId);
        report.HasMany(x => x.History).WithOne(x => x.Issue).HasForeignKey(x => x.IssueId);
        report.Navigation(x => x.Attachments).UsePropertyAccessMode(PropertyAccessMode.Field);
        report.Navigation(x => x.History).UsePropertyAccessMode(PropertyAccessMode.Field);
        model.Entity<IssueAttachment>().ToTable("report_attachments");
        model.Entity<IssueHistory>().ToTable("report_history");

        var org = model.Entity<Organizations.Organization>();
        org.ToTable("organizations");
        org.HasKey(x => x.Id);
        org.Property(x => x.Name).HasMaxLength(200).IsRequired();
        org.Property(x => x.Code).HasMaxLength(32).IsRequired();
        org.Property(x => x.Status).HasMaxLength(20).IsRequired();
        org.Property(x => x.ContactEmail).HasMaxLength(200);
        org.Property(x => x.ContactPhone).HasMaxLength(50);
        org.HasMany(x => x.Areas).WithOne(x => x.Organization).HasForeignKey(x => x.OrganizationId).OnDelete(DeleteBehavior.Cascade);

        var area = model.Entity<Organizations.OrganizationArea>();
        area.ToTable("organization_areas");
        area.HasKey(x => x.Id);
        area.Property(x => x.Name).HasMaxLength(200).IsRequired();
        area.Property(x => x.AllocatedVelinId).HasMaxLength(100).IsRequired();
        area.Property(x => x.PolygonGeoJson).HasColumnType("text").IsRequired();

        var tmpl = model.Entity<Guides.GuideTemplate>();
        tmpl.ToTable("guide_templates");
        tmpl.HasKey(x => x.Id);
        tmpl.Property(x => x.Id).HasMaxLength(100);
        tmpl.Property(x => x.Title).HasMaxLength(300).IsRequired();
        tmpl.Property(x => x.Category).HasMaxLength(100).IsRequired();
        tmpl.Property(x => x.QuickSummary).HasColumnType("text");
        tmpl.Property(x => x.DiagramMermaid).HasColumnType("text");
        tmpl.Property(x => x.LegalBasisJson).HasColumnType("text");
        tmpl.Property(x => x.RecommendedOrgTypesJson).HasColumnType("text");
        tmpl.Property(x => x.StepsJson).HasColumnType("text").IsRequired();
        tmpl.Property(x => x.EmergencyContactsJson).HasColumnType("text").IsRequired();

        var guide = model.Entity<Guides.Guide>();
        guide.ToTable("guides");
        guide.HasKey(x => x.Id);
        guide.HasIndex(x => new { x.OrganizationId, x.Status });
        guide.Property(x => x.Title).HasMaxLength(300).IsRequired();
        guide.Property(x => x.Category).HasMaxLength(100).IsRequired();
        guide.Property(x => x.QuickSummary).HasColumnType("text");
        guide.Property(x => x.DiagramMermaid).HasColumnType("text");
        guide.Property(x => x.LegalBasisJson).HasColumnType("text");
        guide.Property(x => x.Status).HasConversion<string>().HasMaxLength(20);
        guide.Property(x => x.StepsJson).HasColumnType("text").IsRequired();
        guide.Property(x => x.EmergencyContactsJson).HasColumnType("text").IsRequired();
    }
}
