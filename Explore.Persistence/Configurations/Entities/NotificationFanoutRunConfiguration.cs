// ABOUTME: EF Core configuration for notification fanout idempotency and progress tracking.
// ABOUTME: Enforces one fanout run per tenant/source/event tuple and worker-friendly polling indexes.

using Explore.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Explore.Persistence.Configurations.Entities;

public class NotificationFanoutRunConfiguration : IEntityTypeConfiguration<NotificationFanoutRun>
{
    public void Configure(EntityTypeBuilder<NotificationFanoutRun> builder)
    {
        builder.ToTable("notification_fanout_runs", t =>
        {
            t.HasCheckConstraint("ck_notification_fanout_runs_processed_count_nonnegative", "processed_count >= 0");
            t.HasCheckConstraint("ck_notification_fanout_runs_created_count_nonnegative", "created_notification_count >= 0");
            t.HasCheckConstraint("ck_notification_fanout_runs_status", "status IN ('pending', 'processing', 'completed', 'failed')");
        });

        builder.Property(e => e.Id).HasDefaultValueSql("uuidv7()");
        builder.Property(e => e.FanoutKind).IsRequired().HasMaxLength(100);
        builder.Property(e => e.Status).IsRequired().HasMaxLength(50).HasDefaultValue("pending");
        builder.Property(e => e.LastError).HasMaxLength(2000);
        builder.Property(e => e.CreatedAt).HasDefaultValueSql("NOW()").IsRequired();
        builder.Property(e => e.ConcurrencyStamp).IsConcurrencyToken();

        builder.HasOne(e => e.Tenant)
            .WithMany()
            .HasForeignKey(e => e.TenantId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(e => e.NotificationEntityType)
            .WithMany()
            .HasForeignKey(e => e.NotificationEntityTypeId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(e => e.SourceActor)
            .WithMany()
            .HasForeignKey(e => new { e.TenantId, e.SourceActorId })
            .HasPrincipalKey(e => new { e.TenantId, e.Id })
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(e => new { e.TenantId, e.FanoutKind, e.NotificationEntityTypeId, e.EntityId, e.SourceActorId })
            .IsUnique()
            .HasDatabaseName("ux_notification_fanout_runs_source");

        builder.HasIndex(e => new { e.Status, e.CreatedAt })
            .HasDatabaseName("ix_notification_fanout_runs_worker_poll");
    }
}
