// ABOUTME: Defines supported setup-time Keycloak bootstrap modes for external identity providers.
// ABOUTME: Keeps bootstrap intent strongly typed before infrastructure performs Keycloak Admin API calls.

namespace Explore.Application.Onboarding;

public enum KeycloakBootstrapMode
{
    PatchExistingRealm = 0,
    CreateRealm = 1
}
