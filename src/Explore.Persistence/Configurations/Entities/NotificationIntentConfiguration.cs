// ABOUTME: EF Core mappings for durable notification intent, local delivery, and delegation audit rows.
// ABOUTME: Enforces tenant scoping, safe-payload metadata bounds, and normalized routing foreign keys.

using Explore.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Explore.Persistence.Configurations.Entities;

public sealed class NotificationIntentConfiguration : IEntityTypeConfiguration<NotificationIntent>
{
    public void Configure(EntityTypeBuilder<NotificationIntent> builder)
    {
        builder.ToTable("notification_intents");

        builder.Property(e => e.Id).HasDefaultValueSql("uuidv7()");
        builder.Property(e => e.TemplateKey).IsRequired().HasMaxLength(160);
        builder.Property(e => e.DeduplicationKey).IsRequired().HasMaxLength(300);
        builder.Property(e => e.SafePayloadReference).HasMaxLength(500);
        builder.Property(e => e.SafePayloadHash).HasMaxLength(128);
        builder.Property(e => e.CorrelationId).HasMaxLength(200);

        builder.HasOne(e => e.Tenant)
            .WithMany()
            .HasForeignKey(e => e.TenantId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(e => e.Category)
            .WithMany()
            .HasForeignKey(e => e.CategoryId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(e => e.OwnershipType)
            .WithMany()
            .HasForeignKey(e => e.OwnershipTypeId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(e => e.RecipientKind)
            .WithMany()
            .HasForeignKey(e => e.RecipientKindId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(e => e.Status)
            .WithMany()
            .HasForeignKey(e => e.StatusId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(e => e.User)
            .WithMany()
            .HasForeignKey(e => e.UserId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(e => e.Event)
            .WithMany()
            .HasForeignKey(e => e.EventId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(e => e.Report)
            .WithMany()
            .HasForeignKey(e => e.ReportId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(e => e.ReportDecision)
            .WithMany()
            .HasForeignKey(e => e.ReportDecisionId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(e => new { e.TenantId, e.DeduplicationKey })
            .HasDatabaseName("ux_notification_intents_tenant_deduplication_key")
            .IsUnique()
            .HasFilter("is_deleted = false");

        builder.HasIndex(e => new { e.TenantId, e.StatusId, e.CreatedAt })
            .HasDatabaseName("ix_notification_intents_tenant_status_created");

        builder.HasIndex(e => new { e.TenantId, e.CategoryId, e.CreatedAt })
            .HasDatabaseName("ix_notification_intents_tenant_category_created");

        builder.HasIndex(e => new { e.TenantId, e.OwnershipTypeId, e.CreatedAt })
            .HasDatabaseName("ix_notification_intents_tenant_owner_created");
    }
}

public sealed class NotificationDeliveryConfiguration : IEntityTypeConfiguration<NotificationDelivery>
{
    public void Configure(EntityTypeBuilder<NotificationDelivery> builder)
    {
        builder.ToTable("notification_deliveries");

        builder.Property(e => e.Id).HasDefaultValueSql("uuidv7()");
        builder.Property(e => e.ProviderMessageId).HasMaxLength(500);
        builder.Property(e => e.ProviderStatus).HasMaxLength(100);
        builder.Property(e => e.FailureCategory).HasMaxLength(100);

        builder.HasOne(e => e.Tenant)
            .WithMany()
            .HasForeignKey(e => e.TenantId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(e => e.NotificationIntent)
            .WithMany(e => e.Deliveries)
            .HasForeignKey(e => e.NotificationIntentId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(e => e.EmailDispatchOutbox)
            .WithMany()
            .HasForeignKey(e => e.EmailDispatchOutboxId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(e => e.Status)
            .WithMany()
            .HasForeignKey(e => e.StatusId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(e => new { e.TenantId, e.NotificationIntentId })
            .HasDatabaseName("ix_notification_deliveries_tenant_intent");

        builder.HasIndex(e => new { e.TenantId, e.StatusId, e.CreatedAt })
            .HasDatabaseName("ix_notification_deliveries_tenant_status_created");

        builder.HasIndex(e => new { e.TenantId, e.EmailDispatchOutboxId })
            .HasDatabaseName("ix_notification_deliveries_tenant_email_dispatch_outbox");
    }
}

public sealed class NotificationExternalDelegationConfiguration : IEntityTypeConfiguration<NotificationExternalDelegation>
{
    public void Configure(EntityTypeBuilder<NotificationExternalDelegation> builder)
    {
        builder.ToTable("notification_external_delegations");

        builder.Property(e => e.Id).HasDefaultValueSql("uuidv7()");
        builder.Property(e => e.TemplateKey).IsRequired().HasMaxLength(160);
        builder.Property(e => e.SafePayloadHash).HasMaxLength(128);
        builder.Property(e => e.ExternalProviderId).HasMaxLength(200);
        builder.Property(e => e.ExternalCorrelationId).HasMaxLength(200);
        builder.Property(e => e.ExternalDeliveryStatus).HasMaxLength(100);
        builder.Property(e => e.FailureCategory).HasMaxLength(100);

        builder.HasOne(e => e.Tenant)
            .WithMany()
            .HasForeignKey(e => e.TenantId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(e => e.NotificationIntent)
            .WithMany(e => e.ExternalDelegations)
            .HasForeignKey(e => e.NotificationIntentId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(e => e.ProviderKind)
            .WithMany()
            .HasForeignKey(e => e.ProviderKindId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(e => e.AccountAuthorityKind)
            .WithMany()
            .HasForeignKey(e => e.AccountAuthorityKindId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(e => e.Status)
            .WithMany()
            .HasForeignKey(e => e.StatusId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(e => e.RecipientKind)
            .WithMany()
            .HasForeignKey(e => e.RecipientKindId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(e => e.Report)
            .WithMany()
            .HasForeignKey(e => e.ReportId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(e => e.ReportDecision)
            .WithMany()
            .HasForeignKey(e => e.ReportDecisionId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(e => new { e.TenantId, e.NotificationIntentId })
            .HasDatabaseName("ix_notification_external_delegations_tenant_intent");

        builder.HasIndex(e => new { e.TenantId, e.ProviderKindId, e.StatusId, e.CreatedAt })
            .HasDatabaseName("ix_notification_external_delegations_tenant_provider_status");

        builder.HasIndex(e => new { e.TenantId, e.AccountAuthorityKindId, e.StatusId, e.CreatedAt })
            .HasDatabaseName("ix_notification_external_delegations_tenant_account_authority_status");

        builder.HasIndex(e => new { e.TenantId, e.ExternalCorrelationId })
            .HasDatabaseName("ix_notification_external_delegations_tenant_external_correlation");
    }
}
