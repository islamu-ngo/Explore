using Explore.Application.Contracts.Infrastructure;
using Explore.Domain;
using Explore.Domain.Federation;
using Explore.Domain.Interfaces;
using Explore.Domain.Modules;
using Explore.Persistence.Configurations.Entities;
using Explore.Persistence.QueryFilters;
using Microsoft.EntityFrameworkCore;
using StorageObject = Explore.Domain.StorageObject;

namespace Explore.Persistence;

public class ExploreDbContext : DbContext
{
    /// <summary>
    /// Tenant context for multi-tenant data isolation.
    /// Set via property injection after DbContext is retrieved from pool.
    /// When null, Global Query Filters are bypassed (e.g., during migrations).
    /// </summary>
    public ITenantContext? TenantContext { get; set; }

    /// <summary>
    /// Current user service for audit field population.
    /// Set via property injection after DbContext is retrieved from pool.
    /// When null (e.g., during migrations), audit fields use null values.
    /// </summary>
    public ICurrentUserService? CurrentUserService { get; set; }

    /// <summary>
    /// Constructor for DbContext pooling compatibility.
    /// All scoped dependencies (TenantContext, CurrentUserService) are set via property injection.
    /// </summary>
    public ExploreDbContext(DbContextOptions<ExploreDbContext> options) : base(options)
    {
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Auto-discover and apply all IEntityTypeConfiguration implementations from this assembly
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(ExploreDbContext).Assembly);

