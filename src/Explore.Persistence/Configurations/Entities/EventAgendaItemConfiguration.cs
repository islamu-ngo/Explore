// ABOUTME: EF configuration for EventAgendaItem - event-level timeline band with cached local projection fields.
// ABOUTME: Indexed for (TenantId, EventId, LocalStartDate, LocalStartMinuteOfDay) day/room agenda queries.

using Explore.Domain;
using Explore.Persistence.ValueGenerators;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Explore.Persistence.Configurations.Entities;

public class EventAgendaItemConfiguration : IEntityTypeConfiguration<EventAgendaItem>
{
    public void Configure(EntityTypeBuilder<EventAgendaItem> builder)
    {
        builder.Property(e => e.Id).HasValueGenerator<GuidVersion7ValueGenerator>();

        builder.Property(e => e.Title).HasMaxLength(500).IsRequired();
        builder.Property(e => e.Description).HasMaxLength(2000);

        builder.HasOne(e => e.Event)
            .WithMany(e => e.AgendaItems)
            .HasForeignKey(e => new { e.TenantId, e.EventId })
            .HasPrincipalKey(e => new { e.TenantId, e.Id })
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(e => e.EventLocation)
            .WithMany()
            .HasForeignKey(e => new { e.TenantId, e.EventId, e.EventLocationId })
            .HasPrincipalKey(e => new { e.TenantId, e.EventId, e.Id })
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(e => e.EventDay)
            .WithMany()
            .HasForeignKey(e => new { e.TenantId, e.EventId, e.EventDayId })
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

        builder.HasOne(e => e.Kind)
            .WithMany()
            .HasForeignKey(e => e.KindId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(e => e.Tenant)
            .WithMany()
            .HasForeignKey(e => e.TenantId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(e => new { e.TenantId, e.EventId, e.LocalStartDate, e.LocalStartMinuteOfDay })
            .HasDatabaseName("ix_event_agenda_items_tenant_event_local_start");

        builder.HasIndex(e => new { e.TenantId, e.EventId, e.SortOrder })
            .HasDatabaseName("ix_event_agenda_items_tenant_event_sort");

        builder.HasIndex(e => new { e.TenantId, e.EventId, e.EventLocationId, e.LocationId })
            .HasDatabaseName("ix_event_agenda_items_elp_consistency");

        builder.HasAnnotation(
            "EventLocationPrivacy:ConsistencyTrigger",
            "event_agenda_items:tenant_id,event_id,event_location_id,location_id,room_id");

        builder.ToTable(t =>
        {
            t.HasCheckConstraint(
                "CK_EventAgendaItem_EndAfterStart",
                "end_time > start_time");
            t.HasCheckConstraint(
                "CK_EventAgendaItem_LocalDateRange",
                "local_end_date >= local_start_date");
            t.HasCheckConstraint(
                "CK_EventAgendaItem_LocalStartMinuteRange",
                "local_start_minute_of_day BETWEEN 0 AND 1439");
            t.HasCheckConstraint(
                "CK_EventAgendaItem_LocalEndMinuteRange",
                "local_end_minute_of_day BETWEEN 0 AND 1439");
            t.HasCheckConstraint(
                "CK_EventAgendaItem_LocalStartMinuteMatchesTime",
                "local_start_minute_of_day = ((EXTRACT(HOUR FROM local_start_time)::int * 60) + EXTRACT(MINUTE FROM local_start_time)::int)");
            t.HasCheckConstraint(
                "CK_EventAgendaItem_LocalEndMinuteMatchesTime",
                "local_end_minute_of_day = ((EXTRACT(HOUR FROM local_end_time)::int * 60) + EXTRACT(MINUTE FROM local_end_time)::int)");
            t.HasCheckConstraint(
                "CK_EventAgendaItem_RoomRequiresLocation",
                "room_id IS NULL OR location_id IS NOT NULL");
            // ELP-230C contraction: a physical venue reference is only legal when it is mediated by an
            // event-scoped EventLocation. This closes the legacy write path that could attach a raw
            // Location without a per-event disclosure policy.
            t.HasCheckConstraint(
                "CK_EventAgendaItem_PhysicalLocationRequiresEventLocation",
                "location_id IS NULL OR event_location_id IS NOT NULL");
        });

        builder.Property(e => e.ConcurrencyStamp).IsConcurrencyToken();
    }
}
