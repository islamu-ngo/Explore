using System;

namespace Explore.Persistence.Seed;

/// <summary>
/// Static deterministic GUIDs for seed data.
/// These should NEVER be changed once used in production to avoid migration issues.
/// Use UUIDv7 format for time-ordered IDs: 018e4e5c-xxxx-7xxx-8xxx-xxxxxxxxxxxx
/// </summary>
public static class SeedIds
{
    // ===== Tenants =====
    public static readonly Guid DefaultTenantId = Guid.Parse("018e4e5c-7f00-7000-8000-000000000001");

    // ===== Actors =====
    /// <summary>Actor for ISLAMU organization</summary>
    public static readonly Guid IslamuOrganizationActorId = Guid.Parse("018e4e5c-7f00-7000-8000-000000000021");
    /// <summary>Personal actor for System User</summary>
    public static readonly Guid SystemUserActorId = Guid.Parse("018e4e5c-7f00-7000-8000-000000000022");

    // ===== Users =====
    public static readonly Guid SystemUserId = Guid.Parse("018e4e5c-7f00-7000-8000-000000000030");

    // ===== Organizations =====
    public static readonly Guid IslamuOrganizationId = Guid.Parse("018e4e5c-7f00-7000-8000-000000000040");

    // ===== Organization Members =====
    public static readonly Guid SystemUserIslamuMemberId = Guid.Parse("018e4e5c-7f00-7000-8000-000000000041");

    // ===== Storage Objects =====
    public static readonly Guid DefaultEventImageId = Guid.Parse("018e4e5c-7f00-7000-8000-000000000050");
    public static readonly Guid DefaultProfileImageId = Guid.Parse("018e4e5c-7f00-7000-8000-000000000051");
    public static readonly Guid DefaultOrganizationLogoId = Guid.Parse("018e4e5c-7f00-7000-8000-000000000052");

    // ===== Events =====
    public static readonly Guid SampleEventId = Guid.Parse("018e4e5c-7f00-7000-8000-000000000060");

    // ===== Categories (Guid-based) =====
    public static readonly Guid IslamicStudiesCategoryId = Guid.Parse("018e4e5c-7f00-7000-8000-000000000100");
    public static readonly Guid QuranCategoryId = Guid.Parse("018e4e5c-7f00-7000-8000-000000000101");
    public static readonly Guid HadithCategoryId = Guid.Parse("018e4e5c-7f00-7000-8000-000000000102");
    public static readonly Guid FiqhCategoryId = Guid.Parse("018e4e5c-7f00-7000-8000-000000000103");
    public static readonly Guid AqeedahCategoryId = Guid.Parse("018e4e5c-7f00-7000-8000-000000000104");
    public static readonly Guid SeerahCategoryId = Guid.Parse("018e4e5c-7f00-7000-8000-000000000105");
    public static readonly Guid ArabicLanguageCategoryId = Guid.Parse("018e4e5c-7f00-7000-8000-000000000106");
    public static readonly Guid CommunityEventsCategoryId = Guid.Parse("018e4e5c-7f00-7000-8000-000000000107");

    // ===== Tags (Guid-based) =====
    public static readonly Guid BeginnerTagId = Guid.Parse("018e4e5c-7f00-7000-8000-000000000200");
    public static readonly Guid IntermediateTagId = Guid.Parse("018e4e5c-7f00-7000-8000-000000000201");
    public static readonly Guid AdvancedTagId = Guid.Parse("018e4e5c-7f00-7000-8000-000000000202");
    public static readonly Guid FreeTagId = Guid.Parse("018e4e5c-7f00-7000-8000-000000000203");
    public static readonly Guid PaidTagId = Guid.Parse("018e4e5c-7f00-7000-8000-000000000204");
    public static readonly Guid OnlineTagId = Guid.Parse("018e4e5c-7f00-7000-8000-000000000205");
    public static readonly Guid InPersonTagId = Guid.Parse("018e4e5c-7f00-7000-8000-000000000206");

    // ===== Locations (Guid-based) =====
    public static readonly Guid OnlineLocationId = Guid.Parse("018e4e5c-7f00-7000-8000-000000000300");

    // ===== Tenant Settings =====
    public static readonly Guid DefaultTenantSettingsId = Guid.Parse("018e4e5c-7f00-7000-8000-000000000400");

