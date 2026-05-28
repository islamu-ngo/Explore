// ABOUTME: EF configuration for EventSession rows, scheduling constraints, and tenant-safe event graph links.
// ABOUTME: Composite FKs bind sessions to same-tenant events, days, locations, and rooms at the database boundary.

using Explore.Domain;
using Explore.Persistence.Schema;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Explore.Persistence.Configurations.Entities;

public class EventSessionConfiguration : IEntityTypeConfiguration<EventSession>
{
    public void Configure(EntityTypeBuilder<EventSession> builder)
    {
        builder.Property(e => e.Id).HasDefaultValueSql("uuidv7()");
        builder.HasAlternateKey(e => new { e.TenantId, e.Id });
        builder.HasAlternateKey(e => new { e.TenantId, e.EventId, e.Id });

        builder.Property(e => e.Price)
            .HasPrecision(19, 4);

        builder.Property(e => e.CurrencyCode)
            .HasMaxLength(3);

        builder.Property(e => e.Title).HasMaxLength(500);
        builder.Property(e => e.Slug).HasMaxLength(200);
        builder.Property(e => e.Description).HasMaxLength(500);

        builder.HasOne(e => e.Event)
            .WithMany(e => e.Sessions)
            .HasForeignKey(e => new { e.TenantId, e.EventId })
            .HasPrincipalKey(e => new { e.TenantId, e.Id })
            .OnDelete(DeleteBehavior.Cascade);

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

        builder.HasOne(e => e.EventSessionKind)
            .WithMany()
            .HasForeignKey(e => e.EventSessionKindId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasOne(e => e.EventDay)
            .WithMany()
            .HasForeignKey(e => new { e.TenantId, e.EventId, e.EventDayId })
            .HasPrincipalKey(e => new { e.TenantId, e.EventId, e.Id })
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(e => e.Tenant)
            .WithMany()
            .HasForeignKey(e => e.TenantId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(e => new { e.TenantId, e.EventId, e.LocalStartDate, e.LocalStartMinuteOfDay })
            .HasDatabaseName("ix_event_sessions_tenant_event_local_start");

        builder.HasIndex(e => new { e.TenantId, e.LocationId, e.RoomId, e.StartTime, e.EndTime })
            .HasDatabaseName("ix_event_sessions_tenant_location_room_time");

        builder.HasIndex(e => new { e.TenantId, e.EventDayId, e.SortOrder })
            .HasDatabaseName("ix_event_sessions_tenant_day_sort");

        builder.HasIndex(e => e.EventSessionKindId)
            .HasDatabaseName("ix_event_sessions_event_session_kind_id");

        builder.HasPostgresExclusionConstraint(
            name: "EX_EventSession_RoomNoOverlap",
            usingMethod: "gist",
            elementsSql: """
                tenant_id WITH =,
                location_id WITH =,
                room_id WITH =,
                tstzrange(start_time, end_time, '[)') WITH &&
                """,
            predicateSql: "is_deleted = false AND room_id IS NOT NULL",
            preflightConflictExistsSql: """
                SELECT 1
                FROM event_sessions a
                JOIN event_sessions b
                  ON a.tenant_id = b.tenant_id
                 AND a.location_id = b.location_id
                 AND a.room_id = b.room_id
                 AND a.id::text < b.id::text
                WHERE a.is_deleted = false
                  AND b.is_deleted = false
                  AND a.room_id IS NOT NULL
                  AND tstzrange(a.start_time, a.end_time, '[)')
                      && tstzrange(b.start_time, b.end_time, '[)')
                """,
            preflightFailureMessage: "event_sessions contains overlapping active room assignments");

        builder.ToTable(t =>
        {
            t.HasCheckConstraint(
                "CK_EventSession_NonNegativePrice",
                "price IS NULL OR price >= 0");
            t.HasCheckConstraint(
                "CK_EventSession_EndAfterStart",
                "end_time > start_time");
            t.HasCheckConstraint(
                "CK_EventSession_LocalStartMinuteRange",
                "local_start_minute_of_day BETWEEN 0 AND 1439");
            t.HasCheckConstraint(
                "CK_EventSession_LocalEndMinuteRange",
                "local_end_minute_of_day BETWEEN 0 AND 1439");
            t.HasCheckConstraint(
                "CK_EventSession_LocalStartMinuteMatchesTime",
                "local_start_minute_of_day = ((EXTRACT(HOUR FROM local_start_time)::int * 60) + EXTRACT(MINUTE FROM local_start_time)::int)");
            t.HasCheckConstraint(
                "CK_EventSession_LocalEndMinuteMatchesTime",
                "local_end_minute_of_day = ((EXTRACT(HOUR FROM local_end_time)::int * 60) + EXTRACT(MINUTE FROM local_end_time)::int)");
            t.HasCheckConstraint(
                "CK_EventSession_RoomRequiresLocation",
                "room_id IS NULL OR location_id IS NOT NULL");
        });

        // Optimistic concurrency control (database-agnostic)
        builder.Property(e => e.ConcurrencyStamp)
            .IsConcurrencyToken();
    }
}
