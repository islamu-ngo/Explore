// ABOUTME: EF Core configuration for tenant administrator role lookup values.
// ABOUTME: Seeds deterministic role IDs aligned with TenantAdministratorRoleEnum.

using Explore.Domain;
using Explore.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Explore.Persistence.Configurations.Entities;

public class TenantAdministratorRoleConfiguration : IEntityTypeConfiguration<TenantAdministratorRole>
{
    public void Configure(EntityTypeBuilder<TenantAdministratorRole> builder)
    {
        builder.ToTable("TenantAdministratorRoles");

        builder.HasKey(e => e.Id);

        builder.Property(e => e.Id)
            .ValueGeneratedNever();

        builder.Property(e => e.FullName)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(e => e.MasterCode)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(e => e.Description)
            .HasMaxLength(500);

        builder.HasIndex(e => e.MasterCode)
            .IsUnique();

        // NOTE: Lookup seed data moved to LookupTableSeeder for runtime seeding.
        // See Explore.Persistence/Seed/LookupTableSeeder.cs
    }
}
