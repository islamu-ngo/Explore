// ABOUTME: Configures the location_pii extension table with strict 1:1 PK/FK to locations.
// Stores removable precise address and coordinates outside the core location table.

namespace Explore.Persistence.Configurations.Entities;

using Explore.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

public class LocationPiiConfiguration : IEntityTypeConfiguration<LocationPii>
{
    public void Configure(EntityTypeBuilder<LocationPii> builder)
    {
        builder.ToTable("location_pii");

        builder.HasKey(e => e.LocationId);

        builder.Property(e => e.Address)
            .HasMaxLength(500)
            .IsRequired();

        builder.Property(e => e.Postcode)
            .HasMaxLength(500)
            .IsRequired();
    }
}
