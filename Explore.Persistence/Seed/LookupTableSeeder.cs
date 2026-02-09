// ABOUTME: Seeds all lookup/enum tables at runtime in ALL environments.
// Replaces HasData() in entity configurations to avoid EF Core circular FK migration bug (#36682).

using Explore.Domain;
using Explore.Domain.Constants;
using Explore.Domain.Enums;
using Explore.Domain.Modules;
using Microsoft.EntityFrameworkCore;

namespace Explore.Persistence.Seed;

/// <summary>
/// Seeds lookup/enum tables at runtime. Runs in ALL environments (dev, staging, production).
///
/// Why runtime seeding instead of HasData():
/// EF Core 10 has a known bug (#36682) where dotnet ef migrations add crashes with
/// "Sequence contains no elements" when the model has circular FKs (User/Organization ↔ Actor)
/// combined with HasData on any entities. Moving seed data to runtime eliminates the issue.
///
/// The data was originally seeded via HasData() in entity configurations and exists in
/// existing migration files. This seeder ensures idempotent seeding for fresh databases
/// where migrations run in order.
/// </summary>
public static class LookupTableSeeder
{
    /// <summary>
    /// Seeds all lookup tables if they don't already contain data.
    /// Must be called after migrations are applied.
    /// </summary>
    public static async Task SeedAsync(ExploreDbContext context, CancellationToken cancellationToken = default)
    {
        await SeedActorTypesAsync(context, cancellationToken);
        await SeedApprovalStatusesAsync(context, cancellationToken);
        await SeedAudienceAgesAsync(context, cancellationToken);
        await SeedAudienceGendersAsync(context, cancellationToken);
        await SeedDidCustodyTypesAsync(context, cancellationToken);
        await SeedEventFormatsAsync(context, cancellationToken);
        await SeedEventStatusesAsync(context, cancellationToken);
        await SeedEventTypesAsync(context, cancellationToken);
        await SeedFileTypesAsync(context, cancellationToken);
        await SeedLanguagesAsync(context, cancellationToken);
        await SeedMadhabsAsync(context, cancellationToken);
        await SeedModuleDefinitionsAsync(context, cancellationToken);
        await SeedOrganizationPositionsAsync(context, cancellationToken);
        await SeedOrganizationRolesAsync(context, cancellationToken);
        await SeedRegistrationModesAsync(context, cancellationToken);
        await SeedSystemSettingsAsync(context, cancellationToken);
        await SeedTagTypesAsync(context, cancellationToken);
        await SeedTenantAdministratorRolesAsync(context, cancellationToken);
        await SeedVisibilityTypesAsync(context, cancellationToken);
    }

    private static async Task SeedActorTypesAsync(ExploreDbContext context, CancellationToken ct)
    {
        if (await context.Set<ActorType>().AnyAsync(ct)) return;

        context.Set<ActorType>().AddRange(
            new ActorType { Id = (int)ActorTypeEnum.User, MasterCode = "USER", FullName = "User", Description = "Individual user actor" },
            new ActorType { Id = (int)ActorTypeEnum.Organization, MasterCode = "ORGANIZATION", FullName = "Organization", Description = "Organization actor" },
            new ActorType { Id = (int)ActorTypeEnum.Bot, MasterCode = "BOT", FullName = "Bot", Description = "Automated bot actor" });
        await context.SaveChangesAsync(ct);
    }

    private static async Task SeedApprovalStatusesAsync(ExploreDbContext context, CancellationToken ct)
    {
        if (await context.Set<ApprovalStatus>().AnyAsync(ct)) return;

        context.Set<ApprovalStatus>().AddRange(
            new ApprovalStatus { Id = (int)ApprovalStatusEnum.Pending, MasterCode = "PENDING", FullName = "Pending", Description = "Status is pending approval of Admin verifying the Existence of Legal Entity" },
            new ApprovalStatus { Id = (int)ApprovalStatusEnum.Approved, MasterCode = "APPROVED", FullName = "Approved", Description = "Status has been approved by Admin after verifying the Existence of Legal Entity" },
            new ApprovalStatus { Id = (int)ApprovalStatusEnum.Rejected, MasterCode = "REJECTED", FullName = "Rejected", Description = "Status has been rejected by Admin after failing to verify the Existence of Legal Entity" });
        await context.SaveChangesAsync(ct);
    }

