// ABOUTME: BFF-local authentication scheme names shared by the host's login and refresh flows.
// ABOUTME: BFF cannot reference Domain directly; these must stay in sync with the Domain version.

namespace Explore.Blazor.Constants;

public static class AuthSchemeNames
{
    /// <summary>
    /// Keycloak OIDC authentication scheme.
    /// </summary>
    public const string Keycloak = "Keycloak";

    /// <summary>
    /// Google OAuth 2.0 authentication scheme.
    /// </summary>
    public const string Google = "Google";

    /// <summary>
    /// AT Protocol DID-based authentication scheme.
    /// </summary>
    public const string Atproto = "Atproto";

    /// <summary>
    /// All known provider scheme names. Used for iteration and validation.
    /// </summary>
    public static readonly IReadOnlyList<string> AllProviders = [Keycloak, Google, Atproto];
}
