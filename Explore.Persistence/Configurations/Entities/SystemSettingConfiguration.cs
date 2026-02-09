// ABOUTME: EF Core configuration for SystemSetting entity with UUID v7 generation
// and unique constraint on Key.

namespace Explore.Persistence.Configurations.Entities;

using Explore.Domain;
using Explore.Persistence.Seed;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

public class SystemSettingConfiguration : IEntityTypeConfiguration<SystemSetting>
{
    public void Configure(EntityTypeBuilder<SystemSetting> builder)
    {
        builder.HasKey(e => e.Id);

        // UUID v7 generation for better index performance
        builder.Property(e => e.Id)
            .HasDefaultValueSql("uuidv7()");

        // Unique constraint on setting key
        builder.HasIndex(e => e.SettingKey)
            .IsUnique();

        builder.Property(e => e.SettingKey)
            .IsRequired()
            .HasMaxLength(256);

        builder.Property(e => e.Value)
            .IsRequired();

        builder.Property(e => e.ValueType)
            .IsRequired()
            .HasConversion<int>();

        builder.Property(e => e.IsLocked)
            .IsRequired()
            .HasDefaultValue(false);

        builder.Property(e => e.AllowedValues)
            .HasColumnType("jsonb");

        builder.Property(e => e.Description)
            .HasMaxLength(1000);

        builder.Property(e => e.Category)
            .HasMaxLength(100);

        builder.Property(e => e.DisplayOrder)
            .HasDefaultValue(0);

        builder.Property(e => e.CreatedAt)
            .IsRequired()
            .HasDefaultValueSql("NOW()");

        // Seed initial system settings using centralized SeedIds
    }
}

