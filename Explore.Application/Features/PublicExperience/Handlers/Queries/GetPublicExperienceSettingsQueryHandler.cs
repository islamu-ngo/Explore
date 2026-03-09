// ABOUTME: Resolves effective public experience settings through system->tenant cascade for current tenant context.
// ABOUTME: Supports anonymous-safe home page routing and white-label branding consumption.

using System.Text.Json;
using Explore.Application.Analytics;
using Explore.Application.Contracts.Infrastructure;
using Explore.Application.Contracts.Persistence;
using Explore.Application.Contracts.Services;
using Explore.Application.DTOs.Onboarding;
using Explore.Application.Features.PublicExperience.Requests.Queries;
using Explore.Domain.Constants;
using MediatR;

namespace Explore.Application.Features.PublicExperience.Handlers.Queries;

public class GetPublicExperienceSettingsQueryHandler : IRequestHandler<GetPublicExperienceSettingsQuery, PublicExperienceSettingsDto>
{
    private readonly ITenantContext _tenantContext;
    private readonly ISystemSettingRepository _systemSettingRepository;
    private readonly IAnalyticsConfigResolver _analyticsConfigResolver;
    private readonly ITenantPolicySettingService _policySettingService;
    private readonly IModuleService _moduleService;
    private readonly IInstanceGovernanceSettingService _instanceGovernanceSettingService;
    private readonly IAnalyticsGovernanceService _analyticsGovernanceService;

    public GetPublicExperienceSettingsQueryHandler(
        ITenantContext tenantContext,
        ISystemSettingRepository systemSettingRepository,
        IAnalyticsConfigResolver analyticsConfigResolver,
        ITenantPolicySettingService policySettingService,
        IModuleService moduleService,
        IInstanceGovernanceSettingService instanceGovernanceSettingService,
        IAnalyticsGovernanceService analyticsGovernanceService)
    {
        _tenantContext = tenantContext;
        _systemSettingRepository = systemSettingRepository;
        _analyticsConfigResolver = analyticsConfigResolver;
        _policySettingService = policySettingService;
        _moduleService = moduleService;
        _instanceGovernanceSettingService = instanceGovernanceSettingService;
        _analyticsGovernanceService = analyticsGovernanceService;
    }

