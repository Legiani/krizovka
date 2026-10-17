using Krizovka.Users.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Krizovka.Users.Infrastructure.Persistence.Configurations;

/// <summary>
/// Entity configuration for BackofficePermission.
/// Table/column names are handled automatically by UseSnakeCaseNamingConvention():
///   BackofficePermission → backoffice_permissions
///   ScopeType            → scope_type
///   ScopeValue           → scope_value
/// </summary>
public class BackofficePermissionConfiguration : IEntityTypeConfiguration<BackofficePermission>
{
    public void Configure(EntityTypeBuilder<BackofficePermission> builder)
    {
        builder.HasKey(x => x.Id);

        builder.Property(x => x.Module)
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(x => x.Action)
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(x => x.ScopeType)
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(x => x.ScopeValue)
            .HasMaxLength(100)
            .IsRequired();

        builder.HasOne(x => x.Role)
            .WithMany(r => r.Permissions)
            .HasForeignKey(x => x.RoleId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(x => x.RoleId);
    }
}
