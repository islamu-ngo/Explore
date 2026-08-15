// ABOUTME: Startup validation for API-hosted Quartz.NET scheduler settings.
// ABOUTME: Fails fast on an unauthenticated status endpoint or structurally invalid scheduler options.

using Microsoft.Extensions.Options;

namespace Explore.API.Configuration;

public sealed class QuartzSchedulerSettingsValidator : IValidateOptions<QuartzSchedulerSettings>
{
    public ValidateOptionsResult Validate(string? name, QuartzSchedulerSettings options)
    {
        ArgumentNullException.ThrowIfNull(options);

        List<string> failures = [];

        if (string.IsNullOrWhiteSpace(options.SchedulerName))
        {
            failures.Add("Scheduler:Quartz:SchedulerName is required.");
        }

        if (string.IsNullOrWhiteSpace(options.InstanceId))
        {
            failures.Add("Scheduler:Quartz:InstanceId is required.");
        }

        if (options.MaxConcurrency <= 0)
        {
            failures.Add("Scheduler:Quartz:MaxConcurrency must be greater than zero.");
        }

        if (string.IsNullOrWhiteSpace(options.TablePrefix))
        {
            failures.Add("Scheduler:Quartz:TablePrefix is required.");
        }
        else if (!IsSafeTablePrefix(options.TablePrefix))
        {
            failures.Add(
                "Scheduler:Quartz:TablePrefix must contain only letters, digits, or underscores so it is safe to inline into DDL.");
        }

        if (options.ClusteringEnabled)
        {
            if (!options.UsePersistentStore)
            {
                failures.Add("Scheduler:Quartz:ClusteringEnabled requires Scheduler:Quartz:UsePersistentStore to be true.");
            }

            if (!options.InstanceId.Equals(QuartzSchedulerSettings.AutoInstanceId, StringComparison.Ordinal))
            {
                failures.Add(
                    $"Scheduler:Quartz:InstanceId must be {QuartzSchedulerSettings.AutoInstanceId} when clustering is enabled so each node registers a unique identity.");
            }

            if (options.ClusterCheckinIntervalSeconds <= 0)
            {
                failures.Add("Scheduler:Quartz:ClusterCheckinIntervalSeconds must be greater than zero when clustering is enabled.");
            }
        }

        if (options.StatusEndpointEnabled)
        {
            if (string.IsNullOrWhiteSpace(options.StatusEndpointPath) ||
                options.StatusEndpointPath[0] != '/' ||
                options.StatusEndpointPath == "/")
            {
                failures.Add("Scheduler:Quartz:StatusEndpointPath must be an absolute non-root path when the status endpoint is enabled.");
            }

            if (string.IsNullOrWhiteSpace(options.StatusEndpointAuthorizationPolicy))
            {
                failures.Add("Scheduler:Quartz:StatusEndpointAuthorizationPolicy is required when the status endpoint is enabled.");
            }
            else if (options.StatusEndpointAuthorizationPolicy.Equals("AllowAnonymous", StringComparison.OrdinalIgnoreCase))
            {
                failures.Add("Scheduler:Quartz:StatusEndpointAuthorizationPolicy must not allow anonymous scheduler access.");
            }
        }

        return failures.Count == 0
            ? ValidateOptionsResult.Success
            : ValidateOptionsResult.Fail(failures);
    }

    private static bool IsSafeTablePrefix(string prefix)
    {
        foreach (var character in prefix)
        {
            if (!char.IsAsciiLetterOrDigit(character) && character != '_')
            {
                return false;
            }
        }

        return true;
    }
}
