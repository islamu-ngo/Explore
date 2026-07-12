// ABOUTME: Configuration extensions for the API project.
// ABOUTME: Adds Infisical as configuration source and maps Infisical secret names to .NET config keys.

namespace Explore.API.Extensions;

using Explore.Domain.Constants;
using Explore.Domain.Secrets;
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
            source.Paths.AddRange(["/keycloak", "/postgresql", "/api", "/blazor", "/cerbos", "/mcp", "/ai", "/storage", "/smtp", "/integrations/listmonk"]);
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
    ///   /cerbos:   CERBOS_GRPC_ENDPOINT, CERBOS_HTTP_ENDPOINT, CERBOS_USE_POLICY_SCOPE
    ///   /api|/mcp: MCP_ENABLED, MCP_ENDPOINT_PATH, MCP_STATELESS, MCP_ENABLE_LEGACY_SSE
    ///   /ai:       AI_ENDPOINT, AI_MODEL_ID, AI_API_KEY, AI_PROVIDER
    ///   /storage:  STORAGE_S3_ENDPOINT, STORAGE_S3_BUCKET_NAME, STORAGE_S3_ACCESS_KEY_ID, etc.
    ///   /smtp:     MAIL_SMTP_HOST, MAIL_SMTP_PORT, MAIL_SMTP_USERNAME, MAIL_SMTP_PASSWORD, etc.
    ///   /api:      VAPID_PUBLIC_KEY, VAPID_PRIVATE_KEY, VAPID_SUBJECT
    ///   /api:      USE_COMMERCIAL_LUCKYPENNY, LUCKYPENNY_LICENSE_KEY (Lucky Penny dual-versioning)
    ///   S3 legacy: ISLAMU_EVENT_S3_ENDPOINT, ISLAMU_EVENT_REGION, etc.
    /// Postgres is handled by BootstrapSecretLoader from discrete POSTGRESQL_* secrets.
    /// </remarks>
    private static void ApplyMapping(IConfigurationBuilder configBuilder, IConfiguration config)
    {
        var rawRealm = config["KEYCLOAK_REALM"] ?? config["Keycloak:Realm"];
        var baseUrl = config["KEYCLOAK_ENDPOINT"];
        var explicitAuthority = config["Keycloak:Authority"];
        var rawKeycloakClientId = ReadFirst(
            config,
            "Keycloak:ClientId",
            "KEYCLOAK_CLIENT_ID",
            "KEYCLOAK_BLAZOR_CLIENT_ID");
        var rawKeycloakClientSecret = ReadFirst(
            config,
            "Keycloak:ClientSecret",
            "Keycloak:BlazorClientSecret",
            "KEYCLOAK_BLAZOR_CLIENT_SECRET");

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
        var keycloakClientId = !string.IsNullOrWhiteSpace(rawKeycloakClientId)
            ? rawKeycloakClientId
            : keycloakAuthority is null
                ? null
                : "islamu-event-blazor";
        // Preserve the operator's raw input (bare host:port or full URL). Normalization happens only
        // at gRPC channel creation time so we don't surface a misleading scheme back to the UI/storage.
        var cerbosGrpcEndpoint = ReadFirst(config, "Cerbos:GrpcEndpoint", "CERBOS_GRPC_ENDPOINT")?.Trim();
        var cerbosHttpEndpoint = ReadFirst(config, "Cerbos:HttpEndpoint", "CERBOS_HTTP_ENDPOINT")?.Trim();
        var cerbosUsePolicyScope = NormalizeBoolean(
            ReadFirst(
                config,
                "Cerbos:UsePolicyScope",
                "CERBOS_USE_POLICY_SCOPE",
                "CERBOS__USE_POLICY_SCOPE"));
        var cerbosUseTls = NormalizeBoolean(
            ReadFirst(
                config,
                "Cerbos:UseTls",
                "CERBOS_USE_TLS",
                "CERBOS__USE_TLS"));
        var cerbosPlaintextMode = NormalizeBoolean(
            ReadFirst(
                config,
                "Cerbos:PlaintextMode",
                "CERBOS_PLAINTEXT_MODE",
                "CERBOS__PLAINTEXT_MODE"));
        var cerbosAdminUsername = ReadFirst(config, "Cerbos:AdminApi:AdminUsername", "CERBOS_ADMIN_USERNAME");
        var cerbosAdminPassword = ReadFirst(config, "Cerbos:AdminApi:AdminPassword", "CERBOS_ADMIN_PASSWORD");
        var deploymentMode = NormalizeDeploymentMode(config["DEPLOYMENT_MODE"]);
        var managedControlPlaneEnabled = NormalizeBoolean(ReadFirst(
            config,
            "CONTROL_PLANE_MANAGED_MODE",
            "ManagedControlPlane:Enabled"));
        var managedControlPlaneUrl = ReadFirst(
            config,
            "CONTROL_PLANE_URL",
            "ManagedControlPlane:ControlPlaneUrl");
        var managedInstanceId = ReadFirst(
            config,
            "CONTROL_PLANE_INSTANCE_ID",
            "ManagedControlPlane:ManagedInstanceId");
        var managedRegistrationToken = ReadFirst(
            config,
            "CONTROL_PLANE_REGISTRATION_TOKEN",
            "ManagedControlPlane:RegistrationToken");
        var managedMaximumTenantCount = ReadFirst(
            config,
            "CONTROL_PLANE_MAXIMUM_TENANT_COUNT",
            "ManagedControlPlane:MaximumTenantCount");
        var managedTenantAdministratorSignInUrl = ReadFirst(
            config,
            "CONTROL_PLANE_TENANT_ADMINISTRATOR_SIGN_IN_URL",
            "ManagedControlPlane:TenantAdministratorSignInUrl");
        var mcpEnabled = NormalizeBoolean(
            ReadFirst(
                config,
                "MCP_ENABLED",
                "MCP__ENABLED",
                "Api:McpEnabled",
                "Api:Mcp:Enabled")) ?? "true";
        var mcpEndpointPath = NormalizeMcpEndpointPath(
            ReadFirst(
                config,
                "MCP_ENDPOINT_PATH",
                "MCP__ENDPOINT_PATH",
                "Api:McpEndpointPath",
                "Api:Mcp:EndpointPath")) ?? "/mcp";
        var mcpStateless = NormalizeBoolean(
            ReadFirst(
                config,
                "MCP_STATELESS",
                "MCP__STATELESS",
                "Api:McpStateless",
                "Api:Mcp:Stateless")) ?? "true";
        var mcpEnableLegacySse = NormalizeBoolean(
            ReadFirst(
                config,
                "MCP_ENABLE_LEGACY_SSE",
                "MCP__ENABLE_LEGACY_SSE",
                "Api:McpEnableLegacySse",
                "Api:Mcp:EnableLegacySse")) ?? "true";
        var aiEndpointUrl = ReadFirst(config, "AI_ENDPOINT", "AI__ENDPOINT", "AiProvider:EndpointUrl");
        var aiModelId = ReadFirst(config, "AI_MODEL_ID", "AI__MODEL_ID", "AiProvider:ModelId");
        var aiApiKey = ReadFirst(config, "AI_API_KEY", "AI__API_KEY", "AiProvider:ApiKey");
        var aiProviderMasterCode = ReadFirst(config, "AI_PROVIDER", "AI__PROVIDER", "AiProvider:Provider")?.Trim().ToUpperInvariant().Replace('-', '_');
        var aiProviderId = MapProviderMasterCodeToId(aiProviderMasterCode);
        var aiProviderDefaultsAvailable = aiProviderMasterCode is "OPENAI" or "ANTHROPIC"
            ? !string.IsNullOrWhiteSpace(aiApiKey) && !string.IsNullOrWhiteSpace(aiModelId)
            : !string.IsNullOrWhiteSpace(aiEndpointUrl) && !string.IsNullOrWhiteSpace(aiModelId);
        var smtpHost = ReadFirst(config, "MAIL_SMTP_HOST", "SMTP_HOST", "Smtp:Host");
        var smtpPort = ReadFirst(config, "MAIL_SMTP_PORT", "SMTP_PORT", "Smtp:Port");
        var smtpUsername = ReadFirst(config, "MAIL_SMTP_USERNAME", "SMTP_USERNAME", "Smtp:Username");
        var smtpPassword = ReadFirst(config, "MAIL_SMTP_PASSWORD", "SMTP_PASSWORD", "Smtp:Password");
        var smtpEncryption = ReadFirst(config, "MAIL_SMTP_ENCRYPTION", "SMTP_SECURITY", "Smtp:Encryption");
        var smtpFromAddress = ReadFirst(config, "MAIL_SMTP_FROM_ADDRESS", "SMTP_FROM_ADDRESS", "Smtp:FromAddress");
        var smtpFromName = ReadFirst(config, "MAIL_SMTP_FROM_NAME", "SMTP_FROM_NAME", "Smtp:FromName");
        var listmonkEnabled = NormalizeBoolean(ReadFirst(config, "LISTMONK_ENABLED", GovernanceSettingKeys.Integrations.Listmonk.Enabled));
        var listmonkInstanceUrl = ReadFirst(config, "LISTMONK_INSTANCE_URL", GovernanceSettingKeys.Integrations.Listmonk.InstanceUrl);
        var listmonkDefaultListId = ReadFirst(config, "LISTMONK_DEFAULT_LIST_ID", GovernanceSettingKeys.Integrations.Listmonk.DefaultListId);
        var listmonkPreconfirmSubscriptions = NormalizeBoolean(ReadFirst(config, "LISTMONK_PRECONFIRM_SUBSCRIPTIONS", GovernanceSettingKeys.Integrations.Listmonk.PreconfirmSubscriptions));
        var listmonkSyncOnRegistration = NormalizeBoolean(ReadFirst(config, "LISTMONK_SYNC_ON_REGISTRATION", GovernanceSettingKeys.Integrations.Listmonk.SyncOnRegistration));
        var listmonkApiUsername = ReadFirst(config, "LISTMONK_API_USERNAME", SecretDefinitionRegistry.Keys.Integrations.Listmonk.ApiUsername);
        var listmonkApiKey = ReadFirst(config, "LISTMONK_API_KEY", SecretDefinitionRegistry.Keys.Integrations.Listmonk.ApiKey);
        var vapidPublicKey = ReadFirst(config, "VAPID_PUBLIC_KEY", "WebPush:VapidPublicKey");
        var vapidPrivateKey = ReadFirst(config, "VAPID_PRIVATE_KEY", "WebPush:VapidPrivateKey");
        var vapidSubject = ReadFirst(config, "VAPID_SUBJECT", "WebPush:VapidSubject");
        var webPushEnabled = NormalizeBoolean(ReadFirst(config, "WEB_PUSH_ENABLED", "WebPush:Enabled"))
            ?? (!string.IsNullOrWhiteSpace(vapidPublicKey)
                && !string.IsNullOrWhiteSpace(vapidPrivateKey)
                && !string.IsNullOrWhiteSpace(vapidSubject)
                    ? "true"
                    : null);

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
        TrySet(mappedConfig, config, "Keycloak:ClientId", keycloakClientId);
        TrySet(mappedConfig, config, "Keycloak:ClientSecret", rawKeycloakClientSecret);
        TrySet(mappedConfig, config, "Keycloak:Audience", "islamu-event-api");
        TrySet(mappedConfig, config, "Keycloak:RequireHttpsMetadata", "true");

        // S3
        TrySet(mappedConfig, config, "S3Settings:Region", ReadFirst(config, "STORAGE_S3_REGION", "Storage:S3Region", "Storage:S3:Region", "ISLAMU_EVENT_REGION"));
        TrySet(mappedConfig, config, "S3Settings:BucketName", ReadFirst(config, "STORAGE_S3_BUCKET_NAME", "Storage:S3BucketName", "Storage:S3:BucketName", "ISLAMU_EVENT_PRIVATE_BUCKET_NAME"));
        TrySet(mappedConfig, config, "S3Settings:AccessKeyId", ReadFirst(config, "STORAGE_S3_ACCESS_KEY_ID", "Storage:S3AccessKeyId", "Storage:S3:AccessKeyId", "ISLAMU_EVENT_PRIVATE_ACCESS_KEY_ID"));
        TrySet(mappedConfig, config, "S3Settings:SecretAccessKey", ReadFirst(config, "STORAGE_S3_SECRET_ACCESS_KEY", "Storage:S3SecretAccessKey", "Storage:S3:SecretAccessKey", "ISLAMU_EVENT_PRIVATE_SECRET_ACCESS_KEY_ID"));
        TrySet(mappedConfig, config, "S3Settings:Endpoint", ReadFirst(config, "STORAGE_S3_ENDPOINT", "Storage:S3Endpoint", "Storage:S3:Endpoint", "ISLAMU_EVENT_S3_ENDPOINT"));
        TrySet(mappedConfig, config, "S3Settings:PublicEndpoint", ReadFirst(config, "STORAGE_S3_PUBLIC_ENDPOINT", "Storage:S3PublicEndpoint", "Storage:S3:PublicEndpoint", "ISLAMU_EVENT_S3_PUBLIC_ENDPOINT"));

        TrySet(mappedConfig, config, "Smtp:Host", smtpHost);
        TrySet(mappedConfig, config, "Smtp:Port", smtpPort);
        TrySet(mappedConfig, config, "Smtp:Username", smtpUsername);
        TrySet(mappedConfig, config, "Smtp:Password", smtpPassword);
        TrySet(mappedConfig, config, "Smtp:Encryption", smtpEncryption);
        TrySet(mappedConfig, config, "Smtp:FromAddress", smtpFromAddress);
        TrySet(mappedConfig, config, "Smtp:FromName", smtpFromName);

        TrySet(mappedConfig, config, GovernanceSettingKeys.Integrations.Listmonk.Enabled, listmonkEnabled);
        TrySet(mappedConfig, config, GovernanceSettingKeys.Integrations.Listmonk.InstanceUrl, listmonkInstanceUrl);
        TrySet(mappedConfig, config, GovernanceSettingKeys.Integrations.Listmonk.DefaultListId, listmonkDefaultListId);
        TrySet(mappedConfig, config, GovernanceSettingKeys.Integrations.Listmonk.PreconfirmSubscriptions, listmonkPreconfirmSubscriptions);
        TrySet(mappedConfig, config, GovernanceSettingKeys.Integrations.Listmonk.SyncOnRegistration, listmonkSyncOnRegistration);
        TrySet(mappedConfig, config, SecretDefinitionRegistry.Keys.Integrations.Listmonk.ApiUsername, listmonkApiUsername);
        TrySet(mappedConfig, config, SecretDefinitionRegistry.Keys.Integrations.Listmonk.ApiKey, listmonkApiKey);

        TrySet(mappedConfig, config, "WebPush:Enabled", webPushEnabled);
        TrySet(mappedConfig, config, "WebPush:VapidPublicKey", vapidPublicKey);
        TrySet(mappedConfig, config, "WebPush:VapidPrivateKey", vapidPrivateKey);
        TrySet(mappedConfig, config, "WebPush:VapidSubject", vapidSubject);

        // Cerbos
        if (!string.IsNullOrWhiteSpace(cerbosGrpcEndpoint))
        {
            mappedConfig["Cerbos:GrpcEndpoint"] = cerbosGrpcEndpoint;
        }
        if (!string.IsNullOrWhiteSpace(cerbosHttpEndpoint))
        {
            mappedConfig["Cerbos:HttpEndpoint"] = cerbosHttpEndpoint;
            mappedConfig["Cerbos:AdminApi:Endpoints:0"] = cerbosHttpEndpoint;
        }
        TrySet(mappedConfig, config, "Cerbos:UsePolicyScope", cerbosUsePolicyScope);
        TrySet(mappedConfig, config, "Cerbos:UseTls", cerbosUseTls);
        TrySet(mappedConfig, config, "Cerbos:PlaintextMode", cerbosPlaintextMode);
        TrySet(mappedConfig, config, "Cerbos:AdminApi:AdminUsername", cerbosAdminUsername);
        TrySet(mappedConfig, config, "Cerbos:AdminApi:AdminPassword", cerbosAdminPassword);

        // Deployment
        TrySet(mappedConfig, config, "Deployment:Mode", deploymentMode);
        TrySet(mappedConfig, config, "ManagedControlPlane:Enabled", managedControlPlaneEnabled);
        TrySet(mappedConfig, config, "ManagedControlPlane:ControlPlaneUrl", managedControlPlaneUrl);
        TrySet(mappedConfig, config, "ManagedControlPlane:ManagedInstanceId", managedInstanceId);
        TrySet(mappedConfig, config, "ManagedControlPlane:RegistrationToken", managedRegistrationToken);
        TrySet(mappedConfig, config, "ManagedControlPlane:MaximumTenantCount", managedMaximumTenantCount);
        TrySet(mappedConfig, config, "ManagedControlPlane:TenantAdministratorSignInUrl", managedTenantAdministratorSignInUrl);

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
            TrySet(mappedConfig, config, "AiProvider:Provider", aiProviderId ?? "3");
        }

        // Lucky Penny dual-versioning (AutoMapper 15+ / MediatR 13+ commercial licensing).
        // USE_COMMERCIAL_LUCKYPENNY and LUCKYPENNY_LICENSE_KEY come from Infisical /api folder.
        // Version secrets (AUTOMAPPER_COMMERCIAL_VERSION, MEDIATR_COMMERCIAL_VERSION) are build-time
        // MSBuild properties only — they are not mapped to runtime configuration.
        TrySet(mappedConfig, config, "Licensing:LuckyPenny:Enabled",
            NormalizeBoolean(ReadFirst(config, "USE_COMMERCIAL_LUCKYPENNY", "Licensing:LuckyPenny:Enabled")));
        TrySet(mappedConfig, config, "Licensing:LuckyPenny:LicenseKey",
            ReadFirst(config, "LUCKYPENNY_LICENSE_KEY", "Licensing:LuckyPenny:LicenseKey"));

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

    private static string? MapProviderMasterCodeToId(string? masterCode) => masterCode switch
    {
        "NONE" => "1",
        "FAKE" => "2",
        "OPENAI_COMPATIBLE" => "3",
        "ANTHROPIC_COMPATIBLE" => "4",
        "OPENAI" => "5",
        "AZURE_OPENAI" => "6",
        "ANTHROPIC" => "7",
        _ => null
    };

    private static string? NormalizeMcpEndpointPath(string? rawValue)
    {
        if (string.IsNullOrWhiteSpace(rawValue))
        {
            return null;
        }

        var path = rawValue.Trim();

        return path.StartsWith('/')
            ? path
            : $"/{path}";
    }
}
