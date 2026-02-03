// ABOUTME: HTTP client factory that supports credential rotation.
// Uses atomic swap pattern with grace period to handle in-flight requests during rotation.

using System.Collections.Concurrent;
using Explore.Secrets.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Explore.Secrets.Services;

/// <summary>
/// HTTP client factory that supports credential rotation without connection leaks.
/// Uses atomic swap pattern with configurable grace period for in-flight requests.
/// </summary>
/// <remarks>
/// Key features:
/// - Listens to IOptionsMonitor for credential changes
/// - Atomic swap: creates new client, schedules old for disposal after grace period
/// - Thread-safe client access via ConcurrentDictionary
/// - Graceful drain allows in-flight requests to complete
/// </remarks>
public sealed class RotationAwareHttpClientFactory : IHttpClientFactory, IDisposable
{
    private readonly ConcurrentDictionary<string, ClientEntry> _clients = new();
    private readonly IOptionsMonitor<HttpClientCredentialOptions> _credentialOptions;
    private readonly IOptionsMonitor<RotationOptions> _rotationOptions;
    private readonly ILogger<RotationAwareHttpClientFactory> _logger;
    private readonly IDisposable? _credentialChangeListener;
    private readonly SemaphoreSlim _rotationSemaphore;
    private readonly object _disposeLock = new();
    private bool _disposed;

    /// <summary>
    /// Initializes a new instance of the RotationAwareHttpClientFactory.
    /// </summary>
    public RotationAwareHttpClientFactory(
        IOptionsMonitor<HttpClientCredentialOptions> credentialOptions,
        IOptionsMonitor<RotationOptions> rotationOptions,
        ILogger<RotationAwareHttpClientFactory> logger)
    {
        _credentialOptions = credentialOptions ?? throw new ArgumentNullException(nameof(credentialOptions));
        _rotationOptions = rotationOptions ?? throw new ArgumentNullException(nameof(rotationOptions));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _rotationSemaphore = new SemaphoreSlim(rotationOptions.CurrentValue.MaxConcurrentRotations);

        // Subscribe to credential changes
        _credentialChangeListener = _credentialOptions.OnChange(OnCredentialsChanged);

        _logger.LogDebug("RotationAwareHttpClientFactory initialized");
    }

