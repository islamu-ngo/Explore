// ABOUTME: Setting definitions for authorization provider selection.
// ABOUTME: Instance-only setting controlling which authorization backend is used.

namespace Explore.Domain.Settings.Definitions;

public static class SecuritySettingDefinitions
{
    public static readonly SettingDefinition AuthorizationProvider = new(
        Key: "authorization.provider",
        ValueType: SettingValueType.String,
        DefaultValue: "\"cerbos\"",
        Category: "Security",
        Description: "Authorization provider (cerbos)",
        MinScope: SettingScope.Instance,
        MaxScope: SettingScope.Instance);

    public static IReadOnlyList<SettingDefinition> All => [AuthorizationProvider];
}
