// ABOUTME: Maps the transactional incoming webhook inbox aggregate and its tenant-safe evidence relationships.
// ABOUTME: Enforces persisted identity, payload-hash, generation, fence, settlement, and claim invariants.

using Explore.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Explore.Persistence.Configurations.Entities;

public class IncomingWebhookMessageConfiguration : IEntityTypeConfiguration<IncomingWebhookMessage>
{
    public void Configure(EntityTypeBuilder<IncomingWebhookMessage> builder)
    {
        builder.ToTable("incoming_webhook_messages", table =>
        {
            table.HasCheckConstraint(
                "ck_incoming_webhook_messages_payload_hash",
                "payload_hash ~ '^sha256:[0-9a-f]{64}$'");
            table.HasCheckConstraint(
                "ck_incoming_webhook_messages_processing_generation",
                "processing_generation >= 1");
            table.HasCheckConstraint(
                "ck_incoming_webhook_messages_processing_fence",
                "processing_fence >= 0");
            table.HasCheckConstraint(
                "ck_incoming_webhook_messages_payload_byte_length",
                "payload_byte_length > 0");
        });

        builder.Property(e => e.Id).HasDefaultValueSql("uuidv7()");
        builder.Property(e => e.Provider).HasMaxLength(IncomingWebhookMessage.MaxProviderLength).IsRequired();
        builder.Property(e => e.ProviderMessageId).HasMaxLength(IncomingWebhookMessage.MaxProviderMessageIdLength).IsRequired();
        builder.Property(e => e.IdempotencyKey).HasMaxLength(IncomingWebhookMessage.MaxIdempotencyKeyLength);
        builder.Property(e => e.EventType).HasMaxLength(IncomingWebhookMessage.MaxEventTypeLength);
        builder.Property(e => e.HeadersJson).HasColumnType("jsonb");
        builder.Ignore(e => e.PayloadBytes);
        builder.Property<byte[]?>("_payloadBytes")
            .HasColumnName("payload_bytes")
            .HasColumnType("bytea");
        builder.Property(e => e.PayloadHash).HasMaxLength(71).IsRequired();
        builder.Property(e => e.PayloadByteLength).IsRequired();
        builder.Property(e => e.PayloadProvenanceId).IsRequired();
        builder.Property(e => e.ContentType).HasMaxLength(IncomingWebhookMessage.MaxContentTypeLength).IsRequired();
        builder.Property(e => e.ContentEncoding).HasMaxLength(IncomingWebhookMessage.MaxContentEncodingLength).IsRequired();
        builder.Property(e => e.StatusId).IsRequired();
        builder.Ignore(e => e.Status);
        builder.Property(e => e.ProcessingGeneration).IsRequired();
        builder.Property(e => e.ProcessingFence).IsRequired().IsConcurrencyToken();
        builder.Property(e => e.ProcessingLeaseOwner).HasMaxLength(IncomingWebhookMessage.MaxLeaseOwnerLength);
        builder.Property(e => e.FailureCategory).HasMaxLength(IncomingWebhookMessage.MaxFailureCodeLength);
        builder.Property(e => e.SafeDetail).HasMaxLength(IncomingWebhookMessage.MaxSafeDetailLength);
        builder.Property(e => e.SettledEffectKind).HasMaxLength(IncomingWebhookEffectReceipt.MaxEffectKindLength);

        builder.HasAlternateKey(e => new { e.TenantId, e.Id })
            .HasName("ak_incoming_webhook_messages_tenant_id");

        builder.HasOne(e => e.Tenant)
            .WithMany()
            .HasForeignKey(e => e.TenantId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(e => e.StatusLookup)
            .WithMany()
            .HasForeignKey(e => e.StatusId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(e => e.PayloadProvenanceLookup)
            .WithMany()
            .HasForeignKey(e => e.PayloadProvenanceId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasMany(e => e.ProcessingAttempts)
            .WithOne(e => e.IncomingWebhookMessage)
            .HasForeignKey(e => new { e.TenantId, e.IncomingWebhookMessageId })
            .HasPrincipalKey(e => new { e.TenantId, e.Id })
            .OnDelete(DeleteBehavior.Restrict);
        builder.Navigation(e => e.ProcessingAttempts).UsePropertyAccessMode(PropertyAccessMode.Field);

        builder.HasMany(e => e.RedriveRecords)
            .WithOne(e => e.IncomingWebhookMessage)
            .HasForeignKey(e => new { e.TenantId, e.IncomingWebhookMessageId })
            .HasPrincipalKey(e => new { e.TenantId, e.Id })
            .OnDelete(DeleteBehavior.Restrict);
        builder.Navigation(e => e.RedriveRecords).UsePropertyAccessMode(PropertyAccessMode.Field);

        builder.HasIndex(e => new { e.TenantId, e.Provider, e.ProviderMessageId })
            .HasDatabaseName("ux_incoming_webhook_messages_tenant_provider_message")
            .IsUnique();

        builder.HasIndex(e => new { e.TenantId, e.Provider, e.IdempotencyKey })
            .HasDatabaseName("ix_incoming_webhook_messages_tenant_provider_idempotency")
            .HasFilter("idempotency_key IS NOT NULL");

        builder.HasIndex(e => new { e.TenantId, e.StatusId, e.NextAttemptAt, e.ProcessingLeaseExpiresAt })
            .HasDatabaseName("ix_incoming_webhook_messages_claim_due");

        builder.HasIndex(e => new { e.TenantId, e.StatusId, e.ReceivedAt })
            .HasDatabaseName("ix_incoming_webhook_messages_tenant_status_received");
    }
}
