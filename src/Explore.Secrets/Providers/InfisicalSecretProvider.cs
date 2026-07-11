// ABOUTME: Secret provider that retrieves secrets from Infisical.
// Uses Universal Auth for machine identity authentication.

using System.Collections.Concurrent;
using Explore.Secrets.Abstractions;
using Explore.Secrets.Configuration;
using Infisical.Sdk;
using Infisical.Sdk.Model;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Explore.Secrets.Providers;

/// <summary>
/// Secret provider that retrieves secrets from Infisical using Universal Auth.
/// Caches secrets locally and supports periodic refresh.
/// </summary>
public sealed class InfisicalSecretProvider : ISecretProvider, IAsyncDisposable
{
    private readonly ILogger<InfisicalSecretProvider> _logger;
    private readonly InfisicalOptions _options;
    private readonly ConcurrentDictionary<string, SecretValue> _secretCache = new(StringComparer.OrdinalIgnoreCase);

    private InfisicalClient? _client;
    private bool _initialized;
    private DateTime? _lastSuccessfulRefresh;
    private int _consecutiveFailures;
    private readonly SemaphoreSlim _initLock = new(1, 1);
    private readonly SemaphoreSlim _refreshLock = new(1, 1);

    public InfisicalSecretProvider(
        ILogger<InfisicalSecretProvider> logger,
        IOptions<SecretProviderOptions> options)
    {
        _logger = logger;
        _options = options.Value.Infisical;
    }

    /// <inheritdoc />
    public SecretProviderType ProviderType => SecretProviderType.Infisical;

    /// <inheritdoc />
    public bool SupportsRefresh => true;

    /// <inheritdoc />
    public async Task InitializeAsync(CancellationToken cancellationToken = default)
    {
        await _initLock.WaitAsync(cancellationToken);
        try
        {
            if (_initialized)
            {
                _logger.LogDebug("Infisical provider already initialized");
                return;
            }

            ValidateConfiguration();

            _logger.LogInformation(
                "Initializing Infisical secret provider for project {ProjectId} in {Environment}",
                _options.ProjectId,
                _options.Environment);

            var settings = new InfisicalSdkSettingsBuilder()
                .WithHostUri(_options.Url ?? "https://app.infisical.com")
                .Build();

            _client = new InfisicalClient(settings);

            // Authenticate using Universal Auth
            await _client.Auth().UniversalAuth().LoginAsync(
                _options.ClientId!,
                _options.ClientSecret!);

            _logger.LogDebug("Infisical authentication successful");

            // Load initial secrets
            await LoadSecretsAsync(cancellationToken);

            _initialized = true;
            _lastSuccessfulRefresh = DateTime.UtcNow;
            _consecutiveFailures = 0;

            _logger.LogInformation(
                "Infisical secret provider initialized with {Count} secrets from {PathCount} paths",
                _secretCache.Count,
                _options.Paths.Count);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _consecutiveFailures++;
            _logger.LogError(ex, "Failed to initialize Infisical secret provider");
            throw SecretProviderException.Permanent(
                $"Failed to initialize Infisical provider: {ex.Message}",
                SecretProviderType.Infisical,
                "Initialize",
                ex);
        }
        finally
        {
            _initLock.Release();
        }
    }

    /// <inheritdoc />
    public Task<string?> GetSecretAsync(string key, CancellationToken cancellationToken = default)
    {
        EnsureInitialized();

        if (_secretCache.TryGetValue(key, out var secret))
        {
            _logger.LogDebug("Retrieved secret from cache: {Key}", RedactKey(key));
            return Task.FromResult<string?>(secret.Value);
        }

        // Try alternate key formats
        var infisicalKey = ConvertToInfisicalKey(key);
        if (_secretCache.TryGetValue(infisicalKey, out secret))
        {
            _logger.LogDebug("Retrieved secret from cache using Infisical key: {Key}", RedactKey(key));
            return Task.FromResult<string?>(secret.Value);
        }

        _logger.LogDebug("Secret not found in cache: {Key}", RedactKey(key));
        return Task.FromResult<string?>(null);
    }

