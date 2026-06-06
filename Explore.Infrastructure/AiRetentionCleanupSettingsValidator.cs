// ABOUTME: Startup validator for scheduled AI assistant retention cleanup settings.
// ABOUTME: Fails fast when scheduling or tenant batch bounds are unsafe.

using Microsoft.Extensions.Options;

namespace Explore.Infrastructure;

public sealed class AiRetentionCleanupSettingsValidator : IValidateOptions<AiRetentionCleanupSettings>
{
    public ValidateOptionsResult Validate(string? name, AiRetentionCleanupSettings options)
    {
        var failures = new List<string>();

        if (options.InitialDelaySeconds < 0)
        {
            failures.Add("AiRetentionCleanup:InitialDelaySeconds must be zero or greater.");
        }

        if (options.PollingIntervalMinutes <= 0)
        {
            failures.Add("AiRetentionCleanup:PollingIntervalMinutes must be greater than zero.");
        }

        if (options.MaxTenantsPerPass <= 0)
        {
            failures.Add("AiRetentionCleanup:MaxTenantsPerPass must be greater than zero.");
        }

        return failures.Count == 0
            ? ValidateOptionsResult.Success
            : ValidateOptionsResult.Fail(failures);
    }
}
