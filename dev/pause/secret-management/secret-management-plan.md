# Secret Management Implementation Plan

> **Strategic Plan for Enterprise-Grade Secret Management**
>
> **Project**: Explore
> **Date**: January 2026
> **Updated**: February 2, 2026
> **Status**: In Progress
> **Estimated Effort**: 25-35 hours across 7 phases

---

## Executive Summary

This plan implements a unified, secret-manager-agnostic configuration system for the Explore project. The solution:

1. **Decouples projects from Aspire** - API and Blazor run independently
2. **Supports Infisical as primary provider** - With environment variable fallback for self-hosters
3. **Maintains self-hoster compatibility** - Plain environment variables as fallback
4. **Adds database-backed dynamic settings** - Admin-configurable with encryption and versioning
5. **Enterprise observability** - Prometheus metrics, health checks, audit logging
6. **Connection rotation** - Graceful handling of credential changes for HttpClient and DbContext
7. **Additional cloud providers (deferred)** - Vault, Azure KV, AWS SM support can be added later

---

## Current State Analysis

### Existing Architecture

```
Explore.AppHost (Aspire)
    ├── Infisical SDK integration
    ├── Loads secrets by path (/keycloak, /api, /blazor, /postgresql)
    └── Injects as environment variables to child projects
         ├── Explore.API (receives env vars)
         ├── Explore.Blazor (receives env vars)
         └── Event.MigrationService (receives env vars)
```

### Current Configuration Flow

```
1. AppHost authenticates with Infisical (UniversalAuth)
2. Loads secrets from paths via SDK
3. Sets as environment variables on projects
4. Projects use ConfigurationExtensions to map raw vars to IConfiguration
5. Services consume via IOptions<T> pattern
```

### Key Files Affected

| File | Current Role | Changes Needed |
|------|-------------|----------------|
| `Explore.AppHost/AppHost.cs` | Infisical integration | Remove Infisical, simplify |
| `Explore.API/Extensions/ConfigurationExtensions.cs` | Env var mapping | Integrate with new provider |
| `Explore.Blazor/Extensions/ConfigurationExtension.cs` | Env var mapping | Integrate with new provider |
| `Explore.Infrastructure/InfrastructureServicesRegistration.cs` | Settings binding | Add Explore.Secrets reference |
| `Explore.Persistence/PersistenceServicesRegistration.cs` | DbContext setup | Add AppSetting repository |
| `docker/entrypoint.sh` | Infisical CLI wrapper | DELETE |
| `Dockerfile` | Infisical CLI installation | Simplify |

---

## Target Architecture

### Configuration Priority Stack

```
+------------------------------------------------------------------+
|  1. DATABASE (Operational Settings ONLY)                          |
|     - Admin-managed: SMTP, feature flags, UI customization        |
|     - Encrypted with versioned keys (supports rotation)           |
|     - NEVER: DB connection, Master Key, Keycloak secrets          |
+------------------------------------------------------------------+
          |
          v (fallback)
+------------------------------------------------------------------+
|  2. SECRET MANAGER (High-Value Secrets)                           |
|     - Infisical / Vault / Azure KV / AWS SM                       |
|     - Bootstrap secrets: DB connection, Master Key, API keys      |
|     - Cloud-native auth: Managed Identity, IRSA, AppRole          |
+------------------------------------------------------------------+
          |
          v (fallback)
+------------------------------------------------------------------+
|  3. ENVIRONMENT VARIABLES (Self-Hoster Fallback)                  |
|     - Plain env vars set by user                                  |
|     - Docker Compose, Kubernetes, systemd                         |
+------------------------------------------------------------------+
          |
          v (fallback)
+------------------------------------------------------------------+
|  4. APPSETTINGS.JSON (Non-Sensitive Defaults Only)                |
|     - Logging, feature toggles (disabled by default)              |
+------------------------------------------------------------------+
```

### New Project: Explore.Secrets

```
Explore.Secrets/
├── Explore.Secrets.csproj
├── Abstractions/
│   ├── ISecretProvider.cs
│   ├── IEncryptionService.cs
│   └── SecretProviderType.cs
├── Providers/
│   ├── EnvironmentSecretProvider.cs
│   ├── InfisicalSecretProvider.cs
│   ├── VaultSecretProvider.cs
│   ├── AzureKeyVaultSecretProvider.cs
│   └── AwsSecretsManagerProvider.cs
├── Configuration/
│   ├── SecretProviderOptions.cs
│   ├── SecretRefreshOptions.cs
│   ├── DbConfigurationSource.cs
│   └── DbConfigurationProvider.cs
├── Services/
│   ├── AesEncryptionService.cs
│   ├── SecretProviderFactory.cs
│   ├── SecretRefreshService.cs
│   └── KeyRotationService.cs
├── Observability/
│   ├── SecretRefreshMetrics.cs
│   └── SecretProviderHealthCheck.cs
├── Validation/
│   └── RequiredSecretsValidator.cs
└── Extensions/
    └── ServiceCollectionExtensions.cs
```

