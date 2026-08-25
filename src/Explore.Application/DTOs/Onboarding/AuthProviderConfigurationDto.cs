// ABOUTME: DTO for instance-level authentication provider configuration managed during setup and admin UI.
// ABOUTME: Represents enabled auth providers (Keycloak, ATProto, Google) and their credentials.

using Explore.Application.DTOs.Secrets;

namespace Explore.Application.DTOs.Onboarding;

public sealed record AuthProviderConfigurationDto
{
    // Keycloak
    public bool KeycloakEnabled { get; init; }
    public string KeycloakAuthority { get; init; } = string.Empty;
    public string KeycloakClientId { get; init; } = string.Empty;
    public string KeycloakClientSecret { get; set; } = string.Empty;
    public bool KeycloakDetectedFromEnvironment { get; init; }
    public SecretOwnershipDto KeycloakClientSecretOwnership { get; init; } = new();

    // ATProto Login
    public bool AtprotoLoginEnabled { get; init; }
    public string AtprotoPublicUrl { get; init; } = string.Empty;

    // Google SSO
    public bool GoogleSsoEnabled { get; init; }
    public string GoogleClientId { get; init; } = string.Empty;
    public string GoogleClientSecret { get; set; } = string.Empty;

    // Lock flags (for multi-tenant override control)
    public bool LockKeycloakEnabled { get; init; }
    public bool LockAtprotoLoginEnabled { get; init; }
    public bool LockGoogleSsoEnabled { get; init; }
}