    private static async Task SeedAudienceAgesAsync(ExploreDbContext context, CancellationToken ct)
    {
        if (await context.Set<AudienceAge>().AnyAsync(ct)) return;

        context.Set<AudienceAge>().AddRange(
            new AudienceAge { Id = (int)AudienceAgeEnum.AllAges, MasterCode = "ALL_AGES", FullName = "All Ages", MinAge = null, MaxAge = null },
            new AudienceAge { Id = (int)AudienceAgeEnum.AdultsOnly18Plus, MasterCode = "ADULTS_18_PLUS", FullName = "Adults Only (18+)", MinAge = 18, MaxAge = null },
            new AudienceAge { Id = (int)AudienceAgeEnum.Teens16Plus, MasterCode = "TEENS_16_PLUS", FullName = "Teens & Adults (16+)", MinAge = 16, MaxAge = null },
            new AudienceAge { Id = (int)AudienceAgeEnum.Preteens12Plus, MasterCode = "PRETEENS_12_PLUS", FullName = "Preteens & Up (12+)", MinAge = 12, MaxAge = null },
            new AudienceAge { Id = (int)AudienceAgeEnum.ChildrenUnder6, MasterCode = "CHILDREN_UNDER_6", FullName = "Young Children (0-6)", MinAge = null, MaxAge = 6 },
            new AudienceAge { Id = (int)AudienceAgeEnum.YouthUnder12, MasterCode = "YOUTH_UNDER_12", FullName = "Children (0-12)", MinAge = null, MaxAge = 12 },
            new AudienceAge { Id = (int)AudienceAgeEnum.YouthUnder16, MasterCode = "YOUTH_UNDER_16", FullName = "Children & Young Teens (0-16)", MinAge = null, MaxAge = 16 },
            new AudienceAge { Id = (int)AudienceAgeEnum.YouthUnder18, MasterCode = "YOUTH_UNDER_18", FullName = "Youth (0-18)", MinAge = null, MaxAge = 18 });
        await context.SaveChangesAsync(ct);
    }

    private static async Task SeedAudienceGendersAsync(ExploreDbContext context, CancellationToken ct)
    {
        if (await context.Set<AudienceGender>().AnyAsync(ct)) return;

        context.Set<AudienceGender>().AddRange(
            new AudienceGender { Id = (int)AudienceGenderEnum.Man, MasterCode = "MAN", FullName = "Man", Description = "Only for Man Audience" },
            new AudienceGender { Id = (int)AudienceGenderEnum.Woman, MasterCode = "WOMAN", FullName = "Woman", Description = "Only for Woman Audience" },
            new AudienceGender { Id = (int)AudienceGenderEnum.Both, MasterCode = "BOTH_SEGREGATED", FullName = "Both Segregated", Description = "For Both Man and Woman but Segregated so no free mixing" },
            new AudienceGender { Id = 4, MasterCode = "BOTH_FREE_MIXING", FullName = "Both Free Mixing", Description = "For Both Man and Woman but Free Mixing" });
        await context.SaveChangesAsync(ct);
    }

    private static async Task SeedDidCustodyTypesAsync(ExploreDbContext context, CancellationToken ct)
    {
        if (await context.Set<DidCustodyType>().AnyAsync(ct)) return;

        context.Set<DidCustodyType>().AddRange(
            new DidCustodyType { Id = (int)DidCustodyTypeEnum.Custodial, MasterCode = "CUSTODIAL", FullName = "Custodial", Description = "Platform manages the DID keys" },
            new DidCustodyType { Id = (int)DidCustodyTypeEnum.SelfCustody, MasterCode = "SELF_CUSTODY", FullName = "Self-Custody", Description = "User manages their own DID keys" });
        await context.SaveChangesAsync(ct);
    }