---

## Implementation Phases

### Phase 1: Core Infrastructure with Validation (6-8 hours) ✅ COMPLETE

**Goal**: Create Explore.Secrets project with core abstractions and environment fallback

**Tasks**:
1. Create `Explore.Secrets.csproj` with package references
2. Define `ISecretProvider` interface with health info
3. Define `IEncryptionService` interface with key versioning
4. Implement `EnvironmentSecretProvider` (fallback)
5. Implement `SecretProviderFactory`
6. Implement `RequiredSecretsValidator` with `ValidateOnStart`
7. Create `ServiceCollectionExtensions` for DI
8. Add unit tests (TUnit with native async assertions)

**Package References**:
```xml
<PackageReference Include="Microsoft.Extensions.Configuration.Abstractions" />
<PackageReference Include="Microsoft.Extensions.Options" />
<PackageReference Include="Microsoft.Extensions.Hosting.Abstractions" />
<PackageReference Include="Microsoft.Extensions.Diagnostics.HealthChecks" />
<PackageReference Include="System.Diagnostics.DiagnosticSource" />
```

**Acceptance Criteria**:
- [x] `SECRET_PROVIDER=none` loads from environment variables
- [x] `ValidateOnStart` fails if required secrets missing
- [x] Unit tests pass with >80% coverage

---

### Phase 2: Observability Infrastructure (3-4 hours) ✅ COMPLETE

**Goal**: Add Prometheus metrics, health checks, and audit logging

**Tasks**:
1. Implement `SecretRefreshMetrics` using `System.Diagnostics.Metrics`
2. Implement `SecretProviderHealthCheck` implementing `IHealthCheck`
3. Implement `AuditingSecretProviderDecorator` (decorator pattern)
4. Wire metrics to `/metrics` endpoint (OpenTelemetry)
5. Wire health check to `/health` endpoint
6. Add structured logging with correlation IDs

**Metrics Exposed**:
- `secrets_refresh_total` (counter)
- `secrets_refresh_failures_total` (counter)
- `secrets_refresh_duration_seconds` (histogram)
- `secrets_last_refresh_timestamp` (gauge)
- `secrets_consecutive_failures` (gauge)

**Acceptance Criteria**:
- [x] Prometheus scrape returns metrics
- [x] Health check reflects provider status
- [x] Audit logs include user, key (redacted), timestamp

---

### Phase 3: Infisical Provider with Refresh (3-4 hours) ✅ COMPLETE

**Goal**: Implement Infisical SDK integration with robust refresh

**Tasks**:
1. Add `Infisical.Sdk` NuGet package
2. Implement `InfisicalSecretProvider`
   - Universal Auth authentication
   - Path-based secret loading
   - Secret caching with TTL
3. Implement `SecretRefreshService` as `BackgroundService`
   - `PeriodicTimer` pattern (not `Timer`)
   - Exponential backoff with jitter
   - Serialized loads via `SemaphoreSlim`
4. Add integration tests (optional, requires Infisical)

**Code Pattern (from Infisical docs)**:
```csharp
var settings = new InfisicalSdkSettingsBuilder()
    .WithHostUri(_options.InfisicalUrl)
    .Build();
var client = new InfisicalClient(settings);
await client.Auth().UniversalAuth().LoginAsync(clientId, clientSecret);

var secrets = await client.Secrets().ListAsync(new ListSecretsOptions
{
    ProjectId = _options.InfisicalProjectId,
    EnvironmentSlug = _options.InfisicalEnvironment,
    SecretPath = path,
    ExpandSecretReferences = true
});
```

**Acceptance Criteria**:
- [x] Connects to Infisical and retrieves secrets
- [x] Refresh runs on schedule (default 5 min)
- [x] Exponential backoff on failure (max 5 min)
- [x] Jitter prevents thundering herd

---

### Phase 4: Database Configuration with Key Versioning (4-5 hours) 🟡 NEARLY COMPLETE

**Goal**: Add encrypted database settings with rotation support

**Tasks**:
1. Create `AppSetting` entity in `Explore.Domain`
   - Key versioning fields
   - Audit fields (CreatedAt/By, UpdatedAt/By)
   - `RowVersion` for concurrency
2. Create `AppSettingConfiguration` in `Explore.Persistence`
   - Unique constraint on Key
   - Check constraint (no high-value secrets)
   - Indexes for Category, KeyVersion
