// ABOUTME: EF Core configuration for session templates owned by event templates.
// ABOUTME: Enforces stable session template keys, version uniqueness, and cascade delete from parent event template.

using Explore.Domain;
using Explore.Persistence.ValueGenerators;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Explore.Persistence.Configurations.Entities;

public class EventSessionTemplateConfiguration : IEntityTypeConfiguration<EventSessionTemplate>
{
    public void Configure(EntityTypeBuilder<EventSessionTemplate> builder)
    {
        builder.ToTable("event_session_templates");

        builder.Property(e => e.Id)
            .HasValueGenerator<GuidVersion7ValueGenerator>();

        builder.Property(e => e.SessionTemplateKey)
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(e => e.DisplayName)
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(e => e.Description)
            .HasMaxLength(500);

        builder.HasOne(e => e.EventTemplate)
            .WithMany()
            .HasForeignKey(e => e.EventTemplateId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(e => e.Tenant)
            .WithMany()
            .HasForeignKey(e => e.TenantId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasMany(e => e.Definitions)
            .WithOne(d => d.EventSessionTemplate)
            .HasForeignKey(d => d.EventSessionTemplateId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(e => new { e.EventTemplateId, e.SessionTemplateKey, e.Version })
            .HasDatabaseName("ix_est_template_key_version")
            .IsUnique();

        builder.HasIndex(e => new { e.TenantId, e.IsPublished, e.IsActive })
            .HasDatabaseName("ix_est_tenant_published_active");
    }
}
