ABOUTME: Secret management abstraction with pluggable providers and automatic refresh.
ABOUTME: Covers Explore.Secrets library configuration, health monitoring, and encryption services.

# Secrets Management

The `Explore.Secrets` library provides a provider-agnostic secret management layer with automatic refresh, health monitoring, and encryption.

## Provider Types

| Provider | Enum | Status | Auth Method |
|---|---|---|---|
| None | `0` | Implemented | Environment variables only |
| Infisical | `1` | Implemented | Universal Auth (ClientId + ClientSecret) |
| Vault | `2` | Not implemented | AppRole (RoleId + SecretId) |
| Azure Key Vault | `3` | Not implemented | DefaultAzureCredential |
| AWS Secrets Manager | `4` | Not implemented | Region-based |

When `Provider = None`, secrets come exclusively from environment variables and `appsettings.json`. No external provider is contacted.

## Configuration

### SecretProvider Section

```json
{
  "SecretProvider": {
    "Provider": "None",
    "FailFast": true,
    "Infisical": {
      "Url": "",
      "ProjectId": "",
      "ClientId": "",
      "ClientSecret": "",
      "Environment": "dev",
      "Paths": ["/"]
    }
  }
}
```

### Local Development User Secrets

For local development, the projects share a unified .NET User Secrets ID defined in the `.csproj` files: `event-shared-secrets`.

Maintainers using Infisical-backed Aspire profiles must create or edit the shared `secrets.json` file:
- **Linux/macOS:** `/home/{user}/.microsoft/usersecrets/event-shared-secrets/secrets.json`
- **Windows:** `%APPDATA%\Microsoft\UserSecrets\event-shared-secrets\secrets.json`

Add the bootstrap credentials for your maintainer developer environment inside this file:
```json
{
  "Infisical:Url": "https://example.com",
  "Infisical:ProjectId": "",
  "Infisical:Environment": "dev",
  "Infisical:ClientId": "",
  "Infisical:ClientSecret": ""
}
```

> [!IMPORTANT]
> The contributor default Aspire profile is `local-full`. It starts local infrastructure and sets `SecretProvider:Provider=None` for child projects, so contributors should not need Infisical credentials.
> Maintainer profiles intentionally differ:
> - `local-core` starts local PostgreSQL/Redis but loads auth, policy, storage, webhook, and provider settings from Infisical/config.
> - `local-lite` starts only API and Blazor, with all infrastructure loaded from Infisical/config.
> If Infisical is not used in a maintainer profile, supply equivalent settings through environment variables or appsettings before running the AppHost.

### Docker Compose Environment Files

The repository root `.env.example` mirrors the supported Infisical folder layout and documents which service consumes each key. Copy it to `.env` for local Compose runs; `.env` is intentionally ignored by git.

Docker Compose uses `.env` for interpolation before starting containers. The Compose file then passes explicit `environment:` entries into each service. Do not rely on a broad `env_file: .env` import because it would place unrelated secrets into containers that do not need them.

There are two Infisical paths through the application:

- `SecretProvider:Provider=Infisical` controls the `ISecretResolver` provider used by settings/secret-binding resolution.
- Non-empty bare `Infisical:*` bootstrap values enable the startup compatibility loaders that fetch Infisical paths directly into `IConfiguration`.

For full local runs, keep `SECRET_PROVIDER=None` and leave `INFISICAL_*` blank so local `POSTGRESQL_*`, Keycloak, Cerbos, and storage values remain authoritative. If `INFISICAL_*` is populated, the PostgreSQL bootstrap loader can read `/postgresql` before local environment variables by design.

### SecretRefresh Section

| Key | Default | Purpose |
|---|---|---|
| `Enabled` | `true` | Enable periodic secret refresh |
| `RefreshInterval` | `00:05:00` | Polling interval |
| `InitialDelay` | `00:00:10` | Delay before first refresh |
| `BaseBackoffDelay` | `00:00:05` | Base delay for exponential backoff |
| `MaxBackoffDelay` | `00:05:00` | Maximum backoff cap |
| `JitterFactor` | `0.1` | Randomization factor to prevent thundering herd |
| `UnhealthyThreshold` | `3` | Consecutive failures before unhealthy status |

Backoff formula: `BaseBackoffDelay × 2^(failures - 1)`, capped at `MaxBackoffDelay`, plus jitter.

### Encryption Section

