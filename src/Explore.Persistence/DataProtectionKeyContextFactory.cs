// ABOUTME: Design-time factory for DataProtectionKeyContext migrations and schema updates.
// ABOUTME: Reuses the same structured migrator resolution as the primary ExploreDbContext factory.

using Explore.Persistence.Database;
using Explore.Secrets.Bootstrap;
using Explore.Secrets.Database;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;

namespace Explore.Persistence;

public sealed class DataProtectionKeyContextFactory : IDesignTimeDbContextFactory<DataProtectionKeyContext>
{
    public DataProtectionKeyContext CreateDbContext(string[] args)
    {
        var configurationBuilder = new ConfigurationBuilder()
            .AddUserSecrets<DataProtectionKeyContextFactory>(optional: true)
            .AddEnvironmentVariables();

        return CreateDbContext(configurationBuilder);
    }

    public DataProtectionKeyContext CreateDbContext(IConfigurationBuilder configurationBuilder)
    {
        ArgumentNullException.ThrowIfNull(configurationBuilder);
        BootstrapSecretLoader.ProjectPostgresConfiguration(
            configurationBuilder,
            PrimaryDatabaseRole.Migrator);
        var configuration = configurationBuilder.Build();

        var databaseOptions = PrimaryDatabaseConfiguration.BindMigrator(configuration);

        var optionsBuilder = new DbContextOptionsBuilder<DataProtectionKeyContext>();
        var database = PrimaryDatabaseProviderComposition.ConfigureDataProtection(
            optionsBuilder,
            databaseOptions);

        Console.WriteLine($"[DesignTime] Data Protection database bootstrap source: {database.SafeSummary}");

        return new DataProtectionKeyContext(optionsBuilder.Options);
    }
}
