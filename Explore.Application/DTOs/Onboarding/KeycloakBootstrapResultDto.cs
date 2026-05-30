// ABOUTME: Safe result DTO for setup-time Keycloak bootstrap outcomes.
// ABOUTME: Reports realm/client status without echoing admin credentials, tokens, secrets, or provider response bodies.

namespace Explore.Application.DTOs.Onboarding;

using Explore.Application.Onboarding;

public class KeycloakBootstrapResultDto
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public string? FailureCode { get; set; }
    public KeycloakBootstrapMode Mode { get; set; }
    public string Realm { get; set; } = string.Empty;
    public string BlazorClientId { get; set; } = string.Empty;
    public string? ApiClientId { get; set; }
    public bool RealmCreated { get; set; }
    public bool BlazorClientUpdated { get; set; }
    public bool ApiClientUpdated { get; set; }
}
