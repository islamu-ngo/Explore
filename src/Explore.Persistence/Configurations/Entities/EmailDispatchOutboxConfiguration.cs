// ABOUTME: EF Core configuration for specialized email dispatch outbox, attempts, and receipts.
// ABOUTME: Adds worker-poll, uniqueness, and operator-status indexes for Basic Dispatch Mode.

using Explore.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Explore.Persistence.Configurations.Entities;

public class EmailDispatchOutboxConfiguration : IEntityTypeConfiguration<EmailDispatchOutbox>
{
    public void Configure(EntityTypeBuilder<EmailDispatchOutbox> builder)
    {
        builder.Property(e => e.Id).HasDefaultValueSql("uuidv7()");
        builder.Property(e => e.PublishEventId).IsRequired();
        builder.Property(e => e.Kind).IsRequired();
        builder.Property(e => e.NotificationIntentId).IsRequired();
        builder.Property(e => e.RecipientAddressSource).IsRequired();
        builder.Property(e => e.SourceType).HasMaxLength(100).IsRequired();
        builder.Property(e => e.RecipientEmail).HasMaxLength(320).IsRequired();
        builder.Property(e => e.Subject).HasMaxLength(500).IsRequired();
        builder.Property(e => e.PlainTextBody).HasColumnType("text");
        builder.Property(e => e.HtmlBody).HasColumnType("text");
        builder.Property(e => e.ReplyTo).HasMaxLength(320);
        builder.Property(e => e.Status).IsRequired();
        builder.Property(e => e.MaxAttempts).HasDefaultValue(5);
        builder.Property(e => e.LastFailureCategory).HasMaxLength(100);
        builder.Property(e => e.LastError).HasMaxLength(2000);
        builder.Property(e => e.ProviderMessageId).HasMaxLength(500);
        builder.Property(e => e.CorrelationId).HasMaxLength(200);
        builder.Property(e => e.RabbitMqLastPublishFailureCategory).HasMaxLength(100);

        builder.HasAlternateKey(e => new { e.TenantId, e.Id })
            .HasName("ak_email_dispatch_outbox_tenant_id");
        builder.HasAlternateKey(e => new { e.TenantId, e.Id, e.NotificationIntentId })
            .HasName("ak_email_dispatch_outbox_tenant_id_intent");
        builder.HasAlternateKey(e => new
            {
                e.TenantId,
                e.Id,
                e.NotificationIntentId,
                e.RecipientAddressSource
            })
            .HasName("ak_email_dispatch_outbox_tenant_id_intent_address_source");
        builder.HasAlternateKey(e => new { e.TenantId, e.Id, e.PublishEventId })
            .HasName("ak_email_dispatch_outbox_tenant_id_publish_event");

        builder.HasOne(e => e.Tenant)
            .WithMany()
            .HasForeignKey(e => e.TenantId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(e => e.RegistrationIntent)
            .WithMany()
            .HasForeignKey(e => e.RegistrationIntentId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(e => e.NotificationIntent)
            .WithMany()
            .HasForeignKey(e => new { e.TenantId, e.NotificationIntentId, e.RecipientUserId })
            .HasPrincipalKey(e => new { e.TenantId, e.Id, e.RecipientUserId })
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("fk_email_dispatch_outbox_recipient_matches_intent");

        builder.HasOne(e => e.RecipientTenantUser)
            .WithMany()
            .HasForeignKey(e => new { e.TenantId, e.RecipientUserId })
            .HasPrincipalKey(e => new { e.TenantId, e.UserId })
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(e => e.Event)
            .WithMany()
            .HasForeignKey(e => e.EventId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(e => e.ManagedTenantProvisioningOperation)
            .WithMany()
            .HasForeignKey(e => e.ManagedTenantProvisioningOperationId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(e => new { e.Status, e.NextAttemptAt, e.CreatedAt })
            .HasDatabaseName("ix_email_dispatch_outbox_worker_poll");

        builder.HasIndex(e => new
        {
            e.Status,
            e.NextAttemptAt,
            e.RabbitMqLastPublishAttemptAt,
            e.CreatedAt
        })
            .HasDatabaseName("ix_email_dispatch_outbox_rabbitmq_publish");

        builder.HasIndex(e => new { e.TenantId, e.PublishEventId })
            .HasDatabaseName("ux_email_dispatch_outbox_tenant_publish_event")
            .IsUnique();

        builder.HasIndex(e => new { e.TenantId, e.NotificationIntentId })
            .HasDatabaseName("ux_email_dispatch_outbox_tenant_intent")
            .IsUnique();

        builder.HasIndex(e => new { e.TenantId, e.Status, e.LastFailureAt })
            .HasDatabaseName("ix_email_dispatch_outbox_tenant_status");

        builder.ToTable(table =>
        {
            table.HasCheckConstraint(
                "ck_email_dispatch_outbox_recipient_authority",
                "(recipient_address_source = 1 AND recipient_user_id IS NOT NULL AND managed_tenant_provisioning_operation_id IS NULL AND kind <> 8) " +
                "OR (recipient_address_source = 2 AND recipient_user_id IS NOT NULL AND managed_tenant_provisioning_operation_id IS NOT NULL " +
                "AND kind = 8 AND source_type = 'managed_tenant_provisioning' AND source_id = managed_tenant_provisioning_operation_id)");
            table.HasCheckConstraint(
                "ck_email_dispatch_outbox_processing_fence",
                "(status = 2) = (processing_started_at IS NOT NULL AND processing_lease_token IS NOT NULL)");
            table.HasCheckConstraint(
                "ck_email_dispatch_outbox_unknown_terminal",
                "status <> 7 OR (unknown_at IS NOT NULL AND next_attempt_at IS NULL " +
                "AND processing_started_at IS NULL AND processing_lease_token IS NULL)");
        });
    }
}
