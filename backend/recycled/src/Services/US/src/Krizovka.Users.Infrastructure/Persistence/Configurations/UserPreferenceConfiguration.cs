using Krizovka.Users.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Krizovka.Users.Infrastructure.Persistence.Configurations;

/// <summary>
/// Entity configuration for UserPreference.
/// Table/column names handled by UseSnakeCaseNamingConvention().
/// </summary>
public class UserPreferenceConfiguration : IEntityTypeConfiguration<UserPreference>
{
    public void Configure(EntityTypeBuilder<UserPreference> builder)
    {
        builder.HasKey(x => x.Id);

        // One preference set per user per city
        builder.HasIndex(x => new { x.AccountId, x.SystemId }).IsUnique();

        builder.Property(x => x.Favorites).HasColumnType("jsonb");
        builder.Property(x => x.MenuLayout).HasColumnType("jsonb");
        builder.Property(x => x.NotificationSettings).HasColumnType("jsonb");

        builder.HasOne(x => x.UserAccount)
            .WithMany()
            .HasForeignKey(x => x.AccountId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
