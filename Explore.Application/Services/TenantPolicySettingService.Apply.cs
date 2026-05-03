// ABOUTME: Write path for tenant policy settings — applies overrides while enforcing instance-level delegation constraints.
// ABOUTME: Partial class containing ApplyTenantSettingsAsync and its per-field override helpers.

using System.Text.Json;
using Explore.Application.Contracts.Infrastructure;
using Explore.Application.DTOs.TenantPolicy;
using Explore.Application.Notifications;
using Explore.Domain;
using Explore.Domain.Constants;

namespace Explore.Application.Services;

public partial class TenantPolicySettingService
{
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

        await SetBooleanTenantOverrideAsync(
            tenantId,
            GovernanceSettingKeys.PublicExperience.AnnouncementBarEnabled,
            settings.AnnouncementBarEnabled,
            true,
            actorUserId);

        await SetStringTenantOverrideAsync(
            tenantId,
            GovernanceSettingKeys.PublicExperience.AnnouncementBarMessage,
            settings.AnnouncementBarMessage,
            true,
            actorUserId);

        await SetStringTenantOverrideAsync(
            tenantId,
            GovernanceSettingKeys.PublicExperience.AnnouncementBarLinkText,
            settings.AnnouncementBarLinkText,
            true,
            actorUserId);

        await SetStringTenantOverrideAsync(
            tenantId,
            GovernanceSettingKeys.PublicExperience.AnnouncementBarLinkUrl,
            settings.AnnouncementBarLinkUrl,
            true,
            actorUserId);

        if (settings.ForceAnnouncementBarRedisplay)
        {
            var revisionSetting = await _tenantSettingRepository.GetByTenantAndKey(
                tenantId,
                GovernanceSettingKeys.PublicExperience.AnnouncementBarRevision);
            var nextRevision = DeserializeInteger(revisionSetting?.Value, 0) + 1;

            await UpsertTenantOverrideAsync(
                tenantId,
                GovernanceSettingKeys.PublicExperience.AnnouncementBarRevision,
                JsonSerializer.Serialize(nextRevision),
                actorUserId);
        }

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

        await SetBooleanTenantOverrideAsync(
            tenantId,
            GovernanceSettingKeys.AiAssistant.AllowAnonymousAccess,
            settings.AiAssistantAllowAnonymousAccess,
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

        _ = _mediator.Publish(new SettingChangedNotification(
            settingKey, oldValue, value, SettingSource.TenantOverride, tenantId, actorUserId, DateTime.UtcNow));
    }
}
