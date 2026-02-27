# S3 Object Storage Abstraction - Task Checklist

## Phase 1: Application Layer — Config POCO & Interface ✅ COMPLETE

- [x] **1.1** Create `S3Configuration` POCO
  - File: `Explore.Application/Models/S3Configuration.cs`
  - Properties: Region, BucketName, AccessKeyId, SecretAccessKey, Endpoint, PublicEndpoint, ForcePathStyle, UploadUrlExpirationMinutes, DownloadUrlExpirationMinutes
  - ABOUTME comment, file-scoped namespace
  - Acceptance: Compiles; no external dependencies

- [x] **1.2** Create `IS3ConfigResolver` interface
  - File: `Explore.Application/Contracts/Infrastructure/IS3ConfigResolver.cs`
  - Methods: `ResolveAsync(CancellationToken)`, `InvalidateCache(Guid?)`
  - Mirror ISmtpConfigResolver XML docs
  - Acceptance: Compiles; references S3Configuration

## Phase 2: Domain Layer — Governance Setting Keys ✅ COMPLETE

- [x] **2.1** Add S3 setting keys to `GovernanceSettingKeys.cs`
  - File: `Explore.Domain/Constants/GovernanceSettingKeys.cs`
  - 8 keys: S3Region, S3BucketName, S3AccessKeyId, S3SecretAccessKey, S3Endpoint, S3PublicEndpoint, S3ForcePathStyle, S3UploadUrlExpirationMinutes
  - Convention: `"s3.*"` prefix
  - Acceptance: Compiles; follows email naming pattern

## Phase 3: Persistence Layer — Seed Data ✅ COMPLETE

- [x] **3.1** Add S3 seed IDs to `SeedIds.cs`
  - File: `Explore.Persistence/Seed/SeedIds.cs`
  - 8 IDs in range 0530-0537
  - UUIDv7 format matching existing pattern
  - Acceptance: No GUID collisions; builds

- [x] **3.2** Add S3 SystemSetting seed entries to `LookupTableSeeder.cs`
  - File: `Explore.Persistence/Seed/LookupTableSeeder.cs`
  - 8 entries with Category="ObjectStorage", IsLocked=false
  - Default values: ForcePathStyle=true, UploadExpiration=60, others empty
  - Acceptance: Build passes; entries match SeedIds

- [x] **Build check**: `dotnet build --configuration Release --verbosity quiet`

## Phase 4: Infrastructure Layer — S3ConfigResolver ✅ COMPLETE

- [x] **4.1** Create `S3ConfigResolver`
  - File: `Explore.Infrastructure/Storage/S3ConfigResolver.cs`
  - Dependencies: ISettingsResolver, ITenantContext, IMemoryCache, ILogger
  - Cache key: `S3Config:{tenantId}`, 5-min expiry
  - Returns null if Endpoint, BucketName, or AccessKeyId is empty
  - Defaults: ForcePathStyle=true, UploadExpiration=60
  - ABOUTME comment, file-scoped namespace
  - Acceptance: Compiles; mirrors SmtpConfigResolver exactly

- [x] **Build check**: `dotnet build --configuration Release --verbosity quiet`

## Phase 5: Refactor ObjectStorageService ✅ COMPLETE

- [x] **5.1** Refactor `ObjectStorageService` to use `IS3ConfigResolver`
  - File: `Explore.Infrastructure/Services/ObjectStorageService.cs`
  - Removed: `IOptions<S3Settings>` dependency, singleton `IAmazonS3` parameter
  - Added: `IS3ConfigResolver` dependency
  - Each method: resolve config → create client → execute S3 operation
  - Uses `S3Configuration.UploadUrlExpirationMinutes` instead of hardcoded 40
  - Kept PublicEndpoint presign client logic
  - Changed `GeneratePresignedDownloadUrl` return type: `string` → `Task<string>`
  - Updated all ~15 handler callers to use async/await

- [x] **5.2** Update DI registration in `InfrastructureServicesRegistration.cs`
  - Removed: `services.Configure<S3Settings>()`, `services.AddSingleton<IAmazonS3>()`
  - Added: `services.AddScoped<IS3ConfigResolver, S3ConfigResolver>()`
  - Changed: `AddTransient` → `AddScoped` for ObjectStorageService

