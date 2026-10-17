using Krizovka.Users.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Krizovka.Users.Infrastructure.Persistence.Configurations;

/// <summary>
/// Entity configuration for BackofficeRole.
/// Table/column names are handled automatically by UseSnakeCaseNamingConvention():
///   BackofficeRole → backoffice_roles
/// NOTE: The Role↔Permissions relationship is defined in BackofficePermissionConfiguration.
/// NOTE: The Role↔Users relationship is defined in BackofficeUserConfiguration.
/// </summary>
public class BackofficeRoleConfiguration : IEntityTypeConfiguration<BackofficeRole>
{
    public void Configure(EntityTypeBuilder<BackofficeRole> builder)
    {
        builder.HasKey(x => x.Id);

        builder.Property(x => x.Name)
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(x => x.Code)
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(x => x.Description)
            .HasMaxLength(500);

        builder.HasIndex(x => x.Code).IsUnique();
    }
}
