// ABOUTME: DTO for instance-level SMTP settings managed via admin UI.
// ABOUTME: Represents host, auth, sender identity, security mode, timeout, and TLS validation behavior.

using System.Text.Json.Serialization;

namespace Explore.Application.DTOs.Onboarding;

public sealed record InstanceSmtpSettingsDto
{
    public string Host { get; set; } = string.Empty;
    public int Port { get; set; } = 587;
    [JsonIgnore]
    public string Username { get; set; } = string.Empty;
    [JsonIgnore]
    public string Password { get; set; } = string.Empty;
    public bool UsernameConfigured { get; init; }
    public bool PasswordConfigured { get; init; }
    public string Security { get; set; } = "StartTls";
    public string FromAddress { get; set; } = string.Empty;
    public string FromName { get; set; } = string.Empty;
    public int TimeoutSeconds { get; set; } = 30;
    public bool SkipCertificateValidation { get; set; }
}
