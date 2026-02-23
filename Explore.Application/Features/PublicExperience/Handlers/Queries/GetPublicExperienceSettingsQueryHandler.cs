// ABOUTME: Resolves effective public experience settings through system->tenant cascade for current tenant context.
// ABOUTME: Supports anonymous-safe home page routing and white-label branding consumption.

using System.Text.Json;
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
    private readonly ISettingsResolver _settingsResolver;
    private readonly ITenantPolicySettingService _policySettingService;
    private readonly IModuleService _moduleService;
    private readonly IInstanceGovernanceSettingService _instanceGovernanceSettingService;

    public GetPublicExperienceSettingsQueryHandler(
        ITenantContext tenantContext,
        ISystemSettingRepository systemSettingRepository,
        ISettingsResolver settingsResolver,
        ITenantPolicySettingService policySettingService,
        IModuleService moduleService,
        IInstanceGovernanceSettingService instanceGovernanceSettingService)
    {
        _tenantContext = tenantContext;
        _systemSettingRepository = systemSettingRepository;
        _settingsResolver = settingsResolver;
        _policySettingService = policySettingService;
        _moduleService = moduleService;
        _instanceGovernanceSettingService = instanceGovernanceSettingService;
    }

    public async Task<PublicExperienceSettingsDto> Handle(GetPublicExperienceSettingsQuery request, CancellationToken cancellationToken)
    {
        var tenantId = _tenantContext.TenantId;
        var effectiveTenantSettings = await _policySettingService.ReadEffectiveTenantSettingsAsync(tenantId);
        var enabledModulesInfo = await _moduleService.GetEnabledModulesAsync(tenantId, cancellationToken);
        var enabledModuleKeys = enabledModulesInfo.Select(m => m.ModuleKey).ToList();
        var governanceSettings = await _instanceGovernanceSettingService.ReadSettingsAsync();

        var deploymentModeSetting = await _systemSettingRepository.GetByKey(GovernanceSettingKeys.DeploymentMode);
        var deploymentMode = DeserializeString(deploymentModeSetting?.Value, "SingleTenant");
        var analyticsProvider = await _settingsResolver.GetSettingAsync<string>(GovernanceSettingKeys.AnalyticsProvider, tenantId, cancellationToken) ?? "none";
        var analyticsEnabled = await _settingsResolver.GetSettingAsync<bool>(GovernanceSettingKeys.AnalyticsEnabled, tenantId, cancellationToken);
        var analyticsPublicApiKey = await _settingsResolver.GetSettingAsync<string>(GovernanceSettingKeys.AnalyticsApiKey, tenantId, cancellationToken);
        var analyticsEndpointUrl = await _settingsResolver.GetSettingAsync<string>(GovernanceSettingKeys.AnalyticsEndpointUrl, tenantId, cancellationToken);
        var hasAnalyticsApiKey = !string.IsNullOrWhiteSpace(analyticsPublicApiKey);
        var shouldEnableAnalytics = analyticsEnabled && analyticsProvider != "none" && hasAnalyticsApiKey;

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
            IsIslamicModuleEnabled = enabledModuleKeys.Contains("Mod_Islamic"),
            IsTechModuleEnabled = enabledModuleKeys.Contains("Mod_Tech"),
            EnabledModules = enabledModuleKeys,
            AnalyticsProvider = analyticsProvider,
            AnalyticsEnabled = shouldEnableAnalytics,
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
