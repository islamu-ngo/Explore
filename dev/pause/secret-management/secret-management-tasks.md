# Secret Management - Task Checklist

> **Implementation Progress Tracker**
>
> **Last Updated**: February 2, 2026 (Phase Reorganization - Connection Pool before Additional Providers)

---

## Phase 1: Core Infrastructure with Validation (6-8 hours)

### Status: ✅ COMPLETE

- [x] **1.1** Create `Explore.Secrets` project
  - [x] Create `Explore.Secrets.csproj` with target framework `net10.0`
  - [x] Add package references (Microsoft.Extensions.*)
  - [x] Add project to solution file
  - [x] Set up folder structure (Abstractions, Providers, Services, etc.)

- [x] **1.2** Define core abstractions
  - [x] Create `Abstractions/ISecretProvider.cs`
    - `InitializeAsync()`, `GetSecretAsync()`, `RefreshAsync()`, `GetHealthAsync()`
  - [x] Create `Abstractions/IEncryptionService.cs`
    - `Encrypt()`, `Decrypt()`, `CurrentKeyVersion`, `ReEncryptAllAsync()`
  - [x] Create `Abstractions/SecretProviderType.cs` enum
    - None, Infisical, Vault, AzureKeyVault, AwsSecretsManager
  - [x] Create `Abstractions/ISecretAuditLogger.cs`
  - [x] Create record types: `SecretValue`, `ProviderHealthInfo`

- [x] **1.3** Create configuration options
  - [x] Create `Configuration/SecretProviderOptions.cs`
    - Provider type, refresh interval, fail-fast flag
    - Infisical settings (URL, ProjectId, ClientId, ClientSecret, Environment)
    - Vault settings (URL, RoleId, SecretId, MountPath)
    - Azure settings (VaultUrl, TenantId, ClientId, ClientSecret)
    - AWS settings (Region, AccessKeyId, SecretAccessKey)
  - [x] Create `Configuration/SecretRefreshOptions.cs`
    - RefreshInterval, BaseBackoffDelay, MaxBackoffDelay
  - [x] Create `Configuration/EncryptionOptions.cs`
    - KeyVersions dictionary, CurrentKeyVersion

