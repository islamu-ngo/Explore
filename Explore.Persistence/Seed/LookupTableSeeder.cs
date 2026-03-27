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
        await SeedAnalyticsProvidersAsync(context, cancellationToken);
        await SeedTenantStatusesAsync(context, cancellationToken);
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
        await SeedGroupPositionsAsync(context, cancellationToken);
        await SeedRegistrationModesAsync(context, cancellationToken);
        await SeedRolesAsync(context, cancellationToken);
        await SeedSystemSettingsAsync(context, cancellationToken);
        await SeedTagTypesAsync(context, cancellationToken);
        await SeedVisibilityTypesAsync(context, cancellationToken);
        await SeedPermissionsAsync(context, cancellationToken);
        await SeedNotificationTypesAsync(context, cancellationToken);
        await SeedNotificationEntityTypesAsync(context, cancellationToken);
        await SeedDefaultFooterLinkGroupsAsync(context, cancellationToken);
        await SeedExternalApiKeyStatusesAsync(context, cancellationToken);
        await SeedExternalApiKeyCreditPeriodsAsync(context, cancellationToken);
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

    private static async Task SeedAnalyticsProvidersAsync(ExploreDbContext context, CancellationToken ct)
    {
        if (await context.Set<AnalyticsProvider>().AnyAsync(ct)) return;

        context.Set<AnalyticsProvider>().AddRange(
            new AnalyticsProvider { Id = (int)AnalyticsProviderEnum.None, MasterCode = "NONE", FullName = "None", Description = "Analytics disabled" },
            new AnalyticsProvider { Id = (int)AnalyticsProviderEnum.Posthog, MasterCode = "POSTHOG", FullName = "PostHog", Description = "PostHog analytics provider" },
            new AnalyticsProvider { Id = (int)AnalyticsProviderEnum.Plausible, MasterCode = "PLAUSIBLE", FullName = "Plausible", Description = "Plausible analytics provider" },
            new AnalyticsProvider { Id = (int)AnalyticsProviderEnum.Rybbit, MasterCode = "RYBBIT", FullName = "Rybbit", Description = "Rybbit analytics provider" },
            new AnalyticsProvider { Id = (int)AnalyticsProviderEnum.RudderStack, MasterCode = "RUDDERSTACK", FullName = "RudderStack", Description = "RudderStack analytics provider" });
        await context.SaveChangesAsync(ct);
    }

    private static async Task SeedTenantStatusesAsync(ExploreDbContext context, CancellationToken ct)
    {
        if (await context.Set<TenantStatus>().AnyAsync(ct)) return;

        context.Set<TenantStatus>().AddRange(
            new TenantStatus { Id = (int)TenantStatusEnum.Provisioning, MasterCode = "PROVISIONING", FullName = "Provisioning", Description = "Tenant is being set up", IsActiveState = false },
            new TenantStatus { Id = (int)TenantStatusEnum.Active, MasterCode = "ACTIVE", FullName = "Active", Description = "Tenant is active and operational", IsActiveState = true },
            new TenantStatus { Id = (int)TenantStatusEnum.Suspended, MasterCode = "SUSPENDED", FullName = "Suspended", Description = "Tenant is temporarily suspended", IsActiveState = false },
            new TenantStatus { Id = (int)TenantStatusEnum.Archived, MasterCode = "ARCHIVED", FullName = "Archived", Description = "Tenant is archived and read-only", IsActiveState = false },
            new TenantStatus { Id = (int)TenantStatusEnum.Purged, MasterCode = "PURGED", FullName = "Purged", Description = "Tenant data has been permanently removed", IsActiveState = false });
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

    private static async Task SeedGroupPositionsAsync(ExploreDbContext context, CancellationToken ct)
    {
        if (await context.Set<GroupPosition>().AnyAsync(ct)) return;

        context.Set<GroupPosition>().AddRange(
            new GroupPosition { Id = (int)GroupPositionEnum.Leader, MasterCode = "LEADER", FullName = "Leader", Description = "Group leader" },
            new GroupPosition { Id = (int)GroupPositionEnum.CoLeader, MasterCode = "CO_LEADER", FullName = "Co-Leader", Description = "Group co-leader" },
            new GroupPosition { Id = (int)GroupPositionEnum.Coordinator, MasterCode = "COORDINATOR", FullName = "Coordinator", Description = "Group coordinator" },
            new GroupPosition { Id = (int)GroupPositionEnum.Moderator, MasterCode = "MODERATOR", FullName = "Moderator", Description = "Group moderator" },
            new GroupPosition { Id = (int)GroupPositionEnum.Secretary, MasterCode = "SECRETARY", FullName = "Secretary", Description = "Group secretary" },
            new GroupPosition { Id = (int)GroupPositionEnum.Treasurer, MasterCode = "TREASURER", FullName = "Treasurer", Description = "Group treasurer" },
            new GroupPosition { Id = (int)GroupPositionEnum.Mentor, MasterCode = "MENTOR", FullName = "Mentor", Description = "Group mentor" },
            new GroupPosition { Id = (int)GroupPositionEnum.Facilitator, MasterCode = "FACILITATOR", FullName = "Facilitator", Description = "Group facilitator" },
            new GroupPosition { Id = (int)GroupPositionEnum.Volunteer, MasterCode = "VOLUNTEER", FullName = "Volunteer", Description = "Group volunteer" },
            new GroupPosition { Id = (int)GroupPositionEnum.Member, MasterCode = "MEMBER", FullName = "Member", Description = "General group member" });
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
            new SystemSetting { Id = SeedIds.SystemSettingDeploymentModeId, SettingKey = GovernanceSettingKeys.Deployment.Mode, Value = "\"MultiTenant\"", ValueType = SettingValueType.String, IsLocked = true, AllowedValues = "[\"SingleTenant\", \"MultiTenant\"]", Description = "Deployment mode of the application", Category = "System", DisplayOrder = 1, CreatedAt = seedTimestamp },
            new SystemSetting { Id = SeedIds.SystemSettingMaxSessionsPerEventId, SettingKey = "events.max_sessions_per_event", Value = "100", ValueType = SettingValueType.Integer, IsLocked = false, Description = "Maximum number of sessions allowed per event", Category = "Events", DisplayOrder = 1, CreatedAt = seedTimestamp },
            new SystemSetting { Id = SeedIds.SystemSettingRequireApprovalId, SettingKey = "events.require_approval", Value = "false", ValueType = SettingValueType.Boolean, IsLocked = false, Description = "Whether events require admin approval before publishing", Category = "Events", DisplayOrder = 2, CreatedAt = seedTimestamp },
            new SystemSetting { Id = SeedIds.SystemSettingIslamicModuleId, SettingKey = GovernanceSettingKeys.Modules.IslamicEnabled, Value = "true", ValueType = SettingValueType.Boolean, IsLocked = false, Description = "Enable Islamic event module", Category = "Modules", DisplayOrder = 1, CreatedAt = seedTimestamp },
            new SystemSetting { Id = SeedIds.SystemSettingTechModuleId, SettingKey = GovernanceSettingKeys.Modules.TechEnabled, Value = "true", ValueType = SettingValueType.Boolean, IsLocked = false, Description = "Enable Tech event module", Category = "Modules", DisplayOrder = 2, CreatedAt = seedTimestamp },
            new SystemSetting { Id = SeedIds.SystemSettingTenantSelfServiceRegistrationId, SettingKey = GovernanceSettingKeys.Tenants.SelfServiceRegistration, Value = "false", ValueType = SettingValueType.Boolean, IsLocked = false, Description = "Whether tenants can self-register without manual instance admin invitation", Category = "Tenant", DisplayOrder = 1, CreatedAt = seedTimestamp },
            new SystemSetting { Id = SeedIds.SystemSettingTenantWhiteLabelingEnabledId, SettingKey = GovernanceSettingKeys.Tenants.WhiteLabelingEnabled, Value = "false", ValueType = SettingValueType.Boolean, IsLocked = false, Description = "Whether tenant-level white-label branding overrides are enabled in multi-tenant mode", Category = "Tenant", DisplayOrder = 2, CreatedAt = seedTimestamp },
            new SystemSetting { Id = SeedIds.SystemSettingRoutingDefaultPublicHomePageId, SettingKey = GovernanceSettingKeys.Routing.DefaultPublicHomePage, Value = "\"EventList\"", ValueType = SettingValueType.String, IsLocked = false, AllowedValues = "[\"EventList\", \"LandingPage\"]", Description = "Default public home page for tenants", Category = "Routing", DisplayOrder = 1, CreatedAt = seedTimestamp },
            new SystemSetting { Id = SeedIds.SystemSettingUserSubmissionEnabledId, SettingKey = GovernanceSettingKeys.Events.UserSubmissionEnabled, Value = "true", ValueType = SettingValueType.Boolean, IsLocked = false, Description = "Whether tenant users are allowed to submit events", Category = "Events", DisplayOrder = 3, CreatedAt = seedTimestamp },
            new SystemSetting { Id = SeedIds.SystemSettingOrganizationVerificationRequiredId, SettingKey = GovernanceSettingKeys.Organizations.VerificationRequired, Value = "true", ValueType = SettingValueType.Boolean, IsLocked = false, Description = "Whether organization verification is required before organizations can operate", Category = "Organizations", DisplayOrder = 1, CreatedAt = seedTimestamp },
            new SystemSetting { Id = SeedIds.SystemSettingOrganizationTenantCanOmitVerificationId, SettingKey = GovernanceSettingKeys.Organizations.TenantCanOmitVerification, Value = "false", ValueType = SettingValueType.Boolean, IsLocked = false, Description = "Whether tenant administrators may omit organization verification requirements", Category = "Organizations", DisplayOrder = 2, CreatedAt = seedTimestamp },
            new SystemSetting { Id = SeedIds.SystemSettingOrgSubmissionEnabledId, SettingKey = GovernanceSettingKeys.Events.OrganizationSubmissionEnabled, Value = "true", ValueType = SettingValueType.Boolean, IsLocked = false, Description = "Whether organizations are allowed to submit events", Category = "Events", DisplayOrder = 4, CreatedAt = seedTimestamp },
            new SystemSetting { Id = SeedIds.SystemSettingGroupSubmissionEnabledId, SettingKey = GovernanceSettingKeys.Events.GroupSubmissionEnabled, Value = "true", ValueType = SettingValueType.Boolean, IsLocked = false, Description = "Whether groups are allowed to submit events", Category = "Events", DisplayOrder = 5, CreatedAt = seedTimestamp },
            new SystemSetting { Id = SeedIds.SystemSettingOrgSelfRegistrationEnabledId, SettingKey = GovernanceSettingKeys.Organizations.SelfRegistrationEnabled, Value = "true", ValueType = SettingValueType.Boolean, IsLocked = false, Description = "Whether users can self-register organizations", Category = "Organizations", DisplayOrder = 3, CreatedAt = seedTimestamp },
            new SystemSetting { Id = SeedIds.SystemSettingGroupSelfRegistrationEnabledId, SettingKey = GovernanceSettingKeys.Groups.SelfRegistrationEnabled, Value = "true", ValueType = SettingValueType.Boolean, IsLocked = false, Description = "Whether users can self-register groups", Category = "Groups", DisplayOrder = 1, CreatedAt = seedTimestamp },
            new SystemSetting { Id = SeedIds.SystemSettingDomainsInstanceBaseDomainId, SettingKey = GovernanceSettingKeys.Domains.InstanceBaseDomain, Value = "\"\"", ValueType = SettingValueType.String, IsLocked = false, Description = "Instance base domain used for tenant subdomain generation", Category = "Domains", DisplayOrder = 1, CreatedAt = seedTimestamp },
            new SystemSetting { Id = SeedIds.SystemSettingDomainsAllowTenantCustomDomainId, SettingKey = GovernanceSettingKeys.Domains.AllowTenantCustomDomain, Value = "true", ValueType = SettingValueType.Boolean, IsLocked = false, Description = "Whether tenant administrators can configure custom domains", Category = "Domains", DisplayOrder = 2, CreatedAt = seedTimestamp },
            new SystemSetting { Id = SeedIds.SystemSettingDomainsTenantSubdomainId, SettingKey = GovernanceSettingKeys.Domains.TenantSubdomain, Value = "\"\"", ValueType = SettingValueType.String, IsLocked = false, Description = "Tenant subdomain override placeholder", Category = "Domains", DisplayOrder = 3, CreatedAt = seedTimestamp },
            new SystemSetting { Id = SeedIds.SystemSettingDomainsTenantCustomDomainId, SettingKey = GovernanceSettingKeys.Domains.TenantCustomDomain, Value = "\"\"", ValueType = SettingValueType.String, IsLocked = false, Description = "Tenant custom domain override placeholder", Category = "Domains", DisplayOrder = 4, CreatedAt = seedTimestamp },
            new SystemSetting { Id = SeedIds.SystemSettingBrandingDisplayNameId, SettingKey = GovernanceSettingKeys.Branding.DisplayName, Value = "\"\"", ValueType = SettingValueType.String, IsLocked = false, Description = "Default brand display name shown when tenants do not override branding", Category = "Branding", DisplayOrder = 1, CreatedAt = seedTimestamp },
            new SystemSetting { Id = SeedIds.SystemSettingBrandingLogoUrlId, SettingKey = GovernanceSettingKeys.Branding.LogoUrl, Value = "\"\"", ValueType = SettingValueType.String, IsLocked = false, Description = "Default logo URL shown when tenants do not override branding", Category = "Branding", DisplayOrder = 2, CreatedAt = seedTimestamp },
            new SystemSetting { Id = SeedIds.SystemSettingBrandingFaviconUrlId, SettingKey = GovernanceSettingKeys.Branding.FaviconUrl, Value = "\"\"", ValueType = SettingValueType.String, IsLocked = false, Description = "Default favicon URL shown when tenants do not override branding", Category = "Branding", DisplayOrder = 3, CreatedAt = seedTimestamp },
            new SystemSetting { Id = SeedIds.SystemSettingBrandingCustomCssUrlId, SettingKey = GovernanceSettingKeys.Branding.CustomCssUrl, Value = "\"\"", ValueType = SettingValueType.String, IsLocked = false, Description = "Default custom CSS URL applied when tenants do not override branding", Category = "Branding", DisplayOrder = 4, CreatedAt = seedTimestamp },

            // Email / SMTP settings — unlocked by default so tenants can bring their own SMTP
            new SystemSetting { Id = SeedIds.SystemSettingEmailSmtpHostId, SettingKey = GovernanceSettingKeys.Email.SmtpHost, Value = "\"\"", ValueType = SettingValueType.String, IsLocked = false, Description = "SMTP server hostname (e.g., smtp.gmail.com, smtp.mailgun.org)", Category = "Email", DisplayOrder = 1, CreatedAt = seedTimestamp },
            new SystemSetting { Id = SeedIds.SystemSettingEmailSmtpPortId, SettingKey = GovernanceSettingKeys.Email.SmtpPort, Value = "587", ValueType = SettingValueType.Integer, IsLocked = false, Description = "SMTP server port (587 for StartTLS, 465 for SSL, 25 for unencrypted)", Category = "Email", DisplayOrder = 2, CreatedAt = seedTimestamp },
            new SystemSetting { Id = SeedIds.SystemSettingEmailSmtpUsernameId, SettingKey = InfrastructureSecretSettingKeys.Email.SmtpUsername, Value = "\"\"", ValueType = SettingValueType.String, IsLocked = false, Description = "SMTP authentication username", Category = "Email", DisplayOrder = 3, CreatedAt = seedTimestamp },
            new SystemSetting { Id = SeedIds.SystemSettingEmailSmtpPasswordId, SettingKey = InfrastructureSecretSettingKeys.Email.SmtpPassword, Value = "\"\"", ValueType = SettingValueType.String, IsLocked = false, Description = "SMTP authentication password (stored encrypted)", Category = "Email", DisplayOrder = 4, CreatedAt = seedTimestamp },
            new SystemSetting { Id = SeedIds.SystemSettingEmailSmtpSecurityId, SettingKey = GovernanceSettingKeys.Email.SmtpSecurity, Value = "\"StartTls\"", ValueType = SettingValueType.String, IsLocked = false, AllowedValues = "[\"None\", \"StartTls\", \"SslOnConnect\", \"Auto\"]", Description = "SMTP connection security mode", Category = "Email", DisplayOrder = 5, CreatedAt = seedTimestamp },
            new SystemSetting { Id = SeedIds.SystemSettingEmailFromAddressId, SettingKey = GovernanceSettingKeys.Email.FromAddress, Value = "\"\"", ValueType = SettingValueType.String, IsLocked = false, Description = "Default sender email address for outbound emails", Category = "Email", DisplayOrder = 6, CreatedAt = seedTimestamp },
            new SystemSetting { Id = SeedIds.SystemSettingEmailFromNameId, SettingKey = GovernanceSettingKeys.Email.FromName, Value = "\"Explore\"", ValueType = SettingValueType.String, IsLocked = false, Description = "Default sender display name for outbound emails", Category = "Email", DisplayOrder = 7, CreatedAt = seedTimestamp },
            new SystemSetting { Id = SeedIds.SystemSettingEmailSmtpTimeoutId, SettingKey = GovernanceSettingKeys.Email.SmtpTimeoutSeconds, Value = "30", ValueType = SettingValueType.Integer, IsLocked = false, Description = "SMTP connection timeout in seconds", Category = "Email", DisplayOrder = 8, CreatedAt = seedTimestamp },
            new SystemSetting { Id = SeedIds.SystemSettingEmailSmtpSkipCertValidationId, SettingKey = GovernanceSettingKeys.Email.SmtpSkipCertValidation, Value = "false", ValueType = SettingValueType.Boolean, IsLocked = false, Description = "Skip TLS certificate validation (development/self-signed certs only)", Category = "Email", DisplayOrder = 9, CreatedAt = seedTimestamp },

            // Object Storage / S3
            new SystemSetting { Id = SeedIds.SystemSettingS3EndpointId, SettingKey = GovernanceSettingKeys.Storage.Endpoint, Value = "\"\"", ValueType = SettingValueType.String, IsLocked = false, Description = "S3-compatible endpoint URL (e.g., https://fsn1.your-objectstorage.com)", Category = "ObjectStorage", DisplayOrder = 1, CreatedAt = seedTimestamp },
            new SystemSetting { Id = SeedIds.SystemSettingS3PublicEndpointId, SettingKey = GovernanceSettingKeys.Storage.PublicEndpoint, Value = "\"\"", ValueType = SettingValueType.String, IsLocked = false, Description = "Public endpoint for presigned URLs (if different from internal endpoint)", Category = "ObjectStorage", DisplayOrder = 2, CreatedAt = seedTimestamp },
            new SystemSetting { Id = SeedIds.SystemSettingS3BucketNameId, SettingKey = GovernanceSettingKeys.Storage.BucketName, Value = "\"\"", ValueType = SettingValueType.String, IsLocked = false, Description = "S3 bucket name for object storage", Category = "ObjectStorage", DisplayOrder = 3, CreatedAt = seedTimestamp },
            new SystemSetting { Id = SeedIds.SystemSettingS3AccessKeyIdId, SettingKey = InfrastructureSecretSettingKeys.Storage.AccessKeyId, Value = "\"\"", ValueType = SettingValueType.String, IsLocked = false, Description = "S3 access key ID for authentication", Category = "ObjectStorage", DisplayOrder = 4, CreatedAt = seedTimestamp },
            new SystemSetting { Id = SeedIds.SystemSettingS3SecretAccessKeyId, SettingKey = InfrastructureSecretSettingKeys.Storage.SecretAccessKey, Value = "\"\"", ValueType = SettingValueType.String, IsLocked = false, Description = "S3 secret access key for authentication (stored encrypted)", Category = "ObjectStorage", DisplayOrder = 5, CreatedAt = seedTimestamp },
            new SystemSetting { Id = SeedIds.SystemSettingS3RegionId, SettingKey = GovernanceSettingKeys.Storage.Region, Value = "\"fsn1\"", ValueType = SettingValueType.String, IsLocked = false, Description = "S3 region identifier (e.g., fsn1 for Hetzner, us-east-1 for AWS)", Category = "ObjectStorage", DisplayOrder = 6, CreatedAt = seedTimestamp },
            new SystemSetting { Id = SeedIds.SystemSettingS3ForcePathStyleId, SettingKey = GovernanceSettingKeys.Storage.ForcePathStyle, Value = "true", ValueType = SettingValueType.Boolean, IsLocked = false, Description = "Use path-style URLs (required by most non-AWS S3 providers)", Category = "ObjectStorage", DisplayOrder = 7, CreatedAt = seedTimestamp },
            new SystemSetting { Id = SeedIds.SystemSettingS3UploadUrlExpirationMinutesId, SettingKey = GovernanceSettingKeys.Storage.UploadUrlExpirationMinutes, Value = "60", ValueType = SettingValueType.Integer, IsLocked = false, Description = "Presigned upload URL expiration time in minutes", Category = "ObjectStorage", DisplayOrder = 8, CreatedAt = seedTimestamp },

            // Analytics
            new SystemSetting { Id = SeedIds.SystemSettingAnalyticsProviderId, SettingKey = GovernanceSettingKeys.Analytics.Provider, Value = "\"none\"", ValueType = SettingValueType.String, IsLocked = false, Description = "Analytics provider (none, posthog, plausible, rybbit, rudderstack)", AllowedValues = "[\"none\",\"posthog\",\"plausible\",\"rybbit\",\"rudderstack\"]", Category = "Analytics", DisplayOrder = 1, CreatedAt = seedTimestamp },
            new SystemSetting { Id = SeedIds.SystemSettingAnalyticsEnabledId, SettingKey = GovernanceSettingKeys.Analytics.Enabled, Value = "false", ValueType = SettingValueType.Boolean, IsLocked = false, Description = "Enable analytics tracking", Category = "Analytics", DisplayOrder = 2, CreatedAt = seedTimestamp },
            new SystemSetting { Id = SeedIds.SystemSettingAnalyticsApiKeyId, SettingKey = GovernanceSettingKeys.Analytics.ApiKey, Value = "\"\"", ValueType = SettingValueType.String, IsLocked = false, Description = "Analytics provider public/write API key", Category = "Analytics", DisplayOrder = 3, CreatedAt = seedTimestamp },
            new SystemSetting { Id = SeedIds.SystemSettingAnalyticsEndpointUrlId, SettingKey = GovernanceSettingKeys.Analytics.EndpointUrl, Value = "\"\"", ValueType = SettingValueType.String, IsLocked = false, Description = "Analytics provider endpoint URL (supports self-hosted deployments)", Category = "Analytics", DisplayOrder = 4, CreatedAt = seedTimestamp },
            new SystemSetting { Id = SeedIds.SystemSettingAnalyticsPersonalApiKeyId, SettingKey = GovernanceSettingKeys.Analytics.PersonalApiKey, Value = "\"\"", ValueType = SettingValueType.String, IsLocked = false, Description = "Personal API key used for analytics feature flag evaluation when supported", Category = "Analytics", DisplayOrder = 5, CreatedAt = seedTimestamp },

            // Localization / TMS
            new SystemSetting { Id = SeedIds.SystemSettingLocalizationDefaultLanguageId, SettingKey = GovernanceSettingKeys.Localization.DefaultLanguage, Value = "\"en\"", ValueType = SettingValueType.String, IsLocked = false, Description = "Default language code (ISO 639-1) for the instance", Category = "Localization", DisplayOrder = 1, CreatedAt = seedTimestamp },
            new SystemSetting { Id = SeedIds.SystemSettingLocalizationTmsProviderId, SettingKey = GovernanceSettingKeys.Localization.TmsProvider, Value = "\"none\"", ValueType = SettingValueType.String, IsLocked = false, AllowedValues = "[\"none\",\"tolgee\",\"weblate\"]", Description = "Translation Management System provider (none uses offline bundles)", Category = "Localization", DisplayOrder = 2, CreatedAt = seedTimestamp },
            new SystemSetting { Id = SeedIds.SystemSettingLocalizationTmsApiUrlId, SettingKey = GovernanceSettingKeys.Localization.TmsApiUrl, Value = "\"\"", ValueType = SettingValueType.String, IsLocked = false, Description = "TMS API base URL (e.g., https://app.tolgee.io or self-hosted URL)", Category = "Localization", DisplayOrder = 3, CreatedAt = seedTimestamp },
            new SystemSetting { Id = SeedIds.SystemSettingLocalizationTmsProjectIdId, SettingKey = GovernanceSettingKeys.Localization.TmsProjectId, Value = "\"\"", ValueType = SettingValueType.String, IsLocked = false, Description = "TMS project identifier", Category = "Localization", DisplayOrder = 4, CreatedAt = seedTimestamp },
            new SystemSetting { Id = SeedIds.SystemSettingLocalizationTmsComponentId, SettingKey = GovernanceSettingKeys.Localization.TmsComponent, Value = "\"\"", ValueType = SettingValueType.String, IsLocked = false, Description = "Weblate component slug (Weblate-specific, leave empty for Tolgee)", Category = "Localization", DisplayOrder = 5, CreatedAt = seedTimestamp }
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
            new TagType { Id = 1, MasterCode = "TITLE", FullName = "Title", Description = "Title-based tags for labeling and categorization" },
            new TagType { Id = 2, MasterCode = "PEOPLE", FullName = "People", Description = "People-based tags for associating persons with content" });
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

    private static async Task SeedRolesAsync(ExploreDbContext context, CancellationToken ct)
    {
        var expectedRoles = new[]
        {
            // Platform scope (1-9)
            new Role { Id = (int)RoleEnum.Admin, MasterCode = "platform.admin", FullName = "Admin", Description = "Platform administration", Scope = RoleScopeEnum.Platform, IsSystem = true },
            new Role { Id = (int)RoleEnum.Moderator, MasterCode = "platform.moderator", FullName = "Moderator", Description = "Platform moderation", Scope = RoleScopeEnum.Platform, IsSystem = true },
            new Role { Id = (int)RoleEnum.Member, MasterCode = "platform.member", FullName = "Member", Description = "Platform member", Scope = RoleScopeEnum.Platform, IsSystem = true },

            // Tenant scope (10-19)
            new Role { Id = (int)RoleEnum.TenantAdmin, MasterCode = "tenant.admin", FullName = "Admin", Description = "Tenant administration", Scope = RoleScopeEnum.Tenant, IsSystem = true },
            new Role { Id = (int)RoleEnum.TenantModerator, MasterCode = "tenant.moderator", FullName = "Moderator", Description = "Tenant content moderation", Scope = RoleScopeEnum.Tenant, IsSystem = true },
            new Role { Id = (int)RoleEnum.TenantMember, MasterCode = "tenant.member", FullName = "Member", Description = "Tenant member", Scope = RoleScopeEnum.Tenant, IsSystem = true },

            // Organization scope (20-29)
            new Role { Id = (int)RoleEnum.OrgAdmin, MasterCode = "org.admin", FullName = "Admin", Description = "Organization administrator", Scope = RoleScopeEnum.Organization, IsSystem = true },
            new Role { Id = (int)RoleEnum.OrgModerator, MasterCode = "org.moderator", FullName = "Moderator", Description = "Organization moderator", Scope = RoleScopeEnum.Organization, IsSystem = true },
            new Role { Id = (int)RoleEnum.OrgMember, MasterCode = "org.member", FullName = "Member", Description = "Regular organization member", Scope = RoleScopeEnum.Organization, IsSystem = true }
        };

        var existingIds = await context.Roles
            .AsNoTracking()
            .Select(x => x.Id)
            .ToListAsync(ct);

        var existingIdSet = existingIds.ToHashSet();
        var missingRoles = expectedRoles
            .Where(x => !existingIdSet.Contains(x.Id))
            .ToList();

        if (missingRoles.Count == 0) return;

        context.Roles.AddRange(missingRoles);
        await context.SaveChangesAsync(ct);
    }

    private static async Task SeedPermissionsAsync(ExploreDbContext context, CancellationToken ct)
    {
        // Permission vocabulary: resource_kind × action pairs for all 18 resource kinds.
        // MasterCode format: "{resource_kind}:{action}" (matches Cerbos resource/action model).
        var expectedPermissions = new List<Permission>();
        var id = 1;

        // Helper to add a permission set for a resource kind
        void AddPermissions(string resourceKind, string groupName, RoleScopeEnum scope, string[] actions, bool isFiltered = false)
        {
            foreach (var action in actions)
            {
                expectedPermissions.Add(new Permission
                {
                    Id = id++,
                    ResourceKind = resourceKind,
                    Action = action,
                    MasterCode = $"{resourceKind}:{action}",
                    FullName = $"{FormatName(action)} {FormatName(resourceKind)}",
                    GroupName = groupName,
                    Scope = scope,
                    IsSystem = true,
                    IsFiltered = isFiltered,
                    IsActive = true
                });
            }
        }

        string[] crud = ["read", "create", "update", "delete"];
        string[] readOnly = ["read"];
        string[] noDelete = ["read", "create", "update"];

        // Events group
        AddPermissions("event", "Events", RoleScopeEnum.Organization, crud);
        AddPermissions("event_session", "Events", RoleScopeEnum.Organization, crud);
        AddPermissions("event_session_agenda_item", "Events", RoleScopeEnum.Organization, crud);
        AddPermissions("event_registration", "Events", RoleScopeEnum.Organization, crud);

        // Organizations group
        AddPermissions("organization", "Organizations", RoleScopeEnum.Organization, crud);
        AddPermissions("organization_member", "Organizations", RoleScopeEnum.Organization, crud);
        AddPermissions("organization_review", "Organizations", RoleScopeEnum.Organization, crud);

        // Content group
        AddPermissions("category", "Content", RoleScopeEnum.Tenant, crud);
        AddPermissions("tag", "Content", RoleScopeEnum.Tenant, crud);
        AddPermissions("location", "Content", RoleScopeEnum.Tenant, crud);
        AddPermissions("storage_object", "Content", RoleScopeEnum.Organization, noDelete);

        // Users group
        AddPermissions("user", "Users", RoleScopeEnum.Platform, readOnly);
        AddPermissions("tenant_user", "Users", RoleScopeEnum.Tenant, crud);

        // Tenant management group
        AddPermissions("tenant", "Tenants", RoleScopeEnum.Platform, crud, isFiltered: true);
        AddPermissions("tenant_setting", "Settings", RoleScopeEnum.Tenant, ["read", "update"]);

        // Instance settings (platform-only, filtered from non-super-admins)
        AddPermissions("instance_setting", "Settings", RoleScopeEnum.Platform, ["read", "update"], isFiltered: true);

        // Federation group
        AddPermissions("indexed_did", "Federation", RoleScopeEnum.Platform, noDelete);
        AddPermissions("atproto_record", "Federation", RoleScopeEnum.Platform, noDelete);

        var existingCodes = await context.Permissions
            .AsNoTracking()
            .Select(x => x.MasterCode)
            .ToListAsync(ct);

        var existingCodeSet = existingCodes.ToHashSet();
        var missingPermissions = expectedPermissions
            .Where(x => !existingCodeSet.Contains(x.MasterCode))
            .ToList();

        if (missingPermissions.Count == 0) return;

        context.Permissions.AddRange(missingPermissions);
        await context.SaveChangesAsync(ct);
    }

    /// <summary>
    /// Formats a snake_case identifier to Title Case for display.
    /// </summary>
    private static string FormatName(string identifier)
    {
        return string.Join(' ', identifier.Split('_')
            .Select(word => char.ToUpperInvariant(word[0]) + word[1..]));
    }

    private static async Task SeedNotificationTypesAsync(ExploreDbContext context, CancellationToken ct)
    {
        if (await context.Set<NotificationType>().AnyAsync(ct)) return;

        context.Set<NotificationType>().AddRange(
            new NotificationType { Id = (int)NotificationTypeEnum.RegistrationConfirmed, MasterCode = "REGISTRATION_CONFIRMED", FullName = "Registration Confirmed", Description = "RSVP or registration was confirmed" },
            new NotificationType { Id = (int)NotificationTypeEnum.ApprovalGranted, MasterCode = "APPROVAL_GRANTED", FullName = "Approval Granted", Description = "An approval request was granted" },
            new NotificationType { Id = (int)NotificationTypeEnum.ApprovalRejected, MasterCode = "APPROVAL_REJECTED", FullName = "Approval Rejected", Description = "An approval request was rejected" },
            new NotificationType { Id = (int)NotificationTypeEnum.WaitlistPromoted, MasterCode = "WAITLIST_PROMOTED", FullName = "Waitlist Promoted", Description = "Promoted from waitlist to confirmed" },
            new NotificationType { Id = (int)NotificationTypeEnum.EventCreated, MasterCode = "EVENT_CREATED", FullName = "Event Created", Description = "A new event was created" },
            new NotificationType { Id = (int)NotificationTypeEnum.EventUpdated, MasterCode = "EVENT_UPDATED", FullName = "Event Updated", Description = "An event was updated" },
            new NotificationType { Id = (int)NotificationTypeEnum.EventCancelled, MasterCode = "EVENT_CANCELLED", FullName = "Event Cancelled", Description = "An event was cancelled" },
            new NotificationType { Id = (int)NotificationTypeEnum.MemberInvited, MasterCode = "MEMBER_INVITED", FullName = "Member Invited", Description = "Invited to join an organization or group" },
            new NotificationType { Id = (int)NotificationTypeEnum.MemberRemoved, MasterCode = "MEMBER_REMOVED", FullName = "Member Removed", Description = "Removed from an organization or group" },
            new NotificationType { Id = (int)NotificationTypeEnum.General, MasterCode = "GENERAL", FullName = "General", Description = "General purpose notification" });
        await context.SaveChangesAsync(ct);
    }

    private static async Task SeedNotificationEntityTypesAsync(ExploreDbContext context, CancellationToken ct)
    {
        if (await context.Set<NotificationEntityType>().AnyAsync(ct)) return;

        context.Set<NotificationEntityType>().AddRange(
            new NotificationEntityType { Id = (int)NotificationEntityTypeEnum.Event, MasterCode = "EVENT", FullName = "Event", Description = "Links to an event" },
            new NotificationEntityType { Id = (int)NotificationEntityTypeEnum.Organization, MasterCode = "ORGANIZATION", FullName = "Organization", Description = "Links to an organization" },
            new NotificationEntityType { Id = (int)NotificationEntityTypeEnum.Group, MasterCode = "GROUP", FullName = "Group", Description = "Links to a group" },
            new NotificationEntityType { Id = (int)NotificationEntityTypeEnum.EventRegistration, MasterCode = "EVENT_REGISTRATION", FullName = "Event Registration", Description = "Links to an event registration" },
            new NotificationEntityType { Id = (int)NotificationEntityTypeEnum.EventSession, MasterCode = "EVENT_SESSION", FullName = "Event Session", Description = "Links to an event session" },
            new NotificationEntityType { Id = (int)NotificationEntityTypeEnum.User, MasterCode = "USER", FullName = "User", Description = "Links to a user" });
        await context.SaveChangesAsync(ct);
    }

    /// <summary>
    /// Seeds default instance-level footer link groups (TenantId = null) with standard navigation links.
    /// Only runs if no instance-level footer link groups exist yet.
    /// </summary>
    private static async Task SeedDefaultFooterLinkGroupsAsync(ExploreDbContext context, CancellationToken ct)
    {
        // Only seed if no instance-level (TenantId = null) footer link groups exist
        if (await context.Set<TenantFooterLinkGroup>().AnyAsync(g => g.TenantId == null, ct)) return;

        var now = DateTime.UtcNow;

        // Group 1: Quick Links
        var quickLinksGroup = new TenantFooterLinkGroup
        {
            Id = Guid.Parse("019573a0-0001-7000-8000-000000000001"),
            TenantId = null,
            Title = "Quick Links",
            Order = 0,
            IsActive = true,
            CreatedAt = now,
        };

        // Group 2: Legal
        var legalGroup = new TenantFooterLinkGroup
        {
            Id = Guid.Parse("019573a0-0002-7000-8000-000000000001"),
            TenantId = null,
            Title = "Legal",
            Order = 1,
            IsActive = true,
            CreatedAt = now,
        };

        context.Set<TenantFooterLinkGroup>().AddRange(quickLinksGroup, legalGroup);
        await context.SaveChangesAsync(ct);

        // Quick Links
        context.Set<TenantFooterLink>().AddRange(
            new TenantFooterLink
            {
                Id = Guid.Parse("019573a0-0003-7000-8000-000000000001"),
                FooterLinkGroupId = quickLinksGroup.Id,
                Label = "About Us",
                Url = "/about",
                OpenInNewTab = false,
                Order = 0,
                IsActive = true,
                CreatedAt = now,
            },
            new TenantFooterLink
            {
                Id = Guid.Parse("019573a0-0004-7000-8000-000000000001"),
                FooterLinkGroupId = quickLinksGroup.Id,
                Label = "Events",
                Url = "/events",
                OpenInNewTab = false,
                Order = 1,
                IsActive = true,
                CreatedAt = now,
            },
            new TenantFooterLink
            {
                Id = Guid.Parse("019573a0-0005-7000-8000-000000000001"),
                FooterLinkGroupId = quickLinksGroup.Id,
                Label = "Contact",
                Url = "/contact",
                OpenInNewTab = false,
                Order = 2,
                IsActive = true,
                CreatedAt = now,
            });

        // Legal
        context.Set<TenantFooterLink>().AddRange(
            new TenantFooterLink
            {
                Id = Guid.Parse("019573a0-0006-7000-8000-000000000001"),
                FooterLinkGroupId = legalGroup.Id,
                Label = "Terms of Service",
                Url = "/terms",
                OpenInNewTab = false,
                Order = 0,
                IsActive = true,
                CreatedAt = now,
            },
            new TenantFooterLink
            {
                Id = Guid.Parse("019573a0-0007-7000-8000-000000000001"),
                FooterLinkGroupId = legalGroup.Id,
                Label = "Privacy Policy",
                Url = "/privacy",
                OpenInNewTab = false,
                Order = 1,
                IsActive = true,
                CreatedAt = now,
            });

        await context.SaveChangesAsync(ct);
    }

    private static async Task SeedExternalApiKeyStatusesAsync(ExploreDbContext context, CancellationToken ct)
    {
        if (await context.Set<ExternalApiKeyStatus>().AnyAsync(ct)) return;

        context.Set<ExternalApiKeyStatus>().AddRange(
            new ExternalApiKeyStatus { Id = (int)ExternalApiKeyStatusEnum.Active, MasterCode = "ACTIVE", FullName = "Active", Description = "Key is active and can authenticate requests", IsUsable = true },
            new ExternalApiKeyStatus { Id = (int)ExternalApiKeyStatusEnum.Revoked, MasterCode = "REVOKED", FullName = "Revoked", Description = "Key has been permanently revoked by owner or admin", IsUsable = false },
            new ExternalApiKeyStatus { Id = (int)ExternalApiKeyStatusEnum.Expired, MasterCode = "EXPIRED", FullName = "Expired", Description = "Key has passed its expiration date", IsUsable = false },
            new ExternalApiKeyStatus { Id = (int)ExternalApiKeyStatusEnum.Suspended, MasterCode = "SUSPENDED", FullName = "Suspended", Description = "Key is temporarily suspended due to credit exhaustion or policy violation", IsUsable = false },
            new ExternalApiKeyStatus { Id = (int)ExternalApiKeyStatusEnum.PendingRotation, MasterCode = "PENDING_ROTATION", FullName = "Pending Rotation", Description = "Key is in rotation overlap window; still usable until new key is confirmed", IsUsable = true });
        await context.SaveChangesAsync(ct);
    }

    private static async Task SeedExternalApiKeyCreditPeriodsAsync(ExploreDbContext context, CancellationToken ct)
    {
        if (await context.Set<ExternalApiKeyCreditPeriod>().AnyAsync(ct)) return;

        context.Set<ExternalApiKeyCreditPeriod>().AddRange(
            new ExternalApiKeyCreditPeriod { Id = (int)ExternalApiKeyCreditPeriodEnum.None, MasterCode = "NONE", FullName = "None", Description = "No credit tracking; unlimited usage" },
            new ExternalApiKeyCreditPeriod { Id = (int)ExternalApiKeyCreditPeriodEnum.Daily, MasterCode = "DAILY", FullName = "Daily", Description = "Credit quota resets every day" },
            new ExternalApiKeyCreditPeriod { Id = (int)ExternalApiKeyCreditPeriodEnum.Weekly, MasterCode = "WEEKLY", FullName = "Weekly", Description = "Credit quota resets every week" },
            new ExternalApiKeyCreditPeriod { Id = (int)ExternalApiKeyCreditPeriodEnum.Monthly, MasterCode = "MONTHLY", FullName = "Monthly", Description = "Credit quota resets every month" },
            new ExternalApiKeyCreditPeriod { Id = (int)ExternalApiKeyCreditPeriodEnum.Yearly, MasterCode = "YEARLY", FullName = "Yearly", Description = "Credit quota resets every year" });
        await context.SaveChangesAsync(ct);
    }
}