    private static async Task SeedEventFormatsAsync(ExploreDbContext context, CancellationToken ct)
    {
        if (await context.Set<EventFormat>().AnyAsync(ct)) return;

        context.Set<EventFormat>().AddRange(
            new EventFormat { Id = (int)EventFormatEnum.Local, MasterCode = "LOCAL", FullName = "Local (In-Person)", Description = "Event takes place at a physical location" },
            new EventFormat { Id = (int)EventFormatEnum.Digital, MasterCode = "DIGITAL", FullName = "Digital (Online)", Description = "Event takes place online" },
            new EventFormat { Id = (int)EventFormatEnum.Hybrid, MasterCode = "HYBRID", FullName = "Hybrid", Description = "Event takes place both in-person and online" });
        await context.SaveChangesAsync(ct);
    }

    private static async Task SeedEventStatusesAsync(ExploreDbContext context, CancellationToken ct)
    {
        if (await context.Set<EventStatus>().AnyAsync(ct)) return;

        context.Set<EventStatus>().AddRange(
            new EventStatus { Id = (int)EventStatusEnum.Draft, MasterCode = "DRAFT", FullName = "Draft", Description = "Event is in draft state and not visible to the public" },
            new EventStatus { Id = (int)EventStatusEnum.Published, MasterCode = "PUBLISHED", FullName = "Published", Description = "Event is published and visible to the public" },
            new EventStatus { Id = (int)EventStatusEnum.Cancelled, MasterCode = "CANCELLED", FullName = "Cancelled", Description = "Event has been cancelled" },
            new EventStatus { Id = (int)EventStatusEnum.Completed, MasterCode = "COMPLETED", FullName = "Completed", Description = "Event has been completed" },
            new EventStatus { Id = (int)EventStatusEnum.Archived, MasterCode = "ARCHIVED", FullName = "Archived", Description = "Event has been archived" });
        await context.SaveChangesAsync(ct);
    }

    private static async Task SeedEventTypesAsync(ExploreDbContext context, CancellationToken ct)
    {
        if (await context.Set<EventType>().AnyAsync(ct)) return;

        context.Set<EventType>().AddRange(
            new EventType { Id = (int)EventTypeEnum.Conference, MasterCode = "CONFERENCE", FullName = "Conference" },
            new EventType { Id = (int)EventTypeEnum.Webinar, MasterCode = "WEBINAR", FullName = "Webinar" },
            new EventType { Id = (int)EventTypeEnum.Workshop, MasterCode = "WORKSHOP", FullName = "Workshop" });
        await context.SaveChangesAsync(ct);
    }

    private static async Task SeedFileTypesAsync(ExploreDbContext context, CancellationToken ct)
    {
        if (await context.Set<FileType>().AnyAsync(ct)) return;

        context.Set<FileType>().AddRange(
            new FileType { Id = (int)FileTypeEnum.Image, MasterCode = "IMAGE", FullName = "Image", Description = "Image file (PNG, JPG, GIF, etc.)" },
            new FileType { Id = (int)FileTypeEnum.Document, MasterCode = "DOCUMENT", FullName = "Document", Description = "Document file (PDF, DOC, etc.)" },
            new FileType { Id = (int)FileTypeEnum.Video, MasterCode = "VIDEO", FullName = "Video", Description = "Video file (MP4, AVI, etc.)" },
            new FileType { Id = (int)FileTypeEnum.Audio, MasterCode = "AUDIO", FullName = "Audio", Description = "Audio file (MP3, WAV, etc.)" },
            new FileType { Id = (int)FileTypeEnum.Other, MasterCode = "OTHER", FullName = "Other", Description = "Other file type" });
        await context.SaveChangesAsync(ct);
    }