- [x] **1.4** Implement EnvironmentSecretProvider
  - [x] Create `Providers/EnvironmentSecretProvider.cs`
  - [x] Map canonical keys to env var format (`:` → `__`)
  - [x] Implement `GetSecretAsync()`, `GetSecretsAsync()`, `GetSecretsByPathAsync()`
  - [x] `SupportsRefresh = false` (env vars don't change)
  - [x] Implement `GetHealthAsync()` (always healthy)

- [x] **1.5** Implement SecretProviderFactory
  - [x] Create `Services/SecretProviderFactory.cs`
  - [x] Factory method based on `SecretProviderOptions.Provider`
  - [x] Return appropriate provider instance
  - [x] Handle `None` → `EnvironmentSecretProvider`

- [x] **1.6** Implement RequiredSecretsValidator
  - [x] Create `Validation/RequiredSecretsValidator.cs`
  - [x] Implement `IValidateOptions<SecretProviderOptions>`
  - [x] Validate required secrets based on provider type
  - [x] Return `ValidateOptionsResult.Fail()` with specific errors

- [x] **1.7** Create DI extensions
  - [x] Create `Extensions/ServiceCollectionExtensions.cs`
  - [x] `AddSecretProvider()` extension method
  - [x] Register `ISecretProvider` as singleton
  - [x] Register options with `ValidateOnStart()`
  - [x] `AddSecretManagement()` for full setup

- [x] **1.8** Add unit tests
  - [x] Create `Explore.Secrets.UnitTests` project
  - [x] Test `EnvironmentSecretProvider` key mapping
  - [x] Test `SecretProviderFactory` creation
  - [x] Test `RequiredSecretsValidator` validation rules
  - [x] **40 tests passing**

---

## Phase 2: Observability Infrastructure (3-4 hours)

### Status: ✅ COMPLETE

- [x] **2.1** Implement SecretRefreshMetrics
  - [x] Create `Observability/SecretRefreshMetrics.cs`
  - [x] Use `System.Diagnostics.Metrics` (IMeterFactory)
  - [x] Define counters: `secrets_refresh_total`, `secrets_refresh_failures_total`
  - [x] Define histogram: `secrets_refresh_duration_seconds`
  - [x] Define gauges: `secrets_last_refresh_timestamp`, `secrets_consecutive_failures`
  - [x] Implement `StartRefreshOperation()`, `RecordRefreshSuccess()`, `RecordRefreshFailure()`

- [x] **2.2** Implement SecretProviderHealthCheck
  - [x] Create `Observability/SecretProviderHealthCheck.cs`
  - [x] Implement `IHealthCheck`
  - [x] Call `ISecretProvider.GetHealthAsync()`
  - [x] Return `HealthCheckResult` with provider info
  - [x] Include last refresh timestamp in data

- [x] **2.3** Implement AuditingSecretProviderDecorator
  - [x] Create `Providers/AuditingSecretProviderDecorator.cs`
  - [x] Decorator pattern wrapping `ISecretProvider`
  - [x] Log secret access with correlation ID
  - [x] Redact sensitive key names in logs
  - [x] Use `IHttpContextAccessor` for user ID

- [x] **2.4** Wire up observability
  - [x] Register `SecretRefreshMetrics` as singleton
  - [x] Register health check in DI extensions
  - [x] Add `AddSecretObservability()` extension method
  - [x] Add `StructuredSecretAuditLogger` for Serilog-compatible logging

- [x] **2.5** Add tests
  - [x] Test metrics recording (13 tests)
  - [x] Test health check responses (11 tests)
  - [x] Test audit logging redaction (23 tests)
  - [x] **87 tests passing total**

---

## Phase 3: Infisical Provider with Refresh (3-4 hours)

### Status: ✅ COMPLETE

- [x] **3.1** Add Infisical SDK
  - [x] Add `Infisical.Sdk` NuGet package to `Explore.Secrets` (v3.0.4)
  - [x] Verify SDK compatibility with .NET 10

- [x] **3.2** Implement InfisicalSecretProvider
  - [x] Create `Providers/InfisicalSecretProvider.cs`
  - [x] Implement `InitializeAsync()` with Universal Auth
  - [x] Implement `GetSecretAsync()` with path resolution
  - [x] Implement `GetSecretsByPathAsync()` using `ListSecretsOptions`
  - [x] Cache secrets in `ConcurrentDictionary`
  - [x] `SupportsRefresh = true`
  - [x] Implement `RefreshAsync()` to reload all paths
  - [x] Implement `GetHealthAsync()` with auth status
  - [x] Implement `IAsyncDisposable` for cleanup

- [x] **3.3** Implement SecretRefreshService
  - [x] Create `Services/SecretRefreshService.cs`
  - [x] Inherit from `BackgroundService`
  - [x] Use `PeriodicTimer` for scheduling
  - [x] Inject `ISecretProvider`, `IConfiguration`, `ILogger`, `SecretRefreshMetrics`
  - [x] Add jitter to initial delay
  - [x] Implement exponential backoff on failure
  - [x] Cap backoff at `MaxBackoffDelay`
  - [x] Call `provider.RefreshAsync()` then reload configuration
  - [x] Record metrics on success/failure

- [x] **3.4** Path mapping configuration
  - [x] Create canonical key → Infisical path mapping (SCREAMING_SNAKE → PascalCase)
  - [x] Support convention-based mapping with path context
  - [x] Store both original key and canonical key in cache

- [x] **3.5** Add tests
  - [x] Unit test InfisicalSecretProvider (9 tests)
  - [x] Unit test SecretRefreshService (9 tests)
  - [x] Test refresh scheduling
  - [x] Test backoff behavior
  - [x] **105 tests passing total**

---

## Phase 4: Database Configuration with Key Versioning (4-5 hours)

### Status: 🟡 NEARLY COMPLETE

- [x] **4.1** Create AppSetting entity
  - [x] Create `Explore.Domain/AppSetting.cs`
  - [x] Add ABOUTME comment
  - [x] Properties: Key (PK), EncryptedValue, KeyVersion, EncryptedAt, EncryptedBy
  - [x] Properties: IsSensitive, Description, Category, ValueType
  - [x] Properties: CreatedAt, CreatedBy, UpdatedAt, UpdatedBy (IAuditableEntity)
  - [x] Property: RowVersion (concurrency token)

- [x] **4.2** Create EF Core configuration
  - [x] Create `Explore.Persistence/Configurations/Entities/AppSettingConfiguration.cs`
  - [x] Primary key on Key column (not GUID)
  - [x] MaxLength(256) for Key
  - [x] Index on Category, KeyVersion, IsSensitive
  - [x] RowVersion as concurrency token
  - [x] Check constraint: Key NOT LIKE 'Database:%' AND Key NOT LIKE 'Security:MasterKey%' AND Key NOT LIKE 'ConnectionStrings:%'

- [x] **4.3** Create repository
  - [x] Create `Explore.Application/Contracts/Persistence/IAppSettingRepository.cs`
  - [x] Methods: GetByKeyAsync, GetByCategoryAsync, GetSettingsNeedingReEncryptionAsync
  - [x] Methods: GetAllAsync, CreateAsync, UpdateAsync, DeleteAsync, ExistsAsync, BulkUpdateAsync
  - [x] Create `Explore.Persistence/Repositories/AppSettingRepository.cs`
  - [x] Registered in `PersistenceServicesRegistration.cs`

- [ ] **4.4** Create migration (PENDING)
  - [ ] Run `dotnet ef migrations add AddAppSettings --project Explore.Persistence --startup-project Explore.API`
  - [ ] Review generated migration
  - [ ] Test migration applies cleanly

- [x] **4.5** Implement AesEncryptionService
  - [x] Create `Explore.Secrets/Services/AesEncryptionService.cs`
  - [x] Support multiple key versions in `Dictionary<int, byte[]>`
  - [x] Implement `Encrypt()` returning `EncryptionResult(Ciphertext, KeyVersion)`
  - [x] Implement `Decrypt(ciphertext, keyVersion)`
  - [x] Use AES-256-GCM with 96-bit nonce, 128-bit tag
  - [x] Format: base64(nonce + tag + ciphertext)
  - [x] Validate key length (must be 32 bytes)
  - [x] Call `CryptographicOperations.ZeroMemory()` on sensitive data
  - [x] Implement `IDisposable` to zero key material
  - [x] **32 tests passing**

- [x] **4.6** Implement DbConfigurationProvider
  - [x] Create `Explore.Secrets/Configuration/DbConfigurationSource.cs`
  - [x] Create `Explore.Secrets/Configuration/DbConfigurationProvider.cs`
  - [x] Inherit from `ConfigurationProvider`
  - [x] Use `SemaphoreSlim` for serialized loads
  - [x] Query AppSettings table with raw ADO.NET (Npgsql - lightweight, no EF Core)
  - [x] Decrypt values using `AesEncryptionService`
  - [x] Handle decryption failures gracefully (log, continue)
  - [x] On first load failure with no data: throw (configurable)
  - [x] On refresh failure with existing data: keep old data
  - [x] Track `LastSuccessfulLoad`, `ConsecutiveFailures`
  - [x] Optional periodic reload with Timer

- [x] **4.7** Implement KeyRotationService
  - [x] Create `Explore.Secrets/Services/KeyRotationService.cs`
  - [x] Method: `ReEncryptAllAsync()` with delegate pattern for flexibility
  - [x] Query settings with KeyVersion < CurrentKeyVersion
  - [x] Decrypt with old key, encrypt with new key
  - [x] Update setting via delegate (supports any persistence)
  - [x] Handle missing key versions gracefully
  - [x] Progress reporting via `IProgress<KeyRotationProgress>`
  - [x] Cancellation support
  - [x] `ReEncryptSingle()` for individual settings
  - [x] **14 tests passing**

- [x] **4.8** Add tests
  - [x] Test AES encryption round-trip (multiple scenarios)
  - [x] Test key versioning (multi-version, re-encryption)
  - [x] Test error handling (invalid keys, tampered data)
  - [x] Test re-encryption workflow (with progress, cancellation)
  - [x] **44 new tests, 149 total passing**

- [x] **4.9** Add DI extensions (BONUS)
  - [x] `AddEncryptionService()` - registers IEncryptionService
  - [x] `AddKeyRotationService()` - registers KeyRotationService
  - [x] `AddDatabaseSettings()` - adds DbConfigurationProvider to IConfigurationBuilder

---

## Phase 5: Connection Pool Rotation (2-3 hours)

### Status: ✅ COMPLETE

- [x] **5.1** Implement RotationAwareHttpClientFactory
  - [x] Create `Services/RotationAwareHttpClientFactory.cs`
  - [x] Implement `IHttpClientFactory`
  - [x] Use `ConcurrentDictionary<string, ClientEntry>` for thread-safe access
  - [x] Listen to `IOptionsMonitor<T>.OnChange`
  - [x] Atomic swap: create new, schedule old disposal
  - [x] Grace period: 30 seconds before disposing old client (configurable)
  - [x] Implement `IDisposable`

- [x] **5.2** Implement RotationAwareDbContextFactory
  - [x] Create `Services/RotationAwareDbContextFactory.cs`
  - [x] Implement `IDbContextFactory<TContext>`
  - [x] Track current connection string with rotation count
  - [x] Listen to `IOptionsMonitor<DatabaseConnectionOptions>.OnChange`
  - [x] New contexts use updated connection string
  - [x] Log connection string changes (with password redaction)

- [x] **5.3** Wire up rotation
  - [x] Register factories in DI via ServiceCollectionExtensions
  - [x] AddRotationAwareHttpClientFactory() extension method
  - [x] AddRotationAwareDbContextFactory<TContext>() extension method
  - [x] AddConnectionRotation<TContext>() convenience method

- [x] **5.4** Add tests
  - [x] Test HttpClient creation and singleton caching (RotationAwareHttpClientFactoryTests)
  - [x] Test credential application (bearer tokens, API keys, custom headers)
  - [x] Test atomic rotation via ForceRotateAsync
  - [x] Test DbContext factory creation (RotationAwareDbContextFactoryTests)
  - [x] Test connection string rotation tracking
  - [x] Test password redaction in logs
  - [x] **19 tests passing**

---

## Phase 6: Integration and Migration (3-4 hours)

### Status: ✅ COMPLETE

- [x] **6.1** Integrate with Explore.API
  - [x] Project reference already exists to `Explore.Secrets`
  - [x] Updated `Program.cs`:
    - Added `using Explore.Secrets.Extensions;`
    - Added `builder.Services.AddSecretManagement(builder.Configuration);`
    - Health check already registered via AddSecretObservability()
  - [x] `ConfigurationExtensions.cs` already uses AddInfisical()
  - [x] Build verified successful

- [x] **6.2** Integrate with Explore.Blazor
  - [x] Project reference already exists to `Explore.Secrets`
  - [x] Updated `Program.cs`:
    - Added `using Explore.Secrets.Extensions;`
    - Added `builder.Services.AddSecretManagement(builder.Configuration);`
  - [x] Build verified successful

- [x] **6.3** Clean up AppHost
  - [x] Removed Infisical SDK code from `AppHost.cs`
  - [x] Removed `LoadInfisicalEnvAsync()` method
  - [x] Removed secret path loading
  - [x] Kept basic orchestration with env var config
  - [x] Removed `Infisical.Sdk` package from `Explore.AppHost.csproj`
  - [x] Build verified successful

- [x] **6.4** Clean up Docker
  - [x] Updated `Explore.API/Dockerfile`:
    - Removed Infisical CLI installation
    - Removed entrypoint script reference
    - Using simple `ENTRYPOINT ["dotnet", "Explore.API.dll"]`
  - [x] Updated `Explore.Blazor/Dockerfile` similarly
  - [x] Note: `entrypoint.sh` files should be deleted manually

- [x] **6.5** Update Docker Compose examples
  - [x] Added `x-secrets-env` YAML anchor for secret management config
  - [x] Added secret provider env vars to explore-api and explore-blazor services
  - [x] Documented SECRET_PROVIDER, Infisical__* variables

- [x] **6.6** Update documentation
  - [x] Updated `docs/CONFIGURATION.md` with comprehensive Secret Management section
    - Provider options (none, infisical)
    - Configuration JSON and environment variables
    - Self-hosted deployment guide
    - Infisical deployment guide
    - Feature list (refresh, health checks, metrics, audit logging)

- [x] **6.7** Final testing
  - [x] Full solution build successful (dotnet build Explore.sln)
  - [x] All Explore.Secrets.UnitTests passed
  - [x] API and Blazor projects build successfully

---

## Phase 7: Additional Secret Providers (6-9 hours) - DEFERRED

### Status: ⏳ DEFERRED

> **Note**: This phase is intentionally deferred until all core functionality (Phases 1-6) is complete. Supporting additional cloud providers (Vault, Azure, AWS) is lower priority than connection rotation and integration.

### Phase 7A: HashiCorp Vault (2-3 hours)

- [ ] **7A.1** Add VaultSharp package
  - [ ] Add `VaultSharp` NuGet package

- [ ] **7A.2** Implement VaultSecretProvider
  - [ ] Create `Providers/VaultSecretProvider.cs`
  - [ ] Implement AppRole auth with response wrapping
  - [ ] Create unwrap client to get SecretID
  - [ ] Create main client with RoleID + SecretID
  - [ ] Read from KV v2 engine
  - [ ] Implement token renewal timer (75% TTL)
  - [ ] Implement `IAsyncDisposable` for cleanup

- [ ] **7A.3** Add tests
  - [ ] Test with mocked VaultClient
  - [ ] Test token renewal logic

### Phase 7B: Azure Key Vault (2-3 hours)

- [ ] **7B.1** Add Azure packages
  - [ ] Add `Azure.Security.KeyVault.Secrets`
  - [ ] Add `Azure.Identity`

- [ ] **7B.2** Implement AzureKeyVaultSecretProvider
  - [ ] Create `Providers/AzureKeyVaultSecretProvider.cs`
  - [ ] Use `DefaultAzureCredential` for auth
  - [ ] Implement secret retrieval with versioning
  - [ ] Cache secrets locally
  - [ ] Handle credential chain fallback

- [ ] **7B.3** Add tests
  - [ ] Test with mocked SecretClient

### Phase 7C: AWS Secrets Manager (2-3 hours)

- [ ] **7C.1** Add AWS package
  - [ ] Add `AWSSDK.SecretsManager`

- [ ] **7C.2** Implement AwsSecretsManagerProvider
  - [ ] Create `Providers/AwsSecretsManagerProvider.cs`
  - [ ] Use default credential chain (supports IRSA)
  - [ ] Parse JSON secret strings
  - [ ] Cache with TTL
  - [ ] Handle region configuration

- [ ] **7C.3** Add tests
  - [ ] Test with mocked AmazonSecretsManagerClient

---

## Post-Implementation

### Status: ⏳ NOT STARTED

- [ ] **P.1** Vault provider testing (requires Vault instance) - Phase 7
- [ ] **P.2** Azure KV provider testing (requires Azure subscription) - Phase 7
- [ ] **P.3** AWS SM provider testing (requires AWS account) - Phase 7
- [ ] **P.4** Performance testing (refresh under load)
- [ ] **P.5** Security review
- [ ] **P.6** Update CHANGELOG.md
- [ ] **P.7** Create release notes

---

## Quick Stats

| Phase | Status | Tasks | Completed |
|-------|--------|-------|-----------|
| Phase 1 | ✅ COMPLETE | 8 | 8 |
| Phase 2 | ✅ COMPLETE | 5 | 5 |
| Phase 3 | ✅ COMPLETE | 5 | 5 |
| Phase 4 | 🟡 NEARLY COMPLETE | 9 | 8 |
| Phase 5 | ✅ COMPLETE | 4 | 4 |
| Phase 6 | ✅ COMPLETE | 7 | 7 |
| Phase 7A | ⏳ DEFERRED | 3 | 0 |
| Phase 7B | ⏳ DEFERRED | 3 | 0 |
| Phase 7C | ⏳ DEFERRED | 3 | 0 |
| **Total** | **In Progress** | **47** | **37** |

## Test Summary

| Phase | New Tests | Total |
|-------|-----------|-------|
| Phase 1 | 40 | 40 |
| Phase 2 | 47 | 87 |
| Phase 3 | 18 | 105 |
| Phase 4 | 44 | 149 |
| Phase 5 | 19 | 168 |
