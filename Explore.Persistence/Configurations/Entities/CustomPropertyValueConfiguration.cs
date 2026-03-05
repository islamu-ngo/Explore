// ABOUTME: EF Core configuration for CustomPropertyValue with typed value columns and polymorphic EntityId.
// ABOUTME: Indexed for efficient per-entity and per-definition lookups.

using Explore.Domain;
using Explore.Persistence.ValueGenerators;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Explore.Persistence.Configurations.Entities;

public class CustomPropertyValueConfiguration : IEntityTypeConfiguration<CustomPropertyValue>
{
    public void Configure(EntityTypeBuilder<CustomPropertyValue> builder)
    {
        builder.ToTable("custom_property_values");

        builder.Property(e => e.Id)
            .HasValueGenerator<GuidVersion7ValueGenerator>();

        builder.Property(e => e.TextValue)
            .HasMaxLength(4000);

        builder.Property(e => e.NumberValue)
            .HasPrecision(19, 4);

        // Relationships
        builder.HasOne(e => e.Definition)
            .WithMany(d => d.Values)
            .HasForeignKey(e => e.CustomPropertyDefinitionId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(e => e.Option)
            .WithMany()
            .HasForeignKey(e => e.OptionId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasOne(e => e.Tenant)
            .WithMany()
            .HasForeignKey(e => e.TenantId)
            .OnDelete(DeleteBehavior.Restrict);

        // Per-entity value lookup (get all custom values for an Event/Org/Group)
        builder.HasIndex(e => e.EntityId)
            .HasDatabaseName("ix_cpv_entity");

        // Per-definition lookup (all values of a specific property across entities)
        builder.HasIndex(e => new { e.CustomPropertyDefinitionId, e.EntityId })
            .HasDatabaseName("ix_cpv_definition_entity");

        // Tenant + definition query
        builder.HasIndex(e => new { e.TenantId, e.CustomPropertyDefinitionId })
            .HasDatabaseName("ix_cpv_tenant_definition");
    }
}
