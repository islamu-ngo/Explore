// ABOUTME: Centralized seed data objects for the application.
// Contains all seed entity instances that reference SeedIds for consistency.

using Explore.Domain;
using Explore.Domain.Enums;
using Explore.Domain.Modules;

namespace Explore.Persistence.Seed;

/// <summary>
/// Centralized seed data objects for business entities.
/// All seed data references SeedIds for consistent, deterministic IDs.
///
/// NOTE: Lookup/enum tables are seeded via LookupTableSeeder at runtime.
/// This class contains business entity seed data that may be conditionally applied.
/// </summary>
public static class SeedData
{
    // ===== Timestamps =====
    private static readonly DateTime SeedTimestamp = new(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc);

    // ===== Tenant =====
    public static Tenant DefaultTenant => new()
    {
        Id = SeedIds.DefaultTenantId,
        FullName = "ISLAMU Default Tenant",
        Slug = "default",
        IsActive = true
    };

    // ===== User =====
    public static User SystemUser => new()
    {
        Id = SeedIds.SystemUserId,
        Email = "system@islamu.org",
        FirstName = "System",
        LastName = "Account",
        ActorId = SeedIds.SystemUserActorId,
        AuthProvider = "system",
        AuthProviderId = "system",
        EmailVerified = true
    };

    // ===== Organization =====
    public static Organization IslamuOrganization => new()
    {
        Id = SeedIds.IslamuOrganizationId,
        FullName = "ISLAMU",
        WebsiteUrl = "https://islamu.ngo",
        Email = "contact@openislamu.org",
        Country = "Belgium",
        City = "Brussels",
        Postcode = "1070",
        Address = "Parc Du Peterbos",
        ApprovalStatusId = (int)ApprovalStatusEnum.Approved,
        ApprovalStatus = null!,
        TenantId = SeedIds.DefaultTenantId,
        Tenant = null!,
        ActorId = SeedIds.IslamuOrganizationActorId,
        CreatedAt = SeedTimestamp
    };

    // ===== Actors =====
    public static Actor SystemUserActor => new()
    {
        Id = SeedIds.SystemUserActorId,
        ActorTypeId = (int)ActorTypeEnum.User,
        ActorType = null!,
        TenantId = SeedIds.DefaultTenantId,
        Tenant = null!,
        DisplayName = "System Account",
        Handle = "system",
        Description = "System user account",
        UserId = SeedIds.SystemUserId,
        OrganizationId = null
    };

    public static Actor IslamuOrganizationActor => new()
    {
        Id = SeedIds.IslamuOrganizationActorId,
        ActorTypeId = (int)ActorTypeEnum.Organization,
        ActorType = null!,
        TenantId = SeedIds.DefaultTenantId,
        Tenant = null!,
        DisplayName = "ISLAMU",
        Handle = "islamu",
        Description = "ISLAMU NGO - Islamic Learning and Media Union",
        UserId = null,
        OrganizationId = SeedIds.IslamuOrganizationId
    };

    // ===== Organization Members =====
    public static OrganizationMember SystemUserIslamuMember => new()
    {
        Id = SeedIds.SystemUserIslamuMemberId,
        OrganizationId = SeedIds.IslamuOrganizationId,
        Organization = null!,
        UserId = SeedIds.SystemUserId,
        User = null!,
        TenantId = SeedIds.DefaultTenantId,
        Tenant = null!,
        OrganizationRoleId = (int)OrganizationRoleEnum.Creator,
        OrganizationRole = null!,
        OrganizationPositionId = (int)OrganizationPositionEnum.Founder,
        CreatedAt = SeedTimestamp
    };

    // ===== Storage Objects =====
    public static StorageObject DefaultEventImage => new()
    {
        Id = SeedIds.DefaultEventImageId,
        Uri = "https://placeholder.islamu.org/event-default.jpg",
        FullName = "Default Event Image",
        Extension = ".jpg",
        Size = 0,
        FileTypeId = (int)FileTypeEnum.Image,
        FileType = null!,
        TenantId = SeedIds.DefaultTenantId,
        Tenant = null!,
        ActorId = SeedIds.SystemUserActorId
    };

    public static StorageObject DefaultProfileImage => new()
    {
        Id = SeedIds.DefaultProfileImageId,
        Uri = "https://placeholder.islamu.org/profile-default.jpg",
        FullName = "Default Profile Image",
        Extension = ".jpg",
        Size = 0,
        FileTypeId = (int)FileTypeEnum.Image,
        FileType = null!,
        TenantId = SeedIds.DefaultTenantId,
        Tenant = null!,
        ActorId = SeedIds.SystemUserActorId
    };

