// ABOUTME: Configuration extensions for the API project.
// ABOUTME: Adds Infisical as configuration source and maps Infisical secret names to .NET config keys.

namespace Explore.API.Extensions;

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
            source.Paths.AddRange(["/keycloak", "/postgresql", "/api", "/blazor", "/cerbos", "/mcp", "/ai"]);
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
    ///   /api:      DEPLOYMENT_MODE (single_tenant or multi_tenant)
    ///   /keycloak: KEYCLOAK_ENDPOINT, KEYCLOAK_REALM
    ///   /cerbos:   CERBOS_GRPC_ENDPOINT
    ///   /api:      MCP_ENABLED, MCP_ENDPOINT_PATH, MCP_STATELESS, MCP_ENABLE_LEGACY_SSE
    ///   /ai:       AI_ENDPOINT, AI_MODEL_ID, AI_API_KEY
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
        // Preserve the operator's raw input (bare host:port or full URL). Normalization happens only
        // at gRPC channel creation time so we don't surface a misleading scheme back to the UI/storage.
        var cerbosGrpcEndpoint = config["CERBOS_GRPC_ENDPOINT"]?.Trim();
        var deploymentMode = NormalizeDeploymentMode(config["DEPLOYMENT_MODE"]);
        var mcpEnabled = NormalizeBoolean(
            ReadFirst(
                config,
                "MCP_ENABLED",
                "MCP__ENABLED",
                "Api:McpEnabled",
                "Api:Mcp:Enabled"));
        var mcpEndpointPath = NormalizeMcpEndpointPath(
            ReadFirst(
                config,
                "MCP_ENDPOINT_PATH",
                "MCP__ENDPOINT_PATH",
                "Api:McpEndpointPath",
                "Api:Mcp:EndpointPath"));
        var mcpStateless = NormalizeBoolean(
            ReadFirst(
                config,
                "MCP_STATELESS",
                "MCP__STATELESS",
                "Api:McpStateless",
                "Api:Mcp:Stateless"));
        var mcpEnableLegacySse = NormalizeBoolean(
            ReadFirst(
                config,
                "MCP_ENABLE_LEGACY_SSE",
                "MCP__ENABLE_LEGACY_SSE",
                "Api:McpEnableLegacySse",
                "Api:Mcp:EnableLegacySse"));
        var aiEndpointUrl = ReadFirst(config, "AI_ENDPOINT", "AI__ENDPOINT", "AiProvider:EndpointUrl");
        var aiModelId = ReadFirst(config, "AI_MODEL_ID", "AI__MODEL_ID", "AiProvider:ModelId");
        var aiApiKey = ReadFirst(config, "AI_API_KEY", "AI__API_KEY", "AiProvider:ApiKey");
        var aiProviderDefaultsAvailable = !string.IsNullOrWhiteSpace(aiEndpointUrl) && !string.IsNullOrWhiteSpace(aiModelId);

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

        // Deployment
        TrySet(mappedConfig, config, "Deployment:Mode", deploymentMode);

        // MCP
        TrySet(mappedConfig, config, "Mcp:Enabled", mcpEnabled);
        TrySet(mappedConfig, config, "Mcp:EndpointPath", mcpEndpointPath);
        TrySet(mappedConfig, config, "Mcp:Stateless", mcpStateless);
        TrySet(mappedConfig, config, "Mcp:EnableLegacySse", mcpEnableLegacySse);

        // AI provider defaults from deployment secrets. Runtime governance can still override
        // application-managed settings through the hierarchical settings system.
        TrySet(mappedConfig, config, "AiProvider:EndpointUrl", aiEndpointUrl);
        TrySet(mappedConfig, config, "AiProvider:ModelId", aiModelId);
        TrySet(mappedConfig, config, "AiProvider:ApiKey", aiApiKey);
        if (aiProviderDefaultsAvailable)
        {
            TrySet(mappedConfig, config, "AiProvider:Enabled", "true");
            TrySet(mappedConfig, config, "AiProvider:Provider", "openai-compatible");
        }

        configBuilder.AddInMemoryCollection(
            mappedConfig.Where(kv => !string.IsNullOrEmpty(kv.Value))
                        .ToDictionary(kv => kv.Key, kv => kv.Value)!
        );
    }

    private static string? NormalizeDeploymentMode(string? rawValue)
    {
        if (string.IsNullOrWhiteSpace(rawValue))
        {
            return null;
        }

        var normalized = rawValue.Trim()
            .Replace("_", string.Empty, StringComparison.Ordinal)
            .Replace("-", string.Empty, StringComparison.Ordinal)
            .Replace(" ", string.Empty, StringComparison.Ordinal);

        return normalized.Equals("MultiTenant", StringComparison.OrdinalIgnoreCase)
            ? "MultiTenant"
            : normalized.Equals("SingleTenant", StringComparison.OrdinalIgnoreCase)
                ? "SingleTenant"
                : null;
    }

    private static string? ReadFirst(IConfiguration config, params string[] keys)
    {
        foreach (var key in keys)
        {
            var value = config[key];
            if (!string.IsNullOrWhiteSpace(value))
            {
                return value;
            }
        }

        return null;
    }

    private static string? NormalizeBoolean(string? rawValue)
    {
        if (string.IsNullOrWhiteSpace(rawValue))
        {
            return null;
        }

        var value = rawValue.Trim();
        if (bool.TryParse(value, out var result))
        {
            return result ? "true" : "false";
        }

        return value.ToLowerInvariant() switch
        {
            "1" or "yes" or "on" => "true",
            "0" or "no" or "off" => "false",
            _ => null
        };
    }

    private static string? NormalizeMcpEndpointPath(string? rawValue)
    {
        if (string.IsNullOrWhiteSpace(rawValue))
        {
            return null;
        }

        var path = rawValue.Trim();

        return path.StartsWith("/", StringComparison.Ordinal)
            ? path
            : $"/{path}";
    }
}
