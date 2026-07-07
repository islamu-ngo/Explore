// ABOUTME: Identifies the account authority responsible for credential-token lifecycle emails.
// ABOUTME: Keeps Keycloak, PDS, and future local identity ownership explicit in notification routing.

namespace Explore.Application.Notifications;

public enum AccountAuthorityKind
{
    None = 0,
    Keycloak = 1,
    AtprotoPds = 2,
    IslamuOperatedPds = 3,
    LocalIdentity = 4,
    ExternalOidc = 5
}
