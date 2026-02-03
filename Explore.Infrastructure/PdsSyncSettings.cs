// ABOUTME: Configuration settings for PDS synchronization background worker.
// ABOUTME: Controls polling interval, retry logic, and batch processing for outbox pattern.

namespace Explore.Infrastructure;

/// <summary>
/// Configuration settings for PDS synchronization.
/// Bind from appsettings.json section "PdsSync".
/// </summary>
public class PdsSyncSettings
{
    /// <summary>
    /// Configuration section name in appsettings.json.
    /// </summary>
    public const string SectionName = "PdsSync";

    /// <summary>
    /// Whether PDS synchronization is enabled.
    /// Default: true.
    /// </summary>
    public bool Enabled { get; set; } = true;

    /// <summary>
    /// Polling interval in seconds for the background worker.
    /// Default: 5 seconds.
    /// </summary>
    public int PollingIntervalSeconds { get; set; } = 5;

    /// <summary>
    /// Maximum number of outbox entries to process in a single batch.
    /// Default: 100.
    /// </summary>
    public int BatchSize { get; set; } = 100;

    /// <summary>
    /// Maximum number of retry attempts before marking as failed.
    /// Default: 5.
    /// </summary>
    public int MaxRetryCount { get; set; } = 5;

    /// <summary>
    /// Initial retry delay in seconds (exponential backoff base).
    /// Default: 1 second.
    /// </summary>
    public int InitialRetryDelaySeconds { get; set; } = 1;

    /// <summary>
    /// Maximum retry delay in seconds (caps exponential backoff).
    /// Default: 3600 seconds (1 hour).
    /// </summary>
    public int MaxRetryDelaySeconds { get; set; } = 3600;

    /// <summary>
    /// Islamu-hosted PDS base URL.
    /// If null/empty, PDS operations will be disabled.
    /// </summary>
    public string? IslamuPdsHost { get; set; }

    /// <summary>
    /// Service DID for signing operations (Islamu service identity).
    /// Required for custodial signing.
    /// </summary>
    public string? ServiceDid { get; set; }

    /// <summary>
    /// Timeout in seconds for individual PDS API calls.
    /// Default: 30 seconds.
    /// </summary>
    public int ApiTimeoutSeconds { get; set; } = 30;

    /// <summary>
    /// Whether to enable detailed logging for debugging.
    /// Default: false.
    /// </summary>
    public bool VerboseLogging { get; set; }

    /// <summary>
    /// Calculates the retry delay using exponential backoff.
    /// </summary>
    /// <param name="retryCount">Current retry count (0-based).</param>
    /// <returns>Delay in seconds, capped at MaxRetryDelaySeconds.</returns>
    public int CalculateRetryDelay(int retryCount)
    {
        // Exponential backoff: delay = InitialRetryDelay * 2^retryCount
        var delay = InitialRetryDelaySeconds * (int)Math.Pow(2, retryCount);
        return Math.Min(delay, MaxRetryDelaySeconds);
    }
}
