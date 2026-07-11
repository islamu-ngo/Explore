// ABOUTME: Request DTO for read-only Keycloak realm sync preview generation.
// ABOUTME: Temporary admin credentials are request-scoped and must never be persisted or returned.

namespace Explore.Application.DTOs.Onboarding;

public class KeycloakRealmSyncPreviewRequestDto
{
    public bool UseTemporaryAdminCredentials { get; set; }
    public string? BootstrapAdminUsername { get; set; }
    public string? BootstrapAdminPassword { get; set; }
    public string? ApiClientId { get; set; } = "islamu-event-api";
    public IReadOnlyList<string> BlazorRedirectUris { get; set; } = [];
    public IReadOnlyList<string> BlazorWebOrigins { get; set; } = [];
}
