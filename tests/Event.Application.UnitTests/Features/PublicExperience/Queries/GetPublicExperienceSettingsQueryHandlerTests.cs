// ABOUTME: Unit tests for GetPublicExperienceSettingsQueryHandler covering modules, deployment mode, and analytics bootstrap.
// ABOUTME: Verifies correct tenant-scoped public experience settings resolution and analytics configuration.

using AutoMapper;
using Explore.Application.Analytics;
using Explore.Application.Contracts.Infrastructure;
using Explore.Application.Contracts.Persistence;
using Explore.Application.Contracts.Services;
using Explore.Application.DTOs.Instance;
using Explore.Application.DTOs.Onboarding;
using Explore.Application.Features.PublicExperience.Handlers.Queries;
using Explore.Application.Features.PublicExperience.Requests.Queries;
using Explore.Application.Models;
using Explore.Application.Services;
using Explore.Application.Settings;
using Explore.Application.Settings.Groups;
using Explore.Domain;
using Explore.Domain.Constants;
using Explore.Domain.Enums;
using Explore.Domain.Settings.Documents;
using Explore.Domain.Settings.Documents.Payloads;
using NSubstitute;

namespace Event.Application.UnitTests.Features.PublicExperience.Queries;

public class GetPublicExperienceSettingsQueryHandlerTests
{
    private readonly ITenantContext _tenantContext;
    private readonly ISystemSettingRepository _systemSettingRepository;
    private readonly IAnalyticsConfigResolver _analyticsConfigResolver;
    private readonly ITranslationConfigResolver _translationConfigResolver;
    private readonly ITenantPolicySettingService _policySettingService;
    private readonly IModuleService _moduleService;
    private readonly IInstanceGovernanceSettingService _instanceGovernanceSettingService;
    private readonly IAnalyticsGovernanceService _analyticsGovernanceService;
    private readonly IAnalyticsRuntimeProfileResolver _runtimeProfileResolver;
    private readonly IHierarchicalSettingsResolver _hierarchicalSettingsResolver;
    private readonly ITypedSettingsDocumentResolver _typedSettingsDocumentResolver;
    private readonly IFooterLinkGroupRepository _footerLinkGroupRepository;
    private readonly IMapper _mapper;
    private readonly GetPublicExperienceSettingsQueryHandler _handler;

    public GetPublicExperienceSettingsQueryHandlerTests()
    {
        _tenantContext = Substitute.For<ITenantContext>();
        _systemSettingRepository = Substitute.For<ISystemSettingRepository>();
        _analyticsConfigResolver = Substitute.For<IAnalyticsConfigResolver>();
        _translationConfigResolver = Substitute.For<ITranslationConfigResolver>();
        _policySettingService = Substitute.For<ITenantPolicySettingService>();
        _moduleService = Substitute.For<IModuleService>();
        _instanceGovernanceSettingService = Substitute.For<IInstanceGovernanceSettingService>();
        _analyticsGovernanceService = new AnalyticsGovernanceService();
        _runtimeProfileResolver = Substitute.For<IAnalyticsRuntimeProfileResolver>();
        _runtimeProfileResolver.Resolve(Arg.Any<AnalyticsSettingGroup>())
            .Returns(new AnalyticsRuntimeProfile());
        _hierarchicalSettingsResolver = Substitute.For<IHierarchicalSettingsResolver>();
        _typedSettingsDocumentResolver = Substitute.For<ITypedSettingsDocumentResolver>();
        _hierarchicalSettingsResolver.ResolveGroupAsync<AnalyticsSettingGroup>(
            Arg.Any<SettingContext>(), Arg.Any<CancellationToken>())
            .Returns(new AnalyticsSettingGroup());
        _instanceGovernanceSettingService.ReadEffectiveSettingsForTenantAsync(Arg.Any<Guid>()).Returns(CreateDefaultGovernanceSettings());
        _analyticsConfigResolver.ResolveAsync(Arg.Any<CancellationToken>()).Returns(new AnalyticsConfiguration());
        _translationConfigResolver.ResolveAsync(Arg.Any<CancellationToken>())
            .Returns(new TranslationConfiguration(TranslationManagementProviderEnum.None, null, null, null, "en"));
        _footerLinkGroupRepository = Substitute.For<IFooterLinkGroupRepository>();
        _footerLinkGroupRepository.GetResolvedGroupsForTenantAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns([]);
        _hierarchicalSettingsResolver.ResolveGroupAsync<FooterSettingGroup>(
            Arg.Any<SettingContext>(), Arg.Any<CancellationToken>())
            .Returns(new FooterSettingGroup());
        _hierarchicalSettingsResolver.ResolveGroupAsync<AiAssistantSettingGroup>(
            Arg.Any<SettingContext>(), Arg.Any<CancellationToken>())
            .Returns(new AiAssistantSettingGroup());
        _hierarchicalSettingsResolver.ResolveGroupAsync<AppearanceSettingGroup>(
            Arg.Any<SettingContext>(), Arg.Any<CancellationToken>())
            .Returns(new AppearanceSettingGroup());
        _mapper = Substitute.For<IMapper>();
        _mapper.Map<List<global::Explore.Application.DTOs.Footer.FooterLinkGroupDto>>(Arg.Any<object>())
            .Returns([]);

        _handler = new GetPublicExperienceSettingsQueryHandler(
            _tenantContext,
            _systemSettingRepository,
            _analyticsConfigResolver,
            _translationConfigResolver,
            _policySettingService,
            _moduleService,
            _instanceGovernanceSettingService,
            _analyticsGovernanceService,
            _runtimeProfileResolver,
            _hierarchicalSettingsResolver,
            _typedSettingsDocumentResolver,
            _footerLinkGroupRepository,
            _mapper);
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

        _systemSettingRepository.GetByKey(GovernanceSettingKeys.Deployment.Mode).Returns(new SystemSetting
        {
            SettingKey = GovernanceSettingKeys.Deployment.Mode,
            Value = "\"MultiTenant\""
        });

        // Act
        var result = await _handler.Handle(new GetPublicExperienceSettingsQuery(), CancellationToken.None);

        // Assert
        await Assert.That(result).IsNotNull();
        await Assert.That(result.TenantId).IsEqualTo(tenantId);
        await Assert.That(result.Mode).IsEqualTo(PublicExperienceMode.DiscoveryCentric);
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
        _systemSettingRepository.GetByKey(GovernanceSettingKeys.Deployment.Mode).Returns((SystemSetting?)null);

        // Act
        var result = await _handler.Handle(new GetPublicExperienceSettingsQuery(), CancellationToken.None);

        // Assert
        await Assert.That(result.DeploymentMode).IsEqualTo("SingleTenant");
        await Assert.That(result.IsIslamicModuleEnabled).IsFalse();
        await Assert.That(result.IsTechModuleEnabled).IsFalse();
        await Assert.That(result.EnabledModules.Count).IsEqualTo(0);
    }

