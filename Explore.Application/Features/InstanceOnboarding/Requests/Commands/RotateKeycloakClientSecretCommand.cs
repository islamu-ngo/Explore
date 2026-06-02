// ABOUTME: Command contract for post-onboarding Keycloak client-secret rotation.
// ABOUTME: Carries current user identity for instance-admin authorization and audit-safe logging.

using Explore.Application.DTOs.Onboarding;
using MediatR;

namespace Explore.Application.Features.InstanceOnboarding.Requests.Commands;

public class RotateKeycloakClientSecretCommand : IRequest<KeycloakClientSecretRotationResultDto>
{
    public Guid UserId { get; set; }
    public KeycloakClientSecretRotationRequestDto Request { get; set; } = new();
}
