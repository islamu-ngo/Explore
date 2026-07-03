// ABOUTME: Authoritative catalog of every secret-backed setting the platform understands.
// ABOUTME: Encodes the user-specified Infisical layout (api, storage, keycloak, cerbos, postgresql, smtp, analytics, ai).

using System.Collections.Frozen;
using Explore.Domain.Constants;
using Explore.Domain.Enums;

namespace Explore.Domain.Secrets;

/// <summary>
/// Source-of-truth catalog of platform secrets.
/// <para>
/// Every allowed setting key, its permissible scopes, its allowed source types and its default
/// Infisical coordinates are declared here. The domain invariant <c>SecretBinding.Create</c> consults
/// the registry to reject illegal combinations before persistence — no resolver or handler may bind
/// a secret that is not listed here.
/// </para>
/// <para>
/// Folder layout matches the user specification verbatim:
/// <list type="bullet">
///   <item><c>/api</c> — SETUP_SECRET</item>
///   <item><c>/storage</c> — STORAGE_S3_*</item>
///   <item><c>/keycloak</c> — KEYCLOAK_*</item>
///   <item><c>/cerbos</c> — CERBOS_GRPC_ENDPOINT</item>
///   <item><c>/postgresql</c> — POSTGRESQL_*</item>
///   <item><c>/smtp</c> — SMTP_*</item>
///   <item><c>/analytics</c> — ANALYTICS_POSTHOG_*</item>
///   <item><c>/ai</c> — AI_*</item>
/// </list>
/// </para>
/// </summary>
public static class SecretDefinitionRegistry
{
    private static readonly FrozenDictionary<string, SecretDefinition> DefinitionsByKey = BuildDefinitions();

    /// <summary>All platform secret definitions keyed by canonical setting key (lower.dot.case).</summary>
    public static IReadOnlyDictionary<string, SecretDefinition> All => DefinitionsByKey;

    /// <summary>Returns <c>true</c> when the platform recognizes the given setting key.</summary>
    public static bool IsKnown(string settingKey) => DefinitionsByKey.ContainsKey(settingKey);

    /// <summary>Returns the definition or <c>null</c> when the key is unknown.</summary>
    public static SecretDefinition? TryGet(string settingKey) =>
        DefinitionsByKey.TryGetValue(settingKey, out var def) ? def : null;

    /// <summary>Returns the definition; throws <see cref="KeyNotFoundException"/> when unknown.</summary>
    public static SecretDefinition GetRequired(string settingKey) =>
        TryGet(settingKey) ?? throw new KeyNotFoundException(
            $"Unknown secret key '{settingKey}'. Add a definition to SecretDefinitionRegistry first.");

    // -- Canonical keys (lower.dot.case, aligned with governance/key naming convention) --

    public static class Keys
    {
        public const string SetupSecret = "api.setup_secret";

        public static class Storage
        {
            public const string Endpoint = "storage.s3.endpoint";
            public const string PublicEndpoint = "storage.s3.public_endpoint";
            public const string BucketName = "storage.s3.bucket_name";
            public const string AccessKeyId = "storage.s3.access_key_id";
            public const string SecretAccessKey = "storage.s3.secret_access_key";
            public const string Region = "storage.s3.region";
        }

        public static class Keycloak
        {
            public const string Realm = "keycloak.realm";
            public const string ClientId = "keycloak.client_id";
            public const string BlazorClientSecret = "keycloak.blazor_client_secret";
            public const string ApiClientSecret = "keycloak.api_client_secret";
            public const string Endpoint = "keycloak.endpoint";
            public const string AdminUsername = "keycloak.admin_username";
            public const string AdminPassword = "keycloak.admin_password";
            public const string DbPassword = "keycloak.db_password";
        }

        public static class Postgresql
        {
            public const string Host = "postgresql.host";
            public const string Port = "postgresql.port";
            public const string Database = "postgresql.database";
            public const string Username = "postgresql.username";
            public const string Password = "postgresql.password";
        }

