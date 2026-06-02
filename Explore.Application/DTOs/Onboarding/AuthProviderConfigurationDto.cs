// ABOUTME: DTO for instance-level authentication provider configuration managed during setup and admin UI.
// ABOUTME: Represents enabled auth providers (Keycloak, ATProto, Google) and their credentials.

using Explore.Application.DTOs.Secrets;

namespace Explore.Application.DTOs.Onboarding;

public class AuthProviderConfigurationDto
{
    // Keycloak
    public bool KeycloakEnabled { get; set; }
    public string KeycloakAuthority { get; set; } = string.Empty;
    public string KeycloakClientId { get; set; } = string.Empty;
    public string KeycloakClientSecret { get; set; } = string.Empty;
    public bool KeycloakDetectedFromEnvironment { get; set; }
    public SecretOwnershipDto KeycloakClientSecretOwnership { get; set; } = new();

    // ATProto Login
    public bool AtprotoLoginEnabled { get; set; }
    public string AtprotoPublicUrl { get; set; } = string.Empty;

    // Google SSO
    public bool GoogleSsoEnabled { get; set; }
    public string GoogleClientId { get; set; } = string.Empty;
    public string GoogleClientSecret { get; set; } = string.Empty;

    // Lock flags (for multi-tenant override control)
    public bool LockKeycloakEnabled { get; set; }
    public bool LockAtprotoLoginEnabled { get; set; }
    public bool LockGoogleSsoEnabled { get; set; }
}
