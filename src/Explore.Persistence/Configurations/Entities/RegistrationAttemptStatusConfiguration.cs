// ABOUTME: Maps stable registration-attempt lifecycle lookup rows without migration-owned seed data.
// ABOUTME: Keeps integer identities and master codes unique for runtime seeding.

using Explore.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Explore.Persistence.Configurations.Entities;

public sealed class RegistrationAttemptStatusConfiguration : IEntityTypeConfiguration<RegistrationAttemptStatus>
{
    public void Configure(EntityTypeBuilder<RegistrationAttemptStatus> builder)
    {
        builder.ToTable("registration_attempt_statuses");
        builder.Property(status => status.Id).ValueGeneratedNever();
        builder.Property(status => status.MasterCode).IsRequired().HasMaxLength(100);
        builder.Property(status => status.FullName).IsRequired().HasMaxLength(200);
        builder.Property(status => status.Description).HasMaxLength(500);
        builder.HasIndex(status => status.MasterCode).IsUnique();
    }
}
