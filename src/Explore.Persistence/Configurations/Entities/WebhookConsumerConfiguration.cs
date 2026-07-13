// ABOUTME: EF Core configuration for webhook consumers and provider application mappings.
// ABOUTME: Defines tenant-safe ownership relationships, uniqueness, and operational lookup indexes.

using Explore.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Explore.Persistence.Configurations.Entities;

public class WebhookConsumerConfiguration : IEntityTypeConfiguration<WebhookConsumer>
{
    public void Configure(EntityTypeBuilder<WebhookConsumer> builder)
    {
        builder.ToTable("webhook_consumers");

        builder.Property(e => e.Id).HasDefaultValueSql("uuidv7()");
        builder.Property(e => e.Name).HasMaxLength(200).IsRequired();
        builder.Property(e => e.ConsumerKindId).IsRequired();
        builder.Property(e => e.StatusId).IsRequired();
        builder.Property(e => e.ProviderModeId).IsRequired();
        builder.Ignore(e => e.ConsumerKind);
        builder.Ignore(e => e.Status);
        builder.Ignore(e => e.ProviderMode);
        builder.Property(e => e.ExternalProviderAppId).HasMaxLength(500);

        builder.HasAlternateKey(e => new { e.TenantId, e.Id })
            .HasName("ak_webhook_consumers_tenant_id_id");

        builder.HasOne(e => e.Tenant)
            .WithMany()
            .HasForeignKey(e => e.TenantId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(e => e.OwnerActor)
            .WithMany()
            .HasForeignKey(e => new { e.TenantId, e.OwnerActorId })
            .HasPrincipalKey(e => new { e.TenantId, e.Id })
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(e => e.OwnerUser)
            .WithMany()
            .HasForeignKey(e => e.OwnerUserId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(e => e.ConsumerKindLookup)
            .WithMany()
            .HasForeignKey(e => e.ConsumerKindId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(e => e.StatusLookup)
            .WithMany()
            .HasForeignKey(e => e.StatusId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(e => e.ProviderModeLookup)
            .WithMany()
            .HasForeignKey(e => e.ProviderModeId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(e => new { e.TenantId, e.Name })
            .HasDatabaseName("ux_webhook_consumers_tenant_name")
            .IsUnique();

        builder.HasIndex(e => new { e.TenantId, e.StatusId, e.ProviderModeId })
            .HasDatabaseName("ix_webhook_consumers_tenant_status_provider");

        builder.HasIndex(e => new { e.TenantId, e.ExternalProviderAppId })
            .HasDatabaseName("ux_webhook_consumers_tenant_external_app")
            .IsUnique()
            .HasFilter("external_provider_app_id IS NOT NULL");
    }
}
