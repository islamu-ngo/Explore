// ABOUTME: EF configuration for LocationRoom - tenant-scoped child of Location used by room-aware scheduling.
// ABOUTME: Enforces per-location name uniqueness and exposes the concurrency stamp as the optimistic concurrency token.

using Explore.Domain;
using Explore.Persistence.ValueGenerators;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Explore.Persistence.Configurations.Entities;

public class LocationRoomConfiguration : IEntityTypeConfiguration<LocationRoom>
{
    public void Configure(EntityTypeBuilder<LocationRoom> builder)
    {
        builder.Property(e => e.Id).HasValueGenerator<GuidVersion7ValueGenerator>();
        builder.HasAlternateKey(e => new { e.TenantId, e.Id });
        builder.HasAlternateKey(e => new { e.TenantId, e.LocationId, e.Id });

        builder.Property(e => e.Name).HasMaxLength(200).IsRequired();
        builder.Property(e => e.Slug).HasMaxLength(200);
        builder.Property(e => e.Description).HasMaxLength(2000);

        builder.HasOne(e => e.Location)
            .WithMany()
            .HasForeignKey(e => new { e.TenantId, e.LocationId })
            .HasPrincipalKey(e => new { e.TenantId, e.Id })
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(e => e.Tenant)
            .WithMany()
            .HasForeignKey(e => e.TenantId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(e => new { e.TenantId, e.LocationId, e.Name })
            .HasDatabaseName("ix_location_rooms_tenant_location_name")
            .IsUnique();

        builder.HasIndex(e => new { e.TenantId, e.LocationId, e.SortOrder })
            .HasDatabaseName("ix_location_rooms_tenant_location_sort");

        builder.ToTable(t => t.HasCheckConstraint(
            "CK_LocationRoom_NonNegativeCapacity",
            "capacity IS NULL OR capacity >= 0"));

        builder.Property(e => e.ConcurrencyStamp).IsConcurrencyToken();
    }
}
