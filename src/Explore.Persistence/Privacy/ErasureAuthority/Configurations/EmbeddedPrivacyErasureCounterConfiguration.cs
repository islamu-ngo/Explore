// ABOUTME: Maps the SQLite authority's singleton monotonic sequence allocator.
// ABOUTME: Uses the fixed ie_ namespace and enforces a non-negative sequence.

using Explore.Domain;
using Explore.Persistence.Schema;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Explore.Persistence.Privacy.ErasureAuthority.Configurations;

public sealed class EmbeddedPrivacyErasureCounterConfiguration
    : IEntityTypeConfiguration<PrivacyErasureCounter>
{
    public void Configure(EntityTypeBuilder<PrivacyErasureCounter> builder)
    {
        builder.ToTable(RelationalModelNamespace.Prefix + "authority_counter", table =>
        {
            table.HasCheckConstraint("ck_authority_counter_singleton", "singleton = 1");
            table.HasCheckConstraint("ck_authority_counter_nonnegative", "last_sequence >= 0");
        });
        builder.HasKey(item => item.Singleton);
        builder.Property(item => item.Singleton).ValueGeneratedNever();
    }
}
