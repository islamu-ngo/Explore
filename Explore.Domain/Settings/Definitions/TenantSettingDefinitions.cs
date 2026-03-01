// ABOUTME: Setting definitions for tenant-level policies (self-service registration, white-labeling).
// ABOUTME: Instance-only settings that control tenant capabilities.

namespace Explore.Domain.Settings.Definitions;

public static class TenantSettingDefinitions
{
    public static readonly SettingDefinition SelfServiceRegistration = new(
        Key: "tenants.self_service_registration",
        ValueType: SettingValueType.Boolean,
        DefaultValue: "false",
        Category: "Tenant",
        Description: "Whether tenants can self-register without manual instance admin invitation");

    public static readonly SettingDefinition WhiteLabelingEnabled = new(
        Key: "tenants.white_labeling_enabled",
        ValueType: SettingValueType.Boolean,
        DefaultValue: "false",
        Category: "Tenant",
        Description: "Whether tenant-level white-label branding overrides are enabled in multi-tenant mode");

    public static IReadOnlyList<SettingDefinition> All =>
        [SelfServiceRegistration, WhiteLabelingEnabled];
}
