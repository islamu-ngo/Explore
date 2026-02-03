// ABOUTME: Configuration extensions for the Migration Service project.
// Adds Infisical as configuration source and maps PostgreSQL connection string.

namespace Event.MigrationService.Extensions;

using Explore.Secrets.Extensions;

public static class ConfigurationExtensions
{
    /// <summary>
    /// Adds Infisical secrets and applies compatibility mapping for the migration service.
    /// </summary>
    /// <remarks>
    /// This method:
    /// 1. Loads bootstrap credentials from user secrets (Infisical:ClientId, etc.)
    /// 2. Connects to Infisical and loads secrets from the /postgresql path
    /// 3. Maps POSTGRESQL_PUBLIC_URL to ConnectionStrings:EventMigrationService
    ///
    /// Required user secrets:
    /// - Infisical:Url (optional, defaults to app.infisical.com)
    /// - Infisical:ProjectId (required)
    /// - Infisical:ClientId (required)
    /// - Infisical:ClientSecret (required)
    /// - Infisical:Environment (optional, defaults to "dev")
    /// </remarks>
    public static void AddInfisicalMigrationCompatibility(this IConfigurationBuilder configBuilder)
    {
        // Build temporary config to read bootstrap credentials (from user secrets/env vars)
        var bootstrapConfig = configBuilder.Build();

        // Add Infisical as configuration source (loads secrets from Infisical service)
        // This uses credentials from user secrets to authenticate with Infisical
        configBuilder.AddInfisical(bootstrapConfig, source =>
        {
            // Configure paths to load from Infisical - only need postgresql for migrations
            source.Paths.Clear();
            source.Paths.AddRange(["/postgresql"]);

            // Don't fail if Infisical isn't configured (allows local dev without Infisical)
            source.ThrowOnFirstLoadFailure = false;
        });

        // Rebuild config after Infisical secrets are added
        var configWithSecrets = configBuilder.Build();

        // Apply compatibility mapping for connection string
        ApplyCompatibilityMapping(configBuilder, configWithSecrets);
    }

    /// <summary>
    /// Maps Infisical secret names to .NET configuration keys.
    /// </summary>
    /// <remarks>
    /// This translates between:
    /// - Infisical naming: POSTGRESQL_PUBLIC_URL
    /// - Aspire naming: ConnectionStrings:EventMigrationService
    /// </remarks>
    private static void ApplyCompatibilityMapping(IConfigurationBuilder configBuilder, IConfiguration config)
    {
        // Read database URL from Infisical or environment
        var rawDbUrl = config["POSTGRESQL_PUBLIC_URL"] 
            ?? config["ConnectionStrings:DefaultConnection"]
            ?? config["ConnectionStrings:EventMigrationService"];

        // Log configuration for debugging
        Console.WriteLine("===========================================");
        Console.WriteLine("Migration Service Configuration (from Infisical):");
        Console.WriteLine($"  Database URL: {(string.IsNullOrEmpty(rawDbUrl) ? "NOT SET!" : "****" + rawDbUrl[Math.Max(0, rawDbUrl.Length - 20)..])}");
        Console.WriteLine("===========================================");

        // Create mapping dictionary
        var mappedConfig = new Dictionary<string, string?>();

        // Map Database connection string for Aspire's AddNpgsqlDbContext
        // Aspire looks for ConnectionStrings:EventMigrationService (based on the name passed to AddNpgsqlDbContext)
        if (!string.IsNullOrEmpty(rawDbUrl))
        {
            mappedConfig["ConnectionStrings:EventMigrationService"] = rawDbUrl;
            // Also set DefaultConnection for compatibility
            mappedConfig["ConnectionStrings:DefaultConnection"] = rawDbUrl;
        }

        // Inject mapped configuration
        configBuilder.AddInMemoryCollection(
            mappedConfig.Where(kv => !string.IsNullOrEmpty(kv.Value))
                        .ToDictionary(kv => kv.Key, kv => kv.Value)!
        );
    }
}