    public static StorageObject DefaultOrganizationLogo => new()
    {
        Id = SeedIds.DefaultOrganizationLogoId,
        Uri = "https://placeholder.islamu.org/org-default.jpg",
        FullName = "Default Organization Logo",
        Extension = ".jpg",
        Size = 0,
        FileTypeId = (int)FileTypeEnum.Image,
        FileType = null!,
        TenantId = SeedIds.DefaultTenantId,
        Tenant = null!,
        ActorId = SeedIds.SystemUserActorId
    };

    // ===== Tenant Settings =====
    public static TenantSettings DefaultTenantSettings => new()
    {
        Id = SeedIds.DefaultTenantSettingsId,
        TenantId = SeedIds.DefaultTenantId,
        Tenant = null!
    };

    // ===== Tenant Capabilities =====
    public static TenantCapability DefaultTenantCoreCapability => new()
    {
        Id = SeedIds.DefaultTenantCoreCapabilityId,
        TenantId = SeedIds.DefaultTenantId,
        Tenant = null!,
        ModuleId = SeedIds.ModuleCoreId,
        Module = null!,
        IsEnabled = true,
        EnabledAt = SeedTimestamp
    };

    public static TenantCapability DefaultTenantIslamicCapability => new()
    {
        Id = SeedIds.DefaultTenantIslamicCapabilityId,
        TenantId = SeedIds.DefaultTenantId,
        Tenant = null!,
        ModuleId = SeedIds.ModuleIslamicId,
        Module = null!,
        IsEnabled = true,
        EnabledAt = SeedTimestamp
    };

    // ===== Location =====
    public static Location OnlineLocation => new()
    {
        Id = SeedIds.OnlineLocationId,
        FullName = "Online / Virtual",
        Address = "Virtual",
        Postcode = "00000",
        Country = "Internet",
        City = "Virtual",
        Timezone = "UTC",
        TenantId = SeedIds.DefaultTenantId,
        Tenant = null!
    };

    // ===== Categories =====
    public static Category IslamicStudiesCategory => new()
    {
        Id = SeedIds.IslamicStudiesCategoryId,
        MasterCode = "ISLAMIC_STUDIES",
        FullName = "Islamic Studies",
        TenantId = SeedIds.DefaultTenantId,
        Tenant = null!,
        ParentId = null
    };

    public static Category QuranCategory => new()
    {
        Id = SeedIds.QuranCategoryId,
        MasterCode = "QURAN",
        FullName = "Quran & Tafsir",
        TenantId = SeedIds.DefaultTenantId,
        Tenant = null!,
        ParentId = SeedIds.IslamicStudiesCategoryId
    };

    public static Category HadithCategory => new()
    {
        Id = SeedIds.HadithCategoryId,
        MasterCode = "HADITH",
        FullName = "Hadith Sciences",
        TenantId = SeedIds.DefaultTenantId,
        Tenant = null!,
        ParentId = SeedIds.IslamicStudiesCategoryId
    };

    public static Category FiqhCategory => new()
    {
        Id = SeedIds.FiqhCategoryId,
        MasterCode = "FIQH",
        FullName = "Fiqh (Islamic Jurisprudence)",
        TenantId = SeedIds.DefaultTenantId,
        Tenant = null!,
        ParentId = SeedIds.IslamicStudiesCategoryId
    };

    public static Category AqeedahCategory => new()
    {
        Id = SeedIds.AqeedahCategoryId,
        MasterCode = "AQEEDAH",
        FullName = "Aqeedah (Islamic Creed)",
        TenantId = SeedIds.DefaultTenantId,
        Tenant = null!,
        ParentId = SeedIds.IslamicStudiesCategoryId
    };

    public static Category SeerahCategory => new()
    {
        Id = SeedIds.SeerahCategoryId,
        MasterCode = "SEERAH",
        FullName = "Seerah (Prophetic Biography)",
        TenantId = SeedIds.DefaultTenantId,
        Tenant = null!,
        ParentId = SeedIds.IslamicStudiesCategoryId
    };

    public static Category ArabicLanguageCategory => new()
    {
        Id = SeedIds.ArabicLanguageCategoryId,
        MasterCode = "ARABIC",
        FullName = "Arabic Language",
        TenantId = SeedIds.DefaultTenantId,
        Tenant = null!,
        ParentId = null
    };

