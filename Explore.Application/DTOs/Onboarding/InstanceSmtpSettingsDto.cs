// ABOUTME: DTO for instance-level SMTP settings managed via admin UI.
// ABOUTME: Represents host, auth, sender identity, security mode, timeout, and TLS validation behavior.

namespace Explore.Application.DTOs.Onboarding;

public class InstanceSmtpSettingsDto
{
    public string Host { get; set; } = string.Empty;
    public int Port { get; set; } = 587;
    public string Username { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public string Security { get; set; } = "StartTls";
    public string FromAddress { get; set; } = string.Empty;
    public string FromName { get; set; } = string.Empty;
    public int TimeoutSeconds { get; set; } = 30;
    public bool SkipCertificateValidation { get; set; }
}
