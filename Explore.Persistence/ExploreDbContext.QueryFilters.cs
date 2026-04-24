// ABOUTME: Partial class containing named global query filter registrations (Tenant + SoftDelete).
// ABOUTME: 48 entity filter registrations grouped by domain area. Filter logic uses TenantContext closure.

using Explore.Domain;
using Explore.Domain.Modules;
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

        // ===== Event Scheduling Refactor - Phase 1 additive entities =====
        modelBuilder.Entity<EventDay>()
            .HasQueryFilter(QueryFilterNames.Tenant, e => TenantContext == null || e.TenantId == (TenantContext != null ? TenantContext.TenantId : Guid.Empty))
            .HasQueryFilter(QueryFilterNames.SoftDelete, e => !e.IsDeleted);

        modelBuilder.Entity<EventAgendaItem>()
            .HasQueryFilter(QueryFilterNames.Tenant, e => TenantContext == null || e.TenantId == (TenantContext != null ? TenantContext.TenantId : Guid.Empty))
            .HasQueryFilter(QueryFilterNames.SoftDelete, e => !e.IsDeleted);

        modelBuilder.Entity<LocationRoom>()
            .HasQueryFilter(QueryFilterNames.Tenant, e => TenantContext == null || e.TenantId == (TenantContext != null ? TenantContext.TenantId : Guid.Empty))
            .HasQueryFilter(QueryFilterNames.SoftDelete, e => !e.IsDeleted);

        modelBuilder.Entity<EventSessionCategory>()
            .HasQueryFilter(QueryFilterNames.Tenant, e => TenantContext == null || e.TenantId == (TenantContext != null ? TenantContext.TenantId : Guid.Empty));

        modelBuilder.Entity<EventSessionTag>()
            .HasQueryFilter(QueryFilterNames.Tenant, e => TenantContext == null || e.TenantId == (TenantContext != null ? TenantContext.TenantId : Guid.Empty));

        modelBuilder.Entity<EventRegistrationIntent>()
            .HasQueryFilter(QueryFilterNames.Tenant, e => TenantContext == null || e.TenantId == (TenantContext != null ? TenantContext.TenantId : Guid.Empty))
            .HasQueryFilter(QueryFilterNames.SoftDelete, e => !e.IsDeleted);
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

        modelBuilder.Entity<EventWithSessionsView>()
            .HasQueryFilter(QueryFilterNames.Tenant, e => TenantContext == null || e.TenantId == (TenantContext != null ? TenantContext.TenantId : Guid.Empty));

        modelBuilder.Entity<CustomPropertyProjectionStatus>()
            .HasQueryFilter(QueryFilterNames.Tenant, e => TenantContext == null || e.TenantId == (TenantContext != null ? TenantContext.TenantId : Guid.Empty));

        modelBuilder.Entity<CustomPropertyProjectionDirtyScope>()
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
}
