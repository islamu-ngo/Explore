// ABOUTME: Captures required Keycloak client-scope mappings for realm sync planning.
// ABOUTME: Keeps offline_access requirements explicit before any additive repair is attempted.

namespace Explore.Application.DTOs.Onboarding;

public class KeycloakClientScopeDesiredStateDto
{
    public string Name { get; set; } = string.Empty;
    public string Protocol { get; set; } = "openid-connect";
    public IReadOnlyList<string> RealmRoleMappings { get; set; } = [];
}
