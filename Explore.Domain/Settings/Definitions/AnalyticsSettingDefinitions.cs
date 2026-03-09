// ABOUTME: Setting definitions for analytics provider configuration (PostHog, Plausible, Rybbit, etc.).
// ABOUTME: Overridable at Tenant scope so tenants can use their own analytics.

namespace Explore.Domain.Settings.Definitions;

public static class AnalyticsSettingDefinitions
{
    public static readonly SettingDefinition Provider = new(
        Key: "analytics.provider",
        ValueType: SettingValueType.String,
        DefaultValue: "\"none\"",
        Category: "Analytics",
        Description: "Analytics provider (none, posthog, plausible, rybbit, rudderstack)",
        MaxScope: SettingScope.Tenant,
        AllowedValues: ["none", "posthog", "plausible", "rybbit", "rudderstack"]);

    public static readonly SettingDefinition Enabled = new(
        Key: "analytics.enabled",
        ValueType: SettingValueType.Boolean,
        DefaultValue: "false",
        Category: "Analytics",
        Description: "Enable analytics tracking",
        MaxScope: SettingScope.Tenant);

    public static readonly SettingDefinition ConsentMode = new(
        Key: "analytics.consent_mode",
        ValueType: SettingValueType.String,
        DefaultValue: "\"pseudonymous\"",
        Category: "Analytics",
        Description: "Analytics identity mode (anonymous, pseudonymous, identified)",
        MaxScope: SettingScope.Tenant,
        AllowedValues: ["anonymous", "pseudonymous", "identified"]);

    public static readonly SettingDefinition TransportMode = new(
        Key: "analytics.transport_mode",
        ValueType: SettingValueType.String,
        DefaultValue: "\"direct\"",
        Category: "Analytics",
        Description: "Analytics browser transport mode (direct, proxy, relay)",
        MaxScope: SettingScope.Tenant,
        AllowedValues: ["direct", "proxy", "relay"]);

    public static readonly SettingDefinition ApiKey = new(
        Key: "analytics.api_key",
        ValueType: SettingValueType.String,
        DefaultValue: "\"\"",
        Category: "Analytics",
        Description: "Analytics provider public/write API key",
        MaxScope: SettingScope.Tenant);

    public static readonly SettingDefinition EndpointUrl = new(
        Key: "analytics.endpoint_url",
        ValueType: SettingValueType.String,
        DefaultValue: "\"\"",
        Category: "Analytics",
        Description: "Analytics provider endpoint URL (supports self-hosted deployments)",
        MaxScope: SettingScope.Tenant);

    public static readonly SettingDefinition PersonalApiKey = new(
        Key: "analytics.personal_api_key",
        ValueType: SettingValueType.String,
        DefaultValue: "\"\"",
        Category: "Analytics",
        Description: "Personal API key used for analytics feature flag evaluation when supported",
        MaxScope: SettingScope.Tenant,
        IsSensitive: true);

    public static IReadOnlyList<SettingDefinition> All =>
        [Provider, Enabled, ConsentMode, TransportMode, ApiKey, EndpointUrl, PersonalApiKey];
}
