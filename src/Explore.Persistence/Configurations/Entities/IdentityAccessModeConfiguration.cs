// ABOUTME: Maps stable identity-access lookup rows without model-owned seed data.
// ABOUTME: Keeps integer IDs and durable business codes aligned with runtime lookup repair.

using Explore.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Explore.Persistence.Configurations.Entities;

public sealed class IdentityAccessModeConfiguration : IEntityTypeConfiguration<IdentityAccessMode>
{
    public void Configure(EntityTypeBuilder<IdentityAccessMode> builder)
    {
        builder.ToTable("identity_access_modes");
        builder.Property(row => row.Id).ValueGeneratedNever();
        builder.Property(row => row.MasterCode).IsRequired().HasMaxLength(100);
        builder.Property(row => row.FullName).IsRequired().HasMaxLength(200);
        builder.Property(row => row.Description).HasMaxLength(500);
        builder.HasIndex(row => row.MasterCode).IsUnique();
    }
}
