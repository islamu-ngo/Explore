// ABOUTME: Write path for tenant policy settings — applies overrides while enforcing instance-level delegation constraints.
// ABOUTME: Partial class containing ApplyTenantSettingsAsync and its per-field override helpers.

using System.Text.Json;
using Explore.Application.Contracts.Infrastructure;
using Explore.Application.DTOs.TenantPolicy;
using Explore.Application.Exceptions;
using Explore.Application.Notifications;
using Explore.Domain;
using Explore.Domain.Constants;
using FluentValidation.Results;

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
        var homePageSetting = await _systemSettingRepository.GetByKey(GovernanceSettingKeys.Routing.DefaultPublicHomePage);
        var allowCustomDomainSetting = await _systemSettingRepository.GetByKey(GovernanceSettingKeys.Domains.AllowTenantCustomDomain);
        var subdomainSetting = await _systemSettingRepository.GetByKey(GovernanceSettingKeys.Domains.TenantSubdomain);
        var customDomainSetting = await _systemSettingRepository.GetByKey(GovernanceSettingKeys.Domains.TenantCustomDomain);
        var communityGuidelinesSetting = await _systemSettingRepository.GetByKey(GovernanceSettingKeys.Policies.CommunityGuidelinesContent);
        var allowTenantRenderOverrideSetting = await _systemSettingRepository.GetByKey(GovernanceSettingKeys.Routing.RenderPolicy.AllowTenantOverride);
        var lockPublicSeoSetting = await _systemSettingRepository.GetByKey(GovernanceSettingKeys.Routing.RenderPolicy.LockTenantPublicSeo);
        var lockOperationalSetting = await _systemSettingRepository.GetByKey(GovernanceSettingKeys.Routing.RenderPolicy.LockTenantOperational);
        var lockAdminSetting = await _systemSettingRepository.GetByKey(GovernanceSettingKeys.Routing.RenderPolicy.LockTenantAdmin);
        var lockAiAssistantSetting = await _systemSettingRepository.GetByKey(GovernanceSettingKeys.TenantDelegation.LockAiAssistant);
        var lockMcpSetting = await _systemSettingRepository.GetByKey(GovernanceSettingKeys.TenantDelegation.LockMcp);
        var lockMcpLegacySseSetting = await _systemSettingRepository.GetByKey(GovernanceSettingKeys.TenantDelegation.LockMcpLegacySse);
        var tenant = await _tenantRepository.GetById(tenantId);
        var fallbackSubdomain = NormalizeSubdomain(tenant?.Slug) ?? "default";
        var isMultiTenant = DeserializeString(deploymentModeSetting?.Value, "SingleTenant").Equals("MultiTenant", StringComparison.OrdinalIgnoreCase);

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
        var aiAssistantProvider = NormalizeAiAssistantProvider(settings.AiAssistantProvider, settings.AiAssistantEnabled);
        var usesExternalProvider = aiAssistantProvider is "openai-compatible" or "anthropic-compatible";
        var aiAssistantAllowedModelIds = usesExternalProvider
            ? NormalizeAiModelIds([settings.AiAssistantModelId], settings.AiAssistantAllowedModelIds)
            : [];

        if (canOverrideAiAssistant)
        {
            ValidateAiAssistantSettings(settings, aiAssistantProvider);
        }

        await SetBooleanTenantOverrideAsync(
            tenantId,
            GovernanceSettingKeys.AiAssistant.Enabled,
            settings.AiAssistantEnabled,
            canOverrideAiAssistant,
            actorUserId);

        await SetStringTenantOverrideAsync(
            tenantId,
            GovernanceSettingKeys.AiAssistant.Provider,
            aiAssistantProvider,
            canOverrideAiAssistant,
            actorUserId);

        await SetStringTenantOverrideAsync(
            tenantId,
            GovernanceSettingKeys.AiAssistant.EndpointUrl,
            usesExternalProvider ? settings.AiAssistantEndpointUrl : string.Empty,
            canOverrideAiAssistant,
            actorUserId);

        await SetStringTenantOverrideAsync(
            tenantId,
            GovernanceSettingKeys.AiAssistant.ApiKey,
            usesExternalProvider ? settings.AiAssistantApiKey : string.Empty,
            canOverrideAiAssistant,
            actorUserId);

        await SetStringTenantOverrideAsync(
            tenantId,
            GovernanceSettingKeys.AiAssistant.ModelId,
            usesExternalProvider ? settings.AiAssistantModelId : string.Empty,
            canOverrideAiAssistant,
            actorUserId);

        await SetStringListTenantOverrideAsync(
            tenantId,
            GovernanceSettingKeys.AiAssistant.AllowedModelIds,
            aiAssistantAllowedModelIds,
            canOverrideAiAssistant && usesExternalProvider,
            actorUserId);

        await SetBooleanTenantOverrideAsync(
            tenantId,
            GovernanceSettingKeys.AiAssistant.AllowAnonymousAccess,
            settings.AiAssistantAllowAnonymousAccess,
            canOverrideAiAssistant,
            actorUserId);

        var canOverrideMcp = !isMultiTenant || !DeserializeBoolean(lockMcpSetting?.Value, true);
        var canOverrideMcpLegacySse = !isMultiTenant || !DeserializeBoolean(lockMcpLegacySseSetting?.Value, true);

        await SetBooleanTenantOverrideAsync(
            tenantId,
            GovernanceSettingKeys.Mcp.Enabled,
            settings.McpEnabled,
            canOverrideMcp,
            actorUserId);

        await SetBooleanTenantOverrideAsync(
            tenantId,
            GovernanceSettingKeys.Mcp.EnableLegacySse,
            settings.McpEnableLegacySse,
            canOverrideMcpLegacySse,
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

    private async Task SetStringListTenantOverrideAsync(
        Guid tenantId,
        string settingKey,
        IReadOnlyList<string> values,
        bool allowTenantOverride,
        Guid? actorUserId)
    {
        var normalizedValues = NormalizeAiModelIds(values);

        if (!allowTenantOverride || normalizedValues.Count == 0)
        {
            await _tenantSettingRepository.RemoveOverride(tenantId, settingKey);
            return;
        }

        await UpsertTenantOverrideAsync(
            tenantId,
            settingKey,
            JsonSerializer.Serialize(normalizedValues),
            actorUserId);
    }

    private static void ValidateAiAssistantSettings(UpdateTenantPolicyRequest settings, string provider)
    {
        if (!settings.AiAssistantEnabled)
        {
            return;
        }

        var failures = new List<ValidationFailure>();
        if (provider is not "fake" and not "openai-compatible" and not "anthropic-compatible")
        {
            failures.Add(new ValidationFailure(
                nameof(settings.AiAssistantProvider),
                "AI Assistant provider must be OpenAI-compatible, Anthropic-compatible, or Fake."));
        }

        if (provider is "openai-compatible" or "anthropic-compatible")
        {
            if (!HasAbsoluteHttpUrl(settings.AiAssistantEndpointUrl))
            {
                failures.Add(new ValidationFailure(
                    nameof(settings.AiAssistantEndpointUrl),
                    "AI Assistant endpoint URL must be an absolute HTTP or HTTPS URL."));
            }

            if (string.IsNullOrWhiteSpace(settings.AiAssistantModelId))
            {
                failures.Add(new ValidationFailure(
                    nameof(settings.AiAssistantModelId),
                    "AI Assistant model ID is required for OpenAI-compatible providers."));
            }
        }

        if (failures.Count > 0)
        {
            throw new ValidationException(new ValidationResult(failures));
        }
    }

    private static string NormalizeAiAssistantProvider(string? provider, bool enabled)
    {
        var normalized = provider?.Trim().ToLowerInvariant() ?? string.Empty;

        if (string.IsNullOrWhiteSpace(normalized) || (enabled && normalized == "none"))
        {
            return enabled ? "openai-compatible" : "none";
        }

        if (!enabled && normalized is not "none" and not "fake" and not "openai-compatible" and not "anthropic-compatible")
        {
            return "none";
        }

        return normalized;
    }

    private static bool HasAbsoluteHttpUrl(string? value)
    {
        return Uri.TryCreate(value?.Trim(), UriKind.Absolute, out var uri)
            && (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps);
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
