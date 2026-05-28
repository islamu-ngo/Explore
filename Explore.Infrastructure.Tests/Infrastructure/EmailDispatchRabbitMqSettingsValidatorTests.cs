// ABOUTME: Unit tests for EmailDispatchRabbitMqSettingsValidator.
// ABOUTME: Verifies optional RabbitMQ Dispatch Mode rejects unsafe topology settings.

using Explore.Infrastructure;

namespace Explore.Infrastructure.Tests.Infrastructure;

public sealed class EmailDispatchRabbitMqSettingsValidatorTests
{
    private readonly EmailDispatchRabbitMqSettingsValidator _validator = new();

    [Test]
    public async Task ValidateDefaultSettingsReturnsSuccess()
    {
        var result = _validator.Validate(null, new EmailDispatchRabbitMqSettings());

        await Assert.That(result.Succeeded).IsTrue();
    }

    [Test]
    public async Task ValidateBlankExchangeNameReturnsFailure()
    {
        var result = _validator.Validate(null, new EmailDispatchRabbitMqSettings
        {
            ExchangeName = " "
        });

        await Assert.That(result.Succeeded).IsFalse();
        await Assert.That(result.FailureMessage).Contains(nameof(EmailDispatchRabbitMqSettings.ExchangeName));
    }

    [Test]
    public async Task ValidateInvalidPublishTimeoutReturnsFailure()
    {
        var result = _validator.Validate(null, new EmailDispatchRabbitMqSettings
        {
            PublishTimeoutSeconds = 0
        });

        await Assert.That(result.Succeeded).IsFalse();
        await Assert.That(result.FailureMessage).Contains(nameof(EmailDispatchRabbitMqSettings.PublishTimeoutSeconds));
    }

    [Test]
    public async Task ValidateBlankConnectionStringNameReturnsFailure()
    {
        var result = _validator.Validate(null, new EmailDispatchRabbitMqSettings
        {
            ConnectionStringName = string.Empty
        });

        await Assert.That(result.Succeeded).IsFalse();
        await Assert.That(result.FailureMessage).Contains(nameof(EmailDispatchRabbitMqSettings.ConnectionStringName));
    }
}
