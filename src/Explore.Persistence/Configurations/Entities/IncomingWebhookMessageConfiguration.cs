// ABOUTME: EF Core configuration for incoming provider callback idempotency and audit rows.
// ABOUTME: Stores verified raw callback metadata as jsonb with provider-message uniqueness per tenant.

using Explore.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Explore.Persistence.Configurations.Entities;

public class IncomingWebhookMessageConfiguration : IEntityTypeConfiguration<IncomingWebhookMessage>
{
    public void Configure(EntityTypeBuilder<IncomingWebhookMessage> builder)
    {
        builder.ToTable("incoming_webhook_messages");

        builder.Property(e => e.Id).HasDefaultValueSql("uuidv7()");
        builder.Property(e => e.Provider).HasMaxLength(100).IsRequired();
        builder.Property(e => e.ProviderMessageId).HasMaxLength(500).IsRequired();
        builder.Property(e => e.IdempotencyKey).HasMaxLength(500);
        builder.Property(e => e.EventType).HasMaxLength(200);
        builder.Property(e => e.HeadersJson).HasColumnType("jsonb");
        builder.Property(e => e.PayloadJson).HasColumnType("jsonb");
        builder.Property(e => e.PayloadHash).HasMaxLength(128).IsRequired();
        builder.Property(e => e.Status).IsRequired();
        builder.Property(e => e.FailureCategory).HasMaxLength(100);
        builder.Property(e => e.SafeDetail).HasMaxLength(1000);

        builder.HasOne(e => e.Tenant)
            .WithMany()
            .HasForeignKey(e => e.TenantId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(e => new { e.TenantId, e.Provider, e.ProviderMessageId })
            .HasDatabaseName("ux_incoming_webhook_messages_tenant_provider_message")
            .IsUnique();

        builder.HasIndex(e => new { e.TenantId, e.Provider, e.IdempotencyKey })
            .HasDatabaseName("ux_incoming_webhook_messages_tenant_provider_idempotency")
            .IsUnique()
            .HasFilter("idempotency_key IS NOT NULL");

        builder.HasIndex(e => new { e.TenantId, e.Status, e.ReceivedAt })
            .HasDatabaseName("ix_incoming_webhook_messages_tenant_status_received");
    }
}
