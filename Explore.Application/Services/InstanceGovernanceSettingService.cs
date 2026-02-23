// ABOUTME: Service implementation for managing instance-level governance settings.
// ABOUTME: Handles deployment mode, module enablement, branding, and domain configuration.

using System.Text.Json;
using Explore.Application.Contracts.Persistence;
using Explore.Application.Contracts.Services;
using Explore.Application.DTOs.Onboarding;
using Explore.Domain;
using Explore.Domain.Constants;
using Explore.Domain.Enums;
using Explore.Domain.Modules;

namespace Explore.Application.Services;

public class InstanceGovernanceSettingService : IInstanceGovernanceSettingService
{
    private const string CoreModuleKey = "Mod_Core";
    private const string IslamicModuleKey = "Mod_Islamic";
    private const string TechModuleKey = "Mod_Tech";
    private const string DefaultBrandDisplayName = "ISLAMU Explore";
    private const string DefaultPublicHomePage = "EventList";
    private const int DefaultRenderPolicyVersion = 1;
    private const string DefaultRenderPolicyPreset = "SeoBalanced";
    private const string DefaultRenderMode = "InteractiveAuto";

    private readonly ISystemSettingRepository _systemSettingRepository;
    private readonly ITenantCapabilityRepository _tenantCapabilityRepository;
    private readonly IModuleDefinitionRepository _moduleDefinitionRepository;

    public InstanceGovernanceSettingService(
        ISystemSettingRepository systemSettingRepository,
        ITenantCapabilityRepository tenantCapabilityRepository,
        IModuleDefinitionRepository moduleDefinitionRepository)
    {
        _systemSettingRepository = systemSettingRepository;
        _tenantCapabilityRepository = tenantCapabilityRepository;
        _moduleDefinitionRepository = moduleDefinitionRepository;
    }