        // Apply Global Query Filters for all ITenantEntity implementations
        ApplyGlobalQueryFilters(modelBuilder);
    }

    private void ApplyGlobalQueryFilters(ModelBuilder modelBuilder)
    {
        // ===== Event Entities =====
        // Entities with both Tenant and Soft Delete filters (separate named filters for selective disabling)
        modelBuilder.Entity<Event>()
            .HasQueryFilter(QueryFilterNames.Tenant, e => TenantContext == null || e.TenantId == (TenantContext != null ? TenantContext.TenantId : Guid.Empty))
            .HasQueryFilter(QueryFilterNames.SoftDelete, e => !e.IsDeleted);

        modelBuilder.Entity<EventSession>()
            .HasQueryFilter(QueryFilterNames.Tenant, e => TenantContext == null || e.TenantId == (TenantContext != null ? TenantContext.TenantId : Guid.Empty))
            .HasQueryFilter(QueryFilterNames.SoftDelete, e => !e.IsDeleted);

        // Event-related entities (tenant only - no soft delete)
        modelBuilder.Entity<EventRegistration>()
            .HasQueryFilter(QueryFilterNames.Tenant, e => TenantContext == null || e.TenantId == (TenantContext != null ? TenantContext.TenantId : Guid.Empty))
            .HasQueryFilter(QueryFilterNames.SoftDelete, e => !e.IsDeleted);
        modelBuilder.Entity<EventCategories>()
            .HasQueryFilter(QueryFilterNames.Tenant, e => TenantContext == null || e.TenantId == (TenantContext != null ? TenantContext.TenantId : Guid.Empty));
        modelBuilder.Entity<EventTags>()
            .HasQueryFilter(QueryFilterNames.Tenant, e => TenantContext == null || e.TenantId == (TenantContext != null ? TenantContext.TenantId : Guid.Empty));
        modelBuilder.Entity<EventSessionLanguage>()
            .HasQueryFilter(QueryFilterNames.Tenant, e => TenantContext == null || e.TenantId == (TenantContext != null ? TenantContext.TenantId : Guid.Empty));
        modelBuilder.Entity<EventSessionSpeaker>()
            .HasQueryFilter(QueryFilterNames.Tenant, e => TenantContext == null || e.TenantId == (TenantContext != null ? TenantContext.TenantId : Guid.Empty));
        modelBuilder.Entity<EventSessionAgendaItem>()
            .HasQueryFilter(QueryFilterNames.Tenant, e => TenantContext == null || e.TenantId == (TenantContext != null ? TenantContext.TenantId : Guid.Empty));
        modelBuilder.Entity<EventSessionIslamicAspect>()
            .HasQueryFilter(QueryFilterNames.Tenant,
                e => TenantContext == null
                    || (e.EventSession != null && e.EventSession.TenantId == (TenantContext != null ? TenantContext.TenantId : Guid.Empty)));

        // Lookup extension: global event types (TenantId = null) + tenant-specific custom event types
        modelBuilder.Entity<EventType>()
            .HasQueryFilter(QueryFilterNames.Tenant,
                e => e.TenantId == null
                    || TenantContext == null
                    || e.TenantId == (TenantContext != null ? TenantContext.TenantId : Guid.Empty));

        // ===== Organization Entities =====
        // Entities with both Tenant and Soft Delete filters
        modelBuilder.Entity<Organization>()
            .HasQueryFilter(QueryFilterNames.Tenant, e => TenantContext == null || e.TenantId == (TenantContext != null ? TenantContext.TenantId : Guid.Empty))
            .HasQueryFilter(QueryFilterNames.SoftDelete, e => !e.IsDeleted);

        modelBuilder.Entity<OrganizationMember>()
            .HasQueryFilter(QueryFilterNames.Tenant, e => TenantContext == null || e.TenantId == (TenantContext != null ? TenantContext.TenantId : Guid.Empty))
            .HasQueryFilter(QueryFilterNames.SoftDelete, e => !e.IsDeleted);

        // Organization review (tenant + soft delete)
        modelBuilder.Entity<OrganizationReview>()
            .HasQueryFilter(QueryFilterNames.Tenant, e => TenantContext == null || e.TenantId == (TenantContext != null ? TenantContext.TenantId : Guid.Empty))
            .HasQueryFilter(QueryFilterNames.SoftDelete, e => !e.IsDeleted);

        // ===== Group Entities =====
        modelBuilder.Entity<Group>()
            .HasQueryFilter(QueryFilterNames.Tenant, e => TenantContext == null || e.TenantId == (TenantContext != null ? TenantContext.TenantId : Guid.Empty))
            .HasQueryFilter(QueryFilterNames.SoftDelete, e => !e.IsDeleted);

        modelBuilder.Entity<GroupMember>()
            .HasQueryFilter(QueryFilterNames.Tenant, e => TenantContext == null || e.TenantId == (TenantContext != null ? TenantContext.TenantId : Guid.Empty))
            .HasQueryFilter(QueryFilterNames.SoftDelete, e => !e.IsDeleted);

        // ===== Custom Properties (EAV) =====
        modelBuilder.Entity<CustomPropertyDefinition>()
            .HasQueryFilter(QueryFilterNames.Tenant, e => TenantContext == null || e.TenantId == (TenantContext != null ? TenantContext.TenantId : Guid.Empty))
            .HasQueryFilter(QueryFilterNames.SoftDelete, e => !e.IsDeleted);

        modelBuilder.Entity<CustomPropertyOption>()
            .HasQueryFilter(QueryFilterNames.SoftDelete, e => !e.IsDeleted);

        modelBuilder.Entity<CustomPropertyValue>()
            .HasQueryFilter(QueryFilterNames.Tenant, e => TenantContext == null || e.TenantId == (TenantContext != null ? TenantContext.TenantId : Guid.Empty))
            .HasQueryFilter(QueryFilterNames.SoftDelete, e => !e.IsDeleted);

        modelBuilder.Entity<EventTemplate>()
            .HasQueryFilter(QueryFilterNames.Tenant, e => TenantContext == null || e.TenantId == (TenantContext != null ? TenantContext.TenantId : Guid.Empty))
            .HasQueryFilter(QueryFilterNames.SoftDelete, e => !e.IsDeleted);

        modelBuilder.Entity<EventTemplateCustomPropertyDefinition>()
            .HasQueryFilter(QueryFilterNames.Tenant, e => TenantContext == null || e.TenantId == (TenantContext != null ? TenantContext.TenantId : Guid.Empty))
            .HasQueryFilter(QueryFilterNames.SoftDelete, e => !e.IsDeleted);

        modelBuilder.Entity<EventTemplateCustomPropertyOption>()
            .HasQueryFilter(QueryFilterNames.SoftDelete, e => !e.IsDeleted);

        modelBuilder.Entity<EventCustomPropertyDefinition>()
            .HasQueryFilter(QueryFilterNames.Tenant, e => TenantContext == null || e.TenantId == (TenantContext != null ? TenantContext.TenantId : Guid.Empty))
            .HasQueryFilter(QueryFilterNames.SoftDelete, e => !e.IsDeleted);

        modelBuilder.Entity<EventCustomPropertyOption>()
            .HasQueryFilter(QueryFilterNames.SoftDelete, e => !e.IsDeleted);

        modelBuilder.Entity<EventCustomPropertyValue>()
            .HasQueryFilter(QueryFilterNames.Tenant, e => TenantContext == null || e.TenantId == (TenantContext != null ? TenantContext.TenantId : Guid.Empty))
            .HasQueryFilter(QueryFilterNames.SoftDelete, e => !e.IsDeleted);

        modelBuilder.Entity<EventCustomPropertyProjection>()
            .HasQueryFilter(QueryFilterNames.Tenant, e => TenantContext == null || e.TenantId == (TenantContext != null ? TenantContext.TenantId : Guid.Empty));

        // ===== Event Session EAV Entities =====
        modelBuilder.Entity<EventSessionTemplate>()
            .HasQueryFilter(QueryFilterNames.Tenant, e => TenantContext == null || e.TenantId == (TenantContext != null ? TenantContext.TenantId : Guid.Empty))
            .HasQueryFilter(QueryFilterNames.SoftDelete, e => !e.IsDeleted);

        modelBuilder.Entity<EventSessionTemplateCustomPropertyDefinition>()
            .HasQueryFilter(QueryFilterNames.Tenant, e => TenantContext == null || e.TenantId == (TenantContext != null ? TenantContext.TenantId : Guid.Empty))
            .HasQueryFilter(QueryFilterNames.SoftDelete, e => !e.IsDeleted);

        modelBuilder.Entity<EventSessionTemplateCustomPropertyOption>()
            .HasQueryFilter(QueryFilterNames.SoftDelete, e => !e.IsDeleted);

        modelBuilder.Entity<EventSessionCustomPropertyDefinition>()
            .HasQueryFilter(QueryFilterNames.Tenant, e => TenantContext == null || e.TenantId == (TenantContext != null ? TenantContext.TenantId : Guid.Empty))
            .HasQueryFilter(QueryFilterNames.SoftDelete, e => !e.IsDeleted);

        modelBuilder.Entity<EventSessionCustomPropertyOption>()
            .HasQueryFilter(QueryFilterNames.SoftDelete, e => !e.IsDeleted);

        modelBuilder.Entity<EventSessionCustomPropertyValue>()
            .HasQueryFilter(QueryFilterNames.Tenant, e => TenantContext == null || e.TenantId == (TenantContext != null ? TenantContext.TenantId : Guid.Empty))
            .HasQueryFilter(QueryFilterNames.SoftDelete, e => !e.IsDeleted);

        modelBuilder.Entity<EventSessionCustomPropertyProjection>()
            .HasQueryFilter(QueryFilterNames.Tenant, e => TenantContext == null || e.TenantId == (TenantContext != null ? TenantContext.TenantId : Guid.Empty));

        // ===== Actor Entities =====
        // Entities with both Tenant and Soft Delete filters
        modelBuilder.Entity<Actor>()
            .HasQueryFilter(QueryFilterNames.Tenant, e => TenantContext == null || e.TenantId == (TenantContext != null ? TenantContext.TenantId : Guid.Empty))
            .HasQueryFilter(QueryFilterNames.SoftDelete, e => !e.IsDeleted);

        modelBuilder.Entity<ActorPii>()
            .HasQueryFilter(QueryFilterNames.Tenant,
                e => TenantContext == null
                    || (e.Actor != null && e.Actor.TenantId == (TenantContext != null ? TenantContext.TenantId : Guid.Empty)));

        // Actor-related (tenant only - no soft delete)
        modelBuilder.Entity<ActorKeyStore>()
            .HasQueryFilter(QueryFilterNames.Tenant, e => TenantContext == null || e.TenantId == (TenantContext != null ? TenantContext.TenantId : Guid.Empty));

        // ===== User Entity =====
        // Soft Delete only (not tenant-scoped - global entity)
        modelBuilder.Entity<User>()
            .HasQueryFilter(QueryFilterNames.SoftDelete, u => !u.IsDeleted);

        modelBuilder.Entity<UserPii>()
            .HasQueryFilter(QueryFilterNames.SoftDelete, e => e.User != null && !e.User.IsDeleted);

        // ===== Location Entity =====
        modelBuilder.Entity<Location>()
            .HasQueryFilter(QueryFilterNames.Tenant, e => TenantContext == null || e.TenantId == (TenantContext != null ? TenantContext.TenantId : Guid.Empty));

        modelBuilder.Entity<LocationPii>()
            .HasQueryFilter(QueryFilterNames.Tenant,
                e => TenantContext == null
                    || (e.Location != null && e.Location.TenantId == (TenantContext != null ? TenantContext.TenantId : Guid.Empty)));

        // ===== Storage Entity =====
        modelBuilder.Entity<StorageObject>()
            .HasQueryFilter(QueryFilterNames.Tenant, e => TenantContext == null || e.TenantId == (TenantContext != null ? TenantContext.TenantId : Guid.Empty));

        modelBuilder.Entity<OrganizationPii>()
            .HasQueryFilter(QueryFilterNames.Tenant,
                e => TenantContext == null
                    || (e.Organization != null && e.Organization.TenantId == (TenantContext != null ? TenantContext.TenantId : Guid.Empty)));

        // ===== Category and Tag Entities =====
        modelBuilder.Entity<Category>()
            .HasQueryFilter(QueryFilterNames.Tenant, e => TenantContext == null || e.TenantId == (TenantContext != null ? TenantContext.TenantId : Guid.Empty));
        modelBuilder.Entity<Tag>()
            .HasQueryFilter(QueryFilterNames.Tenant, e => TenantContext == null || e.TenantId == (TenantContext != null ? TenantContext.TenantId : Guid.Empty));
        modelBuilder.Entity<TagTypeTags>()
            .HasQueryFilter(QueryFilterNames.Tenant, e => TenantContext == null || e.TenantId == (TenantContext != null ? TenantContext.TenantId : Guid.Empty));
        modelBuilder.Entity<CategoryTypeCategories>()
            .HasQueryFilter(QueryFilterNames.Tenant, e => TenantContext == null || e.TenantId == (TenantContext != null ? TenantContext.TenantId : Guid.Empty));

        // ===== User-Related Tenant Entities =====
        modelBuilder.Entity<ExternalApiKey>()
            .HasQueryFilter(QueryFilterNames.Tenant, e => TenantContext == null || (e.TenantId != null && e.TenantId == (TenantContext != null ? TenantContext.TenantId : Guid.Empty)));
        modelBuilder.Entity<UserAuthenticationToken>()
            .HasQueryFilter(QueryFilterNames.Tenant, e => TenantContext == null || e.TenantId == (TenantContext != null ? TenantContext.TenantId : Guid.Empty));
        modelBuilder.Entity<UserExternalLogin>()
            .HasQueryFilter(QueryFilterNames.Tenant, e => TenantContext == null || e.TenantId == (TenantContext != null ? TenantContext.TenantId : Guid.Empty));

        // ===== Tenant Entities =====
        modelBuilder.Entity<TenantSettings>()
            .HasQueryFilter(QueryFilterNames.Tenant, e => TenantContext == null || e.TenantId == (TenantContext != null ? TenantContext.TenantId : Guid.Empty));
        modelBuilder.Entity<TenantSetting>()
            .HasQueryFilter(QueryFilterNames.Tenant, e => TenantContext == null || e.TenantId == (TenantContext != null ? TenantContext.TenantId : Guid.Empty));
        modelBuilder.Entity<TenantMember>()
            .HasQueryFilter(QueryFilterNames.Tenant, e => TenantContext == null || e.TenantId == (TenantContext != null ? TenantContext.TenantId : Guid.Empty));
        modelBuilder.Entity<TenantOnboardingState>()
            .HasQueryFilter(QueryFilterNames.Tenant, e => TenantContext == null || e.TenantId == (TenantContext != null ? TenantContext.TenantId : Guid.Empty));

        modelBuilder.Entity<TenantNavigationLink>()
            .HasQueryFilter(QueryFilterNames.Tenant, e => TenantContext == null || e.TenantId == (TenantContext != null ? TenantContext.TenantId : Guid.Empty))
            .HasQueryFilter(QueryFilterNames.SoftDelete, e => !e.IsDeleted);

        // Footer link groups: instance-default groups (TenantId = null) are always visible;
        // tenant-owned groups respect the tenant filter (same pattern as EventType).
        modelBuilder.Entity<TenantFooterLinkGroup>()
            .HasQueryFilter(QueryFilterNames.Tenant,
                e => e.TenantId == null
                    || TenantContext == null
                    || e.TenantId == (TenantContext != null ? TenantContext.TenantId : Guid.Empty));

        // Footer links have no TenantId — isolation flows through the parent group query filter.
        // No additional filter needed; EF will respect the parent filter via navigation includes.

        modelBuilder.Entity<TenantInvitation>()
            .HasQueryFilter(QueryFilterNames.Tenant, e => TenantContext == null || e.TenantId == (TenantContext != null ? TenantContext.TenantId : Guid.Empty));
        // ===== Module Governance Entities =====
        modelBuilder.Entity<TenantCapability>()
            .HasQueryFilter(QueryFilterNames.Tenant, e => TenantContext == null || e.TenantId == (TenantContext != null ? TenantContext.TenantId : Guid.Empty));

        // ===== Audit & Notifications =====
        modelBuilder.Entity<AuditLog>()
            .HasQueryFilter(QueryFilterNames.Tenant, e => TenantContext == null || e.TenantId == (TenantContext != null ? TenantContext.TenantId : Guid.Empty));
        modelBuilder.Entity<Notification>()
            .HasQueryFilter(QueryFilterNames.Tenant, e => TenantContext == null || e.TenantId == (TenantContext != null ? TenantContext.TenantId : Guid.Empty))
            .HasQueryFilter(QueryFilterNames.SoftDelete, e => !e.IsDeleted);
    }

    /// <summary>
    /// Overrides SaveChangesAsync to automatically populate audit fields and handle soft delete.
    /// - IAuditableEntity: Sets CreatedAt/CreatedBy on insert, UpdatedAt/UpdatedBy on update
    /// - ISoftDeletable: Converts hard deletes to soft deletes (IsDeleted=true)
    /// </summary>
    public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        var userId = GetCurrentUserId();
        var now = DateTime.UtcNow;

        foreach (var entry in ChangeTracker.Entries())
        {
            // Handle IConcurrencyAware - automatic concurrency stamp generation
            if (entry.Entity is IConcurrencyAware concurrencyAware &&
                (entry.State == EntityState.Added || entry.State == EntityState.Modified))
            {
                concurrencyAware.ConcurrencyStamp = Guid.NewGuid();
            }

            // Handle IAuditableEntity - automatic audit field population
            if (entry.Entity is IAuditableEntity auditable)
            {
                switch (entry.State)
                {
                    case EntityState.Added:
                        auditable.CreatedAt = now;
                        auditable.CreatedBy = userId;
                        break;

                    case EntityState.Modified:
                        // Only update if not already set by soft delete logic below
                        if (auditable.UpdatedAt == null || auditable.UpdatedAt == default(DateTime))
                        {
                            auditable.UpdatedAt = now;
                            auditable.UpdatedBy = userId;
                        }
                        break;
                }
            }

            // Handle ISoftDeletable - convert hard deletes to soft deletes
            if (entry.Entity is ISoftDeletable deletable && entry.State == EntityState.Deleted)
            {
                // Change state from Deleted to Modified (prevent actual deletion)
                entry.State = EntityState.Modified;

                // Mark as soft deleted
                deletable.IsDeleted = true;
                deletable.DeletedAt = now;
                deletable.DeletedBy = userId;

                // Also update audit fields if entity is auditable
                if (entry.Entity is IAuditableEntity auditableDeleted)
                {
                    auditableDeleted.UpdatedAt = now;
                    auditableDeleted.UpdatedBy = userId;
                }
            }
        }

        return await base.SaveChangesAsync(cancellationToken);
    }

    /// <summary>
    /// Gets the current user ID from the authentication context.
    /// Returns null if no user is authenticated (e.g., during migrations, seeding).
    /// </summary>
    private Guid? GetCurrentUserId()
    {
        return CurrentUserService?.UserId;
    }

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

    // ===== Event Lookup Tables =====
    public DbSet<EventType> EventTypes { get; set; }
    public DbSet<EventStatus> EventStatuses { get; set; }
    public DbSet<EventFormat> EventFormats { get; set; }
    public DbSet<VisibilityType> VisibilityTypes { get; set; }
    public DbSet<RegistrationMode> RegistrationModes { get; set; }

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
