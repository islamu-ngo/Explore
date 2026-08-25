// ABOUTME: Request model for read-only Keycloak realm diagnostics from instance administration.
// ABOUTME: Temporary admin credentials are accepted only for inspection and must never be persisted.

namespace Explore.Application.DTOs.Onboarding;

public sealed record KeycloakRealmDoctorRequestDto
{
    public bool UseTemporaryAdminCredentials { get; init; }
    public string? BootstrapAdminUsername { get; init; }
    public string? BootstrapAdminPassword { get; init; }
    public string? ApiClientId { get; init; } = "islamu-event-api";
}