    public async Task<InstanceGovernanceSettingsDto> ReadSettingsAsync()
    {
        var deploymentMode = await _systemSettingRepository.GetByKey(GovernanceSettingKeys.DeploymentMode);
        var tenantSelfService = await _systemSettingRepository.GetByKey(GovernanceSettingKeys.TenantSelfServiceRegistration);
        var tenantWhiteLabeling = await _systemSettingRepository.GetByKey(GovernanceSettingKeys.TenantWhiteLabelingEnabled);
        var defaultHomePage = await _systemSettingRepository.GetByKey(GovernanceSettingKeys.RoutingDefaultPublicHomePage);
        var renderPolicyVersion = await _systemSettingRepository.GetByKey(GovernanceSettingKeys.RoutingRenderPolicyVersion);
        var renderPolicyPreset = await _systemSettingRepository.GetByKey(GovernanceSettingKeys.RoutingRenderPolicyPreset);
        var renderPolicyAdvancedEnabled = await _systemSettingRepository.GetByKey(GovernanceSettingKeys.RoutingRenderPolicyAdvancedEnabled);
        var globalRenderMode = await _systemSettingRepository.GetByKey(GovernanceSettingKeys.RoutingRenderPolicyGlobalRenderMode);
        var globalPrerenderEnabled = await _systemSettingRepository.GetByKey(GovernanceSettingKeys.RoutingRenderPolicyGlobalPrerenderEnabled);
        var publicSeoRenderMode = await _systemSettingRepository.GetByKey(GovernanceSettingKeys.RoutingRenderPolicyPublicSeoRenderMode);
        var publicSeoPrerenderEnabled = await _systemSettingRepository.GetByKey(GovernanceSettingKeys.RoutingRenderPolicyPublicSeoPrerenderEnabled);
        var operationalRenderMode = await _systemSettingRepository.GetByKey(GovernanceSettingKeys.RoutingRenderPolicyOperationalRenderMode);
        var operationalPrerenderEnabled = await _systemSettingRepository.GetByKey(GovernanceSettingKeys.RoutingRenderPolicyOperationalPrerenderEnabled);
        var adminRenderMode = await _systemSettingRepository.GetByKey(GovernanceSettingKeys.RoutingRenderPolicyAdminRenderMode);
        var adminPrerenderEnabled = await _systemSettingRepository.GetByKey(GovernanceSettingKeys.RoutingRenderPolicyAdminPrerenderEnabled);
        var onboardingRenderMode = await _systemSettingRepository.GetByKey(GovernanceSettingKeys.RoutingRenderPolicyOnboardingRenderMode);
        var onboardingPrerenderEnabled = await _systemSettingRepository.GetByKey(GovernanceSettingKeys.RoutingRenderPolicyOnboardingPrerenderEnabled);
        var disallowInteractiveServerOnOnboarding = await _systemSettingRepository.GetByKey(GovernanceSettingKeys.RoutingRenderPolicyDisallowInteractiveServerOnOnboarding);
        var islamicModule = await _systemSettingRepository.GetByKey(GovernanceSettingKeys.ModulesIslamicEnabled);
        var techModule = await _systemSettingRepository.GetByKey(GovernanceSettingKeys.ModulesTechEnabled);
        var userSubmittedEvents = await _systemSettingRepository.GetByKey(GovernanceSettingKeys.EventsUserSubmissionEnabled);
        var orgVerificationRequired = await _systemSettingRepository.GetByKey(GovernanceSettingKeys.OrganizationsVerificationRequired);
        var tenantCanOmitVerification = await _systemSettingRepository.GetByKey(GovernanceSettingKeys.OrganizationsTenantCanOmitVerification);
        var instanceBaseDomain = await _systemSettingRepository.GetByKey(GovernanceSettingKeys.DomainsInstanceBaseDomain);
        var allowTenantCustomDomains = await _systemSettingRepository.GetByKey(GovernanceSettingKeys.DomainsAllowTenantCustomDomain);
        var tenantSubdomain = await _systemSettingRepository.GetByKey(GovernanceSettingKeys.DomainsTenantSubdomain);
        var tenantCustomDomain = await _systemSettingRepository.GetByKey(GovernanceSettingKeys.DomainsTenantCustomDomain);
        var brandingDisplayName = await _systemSettingRepository.GetByKey(GovernanceSettingKeys.BrandingDisplayName);
        var brandingLogoUrl = await _systemSettingRepository.GetByKey(GovernanceSettingKeys.BrandingLogoUrl);
        var brandingFaviconUrl = await _systemSettingRepository.GetByKey(GovernanceSettingKeys.BrandingFaviconUrl);
        var brandingCustomCssUrl = await _systemSettingRepository.GetByKey(GovernanceSettingKeys.BrandingCustomCssUrl);
        var authorizationProvider = await _systemSettingRepository.GetByKey(GovernanceSettingKeys.AuthorizationProvider);
        var resolvedDeploymentMode = DeserializeString(deploymentMode?.Value, "SingleTenant");
        var isMultiTenant = resolvedDeploymentMode.Equals("MultiTenant", StringComparison.OrdinalIgnoreCase);

        var settings = new InstanceGovernanceSettingsDto
        {
            DeploymentMode = resolvedDeploymentMode,
            AllowTenantSelfServiceRegistration = isMultiTenant && DeserializeBoolean(tenantSelfService?.Value, false),
            AllowTenantWhiteLabeling = isMultiTenant && DeserializeBoolean(tenantWhiteLabeling?.Value, false),
            DefaultPublicHomePage = NormalizeHomePage(DeserializeString(defaultHomePage?.Value, DefaultPublicHomePage)),
            RenderPolicyVersion = Math.Max(DeserializeInt(renderPolicyVersion?.Value, DefaultRenderPolicyVersion), 1),
            RenderPolicyPreset = NormalizeRenderPolicyPreset(DeserializeString(renderPolicyPreset?.Value, DefaultRenderPolicyPreset)),
            EnableAdvancedRenderPolicyOverrides = DeserializeBoolean(renderPolicyAdvancedEnabled?.Value, false),
            GlobalRenderMode = NormalizeRenderMode(DeserializeString(globalRenderMode?.Value, DefaultRenderMode)),
            GlobalPrerenderEnabled = DeserializeBoolean(globalPrerenderEnabled?.Value, false),
            PublicSeoRenderMode = NormalizeRenderMode(DeserializeString(publicSeoRenderMode?.Value, string.Empty)),
            PublicSeoPrerenderEnabled = DeserializeBoolean(publicSeoPrerenderEnabled?.Value, false),
            OperationalRenderMode = NormalizeRenderMode(DeserializeString(operationalRenderMode?.Value, string.Empty)),
            OperationalPrerenderEnabled = DeserializeBoolean(operationalPrerenderEnabled?.Value, false),
            AdminRenderMode = NormalizeRenderMode(DeserializeString(adminRenderMode?.Value, string.Empty)),
            AdminPrerenderEnabled = DeserializeBoolean(adminPrerenderEnabled?.Value, false),
            OnboardingRenderMode = NormalizeRenderMode(DeserializeString(onboardingRenderMode?.Value, string.Empty)),
            OnboardingPrerenderEnabled = DeserializeBoolean(onboardingPrerenderEnabled?.Value, false),
            DisallowInteractiveServerOnOnboarding = DeserializeBoolean(disallowInteractiveServerOnOnboarding?.Value, true),
            EnableIslamicModule = DeserializeBoolean(islamicModule?.Value, true),
            EnableTechModule = DeserializeBoolean(techModule?.Value, true),
            AllowUserSubmittedEvents = DeserializeBoolean(userSubmittedEvents?.Value, true),
            RequireOrganizationVerification = DeserializeBoolean(orgVerificationRequired?.Value, true),
            AllowTenantToOmitVerification = DeserializeBoolean(tenantCanOmitVerification?.Value, false),
            InstanceBaseDomain = DeserializeString(instanceBaseDomain?.Value, string.Empty),
            AllowTenantCustomDomains = DeserializeBoolean(allowTenantCustomDomains?.Value, true),
            DefaultBrandDisplayName = DeserializeString(brandingDisplayName?.Value, DefaultBrandDisplayName),
            DefaultBrandLogoUrl = DeserializeString(brandingLogoUrl?.Value, string.Empty),
            DefaultBrandFaviconUrl = DeserializeString(brandingFaviconUrl?.Value, string.Empty),
            DefaultBrandCustomCssUrl = DeserializeString(brandingCustomCssUrl?.Value, string.Empty),
            LockTenantHomePagePreference = defaultHomePage?.IsLocked == true,
            LockTenantSubdomain = tenantSubdomain?.IsLocked == true,
            LockTenantCustomDomain = tenantCustomDomain?.IsLocked == true,
            LockTenantBrandDisplayName = brandingDisplayName?.IsLocked == true,
            LockTenantBrandLogoUrl = brandingLogoUrl?.IsLocked == true,
            LockTenantBrandFaviconUrl = brandingFaviconUrl?.IsLocked == true,
            LockTenantBrandCustomCssUrl = brandingCustomCssUrl?.IsLocked == true,
            AuthorizationProvider = NormalizeAuthorizationProvider(DeserializeString(authorizationProvider?.Value, "local"))
        };

        NormalizeRenderPolicySettings(settings);
        return settings;
    }

