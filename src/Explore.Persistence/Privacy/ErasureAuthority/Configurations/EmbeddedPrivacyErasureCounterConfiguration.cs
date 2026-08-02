// ABOUTME: Maps the embedded authority's singleton monotonic sequence allocator.
// ABOUTME: Enforces exactly the true singleton key and a non-negative sequence.

using Explore.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Explore.Persistence.Privacy.ErasureAuthority.Configurations;

public sealed class EmbeddedPrivacyErasureCounterConfiguration
    : IEntityTypeConfiguration<PrivacyErasureCounter>
{
    public void Configure(EntityTypeBuilder<PrivacyErasureCounter> builder)
    {
        builder.ToTable("authority_counter", table =>
        {
            table.HasCheckConstraint("ck_authority_counter_singleton", "singleton = 1");
            table.HasCheckConstraint("ck_authority_counter_nonnegative", "last_sequence >= 0");
        });
        builder.HasKey(item => item.Singleton);
        builder.Property(item => item.Singleton).ValueGeneratedNever();
    }
}
