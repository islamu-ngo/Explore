// ABOUTME: EF mappings for tenant-scoped immutable ticket catalogs, ticket types, entitlements, and capacity pools.
// ABOUTME: Uses composite tenant/event keys, restrictive history relationships, and integer/minor-unit columns.

using Explore.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Explore.Persistence.Configurations.Entities;

public sealed class EventTicketCatalogVersionConfiguration : IEntityTypeConfiguration<EventTicketCatalogVersion>
{
    public void Configure(EntityTypeBuilder<EventTicketCatalogVersion> builder)
    {
        builder.ToTable("event_ticket_catalog_versions");
        builder.Property(catalog => catalog.Id).ValueGeneratedNever();
        builder.Property(catalog => catalog.CurrencyCode).IsRequired().HasMaxLength(3);
        builder.Property(catalog => catalog.CreatedAt).IsRequired();
        builder.Property(catalog => catalog.IsDeleted).HasDefaultValue(false);
        builder.Property(catalog => catalog.ConcurrencyStamp).IsConcurrencyToken();
        builder.HasAlternateKey(catalog => new { catalog.TenantId, catalog.Id });

        builder.HasOne<Tenant>().WithMany().HasForeignKey(catalog => catalog.TenantId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<Event>().WithMany(@event => @event.TicketCatalogVersions)
            .HasForeignKey(catalog => new { catalog.TenantId, catalog.EventId })
            .HasPrincipalKey(@event => new { @event.TenantId, @event.Id }).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<TicketCatalogStatus>().WithMany().HasForeignKey(catalog => catalog.TicketCatalogStatusId).OnDelete(DeleteBehavior.Restrict);
        builder.HasMany(catalog => catalog.TicketTypes).WithOne()
            .HasForeignKey(ticketType => new { ticketType.TenantId, ticketType.CatalogId })
            .HasPrincipalKey(catalog => new { catalog.TenantId, catalog.Id }).OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(catalog => new { catalog.TenantId, catalog.EventId, catalog.VersionNumber }).IsUnique().HasFilter("is_deleted = false");
        builder.HasIndex(catalog => new { catalog.TenantId, catalog.EventId }).IsUnique().HasFilter("ticket_catalog_status_id = 2 AND is_deleted = false");
    }
}

public sealed class EventTicketTypeConfiguration : IEntityTypeConfiguration<EventTicketType>
{
    public void Configure(EntityTypeBuilder<EventTicketType> builder)
    {
        builder.ToTable("event_ticket_types");
        builder.Property(ticketType => ticketType.Id).ValueGeneratedNever();
        builder.Property(ticketType => ticketType.Name).IsRequired().HasMaxLength(200);
        builder.Property(ticketType => ticketType.CurrencyCode).IsRequired().HasMaxLength(3);
        builder.Property(ticketType => ticketType.FixedPriceMinor).HasColumnType("bigint");
        builder.Property(ticketType => ticketType.MinimumPriceMinor).HasColumnType("bigint");
        builder.Property(ticketType => ticketType.SuggestedPriceMinor).HasColumnType("bigint");
        builder.Property(ticketType => ticketType.CreatedAt).IsRequired();
        builder.Property(ticketType => ticketType.IsDeleted).HasDefaultValue(false);
        builder.Property(ticketType => ticketType.ConcurrencyStamp).IsConcurrencyToken();
        builder.HasAlternateKey(ticketType => new { ticketType.TenantId, ticketType.Id });

        builder.HasOne<EventTicketCatalogVersion>().WithMany(catalog => catalog.TicketTypes)
            .HasForeignKey(ticketType => new { ticketType.TenantId, ticketType.CatalogId })
            .HasPrincipalKey(catalog => new { catalog.TenantId, catalog.Id }).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<EventCapacityPool>().WithMany()
            .HasForeignKey(ticketType => new { ticketType.TenantId, ticketType.CapacityPoolId })
            .HasPrincipalKey(pool => new { pool.TenantId, pool.Id }).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<TicketPricingMode>().WithMany().HasForeignKey(ticketType => ticketType.TicketPricingModeId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<ParticipantDataCollectionMode>().WithMany().HasForeignKey(ticketType => ticketType.ParticipantDataCollectionModeId).OnDelete(DeleteBehavior.Restrict);
        builder.HasMany(ticketType => ticketType.Entitlements).WithOne()
            .HasForeignKey(entitlement => new { entitlement.TenantId, entitlement.TicketTypeId })
            .HasPrincipalKey(ticketType => new { ticketType.TenantId, ticketType.Id }).OnDelete(DeleteBehavior.Restrict);
    }
}

public sealed class TicketTypeEntitlementConfiguration : IEntityTypeConfiguration<TicketTypeEntitlement>
{
    public void Configure(EntityTypeBuilder<TicketTypeEntitlement> builder)
    {
        builder.ToTable("ticket_type_entitlements");
        builder.Property(entitlement => entitlement.Id).ValueGeneratedNever();
        builder.HasOne<EventTicketType>().WithMany(ticketType => ticketType.Entitlements)
            .HasForeignKey(entitlement => new { entitlement.TenantId, entitlement.TicketTypeId })
            .HasPrincipalKey(ticketType => new { ticketType.TenantId, ticketType.Id }).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<Event>().WithMany().HasForeignKey(entitlement => new { entitlement.TenantId, entitlement.TargetEventId })
            .HasPrincipalKey(@event => new { @event.TenantId, @event.Id }).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<EventDay>().WithMany().HasForeignKey(entitlement => new { entitlement.TenantId, entitlement.TargetEventId, entitlement.EventDayId })
            .HasPrincipalKey(day => new { day.TenantId, day.EventId, day.Id }).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<EventSession>().WithMany().HasForeignKey(entitlement => new { entitlement.TenantId, entitlement.TargetEventId, entitlement.EventSessionId })
            .HasPrincipalKey(session => new { session.TenantId, session.EventId, session.Id }).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<EntitlementScopeType>().WithMany().HasForeignKey(entitlement => entitlement.EntitlementScopeTypeId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<EntitlementSelectionRule>().WithMany().HasForeignKey(entitlement => entitlement.EntitlementSelectionRuleId).OnDelete(DeleteBehavior.Restrict);
    }
}

public sealed class EventCapacityPoolConfiguration : IEntityTypeConfiguration<EventCapacityPool>
{
    public void Configure(EntityTypeBuilder<EventCapacityPool> builder)
    {
        builder.ToTable("event_capacity_pools");
        builder.Property(pool => pool.Id).ValueGeneratedNever();
        builder.Property(pool => pool.Name).IsRequired().HasMaxLength(200);
        builder.Property(pool => pool.CreatedAt).IsRequired();
        builder.Property(pool => pool.IsDeleted).HasDefaultValue(false);
        builder.Property(pool => pool.ConcurrencyStamp).IsConcurrencyToken();
        builder.HasAlternateKey(pool => new { pool.TenantId, pool.Id });
        builder.HasOne<Tenant>().WithMany().HasForeignKey(pool => pool.TenantId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<Event>().WithMany(@event => @event.CapacityPools).HasForeignKey(pool => new { pool.TenantId, pool.EventId })
            .HasPrincipalKey(@event => new { @event.TenantId, @event.Id }).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<CapacityOversellPolicy>().WithMany().HasForeignKey(pool => pool.CapacityOversellPolicyId).OnDelete(DeleteBehavior.Restrict);
        builder.HasIndex(pool => new { pool.TenantId, pool.EventId, pool.Name }).IsUnique().HasFilter("is_deleted = false");
    }
}
