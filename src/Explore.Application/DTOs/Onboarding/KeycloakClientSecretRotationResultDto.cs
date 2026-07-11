// ABOUTME: Safe result contract for Keycloak client-secret rotation attempts.
// ABOUTME: Reports ownership/action status without returning secret values, tokens, or raw provider responses.

namespace Explore.Application.DTOs.Onboarding;

public class KeycloakClientSecretRotationResultDto
{
    public string Status { get; set; } = "blocked";
    public string Message { get; set; } = "Keycloak client secret rotation is blocked.";
    public string ClientId { get; set; } = string.Empty;
    public string SecretOwnershipMode { get; set; } = "application-managed";
    public bool AuthSchemesReloaded { get; set; }
    public bool RequiresRestart { get; set; }
    public string OperatorInstructions { get; set; } = string.Empty;
    public IReadOnlyList<KeycloakRealmSyncOperationDto> Operations { get; set; } = [];
}
