// ABOUTME: EF configuration for immutable instance platform contribution setting versions.
// ABOUTME: Preserves stored copy, option field access, and active-version uniqueness.

using Explore.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Explore.Persistence.Configurations.Entities;

public sealed class PlatformContributionSettingConfiguration : IEntityTypeConfiguration<PlatformContributionSetting>
{
    public void Configure(EntityTypeBuilder<PlatformContributionSetting> builder)
    {
        builder.ToTable("platform_contribution_settings");
        builder.Property(setting => setting.Id).ValueGeneratedNever();
        builder.Property(setting => setting.CreatedAt).IsRequired();
        builder.Property(setting => setting.Heading).IsRequired().HasMaxLength(200);
        builder.Property(setting => setting.Body).IsRequired().HasMaxLength(2000);
        builder.HasMany(setting => setting.Options).WithOne().HasForeignKey("PlatformContributionSettingId").OnDelete(DeleteBehavior.Restrict);
        builder.Navigation(setting => setting.Options).HasField("_options").UsePropertyAccessMode(PropertyAccessMode.Field);
        builder.HasIndex(setting => setting.VersionNumber).IsUnique();
        builder.HasIndex(setting => setting.IsActive).IsUnique().HasFilter("is_active = true");
    }
}
