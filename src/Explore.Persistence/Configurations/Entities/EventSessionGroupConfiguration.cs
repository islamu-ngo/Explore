// ABOUTME: EF configuration for EventSessionGroup tracks/devrooms/program sections.
// ABOUTME: Enforces tenant/event scoped uniqueness and ordering for conference-style program grouping.

using Explore.Domain;
using Explore.Persistence.ValueGenerators;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Explore.Persistence.Configurations.Entities;

public class EventSessionGroupConfiguration : IEntityTypeConfiguration<EventSessionGroup>
{
    public void Configure(EntityTypeBuilder<EventSessionGroup> builder)
    {
        builder.Property(e => e.Id).HasValueGenerator<GuidVersion7ValueGenerator>();
        builder.HasAlternateKey(e => new { e.TenantId, e.Id });
        builder.HasAlternateKey(e => new { e.TenantId, e.EventId, e.Id });

        builder.Property(e => e.Name).HasMaxLength(200).IsRequired();
        builder.Property(e => e.Slug).HasMaxLength(200);
        builder.Property(e => e.Description).HasMaxLength(2000);
        builder.Property(e => e.Color).HasMaxLength(32);

        builder.HasOne(e => e.Event)
            .WithMany(e => e.SessionGroups)
            .HasForeignKey(e => new { e.TenantId, e.EventId })
            .HasPrincipalKey(e => new { e.TenantId, e.Id })
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(e => e.EventLocation)
            .WithMany()
            .HasForeignKey(e => new { e.TenantId, e.EventId, e.EventLocationId })
            .HasPrincipalKey(e => new { e.TenantId, e.EventId, e.Id })
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(e => e.Location)
            .WithMany()
            .HasForeignKey(e => new { e.TenantId, e.LocationId })
            .HasPrincipalKey(e => new { e.TenantId, e.Id })
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(e => e.Room)
            .WithMany()
            .HasForeignKey(e => new { e.TenantId, e.LocationId, e.RoomId })
            .HasPrincipalKey(e => new { e.TenantId, e.LocationId, e.Id })
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(e => e.Tenant)
            .WithMany()
            .HasForeignKey(e => e.TenantId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(e => new { e.TenantId, e.EventId, e.Slug })
            .HasDatabaseName("ix_event_session_groups_tenant_event_slug")
            .IsUnique()
            .HasFilter("is_deleted = false AND slug IS NOT NULL");

        builder.HasIndex(e => new { e.TenantId, e.EventId, e.SortOrder })
            .HasDatabaseName("ix_event_session_groups_tenant_event_sort");

        builder.HasIndex(e => new { e.TenantId, e.EventId, e.EventLocationId, e.LocationId })
            .HasDatabaseName("ix_event_session_groups_elp_consistency");

        builder.HasAnnotation(
            "EventLocationPrivacy:ConsistencyTrigger",
            "event_session_groups:tenant_id,event_id,event_location_id,location_id,room_id");

        builder.ToTable(t =>
        {
            t.HasCheckConstraint(
                "CK_EventSessionGroup_RoomRequiresLocation",
                "room_id IS NULL OR location_id IS NOT NULL");
            // ELP-230C contraction: a physical venue reference is only legal when it is mediated by an
            // event-scoped EventLocation. This closes the legacy write path that could attach a raw
            // Location without a per-event disclosure policy.
            t.HasCheckConstraint(
                "CK_EventSessionGroup_PhysicalLocationRequiresEventLocation",
                "location_id IS NULL OR event_location_id IS NOT NULL");
        });

        builder.Property(e => e.ConcurrencyStamp).IsConcurrencyToken();
    }
}
