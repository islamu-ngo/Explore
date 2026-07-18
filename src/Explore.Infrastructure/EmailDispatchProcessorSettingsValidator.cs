// ABOUTME: Startup validator for Basic Dispatch Mode email dispatch settings.
// ABOUTME: Fails fast on invalid polling, batch, retry, or consumer identity configuration.

using Microsoft.Extensions.Options;

namespace Explore.Infrastructure;

public sealed class EmailDispatchProcessorSettingsValidator : IValidateOptions<EmailDispatchProcessorSettings>
{
    public ValidateOptionsResult Validate(string? name, EmailDispatchProcessorSettings options)
    {
        var failures = new List<string>();

        if (!Enum.IsDefined(options.Mode))
        {
            failures.Add("EmailDispatchProcessor:Mode must be Disabled, TickerQ, or HostedService.");
        }

        if (options.PollingIntervalSeconds <= 0)
        {
            failures.Add("EmailDispatchProcessor:PollingIntervalSeconds must be greater than zero.");
        }

        if (options.BatchSize <= 0)
        {
            failures.Add("EmailDispatchProcessor:BatchSize must be greater than zero.");
        }

        if (options.MaxRowsPerTenantPerBatch is <= 0 || options.MaxRowsPerTenantPerBatch > options.BatchSize)
        {
            failures.Add("EmailDispatchProcessor:MaxRowsPerTenantPerBatch must be between 1 and BatchSize.");
        }

        if (options.MaxConcurrentDispatches <= 0)
        {
            failures.Add("EmailDispatchProcessor:MaxConcurrentDispatches must be greater than zero.");
        }

        if (options.MaxConcurrentDispatchesPerTenant is <= 0 ||
            options.MaxConcurrentDispatchesPerTenant > options.MaxConcurrentDispatches)
        {
            failures.Add("EmailDispatchProcessor:MaxConcurrentDispatchesPerTenant must be between 1 and MaxConcurrentDispatches.");
        }

        if (options.SmtpRateLimitPerMinute <= 0)
        {
            failures.Add("EmailDispatchProcessor:SmtpRateLimitPerMinute must be greater than zero.");
        }

        if (options.OptionalBacklogLowWatermark < 0 ||
            options.OptionalBacklogHighWatermark <= options.OptionalBacklogLowWatermark)
        {
            failures.Add("EmailDispatchProcessor:OptionalBacklogLowWatermark must be zero or greater and lower than OptionalBacklogHighWatermark.");
        }

        if (options.MaxAttemptCount <= 0)
        {
            failures.Add("EmailDispatchProcessor:MaxAttemptCount must be greater than zero.");
        }

        if (options.InitialRetryDelaySeconds <= 0)
        {
            failures.Add("EmailDispatchProcessor:InitialRetryDelaySeconds must be greater than zero.");
        }

        if (options.MaxRetryDelaySeconds < options.InitialRetryDelaySeconds)
        {
            failures.Add("EmailDispatchProcessor:MaxRetryDelaySeconds must be greater than or equal to InitialRetryDelaySeconds.");
        }

        if (options.ProcessingLeaseTimeoutSeconds <= 0)
        {
            failures.Add("EmailDispatchProcessor:ProcessingLeaseTimeoutSeconds must be greater than zero.");
        }

        if (options.HealthDueDispatchWarningThreshold is < 1 or > 100000)
        {
            failures.Add("EmailDispatchProcessor:HealthDueDispatchWarningThreshold must be between 1 and 100000.");
        }

        if (options.HealthStaleProcessingWarningThreshold is < 1 or > 10000)
        {
            failures.Add("EmailDispatchProcessor:HealthStaleProcessingWarningThreshold must be between 1 and 10000.");
        }

        if (options.HealthDeadLetterWarningThreshold is < 1 or > 10000)
        {
            failures.Add("EmailDispatchProcessor:HealthDeadLetterWarningThreshold must be between 1 and 10000.");
        }

        if (options.HealthOldestPendingWarningSeconds <= 0)
        {
            failures.Add("EmailDispatchProcessor:HealthOldestPendingWarningSeconds must be greater than zero.");
        }

        if (options.HealthTenantBacklogWarningThreshold <= 0)
        {
            failures.Add("EmailDispatchProcessor:HealthTenantBacklogWarningThreshold must be greater than zero.");
        }

        if (options.HealthTenantSampleLimit is < 1 or > 100)
        {
            failures.Add("EmailDispatchProcessor:HealthTenantSampleLimit must be between 1 and 100.");
        }

        if (string.IsNullOrWhiteSpace(options.ConsumerId))
        {
            failures.Add("EmailDispatchProcessor:ConsumerId is required when Basic Dispatch Mode is configured.");
        }

        return failures.Count == 0
            ? ValidateOptionsResult.Success
            : ValidateOptionsResult.Fail(failures);
    }
}
