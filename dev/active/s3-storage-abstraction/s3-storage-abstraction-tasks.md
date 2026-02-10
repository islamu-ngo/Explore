# S3 Object Storage Abstraction - Task Checklist

## Phase 1: Application Layer — Config POCO & Interface ⏳ NOT STARTED

- [ ] **1.1** Create `S3Configuration` POCO
  - File: `Explore.Application/Models/S3Configuration.cs`
  - Properties: Region, BucketName, AccessKeyId, SecretAccessKey, Endpoint, PublicEndpoint, ForcePathStyle, UploadUrlExpirationMinutes, DownloadUrlExpirationMinutes
  - ABOUTME comment, file-scoped namespace
  - Acceptance: Compiles; no external dependencies

- [ ] **1.2** Create `IS3ConfigResolver` interface
  - File: `Explore.Application/Contracts/Infrastructure/IS3ConfigResolver.cs`
  - Methods: `ResolveAsync(CancellationToken)`, `InvalidateCache(Guid?)`
  - Mirror ISmtpConfigResolver XML docs
  - Acceptance: Compiles; references S3Configuration

## Phase 2: Domain Layer — Governance Setting Keys ⏳ NOT STARTED

- [ ] **2.1** Add S3 setting keys to `GovernanceSettingKeys.cs`
  - File: `Explore.Domain/Constants/GovernanceSettingKeys.cs`
  - 8 keys: S3Region, S3BucketName, S3AccessKeyId, S3SecretAccessKey, S3Endpoint, S3PublicEndpoint, S3ForcePathStyle, S3UploadUrlExpirationMinutes
  - Convention: `"s3.*"` prefix
  - Acceptance: Compiles; follows email naming pattern

## Phase 3: Persistence Layer — Seed Data ⏳ NOT STARTED

- [ ] **3.1** Add S3 seed IDs to `SeedIds.cs`
  - File: `Explore.Persistence/Seed/SeedIds.cs`
  - 8 IDs in range 0530-0537
  - UUIDv7 format matching existing pattern
  - Acceptance: No GUID collisions; builds

- [ ] **3.2** Add S3 SystemSetting seed entries to `LookupTableSeeder.cs`
  - File: `Explore.Persistence/Seed/LookupTableSeeder.cs`
  - 8 entries with Category="ObjectStorage", IsLocked=false
  - Default values: ForcePathStyle=true, UploadExpiration=60, others empty
  - Acceptance: Build passes; entries match SeedIds

- [ ] **Build check**: `dotnet build --configuration Release --verbosity quiet`

## Phase 4: Infrastructure Layer — S3ConfigResolver ⏳ NOT STARTED

- [ ] **4.1** Create `S3ConfigResolver`
  - File: `Explore.Infrastructure/Storage/S3ConfigResolver.cs`
  - Dependencies: ISettingsResolver, ITenantContext, IMemoryCache, ILogger
  - Cache key: `S3Config:{tenantId}`, 5-min expiry
  - Returns null if Endpoint, BucketName, or AccessKeyId is empty
  - Defaults: ForcePathStyle=true, UploadExpiration=60
  - ABOUTME comment, file-scoped namespace
  - Acceptance: Compiles; mirrors SmtpConfigResolver exactly

- [ ] **Build check**: `dotnet build --configuration Release --verbosity quiet`

## Phase 5: Refactor ObjectStorageService ⏳ NOT STARTED

- [ ] **5.1** Refactor `ObjectStorageService` to use `IS3ConfigResolver`
  - File: `Explore.Infrastructure/Services/ObjectStorageService.cs`
  - Remove: `IOptions<S3Settings>` dependency, singleton `IAmazonS3` parameter
  - Add: `IS3ConfigResolver` dependency
  - Each method: resolve config → create/cache client → execute S3 operation
  - Use `S3Configuration.UploadUrlExpirationMinutes` instead of hardcoded 40
  - Keep PublicEndpoint presign client logic
  - Acceptance: Interface unchanged; all consumers work without changes

