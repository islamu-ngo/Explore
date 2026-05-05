// ABOUTME: EF Core configuration for external API key owner type lookup values.
// ABOUTME: Maps ExternalApiKeyOwnerTypeLookup to the external_api_key_owner_types table.

using Explore.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Explore.Persistence.Configurations.Entities;

public class ExternalApiKeyOwnerTypeLookupConfiguration : IEntityTypeConfiguration<ExternalApiKeyOwnerTypeLookup>
{
    public void Configure(EntityTypeBuilder<ExternalApiKeyOwnerTypeLookup> builder)
    {
        builder.ToTable("external_api_key_owner_types");
        builder.Property(e => e.Id).ValueGeneratedNever();
        builder.Property(e => e.MasterCode).IsRequired().HasMaxLength(50);
        builder.Property(e => e.FullName).IsRequired().HasMaxLength(100);
        builder.Property(e => e.Description).HasMaxLength(500);
        builder.HasIndex(e => e.MasterCode).IsUnique();
    }
}
