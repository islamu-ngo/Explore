// ABOUTME: Setting definitions for conservative anonymous public-experience posture configuration.
// ABOUTME: Keeps metadata instance-to-tenant scoped and stores bounded config documents as JSON defaults.

namespace Explore.Domain.Settings.Definitions;

using Explore.Domain.Constants;

public static class PublicExperienceSettingDefinitions
{
    public static readonly SettingDefinition Mode = PublicExperienceDefinition(
        key: GovernanceSettingKeys.PublicExperience.Mode,
        valueType: SettingValueType.String,
        defaultValue: "\"DiscoveryCentric\"",
        description: "Anonymous public experience posture. Allowed values: DiscoveryCentric, OrganizationCentric",
        allowedValues: ["DiscoveryCentric", "OrganizationCentric"]);

    public static readonly SettingDefinition EventCatalogLabel = PublicExperienceDefinition(
        key: GovernanceSettingKeys.PublicExperience.EventCatalogLabel,
        valueType: SettingValueType.String,
        defaultValue: "\"Events\"",
        description: "Display label for the public event catalog entry point");

    public static readonly SettingDefinition PrimaryOrganizationId = PublicExperienceDefinition(
        key: GovernanceSettingKeys.PublicExperience.PrimaryOrganizationId,
        valueType: SettingValueType.String,
        defaultValue: "\"\"",
        description: "Optional tenant-local primary organization identifier for organization-centric public experience metadata");

    public static readonly SettingDefinition HomeBlocks = PublicExperienceDefinition(
        key: GovernanceSettingKeys.PublicExperience.HomeBlocks,
        valueType: SettingValueType.Json,
        defaultValue: "{\"schemaVersion\":1,\"blocks\":[]}",
        description: "Versioned public home block configuration document");

    public static readonly SettingDefinition Ctas = PublicExperienceDefinition(
        key: GovernanceSettingKeys.PublicExperience.Ctas,
        valueType: SettingValueType.Json,
        defaultValue: "{\"schemaVersion\":1,\"ctas\":[]}",
        description: "Versioned public call-to-action configuration document");

    public static readonly SettingDefinition EventSectionPresets = PublicExperienceDefinition(
        key: GovernanceSettingKeys.PublicExperience.EventSectionPresets,
        valueType: SettingValueType.Json,
        defaultValue: "{\"schemaVersion\":1,\"presets\":[]}",
        description: "Versioned public event-section preset configuration document");

    public static IReadOnlyList<SettingDefinition> All =>
    [
        Mode,
        EventCatalogLabel,
        PrimaryOrganizationId,
        HomeBlocks,
        Ctas,
        EventSectionPresets
    ];

    private static SettingDefinition PublicExperienceDefinition(
        string key,
        SettingValueType valueType,
        string defaultValue,
        string description,
        string[]? allowedValues = null) =>
        new(
            Key: key,
            ValueType: valueType,
            DefaultValue: defaultValue,
            Category: "PublicExperience",
            Description: description,
            MinScope: SettingScope.Instance,
            MaxScope: SettingScope.Tenant,
            AllowedValues: allowedValues);
}
