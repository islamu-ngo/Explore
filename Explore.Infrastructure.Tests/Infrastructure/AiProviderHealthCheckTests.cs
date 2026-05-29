// ABOUTME: Unit tests for the AI provider readiness health check adapter.
// ABOUTME: Verifies disabled mode is healthy and unhealthy provider settings are safely surfaced.

using Explore.Application.Telemetry;
using Explore.Infrastructure.Ai;
using Explore.Infrastructure.HealthChecks;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Options;
using NSubstitute;
using System.Diagnostics.Metrics;

namespace Explore.Infrastructure.Tests.Infrastructure;

public sealed class AiProviderHealthCheckTests
{
    [Test]
    public async Task CheckHealthAsync_WhenDisabled_ReturnsHealthy()
    {
        var healthCheck = CreateHealthCheck(new AiProviderSettings());

        var result = await healthCheck.CheckHealthAsync(new HealthCheckContext());

        await Assert.That(result.Status).IsEqualTo(HealthStatus.Healthy);
        await Assert.That(result.Description).Contains("intentionally disabled");
        await Assert.That(result.Data["enabled"]).IsEqualTo(false);
    }

    [Test]
    public async Task CheckHealthAsync_WhenSettingsInvalid_ReturnsUnhealthyWithoutSecrets()
    {
        var healthCheck = CreateHealthCheck(new AiProviderSettings
        {
            Enabled = true,
            Provider = AiProviderSettings.ProviderOpenAiCompatible,
            EndpointUrl = "https://user:password@ai.example.test/v1",
            ApiKey = "secret-key",
            ModelId = "gpt-test"
        });

        var result = await healthCheck.CheckHealthAsync(new HealthCheckContext());

        await Assert.That(result.Status).IsEqualTo(HealthStatus.Unhealthy);
        await Assert.That(result.Data.Keys).DoesNotContain("endpointUrl");
        await Assert.That(result.Data.Keys).DoesNotContain("apiKey");
        await Assert.That(result.Data.Keys).DoesNotContain("modelId");
        await Assert.That(result.Data["apiKeyConfigured"]).IsEqualTo(true);
    }

    private static AiProviderHealthCheck CreateHealthCheck(AiProviderSettings settings)
    {
        var meterFactory = Substitute.For<IMeterFactory>();
        meterFactory.Create(Arg.Any<MeterOptions>()).Returns(new Meter(BusinessMetrics.MeterName));

        return new AiProviderHealthCheck(
            Options.Create(settings),
            new AiProviderHealthReporter(new AiProviderSettingsValidator()),
            new BusinessMetrics(meterFactory));
    }
}
