using Microsoft.EntityFrameworkCore;
using System.Data.Entity.Core.Metadata.Edm;
using System.Reflection.Metadata;

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

            //modelBuilder.ApplyConfiguration(new CategoryConfiguration());
            //modelBuilder.ApplyConfiguration(new TagConfiguration());
            //modelBuilder.ApplyConfiguration(new UserAccountConfiguration());
            //modelBuilder.ApplyConfiguration(new BlobFileConfiguration());

            modelBuilder.ApplyConfigurationsFromAssembly(typeof(ExploreDbContext).Assembly);
        }

        //public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        //{
        //    foreach (var entry in ChangeTracker.Entries())
        //    {
        //        ActionType actionType = ActionType.Update;

        //        if (entry.State == EntityState.Added)
        //        {
        //            actionType = ActionType.Create;
        //        }
        //        //var logMessage = CreateLogMessage(entry, actionType);
        //        //LogHelper.Log(logMessage); // Assuming LogHelper has a static Log method
        //    }

        //    return base.SaveChangesAsync(cancellationToken);
        //}

        //public DbSet<UserAccount> UserAccounts { get; set; }
    }
}
