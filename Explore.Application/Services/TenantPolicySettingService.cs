// ABOUTME: Service implementation for managing tenant policy settings with instance-level delegation constraints.
// ABOUTME: Applies tenant overrides with enforcement of instance-level delegation constraints.

using System.Text.Json;
using Explore.Application.Contracts.Persistence;
using Explore.Application.Contracts.Services;
using Explore.Application.DTOs.Onboarding;
using Explore.Domain;
using Explore.Domain.Constants;

namespace Explore.Application.Services;

public class TenantPolicySettingService : ITenantPolicySettingService
{
    private const string DefaultBrandDisplayName = "ISLAMU Explore";
    private const string DefaultPublicHomePage = "EventList";

    private readonly ISystemSettingRepository _systemSettingRepository;
    private readonly ITenantSettingRepository _tenantSettingRepository;
    private readonly ITenantRepository _tenantRepository;

    public TenantPolicySettingService(
        ISystemSettingRepository systemSettingRepository,
        ITenantSettingRepository tenantSettingRepository,
        ITenantRepository tenantRepository)
    {
        _systemSettingRepository = systemSettingRepository;
        _tenantSettingRepository = tenantSettingRepository;
        _tenantRepository = tenantRepository;
    }

    public async Task<TenantPolicySettingsDto> ReadEffectiveTenantSettingsAsync(Guid tenantId)
    {
        var systemUserSubmission = await _systemSettingRepository.GetByKey(GovernanceSettingKeys.EventsUserSubmissionEnabled);
        var systemRequireApproval = await _systemSettingRepository.GetByKey(GovernanceSettingKeys.EventsRequireApproval);
        var systemRequireVerification = await _systemSettingRepository.GetByKey(GovernanceSettingKeys.OrganizationsVerificationRequired);
        var systemTenantCanOmitVerification = await _systemSettingRepository.GetByKey(GovernanceSettingKeys.OrganizationsTenantCanOmitVerification);
        var systemDeploymentMode = await _systemSettingRepository.GetByKey(GovernanceSettingKeys.DeploymentMode);
        var systemTenantWhiteLabeling = await _systemSettingRepository.GetByKey(GovernanceSettingKeys.TenantWhiteLabelingEnabled);
        var systemHomePage = await _systemSettingRepository.GetByKey(GovernanceSettingKeys.RoutingDefaultPublicHomePage);
        var systemInstanceBaseDomain = await _systemSettingRepository.GetByKey(GovernanceSettingKeys.DomainsInstanceBaseDomain);
        var systemAllowCustomDomain = await _systemSettingRepository.GetByKey(GovernanceSettingKeys.DomainsAllowTenantCustomDomain);
        var systemTenantSubdomain = await _systemSettingRepository.GetByKey(GovernanceSettingKeys.DomainsTenantSubdomain);
        var systemTenantCustomDomain = await _systemSettingRepository.GetByKey(GovernanceSettingKeys.DomainsTenantCustomDomain);
        var systemBrandDisplayName = await _systemSettingRepository.GetByKey(GovernanceSettingKeys.BrandingDisplayName);
        var systemBrandLogoUrl = await _systemSettingRepository.GetByKey(GovernanceSettingKeys.BrandingLogoUrl);
        var systemBrandFaviconUrl = await _systemSettingRepository.GetByKey(GovernanceSettingKeys.BrandingFaviconUrl);
        var systemBrandCustomCssUrl = await _systemSettingRepository.GetByKey(GovernanceSettingKeys.BrandingCustomCssUrl);

        var tenantUserSubmission = await _tenantSettingRepository.GetByTenantAndKey(tenantId, GovernanceSettingKeys.EventsUserSubmissionEnabled);
        var tenantRequireApproval = await _tenantSettingRepository.GetByTenantAndKey(tenantId, GovernanceSettingKeys.EventsRequireApproval);
        var tenantRequireVerification = await _tenantSettingRepository.GetByTenantAndKey(tenantId, GovernanceSettingKeys.OrganizationsVerificationRequired);
        var tenantHomePage = await _tenantSettingRepository.GetByTenantAndKey(tenantId, GovernanceSettingKeys.RoutingDefaultPublicHomePage);
        var tenantSubdomain = await _tenantSettingRepository.GetByTenantAndKey(tenantId, GovernanceSettingKeys.DomainsTenantSubdomain);
        var tenantCustomDomain = await _tenantSettingRepository.GetByTenantAndKey(tenantId, GovernanceSettingKeys.DomainsTenantCustomDomain);
        var tenantBrandDisplayName = await _tenantSettingRepository.GetByTenantAndKey(tenantId, GovernanceSettingKeys.BrandingDisplayName);
        var tenantBrandLogoUrl = await _tenantSettingRepository.GetByTenantAndKey(tenantId, GovernanceSettingKeys.BrandingLogoUrl);
        var tenantBrandFaviconUrl = await _tenantSettingRepository.GetByTenantAndKey(tenantId, GovernanceSettingKeys.BrandingFaviconUrl);
        var tenantBrandCustomCssUrl = await _tenantSettingRepository.GetByTenantAndKey(tenantId, GovernanceSettingKeys.BrandingCustomCssUrl);

        var tenant = await _tenantRepository.GetById(tenantId);
        var fallbackSubdomain = NormalizeSubdomain(tenant?.Slug) ?? "default";
        var isMultiTenant = DeserializeString(systemDeploymentMode?.Value, "SingleTenant").Equals("MultiTenant", StringComparison.OrdinalIgnoreCase);
        var isTenantWhiteLabelingEnabled = isMultiTenant && DeserializeBoolean(systemTenantWhiteLabeling?.Value, false);
        var canOverrideHomePage = systemHomePage?.IsLocked != true;
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
            RequireEventApproval = ResolveBoolean(
                tenantRequireApproval?.Value,
                systemRequireApproval?.Value,
                false,
                systemRequireApproval?.IsLocked != true),
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
                isTenantWhiteLabelingEnabled && systemBrandDisplayName?.IsLocked != true),
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
            CanOverrideBrandDisplayName = isTenantWhiteLabelingEnabled && systemBrandDisplayName?.IsLocked != true,
            CanOverrideBrandLogoUrl = isTenantWhiteLabelingEnabled && systemBrandLogoUrl?.IsLocked != true,
            CanOverrideBrandFaviconUrl = isTenantWhiteLabelingEnabled && systemBrandFaviconUrl?.IsLocked != true,
            CanOverrideBrandCustomCssUrl = isTenantWhiteLabelingEnabled && systemBrandCustomCssUrl?.IsLocked != true
        };
    }

    public async Task ApplyTenantSettingsAsync(Guid tenantId, Guid? actorUserId, TenantPolicySettingsDto settings)
    {
        var userSubmissionSetting = await _systemSettingRepository.GetByKey(GovernanceSettingKeys.EventsUserSubmissionEnabled);
        var requireApprovalSetting = await _systemSettingRepository.GetByKey(GovernanceSettingKeys.EventsRequireApproval);
        var requireVerificationSetting = await _systemSettingRepository.GetByKey(GovernanceSettingKeys.OrganizationsVerificationRequired);
        var canOmitVerificationSetting = await _systemSettingRepository.GetByKey(GovernanceSettingKeys.OrganizationsTenantCanOmitVerification);
        var deploymentModeSetting = await _systemSettingRepository.GetByKey(GovernanceSettingKeys.DeploymentMode);
        var tenantWhiteLabelingSetting = await _systemSettingRepository.GetByKey(GovernanceSettingKeys.TenantWhiteLabelingEnabled);
        var homePageSetting = await _systemSettingRepository.GetByKey(GovernanceSettingKeys.RoutingDefaultPublicHomePage);
        var allowCustomDomainSetting = await _systemSettingRepository.GetByKey(GovernanceSettingKeys.DomainsAllowTenantCustomDomain);
        var subdomainSetting = await _systemSettingRepository.GetByKey(GovernanceSettingKeys.DomainsTenantSubdomain);
        var customDomainSetting = await _systemSettingRepository.GetByKey(GovernanceSettingKeys.DomainsTenantCustomDomain);
        var brandDisplayNameSetting = await _systemSettingRepository.GetByKey(GovernanceSettingKeys.BrandingDisplayName);
        var brandLogoUrlSetting = await _systemSettingRepository.GetByKey(GovernanceSettingKeys.BrandingLogoUrl);
        var brandFaviconUrlSetting = await _systemSettingRepository.GetByKey(GovernanceSettingKeys.BrandingFaviconUrl);
        var brandCustomCssUrlSetting = await _systemSettingRepository.GetByKey(GovernanceSettingKeys.BrandingCustomCssUrl);
        var tenant = await _tenantRepository.GetById(tenantId);
        var fallbackSubdomain = NormalizeSubdomain(tenant?.Slug) ?? "default";
        var isMultiTenant = DeserializeString(deploymentModeSetting?.Value, "SingleTenant").Equals("MultiTenant", StringComparison.OrdinalIgnoreCase);
        var isTenantWhiteLabelingEnabled = isMultiTenant && DeserializeBoolean(tenantWhiteLabelingSetting?.Value, false);

        await SetBooleanTenantOverrideAsync(
            tenantId,
            GovernanceSettingKeys.EventsUserSubmissionEnabled,
            settings.AllowUserSubmittedEvents,
            userSubmissionSetting?.IsLocked != true,
            actorUserId);

        await SetBooleanTenantOverrideAsync(
            tenantId,
            GovernanceSettingKeys.EventsRequireApproval,
            settings.RequireEventApproval,
            requireApprovalSetting?.IsLocked != true,
            actorUserId);

        var canTenantOmitVerification = DeserializeBoolean(canOmitVerificationSetting?.Value, false)
            && requireVerificationSetting?.IsLocked != true;
        var effectiveRequireVerification = canTenantOmitVerification
            ? settings.RequireOrganizationVerification
            : DeserializeBoolean(requireVerificationSetting?.Value, true);

        await SetBooleanTenantOverrideAsync(
            tenantId,
            GovernanceSettingKeys.OrganizationsVerificationRequired,
            effectiveRequireVerification,
            canTenantOmitVerification,
            actorUserId);

        await SetStringTenantOverrideAsync(
            tenantId,
            GovernanceSettingKeys.RoutingDefaultPublicHomePage,
            NormalizeHomePage(settings.PreferredHomePage),
            homePageSetting?.IsLocked != true,
            actorUserId);

        await SetStringTenantOverrideAsync(
            tenantId,
            GovernanceSettingKeys.DomainsTenantSubdomain,
            NormalizeSubdomain(settings.Subdomain) ?? fallbackSubdomain,
            subdomainSetting?.IsLocked != true,
            actorUserId);

        await SetStringTenantOverrideAsync(
            tenantId,
            GovernanceSettingKeys.DomainsTenantCustomDomain,
            NormalizeOptionalHost(settings.CustomDomain),
            customDomainSetting?.IsLocked != true && DeserializeBoolean(allowCustomDomainSetting?.Value, true),
            actorUserId);

        await SetStringTenantOverrideAsync(
            tenantId,
            GovernanceSettingKeys.BrandingDisplayName,
            settings.BrandDisplayName,
            isTenantWhiteLabelingEnabled && brandDisplayNameSetting?.IsLocked != true,
            actorUserId);

        await SetStringTenantOverrideAsync(
            tenantId,
            GovernanceSettingKeys.BrandingLogoUrl,
            settings.BrandLogoUrl,
            isTenantWhiteLabelingEnabled && brandLogoUrlSetting?.IsLocked != true,
            actorUserId);

        await SetStringTenantOverrideAsync(
            tenantId,
            GovernanceSettingKeys.BrandingFaviconUrl,
            settings.BrandFaviconUrl,
            isTenantWhiteLabelingEnabled && brandFaviconUrlSetting?.IsLocked != true,
            actorUserId);

        await SetStringTenantOverrideAsync(
            tenantId,
            GovernanceSettingKeys.BrandingCustomCssUrl,
            settings.BrandCustomCssUrl,
            isTenantWhiteLabelingEnabled && brandCustomCssUrlSetting?.IsLocked != true,
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

            return;
        }

        existing.Value = value;
        existing.UpdatedAt = DateTime.UtcNow;
        existing.UpdatedBy = actorUserId;
        await _tenantSettingRepository.Update(existing);
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
