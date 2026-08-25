// ABOUTME: MediatR command for setup-time external Keycloak realm bootstrap.
// ABOUTME: Carries one-time admin credentials only through the Application workflow and never persists them.

using Explore.Application.DTOs.Onboarding;
using Explore.Application.Responses;
using MediatR;

namespace Explore.Application.Features.InstanceOnboarding.Requests.Commands;

public sealed record BootstrapKeycloakRealmCommand : IRequest<BaseCommandResponse<Guid>>
{
    public required KeycloakBootstrapRequestDto BootstrapRequest { get; init; } = new();
}
