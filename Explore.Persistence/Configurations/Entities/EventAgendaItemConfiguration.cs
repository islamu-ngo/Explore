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
            .HasForeignKey(e => e.EventId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(e => e.EventDay)
            .WithMany()
            .HasForeignKey(e => e.EventDayId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasOne(e => e.Location)
            .WithMany()
            .HasForeignKey(e => e.LocationId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasOne(e => e.Room)
            .WithMany()
            .HasForeignKey(e => e.RoomId)
            .OnDelete(DeleteBehavior.SetNull);

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

        builder.ToTable(t => t.HasCheckConstraint(
            "CK_EventAgendaItem_EndAfterStart",
            "end_time > start_time"));

        builder.Property(e => e.ConcurrencyStamp).IsConcurrencyToken();
    }
}