    public async Task<PublicExperienceSettingsDto> Handle(GetPublicExperienceSettingsQuery request, CancellationToken cancellationToken)
    {
        var tenantId = _tenantContext.TenantId;
        var effectiveTenantSettings = await _policySettingService.ReadEffectiveTenantSettingsAsync(tenantId);
        var enabledModulesInfo = await _moduleService.GetEnabledModulesAsync(tenantId, cancellationToken);
        var enabledModuleKeys = enabledModulesInfo.Select(m => m.ModuleKey).ToList();
        var governanceSettings = await _instanceGovernanceSettingService.ReadEffectiveSettingsForTenantAsync(tenantId);
        var analyticsConfiguration = await _analyticsConfigResolver.ResolveAsync(cancellationToken);

        var deploymentModeSetting = await _systemSettingRepository.GetByKey(GovernanceSettingKeys.DeploymentMode);
        var deploymentMode = DeserializeString(deploymentModeSetting?.Value, "SingleTenant");
        var analyticsProvider = analyticsConfiguration.Provider.ToString().ToLowerInvariant();
        var analyticsPublicApiKey = analyticsConfiguration.ApiKey;
        var analyticsEndpointUrl = analyticsConfiguration.EndpointUrl;
        var hasAnalyticsApiKey = !string.IsNullOrWhiteSpace(analyticsPublicApiKey);
        var transportRequiresPublicApiKey = analyticsConfiguration.TransportMode != AnalyticsTransportMode.Relay;
        var shouldEnableAnalytics = analyticsConfiguration.IsEnabled
            && analyticsConfiguration.Provider != Domain.Enums.AnalyticsProviderEnum.None
            && (!transportRequiresPublicApiKey || hasAnalyticsApiKey);
        var analyticsConsentMode = analyticsConfiguration.ConsentMode.ToString().ToLowerInvariant();
        var analyticsTransportMode = analyticsConfiguration.TransportMode.ToString().ToLowerInvariant();
        var analyticsAllowIdentify = _analyticsGovernanceService.AllowsIdentify(analyticsConfiguration.Provider, analyticsConfiguration.ConsentMode);

        return new PublicExperienceSettingsDto
        {
            TenantId = tenantId,
            DeploymentMode = deploymentMode,
            PreferredHomePage = effectiveTenantSettings.PreferredHomePage,
            BrandDisplayName = effectiveTenantSettings.BrandDisplayName,
            BrandLogoUrl = effectiveTenantSettings.BrandLogoUrl,
            BrandFaviconUrl = effectiveTenantSettings.BrandFaviconUrl,
            BrandCustomCssUrl = effectiveTenantSettings.BrandCustomCssUrl,
            InstanceBaseDomain = effectiveTenantSettings.InstanceBaseDomain,
            Subdomain = effectiveTenantSettings.Subdomain,
            CustomDomain = effectiveTenantSettings.CustomDomain,
            AllowUserSubmittedEvents = effectiveTenantSettings.AllowUserSubmittedEvents,
            AllowOrganizationSubmittedEvents = effectiveTenantSettings.AllowOrganizationSubmittedEvents,
            AllowGroupSubmittedEvents = effectiveTenantSettings.AllowGroupSubmittedEvents,
            AllowOrganizationSelfRegistration = effectiveTenantSettings.AllowOrganizationSelfRegistration,
            AllowGroupSelfRegistration = effectiveTenantSettings.AllowGroupSelfRegistration,
            EventCardClickOpensDetailPage = effectiveTenantSettings.EventCardClickOpensDetailPage,
            IsIslamicModuleEnabled = enabledModuleKeys.Contains("Mod_Islamic"),
            IsTechModuleEnabled = enabledModuleKeys.Contains("Mod_Tech"),
            EnabledModules = enabledModuleKeys,
            AnalyticsProvider = analyticsProvider,
            AnalyticsEnabled = shouldEnableAnalytics,
            AnalyticsConsentMode = analyticsConsentMode,
            AnalyticsTransportMode = analyticsTransportMode,
            AnalyticsAllowIdentify = analyticsAllowIdentify,
            AnalyticsPublicApiKey = analyticsPublicApiKey ?? string.Empty,
            AnalyticsEndpointUrl = analyticsEndpointUrl ?? string.Empty,
            RenderPolicyVersion = governanceSettings.RenderPolicyVersion,
            RenderPolicyPreset = governanceSettings.RenderPolicyPreset,
            EnableAdvancedRenderPolicyOverrides = governanceSettings.EnableAdvancedRenderPolicyOverrides,
            GlobalRenderMode = governanceSettings.GlobalRenderMode,
            GlobalPrerenderEnabled = governanceSettings.GlobalPrerenderEnabled,
            PublicSeoRenderMode = governanceSettings.PublicSeoRenderMode,
            PublicSeoPrerenderEnabled = governanceSettings.PublicSeoPrerenderEnabled,
            OperationalRenderMode = governanceSettings.OperationalRenderMode,
            OperationalPrerenderEnabled = governanceSettings.OperationalPrerenderEnabled,
            AdminRenderMode = governanceSettings.AdminRenderMode,
            AdminPrerenderEnabled = governanceSettings.AdminPrerenderEnabled,
            OnboardingRenderMode = governanceSettings.OnboardingRenderMode,
            OnboardingPrerenderEnabled = governanceSettings.OnboardingPrerenderEnabled,
            DisallowInteractiveServerOnOnboarding = governanceSettings.DisallowInteractiveServerOnOnboarding
        };
    }

    private static string DeserializeString(string? rawValue, string defaultValue)
    {
        if (string.IsNullOrWhiteSpace(rawValue))
        {
            return defaultValue;
        }

        try
        {
            var deserialized = JsonSerializer.Deserialize<string>(rawValue);
            return string.IsNullOrWhiteSpace(deserialized) ? defaultValue : deserialized;
        }
        catch
        {
            return rawValue.Trim('"');
        }
    }
}
