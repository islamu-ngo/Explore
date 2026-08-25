// ABOUTME: Represents required Keycloak realm-role composite relationships for sync planning.
// ABOUTME: Used to model offline_access default-role requirements without mutating Keycloak.

namespace Explore.Application.DTOs.Onboarding;

public sealed record KeycloakRoleCompositeDesiredStateDto
{
    public string RoleName { get; init; } = string.Empty;
    public IReadOnlyList<string> CompositeRoleNames { get; set; } = [];
}
