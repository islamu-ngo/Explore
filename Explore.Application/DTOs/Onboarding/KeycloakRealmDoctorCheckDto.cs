// ABOUTME: Safe per-check result for read-only Keycloak realm compatibility diagnostics.
// ABOUTME: Carries operator-facing status and remediation without provider secrets or raw bodies.

namespace Explore.Application.DTOs.Onboarding;

public class KeycloakRealmDoctorCheckDto
{
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public string? Remediation { get; set; }
}
