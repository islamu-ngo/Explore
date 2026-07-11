// ABOUTME: Request DTO for explicit Keycloak confidential-client secret rotation.
// ABOUTME: Keeps new client secrets and temporary admin credentials request-scoped and out of persisted responses.

namespace Explore.Application.DTOs.Onboarding;

public class KeycloakClientSecretRotationRequestDto
{
    public string? ClientId { get; set; }
    public string SecretOwnershipMode { get; set; } = "application-managed";
    public bool ConfirmApplicationManagedSecret { get; set; }
    public string? NewClientSecret { get; set; }
    public string? BootstrapAdminUsername { get; set; }
    public string? BootstrapAdminPassword { get; set; }
}
