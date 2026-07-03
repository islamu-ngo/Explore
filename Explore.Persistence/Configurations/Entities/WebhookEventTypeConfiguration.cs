// ABOUTME: EF Core configuration for the canonical outgoing webhook event type catalog.
// ABOUTME: Stores versioned jsonb schemas used by Local and Svix provider synchronization.

using Explore.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Explore.Persistence.Configurations.Entities;

public class WebhookEventTypeConfiguration : IEntityTypeConfiguration<WebhookEventType>
{
    public void Configure(EntityTypeBuilder<WebhookEventType> builder)
    {
        builder.ToTable("webhook_event_types");

        builder.Property(e => e.Id).HasDefaultValueSql("uuidv7()");
        builder.Property(e => e.Name).HasMaxLength(200).IsRequired();
        builder.Property(e => e.GroupName).HasMaxLength(100).IsRequired();
        builder.Property(e => e.Description).HasMaxLength(1000).IsRequired();
        builder.Property(e => e.SchemaJson).HasColumnType("jsonb").IsRequired();
        builder.Property(e => e.SchemaVersion).IsRequired();
        builder.Property(e => e.IsPublic).IsRequired();
        builder.Property(e => e.IsEnabled).IsRequired();
        builder.Property(e => e.PayloadRetentionDays).HasDefaultValue(14);

        builder.HasIndex(e => e.Name)
            .HasDatabaseName("ux_webhook_event_types_name")
            .IsUnique();

        builder.HasIndex(e => new { e.GroupName, e.IsEnabled, e.IsPublic })
            .HasDatabaseName("ix_webhook_event_types_group_enabled_public");
    }
}
