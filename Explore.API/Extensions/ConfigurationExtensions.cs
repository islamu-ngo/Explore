// ABOUTME: Configuration extensions for the API project.
// ABOUTME: Adds Infisical as configuration source and maps Infisical secret names to .NET config keys.

namespace Explore.API.Extensions;

using Explore.Application.Utilities;
using Explore.Secrets.Extensions;

public static class ConfigurationExtensions
{
    /// <summary>
    /// Adds Infisical secrets and maps them to canonical .NET configuration keys.
    /// </summary>
    public static void AddInfisicalCompatibility(this IConfigurationBuilder configBuilder)
    {
        var bootstrapConfig = configBuilder.Build();

        configBuilder.AddInfisical(bootstrapConfig, source =>
        {
            source.Paths.Clear();
            source.Paths.AddRange(["/keycloak", "/postgresql", "/api", "/blazor", "/cerbos"]);
            source.ThrowOnFirstLoadFailure = false;
        });

        var configWithSecrets = configBuilder.Build();
        ApplyMapping(configBuilder, configWithSecrets);
    }

    /// <summary>
    /// Maps Infisical secret names to .NET configuration keys.
    /// </summary>
    /// <remarks>
    /// Canonical Infisical keys:
    ///   /keycloak: KEYCLOAK_ENDPOINT, KEYCLOAK_REALM
    ///   /cerbos:   CERBOS_GRPC_ENDPOINT
    ///   S3 keys:   ISLAMU_EVENT_S3_ENDPOINT, ISLAMU_EVENT_REGION, etc.
    /// Postgres is handled by BootstrapSecretLoader from discrete POSTGRESQL_* secrets.
    /// </remarks>
    private static void ApplyMapping(IConfigurationBuilder configBuilder, IConfiguration config)
    {
        var rawRealm = config["KEYCLOAK_REALM"] ?? config["Keycloak:Realm"];
        var baseUrl = config["KEYCLOAK_ENDPOINT"];
        var explicitAuthority = config["Keycloak:Authority"];

        string? keycloakAuthority = null;
        if (!string.IsNullOrEmpty(explicitAuthority))
        {
            keycloakAuthority = explicitAuthority;
        }
        else if (!string.IsNullOrWhiteSpace(baseUrl) && !string.IsNullOrWhiteSpace(rawRealm))
        {
            keycloakAuthority = $"{baseUrl.TrimEnd('/')}/realms/{rawRealm}";
        }

        var metadataAddress = keycloakAuthority != null
            ? $"{keycloakAuthority}/.well-known/openid-configuration"
            : null;
        var authorizationUrl = keycloakAuthority != null
            ? $"{keycloakAuthority}/protocol/openid-connect/auth"
            : null;
        var cerbosGrpcEndpoint = GrpcEndpointNormalizer.Normalize(config["CERBOS_GRPC_ENDPOINT"]);

        var mappedConfig = new Dictionary<string, string?>();

        static void TrySet(IDictionary<string, string?> dict, IConfiguration root, string key, string? value)
        {
            if (string.IsNullOrWhiteSpace(value) || !string.IsNullOrEmpty(root[key]))
                return;
            dict[key] = value;
        }

        // Keycloak
        TrySet(mappedConfig, config, "Keycloak:Realm", rawRealm);
        TrySet(mappedConfig, config, "Keycloak:Authority", keycloakAuthority);
        TrySet(mappedConfig, config, "Keycloak:MetadataAddress", metadataAddress);
        TrySet(mappedConfig, config, "Keycloak:AuthorizationUrl", authorizationUrl);
        TrySet(mappedConfig, config, "Keycloak:Audience", "islamu-event-api");
        TrySet(mappedConfig, config, "Keycloak:RequireHttpsMetadata", "true");

        // S3
        TrySet(mappedConfig, config, "S3Settings:Region", config["ISLAMU_EVENT_REGION"]);
        TrySet(mappedConfig, config, "S3Settings:BucketName", config["ISLAMU_EVENT_PRIVATE_BUCKET_NAME"]);
        TrySet(mappedConfig, config, "S3Settings:AccessKeyId", config["ISLAMU_EVENT_PRIVATE_ACCESS_KEY_ID"]);
        TrySet(mappedConfig, config, "S3Settings:SecretAccessKey", config["ISLAMU_EVENT_PRIVATE_SECRET_ACCESS_KEY_ID"]);
        TrySet(mappedConfig, config, "S3Settings:Endpoint", config["ISLAMU_EVENT_S3_ENDPOINT"]);
        TrySet(mappedConfig, config, "S3Settings:PublicEndpoint", config["ISLAMU_EVENT_S3_PUBLIC_ENDPOINT"]);

        // Cerbos
        if (!string.IsNullOrWhiteSpace(cerbosGrpcEndpoint))
        {
            mappedConfig["Cerbos:GrpcEndpoint"] = cerbosGrpcEndpoint;
        }

        configBuilder.AddInMemoryCollection(
            mappedConfig.Where(kv => !string.IsNullOrEmpty(kv.Value))
                        .ToDictionary(kv => kv.Key, kv => kv.Value)!
        );
    }
}
