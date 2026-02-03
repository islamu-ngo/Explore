// ABOUTME: Secret provider that reads from environment variables.
// Fallback provider for self-hosters and local development.

using Explore.Secrets.Abstractions;
using Microsoft.Extensions.Logging;

namespace Explore.Secrets.Providers;

/// <summary>
/// Secret provider that reads from environment variables.
/// Maps canonical keys (e.g., "Database:ConnectionString") to environment variable format
/// (e.g., "DATABASE__CONNECTIONSTRING").
/// </summary>
public sealed class EnvironmentSecretProvider : ISecretProvider
{
    private readonly ILogger<EnvironmentSecretProvider> _logger;
    private bool _initialized;

    public EnvironmentSecretProvider(ILogger<EnvironmentSecretProvider> logger)
    {
        _logger = logger;
    }

    /// <inheritdoc />
    public SecretProviderType ProviderType => SecretProviderType.None;

    /// <inheritdoc />
    public bool SupportsRefresh => false;

    /// <inheritdoc />
    public Task InitializeAsync(CancellationToken cancellationToken = default)
    {
        _initialized = true;
        _logger.LogInformation("Environment secret provider initialized (no external secret manager)");
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public Task<string?> GetSecretAsync(string key, CancellationToken cancellationToken = default)
    {
        EnsureInitialized();

        var envVarName = ConvertKeyToEnvVar(key);
        var value = Environment.GetEnvironmentVariable(envVarName);

        if (value is null)
        {
            // Try alternative formats
            value = Environment.GetEnvironmentVariable(key.Replace(":", "__"));
            if (value is null)
            {
                value = Environment.GetEnvironmentVariable(key.Replace(":", "_"));
            }
        }

        if (value is not null)
        {
            _logger.LogDebug("Retrieved secret from environment variable: {Key}", RedactKey(key));
        }

        return Task.FromResult(value);
    }

    /// <inheritdoc />
    public Task<SecretValue?> GetSecretWithMetadataAsync(string key, CancellationToken cancellationToken = default)
    {
        var value = GetSecretAsync(key, cancellationToken).GetAwaiter().GetResult();
        return Task.FromResult(value is not null ? new SecretValue(value) : null);
    }

    /// <inheritdoc />
    public Task<IReadOnlyDictionary<string, string>> GetSecretsByPathAsync(
        string pathPrefix,
        CancellationToken cancellationToken = default)
    {
        EnsureInitialized();

        var envVarPrefix = ConvertKeyToEnvVar(pathPrefix);
        var results = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        foreach (var envVar in Environment.GetEnvironmentVariables().Keys.Cast<string>())
        {
            if (envVar.StartsWith(envVarPrefix, StringComparison.OrdinalIgnoreCase))
            {
                var value = Environment.GetEnvironmentVariable(envVar);
                if (!string.IsNullOrEmpty(value))
                {
                    var key = ConvertEnvVarToKey(envVar);
                    results[key] = value;
                }
            }
        }

        _logger.LogDebug(
            "Retrieved {Count} secrets from environment with prefix: {Prefix}",
            results.Count,
            RedactKey(pathPrefix));

        return Task.FromResult<IReadOnlyDictionary<string, string>>(results);
    }

    /// <inheritdoc />
    public Task RefreshAsync(CancellationToken cancellationToken = default)
    {
        // Environment variables don't support refresh
        _logger.LogDebug("Refresh requested but environment variables don't support refresh");
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public Task<ProviderHealthInfo> GetHealthAsync(CancellationToken cancellationToken = default)
    {
        // Environment provider is always healthy if initialized
        return Task.FromResult(new ProviderHealthInfo(
            IsHealthy: _initialized,
            ProviderType: SecretProviderType.None,
            LastSuccessfulRefresh: null, // N/A for env provider
            ConsecutiveFailures: 0));
    }

    /// <summary>
    /// Converts a canonical key to environment variable format.
    /// "Database:ConnectionString" -> "DATABASE__CONNECTIONSTRING"
    /// </summary>
    private static string ConvertKeyToEnvVar(string key)
    {
        return key
            .Replace(":", "__")
            .Replace(".", "__")
            .ToUpperInvariant();
    }

    /// <summary>
    /// Converts an environment variable name back to canonical key format.
    /// "DATABASE__CONNECTIONSTRING" -> "Database:ConnectionString"
    /// </summary>
    private static string ConvertEnvVarToKey(string envVar)
    {
        // Split by double underscore and capitalize each part
        var parts = envVar.Split("__", StringSplitOptions.RemoveEmptyEntries);
        var capitalizedParts = parts.Select(p =>
            char.ToUpperInvariant(p[0]) + p[1..].ToLowerInvariant());
        return string.Join(":", capitalizedParts);
    }

    /// <summary>
    /// Redacts a key for safe logging.
    /// "Database:ConnectionString" -> "Database:***"
    /// </summary>
    private static string RedactKey(string key)
    {
        var colonIndex = key.IndexOf(':');
        return colonIndex > 0 ? key[..(colonIndex + 1)] + "***" : key;
    }

    private void EnsureInitialized()
    {
        if (!_initialized)
        {
            throw new InvalidOperationException(
                "Environment secret provider not initialized. Call InitializeAsync first.");
        }
    }
}
