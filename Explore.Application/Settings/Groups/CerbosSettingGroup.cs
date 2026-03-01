// ABOUTME: Strongly-typed Cerbos authorization setting group resolved via batch loading.
// ABOUTME: Replaces the N+1 pattern in CerbosConfigResolver with a single ResolveGroupAsync call.

namespace Explore.Application.Settings.Groups;

using Explore.Application.Contracts.Infrastructure;

/// <summary>
/// Strongly-typed group for Cerbos authorization service settings.
/// </summary>
public class CerbosSettingGroup : ISettingGroup
{
    public string? Endpoint { get; private set; }
    public int Port { get; private set; } = 3593;
    public bool UseTls { get; private set; }
    public string? TlsCertPath { get; private set; }
    public string? TlsKeyPath { get; private set; }
    public int TimeoutSeconds { get; private set; } = 5;
    public string? AdminApiKey { get; private set; }

    public static IEnumerable<string> SettingKeys =>
    [
        "cerbos.endpoint", "cerbos.port", "cerbos.use_tls",
        "cerbos.tls_cert_path", "cerbos.tls_key_path",
        "cerbos.timeout_seconds", "cerbos.admin_api_key"
    ];

    public void Populate(IReadOnlyDictionary<string, ResolvedSetting> settings)
    {
        if (settings.TryGetValue("cerbos.endpoint", out var ep))
            Endpoint = SettingValueSerializer.DeserializeString(ep.Value);
        if (settings.TryGetValue("cerbos.port", out var port))
            Port = SettingValueSerializer.Deserialize(port.Value, 3593);
        if (settings.TryGetValue("cerbos.use_tls", out var tls))
            UseTls = SettingValueSerializer.Deserialize(tls.Value, false);
        if (settings.TryGetValue("cerbos.tls_cert_path", out var cert))
            TlsCertPath = SettingValueSerializer.DeserializeString(cert.Value);
        if (settings.TryGetValue("cerbos.tls_key_path", out var key))
            TlsKeyPath = SettingValueSerializer.DeserializeString(key.Value);
        if (settings.TryGetValue("cerbos.timeout_seconds", out var timeout))
            TimeoutSeconds = SettingValueSerializer.Deserialize(timeout.Value, 5);
        if (settings.TryGetValue("cerbos.admin_api_key", out var apiKey))
            AdminApiKey = SettingValueSerializer.DeserializeString(apiKey.Value);
    }
}
