// ABOUTME: EF Core configuration for endpoint event type subscription rows.
// ABOUTME: Keeps LocalProvider fanout filtering tenant-safe while event type definitions remain global.

using Explore.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Explore.Persistence.Configurations.Entities;

public class WebhookEndpointSubscriptionConfiguration : IEntityTypeConfiguration<WebhookEndpointSubscription>
{
    public void Configure(EntityTypeBuilder<WebhookEndpointSubscription> builder)
    {
        builder.ToTable("webhook_endpoint_subscriptions");

        builder.Property(e => e.Id).HasDefaultValueSql("uuidv7()");
        builder.Property(e => e.IsEnabled).HasDefaultValue(true);

        builder.HasAlternateKey(e => new { e.TenantId, e.Id })
            .HasName("ak_webhook_endpoint_subscriptions_tenant_id_id");

        builder.HasOne(e => e.Tenant)
            .WithMany()
            .HasForeignKey(e => e.TenantId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(e => e.Endpoint)
            .WithMany(e => e.Subscriptions)
            .HasForeignKey(e => new { e.TenantId, e.EndpointId })
            .HasPrincipalKey(e => new { e.TenantId, e.Id })
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(e => e.EventType)
            .WithMany()
            .HasForeignKey(e => e.EventTypeId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(e => new { e.TenantId, e.EndpointId, e.EventTypeId })
            .HasDatabaseName("ux_webhook_endpoint_subscriptions_endpoint_event_type")
            .IsUnique();

        builder.HasIndex(e => new { e.TenantId, e.EventTypeId, e.IsEnabled })
            .HasDatabaseName("ix_webhook_endpoint_subscriptions_tenant_event_type");
    }
}