| Key | Purpose |
|---|---|
| `CurrentKeyVersion` | Active encryption key version (integer) |
| `KeyVersions` | Dictionary of version → key material |
| `MasterKeyEnvironmentVariable` | Env var holding the master key |

Encryption uses AES-256-GCM with 12-byte nonce and 16-byte authentication tag.

### Rotation Section

| Key | Default | Purpose |
|---|---|---|
| `Enabled` | `false` | Enable automatic key rotation |
| `GracePeriod` | `00:00:30` | Overlap window during rotation |
| `MaxConcurrentRotations` | `5` | Parallel rotation limit |

## Key Mapping

Infisical uses `SCREAMING_SNAKE_CASE` with path-based sections. The provider maps bidirectionally:

| Infisical Path + Key | .NET Configuration Key |
|---|---|
| `/keycloak/REALM_NAME` | `Keycloak:RealmName` |
| `/keycloak/KEYCLOAK_BLAZOR_CLIENT_SECRET` | Blazor BFF `Keycloak:ClientSecret` and Compose `keycloak-init` client-secret sync input |
| `/keycloak/KEYCLOAK_API_CLIENT_SECRET` | Optional legacy/future Compose `keycloak-init` sync input for deployments that intentionally make the API resource-server client confidential; not needed by the current bearer-only API audience client |
| root or AI path + `AI_TOOL_PROPOSALS_ENABLED` | `AiProvider:ToolProposalsEnabled` |
| `/postgresql/POSTGRESQL_HOST` | PostgreSQL bootstrap host |
| `/postgresql/POSTGRESQL_PORT` | PostgreSQL bootstrap port |
| `/postgresql/POSTGRESQL_DATABASE` | PostgreSQL bootstrap database |
| `/postgresql/POSTGRESQL_USERNAME` | PostgreSQL bootstrap username |
| `/postgresql/POSTGRESQL_PASSWORD` | PostgreSQL bootstrap password |
| storage path + `STORAGE_S3_*` | `Storage:S3*` (for example `/storage/STORAGE_S3_ENDPOINT` → `Storage:S3Endpoint`) |
| `/cerbos/CERBOS_USE_POLICY_SCOPE` | `Cerbos:UsePolicyScope` |
| raw process environment + `STORAGE_S3_*` | consumed directly by the S3 resolver as a compatibility fallback |

Environment variable format uses double-underscore separators for .NET keys, for example `S3Settings__Endpoint`. Storage also accepts raw `STORAGE_S3_*` variables for deployment compatibility. PostgreSQL bootstrap intentionally uses discrete `POSTGRESQL_*` values rather than a single URL-form connection string.

Compose Keycloak bootstrap consumes `KEYCLOAK_ADMIN` and `KEYCLOAK_ADMIN_PASSWORD` only inside the one-shot `keycloak-init` container. Those credentials are not application runtime secrets and must not be stored in governance settings or copied into support artifacts. The init logs redact client secret values.

External-Keycloak setup bootstrap accepts a one-time Keycloak admin or service-account username/password through the setup UI. Treat that credential as operator input for a single setup request, not as an ISLAMU-managed secret. ISLAMU must not save it to appsettings, environment variables, Infisical paths, database governance settings, logs, traces, screenshots, or support bundles. After a successful bootstrap, only the runtime Keycloak OIDC values and the Blazor BFF client secret are stored according to the normal authentication secret ownership model.


## Ownership Model

Secrets use a platform-wide ownership model so environment variables and external secret providers are not permanent live overrides by accident. The ownership source is metadata; browser-facing DTOs expose only configured/source/editability flags and never raw values, ciphertext, provider tokens, or resolved secret coordinates.

| Mode | Source types | UI behavior | Runtime meaning |
|---|---|---|---|
| Application-managed | `InlineEncrypted` / application-stored encrypted values | Editable in ISLAMU Event admin/setup UI, masked after save | Saved application settings are the runtime authority; deployment values can only prefill/import until an operator saves. |
| Deployment-managed | `EnvironmentVariable` or `Infisical` binding, or an explicit deployment-managed key list | Read-only badge in UI; rotate outside the app | Values are controlled by environment, appsettings, or the secret provider and changes require provider refresh or redeploy/restart. |
| Deployment bootstrap | Environment/secret-provider value exists but no application-managed value has been saved | Editable prefill with “Bootstrap from Deployment” badge | The value helps first-run setup only. If modified and saved, application-managed settings take precedence from then on. |

