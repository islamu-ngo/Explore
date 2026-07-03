// ABOUTME: EF Core configuration for canonical outgoing webhook message envelopes.
// ABOUTME: Adds provider, aggregate, idempotency, and retention indexes for outbox-backed dispatch.

using Explore.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Explore.Persistence.Configurations.Entities;

public class WebhookMessageConfiguration : IEntityTypeConfiguration<WebhookMessage>
{
    public void Configure(EntityTypeBuilder<WebhookMessage> builder)
    {
        builder.ToTable("webhook_messages");

        builder.Property(e => e.Id).HasDefaultValueSql("uuidv7()");
        builder.Property(e => e.EventType).HasMaxLength(200).IsRequired();
        builder.Property(e => e.EventId).HasMaxLength(200).IsRequired();
        builder.Property(e => e.AggregateKind).HasMaxLength(100).IsRequired();
        builder.Property(e => e.PayloadJson).HasColumnType("jsonb");
        builder.Property(e => e.PayloadHash).HasMaxLength(128).IsRequired();
        builder.Property(e => e.ProviderMode).IsRequired();
        builder.Property(e => e.ProviderMessageId).HasMaxLength(500);
        builder.Property(e => e.Status).IsRequired();

        builder.HasAlternateKey(e => new { e.TenantId, e.Id })
            .HasName("ak_webhook_messages_tenant_id_id");

        builder.HasOne(e => e.Tenant)
            .WithMany()
            .HasForeignKey(e => e.TenantId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(e => e.Consumer)
            .WithMany()
            .HasForeignKey(e => new { e.TenantId, e.ConsumerId })
            .HasPrincipalKey(e => new { e.TenantId, e.Id })
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(e => new { e.TenantId, e.Status, e.CreatedAt })
            .HasDatabaseName("ix_webhook_messages_tenant_status_created");

        builder.HasIndex(e => new { e.TenantId, e.EventType, e.EventId })
            .HasDatabaseName("ux_webhook_messages_tenant_event")
            .IsUnique();

        builder.HasIndex(e => new { e.TenantId, e.AggregateKind, e.AggregateId })
            .HasDatabaseName("ix_webhook_messages_tenant_aggregate");

        builder.HasIndex(e => new { e.TenantId, e.PayloadRetentionUntil })
            .HasDatabaseName("ix_webhook_messages_tenant_payload_retention")
            .HasFilter("payload_json IS NOT NULL");

        builder.HasIndex(e => new { e.TenantId, e.ProviderMessageId })
            .HasDatabaseName("ux_webhook_messages_tenant_provider_message")
            .IsUnique()
            .HasFilter("provider_message_id IS NOT NULL");
    }
}
