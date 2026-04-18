// ABOUTME: Configuration extensions for the API project.
// Adds Infisical as configuration source and provides compatibility mapping for environment variables.

namespace Explore.API.Extensions;

using Explore.Secrets.Extensions;

public static class ConfigurationExtensions
{
    /// <summary>
    /// Adds Infisical secrets and applies compatibility mapping for environment variables.
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
    public static void AddInfisicalCompatibility(this IConfigurationBuilder configBuilder)
    {
        // Build temporary config to read bootstrap credentials (from user secrets/env vars)
        var bootstrapConfig = configBuilder.Build();

        // Add Infisical as configuration source (loads secrets from Infisical service)
        // This uses credentials from user secrets to authenticate with Infisical
        configBuilder.AddInfisical(bootstrapConfig, source =>
        {
            // Configure paths to load from Infisical
            source.Paths.Clear();
            source.Paths.AddRange(["/keycloak", "/postgresql", "/api", "/blazor"]);

            // Don't fail if Infisical isn't configured (allows local dev without Infisical)
            source.ThrowOnFirstLoadFailure = false;
        });

        // Rebuild config after Infisical secrets are added
        var configWithSecrets = configBuilder.Build();

        // Apply compatibility mapping for environment variable names
        ApplyCompatibilityMapping(configBuilder, configWithSecrets);
    }

    /// <summary>
    /// Maps Infisical secret names to .NET configuration keys.
    /// </summary>
    /// <remarks>
    /// This translates between:
    /// - Infisical naming: KEYCLOAK_REALM, ISLAMU_EVENT_S3_ENDPOINT
    /// - .NET naming: Keycloak:Realm, S3Settings:Endpoint
    /// Postgres is handled separately by <c>BootstrapSecretLoader</c> from discrete
    /// POSTGRESQL_HOST/PORT/DATABASE/USERNAME/PASSWORD secrets - no URL form.
    /// </remarks>
    private static void ApplyCompatibilityMapping(IConfigurationBuilder configBuilder, IConfiguration config)
    {
        // Read values (from Infisical, environment, or existing config).
        // Postgres connection string is handled exclusively by BootstrapSecretLoader from
        // discrete POSTGRESQL_* secrets - never mapped here and the URL form is no longer supported.
        var rawRealm = config["Keycloak:Realm"] ?? config["KEYCLOAK_REALM"] ?? "islamu-dev";
        var baseUrl = config["KEYCLOAK_PUBLIC_URL"]
            ?? config["KEYCLOAK_BASE_URL"]
            ?? config["Keycloak:BaseUrl"]
            ?? "https://keycloak.openislamu.org";
        var explicitAuthority = config["Keycloak:Authority"];

        // Compute derived values
        var keycloakAuthority = !string.IsNullOrEmpty(explicitAuthority)
            ? explicitAuthority
            : $"{baseUrl.TrimEnd('/')}/realms/{rawRealm}";
        var metadataAddress = $"{keycloakAuthority}/.well-known/openid-configuration";
        var authorizationUrl = $"{keycloakAuthority}/protocol/openid-connect/auth";

        // Create mapping dictionary
        var mappedConfig = new Dictionary<string, string?>();

        static void TrySet(IDictionary<string, string?> dict, IConfiguration root, string key, string? value)
        {
            if (string.IsNullOrWhiteSpace(value) || !string.IsNullOrEmpty(root[key]))
            {
                return;
            }

            dict[key] = value;
        }

        // Map Keycloak
        TrySet(mappedConfig, config, "Keycloak:Realm", rawRealm);
        TrySet(mappedConfig, config, "Keycloak:Authority", keycloakAuthority);
        TrySet(mappedConfig, config, "Keycloak:MetadataAddress", metadataAddress);
        TrySet(mappedConfig, config, "Keycloak:AuthorizationUrl", authorizationUrl);
        TrySet(mappedConfig, config, "Keycloak:Audience", "explore-api");
        TrySet(mappedConfig, config, "Keycloak:RequireHttpsMetadata", "true");

        // Map S3 settings
        TrySet(mappedConfig, config, "S3Settings:Region", config["ISLAMU_EVENT_REGION"]);
        TrySet(mappedConfig, config, "S3Settings:BucketName", config["ISLAMU_EVENT_PRIVATE_BUCKET_NAME"]);
        TrySet(mappedConfig, config, "S3Settings:AccessKeyId", config["ISLAMU_EVENT_PRIVATE_ACCESS_KEY_ID"]);
        TrySet(mappedConfig, config, "S3Settings:SecretAccessKey", config["ISLAMU_EVENT_PRIVATE_SECRET_ACCESS_KEY_ID"]);
        TrySet(mappedConfig, config, "S3Settings:Endpoint", config["ISLAMU_EVENT_S3_ENDPOINT"]);
        TrySet(mappedConfig, config, "S3Settings:PublicEndpoint", config["ISLAMU_EVENT_S3_PUBLIC_ENDPOINT"]);

        // Inject mapped configuration
        configBuilder.AddInMemoryCollection(
            mappedConfig.Where(kv => !string.IsNullOrEmpty(kv.Value))
                        .ToDictionary(kv => kv.Key, kv => kv.Value)!
        );
    }
}
