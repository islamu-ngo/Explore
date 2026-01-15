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
        private readonly ITenantContext? _tenantContext;

        public ExploreDbContext(DbContextOptions<ExploreDbContext> options) : base(options)
        {
        }

        public ExploreDbContext(DbContextOptions<ExploreDbContext> options, ITenantContext? tenantContext) : base(options)
        {
            _tenantContext = tenantContext;
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
        /// Applies Global Query Filters for multi-tenant data isolation.
        /// When _tenantContext is null (e.g., during migrations), the filter is bypassed.
        /// </summary>
        private void ApplyGlobalQueryFilters(ModelBuilder modelBuilder)
        {
            // Event entities
            modelBuilder.Entity<Event>().HasQueryFilter(e => _tenantContext == null || e.TenantId == _tenantContext.TenantId);
            modelBuilder.Entity<EventSession>().HasQueryFilter(e => _tenantContext == null || e.TenantId == _tenantContext.TenantId);
            modelBuilder.Entity<EventRegistration>().HasQueryFilter(e => _tenantContext == null || e.TenantId == _tenantContext.TenantId);
            modelBuilder.Entity<EventCategories>().HasQueryFilter(e => _tenantContext == null || e.TenantId == _tenantContext.TenantId);
            modelBuilder.Entity<EventTags>().HasQueryFilter(e => _tenantContext == null || e.TenantId == _tenantContext.TenantId);
            modelBuilder.Entity<EventSessionLanguage>().HasQueryFilter(e => _tenantContext == null || e.TenantId == _tenantContext.TenantId);
            modelBuilder.Entity<EventSessionSpeaker>().HasQueryFilter(e => _tenantContext == null || e.TenantId == _tenantContext.TenantId);
            modelBuilder.Entity<EventSessionAgendaItem>().HasQueryFilter(e => _tenantContext == null || e.TenantId == _tenantContext.TenantId);

            // Organization entities
            modelBuilder.Entity<Organization>().HasQueryFilter(e => _tenantContext == null || e.TenantId == _tenantContext.TenantId);
            modelBuilder.Entity<OrganizationReview>().HasQueryFilter(e => _tenantContext == null || e.TenantId == _tenantContext.TenantId);
            modelBuilder.Entity<OrganizationMember>().HasQueryFilter(e => _tenantContext == null || e.TenantId == _tenantContext.TenantId);

            // Actor entities
            modelBuilder.Entity<Actor>().HasQueryFilter(e => _tenantContext == null || e.TenantId == _tenantContext.TenantId);
            modelBuilder.Entity<ActorKeyStore>().HasQueryFilter(e => _tenantContext == null || e.TenantId == _tenantContext.TenantId);

            // Location entity
            modelBuilder.Entity<Location>().HasQueryFilter(e => _tenantContext == null || e.TenantId == _tenantContext.TenantId);

            // Storage entity
            modelBuilder.Entity<StorageObject>().HasQueryFilter(e => _tenantContext == null || e.TenantId == _tenantContext.TenantId);

            // Category and Tag entities
            modelBuilder.Entity<Category>().HasQueryFilter(e => _tenantContext == null || e.TenantId == _tenantContext.TenantId);
            modelBuilder.Entity<Tag>().HasQueryFilter(e => _tenantContext == null || e.TenantId == _tenantContext.TenantId);
            modelBuilder.Entity<TagTypeTags>().HasQueryFilter(e => _tenantContext == null || e.TenantId == _tenantContext.TenantId);

            // User-related tenant entities
            modelBuilder.Entity<UserAuthenticationToken>().HasQueryFilter(e => _tenantContext == null || e.TenantId == _tenantContext.TenantId);
            modelBuilder.Entity<UserExternalLogin>().HasQueryFilter(e => _tenantContext == null || e.TenantId == _tenantContext.TenantId);
            modelBuilder.Entity<UserRole>().HasQueryFilter(e => _tenantContext == null || e.TenantId == _tenantContext.TenantId);

            // Tenant entities (scoped by tenant)
            modelBuilder.Entity<TenantUser>().HasQueryFilter(e => _tenantContext == null || e.TenantId == _tenantContext.TenantId);
            modelBuilder.Entity<TenantSettings>().HasQueryFilter(e => _tenantContext == null || e.TenantId == _tenantContext.TenantId);
        }

        public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            foreach (var entry in ChangeTracker.Entries())
            {
                if (entry.State == EntityState.Added)
                {
                    //actionType = ActionType.Create;
                    // Could add audit logging here
                }
                //var logMessage = CreateLogMessage(entry, actionType);
                //LogHelper.Log(logMessage); // Assuming LogHelper has a static Log method
            }

            return base.SaveChangesAsync(cancellationToken);
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
