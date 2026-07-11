// ABOUTME: Safe read-only Keycloak realm sync preview returned to instance administrators.
// ABOUTME: Combines desired state, diagnostics, and additive operation plans without applying changes.

namespace Explore.Application.DTOs.Onboarding;

public class KeycloakRealmSyncPlanDto
{
    public string Status { get; set; } = "blocked";
    public string Message { get; set; } = string.Empty;
    public string Realm { get; set; } = string.Empty;
    public string Authority { get; set; } = string.Empty;
    public string ClientId { get; set; } = string.Empty;
    public string? ApiClientId { get; set; }
    public bool DestructiveOperationsSupported { get; set; }
    public bool RequiresBackupBeforeApply { get; set; }
    public KeycloakRealmDesiredStateDto DesiredState { get; set; } = new();
    public IReadOnlyList<KeycloakRealmSyncOperationDto> Operations { get; set; } = [];
    public IReadOnlyList<KeycloakRealmDoctorCheckDto> Diagnostics { get; set; } = [];
}
