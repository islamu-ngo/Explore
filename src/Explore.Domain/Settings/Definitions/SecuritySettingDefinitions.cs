// ABOUTME: Instance security settings for authorization selection and external URL transport policy.
// ABOUTME: Defaults public-facing URLs to HTTPS while permitting explicit private-network opt-out.

using Explore.Domain.Constants;

namespace Explore.Domain.Settings.Definitions;

public static class SecuritySettingDefinitions
{
    public static readonly SettingDefinition AuthorizationProvider = new(
        Key: GovernanceSettingKeys.Security.AuthorizationProvider,
        ValueType: SettingValueType.String,
        DefaultValue: "\"cerbos\"",
        Category: "Security",
        Description: "Authorization provider (cerbos)",
        MinScope: SettingScope.Instance,
        MaxScope: SettingScope.Instance);

    public static readonly SettingDefinition RequireHttpsExternalUrls = new(
        Key: GovernanceSettingKeys.Security.RequireHttpsExternalUrls,
        ValueType: SettingValueType.Boolean,
        DefaultValue: "true",
        Category: "Security",
        Description: "Require HTTPS for externally addressed URLs; disable only for trusted HTTP-only private networks",
        MinScope: SettingScope.Instance,
        MaxScope: SettingScope.Instance,
        IsLockable: false);

    public static IReadOnlyList<SettingDefinition> All =>
        [AuthorizationProvider, RequireHttpsExternalUrls];
}
