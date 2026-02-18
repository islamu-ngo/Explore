using Explore.Domain;
using Explore.Persistence.ValueGenerators;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Explore.Persistence.Configurations.Entities;

public class TenantConfiguration : IEntityTypeConfiguration<Tenant>
{
    public void Configure(EntityTypeBuilder<Tenant> builder)
    {
        builder.Property(e => e.Id).HasValueGenerator<GuidVersion7ValueGenerator>();

        builder.Property(e => e.FullName).HasMaxLength(500).IsRequired();
        builder.Property(e => e.Slug).HasMaxLength(500).IsRequired();
        builder.Property(e => e.Description).HasMaxLength(500);
        builder.Property(e => e.TenantStatusId).IsRequired();

        builder.HasOne(e => e.TenantStatus)
            .WithMany()
            .HasForeignKey(e => e.TenantStatusId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(e => e.Slug).IsUnique();

        // NOTE: Business entity seed data moved to DatabaseSeeder for conditional (Development-only) seeding.
        // See Explore.Persistence/Seed/DatabaseSeeder.cs and SeedData.cs
    }
}