    // ===== System Settings =====
    /// <summary>Deployment mode setting (SingleTenant/MultiTenant)</summary>
    public static readonly Guid SystemSettingDeploymentModeId = Guid.Parse("018e4e5c-7f00-7000-8000-000000000500");
    /// <summary>Max sessions per event setting</summary>
    public static readonly Guid SystemSettingMaxSessionsPerEventId = Guid.Parse("018e4e5c-7f00-7000-8000-000000000501");
    /// <summary>Event approval required setting</summary>
    public static readonly Guid SystemSettingRequireApprovalId = Guid.Parse("018e4e5c-7f00-7000-8000-000000000502");
    /// <summary>Islamic module enabled setting</summary>
    public static readonly Guid SystemSettingIslamicModuleId = Guid.Parse("018e4e5c-7f00-7000-8000-000000000503");
    /// <summary>Tech module enabled setting</summary>
    public static readonly Guid SystemSettingTechModuleId = Guid.Parse("018e4e5c-7f00-7000-8000-000000000504");
    /// <summary>Tenant self-service registration setting</summary>
    public static readonly Guid SystemSettingTenantSelfServiceRegistrationId = Guid.Parse("018e4e5c-7f00-7000-8000-000000000505");
    /// <summary>User-submitted event enablement setting</summary>
    public static readonly Guid SystemSettingUserSubmissionEnabledId = Guid.Parse("018e4e5c-7f00-7000-8000-000000000506");
    /// <summary>Organization verification required setting</summary>
    public static readonly Guid SystemSettingOrganizationVerificationRequiredId = Guid.Parse("018e4e5c-7f00-7000-8000-000000000507");
    /// <summary>Tenant can omit organization verification setting</summary>
    public static readonly Guid SystemSettingOrganizationTenantCanOmitVerificationId = Guid.Parse("018e4e5c-7f00-7000-8000-000000000508");
    /// <summary>Default platform/tenant brand display name setting</summary>
    public static readonly Guid SystemSettingBrandingDisplayNameId = Guid.Parse("018e4e5c-7f00-7000-8000-000000000509");
    /// <summary>Default platform/tenant logo URL setting</summary>
    public static readonly Guid SystemSettingBrandingLogoUrlId = Guid.Parse("018e4e5c-7f00-7000-8000-000000000510");
    /// <summary>Default platform/tenant favicon URL setting</summary>
    public static readonly Guid SystemSettingBrandingFaviconUrlId = Guid.Parse("018e4e5c-7f00-7000-8000-000000000511");
    /// <summary>Default platform/tenant custom CSS URL setting</summary>
    public static readonly Guid SystemSettingBrandingCustomCssUrlId = Guid.Parse("018e4e5c-7f00-7000-8000-000000000512");
    /// <summary>Default tenant public home page setting</summary>
    public static readonly Guid SystemSettingRoutingDefaultPublicHomePageId = Guid.Parse("018e4e5c-7f00-7000-8000-000000000513");
    /// <summary>Instance base domain for tenant subdomain generation</summary>
    public static readonly Guid SystemSettingDomainsInstanceBaseDomainId = Guid.Parse("018e4e5c-7f00-7000-8000-000000000514");
    /// <summary>Allow tenant custom domain setting</summary>
    public static readonly Guid SystemSettingDomainsAllowTenantCustomDomainId = Guid.Parse("018e4e5c-7f00-7000-8000-000000000515");
    /// <summary>Tenant subdomain override placeholder setting</summary>
    public static readonly Guid SystemSettingDomainsTenantSubdomainId = Guid.Parse("018e4e5c-7f00-7000-8000-000000000516");
    /// <summary>Tenant custom domain override placeholder setting</summary>
    public static readonly Guid SystemSettingDomainsTenantCustomDomainId = Guid.Parse("018e4e5c-7f00-7000-8000-000000000517");

    // ===== Module Definitions =====
    /// <summary>Core module - basic event functionality (always enabled)</summary>
    public static readonly Guid ModuleCoreId = Guid.Parse("018e4e5c-7f00-7000-8000-000000000600");
    /// <summary>Islamic module - Madhab, prayer times, gender segregation</summary>
    public static readonly Guid ModuleIslamicId = Guid.Parse("018e4e5c-7f00-7000-8000-000000000601");
    /// <summary>Tech module - GitHub repos, skill levels, live coding</summary>
    public static readonly Guid ModuleTechId = Guid.Parse("018e4e5c-7f00-7000-8000-000000000602");

    // ===== Tenant Capabilities (Default Tenant) =====
    /// <summary>Default tenant Core module capability</summary>
    public static readonly Guid DefaultTenantCoreCapabilityId = Guid.Parse("018e4e5c-7f00-7000-8000-000000000610");
    /// <summary>Default tenant Islamic module capability</summary>
    public static readonly Guid DefaultTenantIslamicCapabilityId = Guid.Parse("018e4e5c-7f00-7000-8000-000000000611");

    // Note: Most lookup tables use int IDs with enums.
    // Only entities with Guid primary keys need entries here.
    // Int-based IDs are defined via enums in Domain/Enums folder.
}
