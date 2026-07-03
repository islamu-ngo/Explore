// ABOUTME: EF Core configuration for external webhook provider object links.
// ABOUTME: Tracks Svix app, endpoint, and message ids without making provider ids authoritative.

using Explore.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Explore.Persistence.Configurations.Entities;

public class WebhookProviderLinkConfiguration : IEntityTypeConfiguration<WebhookProviderLink>
{
    public void Configure(EntityTypeBuilder<WebhookProviderLink> builder)
    {
        builder.ToTable("webhook_provider_links");

        builder.Property(e => e.Id).HasDefaultValueSql("uuidv7()");
        builder.Property(e => e.Provider).IsRequired();
        builder.Property(e => e.ExternalAppId).HasMaxLength(500);
        builder.Property(e => e.ExternalEndpointId).HasMaxLength(500);
        builder.Property(e => e.ExternalMessageId).HasMaxLength(500);
        builder.Property(e => e.SyncState).IsRequired();
        builder.Property(e => e.LastErrorCategory).HasMaxLength(100);

        builder.HasOne(e => e.Tenant)
            .WithMany()
            .HasForeignKey(e => e.TenantId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(e => e.Consumer)
            .WithMany()
            .HasForeignKey(e => new { e.TenantId, e.ConsumerId })
            .HasPrincipalKey(e => new { e.TenantId, e.Id })
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(e => e.Endpoint)
            .WithMany()
            .HasForeignKey(e => new { e.TenantId, e.EndpointId })
            .HasPrincipalKey(e => new { e.TenantId, e.Id })
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(e => e.Message)
            .WithMany()
            .HasForeignKey(e => new { e.TenantId, e.MessageId })
            .HasPrincipalKey(e => new { e.TenantId, e.Id })
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(e => new { e.Provider, e.SyncState, e.CreatedAt })
            .HasDatabaseName("ix_webhook_provider_links_provider_sync_state");

        builder.HasIndex(e => new { e.TenantId, e.Provider, e.ExternalAppId })
            .HasDatabaseName("ux_webhook_provider_links_tenant_provider_app")
            .IsUnique()
            .HasFilter("external_app_id IS NOT NULL");

        builder.HasIndex(e => new { e.TenantId, e.Provider, e.ExternalEndpointId })
            .HasDatabaseName("ux_webhook_provider_links_tenant_provider_endpoint")
            .IsUnique()
            .HasFilter("external_endpoint_id IS NOT NULL");

        builder.HasIndex(e => new { e.TenantId, e.Provider, e.ExternalMessageId })
            .HasDatabaseName("ux_webhook_provider_links_tenant_provider_message")
            .IsUnique()
            .HasFilter("external_message_id IS NOT NULL");
    }
}
