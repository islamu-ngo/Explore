// ABOUTME: EF Core configuration for TenantCapability entity.
// ABOUTME: Links modules to tenants with seed data for default tenant.

using Explore.Domain.Modules;
using Explore.Persistence.Seed;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Explore.Persistence.Configurations.Entities;

public class TenantCapabilityConfiguration : IEntityTypeConfiguration<TenantCapability>
{
    public void Configure(EntityTypeBuilder<TenantCapability> builder)
    {
        builder.ToTable("TenantCapabilities");

        builder.Property(c => c.Id).HasDefaultValueSql("uuidv7()");
        builder.Property(c => c.ConfigurationJson).HasColumnType("jsonb");

        // Unique constraint: one capability per tenant-module pair
        builder.HasIndex(c => new { c.TenantId, c.ModuleId }).IsUnique();

        builder.HasOne(c => c.Tenant)
            .WithMany()
            .HasForeignKey(c => c.TenantId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(c => c.Module)
            .WithMany()
            .HasForeignKey(c => c.ModuleId)
            .OnDelete(DeleteBehavior.Cascade);

        // Seed default tenant capabilities (Core + Islamic enabled)
        builder.HasData(
            new TenantCapability
            {
                Id = SeedIds.DefaultTenantCoreCapabilityId,
                TenantId = SeedIds.DefaultTenantId,
                ModuleId = SeedIds.ModuleCoreId,
                IsEnabled = true,
                EnabledAt = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc)
            },
            new TenantCapability
            {
                Id = SeedIds.DefaultTenantIslamicCapabilityId,
                TenantId = SeedIds.DefaultTenantId,
                ModuleId = SeedIds.ModuleIslamicId,
                IsEnabled = true,
                EnabledAt = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc)
            }
        );
    }
}
