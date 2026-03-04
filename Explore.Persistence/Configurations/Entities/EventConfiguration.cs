using System;
using System.Collections.Generic;
using System.Text;
using Explore.Domain;
using Explore.Domain.Enums;
using Explore.Persistence.Seed;
using Explore.Persistence.ValueGenerators;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Explore.Persistence.Configurations.Entities;

public class EventConfiguration : IEntityTypeConfiguration<Event>
{
    public void Configure(EntityTypeBuilder<Event> builder)
    {
        builder.UseTptMappingStrategy();

        builder.Property(e => e.Id).HasValueGenerator<GuidVersion7ValueGenerator>();
        builder.Property(e => e.TotalViews).HasDefaultValue(0);
        builder.Property(e => e.IsUserReported).HasDefaultValue(false);

        builder.Property(e => e.Title).HasMaxLength(200).IsRequired();
        builder.Property(e => e.Subtitle).HasMaxLength(200);
        builder.Property(e => e.Description).HasMaxLength(5000);
        builder.Property(e => e.Slug).HasMaxLength(200);
        builder.Property(e => e.CurrencyCode).HasMaxLength(3);
        builder.Property(e => e.EventUrl).HasMaxLength(2048);
        builder.Property(e => e.ExternalRegistrationUrl).HasMaxLength(2048);
        builder.Property(e => e.Timezone).HasMaxLength(100);
        builder.Property(e => e.Price).HasPrecision(19, 4);

        builder.HasOne(e => e.EventType)
            .WithMany()
            .HasForeignKey(e => e.EventTypeId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(e => e.AudienceGender)
            .WithMany()
            .HasForeignKey(e => e.AudienceGenderId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(e => e.AudienceAge)
            .WithMany()
            .HasForeignKey(e => e.AudienceAgeId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(e => e.Actor)
            .WithMany()
            .HasForeignKey(e => e.ActorId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(e => e.FeaturedImage)
            .WithMany()
            .HasForeignKey(e => e.FeaturedImageId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(e => e.Madhab)
            .WithMany()
            .HasForeignKey(e => e.MadhabId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(e => e.Tenant)
            .WithMany()
            .HasForeignKey(e => e.TenantId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(e => e.VisibilityType)
            .WithMany()
            .HasForeignKey(e => e.VisibilityTypeId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(e => e.EventStatus)
            .WithMany()
            .HasForeignKey(e => e.EventStatusId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(e => e.EventFormat)
            .WithMany()
            .HasForeignKey(e => e.EventFormatId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(e => e.AtprotoRecord)
            .WithMany()
            .HasForeignKey(e => e.AtprotoRecordId)
            .OnDelete(DeleteBehavior.SetNull);

        // Configure aspect navigation properties (shared PK pattern - no FK needed)
        builder.HasOne(e => e.IslamicAspect)
            .WithOne(a => a.Event)
            .HasForeignKey<EventIslamicAspect>(a => a.Id)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(e => e.TechAspect)
            .WithOne(a => a.Event)
            .HasForeignKey<EventTechAspect>(a => a.Id)
            .OnDelete(DeleteBehavior.Cascade);

        // MetadataJson uses PostgreSQL jsonb for efficient JSON querying
        builder.Property(e => e.MetadataJson)
            .HasColumnType("jsonb");

        // ===== Performance Indexes =====

        // Primary listing query: active published events per tenant
        builder.HasIndex(e => new { e.TenantId, e.IsDeleted, e.EventStatusId })
            .HasDatabaseName("ix_events_tenant_active_status");

        // Organization event listing: events by org, sorted by date
        builder.HasIndex(e => new { e.TenantId, e.ActorId, e.CreatedAt })
            .HasDatabaseName("ix_events_tenant_actor_created")
            .IsDescending(false, false, true);

        // Date range queries: upcoming/past events
        builder.HasIndex(e => new { e.TenantId, e.FirstSessionDate, e.LastSessionDate })
            .HasDatabaseName("ix_events_tenant_daterange");

        // Event type filtering
        builder.HasIndex(e => new { e.TenantId, e.EventTypeId })
            .HasDatabaseName("ix_events_tenant_eventtype");

        // Slug lookup (for URL-friendly event access)
        builder.HasIndex(e => new { e.TenantId, e.Slug })
            .HasDatabaseName("ix_events_tenant_slug");

        builder.ToTable(t => t.HasCheckConstraint(
            "CK_Event_NonNegativePrice",
            "price IS NULL OR price >= 0"));

        // Optimistic concurrency control (database-agnostic)
        builder.Property(e => e.ConcurrencyStamp)
            .IsConcurrencyToken();

        // NOTE: Business entity seed data moved to DatabaseSeeder for conditional (Development-only) seeding.
        // See Explore.Persistence/Seed/DatabaseSeeder.cs and SeedData.cs
    }
}
