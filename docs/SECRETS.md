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
| `/database/CONNECTION_STRING` | `Database:ConnectionString` |

Environment variable format uses double-underscore separators: `DATABASE__CONNECTIONSTRING`.

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
