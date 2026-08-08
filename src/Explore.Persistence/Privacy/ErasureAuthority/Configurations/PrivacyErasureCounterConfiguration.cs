// ABOUTME: Maps the singleton monotonic platform privacy-erasure sequence allocator.
// ABOUTME: Enforces one true-key row and a non-negative last allocated sequence.

using Explore.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Explore.Persistence.Privacy.ErasureAuthority.Configurations;

public sealed class PrivacyErasureCounterConfiguration
    : IEntityTypeConfiguration<PrivacyErasureCounter>
{
    public void Configure(EntityTypeBuilder<PrivacyErasureCounter> builder)
    {
        builder.ToTable("authority_counter", table =>
        {
            table.HasCheckConstraint("ck_privacy_erasure_authority_counter_singleton", "singleton");
            table.HasCheckConstraint("ck_privacy_erasure_authority_counter_nonnegative", "last_sequence >= 0");
        });
        builder.HasKey(item => item.Singleton);
        builder.Property(item => item.Singleton).ValueGeneratedNever();
    }
}
