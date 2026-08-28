// ABOUTME: Strongly-typed reporting-intake policy setting group resolved through the hierarchical settings engine.
// ABOUTME: Keeps report-intake enablement separate from external-provider reporting configuration.

namespace Explore.Application.Settings.Groups;

using Explore.Application.Contracts.Infrastructure;
using Explore.Domain.Constants;

public sealed class ReportingIntakeSettingGroup : ISettingGroup
{
    public bool IntakeEnabled { get; private set; } = true;

    public static IEnumerable<string> SettingKeys => [GovernanceSettingKeys.EventReporting.IntakeEnabled];

    public void Populate(IReadOnlyDictionary<string, ResolvedSetting> settings)
    {
        if (settings.TryGetValue(GovernanceSettingKeys.EventReporting.IntakeEnabled, out var intakeEnabled))
            IntakeEnabled = SettingValueSerializer.Deserialize(intakeEnabled.Value, true);
    }
}
