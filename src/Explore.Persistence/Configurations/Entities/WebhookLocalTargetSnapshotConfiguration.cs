// ABOUTME: EF Core configuration for snapshotted Local webhook targets and mutable delivery claims.
// ABOUTME: Enforces composite tenant ownership, normalized delivery state, unique targets, and fenced concurrency.

using Explore.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Explore.Persistence.Configurations.Entities;

public sealed class WebhookLocalTargetSnapshotConfiguration
    : IEntityTypeConfiguration<WebhookLocalTargetSnapshot>
{
    public void Configure(EntityTypeBuilder<WebhookLocalTargetSnapshot> builder)
    {
        builder.ToTable("webhook_local_target_snapshots", table =>
        {
            table.HasCheckConstraint("ck_webhook_local_targets_endpoint_version", "endpoint_configuration_version > 0");
            table.HasCheckConstraint("ck_webhook_local_targets_credential_version", "credential_version > 0");
            table.HasCheckConstraint("ck_webhook_local_targets_max_attempts", "max_attempts > 0");
            table.HasCheckConstraint("ck_webhook_local_targets_timeout", "timeout_seconds > 0");
            table.HasCheckConstraint("ck_webhook_local_targets_delivery_fence", "delivery_fence >= 0");
            table.HasCheckConstraint("ck_webhook_local_targets_concurrency_version", "concurrency_version > 0");
        });

        builder.Property(target => target.Id).HasDefaultValueSql("uuidv7()");
        builder.Property(target => target.DestinationUrl).HasMaxLength(WebhookLocalTargetSnapshot.MaxDestinationUrlLength).IsRequired();
        builder.Property(target => target.CredentialReference).HasMaxLength(WebhookLocalTargetSnapshot.MaxCredentialReferenceLength).IsRequired();
        builder.Property(target => target.DeliveryStatusId).IsRequired();
        builder.Property(target => target.ProcessingLeaseOwner).HasMaxLength(200);
        builder.Property(target => target.DeliveryFence).IsRequired();
        builder.Property(target => target.ConcurrencyVersion).IsRequired().IsConcurrencyToken();
        builder.Ignore(target => target.DeliveryStatus);

        builder.HasAlternateKey(target => new { target.TenantId, target.Id })
            .HasName("ak_webhook_local_target_snapshots_tenant_id_id");

        builder.HasOne(target => target.Tenant)
            .WithMany()
            .HasForeignKey(target => target.TenantId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(target => target.WebhookMessage)
            .WithMany()
            .HasPrincipalKey(message => new { message.TenantId, message.Id })
            .HasForeignKey(target => new { target.TenantId, target.WebhookMessageId })
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(target => target.DeliveryPlanSnapshot)
            .WithMany()
            .HasPrincipalKey(plan => new { plan.TenantId, plan.Id })
            .HasForeignKey(target => new { target.TenantId, target.DeliveryPlanSnapshotId })
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(target => target.WebhookEndpoint)
            .WithMany()
            .HasPrincipalKey(endpoint => new { endpoint.TenantId, endpoint.Id })
            .HasForeignKey(target => new { target.TenantId, target.WebhookEndpointId })
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(target => target.DeliveryStatusLookup)
            .WithMany()
            .HasForeignKey(target => target.DeliveryStatusId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(target => new { target.TenantId, target.DeliveryPlanSnapshotId, target.WebhookEndpointId })
            .HasDatabaseName("ux_webhook_local_targets_tenant_plan_endpoint")
            .IsUnique();
        builder.HasIndex(target => new { target.TenantId, target.DeliveryStatusId, target.NextActionAtUtc, target.ProcessingLeaseExpiresAtUtc })
            .HasDatabaseName("ix_webhook_local_targets_tenant_claim_due");
        builder.HasIndex(target => new { target.TenantId, target.WebhookMessageId })
            .HasDatabaseName("ix_webhook_local_targets_tenant_message");
    }
}
