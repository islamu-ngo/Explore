// ABOUTME: Defines the canonical tenant-scoped policy for accepting event reports.
// ABOUTME: Keeps reporting-intake policy separate from external reporting-provider configuration.

namespace Explore.Domain.Settings.Definitions;

using Explore.Domain.Constants;

public static class EventReportingIntakeSettingDefinitions
{
    public static readonly SettingDefinition IntakeEnabled = new(
        Key: GovernanceSettingKeys.EventReporting.IntakeEnabled,
        ValueType: SettingValueType.Boolean,
        DefaultValue: "true",
        Category: "EventReporting",
        Description: "Whether the tenant accepts event reports",
        MinScope: SettingScope.Instance,
        MaxScope: SettingScope.Tenant,
        IsLockable: true,
        IsSensitive: false)
    {
        RequiresCoordinatedMutation = true,
    };

    public static IReadOnlyList<SettingDefinition> All => [IntakeEnabled];
}
