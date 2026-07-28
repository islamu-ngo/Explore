// ABOUTME: EF configuration for instance platform contribution percentage options.
// ABOUTME: Preserves integer basis-point storage and per-setting uniqueness constraints.

using Explore.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Explore.Persistence.Configurations.Entities;

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
