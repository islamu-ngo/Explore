// ABOUTME: DbContext factory that supports connection string rotation.
// Monitors for connection string changes and ensures new contexts use updated credentials.

using Explore.Secrets.Configuration;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Explore.Secrets.Services;

/// <summary>
/// DbContext factory that supports connection string rotation.
/// Monitors for connection string changes and ensures new contexts use updated credentials.
/// </summary>
/// <remarks>
/// Key features:
/// - Listens to IOptionsMonitor for connection string changes
/// - Each CreateDbContext call uses the latest connection string
/// - Thread-safe via lock on options access
/// - Logs connection string changes (credentials redacted)
///
/// Unlike HttpClient, DbContext instances are short-lived and should be disposed
/// after each unit of work. This factory ensures that newly created contexts
/// automatically use updated connection strings without explicit rotation.
/// </remarks>
/// <typeparam name="TContext">The type of DbContext to create.</typeparam>
public sealed class RotationAwareDbContextFactory<TContext> : IDbContextFactory<TContext>, IDisposable
    where TContext : DbContext
{
    private readonly Func<DbContextOptions<TContext>, TContext> _contextFactory;
    private readonly IOptionsMonitor<DatabaseConnectionOptions> _connectionOptions;
    private readonly IOptionsMonitor<RotationOptions> _rotationOptions;
    private readonly ILogger<RotationAwareDbContextFactory<TContext>> _logger;
    private readonly IDisposable? _connectionChangeListener;
    private readonly object _optionsLock = new();

    private string? _currentConnectionString;
    private DateTime _lastConnectionStringChange;
    private int _rotationCount;
    private bool _disposed;

    /// <summary>
    /// Initializes a new instance of the RotationAwareDbContextFactory.
    /// </summary>
    /// <param name="contextFactory">Factory function to create DbContext instances.</param>
    /// <param name="connectionOptions">Options monitor for database connection settings.</param>
    /// <param name="rotationOptions">Options monitor for rotation settings.</param>
    /// <param name="logger">Logger instance.</param>
    public RotationAwareDbContextFactory(
        Func<DbContextOptions<TContext>, TContext> contextFactory,
        IOptionsMonitor<DatabaseConnectionOptions> connectionOptions,
        IOptionsMonitor<RotationOptions> rotationOptions,
        ILogger<RotationAwareDbContextFactory<TContext>> logger)
    {
        _contextFactory = contextFactory ?? throw new ArgumentNullException(nameof(contextFactory));
        _connectionOptions = connectionOptions ?? throw new ArgumentNullException(nameof(connectionOptions));
        _rotationOptions = rotationOptions ?? throw new ArgumentNullException(nameof(rotationOptions));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));

        // Initialize with current connection string
        _currentConnectionString = connectionOptions.CurrentValue.ConnectionString;
        _lastConnectionStringChange = DateTime.UtcNow;

        // Subscribe to connection string changes
        _connectionChangeListener = _connectionOptions.OnChange(OnConnectionOptionsChanged);

        _logger.LogDebug(
            "RotationAwareDbContextFactory<{ContextType}> initialized",
            typeof(TContext).Name);
    }

    /// <summary>
    /// Gets the number of times the connection string has been rotated.
    /// </summary>
    public int RotationCount => _rotationCount;

    /// <summary>
    /// Gets the timestamp of the last connection string change.
    /// </summary>
    public DateTime LastConnectionStringChange => _lastConnectionStringChange;

    /// <summary>
    /// Gets the current connection string (credentials redacted for logging).
    /// </summary>
    public string? CurrentConnectionStringRedacted => RedactConnectionString(_currentConnectionString);

    /// <summary>
    /// Creates a new DbContext with the current connection string.
    /// </summary>
    /// <returns>A new DbContext instance.</returns>
    public TContext CreateDbContext()
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        string? connectionString;
        lock (_optionsLock)
        {
            connectionString = _currentConnectionString;
        }

        if (string.IsNullOrEmpty(connectionString))
        {
            throw new InvalidOperationException(
                $"Connection string is not configured for {typeof(TContext).Name}. " +
                $"Ensure DatabaseConnectionOptions is properly configured.");
        }

        var optionsBuilder = new DbContextOptionsBuilder<TContext>();
        ConfigureDbContextOptions(optionsBuilder, connectionString);

        var context = _contextFactory(optionsBuilder.Options);

        _logger.LogTrace(
            "Created DbContext<{ContextType}> (rotations: {RotationCount})",
            typeof(TContext).Name,
            _rotationCount);

        return context;
    }

    /// <summary>
    /// Configures the DbContext options with the connection string.
    /// </summary>
    private void ConfigureDbContextOptions(
        DbContextOptionsBuilder<TContext> optionsBuilder,
        string connectionString)
    {
        var dbOptions = _connectionOptions.CurrentValue;

        // Build Npgsql-specific connection string with optional pool settings
        var npgsqlConnectionString = connectionString;

        // Note: Connection pooling settings should be part of the connection string
        // or configured via NpgsqlDataSource. We log them here for visibility.
        if (dbOptions.MaxPoolSize.HasValue)
        {
            _logger.LogDebug("MaxPoolSize configured: {MaxPoolSize}", dbOptions.MaxPoolSize);
        }

        optionsBuilder.UseNpgsql(npgsqlConnectionString);
    }

    private void OnConnectionOptionsChanged(DatabaseConnectionOptions newOptions, string? name)
    {
        if (!_rotationOptions.CurrentValue.Enabled)
        {
            _logger.LogDebug("Connection rotation is disabled, ignoring change");
            return;
        }

        var newConnectionString = newOptions.ConnectionString;

        lock (_optionsLock)
        {
            // Check if connection string actually changed
            if (string.Equals(_currentConnectionString, newConnectionString, StringComparison.Ordinal))
            {
                _logger.LogDebug("Connection string unchanged, skipping rotation");
                return;
            }

            _currentConnectionString = newConnectionString;
            _lastConnectionStringChange = DateTime.UtcNow;
            Interlocked.Increment(ref _rotationCount);
        }

        if (_rotationOptions.CurrentValue.LogRotationEvents)
        {
            _logger.LogInformation(
                "Connection string rotated for {ContextType} (rotation #{RotationCount}). New connection: {ConnectionString}",
                typeof(TContext).Name,
                _rotationCount,
                RedactConnectionString(newConnectionString));
        }
    }

    /// <summary>
    /// Redacts sensitive information from a connection string for logging.
    /// </summary>
    private static string? RedactConnectionString(string? connectionString)
    {
        if (string.IsNullOrEmpty(connectionString))
        {
            return null;
        }

        // Common patterns for passwords in connection strings
        var patterns = new[]
        {
            ("Password=", "Password=***"),
            ("password=", "password=***"),
            ("Pwd=", "Pwd=***"),
            ("pwd=", "pwd=***"),
            ("Secret=", "Secret=***"),
            ("secret=", "secret=***"),
        };

        var redacted = connectionString;
        foreach (var (pattern, replacement) in patterns)
        {
            var index = redacted.IndexOf(pattern, StringComparison.OrdinalIgnoreCase);
            if (index >= 0)
            {
                var endIndex = redacted.IndexOf(';', index);
                if (endIndex > 0)
                {
                    redacted = redacted[..index] + replacement + redacted[endIndex..];
                }
                else
                {
                    redacted = redacted[..index] + replacement;
                }
            }
        }

        return redacted;
    }

    /// <summary>
    /// Forces a connection string update from the current options.
    /// Useful for testing or manual rotation triggers.
    /// </summary>
    public void ForceRefresh()
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        var currentOptions = _connectionOptions.CurrentValue;
        OnConnectionOptionsChanged(currentOptions, null);
    }

    /// <summary>
    /// Disposes the factory and unsubscribes from change notifications.
    /// </summary>
    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;

        _logger.LogDebug(
            "Disposing RotationAwareDbContextFactory<{ContextType}> (total rotations: {RotationCount})",
            typeof(TContext).Name,
            _rotationCount);

        _connectionChangeListener?.Dispose();
    }
}

/// <summary>
/// Non-generic base interface for rotation-aware factory operations.
/// </summary>
public interface IRotationAwareDbContextFactory
{
    /// <summary>
    /// Gets the number of times the connection string has been rotated.
    /// </summary>
    int RotationCount { get; }

    /// <summary>
    /// Gets the timestamp of the last connection string change.
    /// </summary>
    DateTime LastConnectionStringChange { get; }

    /// <summary>
    /// Gets the current connection string (credentials redacted).
    /// </summary>
    string? CurrentConnectionStringRedacted { get; }

    /// <summary>
    /// Forces a connection string refresh from the current options.
    /// </summary>
    void ForceRefresh();
}
