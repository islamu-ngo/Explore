// ABOUTME: Strongly-typed Email/SMTP setting group resolved via batch loading.
// ABOUTME: Replaces the N+1 pattern in SmtpConfigResolver with a single ResolveGroupAsync call.

namespace Explore.Application.Settings.Groups;

using Explore.Application.Contracts.Infrastructure;

/// <summary>
/// Strongly-typed group for all email/SMTP settings.
/// Resolved in a single batch call instead of 9 individual GetSettingAsync calls.
/// </summary>
public class EmailSettingGroup : ISettingGroup
{
    public string? SmtpHost { get; private set; }
    public int SmtpPort { get; private set; } = 587;
    public string? SmtpUsername { get; private set; }
    public string? SmtpPassword { get; private set; }
    public string SmtpSecurity { get; private set; } = "StartTls";
    public string? FromAddress { get; private set; }
    public string FromName { get; private set; } = "Explore";
    public int SmtpTimeoutSeconds { get; private set; } = 30;
    public bool SmtpSkipCertValidation { get; private set; }

    public static IEnumerable<string> SettingKeys =>
    [
        "email.smtp_host", "email.smtp_port", "email.smtp_username",
        "email.smtp_password", "email.smtp_security", "email.from_address",
        "email.from_name", "email.smtp_timeout_seconds", "email.smtp_skip_cert_validation"
    ];

    public void Populate(IReadOnlyDictionary<string, ResolvedSetting> settings)
    {
        if (settings.TryGetValue("email.smtp_host", out var host))
            SmtpHost = SettingValueSerializer.DeserializeString(host.Value);
        if (settings.TryGetValue("email.smtp_port", out var port))
            SmtpPort = SettingValueSerializer.Deserialize(port.Value, 587);
        if (settings.TryGetValue("email.smtp_username", out var user))
            SmtpUsername = SettingValueSerializer.DeserializeString(user.Value);
        if (settings.TryGetValue("email.smtp_password", out var pass))
            SmtpPassword = SettingValueSerializer.DeserializeString(pass.Value);
        if (settings.TryGetValue("email.smtp_security", out var sec))
            SmtpSecurity = SettingValueSerializer.Deserialize(sec.Value, "StartTls");
        if (settings.TryGetValue("email.from_address", out var from))
            FromAddress = SettingValueSerializer.DeserializeString(from.Value);
        if (settings.TryGetValue("email.from_name", out var name))
            FromName = SettingValueSerializer.Deserialize(name.Value, "Explore");
        if (settings.TryGetValue("email.smtp_timeout_seconds", out var timeout))
            SmtpTimeoutSeconds = SettingValueSerializer.Deserialize(timeout.Value, 30);
        if (settings.TryGetValue("email.smtp_skip_cert_validation", out var skipCert))
            SmtpSkipCertValidation = SettingValueSerializer.Deserialize(skipCert.Value, false);
    }
}
