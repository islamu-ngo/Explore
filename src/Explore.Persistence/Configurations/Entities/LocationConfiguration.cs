// ABOUTME: EF configuration for tenant-scoped event locations and location PII partitioning.
// ABOUTME: Exposes a tenant-scoped alternate key for composite FKs from sessions, rooms, and agenda rows.

using Explore.Domain;
using Explore.Domain.Enums;
using Explore.Persistence.Seed;
using Explore.Persistence.ValueGenerators;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Explore.Persistence.Configurations.Entities;

public class LocationConfiguration : IEntityTypeConfiguration<Location>
{
    public void Configure(EntityTypeBuilder<Location> builder)
    {
        builder.Property(e => e.Id).HasValueGenerator<GuidVersion7ValueGenerator>();
        builder.HasAlternateKey(e => new { e.TenantId, e.Id });

        builder.Property(e => e.FullName).HasMaxLength(500).IsRequired();
        builder.Property(e => e.Country).HasMaxLength(500).IsRequired();
        builder.Property(e => e.City).HasMaxLength(500).IsRequired();
        builder.Property(e => e.Timezone).HasMaxLength(500);
        builder.Property(e => e.ConcurrencyStamp).IsConcurrencyToken();
        builder.Property(e => e.LocationKindId)
            .HasDefaultValue((int)LocationKindEnum.Unclassified)
            .IsRequired();
        builder.Property(e => e.LocationPrivacyStateId)
            .HasDefaultValue((int)LocationPrivacyStateEnum.NotProvided)
            .IsRequired();

        builder.HasOne(e => e.Tenant)
            .WithMany()
            .HasForeignKey(e => e.TenantId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(e => e.Pii)
            .WithOne(e => e.Location)
            .HasForeignKey<LocationPii>(e => e.LocationId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Navigation(e => e.Pii).AutoInclude();

        builder.HasOne(e => e.LocationKind)
            .WithMany()
            .HasForeignKey(e => e.LocationKindId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(e => e.LocationPrivacyState)
            .WithMany()
            .HasForeignKey(e => e.LocationPrivacyStateId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(e => e.OwnerUser)
            .WithMany()
            .HasForeignKey(e => e.OwnerUserId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.ToTable(table =>
        {
            table.HasCheckConstraint(
                "ck_locations_owner_private_home",
                "owner_user_id IS NULL OR location_kind_id = 5");
            table.HasCheckConstraint(
                "ck_locations_erasure_state",
                "(location_privacy_state_id = 3 AND owner_user_id IS NULL AND pii_erased_at_utc IS NOT NULL AND pii_erasure_reason IS NOT NULL) OR " +
                "(location_privacy_state_id <> 3 AND pii_erased_at_utc IS NULL AND pii_erasure_reason IS NULL)");
        });

        // ===== Performance Indexes =====

        // Location lookup by city (most common filter)
        builder.HasIndex(e => new { e.TenantId, e.City })
            .HasDatabaseName("ix_locations_tenant_city");

        // Location lookup by country
        builder.HasIndex(e => new { e.TenantId, e.Country })
            .HasDatabaseName("ix_locations_tenant_country");

        // NOTE: Business entity seed data moved to DatabaseSeeder for conditional (Development-only) seeding.
        // See Explore.Persistence/Seed/DatabaseSeeder.cs and SeedData.cs
    }
}
