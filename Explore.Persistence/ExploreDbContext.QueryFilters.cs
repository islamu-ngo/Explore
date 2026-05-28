// ABOUTME: Partial class containing named global query filter registrations (Tenant + SoftDelete).
// ABOUTME: Entity filter registrations are grouped by domain area. Filter logic uses TenantContext closure.

using Explore.Domain;
using Explore.Domain.Modules;
using Explore.Domain.Settings.Documents;
using Explore.Domain.Views;
using Explore.Persistence.QueryFilters;
using Microsoft.EntityFrameworkCore;
using StorageObject = Explore.Domain.StorageObject;

namespace Explore.Persistence;

public partial class ExploreDbContext
{
    private void ApplyGlobalQueryFilters(ModelBuilder modelBuilder)
    {
        // ===== Event Entities =====
        // Entities with both Tenant and Soft Delete filters (separate named filters for selective disabling)
        modelBuilder.Entity<Event>()
            .HasQueryFilter(QueryFilterNames.Tenant, e => IsTenantFilterBypassed || e.TenantId == TenantFilterTenantId)
            .HasQueryFilter(QueryFilterNames.SoftDelete, e => !e.IsDeleted);

        modelBuilder.Entity<EventSession>()
            .HasQueryFilter(QueryFilterNames.Tenant, e => IsTenantFilterBypassed || e.TenantId == TenantFilterTenantId)
            .HasQueryFilter(QueryFilterNames.SoftDelete, e => !e.IsDeleted);

        modelBuilder.Entity<EventSessionGroup>()
            .HasQueryFilter(QueryFilterNames.Tenant, e => IsTenantFilterBypassed || e.TenantId == TenantFilterTenantId)
            .HasQueryFilter(QueryFilterNames.SoftDelete, e => !e.IsDeleted);

        modelBuilder.Entity<EventSeries>()
            .HasQueryFilter(QueryFilterNames.Tenant, e => IsTenantFilterBypassed || e.TenantId == TenantFilterTenantId)
            .HasQueryFilter(QueryFilterNames.SoftDelete, e => !e.IsDeleted);

        // Event-related entities (tenant only - no soft delete)
        modelBuilder.Entity<EventRegistration>()
            .HasQueryFilter(QueryFilterNames.Tenant, e => IsTenantFilterBypassed || e.TenantId == TenantFilterTenantId)
            .HasQueryFilter(QueryFilterNames.SoftDelete, e => !e.IsDeleted);
        modelBuilder.Entity<EventCategories>()
            .HasQueryFilter(QueryFilterNames.Tenant, e => IsTenantFilterBypassed || e.TenantId == TenantFilterTenantId);
        modelBuilder.Entity<EventTags>()
            .HasQueryFilter(QueryFilterNames.Tenant, e => IsTenantFilterBypassed || e.TenantId == TenantFilterTenantId);
        modelBuilder.Entity<EventSessionLanguage>()
            .HasQueryFilter(QueryFilterNames.Tenant, e => IsTenantFilterBypassed || e.TenantId == TenantFilterTenantId);
        modelBuilder.Entity<EventSessionSpeaker>()
            .HasQueryFilter(QueryFilterNames.Tenant, e => IsTenantFilterBypassed || e.TenantId == TenantFilterTenantId);
        modelBuilder.Entity<EventSessionAgendaItem>()
            .HasQueryFilter(QueryFilterNames.Tenant, e => IsTenantFilterBypassed || e.TenantId == TenantFilterTenantId);

        modelBuilder.Entity<EventSessionGroupSession>()
            .HasQueryFilter(QueryFilterNames.Tenant, e => IsTenantFilterBypassed || e.TenantId == TenantFilterTenantId)
            .HasQueryFilter(QueryFilterNames.SoftDelete, e => !e.IsDeleted);

        modelBuilder.Entity<EventRoleAssignment>()
            .HasQueryFilter(QueryFilterNames.Tenant, e => IsTenantFilterBypassed || e.TenantId == TenantFilterTenantId);

        // ===== Event Scheduling Refactor - Phase 1 additive entities =====
        modelBuilder.Entity<EventDay>()
            .HasQueryFilter(QueryFilterNames.Tenant, e => IsTenantFilterBypassed || e.TenantId == TenantFilterTenantId)
            .HasQueryFilter(QueryFilterNames.SoftDelete, e => !e.IsDeleted);

        modelBuilder.Entity<EventAgendaItem>()
            .HasQueryFilter(QueryFilterNames.Tenant, e => IsTenantFilterBypassed || e.TenantId == TenantFilterTenantId)
            .HasQueryFilter(QueryFilterNames.SoftDelete, e => !e.IsDeleted);

        modelBuilder.Entity<LocationRoom>()
            .HasQueryFilter(QueryFilterNames.Tenant, e => IsTenantFilterBypassed || e.TenantId == TenantFilterTenantId)
            .HasQueryFilter(QueryFilterNames.SoftDelete, e => !e.IsDeleted);

        modelBuilder.Entity<EventSessionCategory>()
            .HasQueryFilter(QueryFilterNames.Tenant, e => IsTenantFilterBypassed || e.TenantId == TenantFilterTenantId);

        modelBuilder.Entity<EventSessionTag>()
            .HasQueryFilter(QueryFilterNames.Tenant, e => IsTenantFilterBypassed || e.TenantId == TenantFilterTenantId);

        modelBuilder.Entity<EventRegistrationIntent>()
            .HasQueryFilter(QueryFilterNames.Tenant, e => IsTenantFilterBypassed || e.TenantId == TenantFilterTenantId)
            .HasQueryFilter(QueryFilterNames.SoftDelete, e => !e.IsDeleted);

        modelBuilder.Entity<EventContactShareConsent>()
            .HasQueryFilter(QueryFilterNames.Tenant, e => IsTenantFilterBypassed || e.TenantId == TenantFilterTenantId);

        modelBuilder.Entity<EventContactShareExport>()
            .HasQueryFilter(QueryFilterNames.Tenant, e => IsTenantFilterBypassed || e.TenantId == TenantFilterTenantId);

        modelBuilder.Entity<EmailDispatchOutbox>()
            .HasQueryFilter(QueryFilterNames.Tenant, e => IsTenantFilterBypassed || e.TenantId == TenantFilterTenantId)
            .HasQueryFilter(QueryFilterNames.SoftDelete, e => !e.IsDeleted);

        modelBuilder.Entity<EmailDispatchAttempt>()
            .HasQueryFilter(QueryFilterNames.Tenant, e => IsTenantFilterBypassed || e.TenantId == TenantFilterTenantId);

        modelBuilder.Entity<EmailDispatchReceipt>()
            .HasQueryFilter(QueryFilterNames.Tenant, e => IsTenantFilterBypassed || e.TenantId == TenantFilterTenantId);

        modelBuilder.Entity<EmailDispatchTenantControl>()
            .HasQueryFilter(QueryFilterNames.Tenant, e => IsTenantFilterBypassed || e.TenantId == TenantFilterTenantId);

        modelBuilder.Entity<EventSessionIslamicAspect>()
            .HasQueryFilter(QueryFilterNames.Tenant,
                e => IsTenantFilterBypassed
                    || (e.EventSession != null && e.EventSession.TenantId == TenantFilterTenantId));

        // Lookup extension: global event types (TenantId = null) + tenant-specific custom event types
        modelBuilder.Entity<EventType>()
            .HasQueryFilter(QueryFilterNames.Tenant,
                e => e.TenantId == null
                    || IsTenantFilterBypassed
                    || e.TenantId == TenantFilterTenantId);

        // ===== Organization Entities =====
        // Entities with both Tenant and Soft Delete filters
        modelBuilder.Entity<Organization>()
            .HasQueryFilter(QueryFilterNames.Tenant, e => IsTenantFilterBypassed || e.TenantId == TenantFilterTenantId)
            .HasQueryFilter(QueryFilterNames.SoftDelete, e => !e.IsDeleted);

        modelBuilder.Entity<OrganizationMember>()
            .HasQueryFilter(QueryFilterNames.Tenant, e => IsTenantFilterBypassed || e.TenantId == TenantFilterTenantId)
            .HasQueryFilter(QueryFilterNames.SoftDelete, e => !e.IsDeleted);

        // Organization review (tenant + soft delete)
        modelBuilder.Entity<OrganizationReview>()
            .HasQueryFilter(QueryFilterNames.Tenant, e => IsTenantFilterBypassed || e.TenantId == TenantFilterTenantId)
            .HasQueryFilter(QueryFilterNames.SoftDelete, e => !e.IsDeleted);

        modelBuilder.Entity<OrganizationSetting>()
            .HasQueryFilter(QueryFilterNames.Tenant, e => IsTenantFilterBypassed || e.TenantId == TenantFilterTenantId);

        // ===== Group Entities =====
        modelBuilder.Entity<Group>()
            .HasQueryFilter(QueryFilterNames.Tenant, e => IsTenantFilterBypassed || e.TenantId == TenantFilterTenantId)
            .HasQueryFilter(QueryFilterNames.SoftDelete, e => !e.IsDeleted);

        modelBuilder.Entity<GroupMember>()
            .HasQueryFilter(QueryFilterNames.Tenant, e => IsTenantFilterBypassed || e.TenantId == TenantFilterTenantId)
            .HasQueryFilter(QueryFilterNames.SoftDelete, e => !e.IsDeleted);

        modelBuilder.Entity<GroupSetting>()
            .HasQueryFilter(QueryFilterNames.Tenant, e => IsTenantFilterBypassed || e.TenantId == TenantFilterTenantId);

        // ===== Custom Properties (EAV) =====
        modelBuilder.Entity<CustomPropertyDefinition>()
            .HasQueryFilter(QueryFilterNames.Tenant, e => IsTenantFilterBypassed || e.TenantId == TenantFilterTenantId)
            .HasQueryFilter(QueryFilterNames.SoftDelete, e => !e.IsDeleted);

        modelBuilder.Entity<CustomPropertyOption>()
            .HasQueryFilter(QueryFilterNames.SoftDelete, e => !e.IsDeleted);

        modelBuilder.Entity<CustomPropertyValue>()
            .HasQueryFilter(QueryFilterNames.Tenant, e => IsTenantFilterBypassed || e.TenantId == TenantFilterTenantId)
            .HasQueryFilter(QueryFilterNames.SoftDelete, e => !e.IsDeleted);

        modelBuilder.Entity<EventTemplate>()
            .HasQueryFilter(QueryFilterNames.Tenant, e => IsTenantFilterBypassed || e.TenantId == TenantFilterTenantId)
            .HasQueryFilter(QueryFilterNames.SoftDelete, e => !e.IsDeleted);

        modelBuilder.Entity<EventTemplateCustomPropertyDefinition>()
            .HasQueryFilter(QueryFilterNames.Tenant, e => IsTenantFilterBypassed || e.TenantId == TenantFilterTenantId)
            .HasQueryFilter(QueryFilterNames.SoftDelete, e => !e.IsDeleted);

        modelBuilder.Entity<EventTemplateCustomPropertyOption>()
            .HasQueryFilter(QueryFilterNames.SoftDelete, e => !e.IsDeleted);

        modelBuilder.Entity<EventCustomPropertyDefinition>()
            .HasQueryFilter(QueryFilterNames.Tenant, e => IsTenantFilterBypassed || e.TenantId == TenantFilterTenantId)
            .HasQueryFilter(QueryFilterNames.SoftDelete, e => !e.IsDeleted);

        modelBuilder.Entity<EventCustomPropertyOption>()
            .HasQueryFilter(QueryFilterNames.SoftDelete, e => !e.IsDeleted);

        modelBuilder.Entity<EventCustomPropertyValue>()
            .HasQueryFilter(QueryFilterNames.Tenant, e => IsTenantFilterBypassed || e.TenantId == TenantFilterTenantId)
            .HasQueryFilter(QueryFilterNames.SoftDelete, e => !e.IsDeleted);

        modelBuilder.Entity<EventCustomPropertyProjection>()
            .HasQueryFilter(QueryFilterNames.Tenant, e => IsTenantFilterBypassed || e.TenantId == TenantFilterTenantId);

        modelBuilder.Entity<EventWithSessionsView>()
            .HasQueryFilter(QueryFilterNames.Tenant, e => IsTenantFilterBypassed || e.TenantId == TenantFilterTenantId);

        modelBuilder.Entity<CustomPropertyProjectionStatus>()
            .HasQueryFilter(QueryFilterNames.Tenant, e => IsTenantFilterBypassed || e.TenantId == TenantFilterTenantId);

        modelBuilder.Entity<CustomPropertyProjectionDirtyScope>()
            .HasQueryFilter(QueryFilterNames.Tenant, e => IsTenantFilterBypassed || e.TenantId == TenantFilterTenantId);

        // ===== Event Session EAV Entities =====
        modelBuilder.Entity<EventSessionTemplate>()
            .HasQueryFilter(QueryFilterNames.Tenant, e => IsTenantFilterBypassed || e.TenantId == TenantFilterTenantId)
            .HasQueryFilter(QueryFilterNames.SoftDelete, e => !e.IsDeleted);

        modelBuilder.Entity<EventSessionTemplateCustomPropertyDefinition>()
            .HasQueryFilter(QueryFilterNames.Tenant, e => IsTenantFilterBypassed || e.TenantId == TenantFilterTenantId)
            .HasQueryFilter(QueryFilterNames.SoftDelete, e => !e.IsDeleted);

        modelBuilder.Entity<EventSessionTemplateCustomPropertyOption>()
            .HasQueryFilter(QueryFilterNames.SoftDelete, e => !e.IsDeleted);

        modelBuilder.Entity<EventSessionCustomPropertyDefinition>()
            .HasQueryFilter(QueryFilterNames.Tenant, e => IsTenantFilterBypassed || e.TenantId == TenantFilterTenantId)
            .HasQueryFilter(QueryFilterNames.SoftDelete, e => !e.IsDeleted);

        modelBuilder.Entity<EventSessionCustomPropertyOption>()
            .HasQueryFilter(QueryFilterNames.SoftDelete, e => !e.IsDeleted);

        modelBuilder.Entity<EventSessionCustomPropertyValue>()
            .HasQueryFilter(QueryFilterNames.Tenant, e => IsTenantFilterBypassed || e.TenantId == TenantFilterTenantId)
            .HasQueryFilter(QueryFilterNames.SoftDelete, e => !e.IsDeleted);

        modelBuilder.Entity<EventSessionCustomPropertyProjection>()
            .HasQueryFilter(QueryFilterNames.Tenant, e => IsTenantFilterBypassed || e.TenantId == TenantFilterTenantId);

        // ===== Actor Entities =====
        // Entities with both Tenant and Soft Delete filters
        modelBuilder.Entity<Actor>()
            .HasQueryFilter(QueryFilterNames.Tenant, e => IsTenantFilterBypassed || e.TenantId == TenantFilterTenantId)
            .HasQueryFilter(QueryFilterNames.SoftDelete, e => !e.IsDeleted);

        modelBuilder.Entity<ActorPii>()
            .HasQueryFilter(QueryFilterNames.Tenant,
                e => IsTenantFilterBypassed
                    || (e.Actor != null && e.Actor.TenantId == TenantFilterTenantId));

        // Actor-related (tenant only - no soft delete)
        modelBuilder.Entity<ActorKeyStore>()
            .HasQueryFilter(QueryFilterNames.Tenant, e => IsTenantFilterBypassed || e.TenantId == TenantFilterTenantId);

        // ===== User Entity =====
        // Soft Delete only (not tenant-scoped - global entity)
        modelBuilder.Entity<User>()
            .HasQueryFilter(QueryFilterNames.SoftDelete, u => !u.IsDeleted);

        modelBuilder.Entity<UserPii>()
            .HasQueryFilter(QueryFilterNames.SoftDelete, e => e.User != null && !e.User.IsDeleted);

        // ===== Location Entity =====
        modelBuilder.Entity<Location>()
            .HasQueryFilter(QueryFilterNames.Tenant, e => IsTenantFilterBypassed || e.TenantId == TenantFilterTenantId);

        modelBuilder.Entity<LocationPii>()
            .HasQueryFilter(QueryFilterNames.Tenant,
                e => IsTenantFilterBypassed
                    || (e.Location != null && e.Location.TenantId == TenantFilterTenantId));

        // ===== Storage Entity =====
        modelBuilder.Entity<StorageObject>()
            .HasQueryFilter(QueryFilterNames.Tenant, e => IsTenantFilterBypassed || e.TenantId == TenantFilterTenantId);

        modelBuilder.Entity<OrganizationPii>()
            .HasQueryFilter(QueryFilterNames.Tenant,
                e => IsTenantFilterBypassed
                    || (e.Organization != null && e.Organization.TenantId == TenantFilterTenantId));

        // ===== Category and Tag Entities =====
        modelBuilder.Entity<Category>()
            .HasQueryFilter(QueryFilterNames.Tenant, e => IsTenantFilterBypassed || e.TenantId == TenantFilterTenantId);
        modelBuilder.Entity<Tag>()
            .HasQueryFilter(QueryFilterNames.Tenant, e => IsTenantFilterBypassed || e.TenantId == TenantFilterTenantId);
        modelBuilder.Entity<TagTypeTags>()
            .HasQueryFilter(QueryFilterNames.Tenant, e => IsTenantFilterBypassed || e.TenantId == TenantFilterTenantId);
        modelBuilder.Entity<CategoryTypeCategories>()
            .HasQueryFilter(QueryFilterNames.Tenant, e => IsTenantFilterBypassed || e.TenantId == TenantFilterTenantId);

        // ===== User-Related Tenant Entities =====
        modelBuilder.Entity<ExternalApiKey>()
            .HasQueryFilter(QueryFilterNames.Tenant, e => IsTenantFilterBypassed || (e.TenantId != null && e.TenantId == TenantFilterTenantId));
        modelBuilder.Entity<UserAuthenticationToken>()
            .HasQueryFilter(QueryFilterNames.Tenant, e => IsTenantFilterBypassed || e.TenantId == TenantFilterTenantId);
        modelBuilder.Entity<UserExternalLogin>()
            .HasQueryFilter(QueryFilterNames.Tenant, e => IsTenantFilterBypassed || e.TenantId == TenantFilterTenantId);
        modelBuilder.Entity<UserPreference>()
            .HasQueryFilter(QueryFilterNames.Tenant, e => IsTenantFilterBypassed || e.TenantId == TenantFilterTenantId);
        modelBuilder.Entity<UserNotificationPreference>()
            .HasQueryFilter(QueryFilterNames.Tenant, e => IsTenantFilterBypassed || e.TenantId == TenantFilterTenantId);

        // ===== Tenant Entities =====
        modelBuilder.Entity<TenantSetting>()
            .HasQueryFilter(QueryFilterNames.Tenant, e => IsTenantFilterBypassed || e.TenantId == TenantFilterTenantId);
        modelBuilder.Entity<TenantSettingsDocument>()
            .HasQueryFilter(QueryFilterNames.Tenant, e => IsTenantFilterBypassed || e.TenantId == TenantFilterTenantId);
        modelBuilder.Entity<TenantUser>()
            .HasQueryFilter(QueryFilterNames.Tenant, e => IsTenantFilterBypassed || e.TenantId == TenantFilterTenantId)
            .HasQueryFilter(QueryFilterNames.SoftDelete, e => !e.IsDeleted);
        modelBuilder.Entity<TenantUserProfile>()
            .HasQueryFilter(QueryFilterNames.Tenant, e => IsTenantFilterBypassed || e.TenantId == TenantFilterTenantId);
        modelBuilder.Entity<TenantUserRoleGrant>()
            .HasQueryFilter(QueryFilterNames.Tenant, e => IsTenantFilterBypassed || e.TenantId == TenantFilterTenantId);
        modelBuilder.Entity<TenantOnboardingState>()
            .HasQueryFilter(QueryFilterNames.Tenant, e => IsTenantFilterBypassed || e.TenantId == TenantFilterTenantId);

        modelBuilder.Entity<TenantNavigationLink>()
            .HasQueryFilter(QueryFilterNames.Tenant, e => IsTenantFilterBypassed || e.TenantId == TenantFilterTenantId)
            .HasQueryFilter(QueryFilterNames.SoftDelete, e => !e.IsDeleted);

        // Footer link groups: instance-default groups (TenantId = null) are always visible;
        // tenant-owned groups respect the tenant filter (same pattern as EventType).
        modelBuilder.Entity<TenantFooterLinkGroup>()
            .HasQueryFilter(QueryFilterNames.Tenant,
                e => e.TenantId == null
                    || IsTenantFilterBypassed
                    || e.TenantId == TenantFilterTenantId);

        // Footer links have no TenantId — isolation flows through the parent group query filter.
        // No additional filter needed; EF will respect the parent filter via navigation includes.

        modelBuilder.Entity<TenantInvitation>()
            .HasQueryFilter(QueryFilterNames.Tenant, e => IsTenantFilterBypassed || e.TenantId == TenantFilterTenantId);
        // ===== Module Governance Entities =====
        modelBuilder.Entity<TenantCapability>()
            .HasQueryFilter(QueryFilterNames.Tenant, e => IsTenantFilterBypassed || e.TenantId == TenantFilterTenantId);

        // ===== Audit & Notifications =====
        modelBuilder.Entity<AuditLog>()
            .HasQueryFilter(QueryFilterNames.Tenant, e => IsTenantFilterBypassed || e.TenantId == TenantFilterTenantId);
        modelBuilder.Entity<Notification>()
            .HasQueryFilter(QueryFilterNames.Tenant, e => IsTenantFilterBypassed || e.TenantId == TenantFilterTenantId)
            .HasQueryFilter(QueryFilterNames.SoftDelete, e => !e.IsDeleted);
    }
}
