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
        builder.ToTable("webhook_messages", table =>
        {
            table.HasCheckConstraint(
                "ck_webhook_messages_payload_hash",
                "payload_hash ~ '^sha256:[0-9a-f]{64}$'");
            table.HasCheckConstraint(
                "ck_webhook_messages_payload_provenance",
                "payload_provenance_id > 0");
        });

        builder.Property(e => e.Id).HasDefaultValueSql("uuidv7()");
        builder.Property(e => e.EventType).HasMaxLength(WebhookMessage.MaxEventTypeLength).IsRequired();
        builder.Property(e => e.EventId).HasMaxLength(WebhookMessage.MaxEventIdLength).IsRequired();
        builder.Property(e => e.AggregateKind).HasMaxLength(WebhookMessage.MaxAggregateKindLength).IsRequired();
        builder.Property<byte[]?>("_payloadBytes")
            .HasColumnName("payload_bytes")
            .HasColumnType("bytea");
        builder.Property(e => e.PayloadHash).HasMaxLength(WebhookMessage.PayloadHashLength).IsRequired();
        builder.Property(e => e.PayloadProvenanceId).IsRequired();

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

        builder.HasIndex(e => new { e.TenantId, e.CreatedAt, e.Id })
            .HasDatabaseName("ix_webhook_messages_tenant_created");

        builder.HasIndex(e => new { e.TenantId, e.EventType, e.EventId })
            .HasDatabaseName("ux_webhook_messages_tenant_event")
            .IsUnique();

        builder.HasIndex(e => new { e.TenantId, e.AggregateKind, e.AggregateId })
            .HasDatabaseName("ix_webhook_messages_tenant_aggregate");

        builder.HasIndex(e => new { e.TenantId, e.PayloadRetentionUntil })
            .HasDatabaseName("ix_webhook_messages_tenant_payload_retention")
            .HasFilter("payload_bytes IS NOT NULL");
    }
}
