// ABOUTME: Configures the location_pii extension table with strict 1:1 PK/FK to locations.
// ABOUTME: Stores removable precise address and coordinates outside the core location table.

namespace Explore.Persistence.Configurations.Entities;

using Explore.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

public class LocationPiiConfiguration : IEntityTypeConfiguration<LocationPii>
{
    public void Configure(EntityTypeBuilder<LocationPii> builder)
    {
        builder.ToTable("location_pii", table => table.HasCheckConstraint(
            "CK_LocationPii_CoordinateShape",
            """
            (latitude IS NULL AND longitude IS NULL)
            OR (latitude IS NOT NULL AND longitude IS NOT NULL
                AND latitude BETWEEN -90 AND 90
                AND longitude BETWEEN -180 AND 180)
            """));

        builder.HasKey(e => e.LocationId);

        builder.Property(e => e.Address)
            .HasMaxLength(500)
            .IsRequired();

        builder.Property(e => e.Postcode)
            .HasMaxLength(500)
            .IsRequired();
    }
}
