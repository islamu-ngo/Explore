// ABOUTME: EF configuration for EventSessionTag junction with tenant-scoped uniqueness on (EventSessionId, TagId).
// ABOUTME: Cascades on session/tag deletion; tenant FK is restrict-only.

using Explore.Domain;
using Explore.Persistence.ValueGenerators;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Explore.Persistence.Configurations.Entities;

public class EventSessionTagConfiguration : IEntityTypeConfiguration<EventSessionTag>
{
    public void Configure(EntityTypeBuilder<EventSessionTag> builder)
    {
        builder.Property(e => e.Id).HasValueGenerator<GuidVersion7ValueGenerator>();

        builder.HasOne(e => e.EventSession)
            .WithMany()
            .HasForeignKey(e => new { e.TenantId, e.EventSessionId })
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

        builder.HasIndex(e => new { e.TenantId, e.EventSessionId, e.TagId })
            .HasDatabaseName("ix_event_session_tags_tenant_session_tag")
            .IsUnique();
    }
}
