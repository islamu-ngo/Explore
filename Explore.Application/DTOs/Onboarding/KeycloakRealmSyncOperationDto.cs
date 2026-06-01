// ABOUTME: Describes one read-only Keycloak drift finding or future additive repair action.
// ABOUTME: Never contains provider secrets, tokens, or raw Keycloak response payloads.

namespace Explore.Application.DTOs.Onboarding;

public class KeycloakRealmSyncOperationDto
{
    public string OperationId { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public string TargetType { get; set; } = string.Empty;
    public string Target { get; set; } = string.Empty;
    public string Action { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public string Summary { get; set; } = string.Empty;
    public string Reason { get; set; } = string.Empty;
    public IReadOnlyList<string> Changes { get; set; } = [];
    public bool RequiresBackupBeforeApply { get; set; }
}
