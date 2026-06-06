// ABOUTME: Request DTO for setup-time Keycloak realm and client bootstrap.
// ABOUTME: Separates runtime OIDC settings from one-time bootstrap credentials that must not be persisted.

namespace Explore.Application.DTOs.Onboarding;

using Explore.Application.Onboarding;

public class KeycloakBootstrapRequestDto
{
    public string KeycloakBaseUrl { get; set; } = string.Empty;
    public string Realm { get; set; } = string.Empty;
    public string BlazorClientId { get; set; } = string.Empty;
    public string BlazorClientSecret { get; set; } = string.Empty;
    public IReadOnlyList<string> BlazorRedirectUris { get; set; } = [];
    public IReadOnlyList<string> BlazorWebOrigins { get; set; } = [];
    public string? ApiClientId { get; set; }
    public string? ApiClientSecret { get; set; }
    public KeycloakBootstrapMode Mode { get; set; } = KeycloakBootstrapMode.PatchExistingRealm;
    public string BootstrapAdminUsername { get; set; } = string.Empty;
    public string BootstrapAdminPassword { get; set; } = string.Empty;
}
