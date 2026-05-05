// ABOUTME: API readiness health check for launch-critical SMTP connectivity.
// ABOUTME: Uses the configured email abstraction so SMTP behavior stays behind Infrastructure contracts.

using Explore.Application.Contracts.Infrastructure;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace Explore.API.HealthChecks;

public sealed class SmtpHealthCheck(IEmailService emailService) : IHealthCheck
{
    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        var result = await emailService.TestConnectionAsync(cancellationToken).ConfigureAwait(false);
        var data = new Dictionary<string, object>
        {
            ["durationMs"] = result.Duration.TotalMilliseconds
        };

        if (result.Success)
        {
            return HealthCheckResult.Healthy(result.Message ?? "SMTP connection is ready.", data);
        }

        var message = result.ErrorMessage ?? "SMTP connection test failed.";
        if (message.Contains("not configured", StringComparison.OrdinalIgnoreCase))
        {
            return HealthCheckResult.Degraded(message, data: data);
        }

        return HealthCheckResult.Unhealthy(message, data: data);
    }
}
