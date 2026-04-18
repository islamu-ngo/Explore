// ABOUTME: Configuration extensions for the Aspire Migration Service worker.
// Resolves discrete POSTGRESQL_* secrets via BootstrapSecretLoader - no URL form.

namespace Event.MigrationService.Extensions;

using Explore.Secrets.Bootstrap;

public static class ConfigurationExtensions
{
    /// <summary>
    /// Resolves the migration service's Postgres connection string from discrete secrets
    /// (Infisical <c>/postgresql</c> folder, <c>POSTGRESQL_*</c> env vars, or <c>Postgresql:*</c>
    /// configuration) and exposes it under the Aspire-expected connection-string keys.
    /// </summary>
    /// <remarks>
    /// Resolution order is owned entirely by <see cref="BootstrapSecretLoader"/>:
    /// <list type="number">
    ///   <item>Infisical <c>/postgresql</c> folder (when <c>SecretProvider:Infisical:*</c> bootstrap creds are present).</item>
    ///   <item>Environment variables <c>POSTGRESQL_HOST</c> / <c>POSTGRESQL_PORT</c> / <c>POSTGRESQL_DATABASE</c> / <c>POSTGRESQL_USERNAME</c> / <c>POSTGRESQL_PASSWORD</c>.</item>
    ///   <item>Configuration section <c>Postgresql:Host|Port|Database|Username|Password</c>.</item>
    /// </list>
    /// If <c>ConnectionStrings:EventMigrationService</c> is already set (for example by Aspire
    /// resource wiring or by integration-test fixtures), it is used verbatim and the loader is skipped.
    /// </remarks>
    public static void AddDiscretePostgresBootstrap(this IConfigurationBuilder configBuilder)
    {
        var existingConfig = configBuilder.Build();

        var existingConnectionString = existingConfig["ConnectionStrings:EventMigrationService"]
            ?? existingConfig["ConnectionStrings:DefaultConnection"];

        string connectionString;
        string source;

        if (!string.IsNullOrWhiteSpace(existingConnectionString))
        {
            connectionString = existingConnectionString;
            source = "ConnectionStrings (explicit)";
        }
        else
        {
            var credentials = BootstrapSecretLoader.LoadPostgresConnectionString(existingConfig, logger: null);
            connectionString = credentials.ConnectionString;
            source = credentials.Source;
        }

        Console.WriteLine("===========================================");
        Console.WriteLine("Migration Service Postgres bootstrap:");
        Console.WriteLine($"  Source: {source}");
        Console.WriteLine("===========================================");

        configBuilder.AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["ConnectionStrings:EventMigrationService"] = connectionString,
            ["ConnectionStrings:DefaultConnection"] = connectionString,
        });
    }
}