- [ ] **5.2** Update DI registration in `InfrastructureServicesRegistration.cs`
  - Remove: `services.Configure<S3Settings>()`, `services.AddSingleton<IAmazonS3>()`
  - Add: `services.AddScoped<IS3ConfigResolver, S3ConfigResolver>()`
  - Change: `AddTransient` → `AddScoped` for ObjectStorageService
  - Acceptance: Build passes; DI graph resolves

- [ ] **5.3** Add `TestConnectionAsync` to `IObjectStorageService`
  - File: `Explore.Application/Contracts/Infrastructure/IObjectStorageService.cs`
  - New method: `Task<bool> TestConnectionAsync(CancellationToken ct = default)`
  - Implementation: Try ListBucketsAsync → return true/false
  - Acceptance: Method exists on interface and implementation

- [ ] **Build check**: `dotnet build --configuration Release --verbosity quiet`

## Phase 6: Unit Tests ⏳ NOT STARTED

- [ ] **6.1** Create `S3ConfigResolverTests`
  - File: `Event.Application.UnitTests/Infrastructure/S3ConfigResolverTests.cs`
  - Tests (10+):
    - ResolveAsync_EmptyEndpoint_ReturnsNull
    - ResolveAsync_NullEndpoint_ReturnsNull
    - ResolveAsync_EndpointSetButEmptyBucketName_ReturnsNull
    - ResolveAsync_EndpointSetButEmptyAccessKey_ReturnsNull
    - ResolveAsync_ValidConfig_ReturnsS3Configuration
    - ResolveAsync_DefaultForcePathStyle_True
    - ResolveAsync_DefaultUploadExpiration_60
    - ResolveAsync_CustomUploadExpiration_Preserved
    - ResolveAsync_CachesResult_SecondCallSkipsSettings
    - InvalidateCache_SpecificTenant_AllowsRefresh
    - ResolveAsync_EmptyPublicEndpoint_ReturnsNullPublicEndpoint
  - Pattern: TUnit + NSubstitute (same as SmtpConfigResolverTests)
  - Acceptance: All tests pass

- [ ] **Run all tests**:
  ```
  dotnet test --project Event.Application.UnitTests/Event.Application.UnitTests.csproj --configuration Release --verbosity quiet
  dotnet test --project Event.Domain.UnitTests/Event.Domain.UnitTests.csproj --configuration Release --verbosity quiet
  dotnet test --project Event.Architecture.Tests/Event.Architecture.Tests.csproj --configuration Release --verbosity quiet
  ```

## Phase 7: Cleanup & Documentation ⏳ NOT STARTED

- [ ] **7.1** Update `docs/CODEBASE_STRUCTURE.md`
  - Add new files to file listing
  - Document S3 configuration hierarchy

- [ ] **7.2** Add ABOUTME comments to all new files

- [ ] **7.3** Document env var backward compatibility
  - ConfigurationExtensions.cs env var mapping stays for bootstrap
  - Once DB-stored config is active, env vars are fallback only

- [ ] **7.4** Report obsolete files for user review
  - `S3Settings.cs` — keep during transition, flag for future removal

- [ ] **Final build + test run**

---

## Summary

| Phase | Tasks | Status |
|-------|-------|--------|
| Phase 1: Application Layer | 2 tasks | ⏳ Not Started |
| Phase 2: Domain Layer | 1 task | ⏳ Not Started |
| Phase 3: Persistence Layer | 2 tasks + build check | ⏳ Not Started |
| Phase 4: Infrastructure Resolver | 1 task + build check | ⏳ Not Started |
| Phase 5: Refactor Service | 3 tasks + build check | ⏳ Not Started |
| Phase 6: Unit Tests | 1 task + test runs | ⏳ Not Started |
| Phase 7: Cleanup & Docs | 4 tasks + final verification | ⏳ Not Started |
