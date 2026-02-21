using Explore.Domain;
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

        builder.Property(e => e.FullName).HasMaxLength(500).IsRequired();
        builder.Property(e => e.Country).HasMaxLength(500).IsRequired();
        builder.Property(e => e.City).HasMaxLength(500).IsRequired();
        builder.Property(e => e.Timezone).HasMaxLength(500);

        builder.HasOne(e => e.Tenant)
            .WithMany()
            .HasForeignKey(e => e.TenantId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(e => e.Pii)
            .WithOne(e => e.Location)
            .HasForeignKey<LocationPii>(e => e.LocationId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Navigation(e => e.Pii).AutoInclude();

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
