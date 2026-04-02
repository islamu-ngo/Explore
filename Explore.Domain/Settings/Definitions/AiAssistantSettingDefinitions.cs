// ABOUTME: Setting definitions for AI assistant integration and feature enablement at instance/tenant scope.
// ABOUTME: Enables hierarchical governance with tenant overrides when instance delegation allows it.

namespace Explore.Domain.Settings.Definitions;

public static class AiAssistantSettingDefinitions
{
    public static readonly SettingDefinition Enabled = new(
        Key: "ai_assistant.enabled",
        ValueType: SettingValueType.Boolean,
        DefaultValue: "false",
        Category: "AiAssistant",
        Description: "Enable AI assistant in the application shell",
        MaxScope: SettingScope.Tenant);

    public static readonly SettingDefinition EndpointUrl = new(
        Key: "ai_assistant.endpoint_url",
        ValueType: SettingValueType.String,
        DefaultValue: "\"\"",
        Category: "AiAssistant",
        Description: "Optional AI provider endpoint URL (for self-hosted or compatible APIs)",
        MaxScope: SettingScope.Tenant);

    public static readonly SettingDefinition ApiKey = new(
        Key: "ai_assistant.api_key",
        ValueType: SettingValueType.String,
        DefaultValue: "\"\"",
        Category: "AiAssistant",
        Description: "AI provider API key",
        MaxScope: SettingScope.Tenant,
        IsSensitive: true);

    public static IReadOnlyList<SettingDefinition> All => [Enabled, EndpointUrl, ApiKey];
}
