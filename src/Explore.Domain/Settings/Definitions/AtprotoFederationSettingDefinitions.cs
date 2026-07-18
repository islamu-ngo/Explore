// ABOUTME: Governance definitions for ATProto event capability, validation profile, and personal publication consent.
// ABOUTME: Keeps administrator controls tenant-bounded while publication consent remains current-user-only.

namespace Explore.Domain.Settings.Definitions;

using Explore.Domain.Constants;

public static class AtprotoFederationSettingDefinitions
{
    public const string Category = "AtprotoFederation";

    public static readonly SettingDefinition EventsEnabled = new(
        Key: GovernanceSettingKeys.Federation.AtprotoEventsEnabled,
        ValueType: SettingValueType.Boolean,
        DefaultValue: "false",
        Category: Category,
        Description: "Enable ATProto event fetching and eligible outbound publication",
        MinScope: SettingScope.Instance,
        MaxScope: SettingScope.Tenant,
        IsLockable: true);

    public static readonly SettingDefinition EventValidationProfile = new(
        Key: GovernanceSettingKeys.Federation.AtprotoEventValidationProfile,
        ValueType: SettingValueType.String,
        DefaultValue: "\"platform\"",
        Category: Category,
        Description: "Select platform or community-lexicon event publication readiness",
        MinScope: SettingScope.Instance,
        MaxScope: SettingScope.Tenant,
        IsLockable: true,
        AllowedValues: ["platform", "community_lexicon"]);

    public static readonly SettingDefinition PublishMyEvents = new(
        Key: GovernanceSettingKeys.Federation.AtprotoPublishMyEvents,
        ValueType: SettingValueType.Boolean,
        DefaultValue: "false",
        Category: Category,
        Description: "Allow the authenticated user's eligible events to be published to their linked PDS",
        MinScope: SettingScope.User,
        MaxScope: SettingScope.User,
        IsLockable: false);

    public static IReadOnlyList<SettingDefinition> All =>
        [EventsEnabled, EventValidationProfile, PublishMyEvents];

    public static IReadOnlyList<string> AdministratorKeys =>
        [EventsEnabled.Key, EventValidationProfile.Key];

    public static bool IsAdministratorKey(string key) =>
        AdministratorKeys.Contains(key, StringComparer.Ordinal);
}
