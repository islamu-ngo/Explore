// ABOUTME: EF configuration for EventSessionCategory junction with tenant-scoped uniqueness on (EventSessionId, CategoryId).
// ABOUTME: Cascades on session/category deletion; tenant FK is restrict-only.

using Explore.Domain;
using Explore.Persistence.ValueGenerators;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Explore.Persistence.Configurations.Entities;

public class EventSessionCategoryConfiguration : IEntityTypeConfiguration<EventSessionCategory>
{
    public void Configure(EntityTypeBuilder<EventSessionCategory> builder)
    {
        builder.Property(e => e.Id).HasValueGenerator<GuidVersion7ValueGenerator>();

        builder.HasOne(e => e.EventSession)
            .WithMany()
            .HasForeignKey(e => new { e.TenantId, e.EventSessionId })
            .HasPrincipalKey(e => new { e.TenantId, e.Id })
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(e => e.Category)
            .WithMany()
            .HasForeignKey(e => new { e.TenantId, e.CategoryId })
            .HasPrincipalKey(e => new { e.TenantId, e.Id })
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(e => e.Tenant)
            .WithMany()
            .HasForeignKey(e => e.TenantId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(e => new { e.TenantId, e.EventSessionId, e.CategoryId })
            .HasDatabaseName("ix_event_session_categories_tenant_session_category")
            .IsUnique();
    }
}
