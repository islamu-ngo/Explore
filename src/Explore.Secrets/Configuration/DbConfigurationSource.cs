// ABOUTME: Configuration source for loading encrypted settings from database.
// Used with IConfigurationBuilder.Add() to include database settings in configuration.

namespace Explore.Secrets.Configuration;

using Microsoft.Extensions.Configuration;

/// <summary>
/// Configuration source for database-stored encrypted settings.
/// </summary>
public sealed class DbConfigurationSource : IConfigurationSource
{
    /// <summary>
    /// Connection string for the database.
    /// </summary>
    public required string ConnectionString { get; init; }

    /// <summary>
    /// Options for the encryption service.
    /// </summary>
    public required EncryptionOptions EncryptionOptions { get; init; }

    /// <summary>
    /// Whether to reload configuration when changes are detected.
    /// </summary>
    public bool ReloadOnChange { get; init; } = true;

    /// <summary>
    /// Interval for polling database for changes.
    /// </summary>
    public TimeSpan PollingInterval { get; init; } = TimeSpan.FromMinutes(1);

    /// <summary>
    /// Whether to throw on first load failure or continue with empty configuration.
    /// </summary>
    public bool ThrowOnFirstLoadFailure { get; init; } = true;

    /// <inheritdoc />
    public IConfigurationProvider Build(IConfigurationBuilder builder)
    {
        return new DbConfigurationProvider(this);
    }
}
