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

        builder.Property(e => e.Name).HasMaxLength(200).IsRequired();
        builder.Property(e => e.Slug).HasMaxLength(200);
        builder.Property(e => e.Description).HasMaxLength(2000);
        builder.Property(e => e.Color).HasMaxLength(32);

        builder.HasOne(e => e.Event)
            .WithMany(e => e.SessionGroups)
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

        builder.Property(e => e.ConcurrencyStamp).IsConcurrencyToken();
    }
}