    /// <inheritdoc />
    public Task<SecretValue?> GetSecretWithMetadataAsync(string key, CancellationToken cancellationToken = default)
    {
        EnsureInitialized();

        if (_secretCache.TryGetValue(key, out var secret))
        {
            return Task.FromResult<SecretValue?>(secret);
        }

        var infisicalKey = ConvertToInfisicalKey(key);
        if (_secretCache.TryGetValue(infisicalKey, out secret))
        {
            return Task.FromResult<SecretValue?>(secret);
        }

        return Task.FromResult<SecretValue?>(null);
    }

    /// <inheritdoc />
    public Task<IReadOnlyDictionary<string, string>> GetSecretsByPathAsync(
        string pathPrefix,
        CancellationToken cancellationToken = default)
    {
        EnsureInitialized();

        var results = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        var normalizedPrefix = NormalizePath(pathPrefix);

        foreach (var (key, secret) in _secretCache)
        {
            // Check if the key starts with the path prefix
            if (key.StartsWith(normalizedPrefix, StringComparison.OrdinalIgnoreCase) ||
                key.StartsWith(pathPrefix, StringComparison.OrdinalIgnoreCase))
            {
                results[key] = secret.Value;
            }
        }

        _logger.LogDebug(
            "Retrieved {Count} secrets with prefix: {Prefix}",
            results.Count,
            RedactKey(pathPrefix));

        return Task.FromResult<IReadOnlyDictionary<string, string>>(results);
    }

    /// <inheritdoc />
    public async Task RefreshAsync(CancellationToken cancellationToken = default)
    {
        EnsureInitialized();

        await _refreshLock.WaitAsync(cancellationToken);
        try
        {
            _logger.LogDebug("Refreshing secrets from Infisical");

            var previousCount = _secretCache.Count;
            await LoadSecretsAsync(cancellationToken);

            _lastSuccessfulRefresh = DateTime.UtcNow;
            _consecutiveFailures = 0;

            _logger.LogInformation(
                "Refreshed {Count} secrets from Infisical (previous: {Previous})",
                _secretCache.Count,
                previousCount);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _consecutiveFailures++;
            _logger.LogWarning(
                ex,
                "Failed to refresh secrets from Infisical (consecutive failures: {Failures})",
                _consecutiveFailures);

            throw SecretProviderException.Transient(
                $"Failed to refresh secrets: {ex.Message}",
                SecretProviderType.Infisical,
                "Refresh",
                ex);
        }
        finally
        {
            _refreshLock.Release();
        }
    }

    /// <inheritdoc />
    public Task<ProviderHealthInfo> GetHealthAsync(CancellationToken cancellationToken = default)
    {
        return Task.FromResult(new ProviderHealthInfo(
            IsHealthy: _initialized && _consecutiveFailures < 3,
            ProviderType: SecretProviderType.Infisical,
            LastSuccessfulRefresh: _lastSuccessfulRefresh,
            ConsecutiveFailures: _consecutiveFailures));
    }

