// ABOUTME: Maps immutable local policy coverage for replayed privacy-erasure intents.
// ABOUTME: Keys every row by intent, User subject kind, and applied policy version.

using Explore.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Explore.Persistence.Configurations.Entities;

public sealed class PrivacyErasurePolicyCoverageConfiguration
    : IEntityTypeConfiguration<PrivacyErasurePolicyCoverage>
{
    public void Configure(EntityTypeBuilder<PrivacyErasurePolicyCoverage> builder)
    {
        builder.ToTable(table =>
        {
            table.HasCheckConstraint(
                "ck_privacy_erasure_policy_coverage_subject_kind",
                "subject_kind = 1");
            table.HasCheckConstraint(
                "ck_privacy_erasure_policy_coverage_policy_version",
                "policy_version > 0");
        });

        builder.HasKey(item => new { item.IntentId, item.SubjectKind, item.PolicyVersion });
        builder.Property(item => item.SubjectKind).HasConversion<short>();
    }
}
