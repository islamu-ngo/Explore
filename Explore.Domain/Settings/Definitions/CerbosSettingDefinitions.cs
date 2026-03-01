// ABOUTME: Setting definitions for Cerbos authorization engine configuration.
// ABOUTME: Sensitive keys (admin credentials) are flagged with IsSensitive = true.

namespace Explore.Domain.Settings.Definitions;

public static class CerbosSettingDefinitions
{
    public static readonly SettingDefinition TenantCustomizationEnabled = new(
        Key: "cerbos.tenant_customization_enabled",
        ValueType: SettingValueType.Boolean,
        DefaultValue: "false",
        Category: "Cerbos",
        Description: "Whether tenant-specific Cerbos policy customization is enabled");

    public static readonly SettingDefinition Mode = new(
        Key: "cerbos.mode",
        ValueType: SettingValueType.String,
        DefaultValue: "\"shared\"",
        Category: "Cerbos",
        Description: "Cerbos operational mode (shared, dedicated)",
        MaxScope: SettingScope.Tenant);

    public static readonly SettingDefinition CustomEndpoint = new(
        Key: "cerbos.custom_endpoint",
        ValueType: SettingValueType.String,
        DefaultValue: "\"\"",
        Category: "Cerbos",
        Description: "Custom Cerbos gRPC endpoint URL",
        MaxScope: SettingScope.Tenant);

    public static readonly SettingDefinition FailureMode = new(
        Key: "cerbos.failure_mode",
        ValueType: SettingValueType.String,
        DefaultValue: "\"deny\"",
        Category: "Cerbos",
        Description: "Behavior when Cerbos is unreachable (deny, allow)");

    public static readonly SettingDefinition CustomAdminEndpoint = new(
        Key: "cerbos.custom_admin_endpoint",
        ValueType: SettingValueType.String,
        DefaultValue: "\"\"",
        Category: "Cerbos",
        Description: "Custom Cerbos Admin API endpoint URL",
        MaxScope: SettingScope.Tenant);

    public static readonly SettingDefinition CustomAdminUsername = new(
        Key: "cerbos.custom_admin_username",
        ValueType: SettingValueType.String,
        DefaultValue: "\"\"",
        Category: "Cerbos",
        Description: "Cerbos Admin API username",
        MaxScope: SettingScope.Tenant,
        IsSensitive: true);

    public static readonly SettingDefinition CustomAdminPassword = new(
        Key: "cerbos.custom_admin_password",
        ValueType: SettingValueType.String,
        DefaultValue: "\"\"",
        Category: "Cerbos",
        Description: "Cerbos Admin API password",
        MaxScope: SettingScope.Tenant,
        IsSensitive: true);

    public static IReadOnlyList<SettingDefinition> All =>
    [
        TenantCustomizationEnabled, Mode, CustomEndpoint, FailureMode,
        CustomAdminEndpoint, CustomAdminUsername, CustomAdminPassword
    ];
}
