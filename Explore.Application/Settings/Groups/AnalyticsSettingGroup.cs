// ABOUTME: Strongly-typed Analytics setting group resolved via batch loading.
// ABOUTME: Replaces the N+1 pattern in AnalyticsConfigResolver with a single ResolveGroupAsync call.

namespace Explore.Application.Settings.Groups;

using Explore.Application.Contracts.Infrastructure;

/// <summary>
/// Strongly-typed group for analytics/tracking settings.
/// </summary>
public class AnalyticsSettingGroup : ISettingGroup
{
    public string Provider { get; private set; } = "none";
    public string? SiteId { get; private set; }
    public string? Endpoint { get; private set; }
    public string? ApiKey { get; private set; }
    public bool Enabled { get; private set; }

    public static IEnumerable<string> SettingKeys =>
    [
        "analytics.provider", "analytics.site_id", "analytics.endpoint",
        "analytics.api_key", "analytics.enabled"
    ];

    public void Populate(IReadOnlyDictionary<string, ResolvedSetting> settings)
    {
        if (settings.TryGetValue("analytics.provider", out var provider))
            Provider = SettingValueSerializer.Deserialize(provider.Value, "none");
        if (settings.TryGetValue("analytics.site_id", out var siteId))
            SiteId = SettingValueSerializer.DeserializeString(siteId.Value);
        if (settings.TryGetValue("analytics.endpoint", out var ep))
            Endpoint = SettingValueSerializer.DeserializeString(ep.Value);
        if (settings.TryGetValue("analytics.api_key", out var apiKey))
            ApiKey = SettingValueSerializer.DeserializeString(apiKey.Value);
        if (settings.TryGetValue("analytics.enabled", out var enabled))
            Enabled = SettingValueSerializer.Deserialize(enabled.Value, false);
    }
}
