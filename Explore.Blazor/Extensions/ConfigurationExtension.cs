// ABOUTME: Configuration extensions for the Blazor Server project.
// ABOUTME: Adds Infisical as configuration source and provides compatibility mapping for environment variables.

using Explore.Secrets.Extensions;
using Microsoft.Extensions.Configuration;

namespace Explore.Blazor.Extensions;

public static class ConfigurationExtensions
{
    /// <summary>
    /// Adds Infisical secrets and applies compatibility mapping for Blazor Server.
    /// </summary>
    /// <remarks>
    /// This method:
    /// 1. Loads bootstrap credentials from user secrets (Infisical:ClientId, etc.)
    /// 2. Connects to Infisical and loads all secrets from configured paths
    /// 3. Applies compatibility mapping for legacy environment variable names
    ///
    /// Required user secrets:
    /// - Infisical:Url (optional, defaults to app.infisical.com)
    /// - Infisical:ProjectId (required)
    /// - Infisical:ClientId (required)
    /// - Infisical:ClientSecret (required)
    /// - Infisical:Environment (optional, defaults to "dev")
    /// </remarks>
    public static void AddInfisicalBlazorCompatibility(this IConfigurationBuilder configBuilder)
    {
        // Build temporary config to read bootstrap credentials (from user secrets/env vars)
        var bootstrapConfig = configBuilder.Build();

        // NOTE: Console.WriteLine is intentional here - this runs during IConfigurationBuilder setup,
        // before the DI container and ILogger are available. These are minimal, non-sensitive diagnostics.
        Console.WriteLine("[Blazor Infisical] Checking bootstrap credentials...");
        Console.WriteLine($"[Blazor Infisical] ProjectId: {(string.IsNullOrEmpty(bootstrapConfig["Infisical:ProjectId"]) ? "(not set)" : "(set)")}");
        Console.WriteLine($"[Blazor Infisical] ClientId: {(string.IsNullOrEmpty(bootstrapConfig["Infisical:ClientId"]) ? "(not set)" : "(set)")}");
        Console.WriteLine($"[Blazor Infisical] HasClientSecret: {!string.IsNullOrEmpty(bootstrapConfig["Infisical:ClientSecret"])}");

        // Add Infisical as configuration source (loads secrets from Infisical service)
        configBuilder.AddInfisical(bootstrapConfig, source =>
        {
            // Configure paths to load from Infisical
            source.Paths.Clear();
            source.Paths.AddRange(["/keycloak", "/blazor", "/api"]);

            // Don't fail if Infisical isn't configured (allows local dev without Infisical)
            source.ThrowOnFirstLoadFailure = false;
        });

        // Rebuild config after Infisical secrets are added
        var configWithSecrets = configBuilder.Build();

        // Apply compatibility mapping for environment variable names
        ApplyBlazorCompatibilityMapping(configBuilder, configWithSecrets);
    }

    /// <summary>
    /// Maps Infisical secret names to .NET configuration keys for Blazor Server.
    /// </summary>
    private static void ApplyBlazorCompatibilityMapping(IConfigurationBuilder configBuilder, IConfiguration config)
    {
        // Read values (from Infisical, environment, or existing config)
        var rawRealm = config["Keycloak:Realm"] ?? config["KEYCLOAK_REALM"] ?? "islamu-dev";
        var rawClientSecret = config["EXPLORE_BLAZOR_SERVER_CLIENT_SECRET_COOLIFY"]
            ?? config["EXPLORE_BLAZOR_SERVER_CLIENT_SECRET"]
            ?? config["KEYCLOAK_BLAZOR_CLIENT_SECRET"]
            ?? config["Keycloak:ClientSecret"];
        var rawApiUrl = config["EXPLORE_API_BASE_URL"] ?? config["ExploreApi:BaseUrl"];
        var baseUrl = config["KEYCLOAK_PUBLIC_URL"]
            ?? config["KEYCLOAK_BASE_URL"]
            ?? config["Keycloak:BaseUrl"]
            ?? "https://keycloak.openislamu.org";
        var explicitAuthority = config["Keycloak:Authority"];

        // Construct derived values
        var keycloakAuthority = !string.IsNullOrEmpty(explicitAuthority)
            ? explicitAuthority
            : $"{baseUrl.TrimEnd('/')}/realms/{rawRealm}";
        var metadataAddress = $"{keycloakAuthority}/.well-known/openid-configuration";

        // Log non-sensitive configuration summary for startup diagnostics
        // NOTE: Console.WriteLine is intentional - ILogger is not yet available during configuration setup
        Console.WriteLine("[Blazor Infisical] Keycloak configuration mapped:");
        Console.WriteLine($"  Authority: {keycloakAuthority}");
        Console.WriteLine($"  HasClientSecret: {!string.IsNullOrEmpty(rawClientSecret)}");
        Console.WriteLine($"  API BaseUrl: {rawApiUrl ?? "(not set, will use default)"}");

        var mappedConfig = new Dictionary<string, string?>();

        static void TrySet(IDictionary<string, string?> dict, IConfiguration root, string key, string? value)
        {
            if (string.IsNullOrWhiteSpace(value) || !string.IsNullOrEmpty(root[key]))
            {
                return;
            }

            dict[key] = value;
        }

        // Keycloak Mapping
        TrySet(mappedConfig, config, "Keycloak:Realm", rawRealm);
        TrySet(mappedConfig, config, "Keycloak:Authority", keycloakAuthority);
        TrySet(mappedConfig, config, "Keycloak:MetadataAddress", metadataAddress);
        TrySet(mappedConfig, config, "Keycloak:ClientId", config["Keycloak:ClientId"] ?? "explore-blazor-server");
        TrySet(mappedConfig, config, "Keycloak:RequireHttpsMetadata", "true");

        // Always set client secret from Infisical (override any existing value)
        // This ensures the correct secret from Infisical is used, not a stale value from user secrets
        if (!string.IsNullOrWhiteSpace(rawClientSecret))
        {
            mappedConfig["Keycloak:ClientSecret"] = rawClientSecret;
            Console.WriteLine("[Blazor Infisical] Setting Keycloak:ClientSecret from Infisical (overriding existing)");
        }

        // API Mapping
        if (!string.IsNullOrEmpty(rawApiUrl))
        {
            TrySet(mappedConfig, config, "ExploreApi:BaseUrl", rawApiUrl);
        }

        // Inject mapped configuration
        configBuilder.AddInMemoryCollection(
            mappedConfig.Where(kv => !string.IsNullOrEmpty(kv.Value))
                        .ToDictionary(kv => kv.Key, kv => kv.Value)!
        );
    }
}
