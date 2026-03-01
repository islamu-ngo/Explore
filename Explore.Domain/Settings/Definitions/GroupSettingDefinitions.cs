// ABOUTME: Setting definitions for group self-registration policies.
// ABOUTME: Controls whether users can self-register groups within a tenant.

namespace Explore.Domain.Settings.Definitions;

public static class GroupSettingDefinitions
{
    public static readonly SettingDefinition SelfRegistrationEnabled = new(
        Key: "groups.self_registration_enabled",
        ValueType: SettingValueType.Boolean,
        DefaultValue: "true",
        Category: "Groups",
        Description: "Whether users can self-register groups",
        MaxScope: SettingScope.Tenant);

    public static IReadOnlyList<SettingDefinition> All =>
        [SelfRegistrationEnabled];
}
