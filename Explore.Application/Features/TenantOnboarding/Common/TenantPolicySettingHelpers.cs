// ABOUTME: Shared helper methods for resolving and persisting tenant policy onboarding settings.
// ABOUTME: Applies tenant overrides with enforcement of instance-level delegation constraints.

using System.Linq;
using System.Text.Json;
using Explore.Application.Contracts.Persistence;
using Explore.Application.DTOs.Onboarding;
using Explore.Domain;
using Explore.Domain.Constants;

namespace Explore.Application.Features.TenantOnboarding.Common;

internal static class TenantPolicySettingHelpers
{
    private const string DefaultBrandDisplayName = "ISLAMU Explore";
    private const string DefaultPublicHomePage = "EventList";

    internal static async Task<TenantPolicySettingsDto> ReadEffectiveTenantSettingsAsync(
        ISystemSettingRepository systemSettingRepository,
        ITenantSettingRepository tenantSettingRepository,
        ITenantRepository tenantRepository,
        Guid tenantId)
    {
        var systemUserSubmission = await systemSettingRepository.GetByKey(GovernanceSettingKeys.Events.UserSubmissionEnabled);
        var systemOrgSubmission = await systemSettingRepository.GetByKey(GovernanceSettingKeys.Events.OrganizationSubmissionEnabled);
        var systemGroupSubmission = await systemSettingRepository.GetByKey(GovernanceSettingKeys.Events.GroupSubmissionEnabled);
        var systemRequireApproval = await systemSettingRepository.GetByKey(GovernanceSettingKeys.Events.RequireApproval);
        var systemRequireVerification = await systemSettingRepository.GetByKey(GovernanceSettingKeys.Organizations.VerificationRequired);
        var systemTenantCanOmitVerification = await systemSettingRepository.GetByKey(GovernanceSettingKeys.Organizations.TenantCanOmitVerification);
        var systemOrgSelfRegistration = await systemSettingRepository.GetByKey(GovernanceSettingKeys.Organizations.SelfRegistrationEnabled);
        var systemGroupSelfRegistration = await systemSettingRepository.GetByKey(GovernanceSettingKeys.Groups.SelfRegistrationEnabled);
        var systemHomePage = await systemSettingRepository.GetByKey(GovernanceSettingKeys.Routing.DefaultPublicHomePage);
        var systemInstanceBaseDomain = await systemSettingRepository.GetByKey(GovernanceSettingKeys.Domains.InstanceBaseDomain);
        var systemAllowCustomDomain = await systemSettingRepository.GetByKey(GovernanceSettingKeys.Domains.AllowTenantCustomDomain);
        var systemTenantSubdomain = await systemSettingRepository.GetByKey(GovernanceSettingKeys.Domains.TenantSubdomain);
        var systemTenantCustomDomain = await systemSettingRepository.GetByKey(GovernanceSettingKeys.Domains.TenantCustomDomain);
        var systemBrandDisplayName = await systemSettingRepository.GetByKey(GovernanceSettingKeys.Branding.DisplayName);
        var systemBrandLogoUrl = await systemSettingRepository.GetByKey(GovernanceSettingKeys.Branding.LogoUrl);
        var systemBrandFaviconUrl = await systemSettingRepository.GetByKey(GovernanceSettingKeys.Branding.FaviconUrl);
        var systemBrandCustomCssUrl = await systemSettingRepository.GetByKey(GovernanceSettingKeys.Branding.CustomCssUrl);

        var tenantUserSubmission = await tenantSettingRepository.GetByTenantAndKey(tenantId, GovernanceSettingKeys.Events.UserSubmissionEnabled);
        var tenantOrgSubmission = await tenantSettingRepository.GetByTenantAndKey(tenantId, GovernanceSettingKeys.Events.OrganizationSubmissionEnabled);
        var tenantGroupSubmission = await tenantSettingRepository.GetByTenantAndKey(tenantId, GovernanceSettingKeys.Events.GroupSubmissionEnabled);
        var tenantRequireApproval = await tenantSettingRepository.GetByTenantAndKey(tenantId, GovernanceSettingKeys.Events.RequireApproval);
        var tenantRequireVerification = await tenantSettingRepository.GetByTenantAndKey(tenantId, GovernanceSettingKeys.Organizations.VerificationRequired);
        var tenantOrgSelfRegistration = await tenantSettingRepository.GetByTenantAndKey(tenantId, GovernanceSettingKeys.Organizations.SelfRegistrationEnabled);
        var tenantGroupSelfRegistration = await tenantSettingRepository.GetByTenantAndKey(tenantId, GovernanceSettingKeys.Groups.SelfRegistrationEnabled);
        var tenantHomePage = await tenantSettingRepository.GetByTenantAndKey(tenantId, GovernanceSettingKeys.Routing.DefaultPublicHomePage);
        var tenantSubdomain = await tenantSettingRepository.GetByTenantAndKey(tenantId, GovernanceSettingKeys.Domains.TenantSubdomain);
        var tenantCustomDomain = await tenantSettingRepository.GetByTenantAndKey(tenantId, GovernanceSettingKeys.Domains.TenantCustomDomain);
        var tenantBrandDisplayName = await tenantSettingRepository.GetByTenantAndKey(tenantId, GovernanceSettingKeys.Branding.DisplayName);
        var tenantBrandLogoUrl = await tenantSettingRepository.GetByTenantAndKey(tenantId, GovernanceSettingKeys.Branding.LogoUrl);
        var tenantBrandFaviconUrl = await tenantSettingRepository.GetByTenantAndKey(tenantId, GovernanceSettingKeys.Branding.FaviconUrl);
        var tenantBrandCustomCssUrl = await tenantSettingRepository.GetByTenantAndKey(tenantId, GovernanceSettingKeys.Branding.CustomCssUrl);

        var tenant = await tenantRepository.GetById(tenantId);
        var fallbackSubdomain = NormalizeSubdomain(tenant?.Slug) ?? "default";
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
            RequireOrganizationVerification = requireVerification,
            CanTenantOmitVerification = canOmitVerification,
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
                systemBrandLogoUrl?.IsLocked != true),
            BrandFaviconUrl = ResolveString(
                tenantBrandFaviconUrl?.Value,
                systemBrandFaviconUrl?.Value,
                string.Empty,
                systemBrandFaviconUrl?.IsLocked != true),
            BrandCustomCssUrl = ResolveString(
                tenantBrandCustomCssUrl?.Value,
                systemBrandCustomCssUrl?.Value,
                string.Empty,
                systemBrandCustomCssUrl?.IsLocked != true),
            CanOverrideHomePagePreference = canOverrideHomePage,
            CanOverrideSubdomain = canOverrideSubdomain,
            CanOverrideCustomDomain = canOverrideCustomDomain,
            CanOverrideBrandDisplayName = systemBrandDisplayName?.IsLocked != true,
            CanOverrideBrandLogoUrl = systemBrandLogoUrl?.IsLocked != true,
            CanOverrideBrandFaviconUrl = systemBrandFaviconUrl?.IsLocked != true,
            CanOverrideBrandCustomCssUrl = systemBrandCustomCssUrl?.IsLocked != true
        };
    }

    internal static async Task ApplyTenantSettingsAsync(
        ITenantSettingRepository tenantSettingRepository,
        ISystemSettingRepository systemSettingRepository,
        ITenantRepository tenantRepository,
        Guid tenantId,
        Guid? actorUserId,
        TenantPolicySettingsDto settings)
    {
        var userSubmissionSetting = await systemSettingRepository.GetByKey(GovernanceSettingKeys.Events.UserSubmissionEnabled);
        var orgSubmissionSetting = await systemSettingRepository.GetByKey(GovernanceSettingKeys.Events.OrganizationSubmissionEnabled);
        var groupSubmissionSetting = await systemSettingRepository.GetByKey(GovernanceSettingKeys.Events.GroupSubmissionEnabled);
        var requireApprovalSetting = await systemSettingRepository.GetByKey(GovernanceSettingKeys.Events.RequireApproval);
        var requireVerificationSetting = await systemSettingRepository.GetByKey(GovernanceSettingKeys.Organizations.VerificationRequired);
        var canOmitVerificationSetting = await systemSettingRepository.GetByKey(GovernanceSettingKeys.Organizations.TenantCanOmitVerification);
        var orgSelfRegSetting = await systemSettingRepository.GetByKey(GovernanceSettingKeys.Organizations.SelfRegistrationEnabled);
        var groupSelfRegSetting = await systemSettingRepository.GetByKey(GovernanceSettingKeys.Groups.SelfRegistrationEnabled);
        var homePageSetting = await systemSettingRepository.GetByKey(GovernanceSettingKeys.Routing.DefaultPublicHomePage);
        var allowCustomDomainSetting = await systemSettingRepository.GetByKey(GovernanceSettingKeys.Domains.AllowTenantCustomDomain);
        var subdomainSetting = await systemSettingRepository.GetByKey(GovernanceSettingKeys.Domains.TenantSubdomain);
        var customDomainSetting = await systemSettingRepository.GetByKey(GovernanceSettingKeys.Domains.TenantCustomDomain);
        var brandDisplayNameSetting = await systemSettingRepository.GetByKey(GovernanceSettingKeys.Branding.DisplayName);
        var brandLogoUrlSetting = await systemSettingRepository.GetByKey(GovernanceSettingKeys.Branding.LogoUrl);
        var brandFaviconUrlSetting = await systemSettingRepository.GetByKey(GovernanceSettingKeys.Branding.FaviconUrl);
        var brandCustomCssUrlSetting = await systemSettingRepository.GetByKey(GovernanceSettingKeys.Branding.CustomCssUrl);
        var tenant = await tenantRepository.GetById(tenantId);
        var fallbackSubdomain = NormalizeSubdomain(tenant?.Slug) ?? "default";

        await SetBooleanTenantOverrideAsync(
            tenantSettingRepository,
            tenantId,
            GovernanceSettingKeys.Events.UserSubmissionEnabled,
            settings.AllowUserSubmittedEvents,
            userSubmissionSetting?.IsLocked != true,
            actorUserId);

        await SetBooleanTenantOverrideAsync(
            tenantSettingRepository,
            tenantId,
            GovernanceSettingKeys.Events.OrganizationSubmissionEnabled,
            settings.AllowOrganizationSubmittedEvents,
            orgSubmissionSetting?.IsLocked != true,
            actorUserId);

        await SetBooleanTenantOverrideAsync(
            tenantSettingRepository,
            tenantId,
            GovernanceSettingKeys.Events.GroupSubmissionEnabled,
            settings.AllowGroupSubmittedEvents,
            groupSubmissionSetting?.IsLocked != true,
            actorUserId);

        await SetBooleanTenantOverrideAsync(
            tenantSettingRepository,
            tenantId,
            GovernanceSettingKeys.Organizations.SelfRegistrationEnabled,
            settings.AllowOrganizationSelfRegistration,
            orgSelfRegSetting?.IsLocked != true,
            actorUserId);

        await SetBooleanTenantOverrideAsync(
            tenantSettingRepository,
            tenantId,
            GovernanceSettingKeys.Groups.SelfRegistrationEnabled,
            settings.AllowGroupSelfRegistration,
            groupSelfRegSetting?.IsLocked != true,
            actorUserId);

        await SetBooleanTenantOverrideAsync(
            tenantSettingRepository,
            tenantId,
            GovernanceSettingKeys.Events.RequireApproval,
            settings.RequireEventApproval,
            requireApprovalSetting?.IsLocked != true,
            actorUserId);

        var canTenantOmitVerification = DeserializeBoolean(canOmitVerificationSetting?.Value, false)
            && requireVerificationSetting?.IsLocked != true;
        var effectiveRequireVerification = canTenantOmitVerification
            ? settings.RequireOrganizationVerification
            : DeserializeBoolean(requireVerificationSetting?.Value, true);

        await SetBooleanTenantOverrideAsync(
            tenantSettingRepository,
            tenantId,
            GovernanceSettingKeys.Organizations.VerificationRequired,
            effectiveRequireVerification,
            canTenantOmitVerification,
            actorUserId);

        await SetStringTenantOverrideAsync(
            tenantSettingRepository,
            tenantId,
            GovernanceSettingKeys.Routing.DefaultPublicHomePage,
            NormalizeHomePage(settings.PreferredHomePage),
            homePageSetting?.IsLocked != true,
            actorUserId);

        await SetStringTenantOverrideAsync(
            tenantSettingRepository,
            tenantId,
            GovernanceSettingKeys.Domains.TenantSubdomain,
            NormalizeSubdomain(settings.Subdomain) ?? fallbackSubdomain,
            subdomainSetting?.IsLocked != true,
            actorUserId);

        await SetStringTenantOverrideAsync(
            tenantSettingRepository,
            tenantId,
            GovernanceSettingKeys.Domains.TenantCustomDomain,
            NormalizeOptionalHost(settings.CustomDomain),
            customDomainSetting?.IsLocked != true && DeserializeBoolean(allowCustomDomainSetting?.Value, true),
            actorUserId);

        await SetStringTenantOverrideAsync(
            tenantSettingRepository,
            tenantId,
            GovernanceSettingKeys.Branding.DisplayName,
            settings.BrandDisplayName,
            brandDisplayNameSetting?.IsLocked != true,
            actorUserId);

        await SetStringTenantOverrideAsync(
            tenantSettingRepository,
            tenantId,
            GovernanceSettingKeys.Branding.LogoUrl,
            settings.BrandLogoUrl,
            brandLogoUrlSetting?.IsLocked != true,
            actorUserId);

        await SetStringTenantOverrideAsync(
            tenantSettingRepository,
            tenantId,
            GovernanceSettingKeys.Branding.FaviconUrl,
            settings.BrandFaviconUrl,
            brandFaviconUrlSetting?.IsLocked != true,
            actorUserId);

        await SetStringTenantOverrideAsync(
            tenantSettingRepository,
            tenantId,
            GovernanceSettingKeys.Branding.CustomCssUrl,
            settings.BrandCustomCssUrl,
            brandCustomCssUrlSetting?.IsLocked != true,
            actorUserId);
    }

    private static async Task SetBooleanTenantOverrideAsync(
        ITenantSettingRepository tenantSettingRepository,
        Guid tenantId,
        string settingKey,
        bool value,
        bool allowTenantOverride,
        Guid? actorUserId)
    {
        if (!allowTenantOverride)
        {
            await tenantSettingRepository.RemoveOverride(tenantId, settingKey);
            return;
        }

        await UpsertTenantOverrideAsync(
            tenantSettingRepository,
            tenantId,
            settingKey,
            JsonSerializer.Serialize(value),
            actorUserId);
    }

    private static async Task SetStringTenantOverrideAsync(
        ITenantSettingRepository tenantSettingRepository,
        Guid tenantId,
        string settingKey,
        string? value,
        bool allowTenantOverride,
        Guid? actorUserId)
    {
        if (!allowTenantOverride || string.IsNullOrWhiteSpace(value))
        {
            await tenantSettingRepository.RemoveOverride(tenantId, settingKey);
            return;
        }

        await UpsertTenantOverrideAsync(
            tenantSettingRepository,
            tenantId,
            settingKey,
            JsonSerializer.Serialize(value.Trim()),
            actorUserId);
    }

    private static async Task UpsertTenantOverrideAsync(
        ITenantSettingRepository tenantSettingRepository,
        Guid tenantId,
        string settingKey,
        string value,
        Guid? actorUserId)
    {
        var existing = await tenantSettingRepository.GetByTenantAndKey(tenantId, settingKey);
        if (existing == null)
        {
            await tenantSettingRepository.Create(new TenantSetting
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
        await tenantSettingRepository.Update(existing);
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
