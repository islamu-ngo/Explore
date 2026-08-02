// ABOUTME: Creates the narrow authority context from structured migrator settings.
// ABOUTME: Uses the same validated PostgreSQL contract as runtime and migration composition.

using Explore.Secrets.Database;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;

namespace Explore.Persistence.Privacy.ErasureAuthority;

public sealed class PrivacyErasureAuthorityDbContextFactory
    : IDesignTimeDbContextFactory<PrivacyErasureAuthorityDbContext>
{
    public PrivacyErasureAuthorityDbContext CreateDbContext(string[] args)
    {
        var configurationBuilder = new ConfigurationBuilder()
            .AddUserSecrets<PrivacyErasureAuthorityDbContextFactory>(optional: true)
            .AddEnvironmentVariables()
            .AddCommandLine(args);
        return CreateDbContext(configurationBuilder.Build());
    }

    public PrivacyErasureAuthorityDbContext CreateDbContext(IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(configuration);
        var database = PrivacyErasureAuthorityDatabaseConfiguration
            .ResolveMigratorConnectionString(configuration);

        var options = new DbContextOptionsBuilder<PrivacyErasureAuthorityDbContext>()
            .UseNpgsql(
                database.ConnectionString,
                npgsql => npgsql
                    .MigrationsAssembly("Explore.Persistence")
                    .MigrationsHistoryTable(
                        PrivacyErasureAuthorityDatabaseConfiguration.MigrationsHistoryTable))
            .UseSnakeCaseNamingConvention()
            .Options;
        return new PrivacyErasureAuthorityDbContext(options);
    }
}
