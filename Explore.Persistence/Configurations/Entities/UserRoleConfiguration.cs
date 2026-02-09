using Explore.Domain;
using Explore.Persistence.Seed;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Explore.Persistence.Configurations.Entities;

public class UserRoleConfiguration : IEntityTypeConfiguration<UserRole>
{
    public void Configure(EntityTypeBuilder<UserRole> builder)
    {
        builder.Property(e => e.Id).ValueGeneratedNever();

        builder.Property(e => e.FullName).HasMaxLength(500).IsRequired();
        builder.Property(e => e.MasterCode).HasMaxLength(500).IsRequired();
        builder.Property(e => e.Description).HasMaxLength(500);

        builder.HasOne(e => e.Tenant)
            .WithMany()
            .HasForeignKey(e => e.TenantId)
            .OnDelete(DeleteBehavior.Restrict);

        // NOTE: UserRole seed data moved to DatabaseSeeder for conditional (Development-only) seeding.
        // UserRoles are tenant-scoped so they require the tenant to exist first.
        // See Explore.Persistence/Seed/DatabaseSeeder.cs and SeedData.cs
    }
}
