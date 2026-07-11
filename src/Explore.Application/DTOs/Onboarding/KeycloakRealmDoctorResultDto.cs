// ABOUTME: Safe aggregate result for read-only Keycloak realm compatibility diagnostics.
// ABOUTME: Reports high-level realm health without exposing secrets, tokens, or raw provider payloads.

namespace Explore.Application.DTOs.Onboarding;

public class KeycloakRealmDoctorResultDto
{
    public string OverallStatus { get; set; } = "blocked";
    public string Message { get; set; } = string.Empty;
    public string Realm { get; set; } = string.Empty;
    public string Authority { get; set; } = string.Empty;
    public string ClientId { get; set; } = string.Empty;
    public string? ApiClientId { get; set; }
    public IReadOnlyList<KeycloakRealmDoctorCheckDto> Checks { get; set; } = [];
}
