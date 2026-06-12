// ABOUTME: Unit tests for AI provider readiness reporting and safe health data.
// ABOUTME: Ensures health output surfaces configuration state without leaking endpoints or secrets.

using Explore.Application.Contracts.Infrastructure.Ai;
using Explore.Infrastructure.Ai;
using Microsoft.Extensions.Logging;
using NSubstitute;

namespace Explore.Infrastructure.Tests.Infrastructure;

public sealed class AiProviderHealthReporterTests
{
    private readonly AiProviderHealthReporter _reporter = CreateReporter();

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

    private static AiProviderHealthReporter CreateReporter()
    {
        var validator = new AiProviderSettingsValidator();
        var strategies = new IAiProviderStrategy[]
        {
            new FakeAiProviderStrategy(new FakeAiChatProvider()),
            new StubProviderStrategy(AiProviderSettings.ProviderOpenAiCompatible, "configured_no_probe",
                "OpenAI-compatible AI provider settings are valid; network probing is deferred to the adapter."),
            new StubProviderStrategy(AiProviderSettings.ProviderAnthropicCompatible, "configured_no_probe",
                "Anthropic-compatible AI provider settings are valid; network probing is deferred to the adapter."),
            new StubProviderStrategy(AiProviderSettings.ProviderOpenAiSdk, "configured_no_probe",
                "SDK-backed AI provider settings are valid; network probing is deferred to the adapter.",
                new[] { AiProviderSettings.ProviderOpenAiSdk, AiProviderSettings.ProviderAzureOpenAi }),
        };
        var resolver = new AiProviderStrategyResolver(
            strategies, Substitute.For<ILogger<AiProviderStrategyResolver>>());
        return new AiProviderHealthReporter(validator, resolver);
    }

    private sealed class StubProviderStrategy : IAiProviderStrategy
    {
        private readonly HashSet<int> _supportedProviders;
        private readonly string _healthStatus;
        private readonly string _healthDescription;

        public StubProviderStrategy(int providerId, string healthStatus, string healthDescription,
            int[]? supportedProviders = null)
        {
            _supportedProviders = new HashSet<int>(
                supportedProviders ?? new[] { providerId });
            _healthStatus = healthStatus;
            _healthDescription = healthDescription;
        }

        public int ProviderId => _supportedProviders.First();
        public bool SupportsProvider(int providerId) => _supportedProviders.Contains(providerId);
        public Task<AiChatProviderResult> SendAsync(AiChatPayload request, CancellationToken ct = default) =>
            throw new NotSupportedException();
        public Task<IReadOnlyList<AiModelDescriptor>> ListAvailableModelsAsync(CancellationToken ct = default) =>
            Task.FromResult<IReadOnlyList<AiModelDescriptor>>(Array.Empty<AiModelDescriptor>());
        public AiProviderHealth CheckHealth(IReadOnlyDictionary<string, object> data) =>
            new(true, true, _healthStatus, _healthDescription, data);
    }
}
