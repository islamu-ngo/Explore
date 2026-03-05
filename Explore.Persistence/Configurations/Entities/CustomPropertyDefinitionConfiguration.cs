// ABOUTME: EF Core configuration for CustomPropertyDefinition with indexes and constraints.
// ABOUTME: Enforces unique (TenantId, EntityTypeName, EventTypeId, Name) for property definitions.

using Explore.Domain;
using Explore.Persistence.ValueGenerators;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Explore.Persistence.Configurations.Entities;

public class CustomPropertyDefinitionConfiguration : IEntityTypeConfiguration<CustomPropertyDefinition>
{
    public void Configure(EntityTypeBuilder<CustomPropertyDefinition> builder)
    {
        builder.ToTable("custom_property_definitions");

        builder.Property(e => e.Id)
            .HasValueGenerator<GuidVersion7ValueGenerator>();

        builder.Property(e => e.Name)
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(e => e.DisplayName)
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(e => e.Description)
            .HasMaxLength(500);

        builder.Property(e => e.DefaultValue)
            .HasMaxLength(1000);

        builder.Property(e => e.ValidationRules)
            .HasMaxLength(2000);

        builder.Property(e => e.EntityTypeName)
            .HasConversion<string>()
            .HasMaxLength(50);

        builder.Property(e => e.PropertyType)
            .HasConversion<string>()
            .HasMaxLength(50);

        // Relationships
        builder.HasOne(e => e.EventType)
            .WithMany()
            .HasForeignKey(e => e.EventTypeId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(e => e.Tenant)
            .WithMany()
            .HasForeignKey(e => e.TenantId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasMany(e => e.Options)
            .WithOne(o => o.Definition)
            .HasForeignKey(o => o.CustomPropertyDefinitionId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(e => e.Values)
            .WithOne(v => v.Definition)
            .HasForeignKey(v => v.CustomPropertyDefinitionId)
            .OnDelete(DeleteBehavior.Cascade);

        // Unique constraint: one property name per entity type scope
        builder.HasIndex(e => new { e.TenantId, e.EntityTypeName, e.EventTypeId, e.Name })
            .HasDatabaseName("ix_cpd_tenant_entity_type_name")
            .IsUnique();

        // Listing query: active definitions by entity type
        builder.HasIndex(e => new { e.TenantId, e.EntityTypeName, e.IsActive })
            .HasDatabaseName("ix_cpd_tenant_entity_active");
    }
}
