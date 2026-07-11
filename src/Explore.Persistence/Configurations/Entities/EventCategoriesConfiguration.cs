// ABOUTME: EF configuration for event-to-category assignments with tenant-scoped relational integrity.
// ABOUTME: Composite FKs prevent assigning an event to a category owned by another tenant.

using Explore.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Explore.Persistence.Configurations.Entities;

public class EventCategoriesConfiguration : IEntityTypeConfiguration<EventCategories>
{
    public void Configure(EntityTypeBuilder<EventCategories> builder)
    {
        builder.Property(e => e.ConcurrencyStamp).IsConcurrencyToken();

        builder.HasOne(e => e.Event)
            .WithMany()
            .HasForeignKey(e => new { e.TenantId, e.EventId })
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

        builder.HasIndex(e => new { e.TenantId, e.EventId, e.CategoryId })
            .HasDatabaseName("ix_event_categories_tenant_event_category")
            .IsUnique();
    }
}
