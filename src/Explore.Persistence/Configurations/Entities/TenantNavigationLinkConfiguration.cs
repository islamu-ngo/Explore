using Explore.Domain;
using Explore.Persistence.ValueGenerators;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Explore.Persistence.Configurations.Entities;

/// <summary>
/// Entity Framework Core configuration for TenantNavigationLink.
/// Defines table structure, constraints, and relationships.
/// </summary>
public class TenantNavigationLinkConfiguration : IEntityTypeConfiguration<TenantNavigationLink>
{
    public void Configure(EntityTypeBuilder<TenantNavigationLink> builder)
    {
        // Primary key with UUIDv7 value generator
        builder.Property(e => e.Id).HasValueGenerator<GuidVersion7ValueGenerator>();

        // Required string properties with max length constraints
        builder.Property(e => e.Label)
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(e => e.Url)
            .HasMaxLength(500)
            .IsRequired();

        // Optional icon property
        builder.Property(e => e.Icon)
            .HasMaxLength(100);

        // Order property for sorting navigation links
        builder.Property(e => e.Order)
            .HasDefaultValue(0);

        // Boolean properties with defaults
        builder.Property(e => e.OpenInNewTab)
            .HasDefaultValue(false);

        builder.Property(e => e.IsActive)
            .HasDefaultValue(true);

        // Relationship to Tenant with cascade delete
        // When a Tenant is deleted, all its navigation links are deleted
        builder.HasOne(e => e.Tenant)
            .WithMany(t => t.NavigationLinks)
            .HasForeignKey(e => e.TenantId)
            .OnDelete(DeleteBehavior.Cascade);

        // Index on TenantId for efficient filtering by tenant
        builder.HasIndex(e => e.TenantId);

        // Composite index on TenantId and Order for efficient sorting within a tenant
        builder.HasIndex(e => new { e.TenantId, e.Order });
    }
}
