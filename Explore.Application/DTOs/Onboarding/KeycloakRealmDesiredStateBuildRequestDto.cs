// ABOUTME: Input model for composing Keycloak realm desired state from project contracts.
// ABOUTME: Keeps module contributors independent from Infrastructure Admin API details.

namespace Explore.Application.DTOs.Onboarding;

public class KeycloakRealmDesiredStateBuildRequestDto
{
    public string Realm { get; set; } = string.Empty;
    public string BlazorClientId { get; set; } = string.Empty;
    public string? ApiClientId { get; set; }
    public IReadOnlyList<string> BlazorRedirectUris { get; set; } = [];
    public IReadOnlyList<string> BlazorWebOrigins { get; set; } = [];
}
