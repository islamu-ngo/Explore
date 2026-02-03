// ABOUTME: Background service that periodically refreshes secrets from external providers.
// Uses PeriodicTimer for efficient scheduling with jitter and exponential backoff.

using Explore.Secrets.Abstractions;
using Explore.Secrets.Configuration;
using Explore.Secrets.Observability;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Explore.Secrets.Services;

/// <summary>
/// Background service that periodically refreshes secrets from external providers.
/// Implements exponential backoff on failures with jitter to prevent thundering herd.
/// </summary>
public sealed class SecretRefreshService : BackgroundService
{
    private readonly ISecretProvider _secretProvider;
    private readonly IConfigurationRoot? _configuration;
    private readonly SecretRefreshOptions _options;
    private readonly SecretRefreshMetrics _metrics;
    private readonly ILogger<SecretRefreshService> _logger;

    private int _consecutiveFailures;
    private DateTime? _lastSuccessfulRefresh;

    public SecretRefreshService(
        ISecretProvider secretProvider,
        IConfiguration configuration,
        IOptions<SecretRefreshOptions> options,
        SecretRefreshMetrics metrics,
        ILogger<SecretRefreshService> logger)
    {
        _secretProvider = secretProvider;
        _configuration = configuration as IConfigurationRoot;
        _options = options.Value;
        _metrics = metrics;
        _logger = logger;
    }

    /// <summary>
    /// Gets the number of consecutive refresh failures.
    /// </summary>
    public int ConsecutiveFailures => _consecutiveFailures;

    /// <summary>
    /// Gets the timestamp of the last successful refresh.
    /// </summary>
    public DateTime? LastSuccessfulRefresh => _lastSuccessfulRefresh;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (!_options.Enabled)
        {
            _logger.LogInformation("Secret refresh service is disabled");
            return;
        }

        if (!_secretProvider.SupportsRefresh)
        {
            _logger.LogInformation(
                "Secret provider {Provider} does not support refresh, service will not run",
                _secretProvider.ProviderType);
            return;
        }

        _logger.LogInformation(
            "Secret refresh service starting with interval {Interval}",
            _options.RefreshInterval);

        // Apply initial delay with jitter to spread out startup refreshes
        var initialDelay = _options.AddJitter(_options.InitialDelay);
        _logger.LogDebug("Waiting {Delay} before first refresh", initialDelay);

        try
        {
            await Task.Delay(initialDelay, stoppingToken);
        }
        catch (OperationCanceledException)
        {
            return;
        }

        // Use PeriodicTimer for efficient, drift-free scheduling
        using var timer = new PeriodicTimer(_options.RefreshInterval);

        while (!stoppingToken.IsCancellationRequested)
        {
            await RefreshSecretsAsync(stoppingToken);

            try
            {
                // Calculate next wait time based on failure state
                var waitTime = CalculateNextWaitTime();

                if (waitTime != _options.RefreshInterval)
                {
                    // On failure, use Task.Delay for backoff
                    await Task.Delay(waitTime, stoppingToken);
                }
                else
                {
                    // Normal operation: use the periodic timer
                    await timer.WaitForNextTickAsync(stoppingToken);
                }
            }
            catch (OperationCanceledException)
            {
                break;
            }
        }

        _logger.LogInformation("Secret refresh service stopped");
    }

    private async Task RefreshSecretsAsync(CancellationToken cancellationToken)
    {
        using var operation = _metrics.StartRefreshOperation(_secretProvider.ProviderType);

        try
        {
            _logger.LogDebug(
                "Starting secret refresh (consecutive failures: {Failures})",
                _consecutiveFailures);

            await _secretProvider.RefreshAsync(cancellationToken);

            // Reload configuration if available
            if (_configuration is not null)
            {
                _configuration.Reload();
                _logger.LogDebug("Configuration reloaded after secret refresh");
            }

            _consecutiveFailures = 0;
            _lastSuccessfulRefresh = DateTime.UtcNow;
            operation.Complete();

            _logger.LogInformation("Secret refresh completed successfully");
        }
        catch (SecretProviderException ex) when (ex.IsTransient)
        {
            _consecutiveFailures++;
            operation.Fail("transient");

            _logger.LogWarning(
                ex,
                "Transient error during secret refresh (failures: {Failures}, operation: {Operation})",
                _consecutiveFailures,
                ex.Operation);
        }
        catch (SecretProviderException ex)
        {
            _consecutiveFailures++;
            operation.Fail("permanent");

            _logger.LogError(
                ex,
                "Permanent error during secret refresh (failures: {Failures}, operation: {Operation})",
                _consecutiveFailures,
                ex.Operation);
        }
        catch (OperationCanceledException)
        {
            // Don't record cancellation as failure
            _logger.LogDebug("Secret refresh cancelled");
            throw;
        }
        catch (Exception ex)
        {
            _consecutiveFailures++;
            operation.Fail("unknown");

            _logger.LogError(
                ex,
                "Unexpected error during secret refresh (failures: {Failures})",
                _consecutiveFailures);
        }
    }

    private TimeSpan CalculateNextWaitTime()
    {
        if (_consecutiveFailures == 0)
        {
            // Normal interval with jitter
            return _options.AddJitter(_options.RefreshInterval);
        }

        // Use exponential backoff for failures
        var backoff = _options.CalculateBackoffDelay(_consecutiveFailures);

        _logger.LogDebug(
            "Using backoff delay {Delay} after {Failures} consecutive failures",
            backoff,
            _consecutiveFailures);

        return backoff;
    }

    public override async Task StopAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation(
            "Secret refresh service stopping (last success: {LastSuccess}, failures: {Failures})",
            _lastSuccessfulRefresh,
            _consecutiveFailures);

        await base.StopAsync(cancellationToken);
    }
}
