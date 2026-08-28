// ABOUTME: EF configuration for RegistrationScope lookup - stable int ids, unique master code, seeded by LookupTableSeeder at runtime.
// ABOUTME: Supports the retained registration-scope workflow lookup.

using Explore.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Explore.Persistence.Configurations.Entities;

public class RegistrationScopeConfiguration : IEntityTypeConfiguration<RegistrationScope>
{
    public void Configure(EntityTypeBuilder<RegistrationScope> builder)
    {
        builder.Property(e => e.Id).ValueGeneratedNever();

        builder.Property(e => e.MasterCode).IsRequired().HasMaxLength(100);
        builder.Property(e => e.FullName).IsRequired().HasMaxLength(200);
        builder.Property(e => e.Description).HasMaxLength(500);

        builder.HasIndex(e => e.MasterCode)
            .IsUnique();
    }
}
