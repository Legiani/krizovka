using Krizovka.Users.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Krizovka.Users.Infrastructure.Persistence.Configurations;

/// <summary>
/// Entity configuration for Relationship.
/// Table/column names are handled automatically by UseSnakeCaseNamingConvention().
/// Only configure: HasKey, HasConversion, relationships, indexes.
/// </summary>
public class RelationshipConfiguration : IEntityTypeConfiguration<Relationship>
{
    public void Configure(EntityTypeBuilder<Relationship> builder)
    {
        builder.HasKey(x => x.RelationshipId);

        builder.Property(x => x.RelationType)
            .HasConversion<string>();

        builder.Property(x => x.Status)
            .HasConversion<string>();

        builder.HasOne(x => x.ParentAccount)
            .WithMany(x => x.ParentRelationships)
            .HasForeignKey(x => x.ParentAccountId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(x => x.ChildAccount)
            .WithMany(x => x.ChildRelationships)
            .HasForeignKey(x => x.ChildAccountId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(x => x.ParentAccountId);
        builder.HasIndex(x => x.ChildAccountId);
    }
}
