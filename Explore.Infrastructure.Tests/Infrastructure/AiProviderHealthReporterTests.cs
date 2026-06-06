// ABOUTME: Unit tests for AI provider readiness reporting and safe health data.
// ABOUTME: Ensures health output surfaces configuration state without leaking endpoints or secrets.

using Explore.Infrastructure.Ai;

namespace Explore.Infrastructure.Tests.Infrastructure;

public sealed class AiProviderHealthReporterTests
{
    private readonly AiProviderHealthReporter _reporter = new(new AiProviderSettingsValidator());

    [Test]
    public async Task Check_WhenDisabled_ReturnsHealthyDisabled()
    {
        var health = _reporter.Check(new AiProviderSettings());

        await Assert.That(health.Healthy).IsTrue();
        await Assert.That(health.Status).IsEqualTo("healthy_disabled");
        await Assert.That(health.Data["enabled"]).IsEqualTo(false);
    }

    [Test]
    public async Task Check_WhenEnabledWithoutProvider_ReturnsUnhealthy()
    {
        var health = _reporter.Check(new AiProviderSettings
        {
            Enabled = true
        });

        await Assert.That(health.Healthy).IsFalse();
        await Assert.That(health.Status).IsEqualTo("provider_not_configured");
    }

    [Test]
    public async Task Check_WhenFakeProviderEnabled_ReturnsHealthyFake()
    {
        var health = _reporter.Check(new AiProviderSettings
        {
            Enabled = true,
            Provider = AiProviderSettings.ProviderFake
        });

        await Assert.That(health.Healthy).IsTrue();
        await Assert.That(health.Status).IsEqualTo("healthy_fake");
    }

    [Test]
    public async Task Check_WhenOpenAiCompatibleConfigured_ReturnsConfiguredNoProbe()
    {
        var health = _reporter.Check(new AiProviderSettings
        {
            Enabled = true,
            Provider = AiProviderSettings.ProviderOpenAiCompatible,
            EndpointUrl = "https://ai.example.test/v1",
            ApiKey = "test-key",
            ModelId = "gpt-test"
        });

        await Assert.That(health.Healthy).IsTrue();
        await Assert.That(health.Status).IsEqualTo("configured_no_probe");
    }

    [Test]
    public async Task Check_WhenSdkBackedProviderConfigured_ReturnsConfiguredNoProbeWithoutSecrets()
    {
        var health = _reporter.Check(new AiProviderSettings
        {
            Enabled = true,
            Provider = AiProviderSettings.ProviderAzureOpenAi,
            EndpointUrl = "https://ai.example.openai.azure.com/",
            ApiKey = "secret-key",
            ModelId = "deployment-test"
        });

        await Assert.That(health.Healthy).IsTrue();
        await Assert.That(health.Status).IsEqualTo("configured_no_probe");
        await Assert.That(health.Data.Keys).DoesNotContain("endpointUrl");
        await Assert.That(health.Data.Keys).DoesNotContain("apiKey");
        await Assert.That(health.Data.Keys).DoesNotContain("modelId");
        await Assert.That(health.Data["endpointConfigured"]).IsEqualTo(true);
        await Assert.That(health.Data["apiKeyConfigured"]).IsEqualTo(true);
        await Assert.That(health.Data["modelConfigured"]).IsEqualTo(true);
    }

    [Test]
    public async Task Check_WhenEndpointUnsafe_ReturnsInvalidSettingsWithoutLeakingEndpointOrKey()
    {
        var health = _reporter.Check(new AiProviderSettings
        {
            Enabled = true,
            Provider = AiProviderSettings.ProviderOpenAiCompatible,
            EndpointUrl = "https://user:secret@ai.example.test/v1?prompt=leak",
            ApiKey = "secret-key",
            ModelId = "gpt-test"
        });

        await Assert.That(health.Healthy).IsFalse();
        await Assert.That(health.Status).IsEqualTo("invalid_settings");
        await Assert.That(health.Data.Keys).DoesNotContain("endpointUrl");
        await Assert.That(health.Data.Keys).DoesNotContain("apiKey");
        await Assert.That(health.Data.Keys).DoesNotContain("modelId");
        await Assert.That(health.Data.Keys).DoesNotContain("prompt");
        await Assert.That(health.Data["endpointConfigured"]).IsEqualTo(true);
        await Assert.That(health.Data["apiKeyConfigured"]).IsEqualTo(true);
        await Assert.That(health.Data["modelConfigured"]).IsEqualTo(true);
    }
}
