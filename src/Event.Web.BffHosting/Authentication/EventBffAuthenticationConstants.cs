// ABOUTME: Defines shared browser-BFF authentication property names.
// ABOUTME: Keeps OIDC scheme and token-refresh metadata stable across BFF hosts.

namespace Event.Web.BffHosting.Authentication;

public static class EventBffAuthenticationConstants
{
    public const string OidcSchemePropertyKey = "oidc_scheme";
    public static readonly object TokenRefreshRejectedItemKey = new();
}
