// ABOUTME: EF Core configuration for shared tenant-scoped Layer 3 custom-property definitions.
// ABOUTME: Enforces namespaced machine-key uniqueness plus typed validation and exposure metadata.

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

        builder.Property(e => e.DefaultTextValue)
            .HasMaxLength(1000);

        builder.Property(e => e.RegexPattern)
            .HasMaxLength(1000);

        builder.Property(e => e.AllowedUrlSchemes)
            .HasMaxLength(500);

        builder.Property(e => e.DefaultNumberValue)
            .HasPrecision(19, 4);

        builder.Property(e => e.MinNumber)
            .HasPrecision(19, 4);

        builder.Property(e => e.MaxNumber)
            .HasPrecision(19, 4);

        builder.Property(e => e.EntityTypeName)
            .HasConversion<string>()
            .HasMaxLength(50);

        builder.Property(e => e.PropertyType)
            .HasConversion<string>()
            .HasMaxLength(50);

        builder.Property(e => e.ExposureLevel)
            .HasConversion<string>()
            .HasMaxLength(50);

        // Relationships
        builder.HasOne(e => e.Tenant)
            .WithMany()
            .HasForeignKey(e => e.TenantId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(e => e.DefaultOption)
            .WithMany()
            .HasForeignKey(e => e.DefaultOptionId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasMany(e => e.Options)
            .WithOne(o => o.Definition)
            .HasForeignKey(o => o.CustomPropertyDefinitionId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(e => e.Values)
            .WithOne(v => v.Definition)
            .HasForeignKey(v => v.CustomPropertyDefinitionId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(e => new { e.TenantId, e.EntityTypeName, e.Namespace, e.Key })
            .HasDatabaseName("ix_cpd_tenant_entity_namespace_key")
            .IsUnique();

        builder.HasIndex(e => new { e.TenantId, e.EntityTypeName, e.IsActive })
            .HasDatabaseName("ix_cpd_tenant_entity_active");

        builder.HasIndex(e => new { e.TenantId, e.EntityTypeName, e.IsSearchable, e.IsFilterable })
            .HasDatabaseName("ix_cpd_tenant_entity_search_filter");
    }
}