        public static class Smtp
        {
            public const string Host = "smtp.host";
            public const string Port = "smtp.port";
            public const string Username = "smtp.username";
            public const string Password = "smtp.password";
            public const string FromAddress = "smtp.from_address";
            public const string FromName = "smtp.from_name";
        }

        public static class Analytics
        {
            public const string PosthogPublicKey = "analytics.posthog.public_key";
            public const string PosthogHost = "analytics.posthog.host";
        }

        public static class Cerbos
        {
            public const string GrpcEndpoint = "cerbos.grpc_endpoint";
            public const string CustomAdminUsername = InfrastructureSecretSettingKeys.Cerbos.CustomAdminUsername;
            public const string CustomAdminPassword = InfrastructureSecretSettingKeys.Cerbos.CustomAdminPassword;
        }

        public static class Webhooks
        {
            public const string SvixAuthToken = "webhooks.svix.auth_token";
            public const string SvixOperationalWebhookSecret = "webhooks.svix.operational_webhook_secret";
        }

        public static class Ai
        {
            public const string OpenAiApiKey = "ai.openai.api_key";
            public const string AnthropicApiKey = "ai.anthropic.api_key";
        }
    }

    private static FrozenDictionary<string, SecretDefinition> BuildDefinitions()
    {
        // Instance-only allowed source lists (no Inline for bootstrap secrets, see IsBootstrapSecret rule).
        var nonBootstrapSources = new[]
        {
            SecretSourceType.Infisical,
            SecretSourceType.InlineEncrypted,
            SecretSourceType.EnvironmentVariable,
        };
        var bootstrapSources = new[]
        {
            SecretSourceType.Infisical,
            SecretSourceType.EnvironmentVariable,
        };

        var instanceOnly = new[] { SecretScope.Instance };
        var instanceOrTenant = new[] { SecretScope.Instance, SecretScope.Tenant };

        var defs = new List<SecretDefinition>
        {
            // --- api/SETUP_SECRET ---
            new()
            {
                Key = Keys.SetupSecret,
                AllowedScopes = instanceOnly,
                AllowedSources = bootstrapSources,
                DefaultInfisicalPath = "/api",
                DefaultInfisicalKey = "SETUP_SECRET",
                DefaultEnvironmentVariableName = "SETUP_SECRET",
                IsBootstrapSecret = true,
                Description = "First-run setup token authorising instance onboarding.",
            },

            // --- storage/STORAGE_S3_* ---
            new()
            {
                Key = Keys.Storage.Endpoint,
                AllowedScopes = instanceOrTenant,
                AllowedSources = nonBootstrapSources,
                DefaultInfisicalPath = "/storage",
                DefaultInfisicalKey = "STORAGE_S3_ENDPOINT",
                DefaultEnvironmentVariableName = "STORAGE_S3_ENDPOINT",
                IsBootstrapSecret = false,
                Description = "S3-compatible endpoint URL (internal/backend).",
            },
            new()
            {
                Key = Keys.Storage.PublicEndpoint,
                AllowedScopes = instanceOrTenant,
                AllowedSources = nonBootstrapSources,
                DefaultInfisicalPath = "/storage",
                DefaultInfisicalKey = "STORAGE_S3_PUBLIC_ENDPOINT",
                DefaultEnvironmentVariableName = "STORAGE_S3_PUBLIC_ENDPOINT",
                IsBootstrapSecret = false,
                Description = "Publicly-reachable S3 endpoint used for pre-signed URLs.",
            },
            new()
            {
                Key = Keys.Storage.BucketName,
                AllowedScopes = instanceOrTenant,
                AllowedSources = nonBootstrapSources,
                DefaultInfisicalPath = "/storage",
                DefaultInfisicalKey = "STORAGE_S3_BUCKET_NAME",
                DefaultEnvironmentVariableName = "STORAGE_S3_BUCKET_NAME",
                IsBootstrapSecret = false,
                Description = "Default S3 bucket name for media + private assets.",
            },
            new()
            {
                Key = Keys.Storage.AccessKeyId,
                AllowedScopes = instanceOrTenant,
                AllowedSources = nonBootstrapSources,
                DefaultInfisicalPath = "/storage",
                DefaultInfisicalKey = "STORAGE_S3_ACCESS_KEY_ID",
                DefaultEnvironmentVariableName = "STORAGE_S3_ACCESS_KEY_ID",
                IsBootstrapSecret = false,
                Description = "S3 access key ID.",
            },
            new()
            {
                Key = Keys.Storage.SecretAccessKey,
                AllowedScopes = instanceOrTenant,
                AllowedSources = nonBootstrapSources,
                DefaultInfisicalPath = "/storage",
                DefaultInfisicalKey = "STORAGE_S3_SECRET_ACCESS_KEY",
                DefaultEnvironmentVariableName = "STORAGE_S3_SECRET_ACCESS_KEY",
                IsBootstrapSecret = false,
                Description = "S3 secret access key.",
            },
            new()
            {
                Key = Keys.Storage.Region,
                AllowedScopes = instanceOrTenant,
                AllowedSources = nonBootstrapSources,
                DefaultInfisicalPath = "/storage",
                DefaultInfisicalKey = "STORAGE_S3_REGION",
                DefaultEnvironmentVariableName = "STORAGE_S3_REGION",
                IsBootstrapSecret = false,
                Description = "S3 region (e.g. us-east-1).",
            },

            // --- keycloak/KEYCLOAK_* ---
            new()
            {
                Key = Keys.Keycloak.Realm,
                AllowedScopes = instanceOnly,
                AllowedSources = nonBootstrapSources,
                DefaultInfisicalPath = "/keycloak",
                DefaultInfisicalKey = "KEYCLOAK_REALM",
                DefaultEnvironmentVariableName = "KEYCLOAK_REALM",
                IsBootstrapSecret = false,
                Description = "Keycloak realm name.",
            },
            new()
            {
                Key = Keys.Keycloak.ClientId,
                AllowedScopes = instanceOnly,
                AllowedSources = nonBootstrapSources,
                DefaultInfisicalPath = "/keycloak",
                DefaultInfisicalKey = "KEYCLOAK_CLIENT_ID",
                DefaultEnvironmentVariableName = "KEYCLOAK_CLIENT_ID",
                IsBootstrapSecret = false,
                Description = "Keycloak OIDC client ID (Blazor Server BFF).",
            },
            new()
            {
                Key = Keys.Keycloak.BlazorClientSecret,
                AllowedScopes = instanceOnly,
                AllowedSources = nonBootstrapSources,
                DefaultInfisicalPath = "/keycloak",
                DefaultInfisicalKey = "KEYCLOAK_BLAZOR_CLIENT_SECRET",
                DefaultEnvironmentVariableName = "KEYCLOAK_BLAZOR_CLIENT_SECRET",
                IsBootstrapSecret = false,
                Description = "Keycloak OIDC client secret for the Blazor Server BFF (confidential client).",
            },
            new()
            {
                Key = Keys.Keycloak.ApiClientSecret,
                AllowedScopes = instanceOnly,
                AllowedSources = nonBootstrapSources,
                DefaultInfisicalPath = "/keycloak",
                DefaultInfisicalKey = "KEYCLOAK_API_CLIENT_SECRET",
                DefaultEnvironmentVariableName = "KEYCLOAK_API_CLIENT_SECRET",
                IsBootstrapSecret = false,
                Description = "Keycloak OIDC client secret for the API service (confidential client).",
            },
            new()
            {
                Key = Keys.Keycloak.Endpoint,
                AllowedScopes = instanceOnly,
                AllowedSources = nonBootstrapSources,
                DefaultInfisicalPath = "/keycloak",
                DefaultInfisicalKey = "KEYCLOAK_ENDPOINT",
                DefaultEnvironmentVariableName = "KEYCLOAK_ENDPOINT",
                IsBootstrapSecret = false,
                Description = "Keycloak base URL (e.g. https://auth.example.com).",
            },
            new()
            {
                Key = Keys.Keycloak.AdminUsername,
                AllowedScopes = instanceOnly,
                AllowedSources = nonBootstrapSources,
                DefaultInfisicalPath = "/keycloak",
                DefaultInfisicalKey = "KEYCLOAK_ADMIN_USERNAME",
                DefaultEnvironmentVariableName = "KEYCLOAK_ADMIN_USERNAME",
                IsBootstrapSecret = false,
                Description = "Keycloak realm administrator username.",
            },
            new()
            {
                Key = Keys.Keycloak.AdminPassword,
                AllowedScopes = instanceOnly,
                AllowedSources = nonBootstrapSources,
                DefaultInfisicalPath = "/keycloak",
                DefaultInfisicalKey = "KEYCLOAK_ADMIN_PASSWORD",
                DefaultEnvironmentVariableName = "KEYCLOAK_ADMIN_PASSWORD",
                IsBootstrapSecret = false,
                Description = "Keycloak realm administrator password.",
            },
            new()
            {
                Key = Keys.Keycloak.DbPassword,
                AllowedScopes = instanceOnly,
                AllowedSources = nonBootstrapSources,
                DefaultInfisicalPath = "/keycloak",
                DefaultInfisicalKey = "KEYCLOAK_DB_PASSWORD",
                DefaultEnvironmentVariableName = "KEYCLOAK_DB_PASSWORD",
                IsBootstrapSecret = false,
                Description = "Keycloak backing database password (read by Keycloak itself).",
            },

            // --- cerbos/CERBOS_GRPC_ENDPOINT ---
            new()
            {
                Key = Keys.Cerbos.GrpcEndpoint,
                AllowedScopes = instanceOnly,
                AllowedSources = nonBootstrapSources,
                DefaultInfisicalPath = "/cerbos",
                DefaultInfisicalKey = "CERBOS_GRPC_ENDPOINT",
                DefaultEnvironmentVariableName = "CERBOS_GRPC_ENDPOINT",
                IsBootstrapSecret = false,
                Description = "Cerbos PDP gRPC endpoint (e.g. cerbosgrpc.example.com:443).",
            },
            new()
            {
                Key = Keys.Cerbos.CustomAdminUsername,
                AllowedScopes = instanceOnly,
                AllowedSources = nonBootstrapSources,
                DefaultInfisicalPath = "/cerbos",
                DefaultInfisicalKey = "CERBOS_ADMIN_USERNAME",
                DefaultEnvironmentVariableName = "CERBOS_ADMIN_USERNAME",
                IsBootstrapSecret = false,
                Description = "Cerbos Admin API username used by server-side policy package sync.",
            },
            new()
            {
                Key = Keys.Cerbos.CustomAdminPassword,
                AllowedScopes = instanceOnly,
                AllowedSources = nonBootstrapSources,
                DefaultInfisicalPath = "/cerbos",
                DefaultInfisicalKey = "CERBOS_ADMIN_PASSWORD",
                DefaultEnvironmentVariableName = "CERBOS_ADMIN_PASSWORD",
                IsBootstrapSecret = false,
                Description = "Cerbos Admin API password used by server-side policy package sync.",
            },

            // --- webhooks/WEBHOOKS_SVIX_AUTH_TOKEN ---
            new()
            {
                Key = Keys.Webhooks.SvixAuthToken,
                AllowedScopes = instanceOnly,
                AllowedSources = nonBootstrapSources,
                DefaultInfisicalPath = "/webhooks",
                DefaultInfisicalKey = "WEBHOOKS_SVIX_AUTH_TOKEN",
                DefaultEnvironmentVariableName = "WEBHOOKS_SVIX_AUTH_TOKEN",
                IsBootstrapSecret = false,
                Description = "Svix API token used by the backend-only outgoing webhook provider.",
            },
            new()
            {
                Key = Keys.Webhooks.SvixOperationalWebhookSecret,
                AllowedScopes = instanceOnly,
                AllowedSources = nonBootstrapSources,
                DefaultInfisicalPath = "/webhooks",
                DefaultInfisicalKey = "WEBHOOKS_SVIX_OPERATIONAL_WEBHOOK_SECRET",
                DefaultEnvironmentVariableName = "WEBHOOKS_SVIX_OPERATIONAL_WEBHOOK_SECRET",
                IsBootstrapSecret = false,
                Description = "Svix endpoint signing secret used to verify incoming operational callbacks.",
            },

            // --- postgresql/POSTGRESQL_* (ALL bootstrap) ---
            new()
            {
                Key = Keys.Postgresql.Host,
                AllowedScopes = instanceOnly,
                AllowedSources = bootstrapSources,
                DefaultInfisicalPath = "/postgresql",
                DefaultInfisicalKey = "POSTGRESQL_HOST",
                DefaultEnvironmentVariableName = "POSTGRESQL_HOST",
                IsBootstrapSecret = true,
                Description = "PostgreSQL host.",
            },
            new()
            {
                Key = Keys.Postgresql.Port,
                AllowedScopes = instanceOnly,
                AllowedSources = bootstrapSources,
                DefaultInfisicalPath = "/postgresql",
                DefaultInfisicalKey = "POSTGRESQL_PORT",
                DefaultEnvironmentVariableName = "POSTGRESQL_PORT",
                IsBootstrapSecret = true,
                Description = "PostgreSQL port.",
            },
            new()
            {
                Key = Keys.Postgresql.Database,
                AllowedScopes = instanceOnly,
                AllowedSources = bootstrapSources,
                DefaultInfisicalPath = "/postgresql",
                DefaultInfisicalKey = "POSTGRESQL_DATABASE",
                DefaultEnvironmentVariableName = "POSTGRESQL_DATABASE",
                IsBootstrapSecret = true,
                Description = "PostgreSQL database name.",
            },
            new()
            {
                Key = Keys.Postgresql.Username,
                AllowedScopes = instanceOnly,
                AllowedSources = bootstrapSources,
                DefaultInfisicalPath = "/postgresql",
                DefaultInfisicalKey = "POSTGRESQL_USERNAME",
                DefaultEnvironmentVariableName = "POSTGRESQL_USERNAME",
                IsBootstrapSecret = true,
                Description = "PostgreSQL username.",
            },
            new()
            {
                Key = Keys.Postgresql.Password,
                AllowedScopes = instanceOnly,
                AllowedSources = bootstrapSources,
                DefaultInfisicalPath = "/postgresql",
                DefaultInfisicalKey = "POSTGRESQL_PASSWORD",
                DefaultEnvironmentVariableName = "POSTGRESQL_PASSWORD",
                IsBootstrapSecret = true,
                Description = "PostgreSQL password (NEVER InlineEncrypted - DB unlocks itself).",
            },

            // --- smtp/SMTP_* ---
            new()
            {
                Key = Keys.Smtp.Host,
                AllowedScopes = instanceOrTenant,
                AllowedSources = nonBootstrapSources,
                DefaultInfisicalPath = "/smtp",
                DefaultInfisicalKey = "SMTP_HOST",
                DefaultEnvironmentVariableName = "SMTP_HOST",
                IsBootstrapSecret = false,
                Description = "SMTP server hostname.",
            },
            new()
            {
                Key = Keys.Smtp.Port,
                AllowedScopes = instanceOrTenant,
                AllowedSources = nonBootstrapSources,
                DefaultInfisicalPath = "/smtp",
                DefaultInfisicalKey = "SMTP_PORT",
                DefaultEnvironmentVariableName = "SMTP_PORT",
                IsBootstrapSecret = false,
                Description = "SMTP server port.",
            },
            new()
            {
                Key = Keys.Smtp.Username,
                AllowedScopes = instanceOrTenant,
                AllowedSources = nonBootstrapSources,
                DefaultInfisicalPath = "/smtp",
                DefaultInfisicalKey = "SMTP_USERNAME",
                DefaultEnvironmentVariableName = "SMTP_USERNAME",
                IsBootstrapSecret = false,
                Description = "SMTP authentication username.",
            },
            new()
            {
                Key = Keys.Smtp.Password,
                AllowedScopes = instanceOrTenant,
                AllowedSources = nonBootstrapSources,
                DefaultInfisicalPath = "/smtp",
                DefaultInfisicalKey = "SMTP_PASSWORD",
                DefaultEnvironmentVariableName = "SMTP_PASSWORD",
                IsBootstrapSecret = false,
                Description = "SMTP authentication password.",
            },
            new()
            {
                Key = Keys.Smtp.FromAddress,
                AllowedScopes = instanceOrTenant,
                AllowedSources = nonBootstrapSources,
                DefaultInfisicalPath = "/smtp",
                DefaultInfisicalKey = "SMTP_FROM_ADDRESS",
                DefaultEnvironmentVariableName = "SMTP_FROM_ADDRESS",
                IsBootstrapSecret = false,
                Description = "Default From: email address.",
            },
            new()
            {
                Key = Keys.Smtp.FromName,
                AllowedScopes = instanceOrTenant,
                AllowedSources = nonBootstrapSources,
                DefaultInfisicalPath = "/smtp",
                DefaultInfisicalKey = "SMTP_FROM_NAME",
                DefaultEnvironmentVariableName = "SMTP_FROM_NAME",
                IsBootstrapSecret = false,
                Description = "Default From: display name.",
            },

            // --- analytics/ANALYTICS_POSTHOG_* ---
            new()
            {
                Key = Keys.Analytics.PosthogPublicKey,
                AllowedScopes = instanceOrTenant,
                AllowedSources = nonBootstrapSources,
                DefaultInfisicalPath = "/analytics",
                DefaultInfisicalKey = "ANALYTICS_POSTHOG_PUBLIC_KEY",
                DefaultEnvironmentVariableName = "ANALYTICS_POSTHOG_PUBLIC_KEY",
                IsBootstrapSecret = false,
                Description = "PostHog project public API key.",
            },
            new()
            {
                Key = Keys.Analytics.PosthogHost,
                AllowedScopes = instanceOrTenant,
                AllowedSources = nonBootstrapSources,
                DefaultInfisicalPath = "/analytics",
                DefaultInfisicalKey = "ANALYTICS_POSTHOG_HOST",
                DefaultEnvironmentVariableName = "ANALYTICS_POSTHOG_HOST",
                IsBootstrapSecret = false,
                Description = "PostHog server URL.",
            },

            // --- ai/AI_* ---
            new()
            {
                Key = Keys.Ai.OpenAiApiKey,
                AllowedScopes = instanceOrTenant,
                AllowedSources = nonBootstrapSources,
                DefaultInfisicalPath = "/ai",
                DefaultInfisicalKey = "AI_OPENAI_API_KEY",
                DefaultEnvironmentVariableName = "AI_OPENAI_API_KEY",
                IsBootstrapSecret = false,
                Description = "OpenAI API key (organisation-scoped).",
            },
            new()
            {
                Key = Keys.Ai.AnthropicApiKey,
                AllowedScopes = instanceOrTenant,
                AllowedSources = nonBootstrapSources,
                DefaultInfisicalPath = "/ai",
                DefaultInfisicalKey = "AI_ANTHROPIC_API_KEY",
                DefaultEnvironmentVariableName = "AI_ANTHROPIC_API_KEY",
                IsBootstrapSecret = false,
                Description = "Anthropic API key.",
            },
        };

        return defs.ToFrozenDictionary(d => d.Key, StringComparer.OrdinalIgnoreCase);
    }
}
