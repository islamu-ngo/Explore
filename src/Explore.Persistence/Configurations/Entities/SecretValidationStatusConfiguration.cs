// ABOUTME: EF Core configuration for secret validation status lookup values.
// ABOUTME: Maps SecretValidationStatus to the secret_validation_statuses table.

using Explore.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Explore.Persistence.Configurations.Entities;

public class SecretValidationStatusConfiguration : IEntityTypeConfiguration<SecretValidationStatus>
{
    public void Configure(EntityTypeBuilder<SecretValidationStatus> builder)
    {
        builder.ToTable("secret_validation_statuses");
        builder.Property(e => e.Id).ValueGeneratedNever();
        builder.Property(e => e.MasterCode).IsRequired().HasMaxLength(50);
        builder.Property(e => e.FullName).IsRequired().HasMaxLength(100);
        builder.Property(e => e.Description).HasMaxLength(500);
        builder.HasIndex(e => e.MasterCode).IsUnique();
    }
}
