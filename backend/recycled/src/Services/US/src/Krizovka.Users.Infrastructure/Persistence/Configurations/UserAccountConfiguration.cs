using Krizovka.Users.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Krizovka.Users.Infrastructure.Persistence.Configurations;

/// <summary>
/// Entity configuration for UserAccount.
/// Table/column names are handled automatically by UseSnakeCaseNamingConvention().
/// Only configure: HasKey, HasConversion, HasMaxLength, relationships, indexes.
/// Never call ToTable() or HasColumnName() — the convention handles all naming.
/// </summary>
public class UserAccountConfiguration : IEntityTypeConfiguration<UserAccount>
{
    public void Configure(EntityTypeBuilder<UserAccount> builder)
    {
        builder.HasKey(x => x.AccountId);

        builder.Property(x => x.AccountType)
            .HasConversion<string>();

        builder.Property(x => x.Status)
            .HasConversion<string>();

        builder.Property(x => x.RegistrationSystemId)
            .HasMaxLength(50);

        builder.Property(x => x.Email)
            .HasMaxLength(255);

        builder.Property(x => x.FirstName)
            .HasMaxLength(150);

        builder.Property(x => x.LastName)
            .HasMaxLength(150);

        builder.HasIndex(x => x.IdentityId);
    }
}