- [x] **5.3** Add `TestConnectionAsync` to `IObjectStorageService`
  - File: `Explore.Application/Contracts/Infrastructure/IObjectStorageService.cs`
  - New method: `Task<bool> TestConnectionAsync(CancellationToken ct = default)`
  - Implementation: Try ListBucketsAsync → return true/false

- [x] **Build check**: `dotnet build --configuration Release --verbosity quiet`

## Phase 6: Unit Tests ✅ COMPLETE

- [x] **6.1** Create `S3ConfigResolverTests`
  - File: `Event.Application.UnitTests/Infrastructure/S3ConfigResolverTests.cs`
  - 13 tests:
    - ResolveAsync_EmptyEndpoint_ReturnsNull
    - ResolveAsync_NullEndpoint_ReturnsNull
    - ResolveAsync_EndpointSetButEmptyBucketName_ReturnsNull
    - ResolveAsync_EndpointSetButEmptyAccessKey_ReturnsNull
    - ResolveAsync_EndpointSetButEmptySecretKey_ReturnsNull
    - ResolveAsync_ValidConfig_ReturnsS3Configuration
    - ResolveAsync_DefaultForcePathStyle_True
    - ResolveAsync_DefaultUploadExpiration_60
    - ResolveAsync_CustomUploadExpiration_Preserved
    - ResolveAsync_CachesResult_SecondCallSkipsSettings
    - InvalidateCache_SpecificTenant_AllowsRefresh
    - ResolveAsync_EmptyPublicEndpoint_ReturnsNullPublicEndpoint
    - ResolveAsync_NullRegion_DefaultsToUsEast1
  - Pattern: TUnit + NSubstitute

- [x] **Run all tests**: 203 app + 61 domain + 24 architecture = 288 total — all pass

## Phase 7: Cleanup & Documentation ✅ COMPLETE

- [x] **7.1** Update `docs/CODEBASE_STRUCTURE.md`
  - Added new files to file listing
  - Updated ObjectStorageService description

- [x] **7.2** Add ABOUTME comments to all new files
  - S3Configuration.cs, IS3ConfigResolver.cs, S3ConfigResolver.cs, S3ConfigResolverTests.cs

- [x] **7.3** Document env var backward compatibility
  - S3Settings.cs kept during transition for env var bootstrap
  - Once admin UI supports DB-stored S3 config, env vars become fallback only

- [x] **7.4** Report obsolete files for user review
  - `Explore.Infrastructure/S3Settings.cs` — keep during transition, flag for future removal

- [x] **Final build + test run**: 0 errors, 288 tests pass

---

## Summary

| Phase | Tasks | Status |
|-------|-------|--------|
| Phase 1: Application Layer | 2 tasks | ✅ Complete |
| Phase 2: Domain Layer | 1 task | ✅ Complete |
| Phase 3: Persistence Layer | 2 tasks + build check | ✅ Complete |
| Phase 4: Infrastructure Resolver | 1 task + build check | ✅ Complete |
| Phase 5: Refactor Service | 3 tasks + build check | ✅ Complete |
| Phase 6: Unit Tests | 1 task + test runs | ✅ Complete |
| Phase 7: Cleanup & Docs | 4 tasks + final verification | ✅ Complete |
## Context Reset Session Update (2026-02-15 21:26 Europe/Brussels)

- Status update: No task-state changes in this session for this track.
- Priority update: Keep existing ordering; analytics work was handled in a separate track.
- Next step: Resume from current in-progress or highest-priority unchecked item.

## Context Reset Session Update (2026-02-23 18:12 Europe/Brussels)

- Current implementation state: No new implementation changes in this session for this track.
- Key decisions made this session: Priority focused on admin consolidation handoff in navbar customization track.
- Files modified and why: None in this track during this session.
- Blockers/issues discovered: None newly discovered for this track.
- Next immediate steps: Continue from highest-priority unchecked items in this task file.

## Context Reset Session Update (2026-02-23 18:47 Europe/Brussels)

- Current implementation state: No direct implementation changes in this track during this session.
- Key decisions made this session: Prioritized completion and verification of admin consolidation in the navbar customization track.
- Files modified and why: None for this specific track in this session.
- Blockers/issues discovered: None newly discovered for this track.
- Next immediate steps: Continue from the highest-priority unchecked tasks in this track's tasks file.

---

## Session Checkpoint (2026-02-27 Europe/Brussels)

- [x] Reviewed task continuity status for context reset handoff.
- [ ] Resume implementation work from this task latest documented in-progress section.
- [ ] Re-validate with build/tests once implementation resumes.

