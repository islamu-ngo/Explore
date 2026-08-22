// ABOUTME: Configuration extensions for the Aspire Migration Service worker.
// ABOUTME: Resolves structured migrator database settings and publishes process-local connection strings.

using Explore.Secrets.Database;

namespace Event.MigrationService.Extensions;

public static class ConfigurationExtensions
{
    /// <summary>
    /// Resolves the migration service's database connection string from structured settings
    /// and exposes it under the Aspire-expected connection-string keys.
    /// </summary>
    /// <remarks>
    /// The shared resolver owns provider validation and provider-native string construction.
    /// </remarks>
    public static PrimaryDatabaseConnectionOptions AddPrimaryDatabaseBootstrap(this IConfigurationBuilder configBuilder)
    {
        var existingConfig = configBuilder.Build();
        var databaseOptions = PrimaryDatabaseConfiguration.BindMigrator(existingConfig);
        var database = PrimaryDatabaseConfiguration.BuildConnectionString(databaseOptions);

        Console.WriteLine("===========================================");
        Console.WriteLine("Migration Service database bootstrap:");
        Console.WriteLine($"  Source: {database.SafeSummary}");
        Console.WriteLine("===========================================");

        configBuilder.AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["ConnectionStrings:EventMigrationService"] = database.ConnectionString,
            ["ConnectionStrings:DefaultConnection"] = database.ConnectionString,
        });

        return databaseOptions;
    }
}
