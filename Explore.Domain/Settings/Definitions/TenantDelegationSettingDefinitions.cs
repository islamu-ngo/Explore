// ABOUTME: Setting definitions for tenant delegation lock controls.
// ABOUTME: Controls whether tenant admins can configure their own SMTP, storage, and analytics.

namespace Explore.Domain.Settings.Definitions;

public static class TenantDelegationSettingDefinitions
{
    public static readonly SettingDefinition LockSmtp = new(
        Key: "governance.lock_tenant_smtp",
        ValueType: SettingValueType.Boolean,
        DefaultValue: "true",
        Category: "TenantDelegation",
        Description: "Whether tenant administrators can configure their own SMTP settings",
        MinScope: SettingScope.Instance,
        MaxScope: SettingScope.Instance,
        IsLockable: false);

    public static readonly SettingDefinition LockStorage = new(
        Key: "governance.lock_tenant_storage",
        ValueType: SettingValueType.Boolean,
        DefaultValue: "true",
        Category: "TenantDelegation",
        Description: "Whether tenant administrators can configure their own object storage settings",
        MinScope: SettingScope.Instance,
        MaxScope: SettingScope.Instance,
        IsLockable: false);

    public static readonly SettingDefinition LockAnalytics = new(
        Key: "governance.lock_tenant_analytics",
        ValueType: SettingValueType.Boolean,
        DefaultValue: "true",
        Category: "TenantDelegation",
        Description: "Whether tenant administrators can configure their own analytics settings",
        MinScope: SettingScope.Instance,
        MaxScope: SettingScope.Instance,
        IsLockable: false);

    public static IReadOnlyList<SettingDefinition> All => [LockSmtp, LockStorage, LockAnalytics];
}
