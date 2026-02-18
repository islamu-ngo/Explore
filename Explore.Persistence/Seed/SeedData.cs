// ABOUTME: Centralized seed data objects for Development environment business entities.
// ABOUTME: References SeedIds for deterministic IDs. Used by DatabaseSeeder (Development only).

using Explore.Domain;
using Explore.Domain.Enums;
using Explore.Domain.Modules;

namespace Explore.Persistence.Seed;

/// <summary>
/// Centralized seed data objects for Development environment.
/// All seed data references SeedIds for consistent, deterministic IDs.
///
/// NOTE: Lookup/enum tables are seeded via LookupTableSeeder at runtime in ALL environments.
/// This class contains business entity seed data applied only in Development.
///
/// Seeding order matters due to FK constraints and circular dependencies (User/Org ↔ Actor):
/// 1. Tenant → 2. Users (no ActorId) → 3. Organizations (no ActorId) → 4. Actors →
/// 5. Update Users/Orgs with ActorId → 6. TenantUsers, OrgMembers, Storage →
/// 7. Settings, Capabilities → 8. Categories, Tags, Location → 9. Sample Event
/// </summary>
public static class SeedData
{
    private static readonly DateTime SeedTimestamp = new(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc);

    // ===== Tenant =====
    public static Tenant DefaultTenant => new()
    {
        Id = SeedIds.DefaultTenantId,
        FullName = "ISLAMU Default Tenant",
        Slug = "default",
        TenantStatusId = (int)TenantStatusEnum.Active,
        TenantStatus = null!
    };

    // ===== Users =====
    public static User AdminUser => new()
    {
        Id = SeedIds.AdminUserId,
        Email = "admin@islamu.dev",
        FirstName = "Admin",
        LastName = "User",
        AuthProvider = "dev",
        AuthProviderId = "admin-001",
        EmailVerified = true,
        CreatedAt = SeedTimestamp
    };

    public static User RegularUser => new()
    {
        Id = SeedIds.RegularUserId,
        Email = "user@islamu.dev",
        FirstName = "Regular",
        LastName = "User",
        AuthProvider = "dev",
        AuthProviderId = "user-001",
        EmailVerified = true,
        CreatedAt = SeedTimestamp
    };

    public static User ModeratorUser => new()
    {
        Id = SeedIds.ModeratorUserId,
        Email = "moderator@islamu.dev",
        FirstName = "Moderator",
        LastName = "User",
        AuthProvider = "dev",
        AuthProviderId = "moderator-001",
        EmailVerified = true,
        CreatedAt = SeedTimestamp
    };

    // ===== Organizations =====
    public static Organization IslamuOrg => new()
    {
        Id = SeedIds.IslamuOrgId,
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
        CreatedAt = SeedTimestamp
    };

    public static Organization TechOrg => new()
    {
        Id = SeedIds.TechOrgId,
        FullName = "Tech Community",
        WebsiteUrl = "https://techcommunity.dev",
        Email = "hello@techcommunity.dev",
        Country = "Belgium",
        City = "Antwerp",
        Postcode = "2000",
        Address = "Tech Hub 1",
        ApprovalStatusId = (int)ApprovalStatusEnum.Approved,
        ApprovalStatus = null!,
        TenantId = SeedIds.DefaultTenantId,
        Tenant = null!,
        CreatedAt = SeedTimestamp
    };

    // ===== User Actors =====
    public static Actor AdminUserActor => new()
    {
        Id = SeedIds.AdminUserActorId,
        ActorTypeId = (int)ActorTypeEnum.User,
        ActorType = null!,
        TenantId = SeedIds.DefaultTenantId,
        Tenant = null!,
        DisplayName = "Admin User",
        Handle = "admin",
        Description = "Platform administrator",
        UserId = SeedIds.AdminUserId,
        OrganizationId = null
    };

    public static Actor RegularUserActor => new()
    {
        Id = SeedIds.RegularUserActorId,
        ActorTypeId = (int)ActorTypeEnum.User,
        ActorType = null!,
        TenantId = SeedIds.DefaultTenantId,
        Tenant = null!,
        DisplayName = "Regular User",
        Handle = "user",
        Description = "Regular platform user",
        UserId = SeedIds.RegularUserId,
        OrganizationId = null
    };

    public static Actor ModeratorUserActor => new()
    {
        Id = SeedIds.ModeratorUserActorId,
        ActorTypeId = (int)ActorTypeEnum.User,
        ActorType = null!,
        TenantId = SeedIds.DefaultTenantId,
        Tenant = null!,
        DisplayName = "Moderator User",
        Handle = "moderator",
        Description = "Platform moderator",
        UserId = SeedIds.ModeratorUserId,
        OrganizationId = null
    };

    // ===== Organization Actors =====
    public static Actor IslamuOrgActor => new()
    {
        Id = SeedIds.IslamuOrgActorId,
        ActorTypeId = (int)ActorTypeEnum.Organization,
        ActorType = null!,
        TenantId = SeedIds.DefaultTenantId,
        Tenant = null!,
        DisplayName = "ISLAMU",
        Handle = "islamu",
        Description = "ISLAMU NGO - Islamic Learning and Media Union",
        UserId = null,
        OrganizationId = SeedIds.IslamuOrgId
    };

