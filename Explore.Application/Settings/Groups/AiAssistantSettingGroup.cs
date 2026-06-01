// ABOUTME: Strongly-typed AI assistant setting group resolved via hierarchical settings cascade.
// ABOUTME: Encapsulates enablement and integration credential presence checks.

namespace Explore.Application.Settings.Groups;

using Explore.Application.Contracts.Infrastructure;
using Explore.Domain.Constants;

public class AiAssistantSettingGroup : ISettingGroup
{
    public bool Enabled { get; private set; }
    public string Provider { get; private set; } = "none";
    public string? EndpointUrl { get; private set; }
    public string? ApiKey { get; private set; }
    public string? ModelId { get; private set; }
    public int MaxInputTokens { get; private set; } = 8000;
    public int MaxOutputTokens { get; private set; } = 1024;
    public decimal Temperature { get; private set; } = 0.2m;
    public int TimeoutSeconds { get; private set; } = 30;
    public int RetentionDays { get; private set; } = 30;
    public int DailyMessageLimit { get; private set; } = 50;
    public int DailyTenantMessageLimit { get; private set; } = 1000;
    public int ConcurrentRunLimit { get; private set; } = 1;
    public int SelectedReferenceLimit { get; private set; } = 8;
    public bool ToolProposalsEnabled { get; private set; }
    public bool StreamingEnabled { get; private set; }
    public bool AllowAnonymousAccess { get; private set; }

    public bool HasApiKey => !string.IsNullOrWhiteSpace(ApiKey);
    public bool HasModel => !string.IsNullOrWhiteSpace(ModelId);
    public bool IsFakeProvider => Provider.Equals("fake", StringComparison.OrdinalIgnoreCase);
    public bool IsConfigured => IsFakeProvider || (HasApiKey && HasModel);
    public bool IsAvailable => Enabled && IsConfigured;

    public static IEnumerable<string> SettingKeys =>
    [
        GovernanceSettingKeys.AiAssistant.Enabled,
        GovernanceSettingKeys.AiAssistant.Provider,
        GovernanceSettingKeys.AiAssistant.EndpointUrl,
        GovernanceSettingKeys.AiAssistant.ApiKey,
        GovernanceSettingKeys.AiAssistant.ModelId,
        GovernanceSettingKeys.AiAssistant.MaxInputTokens,
        GovernanceSettingKeys.AiAssistant.MaxOutputTokens,
        GovernanceSettingKeys.AiAssistant.Temperature,
        GovernanceSettingKeys.AiAssistant.TimeoutSeconds,
        GovernanceSettingKeys.AiAssistant.RetentionDays,
        GovernanceSettingKeys.AiAssistant.DailyMessageLimit,
        GovernanceSettingKeys.AiAssistant.DailyTenantMessageLimit,
        GovernanceSettingKeys.AiAssistant.ConcurrentRunLimit,
        GovernanceSettingKeys.AiAssistant.SelectedReferenceLimit,
        GovernanceSettingKeys.AiAssistant.ToolProposalsEnabled,
        GovernanceSettingKeys.AiAssistant.StreamingEnabled,
        GovernanceSettingKeys.AiAssistant.AllowAnonymousAccess
    ];

    public void Populate(IReadOnlyDictionary<string, ResolvedSetting> settings)
    {
        if (settings.TryGetValue(GovernanceSettingKeys.AiAssistant.Enabled, out var enabled))
            Enabled = SettingValueSerializer.Deserialize(enabled.Value, false);
        if (settings.TryGetValue(GovernanceSettingKeys.AiAssistant.Provider, out var provider))
            Provider = SettingValueSerializer.DeserializeString(provider.Value, "none");
        if (settings.TryGetValue(GovernanceSettingKeys.AiAssistant.EndpointUrl, out var endpoint))
            EndpointUrl = SettingValueSerializer.DeserializeString(endpoint.Value);
        if (settings.TryGetValue(GovernanceSettingKeys.AiAssistant.ApiKey, out var apiKey))
            ApiKey = SettingValueSerializer.DeserializeString(apiKey.Value);
        if (settings.TryGetValue(GovernanceSettingKeys.AiAssistant.ModelId, out var modelId))
            ModelId = SettingValueSerializer.DeserializeString(modelId.Value);
        if (settings.TryGetValue(GovernanceSettingKeys.AiAssistant.MaxInputTokens, out var maxInputTokens))
            MaxInputTokens = SettingValueSerializer.DeserializeInt(maxInputTokens.Value, 8000);
        if (settings.TryGetValue(GovernanceSettingKeys.AiAssistant.MaxOutputTokens, out var maxOutputTokens))
            MaxOutputTokens = SettingValueSerializer.DeserializeInt(maxOutputTokens.Value, 1024);
        if (settings.TryGetValue(GovernanceSettingKeys.AiAssistant.Temperature, out var temperature))
            Temperature = SettingValueSerializer.DeserializeDecimal(temperature.Value, 0.2m);
        if (settings.TryGetValue(GovernanceSettingKeys.AiAssistant.TimeoutSeconds, out var timeoutSeconds))
            TimeoutSeconds = SettingValueSerializer.DeserializeInt(timeoutSeconds.Value, 30);
        if (settings.TryGetValue(GovernanceSettingKeys.AiAssistant.RetentionDays, out var retentionDays))
            RetentionDays = SettingValueSerializer.DeserializeInt(retentionDays.Value, 30);
        if (settings.TryGetValue(GovernanceSettingKeys.AiAssistant.DailyMessageLimit, out var dailyMessageLimit))
            DailyMessageLimit = SettingValueSerializer.DeserializeInt(dailyMessageLimit.Value, 50);
        if (settings.TryGetValue(GovernanceSettingKeys.AiAssistant.DailyTenantMessageLimit, out var dailyTenantMessageLimit))
            DailyTenantMessageLimit = SettingValueSerializer.DeserializeInt(dailyTenantMessageLimit.Value, 1000);
        if (settings.TryGetValue(GovernanceSettingKeys.AiAssistant.ConcurrentRunLimit, out var concurrentRunLimit))
            ConcurrentRunLimit = SettingValueSerializer.DeserializeInt(concurrentRunLimit.Value, 1);
        if (settings.TryGetValue(GovernanceSettingKeys.AiAssistant.SelectedReferenceLimit, out var selectedReferenceLimit))
            SelectedReferenceLimit = SettingValueSerializer.DeserializeInt(selectedReferenceLimit.Value, 8);
        if (settings.TryGetValue(GovernanceSettingKeys.AiAssistant.ToolProposalsEnabled, out var toolProposalsEnabled))
            ToolProposalsEnabled = SettingValueSerializer.Deserialize(toolProposalsEnabled.Value, false);
        if (settings.TryGetValue(GovernanceSettingKeys.AiAssistant.StreamingEnabled, out var streamingEnabled))
            StreamingEnabled = SettingValueSerializer.Deserialize(streamingEnabled.Value, false);
        if (settings.TryGetValue(GovernanceSettingKeys.AiAssistant.AllowAnonymousAccess, out var allowAnonymousAccess))
            AllowAnonymousAccess = SettingValueSerializer.Deserialize(allowAnonymousAccess.Value, false);
    }
}
