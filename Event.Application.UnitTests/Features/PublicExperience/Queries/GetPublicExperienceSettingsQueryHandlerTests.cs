using Explore.Application.Analytics;
using Explore.Application.Contracts.Infrastructure;
using Explore.Application.Contracts.Persistence;
using Explore.Application.Contracts.Services;
using Explore.Application.DTOs.Onboarding;
using Explore.Application.Features.PublicExperience.Handlers.Queries;
using Explore.Application.Features.PublicExperience.Requests.Queries;
using Explore.Application.Models;
using Explore.Application.Services;
using Explore.Application.Settings;
using Explore.Application.Settings.Groups;
using Explore.Domain;
using Explore.Domain.Constants;
using NSubstitute;

namespace Event.Application.UnitTests.Features.PublicExperience.Queries;

public class GetPublicExperienceSettingsQueryHandlerTests
{
    private readonly ITenantContext _tenantContext;
    private readonly ISystemSettingRepository _systemSettingRepository;
    private readonly IAnalyticsConfigResolver _analyticsConfigResolver;
    private readonly ITenantPolicySettingService _policySettingService;
    private readonly IModuleService _moduleService;
    private readonly IInstanceGovernanceSettingService _instanceGovernanceSettingService;
    private readonly IAnalyticsGovernanceService _analyticsGovernanceService;
    private readonly IAnalyticsRuntimeProfileResolver _runtimeProfileResolver;
    private readonly IHierarchicalSettingsResolver _hierarchicalSettingsResolver;
    private readonly GetPublicExperienceSettingsQueryHandler _handler;

    public GetPublicExperienceSettingsQueryHandlerTests()
    {
        _tenantContext = Substitute.For<ITenantContext>();
        _systemSettingRepository = Substitute.For<ISystemSettingRepository>();
        _analyticsConfigResolver = Substitute.For<IAnalyticsConfigResolver>();
        _policySettingService = Substitute.For<ITenantPolicySettingService>();
        _moduleService = Substitute.For<IModuleService>();
        _instanceGovernanceSettingService = Substitute.For<IInstanceGovernanceSettingService>();
        _analyticsGovernanceService = new AnalyticsGovernanceService();
        _runtimeProfileResolver = Substitute.For<IAnalyticsRuntimeProfileResolver>();
        _runtimeProfileResolver.Resolve(Arg.Any<AnalyticsSettingGroup>())
            .Returns(new AnalyticsRuntimeProfile());
        _hierarchicalSettingsResolver = Substitute.For<IHierarchicalSettingsResolver>();
        _hierarchicalSettingsResolver.ResolveGroupAsync<AnalyticsSettingGroup>(
            Arg.Any<SettingContext>(), Arg.Any<CancellationToken>())
            .Returns(new AnalyticsSettingGroup());
        _instanceGovernanceSettingService.ReadEffectiveSettingsForTenantAsync(Arg.Any<Guid>()).Returns(new InstanceGovernanceSettingsDto());
        _analyticsConfigResolver.ResolveAsync(Arg.Any<CancellationToken>()).Returns(new AnalyticsConfiguration());

        _handler = new GetPublicExperienceSettingsQueryHandler(
            _tenantContext,
            _systemSettingRepository,
            _analyticsConfigResolver,
            _policySettingService,
            _moduleService,
            _instanceGovernanceSettingService,
            _analyticsGovernanceService,
            _runtimeProfileResolver,
            _hierarchicalSettingsResolver);
    }

