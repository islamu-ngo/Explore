// ABOUTME: Maps stable advance-registration-obligation lookup rows without model-owned seed data.
// ABOUTME: Keeps integer IDs and durable business codes aligned with runtime lookup repair.

using Explore.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Explore.Persistence.Configurations.Entities;

public sealed class AdvanceRegistrationObligationConfiguration
    : IEntityTypeConfiguration<AdvanceRegistrationObligation>
{
    public void Configure(EntityTypeBuilder<AdvanceRegistrationObligation> builder)
    {
        builder.ToTable("advance_registration_obligations");
        builder.Property(row => row.Id).ValueGeneratedNever();
        builder.Property(row => row.MasterCode).IsRequired().HasMaxLength(100);
        builder.Property(row => row.FullName).IsRequired().HasMaxLength(200);
        builder.Property(row => row.Description).HasMaxLength(500);
        builder.HasIndex(row => row.MasterCode).IsUnique();
    }
}
