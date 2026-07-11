// ABOUTME: Request contract for backup-confirmed Keycloak realm sync apply operations.
// ABOUTME: Carries temporary admin credentials for one request without persisting them.

namespace Explore.Application.DTOs.Onboarding;

public class KeycloakRealmSyncApplyRequestDto
{
    public bool BackupConfirmed { get; set; }

    public string? BootstrapAdminUsername { get; set; }

    public string? BootstrapAdminPassword { get; set; }

    public string? ApiClientId { get; set; } = "islamu-event-api";

    public IReadOnlyList<string> BlazorRedirectUris { get; set; } = [];

    public IReadOnlyList<string> BlazorWebOrigins { get; set; } = [];
}
