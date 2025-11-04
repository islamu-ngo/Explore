using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Explore.Persistence
{
    public class ExploreDbContextFactory : IDesignTimeDbContextFactory<ExploreDbContext>
    {
        public ExploreDbContext CreateDbContext(string[] args)
        {
            var optionsBuilder = new DbContextOptionsBuilder<ExploreDbContext>();

            // Chaîne factice uniquement pour la génération de migrations
            optionsBuilder.UseNpgsql(
                    "",
                    b => b.MigrationsAssembly("Explore.Persistence"))
                .UseSnakeCaseNamingConvention();

            return new ExploreDbContext(optionsBuilder.Options);
        }
    }
}