    private static async Task SeedLanguagesAsync(ExploreDbContext context, CancellationToken ct)
    {
        if (await context.Set<Language>().AnyAsync(ct)) return;

        context.Set<Language>().AddRange(
            new Language { Id = 1, MasterCode = "AR", FullName = "Arabic", Description = "Arabic language" },
            new Language { Id = 2, MasterCode = "EN", FullName = "English", Description = "English language" },
            new Language { Id = 3, MasterCode = "FR", FullName = "French", Description = "French language" },
            new Language { Id = 4, MasterCode = "TR", FullName = "Turkish", Description = "Turkish language" },
            new Language { Id = 5, MasterCode = "UR", FullName = "Urdu", Description = "Urdu language" },
            new Language { Id = 6, MasterCode = "ID", FullName = "Indonesian", Description = "Indonesian language" },
            new Language { Id = 7, MasterCode = "MS", FullName = "Malay", Description = "Malay language" },
            new Language { Id = 8, MasterCode = "BN", FullName = "Bengali", Description = "Bengali language" },
            new Language { Id = 9, MasterCode = "FA", FullName = "Persian", Description = "Persian/Farsi language" },
            new Language { Id = 10, MasterCode = "DE", FullName = "German", Description = "German language" },
            new Language { Id = 11, MasterCode = "NL", FullName = "Dutch", Description = "Dutch language" },
            new Language { Id = 12, MasterCode = "ES", FullName = "Spanish", Description = "Spanish language" });
        await context.SaveChangesAsync(ct);
    }

    private static async Task SeedMadhabsAsync(ExploreDbContext context, CancellationToken ct)
    {
        if (await context.Set<Madhab>().AnyAsync(ct)) return;

        context.Set<Madhab>().AddRange(
            new Madhab { Id = (int)MadhabEnum.Hanafi, MasterCode = "HANAFI", FullName = "Hanafi", Description = "Hanafi school of Islamic jurisprudence" },
            new Madhab { Id = (int)MadhabEnum.Maliki, MasterCode = "MALIKI", FullName = "Maliki", Description = "Maliki school of Islamic jurisprudence" },
            new Madhab { Id = (int)MadhabEnum.Shafii, MasterCode = "SHAFII", FullName = "Shafi'i", Description = "Shafi'i school of Islamic jurisprudence" },
            new Madhab { Id = (int)MadhabEnum.Hanbali, MasterCode = "HANBALI", FullName = "Hanbali", Description = "Hanbali school of Islamic jurisprudence" },
            new Madhab { Id = (int)MadhabEnum.Other, MasterCode = "OTHER", FullName = "Other", Description = "Other Islamic jurisprudence approach" });
        await context.SaveChangesAsync(ct);
    }

    private static async Task SeedModuleDefinitionsAsync(ExploreDbContext context, CancellationToken ct)
    {
        if (await context.Set<ModuleDefinition>().AnyAsync(ct)) return;

        var seedTimestamp = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        context.Set<ModuleDefinition>().AddRange(
            new ModuleDefinition { Id = SeedIds.ModuleCoreId, ModuleKey = "Mod_Core", Name = "Core Events", Description = "Basic event functionality - title, description, sessions, locations", IconName = "Event", Category = "Core", DisplayOrder = 0, IsActive = true, CreatedAt = seedTimestamp },
            new ModuleDefinition { Id = SeedIds.ModuleIslamicId, ModuleKey = "Mod_Islamic", Name = "Islamic Events", Description = "Islamic-specific features: Madhab selection, prayer time scheduling, gender segregation", IconName = "Mosque", Category = "Domain", DisplayOrder = 1, IsActive = true, CreatedAt = seedTimestamp },
            new ModuleDefinition { Id = SeedIds.ModuleTechId, ModuleKey = "Mod_Tech", Name = "Tech Events", Description = "Developer event features: GitHub repositories, skill levels, live coding sessions", IconName = "Code", Category = "Domain", DisplayOrder = 2, IsActive = true, CreatedAt = seedTimestamp });
        await context.SaveChangesAsync(ct);
    }

