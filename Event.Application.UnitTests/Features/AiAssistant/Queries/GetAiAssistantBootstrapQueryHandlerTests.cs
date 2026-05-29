// ABOUTME: Unit tests for AI assistant bootstrap query settings resolution.
// ABOUTME: Verifies safe availability, disabled reasons, limits, and model metadata without secrets.

using Explore.Application.Contracts.Infrastructure;
using Explore.Application.Contracts.Infrastructure.Ai;
using Explore.Application.Features.AiAssistant.Handlers.Queries;
using Explore.Application.Features.AiAssistant.Requests.Queries;
using Explore.Application.Settings;
using Explore.Application.Settings.Groups;
using Explore.Domain.Constants;
using NSubstitute;

namespace Event.Application.UnitTests.Features.AiAssistant.Queries;

public sealed class GetAiAssistantBootstrapQueryHandlerTests
{
    private readonly ITenantContext _tenantContext = Substitute.For<ITenantContext>();
    private readonly IHierarchicalSettingsResolver _settingsResolver = Substitute.For<IHierarchicalSettingsResolver>();

    [Test]
    public async Task Handle_WhenDisabled_ReturnsDisabledBootstrapWithoutModels()
    {
        var tenantId = Guid.NewGuid();
        var handler = CreateHandler(tenantId, CreateSettings());

        var result = await handler.Handle(new GetAiAssistantBootstrapQuery(), CancellationToken.None);

        await Assert.That(result.TenantId).IsEqualTo(tenantId);
        await Assert.That(result.Enabled).IsFalse();
        await Assert.That(result.Available).IsFalse();
        await Assert.That(result.DisabledReason).IsEqualTo("disabled");
        await Assert.That(result.Models.Count).IsEqualTo(0);
        await Assert.That(result.Features.ToolProposalsEnabled).IsFalse();
    }

    [Test]
    public async Task Handle_WhenFakeProviderEnabled_ReturnsDeterministicFakeModel()
    {
        var handler = CreateHandler(Guid.NewGuid(), CreateSettings(
            enabled: true,
            provider: AiProviderDefaults.ProviderFake,
            toolProposalsEnabled: true,
            streamingEnabled: true));

        var result = await handler.Handle(new GetAiAssistantBootstrapQuery(), CancellationToken.None);

        await Assert.That(result.Available).IsTrue();
        await Assert.That(result.DisabledReason).IsNull();
        await Assert.That(result.DefaultModelId).IsEqualTo(AiProviderDefaults.FakeModelId);
        await Assert.That(result.Models.Count).IsEqualTo(1);
        await Assert.That(result.Models[0].SupportsToolProposals).IsTrue();
        await Assert.That(result.Models[0].SupportsStreaming).IsFalse();
        await Assert.That(result.Features.ToolProposalsEnabled).IsTrue();
        await Assert.That(result.Features.StreamingEnabled).IsTrue();
    }

    [Test]
    public async Task Handle_WhenOpenAiCompatibleMissingEndpoint_ReturnsEndpointDisabledReason()
    {
        var handler = CreateHandler(Guid.NewGuid(), CreateSettings(
            enabled: true,
            provider: AiProviderDefaults.ProviderOpenAiCompatible,
            apiKey: "secret",
            modelId: "gpt-test"));

        var result = await handler.Handle(new GetAiAssistantBootstrapQuery(), CancellationToken.None);

        await Assert.That(result.Available).IsFalse();
        await Assert.That(result.DisabledReason).IsEqualTo("endpoint_not_configured");
        await Assert.That(result.Models.Count).IsEqualTo(1);
    }

    [Test]
    public async Task Handle_WhenOpenAiCompatibleConfigured_ReturnsConfiguredModelAndLimits()
    {
        var handler = CreateHandler(Guid.NewGuid(), CreateSettings(
            enabled: true,
            provider: AiProviderDefaults.ProviderOpenAiCompatible,
            endpointUrl: "https://ai.example.test/v1",
            apiKey: "secret",
            modelId: "gpt-test",
            maxInputTokens: 16000,
            maxOutputTokens: 2048,
            temperature: 0.4m,
            timeoutSeconds: 45,
            retentionDays: 14,
            dailyMessageLimit: 25,
            toolProposalsEnabled: true));

        var result = await handler.Handle(new GetAiAssistantBootstrapQuery(), CancellationToken.None);

        await Assert.That(result.Available).IsTrue();
        await Assert.That(result.Provider).IsEqualTo(AiProviderDefaults.ProviderOpenAiCompatible);
        await Assert.That(result.DefaultModelId).IsEqualTo("gpt-test");
        await Assert.That(result.Models[0].DisplayName).IsEqualTo("gpt-test");
        await Assert.That(result.Limits.MaxInputTokens).IsEqualTo(16000);
        await Assert.That(result.Limits.MaxOutputTokens).IsEqualTo(2048);
        await Assert.That(result.Limits.Temperature).IsEqualTo(0.4m);
        await Assert.That(result.Limits.TimeoutSeconds).IsEqualTo(45);
        await Assert.That(result.Limits.DailyMessageLimit).IsEqualTo(25);
        await Assert.That(result.RetentionDays).IsEqualTo(14);
        await Assert.That(result.Features.ToolProposalsEnabled).IsTrue();
        await Assert.That(result.Features.StreamingEnabled).IsFalse();
    }

