# S3 Object Storage Abstraction - Context

## SESSION PROGRESS (2026-02-11)

### Completed
- Full research of current S3 implementation (all files read and analyzed)
- Web research on S3-compatible providers, multi-tenant patterns, credential storage
- Codebase exploration agent completed with comprehensive gap analysis
- Plan document created with 7 phases
- Research report on private/public buckets, archive tiers, WASM/SSR considerations
- Context and tasks files created
- Phase 1: Created `S3Configuration` POCO and `IS3ConfigResolver` interface
- Phase 2: Added 8 S3 governance setting keys to `GovernanceSettingKeys.cs`
- Phase 3: Added 8 seed IDs (0530-0537) and 8 SystemSetting seed entries
- Phase 4: Implemented `S3ConfigResolver` in `Infrastructure/Storage/`
- Phase 5: Refactored `ObjectStorageService` to use `IS3ConfigResolver`, updated DI, added `TestConnectionAsync`, updated `GeneratePresignedDownloadUrl` to `Task<string>` and all 15 callers
- Phase 6: Created 13 unit tests in `S3ConfigResolverTests.cs` — all 203 app unit tests pass
- Phase 7: Updated `CODEBASE_STRUCTURE.md`, updated dev docs

### Blockers
- None

---

## Key Decisions

1. **Mirror email pattern exactly** — Same architecture as SmtpConfigResolver (ISettingsResolver cascade, IMemoryCache, per-tenant resolution)
2. **Keep IObjectStorageService interface unchanged** — All 23+ consumer files don't need changes
3. **No public bucket support** — Private + presigned URLs is sufficient (YAGNI)
4. **No archive tiers** — Not supported by all providers; premature optimization
5. **ForcePathStyle defaults to true** — Required by most non-AWS providers (Hetzner, MinIO, B2, R2)
6. **Cache IAmazonS3 client per tenant in resolver** — Avoid creating TCP connections per request
7. **Keep env var bootstrap** — S3Settings.cs stays during transition; env vars populate initial seed data
8. **S3 seed IDs use range 0530-0537** — After email range 0520-0528

## Key Files

### Current Implementation (To Be Refactored)

**`Explore.Infrastructure/Services/ObjectStorageService.cs`**
- Main S3 service implementation
- Uses singleton `IAmazonS3` client + `IOptions<S3Settings>`
- Has `_presignClient` for PublicEndpoint presigned URLs
- Hardcoded 40-min upload URL expiration
- **Will be refactored** to use `IS3ConfigResolver` instead

**`Explore.Infrastructure/S3Settings.cs`**
- Simple 6-property POCO (Region, BucketName, AccessKeyId, SecretAccessKey, Endpoint, PublicEndpoint)
- Located in Infrastructure layer (should be Application for Clean Architecture)
- **Will be superseded** by `S3Configuration` in Application/Models but kept for env var bootstrap

**`Explore.Infrastructure/InfrastructureServicesRegistration.cs`** (lines 29-78)
- Singleton `IAmazonS3` client factory with complex endpoint logic
- Transient `IObjectStorageService` registration
- **Will be simplified** to scoped `IS3ConfigResolver` + scoped service

**`Explore.Application/Contracts/Infrastructure/IObjectStorageService.cs`**
- 3 methods: GeneratePresignedUploadUrl, GeneratePresignedDownloadUrl, GetFileStream
- **Minor change**: Add `TestConnectionAsync` method

### Reference Pattern (Email — Already Implemented)

**`Explore.Infrastructure/Mail/SmtpConfigResolver.cs`**
- The exact pattern to replicate for S3
- Uses `ISettingsResolver`, `ITenantContext`, `IMemoryCache`
- 5-min cache with `SmtpConfig:{tenantId}` key
- Returns null if host is empty → S3 equivalent: return null if endpoint is empty

**`Explore.Application/Contracts/Infrastructure/ISmtpConfigResolver.cs`**
- Interface: `ResolveAsync()` + `InvalidateCache(Guid?)`
- S3 will have identical interface shape

**`Explore.Application/Models/SmtpConfiguration.cs`**
- Clean POCO with all config properties
- S3 equivalent will have: Region, BucketName, AccessKeyId, SecretAccessKey, Endpoint, PublicEndpoint, ForcePathStyle, UploadUrlExpirationMinutes

**`Explore.Domain/Constants/GovernanceSettingKeys.cs`**
- Has email keys (Email*) at the bottom
- S3 keys will be added in new section below email

**`Explore.Persistence/Seed/SeedIds.cs`**
- Email IDs: 0520-0528
- S3 IDs: 0530-0537

**`Explore.Persistence/Seed/LookupTableSeeder.cs`**
- Contains `SeedSystemSettings()` method with all SystemSetting entries
- S3 entries will be added at the end with Category = "ObjectStorage"

### Consumers (No Changes Needed)

- `StorageObjectController.cs` — API endpoints
- 4 CQRS handlers in `Features/StorageObjects/Handlers/`
- `ImageStorageService.cs` — Blazor client
- `StorageObjectRepository.cs` — persistence

### Test Files (To Be Created)

- `Event.Application.UnitTests/Infrastructure/S3ConfigResolverTests.cs` — new

## Provider Compatibility Notes

- **Hetzner** (current): `ServiceURL=https://fsn1.your-objectstorage.com`, ForcePathStyle=true, AuthenticationRegion="fsn1"
- **MinIO** (Docker dev): `ServiceURL=http://localhost:9000`, ForcePathStyle=true
- **AWS S3**: Use `RegionEndpoint` (not ServiceURL), ForcePathStyle=false
- **Critical**: Never set both `ServiceURL` and `RegionEndpoint` — throws `AmazonClientException`

## Quick Resume

To continue:
1. Read this file for current state
2. Read `s3-storage-abstraction-tasks.md` for checklist
3. Start with Phase 1 (Application layer)
4. Build after each phase to catch errors early
5. Run tests after Phase 6
