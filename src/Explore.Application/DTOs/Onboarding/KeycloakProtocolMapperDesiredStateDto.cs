// ABOUTME: Represents desired Keycloak protocol mapper requirements for future audience checks.
// ABOUTME: Included in sync plans so mapper intent is typed before mutation support exists.

namespace Explore.Application.DTOs.Onboarding;

public class KeycloakProtocolMapperDesiredStateDto
{
    public string Name { get; set; } = string.Empty;
    public string MapperType { get; set; } = string.Empty;
    public string? IncludedClientAudience { get; set; }
    public bool AddToAccessToken { get; set; } = true;
    public bool AddToIdToken { get; set; }
}
