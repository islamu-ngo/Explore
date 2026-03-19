// ABOUTME: Strongly-typed Cerbos authorization setting group resolved via batch loading.
// ABOUTME: Keys align to CerbosSettingDefinitions and InfrastructureSecretSettingKeys.Cerbos.

namespace Explore.Application.Settings.Groups;

using Explore.Application.Contracts.Infrastructure;
using Explore.Domain.Constants;

/// <summary>
/// Strongly-typed group for Cerbos authorization service settings.
/// </summary>
public class CerbosSettingGroup : ISettingGroup
{
    public bool TenantCustomizationEnabled { get; private set; }
    public string Mode { get; private set; } = "shared";
    public string? CustomEndpoint { get; private set; }
    public string FailureMode { get; private set; } = "deny";
    public string? CustomAdminEndpoint { get; private set; }
    public string? CustomAdminUsername { get; private set; }
    public string? CustomAdminPassword { get; private set; }

    public static IEnumerable<string> SettingKeys =>
    [
        GovernanceSettingKeys.Cerbos.TenantCustomizationEnabled,
        GovernanceSettingKeys.Cerbos.Mode,
        GovernanceSettingKeys.Cerbos.CustomEndpoint,
        GovernanceSettingKeys.Cerbos.FailureMode,
        GovernanceSettingKeys.Cerbos.CustomAdminEndpoint,
        InfrastructureSecretSettingKeys.Cerbos.CustomAdminUsername,
        InfrastructureSecretSettingKeys.Cerbos.CustomAdminPassword
    ];

    public void Populate(IReadOnlyDictionary<string, ResolvedSetting> settings)
    {
        if (settings.TryGetValue(GovernanceSettingKeys.Cerbos.TenantCustomizationEnabled, out var tce))
            TenantCustomizationEnabled = SettingValueSerializer.Deserialize(tce.Value, false);
        if (settings.TryGetValue(GovernanceSettingKeys.Cerbos.Mode, out var mode))
            Mode = SettingValueSerializer.Deserialize(mode.Value, "shared");
        if (settings.TryGetValue(GovernanceSettingKeys.Cerbos.CustomEndpoint, out var ep))
            CustomEndpoint = SettingValueSerializer.DeserializeString(ep.Value);
        if (settings.TryGetValue(GovernanceSettingKeys.Cerbos.FailureMode, out var fm))
            FailureMode = SettingValueSerializer.Deserialize(fm.Value, "deny");
        if (settings.TryGetValue(GovernanceSettingKeys.Cerbos.CustomAdminEndpoint, out var aep))
            CustomAdminEndpoint = SettingValueSerializer.DeserializeString(aep.Value);
        if (settings.TryGetValue(InfrastructureSecretSettingKeys.Cerbos.CustomAdminUsername, out var user))
            CustomAdminUsername = SettingValueSerializer.DeserializeString(user.Value);
        if (settings.TryGetValue(InfrastructureSecretSettingKeys.Cerbos.CustomAdminPassword, out var pass))
            CustomAdminPassword = SettingValueSerializer.DeserializeString(pass.Value);
    }
}
