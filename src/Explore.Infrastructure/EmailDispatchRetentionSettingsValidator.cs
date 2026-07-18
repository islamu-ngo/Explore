// ABOUTME: Startup validator for email dispatch content retention and redaction settings.
// ABOUTME: Fails fast on unsafe scheduling, retention, or batch configuration.

using Microsoft.Extensions.Options;

namespace Explore.Infrastructure;

public sealed class EmailDispatchRetentionSettingsValidator : IValidateOptions<EmailDispatchRetentionSettings>
{
    public ValidateOptionsResult Validate(string? name, EmailDispatchRetentionSettings options)
    {
        var failures = new List<string>();

        if (options.InitialDelaySeconds < 0)
        {
            failures.Add("EmailDispatchRetention:InitialDelaySeconds must be zero or greater.");
        }

        if (options.PollingIntervalMinutes <= 0)
        {
            failures.Add("EmailDispatchRetention:PollingIntervalMinutes must be greater than zero.");
        }

        if (options.BatchSize <= 0)
        {
            failures.Add("EmailDispatchRetention:BatchSize must be greater than zero.");
        }

        if (options.MaxTenantsPerPass <= 0)
        {
            failures.Add("EmailDispatchRetention:MaxTenantsPerPass must be greater than zero.");
        }

        if (options.RetentionDays <= 0)
        {
            failures.Add("EmailDispatchRetention:RetentionDays must be greater than zero.");
        }

        return failures.Count == 0
            ? ValidateOptionsResult.Success
            : ValidateOptionsResult.Fail(failures);
    }
}