    /// <summary>
    /// Creates or retrieves an HttpClient for the specified name.
    /// </summary>
    /// <param name="name">The logical name of the client.</param>
    /// <returns>An HttpClient configured for the specified name.</returns>
    public HttpClient CreateClient(string name)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        var entry = _clients.GetOrAdd(name, CreateClientEntry);
        return entry.Client;
    }

    /// <summary>
    /// Gets the number of active clients.
    /// </summary>
    public int ActiveClientCount => _clients.Count;

    /// <summary>
    /// Gets whether a client with the specified name exists.
    /// </summary>
    public bool HasClient(string name) => _clients.ContainsKey(name);

    private ClientEntry CreateClientEntry(string name)
    {
        var client = CreateHttpClientInternal(name);
        _logger.LogDebug("Created new HTTP client for '{Name}'", name);
        return new ClientEntry(client, DateTime.UtcNow);
    }

    private HttpClient CreateHttpClientInternal(string name)
    {
        var client = new HttpClient();
        var credentials = _credentialOptions.CurrentValue;

        if (credentials.Clients.TryGetValue(name, out var clientCred))
        {
            ApplyCredentials(client, clientCred, name);
        }

        return client;
    }

    private void ApplyCredentials(HttpClient client, HttpClientCredential credential, string name)
    {
        // Set base address if configured
        if (!string.IsNullOrEmpty(credential.BaseAddress))
        {
            client.BaseAddress = new Uri(credential.BaseAddress);
        }

        // Set timeout if configured
        if (credential.Timeout.HasValue)
        {
            client.Timeout = credential.Timeout.Value;
        }

        // Add bearer token if configured
        if (!string.IsNullOrEmpty(credential.BearerToken))
        {
            client.DefaultRequestHeaders.Authorization =
                new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", credential.BearerToken);
        }

        // Add API key if configured
        if (!string.IsNullOrEmpty(credential.ApiKey))
        {
            client.DefaultRequestHeaders.Add("X-API-Key", credential.ApiKey);
        }

        // Add custom headers
        foreach (var header in credential.Headers)
        {
            if (!string.IsNullOrEmpty(header.Value))
            {
                client.DefaultRequestHeaders.TryAddWithoutValidation(header.Key, header.Value);
            }
        }

        _logger.LogDebug(
            "Applied credentials to HTTP client '{Name}' (BaseAddress: {BaseAddress})",
            name,
            credential.BaseAddress ?? "not set");
    }

    private void OnCredentialsChanged(HttpClientCredentialOptions newCredentials, string? name)
    {
        if (!_rotationOptions.CurrentValue.Enabled)
        {
            _logger.LogDebug("Credential rotation is disabled, ignoring change");
            return;
        }

        _logger.LogInformation("HTTP client credentials changed, initiating rotation");

        // Rotate all clients that have new credentials
        foreach (var clientName in newCredentials.Clients.Keys)
        {
            if (_clients.ContainsKey(clientName))
            {
                _ = RotateClientAsync(clientName);
            }
        }
    }

    private async Task RotateClientAsync(string name)
    {
        try
        {
            // Limit concurrent rotations
            if (!await _rotationSemaphore.WaitAsync(TimeSpan.FromSeconds(5)))
            {
                _logger.LogWarning(
                    "Rotation for client '{Name}' skipped - too many concurrent rotations",
                    name);
                return;
            }

            try
            {
                await RotateClientInternalAsync(name);
            }
            finally
            {
                _rotationSemaphore.Release();
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to rotate HTTP client '{Name}'", name);
        }
    }

    private async Task RotateClientInternalAsync(string name)
    {
        var rotationOptions = _rotationOptions.CurrentValue;

        if (rotationOptions.LogRotationEvents)
        {
            _logger.LogInformation("Rotating HTTP client '{Name}'", name);
        }

        // Create new client with updated credentials
        var newEntry = CreateClientEntry(name);

        // Atomic swap - replace old entry with new one
        if (_clients.TryGetValue(name, out var oldEntry))
        {
            _clients[name] = newEntry;

            if (rotationOptions.LogRotationEvents)
            {
                _logger.LogDebug(
                    "Swapped HTTP client '{Name}', scheduling disposal after {GracePeriod}",
                    name,
                    rotationOptions.GracePeriod);
            }

            // Schedule disposal of old client after grace period
            _ = DisposeAfterGracePeriodAsync(oldEntry.Client, name, rotationOptions.GracePeriod);
        }
        else
        {
            // No existing client, just add the new one
            _clients[name] = newEntry;
        }

        await Task.CompletedTask;
    }

    private async Task DisposeAfterGracePeriodAsync(HttpClient client, string name, TimeSpan gracePeriod)
    {
        try
        {
            await Task.Delay(gracePeriod);
            client.Dispose();

            if (_rotationOptions.CurrentValue.LogRotationEvents)
            {
                _logger.LogDebug(
                    "Disposed old HTTP client '{Name}' after grace period",
                    name);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Error disposing old HTTP client '{Name}' after grace period",
                name);
        }
    }

    /// <summary>
    /// Forces rotation of a specific client.
    /// Useful for testing or manual rotation triggers.
    /// </summary>
    public async Task ForceRotateAsync(string name)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        if (!_clients.ContainsKey(name))
        {
            throw new ArgumentException($"No client with name '{name}' exists", nameof(name));
        }

        await RotateClientAsync(name);
    }

    /// <summary>
    /// Forces rotation of all clients.
    /// </summary>
    public async Task ForceRotateAllAsync()
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        var tasks = _clients.Keys.Select(RotateClientAsync);
        await Task.WhenAll(tasks);
    }

    /// <summary>
    /// Disposes the factory and all managed clients.
    /// </summary>
    public void Dispose()
    {
        lock (_disposeLock)
        {
            if (_disposed)
            {
                return;
            }

            _disposed = true;
        }

        _logger.LogDebug("Disposing RotationAwareHttpClientFactory");

        // Unsubscribe from changes
        _credentialChangeListener?.Dispose();

        // Dispose all clients
        foreach (var entry in _clients.Values)
        {
            try
            {
                entry.Client.Dispose();
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error disposing HTTP client during factory disposal");
            }
        }

        _clients.Clear();
        _rotationSemaphore.Dispose();

        _logger.LogDebug("RotationAwareHttpClientFactory disposed");
    }

    /// <summary>
    /// Internal record to track client creation time.
    /// </summary>
    private sealed record ClientEntry(HttpClient Client, DateTime CreatedAt);
}
