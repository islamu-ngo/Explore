// ABOUTME: Resolves secrets from process environment variables via SecretBinding.EnvironmentVariableName.
// ABOUTME: Returns bounded typed outcomes suitable for bootstrap and local-development workflows.

using Explore.Application.Contracts.Secrets;
using Explore.Domain.Enums;
using Explore.Domain.Secrets;

namespace Explore.Secrets.Sources;

/// <summary>
/// Reads secret values straight from <see cref="Environment.GetEnvironmentVariable(string)"/> using the
/// binding's <see cref="SecretBinding.EnvironmentVariableName"/>. Trivial, always-available source that
/// underpins bootstrap and local-dev workflows — the registry forbids this source for secrets where the
/// value must never hit a shell history / process environment.
/// </summary>
public sealed class EnvironmentSecretSource : ISecretSource
{
    /// <inheritdoc />
    public SecretSourceType SourceType => SecretSourceType.EnvironmentVariable;

    /// <inheritdoc />
    public Task<SecretResolutionResult> GetSecretAsync(
        SecretBinding binding,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(binding);

        if (string.IsNullOrWhiteSpace(binding.EnvironmentVariableName))
        {
            return Task.FromResult(SecretResolutionResult.Invalid);
        }

        var value = Environment.GetEnvironmentVariable(binding.EnvironmentVariableName);
        if (string.IsNullOrEmpty(value))
        {
            return Task.FromResult(SecretResolutionResult.Unconfigured);
        }

        return Task.FromResult(SecretResolutionResult.Resolved(new ResolvedSecret(
            binding.SettingKey,
            value,
            binding.SourceType,
            binding.Scope,
            binding.ScopeId,
            DateTime.UtcNow)));
    }

    /// <inheritdoc />
    public async Task<bool> ValidateAsync(SecretBinding binding, CancellationToken cancellationToken = default)
    {
        var result = await GetSecretAsync(binding, cancellationToken).ConfigureAwait(false);
        return result.IsResolved;
    }
}
