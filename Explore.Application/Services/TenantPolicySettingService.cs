// ABOUTME: Service implementation for managing tenant policy settings with instance-level delegation constraints.
// ABOUTME: Applies tenant overrides with enforcement of instance-level delegation constraints.

using System.Text.Json;
using Explore.Application.Contracts.Infrastructure;
using Explore.Application.Contracts.Persistence;
using Explore.Application.Contracts.Services;
using Explore.Application.DTOs.Onboarding;
using Explore.Application.DTOs.TenantPolicy;
using Explore.Application.Notifications;
using Explore.Domain;
using Explore.Domain.Constants;
using MediatR;

namespace Explore.Application.Services;

public class TenantPolicySettingService : ITenantPolicySettingService
{
    private const string DefaultBrandDisplayName = "";
    private const string DefaultPublicHomePage = "EventList";
    private const string DefaultCommunityGuidelinesContent =
        "# Community Guidelines\n\n" +
        "## Our Community\n\n" +
        "This platform is a space for sharing events with our community. To ensure a positive experience for everyone, we ask all event organizers and participants to follow these guidelines.\n\n" +
        "## Event Posting Standards\n\n" +
        "**Accuracy and Transparency**\n" +
        "- Provide complete and accurate event information including date, time, location, and organizer details.\n" +
        "- Clearly describe what attendees can expect from your event.\n" +
        "- Notify attendees promptly of any changes, cancellations, or updates.\n\n" +
        "**Appropriate Content**\n" +
        "- Events must be relevant to the community and align with the platform's purpose.\n" +
        "- Event titles and descriptions must be truthful and not misleading.\n" +
        "- Do not post duplicate or spam events.\n\n" +
        "**Inclusive and Respectful Language**\n" +
        "- Use welcoming, inclusive language in event descriptions and communications.\n" +
        "- Events must not promote discrimination based on race, ethnicity, religion, gender, disability, or any other protected characteristic.\n" +
        "- Maintain respectful communication with attendees and other organizers.\n\n" +
        "## Prohibited Content\n\n" +
        "The following types of events and content are not permitted on this platform:\n\n" +
        "- Events that promote illegal activities or violate applicable laws\n" +
        "- Hateful, discriminatory, or violent content\n" +
        "- Harassment or targeted abuse of individuals or groups\n" +
        "- Deceptive, fraudulent, or misleading events\n" +
        "- Spam or commercially exploitative content\n\n" +
        "## Participation Guidelines\n\n" +
        "**As an Attendee**\n" +
        "- Respect the organizer's event rules and code of conduct.\n" +
        "- Be courteous to other attendees and event staff.\n" +
        "- Cancel your registration if your plans change.\n\n" +
        "**As an Organizer**\n" +
        "- Respond to attendee inquiries in a timely manner.\n" +
        "- Enforce a safe and welcoming environment at your events.\n" +
        "- Honor the commitments made in your event listing.\n\n" +
        "## Reporting Violations\n\n" +
        "If you encounter content or behavior that violates these guidelines, please report it to the platform administrators. All reports are taken seriously.\n\n" +
        "## Consequences\n\n" +
        "Violations of these guidelines may result in removal of the event listing, a warning, temporary suspension, or permanent removal from the platform for serious or repeated violations.";

    private readonly ISystemSettingRepository _systemSettingRepository;
    private readonly ITenantSettingRepository _tenantSettingRepository;
    private readonly ITenantRepository _tenantRepository;
    private readonly IMediator _mediator;

