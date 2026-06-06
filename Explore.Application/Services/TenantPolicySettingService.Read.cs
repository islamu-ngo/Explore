// ABOUTME: Read path for tenant policy settings — resolves effective values by merging tenant overrides with instance defaults.
// ABOUTME: Partial class containing ReadEffectiveTenantSettingsAsync and its governance flag computations.

using Explore.Application.DTOs.Onboarding;
using Explore.Domain.Constants;

namespace Explore.Application.Services;

public partial class TenantPolicySettingService
{
    public async Task<TenantPolicySettingsDto> ReadEffectiveTenantSettingsAsync(Guid tenantId)
    {
        var systemUserSubmission = await _systemSettingRepository.GetByKey(GovernanceSettingKeys.Events.UserSubmissionEnabled);
        var systemOrgSubmission = await _systemSettingRepository.GetByKey(GovernanceSettingKeys.Events.OrganizationSubmissionEnabled);
        var systemGroupSubmission = await _systemSettingRepository.GetByKey(GovernanceSettingKeys.Events.GroupSubmissionEnabled);
        var systemRequireApproval = await _systemSettingRepository.GetByKey(GovernanceSettingKeys.Events.RequireApproval);
        var systemEventCardClickOpensDetailPage = await _systemSettingRepository.GetByKey(GovernanceSettingKeys.Events.CardClickOpensDetailPage);
        var systemRequireVerification = await _systemSettingRepository.GetByKey(GovernanceSettingKeys.Organizations.VerificationRequired);
        var systemTenantCanOmitVerification = await _systemSettingRepository.GetByKey(GovernanceSettingKeys.Organizations.TenantCanOmitVerification);
        var systemOrgSelfRegistration = await _systemSettingRepository.GetByKey(GovernanceSettingKeys.Organizations.SelfRegistrationEnabled);
        var systemGroupSelfRegistration = await _systemSettingRepository.GetByKey(GovernanceSettingKeys.Groups.SelfRegistrationEnabled);
        var systemDeploymentMode = await _systemSettingRepository.GetByKey(GovernanceSettingKeys.Deployment.Mode);
        var systemTenantWhiteLabeling = await _systemSettingRepository.GetByKey(GovernanceSettingKeys.Tenants.WhiteLabelingEnabled);
        var systemHomePage = await _systemSettingRepository.GetByKey(GovernanceSettingKeys.Routing.DefaultPublicHomePage);
        var systemInstanceBaseDomain = await _systemSettingRepository.GetByKey(GovernanceSettingKeys.Domains.InstanceBaseDomain);
        var systemAllowCustomDomain = await _systemSettingRepository.GetByKey(GovernanceSettingKeys.Domains.AllowTenantCustomDomain);
        var systemTenantSubdomain = await _systemSettingRepository.GetByKey(GovernanceSettingKeys.Domains.TenantSubdomain);
        var systemTenantCustomDomain = await _systemSettingRepository.GetByKey(GovernanceSettingKeys.Domains.TenantCustomDomain);
        var systemBrandDisplayName = await _systemSettingRepository.GetByKey(GovernanceSettingKeys.Branding.DisplayName);
        var systemBrandLogoUrl = await _systemSettingRepository.GetByKey(GovernanceSettingKeys.Branding.LogoUrl);
        var systemBrandFaviconUrl = await _systemSettingRepository.GetByKey(GovernanceSettingKeys.Branding.FaviconUrl);
        var systemBrandCustomCssUrl = await _systemSettingRepository.GetByKey(GovernanceSettingKeys.Branding.CustomCssUrl);
        var systemAllowTenantRenderOverride = await _systemSettingRepository.GetByKey(GovernanceSettingKeys.Routing.RenderPolicy.AllowTenantOverride);
        var systemLockPublicSeo = await _systemSettingRepository.GetByKey(GovernanceSettingKeys.Routing.RenderPolicy.LockTenantPublicSeo);
        var systemLockOperational = await _systemSettingRepository.GetByKey(GovernanceSettingKeys.Routing.RenderPolicy.LockTenantOperational);
        var systemLockAdmin = await _systemSettingRepository.GetByKey(GovernanceSettingKeys.Routing.RenderPolicy.LockTenantAdmin);
        var systemLockSmtp = await _systemSettingRepository.GetByKey(GovernanceSettingKeys.TenantDelegation.LockSmtp);
        var systemLockStorage = await _systemSettingRepository.GetByKey(GovernanceSettingKeys.TenantDelegation.LockStorage);
        var systemLockAnalytics = await _systemSettingRepository.GetByKey(GovernanceSettingKeys.TenantDelegation.LockAnalytics);
        var systemLockAiAssistant = await _systemSettingRepository.GetByKey(GovernanceSettingKeys.TenantDelegation.LockAiAssistant);
        var systemAiAssistantEnabled = await _systemSettingRepository.GetByKey(GovernanceSettingKeys.AiAssistant.Enabled);
        var systemAiAssistantProvider = await _systemSettingRepository.GetByKey(GovernanceSettingKeys.AiAssistant.Provider);
        var systemAiAssistantEndpointUrl = await _systemSettingRepository.GetByKey(GovernanceSettingKeys.AiAssistant.EndpointUrl);
        var systemAiAssistantApiKey = await _systemSettingRepository.GetByKey(GovernanceSettingKeys.AiAssistant.ApiKey);
        var systemAiAssistantModelId = await _systemSettingRepository.GetByKey(GovernanceSettingKeys.AiAssistant.ModelId);
        var systemAiAssistantAllowAnonymousAccess = await _systemSettingRepository.GetByKey(GovernanceSettingKeys.AiAssistant.AllowAnonymousAccess);
        var systemCommunityGuidelines = await _systemSettingRepository.GetByKey(GovernanceSettingKeys.Policies.CommunityGuidelinesContent);
        var systemRenderPreset = await _systemSettingRepository.GetByKey(GovernanceSettingKeys.Routing.RenderPolicy.Preset);
        var systemRenderAdvanced = await _systemSettingRepository.GetByKey(GovernanceSettingKeys.Routing.RenderPolicy.AdvancedEnabled);
        var systemGlobalRenderMode = await _systemSettingRepository.GetByKey(GovernanceSettingKeys.Routing.RenderPolicy.Fallback.RenderMode);
        var systemGlobalPrerender = await _systemSettingRepository.GetByKey(GovernanceSettingKeys.Routing.RenderPolicy.Fallback.PrerenderEnabled);
        var systemPublicSeoRenderMode = await _systemSettingRepository.GetByKey(GovernanceSettingKeys.Routing.RenderPolicy.PublicSeo.RenderMode);
        var systemPublicSeoPrerender = await _systemSettingRepository.GetByKey(GovernanceSettingKeys.Routing.RenderPolicy.PublicSeo.PrerenderEnabled);
        var systemOperationalRenderMode = await _systemSettingRepository.GetByKey(GovernanceSettingKeys.Routing.RenderPolicy.Operational.RenderMode);
        var systemOperationalPrerender = await _systemSettingRepository.GetByKey(GovernanceSettingKeys.Routing.RenderPolicy.Operational.PrerenderEnabled);
        var systemAdminRenderMode = await _systemSettingRepository.GetByKey(GovernanceSettingKeys.Routing.RenderPolicy.Admin.RenderMode);
        var systemAdminPrerender = await _systemSettingRepository.GetByKey(GovernanceSettingKeys.Routing.RenderPolicy.Admin.PrerenderEnabled);

        var tenantUserSubmission = await _tenantSettingRepository.GetByTenantAndKey(tenantId, GovernanceSettingKeys.Events.UserSubmissionEnabled);
        var tenantOrgSubmission = await _tenantSettingRepository.GetByTenantAndKey(tenantId, GovernanceSettingKeys.Events.OrganizationSubmissionEnabled);
        var tenantGroupSubmission = await _tenantSettingRepository.GetByTenantAndKey(tenantId, GovernanceSettingKeys.Events.GroupSubmissionEnabled);
        var tenantRequireApproval = await _tenantSettingRepository.GetByTenantAndKey(tenantId, GovernanceSettingKeys.Events.RequireApproval);
        var tenantEventCardClickOpensDetailPage = await _tenantSettingRepository.GetByTenantAndKey(tenantId, GovernanceSettingKeys.Events.CardClickOpensDetailPage);
        var tenantRequireVerification = await _tenantSettingRepository.GetByTenantAndKey(tenantId, GovernanceSettingKeys.Organizations.VerificationRequired);
        var tenantOrgSelfRegistration = await _tenantSettingRepository.GetByTenantAndKey(tenantId, GovernanceSettingKeys.Organizations.SelfRegistrationEnabled);
        var tenantGroupSelfRegistration = await _tenantSettingRepository.GetByTenantAndKey(tenantId, GovernanceSettingKeys.Groups.SelfRegistrationEnabled);
        var tenantHomePage = await _tenantSettingRepository.GetByTenantAndKey(tenantId, GovernanceSettingKeys.Routing.DefaultPublicHomePage);
        var tenantSubdomain = await _tenantSettingRepository.GetByTenantAndKey(tenantId, GovernanceSettingKeys.Domains.TenantSubdomain);
        var tenantCustomDomain = await _tenantSettingRepository.GetByTenantAndKey(tenantId, GovernanceSettingKeys.Domains.TenantCustomDomain);
        var tenantBrandDisplayName = await _tenantSettingRepository.GetByTenantAndKey(tenantId, GovernanceSettingKeys.Branding.DisplayName);
        var tenantBrandLogoUrl = await _tenantSettingRepository.GetByTenantAndKey(tenantId, GovernanceSettingKeys.Branding.LogoUrl);
        var tenantBrandFaviconUrl = await _tenantSettingRepository.GetByTenantAndKey(tenantId, GovernanceSettingKeys.Branding.FaviconUrl);
        var tenantBrandCustomCssUrl = await _tenantSettingRepository.GetByTenantAndKey(tenantId, GovernanceSettingKeys.Branding.CustomCssUrl);
        var tenantAnnouncementEnabled = await _tenantSettingRepository.GetByTenantAndKey(tenantId, GovernanceSettingKeys.PublicExperience.AnnouncementBarEnabled);
        var tenantAnnouncementMessage = await _tenantSettingRepository.GetByTenantAndKey(tenantId, GovernanceSettingKeys.PublicExperience.AnnouncementBarMessage);
        var tenantAnnouncementLinkText = await _tenantSettingRepository.GetByTenantAndKey(tenantId, GovernanceSettingKeys.PublicExperience.AnnouncementBarLinkText);
        var tenantAnnouncementLinkUrl = await _tenantSettingRepository.GetByTenantAndKey(tenantId, GovernanceSettingKeys.PublicExperience.AnnouncementBarLinkUrl);
        var tenantAnnouncementRevision = await _tenantSettingRepository.GetByTenantAndKey(tenantId, GovernanceSettingKeys.PublicExperience.AnnouncementBarRevision);
        var tenantAiAssistantEnabled = await _tenantSettingRepository.GetByTenantAndKey(tenantId, GovernanceSettingKeys.AiAssistant.Enabled);
        var tenantAiAssistantProvider = await _tenantSettingRepository.GetByTenantAndKey(tenantId, GovernanceSettingKeys.AiAssistant.Provider);
        var tenantAiAssistantEndpointUrl = await _tenantSettingRepository.GetByTenantAndKey(tenantId, GovernanceSettingKeys.AiAssistant.EndpointUrl);
        var tenantAiAssistantApiKey = await _tenantSettingRepository.GetByTenantAndKey(tenantId, GovernanceSettingKeys.AiAssistant.ApiKey);
        var tenantAiAssistantModelId = await _tenantSettingRepository.GetByTenantAndKey(tenantId, GovernanceSettingKeys.AiAssistant.ModelId);
        var tenantAiAssistantAllowAnonymousAccess = await _tenantSettingRepository.GetByTenantAndKey(tenantId, GovernanceSettingKeys.AiAssistant.AllowAnonymousAccess);
        var tenantCommunityGuidelines = await _tenantSettingRepository.GetByTenantAndKey(tenantId, GovernanceSettingKeys.Policies.CommunityGuidelinesContent);

        var isMultiTenant = DeserializeString(systemDeploymentMode?.Value, "SingleTenant").Equals("MultiTenant", StringComparison.OrdinalIgnoreCase);

        var allowTenantRenderOverride = !isMultiTenant || DeserializeBoolean(systemAllowTenantRenderOverride?.Value, false);
        var canOverridePublicSeo = allowTenantRenderOverride && (!isMultiTenant || !DeserializeBoolean(systemLockPublicSeo?.Value, false));
        var canOverrideOperational = allowTenantRenderOverride && (!isMultiTenant || !DeserializeBoolean(systemLockOperational?.Value, false));
        var canOverrideAdmin = allowTenantRenderOverride && (!isMultiTenant || !DeserializeBoolean(systemLockAdmin?.Value, false));

        var tenant = await _tenantRepository.GetById(tenantId);
        var fallbackSubdomain = NormalizeSubdomain(tenant?.Slug) ?? "default";

        var isTenantWhiteLabelingEnabled = isMultiTenant && DeserializeBoolean(systemTenantWhiteLabeling?.Value, false);
        var canOverrideHomePage = systemHomePage?.IsLocked != true;
        var canOverrideEventCardClickBehavior = systemEventCardClickOpensDetailPage?.IsLocked != true;
        var canOverrideSubdomain = systemTenantSubdomain?.IsLocked != true;
        var canOverrideCustomDomain = systemTenantCustomDomain?.IsLocked != true
            && DeserializeBoolean(systemAllowCustomDomain?.Value, true);
        var canOmitVerification = DeserializeBoolean(systemTenantCanOmitVerification?.Value, false)
            && systemRequireVerification?.IsLocked != true;
        var requireVerification = ResolveBoolean(
            tenantRequireVerification?.Value,
            systemRequireVerification?.Value,
            true,
            canOmitVerification);

        return new TenantPolicySettingsDto
        {
            AllowUserSubmittedEvents = ResolveBoolean(
                tenantUserSubmission?.Value,
                systemUserSubmission?.Value,
                true,
                systemUserSubmission?.IsLocked != true),
            AllowOrganizationSubmittedEvents = ResolveBoolean(
                tenantOrgSubmission?.Value,
                systemOrgSubmission?.Value,
                true,
                systemOrgSubmission?.IsLocked != true),
            AllowGroupSubmittedEvents = ResolveBoolean(
                tenantGroupSubmission?.Value,
                systemGroupSubmission?.Value,
                true,
                systemGroupSubmission?.IsLocked != true),
            AllowOrganizationSelfRegistration = ResolveBoolean(
                tenantOrgSelfRegistration?.Value,
                systemOrgSelfRegistration?.Value,
                true,
                systemOrgSelfRegistration?.IsLocked != true),
            AllowGroupSelfRegistration = ResolveBoolean(
                tenantGroupSelfRegistration?.Value,
                systemGroupSelfRegistration?.Value,
                true,
                systemGroupSelfRegistration?.IsLocked != true),
            RequireEventApproval = ResolveBoolean(
                tenantRequireApproval?.Value,
                systemRequireApproval?.Value,
                false,
                systemRequireApproval?.IsLocked != true),
            EventCardClickOpensDetailPage = ResolveBoolean(
                tenantEventCardClickOpensDetailPage?.Value,
                systemEventCardClickOpensDetailPage?.Value,
                false,
                canOverrideEventCardClickBehavior),
            RequireOrganizationVerification = requireVerification,
            CanTenantOmitVerification = canOmitVerification,
            IsTenantWhiteLabelingEnabled = isTenantWhiteLabelingEnabled,
            PreferredHomePage = NormalizeHomePage(ResolveString(
                tenantHomePage?.Value,
                systemHomePage?.Value,
                DefaultPublicHomePage,
                canOverrideHomePage)),
            InstanceBaseDomain = NormalizeOptionalHost(DeserializeString(systemInstanceBaseDomain?.Value, string.Empty)),
            Subdomain = NormalizeSubdomain(ResolveString(
                tenantSubdomain?.Value,
                systemTenantSubdomain?.Value,
                fallbackSubdomain,
                canOverrideSubdomain)) ?? fallbackSubdomain,
            CustomDomain = NormalizeOptionalHost(ResolveString(
                tenantCustomDomain?.Value,
                systemTenantCustomDomain?.Value,
                string.Empty,
                canOverrideCustomDomain)),
            BrandDisplayName = ResolveString(
                tenantBrandDisplayName?.Value,
                systemBrandDisplayName?.Value,
                DefaultBrandDisplayName,
                systemBrandDisplayName?.IsLocked != true),
            BrandLogoUrl = ResolveString(
                tenantBrandLogoUrl?.Value,
                systemBrandLogoUrl?.Value,
                string.Empty,
                isTenantWhiteLabelingEnabled && systemBrandLogoUrl?.IsLocked != true),
            BrandFaviconUrl = ResolveString(
                tenantBrandFaviconUrl?.Value,
                systemBrandFaviconUrl?.Value,
                string.Empty,
                isTenantWhiteLabelingEnabled && systemBrandFaviconUrl?.IsLocked != true),
            BrandCustomCssUrl = ResolveString(
                tenantBrandCustomCssUrl?.Value,
                systemBrandCustomCssUrl?.Value,
                string.Empty,
                isTenantWhiteLabelingEnabled && systemBrandCustomCssUrl?.IsLocked != true),
            AnnouncementBarEnabled = DeserializeBoolean(tenantAnnouncementEnabled?.Value, false),
            AnnouncementBarMessage = DeserializeString(tenantAnnouncementMessage?.Value, string.Empty),
            AnnouncementBarLinkText = DeserializeString(tenantAnnouncementLinkText?.Value, string.Empty),
            AnnouncementBarLinkUrl = DeserializeString(tenantAnnouncementLinkUrl?.Value, string.Empty),
            AnnouncementBarRevision = DeserializeInteger(tenantAnnouncementRevision?.Value, 0),
            CanOverrideHomePagePreference = canOverrideHomePage,
            CanOverrideSubdomain = canOverrideSubdomain,
            CanOverrideCustomDomain = canOverrideCustomDomain,
            CanOverrideBrandDisplayName = systemBrandDisplayName?.IsLocked != true,
            CanOverrideBrandLogoUrl = isTenantWhiteLabelingEnabled && systemBrandLogoUrl?.IsLocked != true,
            CanOverrideBrandFaviconUrl = isTenantWhiteLabelingEnabled && systemBrandFaviconUrl?.IsLocked != true,
            CanOverrideBrandCustomCssUrl = isTenantWhiteLabelingEnabled && systemBrandCustomCssUrl?.IsLocked != true,
            CanOverrideEventCardClickBehavior = canOverrideEventCardClickBehavior,
            RenderPolicyPreset = ResolveString(
                allowTenantRenderOverride ? (await _tenantSettingRepository.GetByTenantAndKey(tenantId, GovernanceSettingKeys.Routing.RenderPolicy.Preset))?.Value : null,
                systemRenderPreset?.Value,
                "AllInteractiveServer",
                allowTenantRenderOverride),
            EnableAdvancedRenderPolicyOverrides = allowTenantRenderOverride
                ? ResolveBoolean(
                    (await _tenantSettingRepository.GetByTenantAndKey(tenantId, GovernanceSettingKeys.Routing.RenderPolicy.AdvancedEnabled))?.Value,
                    systemRenderAdvanced?.Value,
                    false,
                    true)
                : DeserializeBoolean(systemRenderAdvanced?.Value, false),
            GlobalRenderMode = ResolveString(
                allowTenantRenderOverride ? (await _tenantSettingRepository.GetByTenantAndKey(tenantId, GovernanceSettingKeys.Routing.RenderPolicy.Fallback.RenderMode))?.Value : null,
                systemGlobalRenderMode?.Value,
                "InteractiveServer",
                allowTenantRenderOverride),
            GlobalPrerenderEnabled = allowTenantRenderOverride
                ? ResolveBoolean(
                    (await _tenantSettingRepository.GetByTenantAndKey(tenantId, GovernanceSettingKeys.Routing.RenderPolicy.Fallback.PrerenderEnabled))?.Value,
                    systemGlobalPrerender?.Value,
                    false,
                    true)
                : DeserializeBoolean(systemGlobalPrerender?.Value, false),
            PublicSeoRenderMode = ResolveString(
                canOverridePublicSeo ? (await _tenantSettingRepository.GetByTenantAndKey(tenantId, GovernanceSettingKeys.Routing.RenderPolicy.PublicSeo.RenderMode))?.Value : null,
                systemPublicSeoRenderMode?.Value,
                string.Empty,
                canOverridePublicSeo),
            PublicSeoPrerenderEnabled = canOverridePublicSeo
                ? ResolveBoolean(
                    (await _tenantSettingRepository.GetByTenantAndKey(tenantId, GovernanceSettingKeys.Routing.RenderPolicy.PublicSeo.PrerenderEnabled))?.Value,
                    systemPublicSeoPrerender?.Value,
                    false,
                    true)
                : DeserializeBoolean(systemPublicSeoPrerender?.Value, false),
            OperationalRenderMode = ResolveString(
                canOverrideOperational ? (await _tenantSettingRepository.GetByTenantAndKey(tenantId, GovernanceSettingKeys.Routing.RenderPolicy.Operational.RenderMode))?.Value : null,
                systemOperationalRenderMode?.Value,
                string.Empty,
                canOverrideOperational),
            OperationalPrerenderEnabled = canOverrideOperational
                ? ResolveBoolean(
                    (await _tenantSettingRepository.GetByTenantAndKey(tenantId, GovernanceSettingKeys.Routing.RenderPolicy.Operational.PrerenderEnabled))?.Value,
                    systemOperationalPrerender?.Value,
                    false,
                    true)
                : DeserializeBoolean(systemOperationalPrerender?.Value, false),
            AdminRenderMode = ResolveString(
                canOverrideAdmin ? (await _tenantSettingRepository.GetByTenantAndKey(tenantId, GovernanceSettingKeys.Routing.RenderPolicy.Admin.RenderMode))?.Value : null,
                systemAdminRenderMode?.Value,
                string.Empty,
                canOverrideAdmin),
            AdminPrerenderEnabled = canOverrideAdmin
                ? ResolveBoolean(
                    (await _tenantSettingRepository.GetByTenantAndKey(tenantId, GovernanceSettingKeys.Routing.RenderPolicy.Admin.PrerenderEnabled))?.Value,
                    systemAdminPrerender?.Value,
                    false,
                    true)
                : DeserializeBoolean(systemAdminPrerender?.Value, false),
            CanOverrideRenderPolicy = allowTenantRenderOverride,
            CanOverridePublicSeoRenderPolicy = canOverridePublicSeo,
            CanOverrideOperationalRenderPolicy = canOverrideOperational,
            CanOverrideAdminRenderPolicy = canOverrideAdmin,
            CanOverrideSmtp = !isMultiTenant || !DeserializeBoolean(systemLockSmtp?.Value, true),
            CanOverrideStorage = !isMultiTenant || !DeserializeBoolean(systemLockStorage?.Value, true),
            CanOverrideAnalytics = !isMultiTenant || !DeserializeBoolean(systemLockAnalytics?.Value, true),
            CanOverrideAiAssistant = !isMultiTenant || !DeserializeBoolean(systemLockAiAssistant?.Value, true),
            AiAssistantEnabled = ResolveBoolean(
                tenantAiAssistantEnabled?.Value,
                systemAiAssistantEnabled?.Value,
                false,
                !isMultiTenant || !DeserializeBoolean(systemLockAiAssistant?.Value, true)),
            AiAssistantProvider = ResolveString(
                tenantAiAssistantProvider?.Value,
                systemAiAssistantProvider?.Value,
                "none",
                !isMultiTenant || !DeserializeBoolean(systemLockAiAssistant?.Value, true)),
            AiAssistantEndpointUrl = ResolveString(
                tenantAiAssistantEndpointUrl?.Value,
                systemAiAssistantEndpointUrl?.Value,
                string.Empty,
                !isMultiTenant || !DeserializeBoolean(systemLockAiAssistant?.Value, true)),
            AiAssistantApiKey = ResolveString(
                tenantAiAssistantApiKey?.Value,
                systemAiAssistantApiKey?.Value,
                string.Empty,
                !isMultiTenant || !DeserializeBoolean(systemLockAiAssistant?.Value, true)),
            AiAssistantModelId = ResolveString(
                tenantAiAssistantModelId?.Value,
                systemAiAssistantModelId?.Value,
                string.Empty,
                !isMultiTenant || !DeserializeBoolean(systemLockAiAssistant?.Value, true)),
            AiAssistantAllowAnonymousAccess = ResolveBoolean(
                tenantAiAssistantAllowAnonymousAccess?.Value,
                systemAiAssistantAllowAnonymousAccess?.Value,
                false,
                !isMultiTenant || !DeserializeBoolean(systemLockAiAssistant?.Value, true)),
            CommunityGuidelinesContent = ResolveString(
                tenantCommunityGuidelines?.Value,
                systemCommunityGuidelines?.Value,
                DefaultCommunityGuidelinesContent,
                systemCommunityGuidelines?.IsLocked != true),
            CanOverrideCommunityGuidelines = systemCommunityGuidelines?.IsLocked != true
        };
    }
}