    public static Category CommunityEventsCategory => new()
    {
        Id = SeedIds.CommunityEventsCategoryId,
        MasterCode = "COMMUNITY",
        FullName = "Community Events",
        TenantId = SeedIds.DefaultTenantId,
        Tenant = null!,
        ParentId = null
    };

    // ===== Tags =====
    public static Tag BeginnerTag => new()
    {
        Id = SeedIds.BeginnerTagId,
        MasterCode = "BEGINNER",
        FullName = "Beginner",
        TenantId = SeedIds.DefaultTenantId,
        Tenant = null!
    };

    public static Tag IntermediateTag => new()
    {
        Id = SeedIds.IntermediateTagId,
        MasterCode = "INTERMEDIATE",
        FullName = "Intermediate",
        TenantId = SeedIds.DefaultTenantId,
        Tenant = null!
    };

    public static Tag AdvancedTag => new()
    {
        Id = SeedIds.AdvancedTagId,
        MasterCode = "ADVANCED",
        FullName = "Advanced",
        TenantId = SeedIds.DefaultTenantId,
        Tenant = null!
    };

    public static Tag FreeTag => new()
    {
        Id = SeedIds.FreeTagId,
        MasterCode = "FREE",
        FullName = "Free",
        TenantId = SeedIds.DefaultTenantId,
        Tenant = null!
    };

    public static Tag PaidTag => new()
    {
        Id = SeedIds.PaidTagId,
        MasterCode = "PAID",
        FullName = "Paid",
        TenantId = SeedIds.DefaultTenantId,
        Tenant = null!
    };

    public static Tag OnlineTag => new()
    {
        Id = SeedIds.OnlineTagId,
        MasterCode = "ONLINE",
        FullName = "Online",
        TenantId = SeedIds.DefaultTenantId,
        Tenant = null!
    };

    public static Tag InPersonTag => new()
    {
        Id = SeedIds.InPersonTagId,
        MasterCode = "IN_PERSON",
        FullName = "In-Person",
        TenantId = SeedIds.DefaultTenantId,
        Tenant = null!
    };

    // ===== Sample Event (Development Only) =====
    public static Event SampleEvent => new()
    {
        Id = SeedIds.SampleEventId,
        Title = "Welcome to ISLAMU Events",
        Description = "This is a sample event to demonstrate the ISLAMU Events platform. Feel free to explore and create your own events!",
        Slug = "welcome-to-islamu-events",
        EventTypeId = (int)EventTypeEnum.Webinar,
        AudienceGenderId = (int)AudienceGenderEnum.Both,
        AudienceAgeId = (int)AudienceAgeEnum.AllAges,
        ActorId = SeedIds.IslamuOrganizationActorId,
        Actor = null!,
        Price = 0,
        CurrencyCode = "EUR",
        FeaturedImageId = SeedIds.DefaultEventImageId,
        TotalViews = 0,
        IsRegistrationRequired = false,
        MadhabId = null,
        TenantId = SeedIds.DefaultTenantId,
        Tenant = null!,
        VisibilityTypeId = (int)VisibilityTypeEnum.Public,
        VisibilityType = null!,
        EventStatusId = (int)EventStatusEnum.Published,
        EventStatus = null!,
        EventFormatId = (int)EventFormatEnum.Digital,
        EventFormat = null!,
        Timezone = "Europe/Brussels"
    };

    // ===== UserRoles (Development Only - required for testing) =====
    public static UserRole SuperAdminRole => new()
    {
        Id = 1,
        FullName = "Super Administrator",
        MasterCode = "SUPER_ADMIN",
        Description = "Full system access",
        TenantId = SeedIds.DefaultTenantId,
        Tenant = null!
    };

    public static UserRole AdminRole => new()
    {
        Id = 2,
        FullName = "Administrator",
        MasterCode = "ADMIN",
        Description = "Organization administrator",
        TenantId = SeedIds.DefaultTenantId,
        Tenant = null!
    };

    public static UserRole ModeratorRole => new()
    {
        Id = 3,
        FullName = "Moderator",
        MasterCode = "MODERATOR",
        Description = "Content moderator",
        TenantId = SeedIds.DefaultTenantId,
        Tenant = null!
    };

    public static UserRole UserRoleData => new()
    {
        Id = 4,
        FullName = "User",
        MasterCode = "USER",
        Description = "Standard user",
        TenantId = SeedIds.DefaultTenantId,
        Tenant = null!
    };
}
