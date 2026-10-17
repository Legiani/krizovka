using Krizovka.Users.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Krizovka.Users.Infrastructure.Persistence.Configurations;

/// <summary>
/// Entity configuration for AccountClaim.
/// Table/column names are handled automatically by UseSnakeCaseNamingConvention().
/// Only configure: HasKey, HasConversion, relationships, indexes.
/// </summary>
public class AccountClaimConfiguration : IEntityTypeConfiguration<AccountClaim>
{
    public void Configure(EntityTypeBuilder<AccountClaim> builder)
    {
        builder.HasKey(x => x.ClaimId);

        builder.Property(x => x.Status)
            .HasConversion<string>();

        builder.HasOne(x => x.Account)
            .WithMany()
            .HasForeignKey(x => x.AccountId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(x => x.ClaimToken).IsUnique();
    }
}
