// ABOUTME: Configures the actor_pii extension table with strict 1:1 PK/FK to actors.
// Stores removable actor-identifying fields separately from the core actor record.

namespace Explore.Persistence.Configurations.Entities;

using Explore.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

public class ActorPiiConfiguration : IEntityTypeConfiguration<ActorPii>
{
    public void Configure(EntityTypeBuilder<ActorPii> builder)
    {
        builder.ToTable("actor_pii");

        builder.HasKey(e => e.ActorId);

        builder.Property(e => e.DisplayName)
            .HasMaxLength(500)
            .IsRequired();

        builder.Property(e => e.Did)
            .HasMaxLength(500);

        builder.Property(e => e.Handle)
            .HasMaxLength(500);

        builder.Property(e => e.ProfilePictureUri)
            .HasMaxLength(500);

        builder.HasIndex(e => e.Did)
            .HasDatabaseName("ix_actor_pii_did");

        builder.HasIndex(e => e.Handle)
            .HasDatabaseName("ix_actor_pii_handle");
    }
}
