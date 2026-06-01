// ABOUTME: Typed desired-state contract for ISLAMU-owned Keycloak realm requirements.
// ABOUTME: Supports additive drift planning while explicitly excluding destructive operations.

namespace Explore.Application.DTOs.Onboarding;

public class KeycloakRealmDesiredStateDto
{
    public string Realm { get; set; } = string.Empty;
    public string BlazorClientId { get; set; } = string.Empty;
    public string? ApiClientId { get; set; }
    public bool DestructiveOperationsSupported { get; set; }
    public IReadOnlyList<string> RequiredRealmRoles { get; set; } = [];
    public IReadOnlyList<KeycloakRoleCompositeDesiredStateDto> RoleComposites { get; set; } = [];
    public IReadOnlyList<KeycloakClientScopeDesiredStateDto> ClientScopes { get; set; } = [];
    public IReadOnlyList<KeycloakClientDesiredStateDto> Clients { get; set; } = [];
}
