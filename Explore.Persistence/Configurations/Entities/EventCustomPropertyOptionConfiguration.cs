// ABOUTME: EF Core configuration for event-local custom-property options.
// ABOUTME: Preserves stable machine identity and source-template provenance per runtime definition.

using Explore.Domain;
using Explore.Persistence.ValueGenerators;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Explore.Persistence.Configurations.Entities;

public class EventCustomPropertyOptionConfiguration : IEntityTypeConfiguration<EventCustomPropertyOption>
{
    public void Configure(EntityTypeBuilder<EventCustomPropertyOption> builder)
    {
        builder.ToTable("event_custom_property_options");

        builder.Property(e => e.Id)
            .HasValueGenerator<GuidVersion7ValueGenerator>();

        builder.Property(e => e.Namespace)
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(e => e.Key)
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(e => e.DisplayName)
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(e => e.Description)
            .HasMaxLength(500);

        builder.Property(e => e.Value)
            .HasMaxLength(500)
            .IsRequired();

        builder.HasOne(e => e.ParentOption)
            .WithMany(e => e.ChildOptions)
            .HasForeignKey(e => e.ParentOptionId)
            .HasConstraintName("fk_ecpo_parent_option")
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasOne(e => e.Definition)
            .WithMany(e => e.Options)
            .HasForeignKey(e => e.EventCustomPropertyDefinitionId)
            .HasConstraintName("fk_ecpo_definition")
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(e => new { e.EventCustomPropertyDefinitionId, e.SortOrder })
            .HasDatabaseName("ix_ecpo_definition_sort");

        builder.HasIndex(e => new { e.EventCustomPropertyDefinitionId, e.Namespace, e.Key })
            .HasDatabaseName("ix_ecpo_definition_namespace_key")
            .IsUnique();
    }
}
