// ABOUTME: Builds typed Keycloak realm desired state from registered project contributors.
// ABOUTME: Provides a single composition point for doctor, preview, apply, and drift detection flows.

using Explore.Application.DTOs.Onboarding;

namespace Explore.Application.Contracts.Services;

public interface IKeycloakRealmDesiredStateBuilder
{
    KeycloakRealmDesiredStateDto Build(KeycloakRealmDesiredStateBuildRequestDto request);
}