3. Create migration
4. Implement `IAppSettingRepository` and repository
5. Implement `AesEncryptionService`
   - Multi-version key support
   - `CryptographicOperations.ZeroMemory` for cleanup
6. Implement `DbConfigurationProvider`
   - `SemaphoreSlim` for serialized loads
   - Proper error handling (don't clear on failure)
7. Implement `KeyRotationService` for re-encryption
8. Add unit tests

**Entity Design**:
```csharp
public class AppSetting
{
    public string Key { get; set; }              // PK, e.g., "Smtp:Host"
    public string EncryptedValue { get; set; }  // AES-256-GCM ciphertext
    public int KeyVersion { get; set; }         // Which master key version
    public DateTime EncryptedAt { get; set; }
    public Guid? EncryptedBy { get; set; }
    public bool IsSensitive { get; set; }
    public string? Category { get; set; }
    public DateTime CreatedAt { get; set; }
    public Guid? CreatedBy { get; set; }
    public DateTime UpdatedAt { get; set; }
    public Guid? UpdatedBy { get; set; }
    [Timestamp]
    public byte[] RowVersion { get; set; }
}
```

**Acceptance Criteria**:
- [x] Settings encrypted with AES-256-GCM
- [x] Key version tracked per setting
- [x] Re-encryption workflow works
- [x] Concurrency conflicts handled
- [x] Check constraint blocks DB connection strings
- [ ] Database migration created and applied

---

### Phase 5: Connection Pool Rotation (2-3 hours)

**Goal**: Handle secret rotation for HttpClient and DbContext

**Tasks**:
1. Implement `RotationAwareHttpClientFactory`
   - Atomic swap pattern
   - Graceful drain (30s timeout)
   - `IOptionsMonitor<T>.OnChange` listener
2. Implement `RotationAwareDbContextFactory<T>`
   - Connection string change detection
   - New contexts use updated credentials
3. Wire up with DI
4. Add integration tests

**Pattern**:
```csharp
_credentialChangeListener = _credentials.OnChange((newCreds, name) =>
{
    // Atomic swap: create new client, dispose old after grace period
    var newClient = CreateClientInternal(name, newCreds);
    var oldClientLazy = _clients.AddOrUpdate(name, ...);
    Task.Delay(TimeSpan.FromSeconds(30)).ContinueWith(_ => oldClientLazy.Value.Dispose());
});
```

**Acceptance Criteria**:
- [ ] HttpClient rotates on credential change
- [ ] DB connections use updated credentials
- [ ] No connection leaks
- [ ] In-flight requests complete gracefully

---

### Phase 6: Integration and Migration (3-4 hours)

**Goal**: Integrate with existing projects and clean up legacy code

**Tasks**:
1. Add `Explore.Secrets` reference to `Explore.API`
2. Add `Explore.Secrets` reference to `Explore.Blazor`
3. Refactor `ConfigurationExtensions.cs` to use new provider
4. Update `Program.cs` in API and Blazor
   - Add secret provider registration
   - Add health check registration
   - Add metrics registration
5. Remove Infisical code from `Explore.AppHost`
6. Delete `docker/entrypoint.sh`
7. Simplify `Dockerfile` (remove Infisical CLI)
8. Update Docker Compose examples
9. Update documentation (README, OPERATIONS.md)

**Before (API Program.cs)**:
```csharp
builder.Configuration.AddInfisicalCompatibility();
```

**After (API Program.cs)**:
```csharp
builder.Configuration.AddSecretProvider(options =>
{
    options.Provider = SecretProviderType.Infisical;
    options.FailFast = builder.Environment.IsProduction();
    // ... binding from environment
});

builder.Services.AddSecretManagement(builder.Configuration);
builder.Services.AddHealthChecks()
    .AddCheck<SecretProviderHealthCheck>("secrets");
```

**Acceptance Criteria**:
- [ ] API starts with any provider (none, infisical, vault, etc.)
- [ ] Blazor starts independently
- [ ] Docker image works without Infisical CLI
- [ ] AppHost runs without Infisical integration
- [ ] Self-hoster documentation complete

---

### Phase 7: Additional Secret Providers (6-9 hours, 2-3 each) - DEFERRED

> **Note**: This phase is intentionally deferred until all core functionality is complete. Supporting additional cloud providers (Vault, Azure, AWS) is lower priority than connection rotation and integration.

**Goal**: Add HashiCorp Vault, Azure Key Vault, AWS Secrets Manager

#### Phase 7A: HashiCorp Vault (2-3 hours)

**Tasks**:
1. Add `VaultSharp` NuGet package
2. Implement `VaultSecretProvider`
   - AppRole authentication
   - Response-wrapped SecretID support
   - Token renewal at 75% TTL
   - KV v2 secret reading
3. Add tests

**AppRole Pattern**:
```csharp
// Unwrap SecretID from orchestrator-provided wrapped token
var unwrapClient = new VaultClient(new VaultClientSettings(url, new TokenAuthMethodInfo(wrappingToken)));
var secretIdData = await unwrapClient.V1.System.UnwrapWrappedResponseDataAsync<Dictionary<string, object>>(null);
var secretId = secretIdData.Data["secret_id"].ToString();

// Authenticate with RoleID + SecretID
var authMethod = new AppRoleAuthMethodInfo(roleId, secretId);
_client = new VaultClient(new VaultClientSettings(url, authMethod));
```

#### Phase 7B: Azure Key Vault (2-3 hours)

**Tasks**:
1. Add `Azure.Security.KeyVault.Secrets` + `Azure.Identity`
2. Implement `AzureKeyVaultSecretProvider`
   - `DefaultAzureCredential` for Managed Identity
   - Fallback credential chain
   - Secret versioning support
3. Add tests

**Pattern**:
```csharp
var credential = new DefaultAzureCredential();
var client = new SecretClient(new Uri(vaultUrl), credential);
var secret = await client.GetSecretAsync(secretName);
```

#### Phase 7C: AWS Secrets Manager (2-3 hours)

**Tasks**:
1. Add `AWSSDK.SecretsManager`
2. Implement `AwsSecretsManagerProvider`
   - IRSA credential chain
   - JSON secret parsing
   - Caching with TTL
3. Add tests

**Pattern**:
```csharp
// IRSA: credentials auto-discovered from env/metadata
var client = new AmazonSecretsManagerClient(RegionEndpoint.GetBySystemName(region));
var response = await client.GetSecretValueAsync(new GetSecretValueRequest { SecretId = secretName });
var secrets = JsonSerializer.Deserialize<Dictionary<string, string>>(response.SecretString);
```

**Acceptance Criteria**:
- [ ] Each provider connects and retrieves secrets
- [ ] Token/credential renewal works
- [ ] Graceful handling of unavailability

---

## Total Estimated Effort

| Phase | Description | Hours | Status |
|-------|-------------|-------|--------|
| 1 | Core Infrastructure with Validation | 6-8 | ✅ COMPLETE |
| 2 | Observability Infrastructure | 3-4 | ✅ COMPLETE |
| 3 | Infisical Provider with Refresh | 3-4 | ✅ COMPLETE |
| 4 | Database Configuration with Key Versioning | 4-5 | 🟡 NEARLY COMPLETE |
| 5 | Connection Pool Rotation | 2-3 | ⏳ NOT STARTED |
| 6 | Integration and Migration | 3-4 | ⏳ NOT STARTED |
| 7 | Additional Secret Providers (DEFERRED) | 6-9 | ⏳ DEFERRED |
| **Total** | | **27-37** | |

---

## Risk Assessment

| Risk | Impact | Mitigation |
|------|--------|------------|
| Breaking existing deployments | High | Feature flag for gradual rollout |
| Secret manager unavailability | High | Cached secrets, fail-fast in prod |
| Key rotation data loss | High | Staged re-encryption, backups |
| Performance degradation | Medium | Caching, connection pooling |
| Complex debugging | Medium | Comprehensive logging, metrics |

---

## Success Metrics

1. **Independence**: API runs with `dotnet run` (no AppHost)
2. **Provider Coverage**: Infisical + environment fallback (additional providers deferred)
3. **Self-Hoster**: Works with plain env vars
4. **Observability**: Metrics, health checks, audit logs
5. **Security**: Key rotation, encryption at rest
6. **Documentation**: Complete migration guide

---

## Dependencies

### NuGet Packages

| Package | Version | Purpose |
|---------|---------|---------|
| `Infisical.Sdk` | Latest | Infisical SDK |
| `VaultSharp` | Latest | HashiCorp Vault (Phase 7) |
| `Azure.Security.KeyVault.Secrets` | Latest | Azure Key Vault (Phase 7) |
| `Azure.Identity` | Latest | Azure auth (Phase 7) |
| `AWSSDK.SecretsManager` | Latest | AWS Secrets Manager (Phase 7) |
| `Dapper` | Latest | Lightweight DB queries |
| `Npgsql` | Latest | PostgreSQL driver |

### External Services (for testing)

- Infisical instance (self-hosted or cloud)
- HashiCorp Vault (dev mode acceptable) - Phase 7
- Azure Key Vault (optional) - Phase 7
- AWS Secrets Manager (optional) - Phase 7

---

## Related Documentation

- [Secret Management Implementation Details](../secret-management-implementation.md)
- [Independent Project Setup](../independent-project-setup.md)
- [OPERATIONS.md](../../docs/OPERATIONS.md)
- [ARCHITECTURE.md](../../docs/ARCHITECTURE.md)
