// ABOUTME: EF Core configuration for webhook consumers and their typed ownership references.
// ABOUTME: Enforces one instance, tenant, organization, group, or tenant-user owner with scoped indexes.

using Explore.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Explore.Persistence.Configurations.Entities;

public class WebhookConsumerConfiguration : IEntityTypeConfiguration<WebhookConsumer>
{
    public void Configure(EntityTypeBuilder<WebhookConsumer> builder)
    {
        builder.ToTable("webhook_consumers", table =>
        {
            table.HasCheckConstraint(
                "ck_webhook_consumers_configuration_version",
                "configuration_version > 0");
            table.HasCheckConstraint(
                "ck_webhook_consumers_typed_owner",
                "(consumer_kind_id = 1 AND tenant_id IS NOT NULL AND instance_id IS NULL AND organization_id IS NULL AND group_id IS NULL AND owner_user_id IS NULL) OR " +
                "(consumer_kind_id = 2 AND tenant_id IS NOT NULL AND instance_id IS NULL AND organization_id IS NOT NULL AND group_id IS NULL AND owner_user_id IS NULL) OR " +
                "(consumer_kind_id = 3 AND tenant_id IS NOT NULL AND instance_id IS NULL AND organization_id IS NULL AND group_id IS NOT NULL AND owner_user_id IS NULL) OR " +
                "(consumer_kind_id = 4 AND tenant_id IS NOT NULL AND instance_id IS NULL AND organization_id IS NULL AND group_id IS NULL AND owner_user_id IS NOT NULL) OR " +
                "(consumer_kind_id = 5 AND tenant_id IS NULL AND instance_id IS NOT NULL AND organization_id IS NULL AND group_id IS NULL AND owner_user_id IS NULL)");
            table.HasCheckConstraint(
                "ck_webhook_consumers_configuration_scope",
                "configuration_scope_id = COALESCE(tenant_id, instance_id)");
        });

        builder.Property(e => e.Id).HasDefaultValueSql("uuidv7()");
        builder.Property(e => e.Name).HasMaxLength(200).IsRequired();
        var configurationScope = builder.Property(e => e.ConfigurationScopeId);
        configurationScope
            .HasComputedColumnSql("COALESCE(tenant_id, instance_id)", stored: true)
            .ValueGeneratedOnAdd()
            .IsRequired()
            .UsePropertyAccessMode(PropertyAccessMode.Property);
        configurationScope.Metadata.SetBeforeSaveBehavior(PropertySaveBehavior.Ignore);
        builder.Property(e => e.ConsumerKindId).IsRequired();
        builder.Property(e => e.StatusId).IsRequired();
        builder.Property(e => e.ProviderModeId).IsRequired();
        builder.Property(e => e.ConfigurationVersion).HasDefaultValue(1).IsRequired().IsConcurrencyToken();
        builder.Ignore(e => e.ConsumerKind);
        builder.Ignore(e => e.Ownership);
        builder.Ignore(e => e.OwnerId);
        builder.Ignore(e => e.Status);
        builder.Ignore(e => e.ProviderMode);
        builder.Property(e => e.ExternalProviderAppId).HasMaxLength(500);

        builder.HasOne(e => e.Tenant)
            .WithMany()
            .HasForeignKey(e => e.TenantId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(e => e.Instance)
            .WithMany()
            .HasForeignKey(e => e.InstanceId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Ignore(e => e.Organization);
        builder.HasOne<OrganizationTenant>()
            .WithMany()
            .HasForeignKey(e => new { e.TenantId, e.OrganizationId })
            .HasPrincipalKey(e => new { e.TenantId, e.OrganizationId })
            .OnDelete(DeleteBehavior.Restrict);

        builder.Ignore(e => e.Group);
        builder.HasOne<GroupTenant>()
            .WithMany()
            .HasForeignKey(e => new { e.TenantId, e.GroupId })
            .HasPrincipalKey(e => new { e.TenantId, e.GroupId })
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(e => e.OwnerTenantUser)
            .WithMany()
            .HasForeignKey(e => new { e.TenantId, e.OwnerUserId })
            .HasPrincipalKey(e => new { e.TenantId, e.UserId })
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(e => e.ConsumerKindLookup)
            .WithMany()
            .HasForeignKey(e => e.ConsumerKindId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(e => e.StatusLookup)
            .WithMany()
            .HasForeignKey(e => e.StatusId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(e => e.ProviderModeLookup)
            .WithMany()
            .HasForeignKey(e => e.ProviderModeId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasAlternateKey(e => new { e.ConfigurationScopeId, e.Id })
            .HasName("ak_webhook_consumers_configuration_scope_id");

        builder.HasIndex(e => new { e.InstanceId, e.Name })
            .HasDatabaseName("ux_webhook_consumers_instance_name")
            .IsUnique()
            .HasFilter("consumer_kind_id = 5");

        builder.HasIndex(e => new { e.TenantId, e.Name })
            .HasDatabaseName("ux_webhook_consumers_tenant_name")
            .IsUnique()
            .HasFilter("consumer_kind_id = 1");

        builder.HasIndex(e => new { e.TenantId, e.OrganizationId, e.Name })
            .HasDatabaseName("ux_webhook_consumers_organization_name")
            .IsUnique()
            .HasFilter("consumer_kind_id = 2");

        builder.HasIndex(e => new { e.TenantId, e.GroupId, e.Name })
            .HasDatabaseName("ux_webhook_consumers_group_name")
            .IsUnique()
            .HasFilter("consumer_kind_id = 3");

        builder.HasIndex(e => new { e.TenantId, e.OwnerUserId, e.Name })
            .HasDatabaseName("ux_webhook_consumers_user_name")
            .IsUnique()
            .HasFilter("consumer_kind_id = 4");

        builder.HasIndex(e => new { e.TenantId, e.StatusId, e.ProviderModeId })
            .HasDatabaseName("ix_webhook_consumers_tenant_status_provider");

        builder.HasIndex(e => new { e.InstanceId, e.StatusId, e.ProviderModeId })
            .HasDatabaseName("ix_webhook_consumers_instance_status_provider")
            .HasFilter("instance_id IS NOT NULL");

        builder.HasIndex(e => e.ExternalProviderAppId)
            .HasDatabaseName("ux_webhook_consumers_external_app")
            .IsUnique()
            .HasFilter("external_provider_app_id IS NOT NULL");
    }
}
