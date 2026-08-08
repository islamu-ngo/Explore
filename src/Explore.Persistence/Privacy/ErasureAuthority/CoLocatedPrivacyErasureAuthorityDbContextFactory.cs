// ABOUTME: Creates the co-located PostgreSQL authority context from primary migrator settings.
// ABOUTME: Keeps generated migrations aligned with the configurable primary database schema.

using Explore.Persistence.Database;
using Explore.Secrets.Database;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;

namespace Explore.Persistence.Privacy.ErasureAuthority;

public sealed class CoLocatedPrivacyErasureAuthorityDbContextFactory
    : IDesignTimeDbContextFactory<CoLocatedPrivacyErasureAuthorityDbContext>
{
    public CoLocatedPrivacyErasureAuthorityDbContext CreateDbContext(string[] args)
    {
        IConfiguration configuration = new ConfigurationBuilder()
            .AddUserSecrets<CoLocatedPrivacyErasureAuthorityDbContextFactory>(optional: true)
            .AddEnvironmentVariables()
            .AddCommandLine(args)
            .Build();
        PrimaryDatabaseConnectionOptions database =
            PrimaryDatabaseConfiguration.BindMigrator(configuration);
        var options = new DbContextOptionsBuilder<CoLocatedPrivacyErasureAuthorityDbContext>();
        PrimaryDatabaseProviderComposition.ConfigureCoLocatedPrivacyErasureAuthority(
            options,
            database);
        return new CoLocatedPrivacyErasureAuthorityDbContext(options.Options);
    }
}
