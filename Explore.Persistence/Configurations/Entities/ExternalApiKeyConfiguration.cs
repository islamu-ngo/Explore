// ABOUTME: EF Core configuration for persisted external API key credentials.
// ABOUTME: Enforces unique public key ids, optional tenant ownership, status/credit-period FK lookups, and credit quota fields.

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
        builder.Property(e => e.Description).HasMaxLength(1000);
        builder.Property(e => e.KeyId).HasMaxLength(64).IsRequired();
        builder.Property(e => e.SecretHash).HasMaxLength(500).IsRequired();
        builder.Property(e => e.Scopes).HasMaxLength(1000).IsRequired();
        builder.Property(e => e.LastUsedIp).HasMaxLength(64);
        builder.Property(e => e.OwnerType).HasConversion<int>().IsRequired();

        builder.Property(e => e.ExternalApiKeyStatusId)
            .HasDefaultValue((int)ExternalApiKeyStatusEnum.Active)
            .IsRequired();

        builder.Property(e => e.ExternalApiKeyCreditPeriodId)
            .HasDefaultValue((int)ExternalApiKeyCreditPeriodEnum.None)
            .IsRequired();

        builder.Property(e => e.TenantId).IsRequired(false);

        builder.HasIndex(e => e.KeyId).IsUnique();
        builder.HasIndex(e => new { e.TenantId, e.OwnerType, e.OwnerId });
        builder.HasIndex(e => new { e.TenantId, e.ExternalApiKeyStatusId });

        builder.HasOne(e => e.Tenant)
            .WithMany()
            .HasForeignKey(e => e.TenantId)
            .IsRequired(false)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(e => e.ExternalApiKeyStatus)
            .WithMany()
            .HasForeignKey(e => e.ExternalApiKeyStatusId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(e => e.ExternalApiKeyCreditPeriod)
            .WithMany()
            .HasForeignKey(e => e.ExternalApiKeyCreditPeriodId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