    private static async Task SeedOrganizationPositionsAsync(ExploreDbContext context, CancellationToken ct)
    {
        if (await context.Set<OrganizationPosition>().AnyAsync(ct)) return;

        context.Set<OrganizationPosition>().AddRange(
            new OrganizationPosition { Id = (int)OrganizationPositionEnum.Founder, MasterCode = "FOUNDER", FullName = "Founder", Description = "Organization founder" },
            new OrganizationPosition { Id = (int)OrganizationPositionEnum.Director, MasterCode = "DIRECTOR", FullName = "Director", Description = "Organization director" },
            new OrganizationPosition { Id = (int)OrganizationPositionEnum.Manager, MasterCode = "MANAGER", FullName = "Manager", Description = "Organization manager" },
            new OrganizationPosition { Id = (int)OrganizationPositionEnum.Teacher, MasterCode = "TEACHER", FullName = "Teacher", Description = "Teacher or instructor" },
            new OrganizationPosition { Id = (int)OrganizationPositionEnum.Secretary, MasterCode = "SECRETARY", FullName = "Secretary", Description = "Organization secretary" },
            new OrganizationPosition { Id = (int)OrganizationPositionEnum.Treasurer, MasterCode = "TREASURER", FullName = "Treasurer", Description = "Organization treasurer" },
            new OrganizationPosition { Id = (int)OrganizationPositionEnum.Coordinator, MasterCode = "COORDINATOR", FullName = "Coordinator", Description = "Event or activity coordinator" },
            new OrganizationPosition { Id = (int)OrganizationPositionEnum.Volunteer, MasterCode = "VOLUNTEER", FullName = "Volunteer", Description = "Organization volunteer" },
            new OrganizationPosition { Id = (int)OrganizationPositionEnum.Intern, MasterCode = "INTERN", FullName = "Intern", Description = "Organization intern" },
            new OrganizationPosition { Id = (int)OrganizationPositionEnum.Advisor, MasterCode = "ADVISOR", FullName = "Advisor", Description = "Organization advisor" },
            new OrganizationPosition { Id = (int)OrganizationPositionEnum.Consultant, MasterCode = "CONSULTANT", FullName = "Consultant", Description = "Organization consultant" },
            new OrganizationPosition { Id = (int)OrganizationPositionEnum.Supervisor, MasterCode = "SUPERVISOR", FullName = "Supervisor", Description = "Supervisor" },
            new OrganizationPosition { Id = (int)OrganizationPositionEnum.Assistant, MasterCode = "ASSISTANT", FullName = "Assistant", Description = "Assistant" },
            new OrganizationPosition { Id = (int)OrganizationPositionEnum.Staff, MasterCode = "STAFF", FullName = "Staff", Description = "General staff member" });
        await context.SaveChangesAsync(ct);
    }

    private static async Task SeedOrganizationRolesAsync(ExploreDbContext context, CancellationToken ct)
    {
        if (await context.Set<OrganizationRole>().AnyAsync(ct)) return;

        context.Set<OrganizationRole>().AddRange(
            new OrganizationRole { Id = (int)OrganizationRoleEnum.Creator, MasterCode = "CREATOR", FullName = "Creator", Description = "Organization creator with full ownership" },
            new OrganizationRole { Id = (int)OrganizationRoleEnum.CoOwner, MasterCode = "CO_OWNER", FullName = "Co-Owner", Description = "Co-owner with near-full access" },
            new OrganizationRole { Id = (int)OrganizationRoleEnum.Admin, MasterCode = "ADMIN", FullName = "Administrator", Description = "Organization Administrator with management access" },
            new OrganizationRole { Id = (int)OrganizationRoleEnum.Moderator, MasterCode = "MODERATOR", FullName = "Moderator", Description = "Organization Moderator with limited access" },
            new OrganizationRole { Id = (int)OrganizationRoleEnum.Member, MasterCode = "MEMBER", FullName = "Member", Description = "Regular organization member" },
            new OrganizationRole { Id = (int)OrganizationRoleEnum.Viewer, MasterCode = "VIEWER", FullName = "Viewer", Description = "Read-only access to organization" });
        await context.SaveChangesAsync(ct);
    }