    public TenantPolicySettingService(
        ISystemSettingRepository systemSettingRepository,
        ITenantSettingRepository tenantSettingRepository,
        ITenantRepository tenantRepository,
        IMediator mediator)
    {
        _systemSettingRepository = systemSettingRepository;
        _tenantSettingRepository = tenantSettingRepository;
        _tenantRepository = tenantRepository;
        _mediator = mediator;
    }

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
        var systemAiAssistantEndpointUrl = await _systemSettingRepository.GetByKey(GovernanceSettingKeys.AiAssistant.EndpointUrl);
        var systemAiAssistantApiKey = await _systemSettingRepository.GetByKey(GovernanceSettingKeys.AiAssistant.ApiKey);
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
        var tenantAiAssistantEnabled = await _tenantSettingRepository.GetByTenantAndKey(tenantId, GovernanceSettingKeys.AiAssistant.Enabled);
        var tenantAiAssistantEndpointUrl = await _tenantSettingRepository.GetByTenantAndKey(tenantId, GovernanceSettingKeys.AiAssistant.EndpointUrl);
        var tenantAiAssistantApiKey = await _tenantSettingRepository.GetByTenantAndKey(tenantId, GovernanceSettingKeys.AiAssistant.ApiKey);
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
            CommunityGuidelinesContent = ResolveString(
                tenantCommunityGuidelines?.Value,
                systemCommunityGuidelines?.Value,
                DefaultCommunityGuidelinesContent,
                systemCommunityGuidelines?.IsLocked != true),
            CanOverrideCommunityGuidelines = systemCommunityGuidelines?.IsLocked != true
        };
    }

    public async Task ApplyTenantSettingsAsync(Guid tenantId, Guid? actorUserId, UpdateTenantPolicyRequest settings)
    {
        var userSubmissionSetting = await _systemSettingRepository.GetByKey(GovernanceSettingKeys.Events.UserSubmissionEnabled);
        var orgSubmissionSetting = await _systemSettingRepository.GetByKey(GovernanceSettingKeys.Events.OrganizationSubmissionEnabled);
        var groupSubmissionSetting = await _systemSettingRepository.GetByKey(GovernanceSettingKeys.Events.GroupSubmissionEnabled);
        var requireApprovalSetting = await _systemSettingRepository.GetByKey(GovernanceSettingKeys.Events.RequireApproval);
        var eventCardClickSetting = await _systemSettingRepository.GetByKey(GovernanceSettingKeys.Events.CardClickOpensDetailPage);
        var requireVerificationSetting = await _systemSettingRepository.GetByKey(GovernanceSettingKeys.Organizations.VerificationRequired);
        var canOmitVerificationSetting = await _systemSettingRepository.GetByKey(GovernanceSettingKeys.Organizations.TenantCanOmitVerification);
        var orgSelfRegSetting = await _systemSettingRepository.GetByKey(GovernanceSettingKeys.Organizations.SelfRegistrationEnabled);
        var groupSelfRegSetting = await _systemSettingRepository.GetByKey(GovernanceSettingKeys.Groups.SelfRegistrationEnabled);
        var deploymentModeSetting = await _systemSettingRepository.GetByKey(GovernanceSettingKeys.Deployment.Mode);
        var tenantWhiteLabelingSetting = await _systemSettingRepository.GetByKey(GovernanceSettingKeys.Tenants.WhiteLabelingEnabled);
        var homePageSetting = await _systemSettingRepository.GetByKey(GovernanceSettingKeys.Routing.DefaultPublicHomePage);
        var allowCustomDomainSetting = await _systemSettingRepository.GetByKey(GovernanceSettingKeys.Domains.AllowTenantCustomDomain);
        var subdomainSetting = await _systemSettingRepository.GetByKey(GovernanceSettingKeys.Domains.TenantSubdomain);
        var customDomainSetting = await _systemSettingRepository.GetByKey(GovernanceSettingKeys.Domains.TenantCustomDomain);
        var brandDisplayNameSetting = await _systemSettingRepository.GetByKey(GovernanceSettingKeys.Branding.DisplayName);
        var brandLogoUrlSetting = await _systemSettingRepository.GetByKey(GovernanceSettingKeys.Branding.LogoUrl);
        var brandFaviconUrlSetting = await _systemSettingRepository.GetByKey(GovernanceSettingKeys.Branding.FaviconUrl);
        var brandCustomCssUrlSetting = await _systemSettingRepository.GetByKey(GovernanceSettingKeys.Branding.CustomCssUrl);
        var communityGuidelinesSetting = await _systemSettingRepository.GetByKey(GovernanceSettingKeys.Policies.CommunityGuidelinesContent);
        var allowTenantRenderOverrideSetting = await _systemSettingRepository.GetByKey(GovernanceSettingKeys.Routing.RenderPolicy.AllowTenantOverride);
        var lockPublicSeoSetting = await _systemSettingRepository.GetByKey(GovernanceSettingKeys.Routing.RenderPolicy.LockTenantPublicSeo);
        var lockOperationalSetting = await _systemSettingRepository.GetByKey(GovernanceSettingKeys.Routing.RenderPolicy.LockTenantOperational);
        var lockAdminSetting = await _systemSettingRepository.GetByKey(GovernanceSettingKeys.Routing.RenderPolicy.LockTenantAdmin);
        var lockAiAssistantSetting = await _systemSettingRepository.GetByKey(GovernanceSettingKeys.TenantDelegation.LockAiAssistant);
        var tenant = await _tenantRepository.GetById(tenantId);
        var fallbackSubdomain = NormalizeSubdomain(tenant?.Slug) ?? "default";
        var isMultiTenant = DeserializeString(deploymentModeSetting?.Value, "SingleTenant").Equals("MultiTenant", StringComparison.OrdinalIgnoreCase);
        var isTenantWhiteLabelingEnabled = isMultiTenant && DeserializeBoolean(tenantWhiteLabelingSetting?.Value, false);

        await SetBooleanTenantOverrideAsync(
            tenantId,
            GovernanceSettingKeys.Events.UserSubmissionEnabled,
            settings.AllowUserSubmittedEvents,
            userSubmissionSetting?.IsLocked != true,
            actorUserId);

        await SetBooleanTenantOverrideAsync(
            tenantId,
            GovernanceSettingKeys.Events.OrganizationSubmissionEnabled,
            settings.AllowOrganizationSubmittedEvents,
            orgSubmissionSetting?.IsLocked != true,
            actorUserId);

        await SetBooleanTenantOverrideAsync(
            tenantId,
            GovernanceSettingKeys.Events.GroupSubmissionEnabled,
            settings.AllowGroupSubmittedEvents,
            groupSubmissionSetting?.IsLocked != true,
            actorUserId);

        await SetBooleanTenantOverrideAsync(
            tenantId,
            GovernanceSettingKeys.Events.RequireApproval,
            settings.RequireEventApproval,
            requireApprovalSetting?.IsLocked != true,
            actorUserId);

        await SetBooleanTenantOverrideAsync(
            tenantId,
            GovernanceSettingKeys.Events.CardClickOpensDetailPage,
            settings.EventCardClickOpensDetailPage,
            eventCardClickSetting?.IsLocked != true,
            actorUserId);

        var canTenantOmitVerification = DeserializeBoolean(canOmitVerificationSetting?.Value, false)
            && requireVerificationSetting?.IsLocked != true;
        var effectiveRequireVerification = canTenantOmitVerification
            ? settings.RequireOrganizationVerification
            : DeserializeBoolean(requireVerificationSetting?.Value, true);

        await SetBooleanTenantOverrideAsync(
            tenantId,
            GovernanceSettingKeys.Organizations.VerificationRequired,
            effectiveRequireVerification,
            canTenantOmitVerification,
            actorUserId);

        await SetBooleanTenantOverrideAsync(
            tenantId,
            GovernanceSettingKeys.Organizations.SelfRegistrationEnabled,
            settings.AllowOrganizationSelfRegistration,
            orgSelfRegSetting?.IsLocked != true,
            actorUserId);

        await SetBooleanTenantOverrideAsync(
            tenantId,
            GovernanceSettingKeys.Groups.SelfRegistrationEnabled,
            settings.AllowGroupSelfRegistration,
            groupSelfRegSetting?.IsLocked != true,
            actorUserId);

        await SetStringTenantOverrideAsync(
            tenantId,
            GovernanceSettingKeys.Routing.DefaultPublicHomePage,
            NormalizeHomePage(settings.PreferredHomePage),
            homePageSetting?.IsLocked != true,
            actorUserId);

        await SetStringTenantOverrideAsync(
            tenantId,
            GovernanceSettingKeys.Domains.TenantSubdomain,
            NormalizeSubdomain(settings.Subdomain) ?? fallbackSubdomain,
            subdomainSetting?.IsLocked != true,
            actorUserId);

        await SetStringTenantOverrideAsync(
            tenantId,
            GovernanceSettingKeys.Domains.TenantCustomDomain,
            NormalizeOptionalHost(settings.CustomDomain),
            customDomainSetting?.IsLocked != true && DeserializeBoolean(allowCustomDomainSetting?.Value, true),
            actorUserId);

        await SetStringTenantOverrideAsync(
            tenantId,
            GovernanceSettingKeys.Branding.DisplayName,
            settings.BrandDisplayName,
            isTenantWhiteLabelingEnabled && brandDisplayNameSetting?.IsLocked != true,
            actorUserId);

        await SetStringTenantOverrideAsync(
            tenantId,
            GovernanceSettingKeys.Branding.LogoUrl,
            settings.BrandLogoUrl,
            isTenantWhiteLabelingEnabled && brandLogoUrlSetting?.IsLocked != true,
            actorUserId);

        await SetStringTenantOverrideAsync(
            tenantId,
            GovernanceSettingKeys.Branding.FaviconUrl,
            settings.BrandFaviconUrl,
            isTenantWhiteLabelingEnabled && brandFaviconUrlSetting?.IsLocked != true,
            actorUserId);

        await SetStringTenantOverrideAsync(
            tenantId,
            GovernanceSettingKeys.Branding.CustomCssUrl,
            settings.BrandCustomCssUrl,
            isTenantWhiteLabelingEnabled && brandCustomCssUrlSetting?.IsLocked != true,
            actorUserId);

        await SetStringTenantOverrideAsync(
            tenantId,
            GovernanceSettingKeys.Policies.CommunityGuidelinesContent,
            settings.CommunityGuidelinesContent,
            communityGuidelinesSetting?.IsLocked != true,
            actorUserId);

        var allowTenantRenderOverride = !isMultiTenant || DeserializeBoolean(allowTenantRenderOverrideSetting?.Value, false);
        var canOverridePublicSeo = allowTenantRenderOverride && (!isMultiTenant || !DeserializeBoolean(lockPublicSeoSetting?.Value, false));
        var canOverrideOperational = allowTenantRenderOverride && (!isMultiTenant || !DeserializeBoolean(lockOperationalSetting?.Value, false));
        var canOverrideAdmin = allowTenantRenderOverride && (!isMultiTenant || !DeserializeBoolean(lockAdminSetting?.Value, false));

        await SetStringTenantOverrideAsync(
            tenantId,
            GovernanceSettingKeys.Routing.RenderPolicy.Preset,
            settings.RenderPolicyPreset,
            allowTenantRenderOverride,
            actorUserId);

        await SetBooleanTenantOverrideAsync(
            tenantId,
            GovernanceSettingKeys.Routing.RenderPolicy.AdvancedEnabled,
            settings.EnableAdvancedRenderPolicyOverrides,
            allowTenantRenderOverride,
            actorUserId);

        await SetStringTenantOverrideAsync(
            tenantId,
            GovernanceSettingKeys.Routing.RenderPolicy.Fallback.RenderMode,
            settings.GlobalRenderMode,
            allowTenantRenderOverride,
            actorUserId);

        await SetBooleanTenantOverrideAsync(
            tenantId,
            GovernanceSettingKeys.Routing.RenderPolicy.Fallback.PrerenderEnabled,
            settings.GlobalPrerenderEnabled,
            allowTenantRenderOverride,
            actorUserId);

        await SetStringTenantOverrideAsync(
            tenantId,
            GovernanceSettingKeys.Routing.RenderPolicy.PublicSeo.RenderMode,
            settings.PublicSeoRenderMode,
            canOverridePublicSeo,
            actorUserId);

        await SetBooleanTenantOverrideAsync(
            tenantId,
            GovernanceSettingKeys.Routing.RenderPolicy.PublicSeo.PrerenderEnabled,
            settings.PublicSeoPrerenderEnabled,
            canOverridePublicSeo,
            actorUserId);

        await SetStringTenantOverrideAsync(
            tenantId,
            GovernanceSettingKeys.Routing.RenderPolicy.Operational.RenderMode,
            settings.OperationalRenderMode,
            canOverrideOperational,
            actorUserId);

        await SetBooleanTenantOverrideAsync(
            tenantId,
            GovernanceSettingKeys.Routing.RenderPolicy.Operational.PrerenderEnabled,
            settings.OperationalPrerenderEnabled,
            canOverrideOperational,
            actorUserId);

        await SetStringTenantOverrideAsync(
            tenantId,
            GovernanceSettingKeys.Routing.RenderPolicy.Admin.RenderMode,
            settings.AdminRenderMode,
            canOverrideAdmin,
            actorUserId);

        await SetBooleanTenantOverrideAsync(
            tenantId,
            GovernanceSettingKeys.Routing.RenderPolicy.Admin.PrerenderEnabled,
            settings.AdminPrerenderEnabled,
            canOverrideAdmin,
            actorUserId);

        var canOverrideAiAssistant = !isMultiTenant || !DeserializeBoolean(lockAiAssistantSetting?.Value, true);

        // AI enablement validation: cannot enable without a configured API key
        var effectiveAiEnabled = settings.AiAssistantEnabled
            && !string.IsNullOrWhiteSpace(settings.AiAssistantApiKey);

        await SetBooleanTenantOverrideAsync(
            tenantId,
            GovernanceSettingKeys.AiAssistant.Enabled,
            effectiveAiEnabled,
            canOverrideAiAssistant,
            actorUserId);

        await SetStringTenantOverrideAsync(
            tenantId,
            GovernanceSettingKeys.AiAssistant.EndpointUrl,
            settings.AiAssistantEndpointUrl,
            canOverrideAiAssistant,
            actorUserId);

        await SetStringTenantOverrideAsync(
            tenantId,
            GovernanceSettingKeys.AiAssistant.ApiKey,
            settings.AiAssistantApiKey,
            canOverrideAiAssistant,
            actorUserId);
    }

    private async Task SetBooleanTenantOverrideAsync(
        Guid tenantId,
        string settingKey,
        bool value,
        bool allowTenantOverride,
        Guid? actorUserId)
    {
        if (!allowTenantOverride)
        {
            await _tenantSettingRepository.RemoveOverride(tenantId, settingKey);
            return;
        }

        await UpsertTenantOverrideAsync(
            tenantId,
            settingKey,
            JsonSerializer.Serialize(value),
            actorUserId);
    }

    private async Task SetStringTenantOverrideAsync(
        Guid tenantId,
        string settingKey,
        string? value,
        bool allowTenantOverride,
        Guid? actorUserId)
    {
        if (!allowTenantOverride || string.IsNullOrWhiteSpace(value))
        {
            await _tenantSettingRepository.RemoveOverride(tenantId, settingKey);
            return;
        }

        await UpsertTenantOverrideAsync(
            tenantId,
            settingKey,
            JsonSerializer.Serialize(value.Trim()),
            actorUserId);
    }

    private async Task UpsertTenantOverrideAsync(
        Guid tenantId,
        string settingKey,
        string value,
        Guid? actorUserId)
    {
        var existing = await _tenantSettingRepository.GetByTenantAndKey(tenantId, settingKey);
        var oldValue = existing?.Value;

        if (existing == null)
        {
            await _tenantSettingRepository.Create(new TenantSetting
            {
                TenantId = tenantId,
                Tenant = null!,
                SettingKey = settingKey,
                Value = value,
                CreatedAt = DateTime.UtcNow,
                CreatedBy = actorUserId
            });
        }
        else
        {
            existing.Value = value;
            existing.UpdatedAt = DateTime.UtcNow;
            existing.UpdatedBy = actorUserId;
            await _tenantSettingRepository.Update(existing);
        }

        // Fire-and-forget: audit notification should not block the write path
        _ = _mediator.Publish(new SettingChangedNotification(
            settingKey, oldValue, value, SettingSource.TenantOverride, tenantId, actorUserId, DateTime.UtcNow));
    }

    private static bool ResolveBoolean(string? tenantOverrideValue, string? systemValue, bool fallback, bool allowTenantOverride)
    {
        if (allowTenantOverride && !string.IsNullOrWhiteSpace(tenantOverrideValue))
        {
            return DeserializeBoolean(tenantOverrideValue, fallback);
        }

        return DeserializeBoolean(systemValue, fallback);
    }

    private static string ResolveString(string? tenantOverrideValue, string? systemValue, string fallback, bool allowTenantOverride)
    {
        if (allowTenantOverride && !string.IsNullOrWhiteSpace(tenantOverrideValue))
        {
            return DeserializeString(tenantOverrideValue, fallback);
        }

        return DeserializeString(systemValue, fallback);
    }

    private static bool DeserializeBoolean(string? rawValue, bool fallback)
    {
        if (string.IsNullOrWhiteSpace(rawValue))
        {
            return fallback;
        }

        try
        {
            return JsonSerializer.Deserialize<bool>(rawValue);
        }
        catch
        {
            return bool.TryParse(rawValue, out var parsed) ? parsed : fallback;
        }
    }

    private static string DeserializeString(string? rawValue, string fallback)
    {
        if (string.IsNullOrWhiteSpace(rawValue))
        {
            return fallback;
        }

        try
        {
            var deserialized = JsonSerializer.Deserialize<string>(rawValue);
            return deserialized ?? fallback;
        }
        catch
        {
            return rawValue.Trim('"');
        }
    }

    private static string NormalizeHomePage(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return DefaultPublicHomePage;
        }

        return value.Equals("LandingPage", StringComparison.OrdinalIgnoreCase)
            ? "LandingPage"
            : "EventList";
    }

    private static string NormalizeOptionalHost(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return string.Empty;
        }

        var normalized = value.Trim().ToLowerInvariant();
        normalized = normalized.Replace("https://", string.Empty, StringComparison.OrdinalIgnoreCase);
        normalized = normalized.Replace("http://", string.Empty, StringComparison.OrdinalIgnoreCase);
        return normalized.Trim('/').Trim();
    }

    private static string? NormalizeSubdomain(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        var normalized = value.Trim().ToLowerInvariant();
        normalized = normalized.Replace(" ", "-");
        normalized = new string(normalized.Where(c => char.IsLetterOrDigit(c) || c == '-').ToArray());
        return string.IsNullOrWhiteSpace(normalized) ? null : normalized;
    }
}