    public async Task ApplySettingsAsync(Guid defaultTenantId, InstanceGovernanceSettingsDto settings, Guid? actorUserId)
    {
        settings.DefaultPublicHomePage = NormalizeHomePage(settings.DefaultPublicHomePage);
        settings.InstanceBaseDomain = NormalizeOptionalHost(settings.InstanceBaseDomain);
        settings.DefaultBrandDisplayName = NormalizeRequiredDisplayName(settings.DefaultBrandDisplayName);
        settings.DefaultBrandLogoUrl = NormalizeOptionalUrl(settings.DefaultBrandLogoUrl);
        settings.DefaultBrandFaviconUrl = NormalizeOptionalUrl(settings.DefaultBrandFaviconUrl);
        settings.DefaultBrandCustomCssUrl = NormalizeOptionalUrl(settings.DefaultBrandCustomCssUrl);
        NormalizeRenderPolicySettings(settings);
        var isMultiTenant = settings.DeploymentMode.Equals("MultiTenant", StringComparison.OrdinalIgnoreCase);

        await UpsertSystemSettingAsync(
            GovernanceSettingKeys.DeploymentMode,
            JsonSerializer.Serialize(settings.DeploymentMode),
            SettingValueType.String,
            true,
            "System",
            1,
            "Deployment mode of the application",
            "[\"SingleTenant\", \"MultiTenant\"]");

        await UpsertSystemSettingAsync(
            GovernanceSettingKeys.TenantSelfServiceRegistration,
            JsonSerializer.Serialize(isMultiTenant && settings.AllowTenantSelfServiceRegistration),
            SettingValueType.Boolean,
            false,
            "Tenant",
            1,
            "Whether tenants can self-register without manual instance admin invitation");

        await UpsertSystemSettingAsync(
            GovernanceSettingKeys.TenantWhiteLabelingEnabled,
            JsonSerializer.Serialize(isMultiTenant && settings.AllowTenantWhiteLabeling),
            SettingValueType.Boolean,
            false,
            "Tenant",
            2,
            "Whether tenant-level white-label branding overrides are enabled in multi-tenant mode");

        await UpsertSystemSettingAsync(
            GovernanceSettingKeys.RoutingDefaultPublicHomePage,
            JsonSerializer.Serialize(settings.DefaultPublicHomePage),
            SettingValueType.String,
            settings.LockTenantHomePagePreference,
            "Routing",
            1,
            "Default public landing experience for tenant domains",
            "[\"EventList\", \"LandingPage\"]");

        await UpsertSystemSettingAsync(
            GovernanceSettingKeys.RoutingRenderPolicyVersion,
            JsonSerializer.Serialize(Math.Max(settings.RenderPolicyVersion, 1)),
            SettingValueType.Integer,
            true,
            "Routing",
            2,
            "Version number for runtime render-policy schema.");

        await UpsertSystemSettingAsync(
            GovernanceSettingKeys.RoutingRenderPolicyPreset,
            JsonSerializer.Serialize(settings.RenderPolicyPreset),
            SettingValueType.String,
            false,
            "Routing",
            3,
            "Render-policy preset selected by instance administrator.",
            "[\"SeoBalanced\", \"AllPrerendered\", \"AllInteractiveAutoNoPrerender\", \"CustomAdvanced\"]");

        await UpsertSystemSettingAsync(
            GovernanceSettingKeys.RoutingRenderPolicyAdvancedEnabled,
            JsonSerializer.Serialize(settings.EnableAdvancedRenderPolicyOverrides),
            SettingValueType.Boolean,
            false,
            "Routing",
            4,
            "Whether advanced render-policy overrides are enabled.");

        await UpsertSystemSettingAsync(
            GovernanceSettingKeys.RoutingRenderPolicyGlobalRenderMode,
            JsonSerializer.Serialize(settings.GlobalRenderMode),
            SettingValueType.String,
            false,
            "Routing",
            5,
            "Global fallback render mode used when route-group overrides are disabled or unavailable.",
            "[\"InteractiveAuto\", \"InteractiveWebAssembly\", \"InteractiveServer\"]");

        await UpsertSystemSettingAsync(
            GovernanceSettingKeys.RoutingRenderPolicyGlobalPrerenderEnabled,
            JsonSerializer.Serialize(settings.GlobalPrerenderEnabled),
            SettingValueType.Boolean,
            false,
            "Routing",
            6,
            "Global fallback prerender flag used when route-group overrides are disabled or unavailable.");

        await UpsertSystemSettingAsync(
            GovernanceSettingKeys.RoutingRenderPolicyPublicSeoRenderMode,
            JsonSerializer.Serialize(settings.PublicSeoRenderMode),
            SettingValueType.String,
            false,
            "Routing",
            7,
            "Render mode applied to SEO-focused public routes.",
            "[\"InteractiveAuto\", \"InteractiveWebAssembly\", \"InteractiveServer\"]");

        await UpsertSystemSettingAsync(
            GovernanceSettingKeys.RoutingRenderPolicyPublicSeoPrerenderEnabled,
            JsonSerializer.Serialize(settings.PublicSeoPrerenderEnabled),
            SettingValueType.Boolean,
            false,
            "Routing",
            8,
            "Whether SEO-focused public routes are prerendered.");

        await UpsertSystemSettingAsync(
            GovernanceSettingKeys.RoutingRenderPolicyOperationalRenderMode,
            JsonSerializer.Serialize(settings.OperationalRenderMode),
            SettingValueType.String,
            false,
            "Routing",
            9,
            "Render mode applied to operational routes.",
            "[\"InteractiveAuto\", \"InteractiveWebAssembly\", \"InteractiveServer\"]");

        await UpsertSystemSettingAsync(
            GovernanceSettingKeys.RoutingRenderPolicyOperationalPrerenderEnabled,
            JsonSerializer.Serialize(settings.OperationalPrerenderEnabled),
            SettingValueType.Boolean,
            false,
            "Routing",
            10,
            "Whether operational routes are prerendered.");

        await UpsertSystemSettingAsync(
            GovernanceSettingKeys.RoutingRenderPolicyAdminRenderMode,
            JsonSerializer.Serialize(settings.AdminRenderMode),
            SettingValueType.String,
            false,
            "Routing",
            11,
            "Render mode applied to administrative routes.",
            "[\"InteractiveAuto\", \"InteractiveWebAssembly\", \"InteractiveServer\"]");

        await UpsertSystemSettingAsync(
            GovernanceSettingKeys.RoutingRenderPolicyAdminPrerenderEnabled,
            JsonSerializer.Serialize(settings.AdminPrerenderEnabled),
            SettingValueType.Boolean,
            false,
            "Routing",
            12,
            "Whether administrative routes are prerendered.");

        await UpsertSystemSettingAsync(
            GovernanceSettingKeys.RoutingRenderPolicyOnboardingRenderMode,
            JsonSerializer.Serialize(settings.OnboardingRenderMode),
            SettingValueType.String,
            true,
            "Routing",
            13,
            "Render mode applied to onboarding routes.",
            "[\"InteractiveAuto\", \"InteractiveWebAssembly\"]");

        await UpsertSystemSettingAsync(
            GovernanceSettingKeys.RoutingRenderPolicyOnboardingPrerenderEnabled,
            JsonSerializer.Serialize(settings.OnboardingPrerenderEnabled),
            SettingValueType.Boolean,
            false,
            "Routing",
            14,
            "Whether onboarding routes are prerendered.");

        await UpsertSystemSettingAsync(
            GovernanceSettingKeys.RoutingRenderPolicyDisallowInteractiveServerOnOnboarding,
            JsonSerializer.Serialize(settings.DisallowInteractiveServerOnOnboarding),
            SettingValueType.Boolean,
            true,
            "Routing",
            15,
            "Guardrail that disallows InteractiveServer on onboarding routes.");

        await UpsertSystemSettingAsync(
            GovernanceSettingKeys.ModulesIslamicEnabled,
            JsonSerializer.Serialize(settings.EnableIslamicModule),
            SettingValueType.Boolean,
            false,
            "Modules",
            1,
            "Enable Islamic event module");

        await UpsertSystemSettingAsync(
            GovernanceSettingKeys.ModulesTechEnabled,
            JsonSerializer.Serialize(settings.EnableTechModule),
            SettingValueType.Boolean,
            false,
            "Modules",
            2,
            "Enable Tech event module");

        await UpsertSystemSettingAsync(
            GovernanceSettingKeys.EventsUserSubmissionEnabled,
            JsonSerializer.Serialize(settings.AllowUserSubmittedEvents),
            SettingValueType.Boolean,
            false,
            "Events",
            3,
            "Whether tenant users are allowed to submit events");

        await UpsertSystemSettingAsync(
            GovernanceSettingKeys.OrganizationsVerificationRequired,
            JsonSerializer.Serialize(settings.RequireOrganizationVerification),
            SettingValueType.Boolean,
            false,
            "Organizations",
            1,
            "Whether organization verification is required before organizations can operate");

        await UpsertSystemSettingAsync(
            GovernanceSettingKeys.OrganizationsTenantCanOmitVerification,
            JsonSerializer.Serialize(settings.AllowTenantToOmitVerification),
            SettingValueType.Boolean,
            false,
            "Organizations",
            2,
            "Whether tenant administrators may omit organization verification requirements");

        await UpsertSystemSettingAsync(
            GovernanceSettingKeys.DomainsInstanceBaseDomain,
            JsonSerializer.Serialize(settings.InstanceBaseDomain),
            SettingValueType.String,
            false,
            "Domains",
            1,
            "Base domain used for tenant subdomain generation");

        await UpsertSystemSettingAsync(
            GovernanceSettingKeys.DomainsAllowTenantCustomDomain,
            JsonSerializer.Serialize(settings.AllowTenantCustomDomains),
            SettingValueType.Boolean,
            false,
            "Domains",
            2,
            "Whether tenant administrators may configure a custom domain");

        await UpsertSystemSettingAsync(
            GovernanceSettingKeys.DomainsTenantSubdomain,
            JsonSerializer.Serialize(string.Empty),
            SettingValueType.String,
            settings.LockTenantSubdomain,
            "Domains",
            3,
            "Tenant-level default subdomain preference placeholder");

        await UpsertSystemSettingAsync(
            GovernanceSettingKeys.DomainsTenantCustomDomain,
            JsonSerializer.Serialize(string.Empty),
            SettingValueType.String,
            settings.LockTenantCustomDomain,
            "Domains",
            4,
            "Tenant-level custom domain preference placeholder");

        await UpsertSystemSettingAsync(
            GovernanceSettingKeys.BrandingDisplayName,
            JsonSerializer.Serialize(settings.DefaultBrandDisplayName),
            SettingValueType.String,
            settings.LockTenantBrandDisplayName,
            "Branding",
            1,
            "Default brand display name shown when tenants do not override branding");

        await UpsertSystemSettingAsync(
            GovernanceSettingKeys.BrandingLogoUrl,
            JsonSerializer.Serialize(settings.DefaultBrandLogoUrl),
            SettingValueType.String,
            settings.LockTenantBrandLogoUrl,
            "Branding",
            2,
            "Default logo URL shown when tenants do not override branding");

        await UpsertSystemSettingAsync(
            GovernanceSettingKeys.BrandingFaviconUrl,
            JsonSerializer.Serialize(settings.DefaultBrandFaviconUrl),
            SettingValueType.String,
            settings.LockTenantBrandFaviconUrl,
            "Branding",
            3,
            "Default favicon URL shown when tenants do not override branding");

        await UpsertSystemSettingAsync(
            GovernanceSettingKeys.BrandingCustomCssUrl,
            JsonSerializer.Serialize(settings.DefaultBrandCustomCssUrl),
            SettingValueType.String,
            settings.LockTenantBrandCustomCssUrl,
            "Branding",
            4,
            "Default custom stylesheet URL applied when tenants do not override branding");

        await UpsertSystemSettingAsync(
            GovernanceSettingKeys.AuthorizationProvider,
            JsonSerializer.Serialize(NormalizeAuthorizationProvider(settings.AuthorizationProvider)),
            SettingValueType.String,
            true,
            "Security",
            1,
            "Authorization provider: 'local' for database-only RBAC, 'cerbos' for full PDP",
            "[\"local\", \"cerbos\"]");

        await UpsertTenantCapabilityAsync(
            defaultTenantId,
            CoreModuleKey,
            true,
            actorUserId);

        await UpsertTenantCapabilityAsync(
            defaultTenantId,
            IslamicModuleKey,
            settings.EnableIslamicModule,
            actorUserId);

        await UpsertTenantCapabilityAsync(
            defaultTenantId,
            TechModuleKey,
            settings.EnableTechModule,
            actorUserId);
    }

