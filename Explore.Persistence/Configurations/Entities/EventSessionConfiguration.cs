using Explore.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Explore.Persistence.Configurations.Entities;

public class EventSessionConfiguration : IEntityTypeConfiguration<EventSession>
{
    public void Configure(EntityTypeBuilder<EventSession> builder)
    {
        builder.Property(e => e.Id).HasDefaultValueSql("uuidv7()");

        builder.Property(e => e.Price)
            .HasPrecision(19, 4);

        builder.Property(e => e.CurrencyCode)
            .HasMaxLength(3);

        builder.Property(e => e.Title).HasMaxLength(500);
        builder.Property(e => e.Slug).HasMaxLength(200);
        builder.Property(e => e.Description).HasMaxLength(500);

        builder.HasOne(e => e.Event)
            .WithMany(e => e.Sessions)
            .HasForeignKey(e => e.EventId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(e => e.Location)
            .WithMany()
            .HasForeignKey(e => e.LocationId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasOne(e => e.Room)
            .WithMany()
            .HasForeignKey(e => e.RoomId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasOne(e => e.EventDay)
            .WithMany()
            .HasForeignKey(e => e.EventDayId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasOne(e => e.Tenant)
            .WithMany()
            .HasForeignKey(e => e.TenantId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(e => new { e.TenantId, e.EventId, e.LocalStartDate, e.LocalStartMinuteOfDay })
            .HasDatabaseName("ix_event_sessions_tenant_event_local_start");

        builder.HasIndex(e => new { e.TenantId, e.RoomId, e.StartTime, e.EndTime })
            .HasDatabaseName("ix_event_sessions_tenant_room_time");

        builder.HasIndex(e => new { e.TenantId, e.EventDayId, e.SortOrder })
            .HasDatabaseName("ix_event_sessions_tenant_day_sort");

        builder.ToTable(t =>
        {
            t.HasCheckConstraint(
                "CK_EventSession_NonNegativePrice",
                "price IS NULL OR price >= 0");
            t.HasCheckConstraint(
                "CK_EventSession_EndAfterStart",
                "end_time > start_time");
        });

        // Optimistic concurrency control (database-agnostic)
        builder.Property(e => e.ConcurrencyStamp)
            .IsConcurrencyToken();
    }
}