    private static async Task SeedRegistrationModesAsync(ExploreDbContext context, CancellationToken ct)
    {
        if (await context.Set<RegistrationMode>().AnyAsync(ct)) return;

        context.Set<RegistrationMode>().AddRange(
            new RegistrationMode { Id = (int)RegistrationModeEnum.Open, MasterCode = "OPEN", FullName = "Open", Description = "Anyone can register" },
            new RegistrationMode { Id = (int)RegistrationModeEnum.ApprovalRequired, MasterCode = "APPROVAL_REQUIRED", FullName = "Approval Required", Description = "Registration requires approval" },
            new RegistrationMode { Id = (int)RegistrationModeEnum.InviteOnly, MasterCode = "INVITE_ONLY", FullName = "Invite Only", Description = "Only invited users can register" },
            new RegistrationMode { Id = (int)RegistrationModeEnum.Closed, MasterCode = "CLOSED", FullName = "Closed", Description = "Registration is closed" });
        await context.SaveChangesAsync(ct);
    }

    private static async Task SeedSystemSettingsAsync(ExploreDbContext context, CancellationToken ct)
    {
        var seedTimestamp = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        var expectedSettings = new[]
        {
            new SystemSetting { Id = SeedIds.SystemSettingDeploymentModeId, SettingKey = GovernanceSettingKeys.DeploymentMode, Value = "\"MultiTenant\"", ValueType = SettingValueType.String, IsLocked = true, AllowedValues = "[\"SingleTenant\", \"MultiTenant\"]", Description = "Deployment mode of the application", Category = "System", DisplayOrder = 1, CreatedAt = seedTimestamp },
            new SystemSetting { Id = SeedIds.SystemSettingMaxSessionsPerEventId, SettingKey = "events.max_sessions_per_event", Value = "100", ValueType = SettingValueType.Integer, IsLocked = false, Description = "Maximum number of sessions allowed per event", Category = "Events", DisplayOrder = 1, CreatedAt = seedTimestamp },
            new SystemSetting { Id = SeedIds.SystemSettingRequireApprovalId, SettingKey = "events.require_approval", Value = "false", ValueType = SettingValueType.Boolean, IsLocked = false, Description = "Whether events require admin approval before publishing", Category = "Events", DisplayOrder = 2, CreatedAt = seedTimestamp },
            new SystemSetting { Id = SeedIds.SystemSettingIslamicModuleId, SettingKey = GovernanceSettingKeys.ModulesIslamicEnabled, Value = "true", ValueType = SettingValueType.Boolean, IsLocked = false, Description = "Enable Islamic event module", Category = "Modules", DisplayOrder = 1, CreatedAt = seedTimestamp },
            new SystemSetting { Id = SeedIds.SystemSettingTechModuleId, SettingKey = GovernanceSettingKeys.ModulesTechEnabled, Value = "true", ValueType = SettingValueType.Boolean, IsLocked = false, Description = "Enable Tech event module", Category = "Modules", DisplayOrder = 2, CreatedAt = seedTimestamp },
            new SystemSetting { Id = SeedIds.SystemSettingTenantSelfServiceRegistrationId, SettingKey = GovernanceSettingKeys.TenantSelfServiceRegistration, Value = "false", ValueType = SettingValueType.Boolean, IsLocked = false, Description = "Whether tenants can self-register without manual instance admin invitation", Category = "Tenant", DisplayOrder = 1, CreatedAt = seedTimestamp },
            new SystemSetting { Id = SeedIds.SystemSettingRoutingDefaultPublicHomePageId, SettingKey = GovernanceSettingKeys.RoutingDefaultPublicHomePage, Value = "\"EventList\"", ValueType = SettingValueType.String, IsLocked = false, AllowedValues = "[\"EventList\", \"LandingPage\"]", Description = "Default public home page for tenants", Category = "Routing", DisplayOrder = 1, CreatedAt = seedTimestamp },
            new SystemSetting { Id = SeedIds.SystemSettingUserSubmissionEnabledId, SettingKey = GovernanceSettingKeys.EventsUserSubmissionEnabled, Value = "true", ValueType = SettingValueType.Boolean, IsLocked = false, Description = "Whether tenant users are allowed to submit events", Category = "Events", DisplayOrder = 3, CreatedAt = seedTimestamp },
            new SystemSetting { Id = SeedIds.SystemSettingOrganizationVerificationRequiredId, SettingKey = GovernanceSettingKeys.OrganizationsVerificationRequired, Value = "true", ValueType = SettingValueType.Boolean, IsLocked = false, Description = "Whether organization verification is required before organizations can operate", Category = "Organizations", DisplayOrder = 1, CreatedAt = seedTimestamp },
            new SystemSetting { Id = SeedIds.SystemSettingOrganizationTenantCanOmitVerificationId, SettingKey = GovernanceSettingKeys.OrganizationsTenantCanOmitVerification, Value = "false", ValueType = SettingValueType.Boolean, IsLocked = false, Description = "Whether tenant administrators may omit organization verification requirements", Category = "Organizations", DisplayOrder = 2, CreatedAt = seedTimestamp },
            new SystemSetting { Id = SeedIds.SystemSettingDomainsInstanceBaseDomainId, SettingKey = GovernanceSettingKeys.DomainsInstanceBaseDomain, Value = "\"\"", ValueType = SettingValueType.String, IsLocked = false, Description = "Instance base domain used for tenant subdomain generation", Category = "Domains", DisplayOrder = 1, CreatedAt = seedTimestamp },
            new SystemSetting { Id = SeedIds.SystemSettingDomainsAllowTenantCustomDomainId, SettingKey = GovernanceSettingKeys.DomainsAllowTenantCustomDomain, Value = "true", ValueType = SettingValueType.Boolean, IsLocked = false, Description = "Whether tenant administrators can configure custom domains", Category = "Domains", DisplayOrder = 2, CreatedAt = seedTimestamp },
            new SystemSetting { Id = SeedIds.SystemSettingDomainsTenantSubdomainId, SettingKey = GovernanceSettingKeys.DomainsTenantSubdomain, Value = "\"\"", ValueType = SettingValueType.String, IsLocked = false, Description = "Tenant subdomain override placeholder", Category = "Domains", DisplayOrder = 3, CreatedAt = seedTimestamp },
            new SystemSetting { Id = SeedIds.SystemSettingDomainsTenantCustomDomainId, SettingKey = GovernanceSettingKeys.DomainsTenantCustomDomain, Value = "\"\"", ValueType = SettingValueType.String, IsLocked = false, Description = "Tenant custom domain override placeholder", Category = "Domains", DisplayOrder = 4, CreatedAt = seedTimestamp },
            new SystemSetting { Id = SeedIds.SystemSettingBrandingDisplayNameId, SettingKey = GovernanceSettingKeys.BrandingDisplayName, Value = "\"ISLAMU Explore\"", ValueType = SettingValueType.String, IsLocked = false, Description = "Default brand display name shown when tenants do not override branding", Category = "Branding", DisplayOrder = 1, CreatedAt = seedTimestamp },
            new SystemSetting { Id = SeedIds.SystemSettingBrandingLogoUrlId, SettingKey = GovernanceSettingKeys.BrandingLogoUrl, Value = "\"\"", ValueType = SettingValueType.String, IsLocked = false, Description = "Default logo URL shown when tenants do not override branding", Category = "Branding", DisplayOrder = 2, CreatedAt = seedTimestamp },
            new SystemSetting { Id = SeedIds.SystemSettingBrandingFaviconUrlId, SettingKey = GovernanceSettingKeys.BrandingFaviconUrl, Value = "\"\"", ValueType = SettingValueType.String, IsLocked = false, Description = "Default favicon URL shown when tenants do not override branding", Category = "Branding", DisplayOrder = 3, CreatedAt = seedTimestamp },
            new SystemSetting { Id = SeedIds.SystemSettingBrandingCustomCssUrlId, SettingKey = GovernanceSettingKeys.BrandingCustomCssUrl, Value = "\"\"", ValueType = SettingValueType.String, IsLocked = false, Description = "Default custom CSS URL applied when tenants do not override branding", Category = "Branding", DisplayOrder = 4, CreatedAt = seedTimestamp }
        };

        var existingIds = await context.Set<SystemSetting>()
            .AsNoTracking()
            .Select(x => x.Id)
            .ToListAsync(ct);

        var existingIdSet = existingIds.ToHashSet();
        var missingSettings = expectedSettings
            .Where(x => !existingIdSet.Contains(x.Id))
            .ToList();

        if (missingSettings.Count == 0)
        {
            return;
        }

        context.Set<SystemSetting>().AddRange(missingSettings);
        await context.SaveChangesAsync(ct);
    }

