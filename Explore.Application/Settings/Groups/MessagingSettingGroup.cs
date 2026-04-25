// ABOUTME: Strongly-typed Messaging setting group resolved via batch loading.
// ABOUTME: Replaces N+1 pattern in MessagingConfigResolver with a single ResolveGroupAsync call.

namespace Explore.Application.Settings.Groups;

using Explore.Application.Contracts.Infrastructure;
using Explore.Domain.Constants;
using Explore.Domain.Enums;

/// <summary>
/// Strongly-typed group for message queue settings.
/// </summary>
public class MessagingSettingGroup : ISettingGroup
{
    public string Provider { get; private set; } = "none";
    public bool Enabled { get; private set; }
    public string? HostName { get; private set; }
    public int Port { get; private set; } = 5672;
    public string? UserName { get; private set; }
    public string? Password { get; private set; }
    public string? VirtualHost { get; private set; } = "/";
    public int MaxInboundMessageBodySize { get; private set; } = 1024 * 1024 * 4;  // 4MB
    public int CircuitBreakerFailureThreshold { get; private set; } = 5;
    public int CircuitBreakerBreakDurationSeconds { get; private set; } = 30;
    public int RetryAttempts { get; private set; } = 3;
    public bool EnableOpenTelemetry { get; private set; } = true;
    public bool EnableCompression { get; private set; } = true;

    /// <summary>Parsed provider enum with safe fallback.</summary>
    public MessagingProviderEnum ProviderEnum => Enum.TryParse<MessagingProviderEnum>(Provider, ignoreCase: true, out var p) ? p : MessagingProviderEnum.None;

    public static IEnumerable<string> SettingKeys =>
    [
        GovernanceSettingKeys.Messaging.Provider,
        GovernanceSettingKeys.Messaging.Enabled,
        GovernanceSettingKeys.Messaging.HostName,
        GovernanceSettingKeys.Messaging.Port,
        GovernanceSettingKeys.Messaging.UserName,
        GovernanceSettingKeys.Messaging.Password,
        GovernanceSettingKeys.Messaging.VirtualHost,
        GovernanceSettingKeys.Messaging.MaxInboundMessageBodySize,
        GovernanceSettingKeys.Messaging.CircuitBreakerFailureThreshold,
        GovernanceSettingKeys.Messaging.CircuitBreakerBreakDurationSeconds,
        GovernanceSettingKeys.Messaging.RetryAttempts,
        GovernanceSettingKeys.Messaging.EnableOpenTelemetry,
        GovernanceSettingKeys.Messaging.EnableCompression
    ];

    public void Populate(IReadOnlyDictionary<string, ResolvedSetting> settings)
    {
        if (settings.TryGetValue(GovernanceSettingKeys.Messaging.Provider, out var provider))
            Provider = SettingValueSerializer.Deserialize(provider.Value, "none");
        if (settings.TryGetValue(GovernanceSettingKeys.Messaging.Enabled, out var enabled))
            Enabled = SettingValueSerializer.Deserialize(enabled.Value, false);
        if (settings.TryGetValue(GovernanceSettingKeys.Messaging.HostName, out var hostName))
            HostName = SettingValueSerializer.DeserializeString(hostName.Value);
        if (settings.TryGetValue(GovernanceSettingKeys.Messaging.Port, out var port))
            Port = SettingValueSerializer.DeserializeInt(port.Value, 5672);
        if (settings.TryGetValue(GovernanceSettingKeys.Messaging.UserName, out var userName))
            UserName = SettingValueSerializer.DeserializeString(userName.Value);
        if (settings.TryGetValue(GovernanceSettingKeys.Messaging.Password, out var password))
            Password = SettingValueSerializer.DeserializeString(password.Value);
        if (settings.TryGetValue(GovernanceSettingKeys.Messaging.VirtualHost, out var virtualHost))
            VirtualHost = SettingValueSerializer.Deserialize(virtualHost.Value, "/");
        if (settings.TryGetValue(GovernanceSettingKeys.Messaging.MaxInboundMessageBodySize, out var maxSize))
            MaxInboundMessageBodySize = SettingValueSerializer.DeserializeInt(maxSize.Value, 1024 * 1024 * 4);
        if (settings.TryGetValue(GovernanceSettingKeys.Messaging.CircuitBreakerFailureThreshold, out var cbThreshold))
            CircuitBreakerFailureThreshold = SettingValueSerializer.DeserializeInt(cbThreshold.Value, 5);
        if (settings.TryGetValue(GovernanceSettingKeys.Messaging.CircuitBreakerBreakDurationSeconds, out var cbDuration))
            CircuitBreakerBreakDurationSeconds = SettingValueSerializer.DeserializeInt(cbDuration.Value, 30);
        if (settings.TryGetValue(GovernanceSettingKeys.Messaging.RetryAttempts, out var retries))
            RetryAttempts = SettingValueSerializer.DeserializeInt(retries.Value, 3);
        if (settings.TryGetValue(GovernanceSettingKeys.Messaging.EnableOpenTelemetry, out var otel))
            EnableOpenTelemetry = SettingValueSerializer.Deserialize(otel.Value, true);
        if (settings.TryGetValue(GovernanceSettingKeys.Messaging.EnableCompression, out var compression))
            EnableCompression = SettingValueSerializer.Deserialize(compression.Value, true);
    }
}
