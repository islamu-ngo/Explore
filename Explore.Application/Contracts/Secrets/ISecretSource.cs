// ABOUTME: Per-source retrieval contract. Each ISecretSource implementation handles exactly one SecretSourceType.
// ABOUTME: Implementations return null when the secret is not found at the source (never throw for missing).

using Explore.Domain.Enums;
using Explore.Domain.Secrets;

namespace Explore.Application.Contracts.Secrets;

/// <summary>
/// Retrieves secret values from a single declared source (Infisical, environment variable, or inline
/// ciphertext). The resolver selects one <see cref="ISecretSource"/> by <see cref="SourceType"/>
/// matching the binding's declared source — there is no source-level fallback chain.
/// </summary>
public interface ISecretSource
{
    /// <summary>Which <see cref="SecretSourceType"/> this source handles. Exactly one source per type.</summary>
    SecretSourceType SourceType { get; }

    /// <summary>
    /// Retrieves the plaintext secret described by <paramref name="binding"/>. Returns <c>null</c> when the
    /// source has no value for the reference (e.g. missing env var, empty Infisical response). Must not
    /// throw for missing data; transient errors should be logged and return <c>null</c> as well.
    /// </summary>
    Task<string?> GetSecretAsync(SecretBinding binding, CancellationToken cancellationToken = default);

    /// <summary>
    /// Performs a live round-trip against the declared source to confirm the reference is valid and the
    /// credential produces a non-empty value. Used by the admin "Validate" action.
    /// </summary>
    Task<bool> ValidateAsync(SecretBinding binding, CancellationToken cancellationToken = default);
}
