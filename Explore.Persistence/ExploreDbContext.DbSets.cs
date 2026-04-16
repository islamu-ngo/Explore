// ABOUTME: Partial class containing all DbSet property declarations for the Explore platform.
// ABOUTME: Organized by domain area: Tenancy, Users, Auth, Actors, Organizations, Groups, Events, etc.

using Explore.Domain;
using Explore.Domain.Federation;
using Explore.Domain.Modules;
using Microsoft.EntityFrameworkCore;
using StorageObject = Explore.Domain.StorageObject;

namespace Explore.Persistence;

public partial class ExploreDbContext
{
    // ===== Multi-tenancy =====
    public DbSet<Tenant> Tenants { get; set; }
    public DbSet<TenantSettings> TenantSettings { get; set; }
    public DbSet<TenantMember> TenantMembers { get; set; }
    public DbSet<TenantOnboardingState> TenantOnboardingStates { get; set; }
    public DbSet<TenantInvitation> TenantInvitations { get; set; }
    public DbSet<TenantLifecycleLog> TenantLifecycleLogs { get; set; }
    public DbSet<PlatformUserRole> PlatformUserRoles { get; set; }
    public DbSet<TenantNavigationLink> TenantNavigationLinks { get; set; }
    public DbSet<TenantFooterLinkGroup> TenantFooterLinkGroups { get; set; }
    public DbSet<TenantFooterLink> TenantFooterLinks { get; set; }
    public DbSet<InstanceBootstrapState> InstanceBootstrapStates { get; set; }

    // ===== Governance Policy Aggregates =====
    public DbSet<Explore.Domain.Policies.InstancePolicySet> InstancePolicySets { get; set; }
    public DbSet<Explore.Domain.Policies.TenantPolicySet> TenantPolicySets { get; set; }
    public DbSet<Explore.Domain.Policies.OrganizationPolicySet> OrganizationPolicySets { get; set; }
    public DbSet<Explore.Domain.Policies.PolicyChangeOutbox> PolicyChangeOutbox { get; set; }

    // ===== Users & Authentication =====
    public DbSet<User> Users { get; set; }
    public DbSet<UserPii> UserPii { get; set; }
    public DbSet<ExternalApiKey> ExternalApiKeys { get; set; }
    public DbSet<ExternalApiKeyStatus> ExternalApiKeyStatuses { get; set; }
    public DbSet<ExternalApiKeyCreditPeriod> ExternalApiKeyCreditPeriods { get; set; }
    public DbSet<ExternalApiKeyQuota> ExternalApiKeyQuotas { get; set; }
    public DbSet<UserAuthenticationToken> UserAuthenticationTokens { get; set; }
    public DbSet<UserExternalLogin> UserExternalLogins { get; set; }

    // ===== Authorization (RBAC) =====
    public DbSet<Role> Roles { get; set; }
    public DbSet<Permission> Permissions { get; set; }
    public DbSet<RolePermission> RolePermissions { get; set; }

    // ===== Actors (Federation/ATProto) =====
    public DbSet<Actor> Actors { get; set; }
    public DbSet<ActorPii> ActorPii { get; set; }
    public DbSet<ActorType> ActorTypes { get; set; }
    public DbSet<DidCustodyType> DidCustodyTypes { get; set; }
    public DbSet<ActorKeyStore> ActorKeyStores { get; set; }

    // ===== Organizations =====
    public DbSet<Organization> Organizations { get; set; }
    public DbSet<OrganizationPii> OrganizationPii { get; set; }
    public DbSet<OrganizationMember> OrganizationMembers { get; set; }
    public DbSet<OrganizationPosition> OrganizationPositions { get; set; }
    public DbSet<OrganizationReview> OrganizationReviews { get; set; }

    // ===== Group Entities =====
    public DbSet<Group> Groups { get; set; }
    public DbSet<GroupMember> GroupMembers { get; set; }
    public DbSet<GroupPosition> GroupPositions { get; set; }

    // ===== Custom Properties (EAV) =====
    public DbSet<CustomPropertyDefinition> CustomPropertyDefinitions { get; set; }
    public DbSet<CustomPropertyOption> CustomPropertyOptions { get; set; }
    public DbSet<CustomPropertyValue> CustomPropertyValues { get; set; }
    public DbSet<EventTemplate> EventTemplates { get; set; }
    public DbSet<EventTemplateCustomPropertyDefinition> EventTemplateCustomPropertyDefinitions { get; set; }
    public DbSet<EventTemplateCustomPropertyOption> EventTemplateCustomPropertyOptions { get; set; }
    public DbSet<EventCustomPropertyDefinition> EventCustomPropertyDefinitions { get; set; }
    public DbSet<EventCustomPropertyOption> EventCustomPropertyOptions { get; set; }
    public DbSet<EventCustomPropertyValue> EventCustomPropertyValues { get; set; }
    public DbSet<EventCustomPropertyProjection> EventCustomPropertyProjections { get; set; }
    public DbSet<CustomPropertyProjectionStatus> CustomPropertyProjectionStatuses { get; set; }
    public DbSet<CustomPropertyProjectionDirtyScope> CustomPropertyProjectionDirtyScopes { get; set; }

