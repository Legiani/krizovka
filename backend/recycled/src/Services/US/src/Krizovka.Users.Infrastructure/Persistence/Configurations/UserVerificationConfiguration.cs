using Krizovka.Users.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Krizovka.Users.Infrastructure.Persistence.Configurations;

/// <summary>
/// Entity configuration for UserVerification.
/// Table/column names are handled automatically by UseSnakeCaseNamingConvention().
/// Only configure: HasKey, HasConversion, HasMaxLength, special column overrides, indexes, relationships.
/// NOTE: Status uses HasColumnName("verification_status") intentionally — column name differs from property name.
/// </summary>
public class UserVerificationConfiguration : IEntityTypeConfiguration<UserVerification>
{
    public void Configure(EntityTypeBuilder<UserVerification> builder)
    {
        builder.HasKey(v => v.VerificationId);

        builder.Property(v => v.VerificationId)
            .ValueGeneratedOnAdd();

        builder.Property(v => v.AttributeName)
            .HasMaxLength(100)
            .IsRequired();

        // Intentional override: property is 'Status' but DB column is 'verification_status'
        builder.Property(v => v.Status)
            .HasColumnName("verification_status")
            .HasMaxLength(50)
            .HasConversion<string>()
            .IsRequired();

        builder.Property(v => v.VerificationMethod)
            .HasMaxLength(100);

        builder.Property(v => v.VerifiedBy)
            .HasMaxLength(255);

        builder.Property(v => v.Metadata)
            .HasColumnType("jsonb");

        builder.Property(v => v.CreatedAt)
            .HasDefaultValueSql("NOW()")
            .IsRequired();

        builder.Property(v => v.UpdatedAt)
            .HasDefaultValueSql("NOW()")
            .IsRequired();

        builder.HasIndex(v => v.AccountId)
            .HasDatabaseName("idx_user_verifications_account");

        builder.HasIndex(v => v.Status)
            .HasDatabaseName("idx_user_verifications_status");

        builder.HasIndex(v => new { v.AccountId, v.AttributeName })
            .IsUnique()
            .HasDatabaseName("idx_user_verifications_account_attribute");

        builder.HasOne(v => v.Account)
            .WithMany()
            .HasForeignKey(v => v.AccountId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
