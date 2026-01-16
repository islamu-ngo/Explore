using System;

namespace Explore.Persistence.Seed
{
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

        // Note: Most lookup tables use int IDs with enums.
        // Only entities with Guid primary keys need entries here.
        // Int-based IDs are defined via enums in Domain/Enums folder.
    }
}
