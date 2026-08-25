// ABOUTME: Request DTO for read-only Keycloak realm sync preview generation.
// ABOUTME: Temporary admin credentials are request-scoped and must never be persisted or returned.

namespace Explore.Application.DTOs.Onboarding;

public sealed record KeycloakRealmSyncPreviewRequestDto
{
    public bool UseTemporaryAdminCredentials { get; init; }
    public string? BootstrapAdminUsername { get; init; }
    public string? BootstrapAdminPassword { get; init; }
    public string? ApiClientId { get; init; } = "islamu-event-api";
    public IReadOnlyList<string> BlazorRedirectUris { get; init; } = [];
    public IReadOnlyList<string> BlazorWebOrigins { get; init; } = [];
}
