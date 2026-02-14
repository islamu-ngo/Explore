// ABOUTME: Static deterministic GUIDs for seed data (lookup tables + dev business entities).
// ABOUTME: Used by LookupTableSeeder (all environments) and DatabaseSeeder (Development only).

namespace Explore.Persistence.Seed;

/// <summary>
/// Static deterministic GUIDs for seed data.
/// These should NEVER be changed once used in production to avoid migration issues.
/// Use UUIDv7 format for time-ordered IDs: 018e4e5c-xxxx-7xxx-8xxx-xxxxxxxxxxxx
/// </summary>
public static class SeedIds
{
    // ===== Tenants =====
    /// <summary>
    /// Default tenant ID used as fallback across the system.
    /// Referenced by TenantContext (API), TenantConfiguration and TenantConstants (Blazor).
    /// </summary>
    public static readonly Guid DefaultTenantId = Guid.Parse("018e4e5c-7f00-7000-8000-000000000001");

    // ===== Actors (Development) =====
    public static readonly Guid AdminUserActorId = Guid.Parse("018e4e5c-7f00-7000-8000-000000000020");
    public static readonly Guid IslamuOrgActorId = Guid.Parse("018e4e5c-7f00-7000-8000-000000000021");
    public static readonly Guid RegularUserActorId = Guid.Parse("018e4e5c-7f00-7000-8000-000000000022");
    public static readonly Guid TechOrgActorId = Guid.Parse("018e4e5c-7f00-7000-8000-000000000023");
    public static readonly Guid ModeratorUserActorId = Guid.Parse("018e4e5c-7f00-7000-8000-000000000024");

    // ===== Users (Development) =====
    public static readonly Guid AdminUserId = Guid.Parse("018e4e5c-7f00-7000-8000-000000000030");
    public static readonly Guid RegularUserId = Guid.Parse("018e4e5c-7f00-7000-8000-000000000031");
    public static readonly Guid ModeratorUserId = Guid.Parse("018e4e5c-7f00-7000-8000-000000000032");

    // ===== Organizations (Development) =====
    public static readonly Guid IslamuOrgId = Guid.Parse("018e4e5c-7f00-7000-8000-000000000040");
    public static readonly Guid TechOrgId = Guid.Parse("018e4e5c-7f00-7000-8000-000000000042");

    // ===== Organization Members (Development) =====
    public static readonly Guid AdminIslamuCreatorId = Guid.Parse("018e4e5c-7f00-7000-8000-000000000041");
    public static readonly Guid RegularIslamuMemberId = Guid.Parse("018e4e5c-7f00-7000-8000-000000000043");
    public static readonly Guid ModeratorIslamuModId = Guid.Parse("018e4e5c-7f00-7000-8000-000000000044");
    public static readonly Guid AdminTechCoOwnerId = Guid.Parse("018e4e5c-7f00-7000-8000-000000000045");
    public static readonly Guid RegularTechCreatorId = Guid.Parse("018e4e5c-7f00-7000-8000-000000000046");

    // ===== Tenant Users (Development) =====
    public static readonly Guid AdminTenantUserId = Guid.Parse("018e4e5c-7f00-7000-8000-000000000070");
    public static readonly Guid RegularTenantUserId = Guid.Parse("018e4e5c-7f00-7000-8000-000000000071");
    public static readonly Guid ModeratorTenantUserId = Guid.Parse("018e4e5c-7f00-7000-8000-000000000072");

    // ===== Storage Objects (Development) =====
    public static readonly Guid DefaultEventImageId = Guid.Parse("018e4e5c-7f00-7000-8000-000000000050");
    public static readonly Guid DefaultProfileImageId = Guid.Parse("018e4e5c-7f00-7000-8000-000000000051");
    public static readonly Guid DefaultOrganizationLogoId = Guid.Parse("018e4e5c-7f00-7000-8000-000000000052");

    // ===== Events (Development) =====
    public static readonly Guid SampleEventId = Guid.Parse("018e4e5c-7f00-7000-8000-000000000060");

