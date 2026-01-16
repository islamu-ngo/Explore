using Explore.Domain;
using Explore.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace Explore.Persistence.Seed
{
    /// <summary>
    /// Async seeding for data that requires conditional logic or depends on runtime state.
    /// Most seed data is now in entity configurations using HasData().
    /// This class is for complex seeding scenarios that can't be done with HasData().
    /// </summary>
    public static class DatabaseSeeder
    {
        /// <summary>
        /// Seeds the database with initial data if not already present.
        /// Called after migrations in the MigrationService.
        /// </summary>
        public static async Task SeedAsync(ExploreDbContext context, CancellationToken cancellationToken = default)
        {
            // Most seeding is done via HasData() in entity configurations.
            // This method is for runtime-dependent or complex seeding scenarios only.
            
            // Currently all seeding is handled via HasData() in configurations.
            // Add any runtime-dependent seeding here if needed in the future.
            
            await context.SaveChangesAsync(cancellationToken);
        }
    }
}
