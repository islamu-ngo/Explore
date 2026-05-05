// ABOUTME: EF Core configuration for secret source type lookup values.
// ABOUTME: Maps SecretSourceTypeLookup to the secret_source_types table.

using Explore.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Explore.Persistence.Configurations.Entities;

public class SecretSourceTypeLookupConfiguration : IEntityTypeConfiguration<SecretSourceTypeLookup>
{
    public void Configure(EntityTypeBuilder<SecretSourceTypeLookup> builder)
    {
        builder.ToTable("secret_source_types");
        builder.Property(e => e.Id).ValueGeneratedNever();
        builder.Property(e => e.MasterCode).IsRequired().HasMaxLength(50);
        builder.Property(e => e.FullName).IsRequired().HasMaxLength(100);
        builder.Property(e => e.Description).HasMaxLength(500);
        builder.HasIndex(e => e.MasterCode).IsUnique();
    }
}
