// ABOUTME: EF Core configuration for email dispatch idempotency receipts keyed by tenant and publish event id.
// ABOUTME: Used by Basic Dispatch Mode now and future RabbitMQ consumers later for duplicate-safe processing.

using Explore.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Explore.Persistence.Configurations.Entities;

public class EmailDispatchReceiptConfiguration : IEntityTypeConfiguration<EmailDispatchReceipt>
{
    public void Configure(EntityTypeBuilder<EmailDispatchReceipt> builder)
    {
        builder.Property(e => e.Id).HasDefaultValueSql("uuidv7()");
        builder.Property(e => e.PublishEventId).IsRequired();
        builder.Property(e => e.Status).IsRequired();
        builder.Property(e => e.ConsumerId).HasMaxLength(200);
        builder.Property(e => e.FailureCode).HasMaxLength(100);
        builder.Property(e => e.FailureMessage).HasMaxLength(1000);
        builder.Property(e => e.ProviderMessageId).HasMaxLength(500);

        builder.HasOne(e => e.Tenant)
            .WithMany()
            .HasForeignKey(e => e.TenantId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(e => e.EmailDispatchOutbox)
            .WithMany()
            .HasForeignKey(e => e.EmailDispatchOutboxId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(e => new { e.TenantId, e.PublishEventId })
            .HasDatabaseName("ux_email_dispatch_receipts_tenant_publish_event")
            .IsUnique();

        builder.HasIndex(e => new { e.EmailDispatchOutboxId, e.Status })
            .HasDatabaseName("ix_email_dispatch_receipts_outbox_status");
    }
}
