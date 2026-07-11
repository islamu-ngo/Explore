// ABOUTME: EF Core configuration for setting value type lookup values.
// ABOUTME: Maps SettingValueTypeLookup to the setting_value_types table.

using Explore.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Explore.Persistence.Configurations.Entities;

public class SettingValueTypeLookupConfiguration : IEntityTypeConfiguration<SettingValueTypeLookup>
{
    public void Configure(EntityTypeBuilder<SettingValueTypeLookup> builder)
    {
        builder.ToTable("setting_value_types");
        builder.Property(e => e.Id).ValueGeneratedNever();
        builder.Property(e => e.MasterCode).IsRequired().HasMaxLength(50);
        builder.Property(e => e.FullName).IsRequired().HasMaxLength(100);
        builder.Property(e => e.Description).HasMaxLength(500);
        builder.HasIndex(e => e.MasterCode).IsUnique();
    }
}
