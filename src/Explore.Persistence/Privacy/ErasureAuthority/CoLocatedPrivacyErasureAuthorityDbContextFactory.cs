// ABOUTME: Creates the co-located PostgreSQL authority context from primary migrator settings.
// ABOUTME: Keeps generated migrations aligned with the configurable primary database schema.

using Explore.Persistence.Database;
using Explore.Secrets.Configuration;
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
        IConfiguration bootstrap = new ConfigurationBuilder()
            .AddEnvironmentVariables()
            .AddCommandLine(args)
            .Build();
        IConfiguration configuration = SecretAuthorityConfiguration.Build(
            bootstrap,
            SecretAuthorityConfiguration.GetEnvironmentName(bootstrap),
            "/database",
            "/database/erasure");
        PrimaryDatabaseConnectionOptions database =
            PrimaryDatabaseConfiguration.BindMigrator(configuration);
        var options = new DbContextOptionsBuilder<CoLocatedPrivacyErasureAuthorityDbContext>();
        PrimaryDatabaseProviderComposition.ConfigureCoLocatedPrivacyErasureAuthority(
            options,
            database);
        return new CoLocatedPrivacyErasureAuthorityDbContext(options.Options);
    }
}
