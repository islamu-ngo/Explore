// ABOUTME: Options controlling setup-time Keycloak bootstrap safety behavior.
// ABOUTME: Keeps local/test URL allowances explicit instead of weakening production SSRF defaults.

namespace Explore.Infrastructure.Services.Keycloak;

public sealed class KeycloakBootstrapOptions
{
    public const string SectionName = "KeycloakBootstrap";

    public bool AllowLocalUrls { get; set; }
}
