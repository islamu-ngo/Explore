// ABOUTME: EF Core configuration for webhook endpoints managed by LocalProvider or mirrored from Svix.
// ABOUTME: Enforces tenant-safe consumer ownership and indexes provider endpoint lookup state.

using Explore.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Explore.Persistence.Configurations.Entities;

public class WebhookEndpointConfiguration : IEntityTypeConfiguration<WebhookEndpoint>
{
    public void Configure(EntityTypeBuilder<WebhookEndpoint> builder)
    {
        builder.ToTable("webhook_endpoints");

        builder.Property(e => e.Id).HasDefaultValueSql("uuidv7()");
        builder.Property(e => e.Url).HasMaxLength(2048).IsRequired();
        builder.Property(e => e.Description).HasMaxLength(1000);
        builder.Property(e => e.StatusId).IsRequired();
        builder.Ignore(e => e.Status);
        builder.Property(e => e.SecretRef).HasMaxLength(500).IsRequired();
        builder.Property(e => e.SecretVersion).HasDefaultValue(1);
        builder.Property(e => e.PreviousSecretRef).HasMaxLength(500);
        builder.Property(e => e.ProviderEndpointId).HasMaxLength(500);
        builder.Property(e => e.MaxAttempts).HasDefaultValue(8);
        builder.Property(e => e.TimeoutSeconds).HasDefaultValue(15);
        builder.Property(e => e.ConsecutiveFailureCount).HasDefaultValue(0);
        builder.Property(e => e.AutoPauseReason).HasMaxLength(100);
        builder.Property(e => e.DeliveryStateVersion).HasDefaultValue(0).IsConcurrencyToken();

        builder.HasAlternateKey(e => new { e.TenantId, e.Id })
            .HasName("ak_webhook_endpoints_tenant_id_id");

        builder.HasOne(e => e.Tenant)
            .WithMany()
            .HasForeignKey(e => e.TenantId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(e => e.Consumer)
            .WithMany()
            .HasForeignKey(e => new { e.TenantId, e.ConsumerId })
            .HasPrincipalKey(e => new { e.TenantId, e.Id })
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(e => e.StatusLookup)
            .WithMany()
            .HasForeignKey(e => e.StatusId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(e => new { e.TenantId, e.ConsumerId, e.StatusId })
            .HasDatabaseName("ix_webhook_endpoints_tenant_consumer_status");

        builder.HasIndex(e => new { e.TenantId, e.StatusId, e.Id })
            .HasDatabaseName("ix_webhook_endpoints_status_tenant_id");

        builder.HasIndex(e => new { e.TenantId, e.ProviderEndpointId })
            .HasDatabaseName("ux_webhook_endpoints_tenant_provider_endpoint")
            .IsUnique()
            .HasFilter("provider_endpoint_id IS NOT NULL");
    }
}
