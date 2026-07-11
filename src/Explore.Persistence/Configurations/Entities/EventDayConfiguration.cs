// ABOUTME: EF configuration for EventDay - first-class event-local day aggregate with authored labels and publishing state.
// ABOUTME: Enforces (EventId, LocalDate) uniqueness and cascades on event deletion so orphaned day rows cannot exist.

using Explore.Domain;
using Explore.Persistence.ValueGenerators;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Explore.Persistence.Configurations.Entities;

public class EventDayConfiguration : IEntityTypeConfiguration<EventDay>
{
    public void Configure(EntityTypeBuilder<EventDay> builder)
    {
        builder.Property(e => e.Id).HasValueGenerator<GuidVersion7ValueGenerator>();
        builder.HasAlternateKey(e => new { e.TenantId, e.Id });
        builder.HasAlternateKey(e => new { e.TenantId, e.EventId, e.Id });

        builder.Property(e => e.Label).HasMaxLength(200);
        builder.Property(e => e.Description).HasMaxLength(5000);
        builder.Property(e => e.BannerText).HasMaxLength(500);

        builder.HasOne(e => e.Event)
            .WithMany(e => e.Days)
            .HasForeignKey(e => new { e.TenantId, e.EventId })
            .HasPrincipalKey(e => new { e.TenantId, e.Id })
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(e => e.BannerImage)
            .WithMany()
            .HasForeignKey(e => e.BannerImageId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasOne(e => e.Tenant)
            .WithMany()
            .HasForeignKey(e => e.TenantId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(e => new { e.TenantId, e.EventId, e.LocalDate })
            .HasDatabaseName("ix_event_days_tenant_event_local_date")
            .IsUnique();

        builder.HasIndex(e => new { e.TenantId, e.EventId, e.SortOrder })
            .HasDatabaseName("ix_event_days_tenant_event_sort");

        builder.HasIndex(e => new { e.TenantId, e.EventId, e.IsPublished })
            .HasDatabaseName("ix_event_days_tenant_event_published");

        builder.Property(e => e.ConcurrencyStamp).IsConcurrencyToken();
    }
}
