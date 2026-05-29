// ABOUTME: Startup validation for API-hosted TickerQ scheduler settings.
// ABOUTME: Fails fast on insecure dashboard or structurally invalid scheduler options.

using Explore.API.Scheduling;
using Microsoft.Extensions.Options;

namespace Explore.API.Configuration;

public sealed class TickerQSchedulerOptionsValidator : IValidateOptions<TickerQSchedulerOptions>
{
    public ValidateOptionsResult Validate(string? name, TickerQSchedulerOptions options)
    {
        List<string> failures = [];

        if (string.IsNullOrWhiteSpace(options.Schema))
        {
            failures.Add("Scheduler:TickerQ:Schema is required.");
        }
        else if (!options.Schema.Equals(ApiTickerQDbContext.Schema, StringComparison.Ordinal))
        {
            failures.Add($"Scheduler:TickerQ:Schema must be {ApiTickerQDbContext.Schema}.");
        }

        if (options.MaxConcurrency <= 0)
        {
            failures.Add("Scheduler:TickerQ:MaxConcurrency must be greater than zero.");
        }

        if (string.IsNullOrWhiteSpace(options.NodeIdentifier))
        {
            failures.Add("Scheduler:TickerQ:NodeIdentifier is required.");
        }

        if (options.DashboardEnabled)
        {
            if (string.IsNullOrWhiteSpace(options.DashboardPath) ||
                options.DashboardPath[0] != '/' ||
                options.DashboardPath == "/")
            {
                failures.Add("Scheduler:TickerQ:DashboardPath must be an absolute non-root path when dashboard is enabled.");
            }

            if (string.IsNullOrWhiteSpace(options.DashboardAuthorizationPolicy))
            {
                failures.Add("Scheduler:TickerQ:DashboardAuthorizationPolicy is required when dashboard is enabled.");
            }

            if (options.DashboardAuthorizationPolicy.Equals("AllowAnonymous", StringComparison.OrdinalIgnoreCase))
            {
                failures.Add("Scheduler:TickerQ:DashboardAuthorizationPolicy must not allow anonymous dashboard access.");
            }

            if (options.DashboardSessionTimeoutMinutes <= 0)
            {
                failures.Add("Scheduler:TickerQ:DashboardSessionTimeoutMinutes must be greater than zero when dashboard is enabled.");
            }
        }

        return failures.Count == 0
            ? ValidateOptionsResult.Success
            : ValidateOptionsResult.Fail(failures);
    }
}
