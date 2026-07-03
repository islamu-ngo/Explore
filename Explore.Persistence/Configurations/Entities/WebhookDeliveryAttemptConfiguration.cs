// ABOUTME: EF Core configuration for LocalProvider webhook HTTP delivery attempt ledger rows.
// ABOUTME: Enforces one attempt number per endpoint/message pair and adds worker polling indexes.

using Explore.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Explore.Persistence.Configurations.Entities;

public class WebhookDeliveryAttemptConfiguration : IEntityTypeConfiguration<WebhookDeliveryAttempt>
{
    public void Configure(EntityTypeBuilder<WebhookDeliveryAttempt> builder)
    {
        builder.ToTable("webhook_delivery_attempts");

        builder.Property(e => e.Id).HasDefaultValueSql("uuidv7()");
        builder.Property(e => e.Status).IsRequired();
        builder.Property(e => e.ProcessingLeaseToken);
        builder.Property(e => e.FailureCategory).HasMaxLength(100);
        builder.Property(e => e.ResponseBodyPreview).HasMaxLength(4096);

        builder.HasOne(e => e.Tenant)
            .WithMany()
            .HasForeignKey(e => e.TenantId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(e => e.Message)
            .WithMany()
            .HasForeignKey(e => new { e.TenantId, e.MessageId })
            .HasPrincipalKey(e => new { e.TenantId, e.Id })
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(e => e.Endpoint)
            .WithMany()
            .HasForeignKey(e => new { e.TenantId, e.EndpointId })
            .HasPrincipalKey(e => new { e.TenantId, e.Id })
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(e => new { e.MessageId, e.EndpointId, e.AttemptNumber })
            .HasDatabaseName("ux_webhook_delivery_attempts_message_endpoint_attempt")
            .IsUnique();

        builder.HasIndex(e => new { e.Status, e.ScheduledAt, e.CreatedAt })
            .HasDatabaseName("ix_webhook_delivery_attempts_worker_poll");

        builder.HasIndex(e => new { e.TenantId, e.EndpointId, e.Status, e.ScheduledAt })
            .HasDatabaseName("ix_webhook_delivery_attempts_tenant_endpoint_status");
    }
}