    // ===== Categories (Development) =====
    public static readonly Guid IslamicStudiesCategoryId = Guid.Parse("018e4e5c-7f00-7000-8000-000000000100");
    public static readonly Guid QuranCategoryId = Guid.Parse("018e4e5c-7f00-7000-8000-000000000101");
    public static readonly Guid HadithCategoryId = Guid.Parse("018e4e5c-7f00-7000-8000-000000000102");
    public static readonly Guid FiqhCategoryId = Guid.Parse("018e4e5c-7f00-7000-8000-000000000103");
    public static readonly Guid AqeedahCategoryId = Guid.Parse("018e4e5c-7f00-7000-8000-000000000104");
    public static readonly Guid SeerahCategoryId = Guid.Parse("018e4e5c-7f00-7000-8000-000000000105");
    public static readonly Guid ArabicLanguageCategoryId = Guid.Parse("018e4e5c-7f00-7000-8000-000000000106");
    public static readonly Guid CommunityEventsCategoryId = Guid.Parse("018e4e5c-7f00-7000-8000-000000000107");

    // ===== Tags (Development) =====
    public static readonly Guid BeginnerTagId = Guid.Parse("018e4e5c-7f00-7000-8000-000000000200");
    public static readonly Guid IntermediateTagId = Guid.Parse("018e4e5c-7f00-7000-8000-000000000201");
    public static readonly Guid AdvancedTagId = Guid.Parse("018e4e5c-7f00-7000-8000-000000000202");
    public static readonly Guid FreeTagId = Guid.Parse("018e4e5c-7f00-7000-8000-000000000203");
    public static readonly Guid PaidTagId = Guid.Parse("018e4e5c-7f00-7000-8000-000000000204");
    public static readonly Guid OnlineTagId = Guid.Parse("018e4e5c-7f00-7000-8000-000000000205");
    public static readonly Guid InPersonTagId = Guid.Parse("018e4e5c-7f00-7000-8000-000000000206");

    // ===== Locations (Development) =====
    public static readonly Guid OnlineLocationId = Guid.Parse("018e4e5c-7f00-7000-8000-000000000300");

    // ===== Tenant Settings (Development) =====
    public static readonly Guid DefaultTenantSettingsId = Guid.Parse("018e4e5c-7f00-7000-8000-000000000400");

    // ===== System Settings =====
    public static readonly Guid SystemSettingDeploymentModeId = Guid.Parse("018e4e5c-7f00-7000-8000-000000000500");
    public static readonly Guid SystemSettingMaxSessionsPerEventId = Guid.Parse("018e4e5c-7f00-7000-8000-000000000501");
    public static readonly Guid SystemSettingRequireApprovalId = Guid.Parse("018e4e5c-7f00-7000-8000-000000000502");
    public static readonly Guid SystemSettingIslamicModuleId = Guid.Parse("018e4e5c-7f00-7000-8000-000000000503");
    public static readonly Guid SystemSettingTechModuleId = Guid.Parse("018e4e5c-7f00-7000-8000-000000000504");
    public static readonly Guid SystemSettingTenantSelfServiceRegistrationId = Guid.Parse("018e4e5c-7f00-7000-8000-000000000505");
    public static readonly Guid SystemSettingUserSubmissionEnabledId = Guid.Parse("018e4e5c-7f00-7000-8000-000000000506");
    public static readonly Guid SystemSettingOrganizationVerificationRequiredId = Guid.Parse("018e4e5c-7f00-7000-8000-000000000507");
    public static readonly Guid SystemSettingOrganizationTenantCanOmitVerificationId = Guid.Parse("018e4e5c-7f00-7000-8000-000000000508");
    public static readonly Guid SystemSettingBrandingDisplayNameId = Guid.Parse("018e4e5c-7f00-7000-8000-000000000509");
    public static readonly Guid SystemSettingBrandingLogoUrlId = Guid.Parse("018e4e5c-7f00-7000-8000-000000000510");
    public static readonly Guid SystemSettingBrandingFaviconUrlId = Guid.Parse("018e4e5c-7f00-7000-8000-000000000511");
    public static readonly Guid SystemSettingBrandingCustomCssUrlId = Guid.Parse("018e4e5c-7f00-7000-8000-000000000512");
    public static readonly Guid SystemSettingRoutingDefaultPublicHomePageId = Guid.Parse("018e4e5c-7f00-7000-8000-000000000513");
    public static readonly Guid SystemSettingDomainsInstanceBaseDomainId = Guid.Parse("018e4e5c-7f00-7000-8000-000000000514");
    public static readonly Guid SystemSettingDomainsAllowTenantCustomDomainId = Guid.Parse("018e4e5c-7f00-7000-8000-000000000515");
    public static readonly Guid SystemSettingDomainsTenantSubdomainId = Guid.Parse("018e4e5c-7f00-7000-8000-000000000516");
    public static readonly Guid SystemSettingDomainsTenantCustomDomainId = Guid.Parse("018e4e5c-7f00-7000-8000-000000000517");

