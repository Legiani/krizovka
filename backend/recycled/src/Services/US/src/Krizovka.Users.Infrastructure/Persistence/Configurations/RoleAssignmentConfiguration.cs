using Krizovka.Users.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Krizovka.Users.Infrastructure.Persistence.Configurations;

/// <summary>
/// Entity configuration for RoleAssignment.
/// Table/column names are handled automatically by UseSnakeCaseNamingConvention().
/// Only configure: HasKey, HasConversion, relationships, indexes.
/// </summary>
public class RoleAssignmentConfiguration : IEntityTypeConfiguration<RoleAssignment>
{
    public void Configure(EntityTypeBuilder<RoleAssignment> builder)
    {
        builder.HasKey(x => x.AssignmentId);

        builder.Property(x => x.ScopeType)
            .HasConversion<string>();

        builder.Property(x => x.Status)
            .HasConversion<string>();

        builder.HasOne(x => x.Account)
            .WithMany(x => x.RoleAssignments)
            .HasForeignKey(x => x.AccountId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(x => x.AccountId);
        builder.HasIndex(x => new { x.ScopeType, x.ScopeId });
    }
}
