// ABOUTME: Captures required Keycloak client-scope mappings for realm sync planning.
// ABOUTME: Keeps offline_access requirements explicit before any additive repair is attempted.

namespace Explore.Application.DTOs.Onboarding;

public sealed record KeycloakClientScopeDesiredStateDto
{
    public string Name { get; init; } = string.Empty;
    public string Protocol { get; init; } = "openid-connect";
    public IReadOnlyList<string> RealmRoleMappings { get; set; } = [];
}
