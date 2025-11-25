using Microsoft.EntityFrameworkCore;
using System.Data.Entity.Core.Metadata.Edm;
using System.Reflection.Metadata;
using Explore.Domain;
using Explore.Persistence.Configurations.Entities;
using StorageObject = Explore.Domain.StorageObject;

namespace Explore.Persistence
{
    public class ExploreDbContext : DbContext
    {
        public ExploreDbContext(DbContextOptions<ExploreDbContext> options) : base(options)
        {
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Use TPT strategy so no program table with discriminator as it is by default!
            //modelBuilder.ApplyConfiguration(new AudienceAgeConfiguration());
            //modelBuilder.ApplyConfiguration(new AudienceGenderConfiguration());
            //modelBuilder.ApplyConfiguration(new EducationConfiguration());
            //modelBuilder.ApplyConfiguration(new EducationTypeConfiguration());
            //modelBuilder.ApplyConfiguration(new EventConfiguration());
            //modelBuilder.ApplyConfiguration(new EventTypeConfiguration());
            //modelBuilder.ApplyConfiguration(new OrganizationConfiguration());
            //modelBuilder.ApplyConfiguration(new OrganizationMemberConfiguration());
            //modelBuilder.ApplyConfiguration(new ProgramConfiguration());
            //modelBuilder.ApplyConfiguration(new ProgramRegistrationConfiguration());
            //modelBuilder.ApplyConfiguration(new ProgramTypeConfiguration());
            //modelBuilder.ApplyConfiguration(new StatusTypeConfiguration());
            //modelBuilder.ApplyConfiguration(new StorageObjectConfiguration());

            modelBuilder.ApplyConfigurationsFromAssembly(typeof(ExploreDbContext).Assembly);
        }

        public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            foreach (var entry in ChangeTracker.Entries())
            {
                //ActionType actionType = ActionType.Update;

                if (entry.State == EntityState.Added)
                {
                    //actionType = ActionType.Create;
                }
                //var logMessage = CreateLogMessage(entry, actionType);
                //LogHelper.Log(logMessage); // Assuming LogHelper has a static Log method
            }

            return base.SaveChangesAsync(cancellationToken);
        }

        public DbSet<AudienceAge> AudienceAges { get; set; }
        public DbSet<AudienceGender> AudienceGenders { get; set; }
        public DbSet<Education> Educations { get; set; }
        public DbSet<EducationType> EducationTypes { get; set; }
        public DbSet<Event> Events { get; set; }
        public DbSet<EventType> EventTypes { get; set; }
        public DbSet<StorageObject> Files { get; set; }
        public DbSet<Organization> Organizations { get; set; }
        public DbSet<OrganizationMember> OrganizationMembers { get; set; }
        public DbSet<Program> Programs { get; set; }
        public DbSet<ProgramRegistartion> ProgramRegistartions { get; set; }
        public DbSet<ProgramType> ProgramTypes { get; set; }
        public DbSet<StatusType> StatusTypes { get; set; }
        public DbSet<User> Users { get; set; }
    }
}
