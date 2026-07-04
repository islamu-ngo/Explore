// ABOUTME: EF configuration for EventRegistrationIntent - parent registration row carrying scope, selected day, policy snapshot.
// ABOUTME: Indexes optimize per-event-per-user lookups; concurrency stamp guards parent-row stale writes.

using Explore.Domain;
using Explore.Persistence.ValueGenerators;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Explore.Persistence.Configurations.Entities;

public class EventRegistrationIntentConfiguration : IEntityTypeConfiguration<EventRegistrationIntent>
{
    public void Configure(EntityTypeBuilder<EventRegistrationIntent> builder)
    {
        builder.Property(e => e.Id).HasValueGenerator<GuidVersion7ValueGenerator>();
        builder.HasAlternateKey(e => new { e.TenantId, e.Id });
        builder.HasAlternateKey(e => new { e.TenantId, e.EventId, e.Id });

        builder.HasOne(e => e.Event)
            .WithMany()
            .HasForeignKey(e => new { e.TenantId, e.EventId })
            .HasPrincipalKey(e => new { e.TenantId, e.Id })
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(e => e.User)
            .WithMany()
            .HasForeignKey(e => e.UserId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(e => e.RegistrationScope)
            .WithMany()
            .HasForeignKey(e => e.RegistrationScopeId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(e => e.SelectedEventDay)
            .WithMany()
            .HasForeignKey(e => new { e.TenantId, e.EventId, e.SelectedEventDayId })
            .HasPrincipalKey(e => new { e.TenantId, e.EventId, e.Id })
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(e => e.RegistrationPolicySnapshot)
            .WithMany()
            .HasForeignKey(e => e.RegistrationPolicySnapshotId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(e => e.ApprovalStatus)
            .WithMany()
            .HasForeignKey(e => e.ApprovalStatusId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(e => e.Tenant)
            .WithMany()
            .HasForeignKey(e => e.TenantId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(e => new { e.TenantId, e.EventId, e.UserId, e.RegistrationScopeId })
            .HasDatabaseName("ix_event_registration_intents_tenant_event_user_scope");

        builder.HasIndex(e => new { e.TenantId, e.EventId, e.SelectedEventDayId })
            .HasDatabaseName("ix_event_registration_intents_tenant_event_day");

        builder.HasIndex(e => new { e.TenantId, e.UserId })
            .HasDatabaseName("ix_event_registration_intents_tenant_user");

        builder.Property(e => e.ConcurrencyStamp).IsConcurrencyToken();

        // Partial unique indexes: one active intent per user per scope per event (or event+day).
        // Excludes soft-deleted rows so re-registration after cancellation is allowed.

        // Event-scope: one intent per user per event
        builder.HasIndex(
                e => new { e.TenantId, e.EventId, e.UserId },
                "ix_event_registration_intents_unique_event_scope")
            .HasDatabaseName("ix_event_registration_intents_unique_event_scope")
            .IsUnique()
            .HasFilter("registration_scope_id = 1 AND is_deleted = false");

        // Day-scope: one intent per user per event per day
        builder.HasIndex(e => new { e.TenantId, e.EventId, e.UserId, e.SelectedEventDayId })
            .HasDatabaseName("ix_event_registration_intents_unique_day_scope")
            .IsUnique()
            .HasFilter("registration_scope_id = 2 AND is_deleted = false");

        // SessionSelection-scope: one intent per user per event
        builder.HasIndex(
                e => new { e.TenantId, e.EventId, e.UserId },
                "ix_event_registration_intents_unique_session_selection_scope")
            .HasDatabaseName("ix_event_registration_intents_unique_session_selection_scope")
            .IsUnique()
            .HasFilter("registration_scope_id = 3 AND is_deleted = false");
    }
}