Do not merge application-managed and deployment-managed values for the same field at runtime. The `SecretResolver` dispatches through one `SecretBinding` source and intentionally does not fallback to another source after a binding is selected. For settings still migrating to the shared secret control plane, the same contract applies at the DTO/UI boundary: deployment values may prefill forms, but they do not silently override saved application settings unless that key is explicitly marked deployment-managed.

Current migrated surface: Cerbos authorization settings expose endpoint and Admin API credential ownership metadata. Cerbos Admin API credentials are now registered in `SecretDefinitionRegistry`; the browser sees only configured flags and ownership badges. SMTP, S3, OAuth, localization/TMS, and AI keys still have area-specific storage/UI paths and must not be documented as fully migrated until their resolvers use the shared ownership metadata consistently.

## ISecretProvider Interface

| Method | Returns | Purpose |
|---|---|---|
| `InitializeAsync` | `Task` | One-time provider setup |
| `GetSecretAsync` | `Task<string?>` | Single secret by key |
| `GetSecretWithMetadataAsync` | `Task<SecretWithMetadata?>` | Secret with version and timestamps |
| `GetSecretsByPathAsync` | `Task<IDictionary>` | All secrets under a path |
| `RefreshAsync` | `Task` | Force refresh from provider |
| `GetHealthAsync` | `Task<HealthCheckResult>` | Provider health status |

Properties: `ProviderType`, `SupportsRefresh`.

## Health Check

Registered as `secret_provider` with tag `secrets`.

| Status | Condition |
|---|---|
| Healthy | Fewer than `UnhealthyThreshold` consecutive failures |
| Degraded | 1–2 consecutive failures |
| Unhealthy | ≥ `UnhealthyThreshold` consecutive failures |

Health data includes: provider type, supports refresh, consecutive failure count, last successful refresh timestamp.

## Metrics

Meter name: `Explore.Secrets`

| Instrument | Type | Purpose |
|---|---|---|
| `secrets_refresh_total` | Counter | Total refresh attempts |
| `secrets_refresh_failures_total` | Counter | Failed refresh attempts |
| `secrets_refresh_duration_seconds` | Histogram | Refresh operation duration |
| `secrets_consecutive_failures` | UpDownCounter | Current consecutive failure count |
| `secrets_last_refresh_timestamp_seconds` | Gauge | Unix timestamp of last successful refresh |

## Refresh Service

A `BackgroundService` using `PeriodicTimer` that:

1. Waits `InitialDelay` before first poll.
2. Calls `ISecretProvider.RefreshAsync()` at each `RefreshInterval`.
3. On success, calls `IConfigurationRoot.Reload()` to propagate changes.
4. On failure, applies exponential backoff with jitter.
5. Logs structured warnings with correlation IDs.

## Audit Decorator

Wraps `ISecretProvider` to log all secret access operations. Sensitive keys are redacted (matches: `password`, `secret`, `key`, `token`, `credential`, `connectionstring`, `apikey`).

Audit entries track: Operation, ProviderType, KeyPattern (redacted), Timestamp, UserId (extracted via `sub` → `nameidentifier` → `sid` fallback), CorrelationId.

## DI Registration

| Method | Purpose |
|---|---|
| `AddSecretProvider` | Core provider registration |
| `AddSecretManagement` | Full setup (provider + refresh + health + metrics + encryption) |
| `AddSecretObservability` | Health checks + metrics |
| `AddSecretMetrics` | Prometheus metrics only |
| `AddSecretHealthCheck` | Health check only |
| `AddSecretRefreshService` | Background refresh service |
| `AddEncryptionService` | AES-256-GCM encryption |
| `AddKeyRotationService` | Automatic key rotation |
| `AddRotationAwareHttpClientFactory` | HttpClient with rotating credentials |
| `AddRotationAwareDbContextFactory<T>` | DbContext with rotating connection strings |
| `AddConnectionRotation<T>` | Generic connection rotation |

Typical startup: `services.AddSecretManagement(configuration)` registers everything.

## Related

- [CONFIGURATION.md](CONFIGURATION.md) — application settings
- [SELF_HOSTING.md](SELF_HOSTING.md) — environment variable reference
- [OPERATIONS.md](OPERATIONS.md) — health checks and metrics