    private static int DeserializeInt(string? rawValue, int defaultValue)
    {
        if (string.IsNullOrWhiteSpace(rawValue))
        {
            return defaultValue;
        }

        try
        {
            return JsonSerializer.Deserialize<int>(rawValue);
        }
        catch
        {
            return int.TryParse(rawValue, out var parsed) ? parsed : defaultValue;
        }
    }

    private static bool DeserializeBoolean(string? rawValue, bool defaultValue)
    {
        if (string.IsNullOrWhiteSpace(rawValue))
        {
            return defaultValue;
        }

        try
        {
            return JsonSerializer.Deserialize<bool>(rawValue);
        }
        catch
        {
            return bool.TryParse(rawValue, out var parsed) ? parsed : defaultValue;
        }
    }

    private static string NormalizeRequiredDisplayName(string? value)
    {
        return string.IsNullOrWhiteSpace(value) ? DefaultBrandDisplayName : value.Trim();
    }

    private static string NormalizeOptionalUrl(string? value)
    {
        return string.IsNullOrWhiteSpace(value) ? string.Empty : value.Trim();
    }

    private static string NormalizeOptionalHost(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return string.Empty;
        }

        var sanitized = value.Trim().ToLowerInvariant();
        sanitized = sanitized.Replace("https://", string.Empty, StringComparison.OrdinalIgnoreCase);
        sanitized = sanitized.Replace("http://", string.Empty, StringComparison.OrdinalIgnoreCase);
        sanitized = sanitized.Trim().Trim('/');
        return sanitized;
    }

    private static string NormalizeHomePage(string? raw)
    {
        if (string.IsNullOrWhiteSpace(raw))
        {
            return DefaultPublicHomePage;
        }

        if (raw.Equals("LandingPage", StringComparison.OrdinalIgnoreCase))
        {
            return "LandingPage";
        }

        return "EventList";
    }

    private static void NormalizeRenderPolicySettings(InstanceGovernanceSettingsDto settings)
    {
        settings.RenderPolicyVersion = Math.Max(settings.RenderPolicyVersion, 1);
        settings.RenderPolicyPreset = NormalizeRenderPolicyPreset(settings.RenderPolicyPreset);
        settings.GlobalRenderMode = NormalizeRenderMode(settings.GlobalRenderMode);

        ApplyPresetDefaults(settings);

        settings.PublicSeoRenderMode = NormalizeRenderMode(settings.PublicSeoRenderMode);
        settings.OperationalRenderMode = NormalizeRenderMode(settings.OperationalRenderMode);
        settings.AdminRenderMode = NormalizeRenderMode(settings.AdminRenderMode);
        settings.OnboardingRenderMode = NormalizeRenderMode(settings.OnboardingRenderMode);

        if (!settings.EnableAdvancedRenderPolicyOverrides)
        {
            settings.PublicSeoRenderMode = settings.GlobalRenderMode;
            settings.PublicSeoPrerenderEnabled = settings.GlobalPrerenderEnabled;
            settings.OperationalRenderMode = settings.GlobalRenderMode;
            settings.OperationalPrerenderEnabled = settings.GlobalPrerenderEnabled;
            settings.AdminRenderMode = settings.GlobalRenderMode;
            settings.AdminPrerenderEnabled = settings.GlobalPrerenderEnabled;
            settings.OnboardingRenderMode = settings.GlobalRenderMode;
            settings.OnboardingPrerenderEnabled = settings.GlobalPrerenderEnabled;

            if (settings.RenderPolicyPreset.Equals(RenderPolicyPresetEnum.SeoBalanced.ToString(), StringComparison.OrdinalIgnoreCase))
            {
                settings.PublicSeoPrerenderEnabled = true;
            }
        }

        if (IsInteractiveServerRenderMode(settings.OnboardingRenderMode))
        {
            settings.OnboardingRenderMode = DefaultRenderMode;
        }

        settings.DisallowInteractiveServerOnOnboarding = true;
    }

    private static string NormalizeRenderPolicyPreset(string? raw)
    {
        if (Enum.TryParse(raw, ignoreCase: true, out RenderPolicyPresetEnum preset))
        {
            return preset.ToString();
        }

        return DefaultRenderPolicyPreset;
    }

    private static string NormalizeRenderMode(string? raw)
    {
        if (Enum.TryParse(raw, ignoreCase: true, out RenderModeOptionEnum mode))
        {
            return mode.ToString();
        }

        return DefaultRenderMode;
    }

    private static bool IsInteractiveServerRenderMode(string? renderMode)
    {
        return Enum.TryParse(renderMode, ignoreCase: true, out RenderModeOptionEnum mode)
            && mode == RenderModeOptionEnum.InteractiveServer;
    }

    private static void ApplyPresetDefaults(InstanceGovernanceSettingsDto settings)
    {
        if (!Enum.TryParse(settings.RenderPolicyPreset, ignoreCase: true, out RenderPolicyPresetEnum preset))
        {
            preset = RenderPolicyPresetEnum.SeoBalanced;
        }

        switch (preset)
        {
            case RenderPolicyPresetEnum.AllPrerendered:
                settings.EnableAdvancedRenderPolicyOverrides = false;
                settings.GlobalPrerenderEnabled = true;
                break;

            case RenderPolicyPresetEnum.AllInteractiveAutoNoPrerender:
                settings.EnableAdvancedRenderPolicyOverrides = false;
                settings.GlobalRenderMode = RenderModeOptionEnum.InteractiveAuto.ToString();
                settings.GlobalPrerenderEnabled = false;
                break;

            case RenderPolicyPresetEnum.SeoBalanced:
                settings.EnableAdvancedRenderPolicyOverrides = false;
                settings.GlobalRenderMode = RenderModeOptionEnum.InteractiveAuto.ToString();
                settings.GlobalPrerenderEnabled = false;
                settings.PublicSeoPrerenderEnabled = true;
                break;

            case RenderPolicyPresetEnum.CustomAdvanced:
                settings.EnableAdvancedRenderPolicyOverrides = true;
                break;
        }
    }

    private static string NormalizeAuthorizationProvider(string? raw)
    {
        if (string.IsNullOrWhiteSpace(raw))
            return "local";

        return raw.Trim().ToLowerInvariant() switch
        {
            "cerbos" => "cerbos",
            _ => "local"
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

    private async Task UpsertSystemSettingAsync(
        string settingKey,
        string value,
        SettingValueType valueType,
        bool isLocked,
        string category,
        int displayOrder,
        string description,
        string? allowedValues = null)
    {
        var existing = await _systemSettingRepository.GetByKey(settingKey);

        if (existing == null)
        {
            await _systemSettingRepository.Create(new SystemSetting
            {
                SettingKey = settingKey,
                Value = value,
                ValueType = valueType,
                IsLocked = isLocked,
                AllowedValues = allowedValues,
                Description = description,
                Category = category,
                DisplayOrder = displayOrder,
                CreatedAt = DateTime.UtcNow
            });

            return;
        }

        existing.Value = value;
        existing.ValueType = valueType;
        existing.IsLocked = isLocked;
        existing.AllowedValues = allowedValues;
        existing.Description = description;
        existing.Category = category;
        existing.DisplayOrder = displayOrder;
        existing.UpdatedAt = DateTime.UtcNow;

        await _systemSettingRepository.Update(existing);
    }

    private async Task UpsertTenantCapabilityAsync(
        Guid tenantId,
        string moduleKey,
        bool isEnabled,
        Guid? actorUserId)
    {
        var module = await _moduleDefinitionRepository.GetByKey(moduleKey);
        if (module == null)
        {
            return;
        }

        var existing = await _tenantCapabilityRepository.GetByTenantAndModuleKey(tenantId, moduleKey);
        if (existing == null)
        {
            await _tenantCapabilityRepository.Create(new TenantCapability
            {
                TenantId = tenantId,
                Tenant = null!,
                ModuleId = module.Id,
                Module = null!,
                IsEnabled = isEnabled,
                EnabledAt = DateTime.UtcNow,
                EnabledBy = actorUserId
            });

            return;
        }

        existing.IsEnabled = isEnabled;
        if (isEnabled && existing.EnabledAt == default)
        {
            existing.EnabledAt = DateTime.UtcNow;
            existing.EnabledBy = actorUserId;
        }

        await _tenantCapabilityRepository.Update(existing);
    }
}
