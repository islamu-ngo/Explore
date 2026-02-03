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
        builder.HasIndex(e => e.Key)
            .IsUnique();

        builder.Property(e => e.Key)
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
        builder.HasData(
            new SystemSetting
            {
                Id = SeedIds.SystemSettingDeploymentModeId,
                Key = "deployment.mode",
                Value = "\"MultiTenant\"",
                ValueType = SettingValueType.String,
                IsLocked = true,
                AllowedValues = "[\"SingleTenant\", \"MultiTenant\"]",
                Description = "Deployment mode of the application",
                Category = "System",
                DisplayOrder = 1,
                CreatedAt = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc)
            },
            new SystemSetting
            {
                Id = SeedIds.SystemSettingMaxSessionsPerEventId,
                Key = "events.max_sessions_per_event",
                Value = "100",
                ValueType = SettingValueType.Integer,
                IsLocked = false,
                Description = "Maximum number of sessions allowed per event",
                Category = "Events",
                DisplayOrder = 1,
                CreatedAt = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc)
            },
            new SystemSetting
            {
                Id = SeedIds.SystemSettingRequireApprovalId,
                Key = "events.require_approval",
                Value = "false",
                ValueType = SettingValueType.Boolean,
                IsLocked = false,
                Description = "Whether events require admin approval before publishing",
                Category = "Events",
                DisplayOrder = 2,
                CreatedAt = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc)
            },
            new SystemSetting
            {
                Id = SeedIds.SystemSettingIslamicModuleId,
                Key = "modules.islamic_enabled",
                Value = "true",
                ValueType = SettingValueType.Boolean,
                IsLocked = false,
                Description = "Enable Islamic event module",
                Category = "Modules",
                DisplayOrder = 1,
                CreatedAt = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc)
            },
            new SystemSetting
            {
                Id = SeedIds.SystemSettingTechModuleId,
                Key = "modules.tech_enabled",
                Value = "true",
                ValueType = SettingValueType.Boolean,
                IsLocked = false,
                Description = "Enable Tech event module",
                Category = "Modules",
                DisplayOrder = 2,
                CreatedAt = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc)
            }
        );
    }
}
