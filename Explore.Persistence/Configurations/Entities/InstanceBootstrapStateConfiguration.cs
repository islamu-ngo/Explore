// ABOUTME: EF Core configuration for platform instance bootstrap state persistence.
// ABOUTME: Ensures one durable onboarding completion marker is stored in the database.

using Explore.Domain;
using Explore.Persistence.ValueGenerators;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Explore.Persistence.Configurations.Entities;

public class InstanceBootstrapStateConfiguration : IEntityTypeConfiguration<InstanceBootstrapState>
{
    public void Configure(EntityTypeBuilder<InstanceBootstrapState> builder)
    {
        builder.HasKey(e => e.Id);

        builder.Property(e => e.Id)
            .HasValueGenerator<GuidVersion7ValueGenerator>();

        builder.Property(e => e.IsCompleted)
            .IsRequired()
            .HasDefaultValue(false);

        builder.Property(e => e.CreatedAt)
            .IsRequired()
            .HasDefaultValueSql("NOW()");

        builder.Property(e => e.SelectedDeploymentMode)
            .HasMaxLength(32);
    }
}
