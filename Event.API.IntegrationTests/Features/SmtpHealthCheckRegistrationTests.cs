// ABOUTME: Tests API SMTP readiness health-check registration metadata.
// ABOUTME: Guards the launch-critical SMTP probe timeout and readiness classification.

using Event.Api.IntegrationTests.Fixtures;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Options;
using TUnit.Core;

namespace ApiIntegrationTests.Features;

[Category(TestCategories.Email)]
public sealed class SmtpHealthCheckRegistrationTests
{
    private static readonly TimeSpan ExpectedSmtpReadinessTimeout = TimeSpan.FromSeconds(5);

    [Test]
    public async Task SmtpReadinessRegistrationUsesBoundedTimeout()
    {
        await using var factory = new CustomWebApplicationFactory();

        var options = factory.Services.GetRequiredService<IOptions<HealthCheckServiceOptions>>().Value;
        var registration = options.Registrations.Single(registration => registration.Name == "smtp");

        registration.Timeout.Should().Be(ExpectedSmtpReadinessTimeout);
        registration.FailureStatus.Should().Be(HealthStatus.Unhealthy);
        registration.Tags.Should().Contain("ready");
        registration.Tags.Should().Contain("smtp");
        registration.Tags.Should().Contain("infrastructure");
    }

    [Test]
    public async Task WebPushReadinessRegistrationUsesReadyDispatchInfrastructureTags()
    {
        await using var factory = new CustomWebApplicationFactory();

        var options = factory.Services.GetRequiredService<IOptions<HealthCheckServiceOptions>>().Value;
        var registration = options.Registrations.Single(registration => registration.Name == "web-push-dispatch");

        registration.FailureStatus.Should().Be(HealthStatus.Unhealthy);
        registration.Tags.Should().Contain("ready");
        registration.Tags.Should().Contain("web-push");
        registration.Tags.Should().Contain("dispatch");
        registration.Tags.Should().Contain("infrastructure");
    }
}
