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

        builder.HasOne(e => e.Tenant)
            .WithMany()
            .HasForeignKey(e => e.TenantId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(e => e.RegistrationIntent)
            .WithMany()
            .HasForeignKey(e => e.RegistrationIntentId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(e => e.User)
            .WithMany()
            .HasForeignKey(e => e.UserId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(e => e.Event)
            .WithMany()
            .HasForeignKey(e => e.EventId)
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

        builder.HasIndex(e => new { e.TenantId, e.SourceType, e.SourceId, e.Kind })
            .HasDatabaseName("ux_email_dispatch_outbox_tenant_source_kind")
            .IsUnique()
            .HasFilter("is_deleted = false");

        builder.HasIndex(e => new { e.TenantId, e.Status, e.LastFailureAt })
            .HasDatabaseName("ix_email_dispatch_outbox_tenant_status");
    }
}
