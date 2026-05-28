// ABOUTME: Startup validation for optional RabbitMQ Dispatch Mode settings.
// ABOUTME: Rejects structurally unsafe broker topology values before side-effect workers run.

using Microsoft.Extensions.Options;

namespace Explore.Infrastructure;

public sealed class EmailDispatchRabbitMqSettingsValidator : IValidateOptions<EmailDispatchRabbitMqSettings>
{
    public ValidateOptionsResult Validate(string? name, EmailDispatchRabbitMqSettings options)
    {
        List<string> failures = [];

        RequireNonBlank(options.ConnectionStringName, nameof(options.ConnectionStringName), failures);
        RequireNonBlank(options.ExchangeName, nameof(options.ExchangeName), failures);
        RequireNonBlank(options.DispatchQueueName, nameof(options.DispatchQueueName), failures);
        RequireNonBlank(options.DispatchRoutingKey, nameof(options.DispatchRoutingKey), failures);
        RequireNonBlank(options.DeadLetterExchangeName, nameof(options.DeadLetterExchangeName), failures);
        RequireNonBlank(options.DeadLetterQueueName, nameof(options.DeadLetterQueueName), failures);
        RequireNonBlank(options.DeadLetterRoutingKey, nameof(options.DeadLetterRoutingKey), failures);
        RequireNonBlank(options.ParkingQueueName, nameof(options.ParkingQueueName), failures);
        RequireNonBlank(options.ParkingRoutingKey, nameof(options.ParkingRoutingKey), failures);
        RequireNonBlank(options.ClientProvidedName, nameof(options.ClientProvidedName), failures);

        if (options.PublishTimeoutSeconds <= 0)
        {
            failures.Add("PublishTimeoutSeconds must be greater than zero.");
        }

        return failures.Count == 0
            ? ValidateOptionsResult.Success
            : ValidateOptionsResult.Fail(failures);
    }

    private static void RequireNonBlank(string? value, string propertyName, List<string> failures)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            failures.Add($"{propertyName} is required.");
        }
    }
}