    // ===== System Settings — Email / SMTP =====
    public static readonly Guid SystemSettingEmailSmtpHostId = Guid.Parse("018e4e5c-7f00-7000-8000-000000000520");
    public static readonly Guid SystemSettingEmailSmtpPortId = Guid.Parse("018e4e5c-7f00-7000-8000-000000000521");
    public static readonly Guid SystemSettingEmailSmtpUsernameId = Guid.Parse("018e4e5c-7f00-7000-8000-000000000522");
    public static readonly Guid SystemSettingEmailSmtpPasswordId = Guid.Parse("018e4e5c-7f00-7000-8000-000000000523");
    public static readonly Guid SystemSettingEmailSmtpSecurityId = Guid.Parse("018e4e5c-7f00-7000-8000-000000000524");
    public static readonly Guid SystemSettingEmailFromAddressId = Guid.Parse("018e4e5c-7f00-7000-8000-000000000525");
    public static readonly Guid SystemSettingEmailFromNameId = Guid.Parse("018e4e5c-7f00-7000-8000-000000000526");
    public static readonly Guid SystemSettingEmailSmtpTimeoutId = Guid.Parse("018e4e5c-7f00-7000-8000-000000000527");
    public static readonly Guid SystemSettingEmailSmtpSkipCertValidationId = Guid.Parse("018e4e5c-7f00-7000-8000-000000000528");

    // ===== System Settings — Object Storage / S3 =====
    public static readonly Guid SystemSettingS3EndpointId = Guid.Parse("018e4e5c-7f00-7000-8000-000000000530");
    public static readonly Guid SystemSettingS3PublicEndpointId = Guid.Parse("018e4e5c-7f00-7000-8000-000000000531");
    public static readonly Guid SystemSettingS3BucketNameId = Guid.Parse("018e4e5c-7f00-7000-8000-000000000532");
    public static readonly Guid SystemSettingS3AccessKeyIdId = Guid.Parse("018e4e5c-7f00-7000-8000-000000000533");
    public static readonly Guid SystemSettingS3SecretAccessKeyId = Guid.Parse("018e4e5c-7f00-7000-8000-000000000534");
    public static readonly Guid SystemSettingS3RegionId = Guid.Parse("018e4e5c-7f00-7000-8000-000000000535");
    public static readonly Guid SystemSettingS3ForcePathStyleId = Guid.Parse("018e4e5c-7f00-7000-8000-000000000536");
    public static readonly Guid SystemSettingS3UploadUrlExpirationMinutesId = Guid.Parse("018e4e5c-7f00-7000-8000-000000000537");

    // ===== Module Definitions =====
    public static readonly Guid ModuleCoreId = Guid.Parse("018e4e5c-7f00-7000-8000-000000000600");
    public static readonly Guid ModuleIslamicId = Guid.Parse("018e4e5c-7f00-7000-8000-000000000601");
    public static readonly Guid ModuleTechId = Guid.Parse("018e4e5c-7f00-7000-8000-000000000602");

    // ===== Tenant Capabilities (Development) =====
    public static readonly Guid DefaultTenantCoreCapabilityId = Guid.Parse("018e4e5c-7f00-7000-8000-000000000610");
    public static readonly Guid DefaultTenantIslamicCapabilityId = Guid.Parse("018e4e5c-7f00-7000-8000-000000000611");
}
