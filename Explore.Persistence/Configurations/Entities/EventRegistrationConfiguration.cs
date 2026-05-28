// ABOUTME: EF configuration for concrete session registration rows under an EventRegistrationIntent.
// ABOUTME: Composite tenant/event FKs keep parent intent, event, and selected session relationally consistent.

using Explore.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Explore.Persistence.Configurations.Entities;

public class EventRegistrationConfiguration : IEntityTypeConfiguration<EventRegistration>
{
    public void Configure(EntityTypeBuilder<EventRegistration> builder)
    {
        builder.Property(e => e.Id).HasDefaultValueSql("uuidv7()");

        builder.HasOne(e => e.Event)
            .WithMany()
            .HasForeignKey(e => new { e.TenantId, e.EventId })
            .HasPrincipalKey(e => new { e.TenantId, e.Id })
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(e => e.User)
            .WithMany()
            .HasForeignKey(e => e.UserId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(e => e.EventSession)
            .WithMany()
            .HasForeignKey(e => new { e.TenantId, e.EventId, e.EventSessionId })
            .HasPrincipalKey(e => new { e.TenantId, e.EventId, e.Id })
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(e => e.EventRegistrationIntent)
            .WithMany()
            .HasForeignKey(e => new { e.TenantId, e.EventId, e.EventRegistrationIntentId })
            .HasPrincipalKey(e => new { e.TenantId, e.EventId, e.Id })
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(e => e.ApprovalStatus)
            .WithMany()
            .HasForeignKey(e => e.ApprovalStatusId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(e => e.Tenant)
            .WithMany()
            .HasForeignKey(e => e.TenantId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(e => e.AtprotoRecord)
            .WithMany()
            .HasForeignKey(e => e.AtprotoRecordId)
            .OnDelete(DeleteBehavior.SetNull);

        // ===== Performance Indexes =====

        // Unique constraint: one access row per user per event session (child-level invariant).
        builder.HasIndex(e => new { e.TenantId, e.EventId, e.EventSessionId, e.UserId })
            .IsUnique()
            .HasFilter("is_deleted = false")
            .HasDatabaseName("ix_eventregistrations_session_user");

        // Registrations by user (my registrations)
        builder.HasIndex(e => e.UserId)
            .HasDatabaseName("ix_eventregistrations_user");

        // Children by parent intent (to walk a user's registration intent down to concrete access rows).
        builder.HasIndex(e => new { e.TenantId, e.EventId, e.EventRegistrationIntentId })
            .HasDatabaseName("ix_eventregistrations_intent");
    }
}
