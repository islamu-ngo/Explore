// ABOUTME: Defines the external identity provider bound to configured-administrator bootstrap.
// ABOUTME: Stable numeric values distinguish Keycloak and AT Protocol authority evidence.

namespace Explore.Domain.Enums;

public enum InstanceBootstrapProviderKind
{
    Keycloak = 1,
    Atproto = 2
}
