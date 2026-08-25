// ABOUTME: Represents desired Keycloak protocol mapper requirements for future audience checks.
// ABOUTME: Included in sync plans so mapper intent is typed before mutation support exists.

namespace Explore.Application.DTOs.Onboarding;

public sealed record KeycloakProtocolMapperDesiredStateDto
{
    public string Name { get; init; } = string.Empty;
    public string MapperType { get; init; } = string.Empty;
    public string? IncludedClientAudience { get; init; }
    public bool AddToAccessToken { get; init; } = true;
    public bool AddToIdToken { get; init; }
}