    private static async Task SeedTagTypesAsync(ExploreDbContext context, CancellationToken ct)
    {
        if (await context.Set<TagType>().AnyAsync(ct)) return;

        context.Set<TagType>().AddRange(
            new TagType { Id = 1, MasterCode = "TOPIC", FullName = "Topic", Description = "Topic-based tags for content categorization" },
            new TagType { Id = 2, MasterCode = "SKILL", FullName = "Skill Level", Description = "Skill level requirements (beginner, intermediate, advanced)" },
            new TagType { Id = 3, MasterCode = "LANGUAGE", FullName = "Language", Description = "Language-based tags" },
            new TagType { Id = 4, MasterCode = "AUDIENCE", FullName = "Audience", Description = "Target audience tags" });
        await context.SaveChangesAsync(ct);
    }

    private static async Task SeedTenantAdministratorRolesAsync(ExploreDbContext context, CancellationToken ct)
    {
        var expectedRoles = new[]
        {
            new TenantAdministratorRole { Id = (int)TenantAdministratorRoleEnum.TenantOwner, FullName = "Tenant Owner", MasterCode = "TENANT_OWNER", Description = "Owns tenant-level governance and lifecycle actions." },
            new TenantAdministratorRole { Id = (int)TenantAdministratorRoleEnum.TenantAdmin, FullName = "Tenant Administrator", MasterCode = "TENANT_ADMIN", Description = "Manages tenant policies, moderation, and delegated controls." },
            new TenantAdministratorRole { Id = (int)TenantAdministratorRoleEnum.TenantModerator, FullName = "Tenant Moderator", MasterCode = "TENANT_MODERATOR", Description = "Moderates tenant content based on delegated permissions." }
        };

        var existingIds = await context.Set<TenantAdministratorRole>()
            .AsNoTracking()
            .Select(x => x.Id)
            .ToListAsync(ct);

        var existingIdSet = existingIds.ToHashSet();
        var missingRoles = expectedRoles
            .Where(x => !existingIdSet.Contains(x.Id))
            .ToList();

        if (missingRoles.Count == 0)
        {
            return;
        }

        context.Set<TenantAdministratorRole>().AddRange(missingRoles);
        await context.SaveChangesAsync(ct);
    }

    private static async Task SeedVisibilityTypesAsync(ExploreDbContext context, CancellationToken ct)
    {
        if (await context.Set<VisibilityType>().AnyAsync(ct)) return;

        context.Set<VisibilityType>().AddRange(
            new VisibilityType { Id = (int)VisibilityTypeEnum.Public, MasterCode = "PUBLIC", FullName = "Public", Description = "Visible to everyone" },
            new VisibilityType { Id = (int)VisibilityTypeEnum.Private, MasterCode = "PRIVATE", FullName = "Private", Description = "Only visible to invited members" },
            new VisibilityType { Id = (int)VisibilityTypeEnum.Unlisted, MasterCode = "UNLISTED", FullName = "Unlisted", Description = "Not listed publicly but accessible via direct link" },
            new VisibilityType { Id = (int)VisibilityTypeEnum.MembersOnly, MasterCode = "MEMBERS_ONLY", FullName = "Members Only", Description = "Only visible to organization members" });
        await context.SaveChangesAsync(ct);
    }
}
