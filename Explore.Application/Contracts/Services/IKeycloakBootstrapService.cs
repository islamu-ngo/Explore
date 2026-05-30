// ABOUTME: Application-layer contract for setup-time Keycloak bootstrap orchestration.
// ABOUTME: Infrastructure implements Keycloak Admin API details while handlers stay HTTP-provider agnostic.

using Explore.Application.DTOs.Onboarding;

namespace Explore.Application.Contracts.Services;

public interface IKeycloakBootstrapService
{
    Task<KeycloakBootstrapResultDto> BootstrapAsync(
        KeycloakBootstrapRequestDto request,
        CancellationToken cancellationToken);
}
