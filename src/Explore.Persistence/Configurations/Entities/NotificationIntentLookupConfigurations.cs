// ABOUTME: EF Core mappings for normalized notification ownership and routing lookup tables.
// ABOUTME: Keeps persistent email-responsibility classifiers stable with integer foreign keys.

using Explore.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Explore.Persistence.Configurations.Entities;

public sealed class NotificationCategoryConfiguration : IEntityTypeConfiguration<NotificationCategory>
{
    public void Configure(EntityTypeBuilder<NotificationCategory> builder)
    {
        ConfigureLookup(builder, "notification_categories", "ux_notification_categories_master_code");
    }

    private static void ConfigureLookup<TLookup>(EntityTypeBuilder<TLookup> builder, string tableName, string masterCodeIndexName)
        where TLookup : class
    {
        builder.ToTable(tableName);
        builder.Property<int>("Id").ValueGeneratedNever();
        builder.Property<string>("MasterCode").IsRequired().HasMaxLength(100);
        builder.Property<string>("FullName").IsRequired().HasMaxLength(200);
        builder.Property<string?>("Description").HasMaxLength(500);
        builder.HasIndex("MasterCode").IsUnique().HasDatabaseName(masterCodeIndexName);
    }
}

public sealed class NotificationOwnershipTypeConfiguration : IEntityTypeConfiguration<NotificationOwnershipType>
{
    public void Configure(EntityTypeBuilder<NotificationOwnershipType> builder)
    {
        builder.ToTable("notification_ownership_types");
        builder.Property(e => e.Id).ValueGeneratedNever();
        builder.Property(e => e.MasterCode).IsRequired().HasMaxLength(100);
        builder.Property(e => e.FullName).IsRequired().HasMaxLength(200);
        builder.Property(e => e.Description).HasMaxLength(500);
        builder.HasIndex(e => e.MasterCode).IsUnique().HasDatabaseName("ux_notification_ownership_types_master_code");
    }
}

public sealed class NotificationIntentStatusConfiguration : IEntityTypeConfiguration<NotificationIntentStatus>
{
    public void Configure(EntityTypeBuilder<NotificationIntentStatus> builder)
    {
        builder.ToTable("notification_intent_statuses");
        builder.Property(e => e.Id).ValueGeneratedNever();
        builder.Property(e => e.MasterCode).IsRequired().HasMaxLength(100);
        builder.Property(e => e.FullName).IsRequired().HasMaxLength(200);
        builder.Property(e => e.Description).HasMaxLength(500);
        builder.HasIndex(e => e.MasterCode).IsUnique().HasDatabaseName("ux_notification_intent_statuses_master_code");
    }
}

public sealed class NotificationRecipientKindConfiguration : IEntityTypeConfiguration<NotificationRecipientKind>
{
    public void Configure(EntityTypeBuilder<NotificationRecipientKind> builder)
    {
        builder.ToTable("notification_recipient_kinds");
        builder.Property(e => e.Id).ValueGeneratedNever();
        builder.Property(e => e.MasterCode).IsRequired().HasMaxLength(100);
        builder.Property(e => e.FullName).IsRequired().HasMaxLength(200);
        builder.Property(e => e.Description).HasMaxLength(500);
        builder.HasIndex(e => e.MasterCode).IsUnique().HasDatabaseName("ux_notification_recipient_kinds_master_code");
    }
}

public sealed class NotificationDeliveryStatusConfiguration : IEntityTypeConfiguration<NotificationDeliveryStatus>
{
    public void Configure(EntityTypeBuilder<NotificationDeliveryStatus> builder)
    {
        builder.ToTable("notification_delivery_statuses");
        builder.Property(e => e.Id).ValueGeneratedNever();
        builder.Property(e => e.MasterCode).IsRequired().HasMaxLength(100);
        builder.Property(e => e.FullName).IsRequired().HasMaxLength(200);
        builder.Property(e => e.Description).HasMaxLength(500);
        builder.HasIndex(e => e.MasterCode).IsUnique().HasDatabaseName("ux_notification_delivery_statuses_master_code");
    }
}

public sealed class NotificationDeliveryPolicyConfiguration : IEntityTypeConfiguration<NotificationDeliveryPolicy>
{
    public void Configure(EntityTypeBuilder<NotificationDeliveryPolicy> builder)
    {
        builder.ToTable("notification_delivery_policies");
        builder.Property(e => e.Id).ValueGeneratedNever();
        builder.Property(e => e.MasterCode).IsRequired().HasMaxLength(100);
        builder.Property(e => e.FullName).IsRequired().HasMaxLength(200);
        builder.Property(e => e.Description).HasMaxLength(500);
        builder.HasIndex(e => e.MasterCode).IsUnique().HasDatabaseName("ux_notification_delivery_policies_master_code");
    }
}

public sealed class NotificationExternalDelegationStatusConfiguration : IEntityTypeConfiguration<NotificationExternalDelegationStatus>
{
    public void Configure(EntityTypeBuilder<NotificationExternalDelegationStatus> builder)
    {
        builder.ToTable("notification_external_delegation_statuses");
        builder.Property(e => e.Id).ValueGeneratedNever();
        builder.Property(e => e.MasterCode).IsRequired().HasMaxLength(100);
        builder.Property(e => e.FullName).IsRequired().HasMaxLength(200);
        builder.Property(e => e.Description).HasMaxLength(500);
        builder.HasIndex(e => e.MasterCode).IsUnique().HasDatabaseName("ux_notification_external_delegation_statuses_master_code");
    }
}

public sealed class ExternalWorkflowProviderKindLookupConfiguration : IEntityTypeConfiguration<ExternalWorkflowProviderKindLookup>
{
    public void Configure(EntityTypeBuilder<ExternalWorkflowProviderKindLookup> builder)
    {
        builder.ToTable("external_workflow_provider_kinds");
        builder.Property(e => e.Id).ValueGeneratedNever();
        builder.Property(e => e.MasterCode).IsRequired().HasMaxLength(100);
        builder.Property(e => e.FullName).IsRequired().HasMaxLength(200);
        builder.Property(e => e.Description).HasMaxLength(500);
        builder.HasIndex(e => e.MasterCode).IsUnique().HasDatabaseName("ux_external_workflow_provider_kinds_master_code");
    }
}

public sealed class AccountAuthorityKindLookupConfiguration : IEntityTypeConfiguration<AccountAuthorityKindLookup>
{
    public void Configure(EntityTypeBuilder<AccountAuthorityKindLookup> builder)
    {
        builder.ToTable("account_authority_kinds");
        builder.Property(e => e.Id).ValueGeneratedNever();
        builder.Property(e => e.MasterCode).IsRequired().HasMaxLength(100);
        builder.Property(e => e.FullName).IsRequired().HasMaxLength(200);
        builder.Property(e => e.Description).HasMaxLength(500);
        builder.HasIndex(e => e.MasterCode).IsUnique().HasDatabaseName("ux_account_authority_kinds_master_code");
    }
}
