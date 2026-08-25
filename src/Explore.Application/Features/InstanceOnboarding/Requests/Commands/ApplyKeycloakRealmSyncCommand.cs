// ABOUTME: MediatR command for applying backup-confirmed additive Keycloak realm repairs.
// ABOUTME: Keeps mutation intent explicit for post-onboarding instance-admin operations.

using Explore.Application.DTOs.Onboarding;
using MediatR;

namespace Explore.Application.Features.InstanceOnboarding.Requests.Commands;

public sealed record ApplyKeycloakRealmSyncCommand : IRequest<KeycloakRealmSyncPlanDto>
{
    public KeycloakRealmSyncApplyRequestDto Request { get; init; } = new();
}