    // ===== Events =====
    public DbSet<Event> Events { get; set; }
    public DbSet<EventSession> EventSessions { get; set; }
    public DbSet<EventSessionIslamicAspect> EventSessionIslamicAspects { get; set; }
    public DbSet<EventRegistration> EventRegistrations { get; set; }
    public DbSet<EventSessionLanguage> EventSessionLanguages { get; set; }
    public DbSet<EventSessionSpeaker> EventSessionSpeakers { get; set; }
    public DbSet<EventSessionAgendaItem> EventSessionAgendaItems { get; set; }
    public DbSet<EventSessionTemplate> EventSessionTemplates { get; set; }
    public DbSet<EventSessionTemplateCustomPropertyDefinition> EventSessionTemplateCustomPropertyDefinitions { get; set; }
    public DbSet<EventSessionTemplateCustomPropertyOption> EventSessionTemplateCustomPropertyOptions { get; set; }
    public DbSet<EventSessionCustomPropertyDefinition> EventSessionCustomPropertyDefinitions { get; set; }
    public DbSet<EventSessionCustomPropertyOption> EventSessionCustomPropertyOptions { get; set; }
    public DbSet<EventSessionCustomPropertyValue> EventSessionCustomPropertyValues { get; set; }
    public DbSet<EventSessionCustomPropertyProjection> EventSessionCustomPropertyProjections { get; set; }
    public DbSet<EventIslamicAspect> EventIslamicAspects { get; set; }
    public DbSet<EventTechAspect> EventTechAspects { get; set; }

    // ===== Event Scheduling Refactor (Phase 1 additive) =====
    public DbSet<EventDay> EventDays { get; set; }
    public DbSet<EventAgendaItem> EventAgendaItems { get; set; }
    public DbSet<LocationRoom> LocationRooms { get; set; }
    public DbSet<EventSessionCategory> EventSessionCategories { get; set; }
    public DbSet<EventSessionTag> EventSessionTags { get; set; }
    public DbSet<EventRegistrationIntent> EventRegistrationIntents { get; set; }

    // ===== Event Lookup Tables =====
    public DbSet<EventType> EventTypes { get; set; }
    public DbSet<EventStatus> EventStatuses { get; set; }
    public DbSet<EventFormat> EventFormats { get; set; }
    public DbSet<VisibilityType> VisibilityTypes { get; set; }
    public DbSet<RegistrationMode> RegistrationModes { get; set; }
    public DbSet<ScheduleItemKind> ScheduleItemKinds { get; set; }
    public DbSet<EventRegistrationPolicy> EventRegistrationPolicies { get; set; }
    public DbSet<RegistrationScope> RegistrationScopes { get; set; }

    // ===== Event Metadata =====
    public DbSet<AudienceAge> AudienceAges { get; set; }
    public DbSet<AudienceGender> AudienceGenders { get; set; }
    public DbSet<Madhab> Madhabs { get; set; }
    public DbSet<Language> Languages { get; set; }
    public DbSet<ApprovalStatus> ApprovalStatuses { get; set; }
    public DbSet<TenantStatus> TenantStatuses { get; set; }
    public DbSet<AnalyticsProvider> AnalyticsProviders { get; set; }

    // ===== Categories & Tags =====
    public DbSet<Category> Categories { get; set; }
    public DbSet<CategoryType> CategoryTypes { get; set; }
    public DbSet<CategoryTypeCategories> CategoryTypeCategories { get; set; }
    public DbSet<Tag> Tags { get; set; }
    public DbSet<TagType> TagTypes { get; set; }
    public DbSet<TagTypeTags> TagTypeTags { get; set; }
    public DbSet<EventCategories> EventCategories { get; set; }
    public DbSet<EventTags> EventTags { get; set; }

    // ===== Locations =====
    public DbSet<Location> Locations { get; set; }
    public DbSet<LocationPii> LocationPii { get; set; }

    // ===== Storage =====
    public DbSet<StorageObject> StorageObjects { get; set; }
    public DbSet<FileType> FileTypes { get; set; }
    public DbSet<OwnerType> OwnerTypes { get; set; }

    // ===== Settings =====
    public DbSet<SystemSetting> SystemSettings { get; set; }
    public DbSet<TenantSetting> TenantSettingOverrides { get; set; }
    public DbSet<OrganizationSetting> OrganizationSettingOverrides { get; set; }
    public DbSet<GroupSetting> GroupSettingOverrides { get; set; }
    public DbSet<UserPreference> UserPreferences { get; set; }
    public DbSet<AppSetting> AppSettings { get; set; }
    public DbSet<ConfigurationChangeLog> ConfigurationChangeLogs { get; set; }
    public DbSet<UiTheme> UiThemes { get; set; }

    // ===== Audit & Notifications =====
    public DbSet<AuditLog> AuditLogs { get; set; }
    public DbSet<Notification> Notifications { get; set; }
    public DbSet<NotificationType> NotificationTypes { get; set; }
    public DbSet<NotificationEntityType> NotificationEntityTypes { get; set; }
    public DbSet<NotificationReason> NotificationReasons { get; set; }

    // ===== Module Governance =====
    public DbSet<ModuleDefinition> ModuleDefinitions { get; set; }
    public DbSet<TenantCapability> TenantCapabilities { get; set; }

    // ===== Federation/Indexer (ATProto) =====
    public DbSet<IndexedDid> IndexedDids { get; set; }
    public DbSet<SyncState> SyncStates { get; set; }
    public DbSet<AtprotoRecord> AtprotoRecords { get; set; }

    // ===== PDS Synchronization (Outbox Pattern) =====
    public DbSet<PdsSyncOutbox> PdsSyncOutbox { get; set; }

    // ===== Generic Outbox (cross-process side effects) =====
    public DbSet<OutboxMessage> OutboxMessages { get; set; }

    // ===== Event Series =====
    public DbSet<EventSeries> EventSeries { get; set; }

    // ===== Contact Share Consents =====
    public DbSet<EventContactShareConsent> EventContactShareConsents { get; set; }

    // ===== Idempotency =====
    public DbSet<IdempotencyRecord> IdempotencyRecords { get; set; }
}
