// ABOUTME: Closed enumeration of supported secret authority types.
// ABOUTME: Only Environment and Infisical are implemented in this greenfield contract.

namespace Explore.Secrets.Abstractions;

/// <summary>
/// Supported secret manager provider types.
/// </summary>
public enum SecretProviderType
{
    Unspecified = -1,

    /// <summary>
    /// No external secret manager - uses environment variables only.
    /// Suitable for self-hosters and local development.
    /// </summary>
    Environment = 0,

    /// <summary>
    /// Infisical secret manager with Universal Auth.
    /// </summary>
    Infisical = 1
}
