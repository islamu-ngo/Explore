// ABOUTME: EF Core configuration for persisted external API key credentials.
// ABOUTME: Enforces unique public key ids, tenant ownership indexes, and restrictive tenant foreign keys.

using Explore.Domain;
using Explore.Domain.Enums;
using Explore.Persistence.ValueGenerators;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Explore.Persistence.Configurations.Entities;

public class ExternalApiKeyConfiguration : IEntityTypeConfiguration<ExternalApiKey>
{
    public void Configure(EntityTypeBuilder<ExternalApiKey> builder)
    {
        builder.Property(e => e.Id).HasValueGenerator<GuidVersion7ValueGenerator>();

        builder.Property(e => e.Name).HasMaxLength(200).IsRequired();
        builder.Property(e => e.KeyId).HasMaxLength(64).IsRequired();
        builder.Property(e => e.SecretHash).HasMaxLength(500).IsRequired();
        builder.Property(e => e.Scopes).HasMaxLength(1000).IsRequired();
        builder.Property(e => e.LastUsedIp).HasMaxLength(64);
        builder.Property(e => e.OwnerType).HasConversion<int>().IsRequired();
        builder.Property(e => e.Status)
            .HasConversion<int>()
            .HasDefaultValue(ExternalApiKeyStatus.Active)
            .IsRequired();

        builder.HasIndex(e => e.KeyId).IsUnique();
        builder.HasIndex(e => new { e.TenantId, e.OwnerType, e.OwnerId });
        builder.HasIndex(e => new { e.TenantId, e.Status });

        builder.HasOne(e => e.Tenant)
            .WithMany()
            .HasForeignKey(e => e.TenantId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
