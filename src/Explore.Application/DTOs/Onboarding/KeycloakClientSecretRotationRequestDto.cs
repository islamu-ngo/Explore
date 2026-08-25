// ABOUTME: Request DTO for explicit Keycloak confidential-client secret rotation.
// ABOUTME: Keeps new client secrets and temporary admin credentials request-scoped and out of persisted responses.

namespace Explore.Application.DTOs.Onboarding;

public sealed record KeycloakClientSecretRotationRequestDto
{
    public string? ClientId { get; init; }
    public string SecretOwnershipMode { get; set; } = "application-managed";
    public bool ConfirmApplicationManagedSecret { get; init; }
    public string? NewClientSecret { get; init; }
    public string? BootstrapAdminUsername { get; init; }
    public string? BootstrapAdminPassword { get; init; }
}