    [Test]
    public async Task Handle_WhenClientPickerDisabled_ExposesPickerKillSwitch()
    {
        var tenantId = Guid.NewGuid();
        _tenantContext.TenantId.Returns(tenantId);

        _policySettingService.ReadEffectiveTenantSettingsAsync(tenantId).Returns(new TenantPolicySettingsDto());
        _moduleService.GetEnabledModulesAsync(tenantId, Arg.Any<CancellationToken>())
            .Returns(new List<ModuleInfo>());
        _systemSettingRepository.GetByKey(GovernanceSettingKeys.Deployment.Mode).Returns((SystemSetting?)null);
        _translationConfigResolver.ResolveAsync(Arg.Any<CancellationToken>())
            .Returns(new TranslationConfiguration(TranslationManagementProviderEnum.None, null, null, null, "en")
            {
                ClientPickerEnabled = false
            });

        var result = await _handler.Handle(new GetPublicExperienceSettingsQuery(), CancellationToken.None);

        await Assert.That(result.ClientPickerEnabled).IsFalse();
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

        _systemSettingRepository.GetByKey(GovernanceSettingKeys.Deployment.Mode).Returns(new SystemSetting
        {
            SettingKey = GovernanceSettingKeys.Deployment.Mode,
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
    public async Task Handle_WithAiAssistantPolicy_IncludesAvailabilityAndAnonymousAccess()
    {
        var tenantId = Guid.NewGuid();
        _tenantContext.TenantId.Returns(tenantId);

        _policySettingService.ReadEffectiveTenantSettingsAsync(tenantId).Returns(new TenantPolicySettingsDto());
        _moduleService.GetEnabledModulesAsync(tenantId, Arg.Any<CancellationToken>())
            .Returns(new List<ModuleInfo>());

        var aiSettings = new AiAssistantSettingGroup();
        aiSettings.Populate(new Dictionary<string, ResolvedSetting>
        {
            [GovernanceSettingKeys.AiAssistant.Enabled] = new() { Value = "true" },
            [GovernanceSettingKeys.AiAssistant.Provider] = new() { Value = "\"openai-compatible\"" },
            [GovernanceSettingKeys.AiAssistant.EndpointUrl] = new() { Value = "\"https://ai.example.test\"" },
            [GovernanceSettingKeys.AiAssistant.ApiKey] = new() { Value = "\"secret-ref\"" },
            [GovernanceSettingKeys.AiAssistant.ModelId] = new() { Value = "\"gpt-test\"" },
            [GovernanceSettingKeys.AiAssistant.AllowAnonymousAccess] = new() { Value = "true" }
        });

        _hierarchicalSettingsResolver.ResolveGroupAsync<AiAssistantSettingGroup>(
                Arg.Any<SettingContext>(), Arg.Any<CancellationToken>())
            .Returns(aiSettings);

        var result = await _handler.Handle(new GetPublicExperienceSettingsQuery(), CancellationToken.None);

        await Assert.That(result.IsAiAssistantEnabled).IsTrue();
        await Assert.That(result.IsAiAssistantAvailable).IsTrue();
        await Assert.That(result.AiAssistantAllowAnonymousAccess).IsTrue();
    }

    [Test]
    public async Task Handle_WithAiAssistantDisabled_DoesNotExposeAvailability()
    {
        var tenantId = Guid.NewGuid();
        _tenantContext.TenantId.Returns(tenantId);

        _policySettingService.ReadEffectiveTenantSettingsAsync(tenantId).Returns(new TenantPolicySettingsDto());
        _moduleService.GetEnabledModulesAsync(tenantId, Arg.Any<CancellationToken>())
            .Returns(new List<ModuleInfo>());

        var aiSettings = new AiAssistantSettingGroup();
        aiSettings.Populate(new Dictionary<string, ResolvedSetting>
        {
            [GovernanceSettingKeys.AiAssistant.Enabled] = new() { Value = "false" }
        });

        _hierarchicalSettingsResolver.ResolveGroupAsync<AiAssistantSettingGroup>(
                Arg.Any<SettingContext>(), Arg.Any<CancellationToken>())
            .Returns(aiSettings);

        var result = await _handler.Handle(new GetPublicExperienceSettingsQuery(), CancellationToken.None);

        await Assert.That(result.IsAiAssistantEnabled).IsFalse();
        await Assert.That(result.IsAiAssistantAvailable).IsFalse();
        await Assert.That(result.AiAssistantAllowAnonymousAccess).IsFalse();
    }

    [Test]
    public async Task Handle_WithAiAssistantEnabledButNotConfigured_ExposesEnabledWithoutAvailability()
    {
        var tenantId = Guid.NewGuid();
        _tenantContext.TenantId.Returns(tenantId);

        _policySettingService.ReadEffectiveTenantSettingsAsync(tenantId).Returns(new TenantPolicySettingsDto());
        _moduleService.GetEnabledModulesAsync(tenantId, Arg.Any<CancellationToken>())
            .Returns(new List<ModuleInfo>());

        var aiSettings = new AiAssistantSettingGroup();
        aiSettings.Populate(new Dictionary<string, ResolvedSetting>
        {
            [GovernanceSettingKeys.AiAssistant.Enabled] = new() { Value = "true" },
            [GovernanceSettingKeys.AiAssistant.Provider] = new() { Value = "\"openai-compatible\"" }
            // Missing endpoint URL and model ID => not configured
        });

        _hierarchicalSettingsResolver.ResolveGroupAsync<AiAssistantSettingGroup>(
                Arg.Any<SettingContext>(), Arg.Any<CancellationToken>())
            .Returns(aiSettings);

        var result = await _handler.Handle(new GetPublicExperienceSettingsQuery(), CancellationToken.None);

        await Assert.That(result.IsAiAssistantEnabled).IsTrue();
        await Assert.That(result.IsAiAssistantAvailable).IsFalse();
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

        _instanceGovernanceSettingService.ReadEffectiveSettingsForTenantAsync(tenantId).Returns(new InstanceGovernanceSettings
        {
            DeploymentMode = new DeploymentModeDto { Mode = Explore.Domain.Enums.DeploymentMode.SingleTenant },
            Modules = new ModuleSettingsDto(),
            EventPolicy = new EventPolicyDto(),
            OrganizationPolicy = new OrganizationPolicyDto(),
            Branding = new BrandingSettingsDto(),
            Domains = new DomainSettingsDto(),
            TenantDelegation = new TenantDelegationSettingsDto(),
            AdminPortal = new AdminPortalSettingsDto(),
            AiAssistant = new AiAssistantGovernanceSettingsDto(),
            Mcp = new McpGovernanceSettingsDto(),
            RenderPolicy = new RenderPolicySettingsDto
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
            }
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

    [Test]
    public async Task Handle_WhenTypedBrandingDocumentExists_UsesTypedBrandingInsteadOfScalarPolicyValues()
    {
        var tenantId = Guid.NewGuid();
        _tenantContext.TenantId.Returns(tenantId);

        _policySettingService.ReadEffectiveTenantSettingsAsync(tenantId).Returns(new TenantPolicySettingsDto
        {
            BrandDisplayName = "Scalar Brand",
            BrandLogoUrl = "https://scalar.example/logo.svg",
            BrandFaviconUrl = "https://scalar.example/favicon.ico",
            BrandCustomCssUrl = "https://scalar.example/custom.css"
        });

        _typedSettingsDocumentResolver.ResolveTenantDocumentAsync<BrandingSettings>(
                Arg.Is<SettingsResolutionContext>(context =>
                    context.TenantId == tenantId
                    && context.RequestsDocument(SettingsDocumentKeys.Tenant.Branding)),
                SettingsDocumentKeys.Tenant.Branding,
                Arg.Any<CancellationToken>())
            .Returns(new ResolvedSettingsDocument<BrandingSettings>
            {
                DocumentKey = SettingsDocumentKeys.Tenant.Branding,
                SchemaVersion = 1,
                DefaultsVersion = "2026-05-branding",
                Payload = new BrandingSettings
                {
                    DisplayName = "Typed Brand",
                    LogoUrl = "https://typed.example/logo.svg",
                    FaviconUrl = "https://typed.example/favicon.ico",
                    CustomCssUrl = "https://typed.example/custom.css"
                },
                Source = SettingsDocumentSource.Tenant,
                SourceScopeId = tenantId,
                ConcurrencyStamp = Guid.NewGuid()
            });

        _moduleService.GetEnabledModulesAsync(tenantId, Arg.Any<CancellationToken>())
            .Returns(new List<ModuleInfo>());

        var result = await _handler.Handle(new GetPublicExperienceSettingsQuery(), CancellationToken.None);

        await Assert.That(result.BrandDisplayName).IsEqualTo("Typed Brand");
        await Assert.That(result.BrandLogoUrl).IsEqualTo("https://typed.example/logo.svg");
        await Assert.That(result.BrandFaviconUrl).IsEqualTo("https://typed.example/favicon.ico");
        await Assert.That(result.BrandCustomCssUrl).IsEqualTo("https://typed.example/custom.css");
    }

    [Test]
    public async Task Handle_WhenTypedBrandingDocumentIsMissing_DoesNotFallbackToScalarBranding()
    {
        var tenantId = Guid.NewGuid();
        _tenantContext.TenantId.Returns(tenantId);

        _policySettingService.ReadEffectiveTenantSettingsAsync(tenantId).Returns(new TenantPolicySettingsDto
        {
            BrandDisplayName = "Scalar Brand",
            BrandLogoUrl = "https://scalar.example/logo.svg",
            BrandFaviconUrl = "https://scalar.example/favicon.ico",
            BrandCustomCssUrl = "https://scalar.example/custom.css"
        });

        _typedSettingsDocumentResolver.ResolveTenantDocumentAsync<BrandingSettings>(
                Arg.Any<SettingsResolutionContext>(),
                SettingsDocumentKeys.Tenant.Branding,
                Arg.Any<CancellationToken>())
            .Returns((ResolvedSettingsDocument<BrandingSettings>?)null);

        _moduleService.GetEnabledModulesAsync(tenantId, Arg.Any<CancellationToken>())
            .Returns(new List<ModuleInfo>());

        var result = await _handler.Handle(new GetPublicExperienceSettingsQuery(), CancellationToken.None);

        await Assert.That(result.BrandDisplayName).IsEqualTo(string.Empty);
        await Assert.That(result.BrandLogoUrl).IsEqualTo(string.Empty);
        await Assert.That(result.BrandFaviconUrl).IsEqualTo(string.Empty);
        await Assert.That(result.BrandCustomCssUrl).IsEqualTo(string.Empty);
    }

    private static InstanceGovernanceSettings CreateDefaultGovernanceSettings()
    {
        return new InstanceGovernanceSettings
        {
            DeploymentMode = new DeploymentModeDto { Mode = Explore.Domain.Enums.DeploymentMode.SingleTenant },
            Modules = new ModuleSettingsDto(),
            EventPolicy = new EventPolicyDto(),
            OrganizationPolicy = new OrganizationPolicyDto(),
            Branding = new BrandingSettingsDto(),
            Domains = new DomainSettingsDto(),
            TenantDelegation = new TenantDelegationSettingsDto(),
            AdminPortal = new AdminPortalSettingsDto(),
            AiAssistant = new AiAssistantGovernanceSettingsDto(),
            Mcp = new McpGovernanceSettingsDto(),
            RenderPolicy = new RenderPolicySettingsDto()
        };
    }
}
