// ABOUTME: Strongly-typed AI assistant setting group resolved via hierarchical settings cascade.
// ABOUTME: Encapsulates enablement and integration credential presence checks.

namespace Explore.Application.Settings.Groups;

using Explore.Application.Contracts.Infrastructure;
using Explore.Domain.Constants;

public class AiAssistantSettingGroup : ISettingGroup
{
    public bool Enabled { get; private set; }
    public string? EndpointUrl { get; private set; }
    public string? ApiKey { get; private set; }
    public bool AllowAnonymousAccess { get; private set; }

    public bool HasApiKey => !string.IsNullOrWhiteSpace(ApiKey);
    public bool IsConfigured => HasApiKey;
    public bool IsAvailable => Enabled && IsConfigured;

    public static IEnumerable<string> SettingKeys =>
    [
        GovernanceSettingKeys.AiAssistant.Enabled,
        GovernanceSettingKeys.AiAssistant.EndpointUrl,
        GovernanceSettingKeys.AiAssistant.ApiKey,
        GovernanceSettingKeys.AiAssistant.AllowAnonymousAccess
    ];

    public void Populate(IReadOnlyDictionary<string, ResolvedSetting> settings)
    {
        if (settings.TryGetValue(GovernanceSettingKeys.AiAssistant.Enabled, out var enabled))
            Enabled = SettingValueSerializer.Deserialize(enabled.Value, false);
        if (settings.TryGetValue(GovernanceSettingKeys.AiAssistant.EndpointUrl, out var endpoint))
            EndpointUrl = SettingValueSerializer.DeserializeString(endpoint.Value);
        if (settings.TryGetValue(GovernanceSettingKeys.AiAssistant.ApiKey, out var apiKey))
            ApiKey = SettingValueSerializer.DeserializeString(apiKey.Value);
        if (settings.TryGetValue(GovernanceSettingKeys.AiAssistant.AllowAnonymousAccess, out var allowAnonymousAccess))
            AllowAnonymousAccess = SettingValueSerializer.Deserialize(allowAnonymousAccess.Value, false);
    }
}