    public static Actor TechOrgActor => new()
    {
        Id = SeedIds.TechOrgActorId,
        ActorTypeId = (int)ActorTypeEnum.Organization,
        ActorType = null!,
        TenantId = SeedIds.DefaultTenantId,
        Tenant = null!,
        DisplayName = "Tech Community",
        Handle = "techcommunity",
        Description = "Tech Community Belgium",
        UserId = null,
        OrganizationId = SeedIds.TechOrgId
    };

    // ===== Tenant Users =====
    public static TenantUser AdminTenantUser => new()
    {
        Id = SeedIds.AdminTenantUserId,
        UserId = SeedIds.AdminUserId,
        User = null!,
        TenantId = SeedIds.DefaultTenantId,
        Tenant = null!,
        RoleId = (int)RoleEnum.TenantOwner,
        Role = null!
    };

    public static TenantUser RegularTenantUser => new()
    {
        Id = SeedIds.RegularTenantUserId,
        UserId = SeedIds.RegularUserId,
        User = null!,
        TenantId = SeedIds.DefaultTenantId,
        Tenant = null!,
        RoleId = (int)RoleEnum.TenantMember,
        Role = null!
    };

    public static TenantUser ModeratorTenantUser => new()
    {
        Id = SeedIds.ModeratorTenantUserId,
        UserId = SeedIds.ModeratorUserId,
        User = null!,
        TenantId = SeedIds.DefaultTenantId,
        Tenant = null!,
        RoleId = (int)RoleEnum.TenantModerator,
        Role = null!
    };

    // ===== Organization Members =====
    // Admin is Creator of ISLAMU (Founder position)
    public static OrganizationMember AdminIslamuCreator => new()
    {
        Id = SeedIds.AdminIslamuCreatorId,
        OrganizationId = SeedIds.IslamuOrgId,
        Organization = null!,
        UserId = SeedIds.AdminUserId,
        User = null!,
        TenantId = SeedIds.DefaultTenantId,
        Tenant = null!,
        RoleId = (int)RoleEnum.OrgCreator,
        Role = null!,
        OrganizationPositionId = (int)OrganizationPositionEnum.Founder,
        CreatedAt = SeedTimestamp
    };

    // Regular user is Member of ISLAMU
    public static OrganizationMember RegularIslamuMember => new()
    {
        Id = SeedIds.RegularIslamuMemberId,
        OrganizationId = SeedIds.IslamuOrgId,
        Organization = null!,
        UserId = SeedIds.RegularUserId,
        User = null!,
        TenantId = SeedIds.DefaultTenantId,
        Tenant = null!,
        RoleId = (int)RoleEnum.OrgMember,
        Role = null!,
        OrganizationPositionId = (int)OrganizationPositionEnum.Volunteer,
        CreatedAt = SeedTimestamp
    };

    // Moderator is Moderator of ISLAMU
    public static OrganizationMember ModeratorIslamuMod => new()
    {
        Id = SeedIds.ModeratorIslamuModId,
        OrganizationId = SeedIds.IslamuOrgId,
        Organization = null!,
        UserId = SeedIds.ModeratorUserId,
        User = null!,
        TenantId = SeedIds.DefaultTenantId,
        Tenant = null!,
        RoleId = (int)RoleEnum.OrgModerator,
        Role = null!,
        OrganizationPositionId = (int)OrganizationPositionEnum.Coordinator,
        CreatedAt = SeedTimestamp
    };

    // Admin is CoOwner of Tech org
    public static OrganizationMember AdminTechCoOwner => new()
    {
        Id = SeedIds.AdminTechCoOwnerId,
        OrganizationId = SeedIds.TechOrgId,
        Organization = null!,
        UserId = SeedIds.AdminUserId,
        User = null!,
        TenantId = SeedIds.DefaultTenantId,
        Tenant = null!,
        RoleId = (int)RoleEnum.OrgCoOwner,
        Role = null!,
        OrganizationPositionId = (int)OrganizationPositionEnum.Director,
        CreatedAt = SeedTimestamp
    };

    // Regular user is Creator of Tech org (Founder position)
    public static OrganizationMember RegularTechCreator => new()
    {
        Id = SeedIds.RegularTechCreatorId,
        OrganizationId = SeedIds.TechOrgId,
        Organization = null!,
        UserId = SeedIds.RegularUserId,
        User = null!,
        TenantId = SeedIds.DefaultTenantId,
        Tenant = null!,
        RoleId = (int)RoleEnum.OrgCreator,
        Role = null!,
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
        ActorId = SeedIds.AdminUserActorId
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
        ActorId = SeedIds.AdminUserActorId
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
        ActorId = SeedIds.AdminUserActorId
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

    // ===== Sample Event =====
    public static Event SampleEvent => new()
    {
        Id = SeedIds.SampleEventId,
        Title = "Welcome to ISLAMU Events",
        Description = "This is a sample event to demonstrate the ISLAMU Events platform. Feel free to explore and create your own events!",
        Slug = "welcome-to-islamu-events",
        EventTypeId = (int)EventTypeEnum.Webinar,
        AudienceGenderId = (int)AudienceGenderEnum.Both,
        AudienceAgeId = (int)AudienceAgeEnum.AllAges,
        ActorId = SeedIds.IslamuOrgActorId,
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
}