    [Test]
    public async Task Handle_WithEnabledIslamicAndTechModules_SetsCapabilityFlagsAndEnabledModuleList()
    {
        // Arrange
        var tenantId = Guid.NewGuid();
        _tenantContext.TenantId.Returns(tenantId);

        _policySettingService.ReadEffectiveTenantSettingsAsync(tenantId).Returns(new TenantPolicySettingsDto
        {
            PreferredHomePage = "EventList",
            BrandDisplayName = "Tenant Brand"
        });

        _moduleService.GetEnabledModulesAsync(tenantId, Arg.Any<CancellationToken>())
            .Returns(new List<ModuleInfo>
            {
                new() { ModuleKey = "Mod_Islamic", Name = "Islamic" },
                new() { ModuleKey = "Mod_Tech", Name = "Tech" },
                new() { ModuleKey = "Mod_Other", Name = "Other" }
            });

        _systemSettingRepository.GetByKey(GovernanceSettingKeys.DeploymentMode).Returns(new SystemSetting
        {
            SettingKey = GovernanceSettingKeys.DeploymentMode,
            Value = "\"MultiTenant\""
        });

        // Act
        var result = await _handler.Handle(new GetPublicExperienceSettingsQuery(), CancellationToken.None);

        // Assert
        await Assert.That(result).IsNotNull();
        await Assert.That(result.TenantId).IsEqualTo(tenantId);
        await Assert.That(result.DeploymentMode).IsEqualTo("MultiTenant");
        await Assert.That(result.IsIslamicModuleEnabled).IsTrue();
        await Assert.That(result.IsTechModuleEnabled).IsTrue();
        await Assert.That(result.EnabledModules).Contains("Mod_Islamic");
        await Assert.That(result.EnabledModules).Contains("Mod_Tech");
        await Assert.That(result.EnabledModules).Contains("Mod_Other");

        await _moduleService.Received(1).GetEnabledModulesAsync(tenantId, Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task Handle_WhenDeploymentModeSettingIsMissing_DefaultsToSingleTenant()
    {
        // Arrange
        var tenantId = Guid.NewGuid();
        _tenantContext.TenantId.Returns(tenantId);

        _policySettingService.ReadEffectiveTenantSettingsAsync(tenantId).Returns(new TenantPolicySettingsDto());
        _moduleService.GetEnabledModulesAsync(tenantId, Arg.Any<CancellationToken>())
            .Returns(new List<ModuleInfo>());
        _systemSettingRepository.GetByKey(GovernanceSettingKeys.DeploymentMode).Returns((SystemSetting?)null);

        // Act
        var result = await _handler.Handle(new GetPublicExperienceSettingsQuery(), CancellationToken.None);

        // Assert
        await Assert.That(result.DeploymentMode).IsEqualTo("SingleTenant");
        await Assert.That(result.IsIslamicModuleEnabled).IsFalse();
        await Assert.That(result.IsTechModuleEnabled).IsFalse();
        await Assert.That(result.EnabledModules.Count).IsEqualTo(0);
    }

    [Test]
    public async Task Handle_WhenDeploymentModeIsRawStringWithoutJson_UsesTrimmedRawValue()
    {
        // Arrange
        var tenantId = Guid.NewGuid();
        _tenantContext.TenantId.Returns(tenantId);

        _policySettingService.ReadEffectiveTenantSettingsAsync(tenantId).Returns(new TenantPolicySettingsDto());
        _moduleService.GetEnabledModulesAsync(tenantId, Arg.Any<CancellationToken>())
            .Returns(new List<ModuleInfo> { new() { ModuleKey = "Mod_Islamic", Name = "Islamic" } });

        _systemSettingRepository.GetByKey(GovernanceSettingKeys.DeploymentMode).Returns(new SystemSetting
        {
            SettingKey = GovernanceSettingKeys.DeploymentMode,
            Value = "SingleTenant"
        });

        // Act
        var result = await _handler.Handle(new GetPublicExperienceSettingsQuery(), CancellationToken.None);

        // Assert
        await Assert.That(result.DeploymentMode).IsEqualTo("SingleTenant");
        await Assert.That(result.IsIslamicModuleEnabled).IsTrue();
        await Assert.That(result.IsTechModuleEnabled).IsFalse();
    }

    [Test]
    public async Task Handle_WithAnalyticsConfigured_ReturnsAnalyticsBootstrapSettings()
    {
        // Arrange
        var tenantId = Guid.NewGuid();
        _tenantContext.TenantId.Returns(tenantId);

        _policySettingService.ReadEffectiveTenantSettingsAsync(tenantId).Returns(new TenantPolicySettingsDto());
        _moduleService.GetEnabledModulesAsync(tenantId, Arg.Any<CancellationToken>())
            .Returns(new List<ModuleInfo>());

        _analyticsConfigResolver.ResolveAsync(Arg.Any<CancellationToken>()).Returns(new AnalyticsConfiguration
        {
            Provider = Explore.Domain.Enums.AnalyticsProviderEnum.Posthog,
            IsEnabled = true,
            ConsentMode = AnalyticsConsentMode.Identified,
            TransportMode = AnalyticsTransportMode.Relay,
            ApiKey = "public-key",
            EndpointUrl = "https://analytics.example.com"
        });

        // Act
        var result = await _handler.Handle(new GetPublicExperienceSettingsQuery(), CancellationToken.None);

        // Assert
        await Assert.That(result.AnalyticsProvider).IsEqualTo("posthog");
        await Assert.That(result.AnalyticsEnabled).IsTrue();
        await Assert.That(result.AnalyticsConsentMode).IsEqualTo("identified");
        await Assert.That(result.AnalyticsTransportMode).IsEqualTo("relay");
        await Assert.That(result.AnalyticsAllowIdentify).IsTrue();
        await Assert.That(result.AnalyticsPublicApiKey).IsEqualTo("public-key");
        await Assert.That(result.AnalyticsEndpointUrl).IsEqualTo("https://analytics.example.com");
    }

    [Test]
    public async Task Handle_WhenAnalyticsApiKeyMissing_DisablesAnalyticsInPayload()
    {
        // Arrange
        var tenantId = Guid.NewGuid();
        _tenantContext.TenantId.Returns(tenantId);

        _policySettingService.ReadEffectiveTenantSettingsAsync(tenantId).Returns(new TenantPolicySettingsDto());
        _moduleService.GetEnabledModulesAsync(tenantId, Arg.Any<CancellationToken>())
            .Returns(new List<ModuleInfo>());

        _analyticsConfigResolver.ResolveAsync(Arg.Any<CancellationToken>()).Returns(new AnalyticsConfiguration
        {
            Provider = Explore.Domain.Enums.AnalyticsProviderEnum.Posthog,
            IsEnabled = true,
            ConsentMode = AnalyticsConsentMode.Pseudonymous,
            TransportMode = AnalyticsTransportMode.Direct,
            ApiKey = string.Empty
        });

        // Act
        var result = await _handler.Handle(new GetPublicExperienceSettingsQuery(), CancellationToken.None);

        // Assert
        await Assert.That(result.AnalyticsProvider).IsEqualTo("posthog");
        await Assert.That(result.AnalyticsEnabled).IsFalse();
        await Assert.That(result.AnalyticsConsentMode).IsEqualTo("pseudonymous");
        await Assert.That(result.AnalyticsTransportMode).IsEqualTo("direct");
        await Assert.That(result.AnalyticsAllowIdentify).IsFalse();
    }

    [Test]
    public async Task Handle_WhenRelayTransportHasNoPublicApiKey_KeepsAnalyticsEnabled()
    {
        var tenantId = Guid.NewGuid();
        _tenantContext.TenantId.Returns(tenantId);

        _policySettingService.ReadEffectiveTenantSettingsAsync(tenantId).Returns(new TenantPolicySettingsDto());
        _moduleService.GetEnabledModulesAsync(tenantId, Arg.Any<CancellationToken>())
            .Returns(new List<ModuleInfo>());

        _analyticsConfigResolver.ResolveAsync(Arg.Any<CancellationToken>()).Returns(new AnalyticsConfiguration
        {
            Provider = Explore.Domain.Enums.AnalyticsProviderEnum.Posthog,
            IsEnabled = true,
            ConsentMode = AnalyticsConsentMode.Pseudonymous,
            TransportMode = AnalyticsTransportMode.Relay,
            ApiKey = string.Empty,
            EndpointUrl = string.Empty
        });

        var result = await _handler.Handle(new GetPublicExperienceSettingsQuery(), CancellationToken.None);

        await Assert.That(result.AnalyticsProvider).IsEqualTo("posthog");
        await Assert.That(result.AnalyticsEnabled).IsTrue();
        await Assert.That(result.AnalyticsTransportMode).IsEqualTo("relay");
        await Assert.That(result.AnalyticsPublicApiKey).IsEqualTo(string.Empty);
        await Assert.That(result.AnalyticsAllowIdentify).IsFalse();
    }

    [Test]
    public async Task Handle_IncludesGovernanceRenderPolicyValuesInPublicPayload()
    {
        // Arrange
        var tenantId = Guid.NewGuid();
        _tenantContext.TenantId.Returns(tenantId);

        _policySettingService.ReadEffectiveTenantSettingsAsync(tenantId).Returns(new TenantPolicySettingsDto());
        _moduleService.GetEnabledModulesAsync(tenantId, Arg.Any<CancellationToken>())
            .Returns(new List<ModuleInfo>());

        _instanceGovernanceSettingService.ReadEffectiveSettingsForTenantAsync(tenantId).Returns(new InstanceGovernanceSettingsDto
        {
            RenderPolicyVersion = 4,
            RenderPolicyPreset = "CustomAdvanced",
            EnableAdvancedRenderPolicyOverrides = true,
            GlobalRenderMode = "InteractiveWebAssembly",
            GlobalPrerenderEnabled = true,
            PublicSeoRenderMode = "InteractiveAuto",
            PublicSeoPrerenderEnabled = true,
            OperationalRenderMode = "InteractiveServer",
            OperationalPrerenderEnabled = false,
            AdminRenderMode = "InteractiveServer",
            AdminPrerenderEnabled = false,
            OnboardingRenderMode = "InteractiveAuto",
            OnboardingPrerenderEnabled = false,
            DisallowInteractiveServerOnOnboarding = true
        });

        // Act
        var result = await _handler.Handle(new GetPublicExperienceSettingsQuery(), CancellationToken.None);

        // Assert
        await Assert.That(result.RenderPolicyVersion).IsEqualTo(4);
        await Assert.That(result.RenderPolicyPreset).IsEqualTo("CustomAdvanced");
        await Assert.That(result.EnableAdvancedRenderPolicyOverrides).IsTrue();
        await Assert.That(result.GlobalRenderMode).IsEqualTo("InteractiveWebAssembly");
        await Assert.That(result.GlobalPrerenderEnabled).IsTrue();
        await Assert.That(result.OperationalRenderMode).IsEqualTo("InteractiveServer");
        await Assert.That(result.DisallowInteractiveServerOnOnboarding).IsTrue();
    }
}
