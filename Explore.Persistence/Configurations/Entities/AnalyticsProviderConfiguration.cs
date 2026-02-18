using Explore.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Explore.Persistence.Configurations.Entities;

public class AnalyticsProviderConfiguration : IEntityTypeConfiguration<AnalyticsProvider>
{
    public void Configure(EntityTypeBuilder<AnalyticsProvider> builder)
    {
        builder.Property(e => e.Id).ValueGeneratedNever();

        builder.Property(e => e.MasterCode).HasMaxLength(500).IsRequired();
        builder.Property(e => e.FullName).HasMaxLength(500).IsRequired();
        builder.Property(e => e.Description).HasMaxLength(500);
    }
}
