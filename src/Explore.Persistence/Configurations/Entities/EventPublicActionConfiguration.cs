// ABOUTME: Maps tenant-owned public event actions, validated destinations, and lookup relationships.
// ABOUTME: Enforces one active primary action per event while retaining soft-deleted history.

using Explore.Domain;
using Explore.Persistence.ValueGenerators;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Explore.Persistence.Configurations.Entities;

public sealed class EventPublicActionConfiguration : IEntityTypeConfiguration<EventPublicAction>
{
    public void Configure(EntityTypeBuilder<EventPublicAction> builder)
    {
        builder.ToTable("event_public_actions");
        builder.Property(row => row.Id).HasValueGenerator<GuidVersion7ValueGenerator>();
        builder.HasAlternateKey(row => new { row.TenantId, row.Id });
        builder.Property(row => row.Url).IsRequired().HasMaxLength(2048);
        builder.Property(row => row.DestinationDomain).IsRequired().HasMaxLength(253);
        builder.Property(row => row.Label).HasMaxLength(200);
        builder.Property(row => row.ConcurrencyStamp).IsConcurrencyToken();
        builder.Property(row => row.IsDeleted).HasDefaultValue(false);

        builder.HasOne(row => row.Tenant)
            .WithMany()
            .HasForeignKey(row => row.TenantId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(row => row.Event)
            .WithMany(@event => @event.PublicActions)
            .HasForeignKey(row => new { row.TenantId, row.EventId })
            .HasPrincipalKey(@event => new { @event.TenantId, @event.Id })
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(row => row.EventPublicActionKind)
            .WithMany()
            .HasForeignKey(row => row.EventPublicActionKindId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(row => row.HealthState)
            .WithMany()
            .HasForeignKey(row => row.HealthStateId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(row => new { row.TenantId, row.EventId })
            .IsUnique()
            .HasFilter("is_primary = true AND is_deleted = false")
            .HasDatabaseName("ux_event_public_actions_tenant_event_primary");
    }
}
