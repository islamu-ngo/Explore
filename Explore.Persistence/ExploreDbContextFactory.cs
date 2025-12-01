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
                    "Host=79.72.94.126;Port=5431;Database=explore_db;Username=postgres;Password=7B9kJfVvkgzQIQ48qWsiVqSIvbONpEFSzCu5nLg27CncrYkjVWLnsISAMjMLSiJv;SSL Mode=Prefer;Trust Server Certificate=true",
                    b => b.MigrationsAssembly("Explore.Persistence"))
                .UseSnakeCaseNamingConvention();

            return new ExploreDbContext(optionsBuilder.Options);
        }
    }
}
