// ABOUTME: EF mappings for immutable instance-scoped platform fee and contribution configuration history.
// ABOUTME: Stores money in bigint minor units and percentages in integer basis points with active-version guards.

using Explore.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Explore.Persistence.Configurations.Entities;

public sealed class PlatformFeePolicyConfiguration : IEntityTypeConfiguration<PlatformFeePolicy>
{
    public void Configure(EntityTypeBuilder<PlatformFeePolicy> builder)
    {
        builder.ToTable("platform_fee_policies");
        builder.Property(policy => policy.Id).ValueGeneratedNever();
        builder.Property(policy => policy.CreatedAt).IsRequired();
        builder.Property(policy => policy.FeeBasisPoints).HasColumnType("integer");
        builder.HasMany(policy => policy.FixedCharges).WithOne().HasForeignKey("PlatformFeePolicyId").OnDelete(DeleteBehavior.Restrict);
        builder.Navigation(policy => policy.FixedCharges).HasField("_fixedCharges").UsePropertyAccessMode(PropertyAccessMode.Field);
        builder.HasIndex(policy => policy.VersionNumber).IsUnique();
        builder.HasIndex(policy => policy.IsActive).IsUnique().HasFilter("is_active = true");
    }
}

public sealed class PlatformFeeFixedChargeConfiguration : IEntityTypeConfiguration<PlatformFeeFixedCharge>
{
    public void Configure(EntityTypeBuilder<PlatformFeeFixedCharge> builder)
    {
        builder.ToTable("platform_fee_fixed_charges");
        builder.Property(charge => charge.Id).ValueGeneratedNever();
        builder.Property(charge => charge.CurrencyCode).IsRequired().HasMaxLength(3);
        builder.Property(charge => charge.AmountMinor).HasColumnType("bigint");
        builder.HasIndex("PlatformFeePolicyId", nameof(PlatformFeeFixedCharge.CurrencyCode)).IsUnique();
    }
}

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

public sealed class PlatformContributionOptionConfiguration : IEntityTypeConfiguration<PlatformContributionOption>
{
    public void Configure(EntityTypeBuilder<PlatformContributionOption> builder)
    {
        builder.ToTable("platform_contribution_options");
        builder.Property(option => option.Id).ValueGeneratedNever();
        builder.Property(option => option.ContributionBasisPoints).HasColumnType("integer");
        builder.HasIndex("PlatformContributionSettingId", nameof(PlatformContributionOption.SortOrder)).IsUnique();
        builder.HasIndex("PlatformContributionSettingId", nameof(PlatformContributionOption.ContributionBasisPoints)).IsUnique();
    }
}
