// ABOUTME: Defines stable lookup identifiers for every supported authentication provider authority.
// ABOUTME: Keeps provider codes at protocol boundaries while persistence uses normalized integer keys.

namespace Explore.Domain.Enums;

public enum AuthenticationProviderKind
{
    Keycloak = 1,
    Atproto = 2,
    Google = 3,
    Local = 4,
    Development = 5,
}
