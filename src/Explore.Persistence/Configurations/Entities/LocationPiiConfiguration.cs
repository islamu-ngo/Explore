// ABOUTME: Configures the location_pii extension table with strict 1:1 PK/FK to locations.
// ABOUTME: Stores removable precise address and coordinates outside the core location table.

namespace Explore.Persistence.Configurations.Entities;

using Explore.Domain;
using Explore.Domain.ValueObjects;
using Explore.Persistence.Schema;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

public class LocationPiiConfiguration : IEntityTypeConfiguration<LocationPii>
{
    public void Configure(EntityTypeBuilder<LocationPii> builder)
    {
        builder.ToTable(table => table.HasCheckConstraint(
            "ck_location_pii_coordinate_shape",
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

        builder.Property(e => e.AddressSubstringKey)
            .HasMaxLength(LocationAddressSubstringKeyV1.MaximumLength)
            .HasDefaultValue(string.Empty)
            .IsRequired()
            .UsePortableOrdinalAscii();

        builder.Property(e => e.AddressSubstringKeyVersion)
            .HasDefaultValue((short)0)
            .IsRequired();

        builder.ToTable(table => table.HasCheckConstraint(
            "ck_location_pii_address_substring_key_version",
            "(address_substring_key_version = 0 AND address_substring_key = '') OR " +
            "(address_substring_key_version = 1 AND address_substring_key <> '' AND length(address_substring_key) % 7 = 0)"));
    }
}
