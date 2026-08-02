// ABOUTME: Design-time factory for EF Core migrations/scaffolding.
// ABOUTME: Resolves structured migrator settings through PrimaryDatabaseConfiguration before any provider registration.

using Explore.Persistence.Database;
using Explore.Secrets.Bootstrap;
using Explore.Secrets.Database;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;

namespace Explore.Persistence;

/// <summary>
/// Design-time factory for <see cref="ExploreDbContext"/> used by the EF Core tooling
/// (<c>dotnet ef migrations add</c>, <c>dotnet ef database update</c>, etc.).
/// </summary>
/// <remarks>
/// Explicit structured <c>Database:Migrator</c> settings take priority. Discrete PostgreSQL
/// or Infisical fields are projected into that same structured contract when needed.
/// </remarks>
public class ExploreDbContextFactory : IDesignTimeDbContextFactory<ExploreDbContext>
{
    public ExploreDbContext CreateDbContext(string[] args)
    {
        var configurationBuilder = new ConfigurationBuilder()
            .AddUserSecrets<ExploreDbContextFactory>(optional: true)
            .AddEnvironmentVariables();

        return CreateDbContext(configurationBuilder);
    }

    public ExploreDbContext CreateDbContext(IConfigurationBuilder configurationBuilder)
    {
        ArgumentNullException.ThrowIfNull(configurationBuilder);
        BootstrapSecretLoader.ProjectPostgresConfiguration(
            configurationBuilder,
            PrimaryDatabaseRole.Migrator);
        var configuration = configurationBuilder.Build();

        var databaseOptions = PrimaryDatabaseConfiguration.BindMigrator(configuration);

        var optionsBuilder = new DbContextOptionsBuilder<ExploreDbContext>();
        var database = PrimaryDatabaseProviderComposition.ConfigureApplication(
            optionsBuilder,
            databaseOptions);

        Console.WriteLine($"[DesignTime] Database bootstrap source: {database.SafeSummary}");

        return new ExploreDbContext(optionsBuilder.Options);
    }
}
