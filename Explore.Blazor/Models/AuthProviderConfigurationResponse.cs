// ABOUTME: BFF-local model for deserializing auth provider configuration from the API.
// ABOUTME: Mirrors the Application layer's AuthProviderConfigurationDto without a project reference.

using System.Text.Json.Serialization;

namespace Explore.Blazor.Models;

/// <summary>
/// Deserialization model for the auth provider configuration API response.
/// Used by <see cref="Services.DynamicAuthSchemeManager"/> to configure OIDC/OAuth schemes.
/// </summary>
public sealed class AuthProviderConfigurationResponse
{
    // Keycloak
    [JsonPropertyName("keycloakEnabled")]
    public bool KeycloakEnabled { get; set; }

    [JsonPropertyName("keycloakAuthority")]
    public string KeycloakAuthority { get; set; } = string.Empty;

    [JsonPropertyName("keycloakClientId")]
    public string KeycloakClientId { get; set; } = string.Empty;

    [JsonPropertyName("keycloakClientSecret")]
    public string KeycloakClientSecret { get; set; } = string.Empty;

    // ATProto Login
    [JsonPropertyName("atprotoLoginEnabled")]
    public bool AtprotoLoginEnabled { get; set; }

    [JsonPropertyName("atprotoPublicUrl")]
    public string AtprotoPublicUrl { get; set; } = string.Empty;

    // Google SSO
    [JsonPropertyName("googleSsoEnabled")]
    public bool GoogleSsoEnabled { get; set; }

    [JsonPropertyName("googleClientId")]
    public string GoogleClientId { get; set; } = string.Empty;

    [JsonPropertyName("googleClientSecret")]
    public string GoogleClientSecret { get; set; } = string.Empty;
}
