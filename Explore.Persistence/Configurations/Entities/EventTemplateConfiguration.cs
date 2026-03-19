// ABOUTME: EF Core configuration for tenant-scoped event templates used to instantiate Layer 3 runtime definitions.
// ABOUTME: Enforces stable template keys, version uniqueness, and optional event-type scoping.

using Explore.Domain;
using Explore.Persistence.ValueGenerators;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Explore.Persistence.Configurations.Entities;

public class EventTemplateConfiguration : IEntityTypeConfiguration<EventTemplate>
{
    public void Configure(EntityTypeBuilder<EventTemplate> builder)
    {
        builder.ToTable("event_templates");

        builder.Property(e => e.Id)
            .HasValueGenerator<GuidVersion7ValueGenerator>();

        builder.Property(e => e.TemplateKey)
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(e => e.DisplayName)
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(e => e.Description)
            .HasMaxLength(500);

        builder.HasOne(e => e.Tenant)
            .WithMany()
            .HasForeignKey(e => e.TenantId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(e => e.EventType)
            .WithMany()
            .HasForeignKey(e => e.EventTypeId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasMany(e => e.Definitions)
            .WithOne(d => d.EventTemplate)
            .HasForeignKey(d => d.EventTemplateId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(e => new { e.TenantId, e.TemplateKey, e.Version })
            .HasDatabaseName("ix_event_templates_tenant_key_version")
            .IsUnique();

        builder.HasIndex(e => new { e.TenantId, e.IsPublished, e.IsActive })
            .HasDatabaseName("ix_event_templates_tenant_published_active");
    }
}
