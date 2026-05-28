// ABOUTME: EF configuration for event-to-tag assignments with tenant-scoped relational integrity.
// ABOUTME: Composite FKs prevent assigning an event to a tag owned by another tenant.

using Explore.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Explore.Persistence.Configurations.Entities;

public class EventTagsConfiguration : IEntityTypeConfiguration<EventTags>
{
    public void Configure(EntityTypeBuilder<EventTags> builder)
    {
        builder.HasOne(e => e.Event)
            .WithMany()
            .HasForeignKey(e => new { e.TenantId, e.EventId })
            .HasPrincipalKey(e => new { e.TenantId, e.Id })
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(e => e.Tag)
            .WithMany()
            .HasForeignKey(e => new { e.TenantId, e.TagId })
            .HasPrincipalKey(e => new { e.TenantId, e.Id })
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(e => e.Tenant)
            .WithMany()
            .HasForeignKey(e => e.TenantId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(e => new { e.TenantId, e.EventId, e.TagId })
            .HasDatabaseName("ix_event_tags_tenant_event_tag")
            .IsUnique();
    }
}
