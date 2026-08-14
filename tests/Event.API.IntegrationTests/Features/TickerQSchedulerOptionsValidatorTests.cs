// ABOUTME: Unit-style tests for TickerQ scheduler startup configuration validation.
// ABOUTME: Proves dashboard and scheduler settings fail fast before unsafe operational exposure.

using Explore.API.Configuration;
using TUnit.Core;

namespace ApiIntegrationTests.Features;

public sealed class TickerQSchedulerOptionsValidatorTests
{
    private readonly TickerQSchedulerOptionsValidator _validator = new();

    [Test]
    public async Task ValidateDefaultSettingsReturnsSuccess()
    {
        var result = _validator.Validate(null, new TickerQSchedulerOptions());

        await Assert.That(result.Succeeded).IsTrue();
    }

    [Test]
    public async Task ValidateRejectsInvalidSchedulerShape()
    {
        var result = _validator.Validate(null, new TickerQSchedulerOptions
        {
            Schema = " ",
            MaxConcurrency = 0,
            NodeIdentifier = " "
        });

        await Assert.That(result.Succeeded).IsFalse();
        await Assert.That(result.FailureMessage).Contains("Schema");
        await Assert.That(result.FailureMessage).Contains("MaxConcurrency");
        await Assert.That(result.FailureMessage).Contains("NodeIdentifier");
    }

    [Test]
    public async Task ValidateRejectsSchemaWithoutMatchingMigration()
    {
        var result = _validator.Validate(null, new TickerQSchedulerOptions
        {
            Schema = "scheduler"
        });

        await Assert.That(result.Succeeded).IsFalse();
        await Assert.That(result.FailureMessage).Contains("must be ticker");
    }

    [Test]
    public async Task ValidateRejectsDashboardWithoutSafeHostAuthentication()
    {
        var result = _validator.Validate(null, new TickerQSchedulerOptions
        {
            DashboardEnabled = true,
            DashboardPath = "admin/scheduler",
            DashboardAuthorizationPolicy = "AllowAnonymous",
            DashboardSessionTimeoutMinutes = 0
        });

        await Assert.That(result.Succeeded).IsFalse();
        await Assert.That(result.FailureMessage).Contains("DashboardPath");
        await Assert.That(result.FailureMessage).Contains("DashboardAuthorizationPolicy");
        await Assert.That(result.FailureMessage).Contains("DashboardSessionTimeoutMinutes");
    }
}
