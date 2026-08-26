// ABOUTME: Maps stable Location address-provenance lookup rows without EF model seed data.
// ABOUTME: Enforces immutable integer IDs and unique machine-readable source codes.

using Explore.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Explore.Persistence.Configurations.Entities;

public sealed class LocationAddressSourceConfiguration : IEntityTypeConfiguration<LocationAddressSource>
{
    public void Configure(EntityTypeBuilder<LocationAddressSource> builder)
    {
        builder.ToTable("location_address_sources");
        builder.Property(row => row.Id).ValueGeneratedNever();
        builder.Property(row => row.MasterCode).IsRequired().HasMaxLength(100);
        builder.Property(row => row.FullName).IsRequired().HasMaxLength(200);
        builder.Property(row => row.Description).HasMaxLength(500);
        builder.HasIndex(row => row.MasterCode).IsUnique();
    }
}
