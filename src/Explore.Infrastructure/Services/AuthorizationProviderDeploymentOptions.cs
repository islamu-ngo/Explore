// ABOUTME: Validated deployment selector for the instance authorization provider.
// ABOUTME: Distinguishes explicit Local or Cerbos intent from unset manual onboarding.

namespace Explore.Infrastructure.Services;

public sealed class AuthorizationProviderDeploymentOptions
{
    public const string SectionName = "Authorization";
    public const string LocalProvider = "local";
    public const string CerbosProvider = "cerbos";

    public string? Provider { get; set; }

    /// <summary>
    /// Whether a sensitive action must be denied when the Cerbos policy revision behind the decision
    /// cannot be established. Defaults to <c>true</c>.
    /// <para>
    /// Turning this off is a deliberate availability-over-integrity trade and is only defensible for a
    /// deployment that manages the policy store entirely out of band (a read-only disk driver under
    /// GitOps, say), where the application never publishes and so can never observe a revision. In that
    /// posture leaving this on denies every sensitive action permanently, which helps nobody. Any
    /// deployment where the application publishes the package should leave it on: there, an unreadable
    /// revision means the store is genuinely unverified.
    /// </para>
    /// </summary>
    public bool DenySensitiveActionsOnUnknownRevision { get; set; } = true;

    public string? GetProvider()
    {
        var provider = Provider?.Trim().ToLowerInvariant();
        return provider switch
        {
            null or "" => null,
            LocalProvider or CerbosProvider => provider,
            _ => throw new InvalidOperationException(
                "Authorization:Provider must be blank, 'local', or 'cerbos'.")
        };
    }

    public static bool IsValid(AuthorizationProviderDeploymentOptions options)
    {
        var provider = options.Provider?.Trim();
        return string.IsNullOrWhiteSpace(provider)
            || provider.Equals(LocalProvider, StringComparison.OrdinalIgnoreCase)
            || provider.Equals(CerbosProvider, StringComparison.OrdinalIgnoreCase);
    }
}
