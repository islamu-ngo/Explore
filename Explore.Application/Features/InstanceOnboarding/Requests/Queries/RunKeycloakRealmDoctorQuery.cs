// ABOUTME: Query contract for read-only Keycloak realm compatibility diagnostics.
// ABOUTME: Keeps post-onboarding provider inspection behind Application service abstractions.

using Explore.Application.DTOs.Onboarding;
using MediatR;

namespace Explore.Application.Features.InstanceOnboarding.Requests.Queries;

public class RunKeycloakRealmDoctorQuery : IRequest<KeycloakRealmDoctorResultDto>
{
    public KeycloakRealmDoctorRequestDto Request { get; set; } = new();
}
