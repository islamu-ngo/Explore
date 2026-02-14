// ABOUTME: Service implementation for managing instance-level governance settings.
// ABOUTME: Handles deployment mode, module enablement, branding, and domain configuration.

using System.Text.Json;
using Explore.Application.Contracts.Persistence;
using Explore.Application.Contracts.Services;
using Explore.Application.DTOs.Onboarding;
using Explore.Domain;
using Explore.Domain.Constants;
using Explore.Domain.Modules;

namespace Explore.Application.Services;

public class InstanceGovernanceSettingService : IInstanceGovernanceSettingService
{
    private const string CoreModuleKey = "Mod_Core";
    private const string IslamicModuleKey = "Mod_Islamic";
    private const string TechModuleKey = "Mod_Tech";
    private const string DefaultBrandDisplayName = "ISLAMU Explore";
    private const string DefaultPublicHomePage = "EventList";

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
        var defaultHomePage = await _systemSettingRepository.GetByKey(GovernanceSettingKeys.RoutingDefaultPublicHomePage);
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

        return new InstanceGovernanceSettingsDto
        {
            DeploymentMode = DeserializeString(deploymentMode?.Value, "SingleTenant"),
            AllowTenantSelfServiceRegistration = DeserializeBoolean(tenantSelfService?.Value, false),
            DefaultPublicHomePage = NormalizeHomePage(DeserializeString(defaultHomePage?.Value, DefaultPublicHomePage)),
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
    }

    public async Task ApplySettingsAsync(Guid defaultTenantId, InstanceGovernanceSettingsDto settings, Guid? actorUserId)
    {
        settings.DefaultPublicHomePage = NormalizeHomePage(settings.DefaultPublicHomePage);
        settings.InstanceBaseDomain = NormalizeOptionalHost(settings.InstanceBaseDomain);
        settings.DefaultBrandDisplayName = NormalizeRequiredDisplayName(settings.DefaultBrandDisplayName);
        settings.DefaultBrandLogoUrl = NormalizeOptionalUrl(settings.DefaultBrandLogoUrl);
        settings.DefaultBrandFaviconUrl = NormalizeOptionalUrl(settings.DefaultBrandFaviconUrl);
        settings.DefaultBrandCustomCssUrl = NormalizeOptionalUrl(settings.DefaultBrandCustomCssUrl);

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
            JsonSerializer.Serialize(settings.AllowTenantSelfServiceRegistration),
            SettingValueType.Boolean,
            false,
            "Tenant",
            1,
            "Whether tenants can self-register without manual instance admin invitation");

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
