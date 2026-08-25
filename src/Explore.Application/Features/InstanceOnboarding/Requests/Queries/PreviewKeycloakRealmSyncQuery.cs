// ABOUTME: MediatR query for generating a read-only Keycloak realm sync preview.
// ABOUTME: Lets instance settings endpoints request typed additive drift plans without Infrastructure details.

using Explore.Application.DTOs.Onboarding;
using MediatR;

namespace Explore.Application.Features.InstanceOnboarding.Requests.Queries;

public sealed record PreviewKeycloakRealmSyncQuery : IRequest<KeycloakRealmSyncPlanDto>
{
    public KeycloakRealmSyncPreviewRequestDto Request { get; init; } = new();
}
