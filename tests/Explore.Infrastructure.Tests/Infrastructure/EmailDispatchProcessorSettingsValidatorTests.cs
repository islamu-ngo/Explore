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
    public async Task ValidateUnsafeFairnessRateAndBackpressureLimitsReturnsFailure()
    {
        var result = _validator.Validate(null, new EmailDispatchProcessorSettings
        {
            BatchSize = 10,
            MaxRowsPerTenantPerBatch = 11,
            MaxConcurrentDispatches = 0,
            MaxConcurrentDispatchesPerTenant = 2,
            SmtpRateLimitPerMinute = 0,
            OptionalBacklogHighWatermark = 10,
            OptionalBacklogLowWatermark = 10,
            HealthOldestPendingWarningSeconds = 0,
            HealthTenantBacklogWarningThreshold = 0,
            HealthTenantSampleLimit = 0
        });

        await Assert.That(result.Succeeded).IsFalse();
        await Assert.That(result.FailureMessage).Contains("MaxRowsPerTenantPerBatch");
        await Assert.That(result.FailureMessage).Contains("MaxConcurrentDispatches");
        await Assert.That(result.FailureMessage).Contains("MaxConcurrentDispatchesPerTenant");
        await Assert.That(result.FailureMessage).Contains("SmtpRateLimitPerMinute");
        await Assert.That(result.FailureMessage).Contains("OptionalBacklogLowWatermark");
        await Assert.That(result.FailureMessage).Contains("HealthOldestPendingWarningSeconds");
        await Assert.That(result.FailureMessage).Contains("HealthTenantBacklogWarningThreshold");
        await Assert.That(result.FailureMessage).Contains("HealthTenantSampleLimit");
    }

    [Test]
    public async Task ValidateInvalidModeReturnsFailure()
    {
        var result = _validator.Validate(null, new EmailDispatchProcessorSettings
        {
            Mode = (EmailDispatchProcessorMode)999
        });

        await Assert.That(result.Succeeded).IsFalse();
        await Assert.That(result.FailureMessage).Contains("Mode");
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
    public async Task ValidateInvalidProcessingLeaseTimeoutReturnsFailure()
    {
        var result = _validator.Validate(null, new EmailDispatchProcessorSettings
        {
            ProcessingLeaseTimeoutSeconds = 0
        });

        await Assert.That(result.Succeeded).IsFalse();
        await Assert.That(result.FailureMessage).Contains("ProcessingLeaseTimeoutSeconds");
    }

    [Test]
    public async Task ValidateInvalidDueDispatchHealthThresholdReturnsFailure()
    {
        var result = _validator.Validate(null, new EmailDispatchProcessorSettings
        {
            HealthDueDispatchWarningThreshold = 0
        });

        await Assert.That(result.Succeeded).IsFalse();
        await Assert.That(result.FailureMessage).Contains("HealthDueDispatchWarningThreshold");
    }

    [Test]
    public async Task ValidateInvalidStaleProcessingHealthThresholdReturnsFailure()
    {
        var result = _validator.Validate(null, new EmailDispatchProcessorSettings
        {
            HealthStaleProcessingWarningThreshold = 0
        });

        await Assert.That(result.Succeeded).IsFalse();
        await Assert.That(result.FailureMessage).Contains("HealthStaleProcessingWarningThreshold");
    }

    [Test]
    public async Task ValidateInvalidDeadLetterHealthThresholdReturnsFailure()
    {
        var result = _validator.Validate(null, new EmailDispatchProcessorSettings
        {
            HealthDeadLetterWarningThreshold = 0
        });

        await Assert.That(result.Succeeded).IsFalse();
        await Assert.That(result.FailureMessage).Contains("HealthDeadLetterWarningThreshold");
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
