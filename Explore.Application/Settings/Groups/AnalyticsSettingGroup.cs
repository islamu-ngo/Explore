// ABOUTME: Strongly-typed Analytics setting group resolved via batch loading.
// ABOUTME: Replaces the N+1 pattern in AnalyticsConfigResolver with a single ResolveGroupAsync call.

namespace Explore.Application.Settings.Groups;

using Explore.Application.Contracts.Infrastructure;
using Explore.Domain.Constants;

/// <summary>
/// Strongly-typed group for analytics/tracking settings.
/// </summary>
public class AnalyticsSettingGroup : ISettingGroup
{
    public string Provider { get; private set; } = "none";
    public string ConsentMode { get; private set; } = "pseudonymous";
    public string TransportMode { get; private set; } = "direct";
    public string? EndpointUrl { get; private set; }
    public string? ApiKey { get; private set; }
    public string? PersonalApiKey { get; private set; }
    public bool Enabled { get; private set; }

    public static IEnumerable<string> SettingKeys =>
    [
        GovernanceSettingKeys.Analytics.Provider,
        GovernanceSettingKeys.Analytics.ConsentMode,
        GovernanceSettingKeys.Analytics.TransportMode,
        GovernanceSettingKeys.Analytics.EndpointUrl,
        GovernanceSettingKeys.Analytics.ApiKey,
        GovernanceSettingKeys.Analytics.PersonalApiKey,
        GovernanceSettingKeys.Analytics.Enabled
    ];

    public void Populate(IReadOnlyDictionary<string, ResolvedSetting> settings)
    {
        if (settings.TryGetValue(GovernanceSettingKeys.Analytics.Provider, out var provider))
            Provider = SettingValueSerializer.Deserialize(provider.Value, "none");
        if (settings.TryGetValue(GovernanceSettingKeys.Analytics.ConsentMode, out var consentMode))
            ConsentMode = SettingValueSerializer.Deserialize(consentMode.Value, "pseudonymous");
        if (settings.TryGetValue(GovernanceSettingKeys.Analytics.TransportMode, out var transportMode))
            TransportMode = SettingValueSerializer.Deserialize(transportMode.Value, "direct");
        if (settings.TryGetValue(GovernanceSettingKeys.Analytics.EndpointUrl, out var endpointUrl))
            EndpointUrl = SettingValueSerializer.DeserializeString(endpointUrl.Value);
        if (settings.TryGetValue(GovernanceSettingKeys.Analytics.ApiKey, out var apiKey))
            ApiKey = SettingValueSerializer.DeserializeString(apiKey.Value);
        if (settings.TryGetValue(GovernanceSettingKeys.Analytics.PersonalApiKey, out var personalApiKey))
            PersonalApiKey = SettingValueSerializer.DeserializeString(personalApiKey.Value);
        if (settings.TryGetValue(GovernanceSettingKeys.Analytics.Enabled, out var enabled))
            Enabled = SettingValueSerializer.Deserialize(enabled.Value, false);
    }
}