    /// <summary>
    /// Loads secrets from all configured paths into the cache.
    /// </summary>
    private async Task LoadSecretsAsync(CancellationToken cancellationToken)
    {
        if (_client is null)
        {
            throw new InvalidOperationException("Infisical client not initialized");
        }

        var newSecrets = new Dictionary<string, SecretValue>(StringComparer.OrdinalIgnoreCase);

        foreach (var path in _options.Paths)
        {
            cancellationToken.ThrowIfCancellationRequested();

            try
            {
                var options = new ListSecretsOptions
                {
                    ProjectId = _options.ProjectId!,
                    EnvironmentSlug = _options.Environment,
                    SecretPath = path,
                    Recursive = true,
                    ExpandSecretReferences = true,
                    ViewSecretValue = true
                };

                var secrets = await _client.Secrets().ListAsync(options);

                if (secrets is null)
                {
                    _logger.LogWarning("No secrets returned from Infisical for path: {Path}", path);
                    continue;
                }

                foreach (var secret in secrets)
                {
                    // Store with both canonical and Infisical key formats
                    var canonicalKey = ConvertToCanonicalKey(secret.SecretKey, path);
                    var secretValue = new SecretValue(
                        secret.SecretValue,
                        Version: secret.Version.ToString());

                    newSecrets[canonicalKey] = secretValue;
                    newSecrets[secret.SecretKey] = secretValue; // Also store original key

                    _logger.LogTrace("Loaded secret: {Key} from path: {Path}", RedactKey(canonicalKey), path);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to load secrets from path: {Path}", path);
                throw;
            }
        }

        // Atomic swap of cache
        _secretCache.Clear();
        foreach (var (key, value) in newSecrets)
        {
            _secretCache[key] = value;
        }
    }

    /// <summary>
    /// Converts an Infisical secret key to canonical format.
    /// e.g., "KEYCLOAK_ENDPOINT" with path "/keycloak" -> "Keycloak:Endpoint"
    /// </summary>
    private static string ConvertToCanonicalKey(string infisicalKey, string path)
    {
        // Normalize path to section name
        var section = path.Trim('/').Replace("/", ":");

        // Convert SCREAMING_SNAKE_CASE to PascalCase
        var parts = infisicalKey.Split('_', StringSplitOptions.RemoveEmptyEntries);
        var pascalCaseParts = parts.Select(p =>
            char.ToUpperInvariant(p[0]) + p[1..].ToLowerInvariant());
        var pascalCaseKey = string.Join("", pascalCaseParts);

        // If path provides context, use it
        if (!string.IsNullOrEmpty(section))
        {
            // Check if the key already starts with the section name
            var sectionParts = section.Split(':', StringSplitOptions.RemoveEmptyEntries);
            var firstSectionPart = sectionParts.FirstOrDefault()?.ToUpperInvariant();

            if (firstSectionPart is not null &&
                infisicalKey.StartsWith(firstSectionPart, StringComparison.OrdinalIgnoreCase))
            {
                // Key already includes section, just convert to Pascal
                return pascalCaseKey.Replace(firstSectionPart,
                    char.ToUpperInvariant(firstSectionPart[0]) + firstSectionPart[1..].ToLowerInvariant());
            }

            return $"{char.ToUpperInvariant(section[0])}{section[1..]}:{pascalCaseKey}";
        }

        return pascalCaseKey;
    }

    /// <summary>
    /// Converts a canonical key to Infisical format.
    /// e.g., "Keycloak:Endpoint" -> "KEYCLOAK_ENDPOINT"
    /// </summary>
    private static string ConvertToInfisicalKey(string canonicalKey)
    {
        return canonicalKey
            .Replace(":", "_")
            .ToUpperInvariant();
    }

    /// <summary>
    /// Normalizes a path for comparison.
    /// </summary>
    private static string NormalizePath(string path)
    {
        return path.Trim('/').Replace("/", ":");
    }

    /// <summary>
    /// Validates that required configuration is present.
    /// </summary>
    private void ValidateConfiguration()
    {
        var errors = new List<string>();

        if (string.IsNullOrWhiteSpace(_options.ProjectId))
            errors.Add("Infisical ProjectId is required");

        if (string.IsNullOrWhiteSpace(_options.ClientId))
            errors.Add("Infisical ClientId is required");

        if (string.IsNullOrWhiteSpace(_options.ClientSecret))
            errors.Add("Infisical ClientSecret is required");

        if (errors.Count > 0)
        {
            throw SecretProviderException.Permanent(
                $"Invalid Infisical configuration: {string.Join(", ", errors)}",
                SecretProviderType.Infisical,
                "Initialize");
        }
    }

    /// <summary>
    /// Redacts a key for safe logging.
    /// </summary>
    private static string RedactKey(string key)
    {
        var separatorIndex = key.IndexOfAny([':', '_']);
        return separatorIndex > 0 ? key[..(separatorIndex + 1)] + "***" : key;
    }

    private void EnsureInitialized()
    {
        if (!_initialized)
        {
            throw new InvalidOperationException(
                "Infisical secret provider not initialized. Call InitializeAsync first.");
        }
    }

    /// <inheritdoc />
    public async ValueTask DisposeAsync()
    {
        _initLock.Dispose();
        _refreshLock.Dispose();

        // InfisicalClient may implement IDisposable
        if (_client is IDisposable disposable)
        {
            disposable.Dispose();
        }
        else if (_client is IAsyncDisposable asyncDisposable)
        {
            await asyncDisposable.DisposeAsync();
        }

        _client = null;
        _initialized = false;
    }
}
