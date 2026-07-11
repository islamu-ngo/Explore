// ABOUTME: Health check for secret provider status.
// Integrates with ASP.NET Core health check system for /health endpoints.

using Explore.Secrets.Abstractions;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Logging;

namespace Explore.Secrets.Observability;

/// <summary>
/// Health check that reports the status of the secret provider.
/// Registers with ASP.NET Core health checks for /health endpoint integration.
/// </summary>
public sealed class SecretProviderHealthCheck : IHealthCheck
{
    /// <summary>
    /// Health check name for registration.
    /// </summary>
    public const string Name = "secret_provider";

    /// <summary>
    /// Tag for categorizing this health check.
    /// </summary>
    public const string Tag = "secrets";

    private readonly ISecretProvider _provider;
    private readonly SecretRefreshMetrics? _metrics;
    private readonly ILogger<SecretProviderHealthCheck> _logger;

    public SecretProviderHealthCheck(
        ISecretProvider provider,
        ILogger<SecretProviderHealthCheck> logger,
        SecretRefreshMetrics? metrics = null)
    {
        _provider = provider;
        _logger = logger;
        _metrics = metrics;
    }

    /// <inheritdoc />
    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var healthInfo = await _provider.GetHealthAsync(cancellationToken);

            var data = new Dictionary<string, object>
            {
                ["provider"] = healthInfo.ProviderType.ToString(),
                ["supportsRefresh"] = _provider.SupportsRefresh,
                ["consecutiveFailures"] = healthInfo.ConsecutiveFailures
            };

            if (healthInfo.LastSuccessfulRefresh.HasValue)
            {
                data["lastSuccessfulRefresh"] = healthInfo.LastSuccessfulRefresh.Value.ToString("O");
                data["secondsSinceLastRefresh"] = (DateTimeOffset.UtcNow - healthInfo.LastSuccessfulRefresh.Value).TotalSeconds;
            }

            // Add metrics data if available
            if (_metrics is not null)
            {
                data["metricsLastRefresh"] = _metrics.LastSuccessfulRefresh.ToString("O");
                data["metricsConsecutiveFailures"] = _metrics.ConsecutiveFailures;
            }

            if (healthInfo.IsHealthy)
            {
                return HealthCheckResult.Healthy(
                    description: $"Secret provider '{healthInfo.ProviderType}' is healthy",
                    data: data);
            }

            // Determine severity based on consecutive failures
            if (healthInfo.ConsecutiveFailures >= 3)
            {
                _logger.LogWarning(
                    "Secret provider unhealthy: {ProviderType} has {FailureCount} consecutive failures. Error: {Error}",
                    healthInfo.ProviderType,
                    healthInfo.ConsecutiveFailures,
                    healthInfo.ErrorMessage);

                return HealthCheckResult.Unhealthy(
                    description: $"Secret provider '{healthInfo.ProviderType}' has {healthInfo.ConsecutiveFailures} consecutive failures: {healthInfo.ErrorMessage}",
                    data: data);
            }

            return HealthCheckResult.Degraded(
                description: $"Secret provider '{healthInfo.ProviderType}' is degraded: {healthInfo.ErrorMessage}",
                data: data);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Health check failed for secret provider");

            return HealthCheckResult.Unhealthy(
                description: $"Health check threw exception: {ex.Message}",
                exception: ex,
                data: new Dictionary<string, object>
                {
                    ["provider"] = _provider.ProviderType.ToString(),
                    ["exceptionType"] = ex.GetType().Name
                });
        }
    }
}
