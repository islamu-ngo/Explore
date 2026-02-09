// ABOUTME: Configuration provider for loading encrypted settings from database.
// Uses Dapper for lightweight queries and decrypts values using AesEncryptionService.

namespace Explore.Secrets.Configuration;

using System.Data;
using Explore.Secrets.Abstractions;
using Explore.Secrets.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Npgsql;

/// <summary>
/// Configuration provider that loads encrypted settings from the database.
/// </summary>
/// <remarks>
/// Features:
/// - Uses Dapper for lightweight database access (no EF Core dependency)
/// - Decrypts values using AES-256-GCM via IEncryptionService
/// - Supports periodic reload with change detection
/// - On refresh failure, keeps existing data (doesn't clear)
/// - Thread-safe loading via SemaphoreSlim
/// </remarks>
public sealed class DbConfigurationProvider : ConfigurationProvider, IDisposable
{
    private readonly DbConfigurationSource _source;
    private readonly IEncryptionService _encryptionService;
    private readonly SemaphoreSlim _loadLock = new(1, 1);
    private readonly ILogger<DbConfigurationProvider>? _logger;
    private readonly Timer? _reloadTimer;
    private bool _disposed;
    private bool _hasLoadedOnce;

    /// <summary>
    /// Gets the timestamp of the last successful load.
    /// </summary>
    public DateTime? LastSuccessfulLoad { get; private set; }

    /// <summary>
    /// Gets the number of consecutive load failures.
    /// </summary>
    public int ConsecutiveFailures { get; private set; }

    /// <summary>
    /// Creates a new database configuration provider.
    /// </summary>
    /// <param name="source">The configuration source with connection settings.</param>
    /// <param name="logger">Optional logger for diagnostics.</param>
    public DbConfigurationProvider(
        DbConfigurationSource source,
        ILogger<DbConfigurationProvider>? logger = null)
    {
        ArgumentNullException.ThrowIfNull(source);

        _source = source;
        _logger = logger;

        // Create encryption service from options
        var encryptionOptions = Options.Create(source.EncryptionOptions);
        _encryptionService = new AesEncryptionService(encryptionOptions, null);

        // Set up periodic reload if enabled
        if (_source.ReloadOnChange && _source.PollingInterval > TimeSpan.Zero)
        {
            _reloadTimer = new Timer(
                _ => ReloadAsync().ConfigureAwait(false).GetAwaiter().GetResult(),
                null,
                _source.PollingInterval,
                _source.PollingInterval);
        }
    }

    /// <inheritdoc />
    public override void Load()
    {
        LoadAsync().ConfigureAwait(false).GetAwaiter().GetResult();
    }

    /// <summary>
    /// Loads settings from the database asynchronously.
    /// </summary>
    public async Task LoadAsync()
    {
        await _loadLock.WaitAsync().ConfigureAwait(false);
        try
        {
            await LoadInternalAsync().ConfigureAwait(false);
            _hasLoadedOnce = true;
            ConsecutiveFailures = 0;
            LastSuccessfulLoad = DateTime.UtcNow;
        }
        catch (Exception ex)
        {
            ConsecutiveFailures++;
            _logger?.LogError(ex,
                "Failed to load settings from database (attempt {Attempt})",
                ConsecutiveFailures);

            // On first load, throw if configured to do so
            if (!_hasLoadedOnce && _source.ThrowOnFirstLoadFailure)
            {
                throw new InvalidOperationException(
                    "Failed to load initial configuration from database.", ex);
            }

            // On subsequent failures, keep existing data
            _logger?.LogWarning(
                "Keeping {Count} existing settings after load failure",
                Data.Count);
        }
        finally
        {
            _loadLock.Release();
        }
    }

    /// <summary>
    /// Reloads settings from the database.
    /// </summary>
    public async Task ReloadAsync()
    {
        await LoadAsync().ConfigureAwait(false);
        OnReload();
    }

    private async Task LoadInternalAsync()
    {
        var newData = new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase);

        await using var connection = new NpgsqlConnection(_source.ConnectionString);
        await connection.OpenAsync().ConfigureAwait(false);

        // Query using raw ADO.NET (lightweight, no Dapper dependency)
        await using var command = connection.CreateCommand();
        command.CommandText = """
            SELECT "Key", "EncryptedValue", "KeyVersion", "IsSensitive"
            FROM "AppSettings"
            ORDER BY "Key"
            """;

        await using var reader = await command.ExecuteReaderAsync().ConfigureAwait(false);

        var loadedCount = 0;
        var decryptionErrors = 0;

        while (await reader.ReadAsync().ConfigureAwait(false))
        {
            var key = reader.GetString(0);
            var encryptedValue = reader.GetString(1);
            var keyVersion = reader.GetInt32(2);
            var isSensitive = reader.GetBoolean(3);

            try
            {
                var decryptedValue = _encryptionService.Decrypt(encryptedValue, keyVersion);
                newData[key] = decryptedValue;
                loadedCount++;

                if (!isSensitive)
                {
                    _logger?.LogDebug("Loaded setting: {Key}", key);
                }
                else
                {
                    _logger?.LogDebug("Loaded sensitive setting: {Key}", key);
                }
            }
            catch (Exception ex)
            {
                decryptionErrors++;
                _logger?.LogError(ex,
                    "Failed to decrypt setting '{Key}' with key version {Version}",
                    key, keyVersion);

                // Don't include failed decryptions - they may have wrong key version
            }
        }

        // Replace data atomically
        Data = newData;

        _logger?.LogInformation(
            "Loaded {Count} settings from database ({Errors} decryption errors)",
            loadedCount, decryptionErrors);
    }

    /// <inheritdoc />
    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _reloadTimer?.Dispose();
        _loadLock.Dispose();
        _encryptionService.Dispose();
        _disposed = true;
    }
}

/// <summary>
/// Extension methods for adding database configuration.
/// </summary>
public static class DbConfigurationExtensions
{
    /// <summary>
    /// Adds database configuration source to the configuration builder.
    /// </summary>
    /// <param name="builder">The configuration builder.</param>
    /// <param name="connectionString">Database connection string.</param>
    /// <param name="encryptionOptions">Encryption options for decrypting values.</param>
    /// <param name="configure">Optional action to configure additional options.</param>
    /// <returns>The configuration builder for chaining.</returns>
    public static IConfigurationBuilder AddDatabaseConfiguration(
        this IConfigurationBuilder builder,
        string connectionString,
        EncryptionOptions encryptionOptions,
        Action<DbConfigurationSource>? configure = null)
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentException.ThrowIfNullOrEmpty(connectionString);
        ArgumentNullException.ThrowIfNull(encryptionOptions);

        var source = new DbConfigurationSource
        {
            ConnectionString = connectionString,
            EncryptionOptions = encryptionOptions
        };

        configure?.Invoke(source);
        builder.Add(source);

        return builder;
    }
}
