// ABOUTME: Setting definitions for tenant delegation lock controls.
// ABOUTME: Controls whether tenant admins can override platform-governed SMTP, storage, analytics, AI, and MCP settings.

namespace Explore.Domain.Settings.Definitions;

using Explore.Domain.Constants;

public static class TenantDelegationSettingDefinitions
{
    public static readonly SettingDefinition LockSmtp = new(
        Key: GovernanceSettingKeys.TenantDelegation.LockSmtp,
        ValueType: SettingValueType.Boolean,
        DefaultValue: "true",
        Category: "TenantDelegation",
        Description: "Whether tenant administrators can configure their own SMTP settings",
        MinScope: SettingScope.Instance,
        MaxScope: SettingScope.Instance,
        IsLockable: false);

    public static readonly SettingDefinition LockStorage = new(
        Key: GovernanceSettingKeys.TenantDelegation.LockStorage,
        ValueType: SettingValueType.Boolean,
        DefaultValue: "true",
        Category: "TenantDelegation",
        Description: "Whether tenant administrators can configure their own object storage settings",
        MinScope: SettingScope.Instance,
        MaxScope: SettingScope.Instance,
        IsLockable: false);

    public static readonly SettingDefinition LockAnalytics = new(
        Key: GovernanceSettingKeys.TenantDelegation.LockAnalytics,
        ValueType: SettingValueType.Boolean,
        DefaultValue: "true",
        Category: "TenantDelegation",
        Description: "Whether tenant administrators can configure their own analytics settings",
        MinScope: SettingScope.Instance,
        MaxScope: SettingScope.Instance,
        IsLockable: false);

    public static readonly SettingDefinition LockAiAssistant = new(
        Key: GovernanceSettingKeys.TenantDelegation.LockAiAssistant,
        ValueType: SettingValueType.Boolean,
        DefaultValue: "true",
        Category: "TenantDelegation",
        Description: "Whether tenant administrators can configure their own AI assistant integration",
        MinScope: SettingScope.Instance,
        MaxScope: SettingScope.Instance,
        IsLockable: false);

    public static readonly SettingDefinition LockMcp = new(
        Key: GovernanceSettingKeys.TenantDelegation.LockMcp,
        ValueType: SettingValueType.Boolean,
        DefaultValue: "true",
        Category: "TenantDelegation",
        Description: "Whether tenant administrators can override MCP adapter runtime enablement",
        MinScope: SettingScope.Instance,
        MaxScope: SettingScope.Instance,
        IsLockable: false);

    public static readonly SettingDefinition LockMcpLegacySse = new(
        Key: GovernanceSettingKeys.TenantDelegation.LockMcpLegacySse,
        ValueType: SettingValueType.Boolean,
        DefaultValue: "true",
        Category: "TenantDelegation",
        Description: "Whether tenant administrators can override MCP legacy SSE runtime requests",
        MinScope: SettingScope.Instance,
        MaxScope: SettingScope.Instance,
        IsLockable: false);

    public static IReadOnlyList<SettingDefinition> All => [LockSmtp, LockStorage, LockAnalytics, LockAiAssistant, LockMcp, LockMcpLegacySse];
}
