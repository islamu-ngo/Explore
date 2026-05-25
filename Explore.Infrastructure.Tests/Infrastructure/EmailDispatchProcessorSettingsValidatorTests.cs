// ABOUTME: Unit tests for EmailDispatchProcessorSettingsValidator.
// ABOUTME: Verifies Basic Dispatch Mode startup validation rejects unsafe worker settings.

using Explore.Infrastructure;

namespace Explore.Infrastructure.Tests.Infrastructure;

public sealed class EmailDispatchProcessorSettingsValidatorTests
{
    private readonly EmailDispatchProcessorSettingsValidator _validator = new();

    [Test]
    public async Task ValidateDefaultSettingsReturnsSuccess()
    {
        var result = _validator.Validate(null, new EmailDispatchProcessorSettings());

        await Assert.That(result.Succeeded).IsTrue();
    }

    [Test]
    public async Task ValidateInvalidPollingIntervalReturnsFailure()
    {
        var result = _validator.Validate(null, new EmailDispatchProcessorSettings
        {
            PollingIntervalSeconds = 0
        });

        await Assert.That(result.Succeeded).IsFalse();
        await Assert.That(result.FailureMessage).Contains("PollingIntervalSeconds");
    }

    [Test]
    public async Task ValidateInvalidBatchSizeReturnsFailure()
    {
        var result = _validator.Validate(null, new EmailDispatchProcessorSettings
        {
            BatchSize = 0
        });

        await Assert.That(result.Succeeded).IsFalse();
        await Assert.That(result.FailureMessage).Contains("BatchSize");
    }

    [Test]
    public async Task ValidateInvalidRetryWindowReturnsFailure()
    {
        var result = _validator.Validate(null, new EmailDispatchProcessorSettings
        {
            InitialRetryDelaySeconds = 60,
            MaxRetryDelaySeconds = 10
        });

        await Assert.That(result.Succeeded).IsFalse();
        await Assert.That(result.FailureMessage).Contains("MaxRetryDelaySeconds");
    }

    [Test]
    public async Task ValidateMissingConsumerIdReturnsFailure()
    {
        var result = _validator.Validate(null, new EmailDispatchProcessorSettings
        {
            ConsumerId = " "
        });

        await Assert.That(result.Succeeded).IsFalse();
        await Assert.That(result.FailureMessage).Contains("ConsumerId");
    }
}
