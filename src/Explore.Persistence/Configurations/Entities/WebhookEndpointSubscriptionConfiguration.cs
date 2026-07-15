// ABOUTME: EF Core configuration for owner-scoped endpoint event type subscription rows.
// ABOUTME: Enforces one instance-or-tenant query scope while event type definitions remain global.

using Explore.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Explore.Persistence.Configurations.Entities;

public class WebhookEndpointSubscriptionConfiguration : IEntityTypeConfiguration<WebhookEndpointSubscription>
{
    public void Configure(EntityTypeBuilder<WebhookEndpointSubscription> builder)
    {
        builder.ToTable("webhook_endpoint_subscriptions", table =>
        {
            table.HasCheckConstraint(
                "ck_webhook_endpoint_subscriptions_configuration_scope",
                "(tenant_id IS NOT NULL AND instance_id IS NULL) OR (tenant_id IS NULL AND instance_id IS NOT NULL)");
            table.HasCheckConstraint(
                "ck_webhook_endpoint_subscriptions_configuration_scope_key",
                "configuration_scope_id = COALESCE(tenant_id, instance_id)");
        });

        builder.Property(e => e.Id).HasDefaultValueSql("uuidv7()");
        var configurationScope = builder.Property(e => e.ConfigurationScopeId);
        configurationScope
            .HasComputedColumnSql("COALESCE(tenant_id, instance_id)", stored: true)
            .ValueGeneratedOnAdd()
            .IsRequired()
            .UsePropertyAccessMode(PropertyAccessMode.Property);
        configurationScope.Metadata.SetBeforeSaveBehavior(PropertySaveBehavior.Ignore);
        builder.Property(e => e.IsEnabled).HasDefaultValue(true);

        builder.HasOne(e => e.Tenant)
            .WithMany()
            .HasForeignKey(e => e.TenantId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(e => e.Instance)
            .WithMany()
            .HasForeignKey(e => e.InstanceId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(e => e.Endpoint)
            .WithMany(e => e.Subscriptions)
            .HasForeignKey(e => new { e.ConfigurationScopeId, e.EndpointId })
            .HasPrincipalKey(e => new { e.ConfigurationScopeId, e.Id })
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(e => e.EventType)
            .WithMany()
            .HasForeignKey(e => e.EventTypeId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(e => new { e.TenantId, e.EndpointId, e.EventTypeId })
            .HasDatabaseName("ux_webhook_endpoint_subscriptions_endpoint_event_type")
            .IsUnique()
            .HasFilter("tenant_id IS NOT NULL");

        builder.HasIndex(e => new { e.InstanceId, e.EndpointId, e.EventTypeId })
            .HasDatabaseName("ux_webhook_endpoint_subscriptions_instance_endpoint_event_type")
            .IsUnique()
            .HasFilter("instance_id IS NOT NULL");

        builder.HasIndex(e => new { e.TenantId, e.EventTypeId, e.IsEnabled })
            .HasDatabaseName("ix_webhook_endpoint_subscriptions_tenant_event_type");

        builder.HasIndex(e => new { e.InstanceId, e.EventTypeId, e.IsEnabled })
            .HasDatabaseName("ix_webhook_endpoint_subscriptions_instance_event_type")
            .HasFilter("instance_id IS NOT NULL");
    }
}
