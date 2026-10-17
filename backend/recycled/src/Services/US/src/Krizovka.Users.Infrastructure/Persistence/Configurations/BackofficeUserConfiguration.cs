using Krizovka.Users.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Krizovka.Users.Infrastructure.Persistence.Configurations;

/// <summary>
/// Entity configuration for BackofficeUser.
/// Table/column names are handled automatically by UseSnakeCaseNamingConvention().
/// NOTE: Remove unique email index — not enforced in DB schema currently.
/// </summary>
public class BackofficeUserConfiguration : IEntityTypeConfiguration<BackofficeUser>
{
    public void Configure(EntityTypeBuilder<BackofficeUser> builder)
    {
        builder.HasKey(x => x.Id);

        builder.Property(x => x.Email)
            .HasMaxLength(255)
            .IsRequired();

        builder.Property(x => x.FirstName)
            .HasMaxLength(150)
            .IsRequired();

        builder.Property(x => x.LastName)
            .HasMaxLength(150)
            .IsRequired();

        builder.Property(x => x.PasswordHash)
            .IsRequired();

        // Relationship defined here only — not repeated in BackofficeRoleConfiguration
        builder.HasOne(x => x.Role)
            .WithMany(r => r.Users)
            .HasForeignKey(x => x.RoleId)
            .OnDelete(DeleteBehavior.SetNull);
    }
}
