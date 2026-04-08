// ABOUTME: EF Core configuration for Notification entity with indexes for efficient querying.
// ABOUTME: Optimized for querying unread notifications per user per tenant with partial index.

using Explore.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Explore.Persistence.Configurations.Entities;

public class NotificationConfiguration : IEntityTypeConfiguration<Notification>
{
    public void Configure(EntityTypeBuilder<Notification> builder)
    {
        builder.Property(e => e.Id).HasDefaultValueSql("uuidv7()");

        builder.Property(e => e.Title).HasMaxLength(500).IsRequired();
        builder.Property(e => e.Body).HasMaxLength(2000);
        builder.Property(e => e.EntityId).HasMaxLength(200);

        builder.Property(e => e.CreatedAt)
            .HasDefaultValueSql("NOW()")
            .IsRequired();

        builder.HasOne(e => e.User)
            .WithMany()
            .HasForeignKey(e => e.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(e => e.Tenant)
            .WithMany()
            .HasForeignKey(e => e.TenantId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(e => e.NotificationType)
            .WithMany()
            .HasForeignKey(e => e.NotificationTypeId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(e => e.NotificationEntityType)
            .WithMany()
            .HasForeignKey(e => e.NotificationEntityTypeId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(e => e.NotificationScope)
            .WithMany()
            .HasForeignKey(e => e.NotificationScopeId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(e => e.SourceActor)
            .WithMany()
            .HasForeignKey(e => e.SourceActorId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasOne(e => e.RecipientContextActor)
            .WithMany()
            .HasForeignKey(e => e.RecipientContextActorId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasOne(e => e.NotificationReason)
            .WithMany()
            .HasForeignKey(e => e.NotificationReasonId)
            .OnDelete(DeleteBehavior.Restrict);

        // Unread notifications per user (most common query)
        builder.HasIndex(e => new { e.TenantId, e.UserId, e.IsRead, e.CreatedAt })
            .HasDatabaseName("ix_notifications_tenant_user_unread")
            .IsDescending(false, false, false, true);

        // Partial index: only unread notifications — fast unread count and listing
        builder.HasIndex(e => new { e.TenantId, e.UserId, e.CreatedAt })
            .HasDatabaseName("ix_notifications_unread_by_user")
            .IsDescending(false, false, true)
            .HasFilter("is_read = false AND is_deleted = false");

        // Notification type queries
        builder.HasIndex(e => new { e.TenantId, e.NotificationTypeId })
            .HasDatabaseName("ix_notifications_tenant_type");

        // Scope-based filtering (e.g., "show only org notifications")
        builder.HasIndex(e => new { e.UserId, e.NotificationScopeId, e.IsRead })
            .HasDatabaseName("ix_notifications_user_scope");

        // Archived notifications for inbox filtering
        builder.HasIndex(e => new { e.UserId, e.IsArchived, e.CreatedAt })
            .HasDatabaseName("ix_notifications_user_archived")
            .IsDescending(false, false, true);
    }
}
