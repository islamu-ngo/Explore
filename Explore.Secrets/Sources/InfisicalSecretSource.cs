// ABOUTME: Resolves secrets via the Infisical Universal Auth API using the binding's environment/path/key metadata.
// ABOUTME: Returns null on missing secret or transient error — the resolver never falls through to another source.

using Explore.Application.Contracts.Secrets;
using Explore.Domain.Enums;
using Explore.Domain.Secrets;
using Microsoft.Extensions.Logging;

namespace Explore.Secrets.Sources;

/// <summary>
/// Retrieves secrets from Infisical via the <see cref="IInfisicalClientFactory"/>. When the factory
/// returns <c>null</c> (Infisical not configured) or the client returns <c>null</c> / throws, the source
/// surfaces <c>null</c> so the resolver can mark the resolution as a miss without leaking source errors.
/// </summary>
public sealed class InfisicalSecretSource : ISecretSource
{
    private readonly IInfisicalClientFactory _clientFactory;
    private readonly ILogger<InfisicalSecretSource> _logger;

    public InfisicalSecretSource(IInfisicalClientFactory clientFactory, ILogger<InfisicalSecretSource> logger)
    {
        ArgumentNullException.ThrowIfNull(clientFactory);
        ArgumentNullException.ThrowIfNull(logger);
        _clientFactory = clientFactory;
        _logger = logger;
    }

    /// <inheritdoc />
    public SecretSourceType SourceType => SecretSourceType.Infisical;

    /// <inheritdoc />
    public async Task<string?> GetSecretAsync(SecretBinding binding, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(binding);

        if (string.IsNullOrWhiteSpace(binding.InfisicalEnvironment)
            || string.IsNullOrWhiteSpace(binding.InfisicalPath)
            || string.IsNullOrWhiteSpace(binding.InfisicalKey))
        {
            _logger.LogWarning(
                "Infisical binding {BindingId} key {SettingKey} is missing required metadata (environment/path/key).",
                binding.Id,
                binding.SettingKey);
            return null;
        }

        var client = await _clientFactory.GetClientAsync(cancellationToken).ConfigureAwait(false);
        if (client is null)
        {
            _logger.LogWarning(
                "Infisical client is not configured; cannot resolve binding {BindingId} key {SettingKey}.",
                binding.Id,
                binding.SettingKey);
            return null;
        }

        try
        {
            return await client.GetSecretAsync(
                binding.InfisicalEnvironment,
                binding.InfisicalPath,
                binding.InfisicalKey,
                cancellationToken).ConfigureAwait(false);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            // Never let source errors bubble up - resolver must see a clean null for "miss"
            // and the details must stay in the log (never near the secret value).
            _logger.LogError(
                ex,
                "Infisical fetch failed for binding {BindingId} key {SettingKey} env={Env} path={Path}.",
                binding.Id,
                binding.SettingKey,
                binding.InfisicalEnvironment,
                binding.InfisicalPath);
            return null;
        }
    }

    /// <inheritdoc />
    public async Task<bool> ValidateAsync(SecretBinding binding, CancellationToken cancellationToken = default)
    {
        var value = await GetSecretAsync(binding, cancellationToken).ConfigureAwait(false);
        return !string.IsNullOrEmpty(value);
    }
}
