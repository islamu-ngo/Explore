// ABOUTME: Unit-style tests for TickerQ scheduler startup configuration validation.
// ABOUTME: Proves dashboard and scheduler settings fail fast before unsafe operational exposure.

using Explore.API.Configuration;
using FluentAssertions;
using TUnit.Core;

namespace ApiIntegrationTests.Features;

public sealed class TickerQSchedulerOptionsValidatorTests
{
    private readonly TickerQSchedulerOptionsValidator _validator = new();

    [Test]
    public void ValidateDefaultSettingsReturnsSuccess()
    {
        var result = _validator.Validate(null, new TickerQSchedulerOptions());

        result.Succeeded.Should().BeTrue();
    }

    [Test]
    public void ValidateRejectsInvalidSchedulerShape()
    {
        var result = _validator.Validate(null, new TickerQSchedulerOptions
        {
            Schema = " ",
            MaxConcurrency = 0,
            NodeIdentifier = " "
        });

        result.Succeeded.Should().BeFalse();
        result.FailureMessage.Should().Contain("Schema");
        result.FailureMessage.Should().Contain("MaxConcurrency");
        result.FailureMessage.Should().Contain("NodeIdentifier");
    }

    [Test]
    public void ValidateRejectsSchemaWithoutMatchingMigration()
    {
        var result = _validator.Validate(null, new TickerQSchedulerOptions
        {
            Schema = "scheduler"
        });

        result.Succeeded.Should().BeFalse();
        result.FailureMessage.Should().Contain("must be ticker");
    }

    [Test]
    public void ValidateRejectsDashboardWithoutSafeHostAuthentication()
    {
        var result = _validator.Validate(null, new TickerQSchedulerOptions
        {
            DashboardEnabled = true,
            DashboardPath = "admin/scheduler",
            DashboardAuthorizationPolicy = "AllowAnonymous",
            DashboardSessionTimeoutMinutes = 0
        });

        result.Succeeded.Should().BeFalse();
        result.FailureMessage.Should().Contain("DashboardPath");
        result.FailureMessage.Should().Contain("DashboardAuthorizationPolicy");
        result.FailureMessage.Should().Contain("DashboardSessionTimeoutMinutes");
    }
}
