// ABOUTME: Maps stable Location address-visibility lookup rows without EF model seed data.
// ABOUTME: Enforces immutable integer IDs and unique machine-readable visibility codes.

using Explore.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Explore.Persistence.Configurations.Entities;

public sealed class LocationAddressVisibilityConfiguration : IEntityTypeConfiguration<LocationAddressVisibility>
{
    public void Configure(EntityTypeBuilder<LocationAddressVisibility> builder)
    {
        builder.ToTable("location_address_visibilities");
        builder.Property(row => row.Id).ValueGeneratedNever();
        builder.Property(row => row.MasterCode).IsRequired().HasMaxLength(100);
        builder.Property(row => row.FullName).IsRequired().HasMaxLength(200);
        builder.Property(row => row.Description).HasMaxLength(500);
        builder.HasIndex(row => row.MasterCode).IsUnique();
    }
}