    private GetAiAssistantBootstrapQueryHandler CreateHandler(Guid tenantId, AiAssistantSettingGroup settings)
    {
        _tenantContext.TenantId.Returns(tenantId);
        _settingsResolver.ResolveGroupAsync<AiAssistantSettingGroup>(Arg.Any<SettingContext>(), Arg.Any<CancellationToken>())
            .Returns(settings);

        return new GetAiAssistantBootstrapQueryHandler(_tenantContext, _settingsResolver);
    }

    private static AiAssistantSettingGroup CreateSettings(
        bool enabled = false,
        string provider = AiProviderDefaults.ProviderNone,
        string endpointUrl = "",
        string apiKey = "",
        string modelId = "",
        int maxInputTokens = 8000,
        int maxOutputTokens = 1024,
        decimal temperature = 0.2m,
        int timeoutSeconds = 30,
        int retentionDays = 30,
        int dailyMessageLimit = 50,
        bool toolProposalsEnabled = false,
        bool streamingEnabled = false)
    {
        var settings = new Dictionary<string, ResolvedSetting>
        {
            [GovernanceSettingKeys.AiAssistant.Enabled] = Setting(GovernanceSettingKeys.AiAssistant.Enabled, enabled),
            [GovernanceSettingKeys.AiAssistant.Provider] = Setting(GovernanceSettingKeys.AiAssistant.Provider, provider),
            [GovernanceSettingKeys.AiAssistant.EndpointUrl] = Setting(GovernanceSettingKeys.AiAssistant.EndpointUrl, endpointUrl),
            [GovernanceSettingKeys.AiAssistant.ApiKey] = Setting(GovernanceSettingKeys.AiAssistant.ApiKey, apiKey),
            [GovernanceSettingKeys.AiAssistant.ModelId] = Setting(GovernanceSettingKeys.AiAssistant.ModelId, modelId),
            [GovernanceSettingKeys.AiAssistant.MaxInputTokens] = Setting(GovernanceSettingKeys.AiAssistant.MaxInputTokens, maxInputTokens),
            [GovernanceSettingKeys.AiAssistant.MaxOutputTokens] = Setting(GovernanceSettingKeys.AiAssistant.MaxOutputTokens, maxOutputTokens),
            [GovernanceSettingKeys.AiAssistant.Temperature] = Setting(GovernanceSettingKeys.AiAssistant.Temperature, temperature),
            [GovernanceSettingKeys.AiAssistant.TimeoutSeconds] = Setting(GovernanceSettingKeys.AiAssistant.TimeoutSeconds, timeoutSeconds),
            [GovernanceSettingKeys.AiAssistant.RetentionDays] = Setting(GovernanceSettingKeys.AiAssistant.RetentionDays, retentionDays),
            [GovernanceSettingKeys.AiAssistant.DailyMessageLimit] = Setting(GovernanceSettingKeys.AiAssistant.DailyMessageLimit, dailyMessageLimit),
            [GovernanceSettingKeys.AiAssistant.ToolProposalsEnabled] = Setting(GovernanceSettingKeys.AiAssistant.ToolProposalsEnabled, toolProposalsEnabled),
            [GovernanceSettingKeys.AiAssistant.StreamingEnabled] = Setting(GovernanceSettingKeys.AiAssistant.StreamingEnabled, streamingEnabled)
        };

        var group = new AiAssistantSettingGroup();
        group.Populate(settings);
        return group;
    }

    private static ResolvedSetting Setting(string key, object value) => new()
    {
        Key = key,
        Value = System.Text.Json.JsonSerializer.Serialize(value),
        Source = SettingSource.SystemDefault,
        IsLocked = false
    };
}
