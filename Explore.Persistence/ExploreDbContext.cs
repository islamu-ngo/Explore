using Microsoft.EntityFrameworkCore;
using System.Data.Entity.Core.Metadata.Edm;
using System.Reflection.Metadata;
using Explore.Domain;
using Explore.Domain.Interfaces;
using Explore.Application.Contracts.Infrastructure;
using Explore.Persistence.Configurations.Entities;
using StorageObject = Explore.Domain.StorageObject;

namespace Explore.Persistence
{
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

        /// <summary>
        /// Applies Global Query Filters for multi-tenant data isolation and soft delete.
        /// When TenantContext is null (e.g., during migrations), the tenant filter is bypassed.
        /// Soft delete filters use named filters (EF Core 10+) for selective disabling.
        /// </summary>
        private void ApplyGlobalQueryFilters(ModelBuilder modelBuilder)
        {
            // ===== Event Entities =====
            // Tenant + Soft Delete filters (combined in single expression)
            modelBuilder.Entity<Event>()
                .HasQueryFilter(e => (TenantContext == null || e.TenantId == TenantContext.TenantId) && !e.IsDeleted);

            modelBuilder.Entity<EventSession>()
                .HasQueryFilter(e => (TenantContext == null || e.TenantId == TenantContext.TenantId) && !e.IsDeleted);

            // Other event-related entities (tenant only - no soft delete yet)
            modelBuilder.Entity<EventRegistration>().HasQueryFilter(e => TenantContext == null || e.TenantId == TenantContext.TenantId);
            modelBuilder.Entity<EventCategories>().HasQueryFilter(e => TenantContext == null || e.TenantId == TenantContext.TenantId);
            modelBuilder.Entity<EventTags>().HasQueryFilter(e => TenantContext == null || e.TenantId == TenantContext.TenantId);
            modelBuilder.Entity<EventSessionLanguage>().HasQueryFilter(e => TenantContext == null || e.TenantId == TenantContext.TenantId);
            modelBuilder.Entity<EventSessionSpeaker>().HasQueryFilter(e => TenantContext == null || e.TenantId == TenantContext.TenantId);
            modelBuilder.Entity<EventSessionAgendaItem>().HasQueryFilter(e => TenantContext == null || e.TenantId == TenantContext.TenantId);

            // ===== Organization Entities =====
            // Tenant + Soft Delete filters (combined in single expression)
            modelBuilder.Entity<Organization>()
                .HasQueryFilter(e => (TenantContext == null || e.TenantId == TenantContext.TenantId) && !e.IsDeleted);

            modelBuilder.Entity<OrganizationMember>()
                .HasQueryFilter(e => (TenantContext == null || e.TenantId == TenantContext.TenantId) && !e.IsDeleted);

            // Organization review (tenant only - no soft delete yet)
            modelBuilder.Entity<OrganizationReview>().HasQueryFilter(e => TenantContext == null || e.TenantId == TenantContext.TenantId);

            // ===== Actor Entities =====
            // Tenant + Soft Delete filters (combined in single expression)
            modelBuilder.Entity<Actor>()
                .HasQueryFilter(e => (TenantContext == null || e.TenantId == TenantContext.TenantId) && !e.IsDeleted);

            modelBuilder.Entity<ActorKeyStore>().HasQueryFilter(e => TenantContext == null || e.TenantId == TenantContext.TenantId);

            // ===== User Entity =====
            // Soft Delete only (not tenant-scoped)
            modelBuilder.Entity<User>()
                .HasQueryFilter(u => !u.IsDeleted);

            // ===== Location Entity =====
            modelBuilder.Entity<Location>().HasQueryFilter(e => TenantContext == null || e.TenantId == TenantContext.TenantId);

            // ===== Storage Entity =====
            modelBuilder.Entity<StorageObject>().HasQueryFilter(e => TenantContext == null || e.TenantId == TenantContext.TenantId);

            // ===== Category and Tag Entities =====
            modelBuilder.Entity<Category>().HasQueryFilter(e => TenantContext == null || e.TenantId == TenantContext.TenantId);
            modelBuilder.Entity<Tag>().HasQueryFilter(e => TenantContext == null || e.TenantId == TenantContext.TenantId);
            modelBuilder.Entity<TagTypeTags>().HasQueryFilter(e => TenantContext == null || e.TenantId == TenantContext.TenantId);

            // ===== User-Related Tenant Entities =====
            modelBuilder.Entity<UserAuthenticationToken>().HasQueryFilter(e => TenantContext == null || e.TenantId == TenantContext.TenantId);
            modelBuilder.Entity<UserExternalLogin>().HasQueryFilter(e => TenantContext == null || e.TenantId == TenantContext.TenantId);
            modelBuilder.Entity<UserRole>().HasQueryFilter(e => TenantContext == null || e.TenantId == TenantContext.TenantId);

            // ===== Tenant Entities =====
            modelBuilder.Entity<TenantUser>().HasQueryFilter(e => TenantContext == null || e.TenantId == TenantContext.TenantId);
            modelBuilder.Entity<TenantSettings>().HasQueryFilter(e => TenantContext == null || e.TenantId == TenantContext.TenantId);
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
        public DbSet<TenantUser> TenantUsers { get; set; }
        public DbSet<TenantSettings> TenantSettings { get; set; }

        // ===== Users & Authentication =====
        public DbSet<User> Users { get; set; }
        public DbSet<UserRole> UserRoles { get; set; }
        public DbSet<UserAuthenticationToken> UserAuthenticationTokens { get; set; }
        public DbSet<UserExternalLogin> UserExternalLogins { get; set; }

        // ===== Actors (Federation/ATProto) =====
        public DbSet<Actor> Actors { get; set; }
        public DbSet<ActorType> ActorTypes { get; set; }
        public DbSet<DidCustodyType> DidCustodyTypes { get; set; }
        public DbSet<ActorKeyStore> ActorKeyStores { get; set; }

        // ===== Organizations =====
        public DbSet<Organization> Organizations { get; set; }
        public DbSet<OrganizationMember> OrganizationMembers { get; set; }
        public DbSet<OrganizationRole> OrganizationRoles { get; set; }
        public DbSet<OrganizationPosition> OrganizationPositions { get; set; }
        public DbSet<OrganizationReview> OrganizationReviews { get; set; }

        // ===== Events =====
        public DbSet<Event> Events { get; set; }
        public DbSet<EventSession> EventSessions { get; set; }
        public DbSet<EventRegistration> EventRegistrations { get; set; }
        public DbSet<EventSessionLanguage> EventSessionLanguages { get; set; }
        public DbSet<EventSessionSpeaker> EventSessionSpeakers { get; set; }
        public DbSet<EventSessionAgendaItem> EventSessionAgendaItems { get; set; }

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

        // ===== Categories & Tags =====
        public DbSet<Category> Categories { get; set; }
        public DbSet<Tag> Tags { get; set; }
        public DbSet<TagType> TagTypes { get; set; }
        public DbSet<TagTypeTags> TagTypeTags { get; set; }
        public DbSet<EventCategories> EventCategories { get; set; }
        public DbSet<EventTags> EventTags { get; set; }

        // ===== Locations =====
        public DbSet<Location> Locations { get; set; }

        // ===== Storage =====
        public DbSet<StorageObject> StorageObjects { get; set; }
        public DbSet<FileType> FileTypes { get; set; }
        public DbSet<OwnerType> OwnerTypes { get; set; }

        // ===== Federation/Indexer (ATProto) =====
        public DbSet<IndexedDid> IndexedDids { get; set; }
        public DbSet<SyncState> SyncStates { get; set; }
        public DbSet<AtprotoRecord> AtprotoRecords { get; set; }
    }
}
