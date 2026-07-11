// ABOUTME: Request model for read-only Keycloak realm diagnostics from instance administration.
// ABOUTME: Temporary admin credentials are accepted only for inspection and must never be persisted.

namespace Explore.Application.DTOs.Onboarding;

public class KeycloakRealmDoctorRequestDto
{
    public bool UseTemporaryAdminCredentials { get; set; }
    public string? BootstrapAdminUsername { get; set; }
    public string? BootstrapAdminPassword { get; set; }
    public string? ApiClientId { get; set; } = "islamu-event-api";
}
