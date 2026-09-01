// ABOUTME: Defines stable authentication scheme names for Event browser-BFF hosts.
// ABOUTME: Keeps cookie and Keycloak OIDC scheme usage consistent across BFF composition roots.

namespace Event.Web.BffHosting.Authentication;

public static class EventBffAuthenticationSchemes
{
    public const string Keycloak = "Keycloak";
    public const string Google = "Google";
    public const string Atproto = "Atproto";
}
