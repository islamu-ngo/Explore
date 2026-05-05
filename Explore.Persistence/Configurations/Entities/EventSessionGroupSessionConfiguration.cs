// ABOUTME: EF configuration for EventSessionGroupSession many-to-many join payload.
// ABOUTME: Indexes group membership and primary assignment per event/session for program summaries.

using Explore.Domain;
using Explore.Persistence.ValueGenerators;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Explore.Persistence.Configurations.Entities;

public class EventSessionGroupSessionConfiguration : IEntityTypeConfiguration<EventSessionGroupSession>
{
    public void Configure(EntityTypeBuilder<EventSessionGroupSession> builder)
    {
        builder.Property(e => e.Id).HasValueGenerator<GuidVersion7ValueGenerator>();

        builder.HasOne(e => e.EventSessionGroup)
            .WithMany(e => e.Sessions)
            .HasForeignKey(e => e.EventSessionGroupId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(e => e.EventSession)
            .WithMany(e => e.SessionGroups)
            .HasForeignKey(e => e.EventSessionId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(e => e.Event)
            .WithMany()
            .HasForeignKey(e => e.EventId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(e => e.Tenant)
            .WithMany()
            .HasForeignKey(e => e.TenantId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(e => new { e.TenantId, e.EventId, e.EventSessionGroupId, e.EventSessionId })
            .HasDatabaseName("ix_event_session_group_sessions_tenant_event_group_session")
            .IsUnique()
            .HasFilter("is_deleted = false");

        builder.HasIndex(e => new { e.TenantId, e.EventId, e.EventSessionId, e.IsPrimary })
            .HasDatabaseName("ix_event_session_group_sessions_tenant_event_session_primary")
            .IsUnique()
            .HasFilter("is_primary = true AND is_deleted = false");

        builder.HasIndex(e => new { e.TenantId, e.EventSessionGroupId, e.SortOrder })
            .HasDatabaseName("ix_event_session_group_sessions_tenant_group_sort");

        builder.Property(e => e.IsDeleted).HasDefaultValue(false);
    }
}
