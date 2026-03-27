# S3 Object Storage Abstraction - Implementation Plan

## Executive Summary

Abstract the current singleton S3 object storage implementation into a provider-agnostic, multi-tenant, database-driven configuration system — mirroring the email SMTP abstraction pattern already implemented. This enables:

- **Any S3-compatible provider**: Hetzner, MinIO, AWS S3, Backblaze B2, Wasabi, DigitalOcean Spaces, Cloudflare R2
- **Per-tenant storage configuration**: Instance admin sets defaults; tenants can override (if unlocked)
- **Private bucket + presigned URL pattern**: Secure for Blazor WASM (no secrets in browser)
- **Cascading settings via `ISettingsResolver`**: Same governance hierarchy as email

## Current State Analysis

### What Exists Today

| Component | File | Issue |
|-----------|------|-------|
| Interface | `Explore.Application/Contracts/Infrastructure/IObjectStorageService.cs` | OK — clean contract, can remain unchanged |
| Implementation | `Explore.Infrastructure/Services/ObjectStorageService.cs` | Singleton `IAmazonS3` client, not per-tenant |
| Config POCO | `Explore.Infrastructure/S3Settings.cs` | In wrong layer (Infrastructure, not Application). Only 6 flat properties |
| DI Registration | `InfrastructureServicesRegistration.cs` | Singleton `IAmazonS3`, Transient service — all tenants share one config |
| Env var mapping | `ConfigurationExtensions.cs` | Maps `ISLAMU_EVENT_*` env vars to `S3Settings` section |
| Controller | `StorageObjectController.cs` | 8 endpoints — no changes needed |
| CQRS handlers | 4 handlers in `Features/StorageObjects/Handlers/` | Inject `IObjectStorageService` — no changes needed |
| Blazor client | `ImageStorageService.cs` | Presigned URL workflow — no changes needed |

### Architecture Gaps

1. **No per-tenant S3 configuration** — all tenants share one bucket/credentials
2. **No `IS3ConfigResolver`** — email has `ISmtpConfigResolver`, S3 has nothing
3. **No `GovernanceSettingKeys` for S3** — email has 9 constants, S3 has zero
4. **No seed data for S3 settings** — email has `SeedIds` + `LookupTableSeeder` entries
5. **Config POCO in wrong layer** — `S3Settings.cs` is in Infrastructure; should be Application
6. **Singleton `IAmazonS3` client** — blocks per-tenant resolution
7. **Hardcoded upload URL expiration** — `const int UploadUrlExpirationMinutes = 40`
8. **No test connection feature** — email has `TestConnectionAsync()`
9. **No caching** — email uses `IMemoryCache` with 5-min expiry per tenant

## Research Report: Private Buckets, Public Buckets & Archive Tiers

### Private Buckets (Current Pattern — Keep)

The current Hetzner private bucket + presigned URL approach is **correct and secure**:

- **How it works**: Server generates time-limited presigned URLs; browser uploads/downloads directly to S3 using these URLs. No credentials exposed to client.
- **Why it matters for Blazor WASM**: WebAssembly runs in the browser — secrets would be visible in browser dev tools. Presigned URLs solve this by keeping credentials server-side.
- **Works with all render modes**: Whether Blazor Interactive Auto runs in WASM or SSR mode, the API call to get a presigned URL goes through the BFF/API. The presigned URL pattern is render-mode agnostic.

### Public Buckets — Not Recommended Now

**Recommendation: Do NOT add public bucket support in this phase.**

Reasons:
- Public buckets bypass access control (anyone with the URL can access files permanently)
- Presigned URLs already provide "public-like" access with time-limited expiry
- Adding public bucket support doubles the configuration surface for minimal benefit
- Can be added later as a per-bucket policy if needed (YAGNI)

**When public buckets would make sense** (future, not now):
- Static assets (CSS, JS, logos) that should be CDN-cached
- Public event images that don't need access control
- Would require a separate "public assets bucket" concept alongside the private storage bucket

### Archive/Storage Tiers — Not Recommended Now

**Recommendation: Do NOT implement archive tiers in this phase.**

S3 storage classes (Standard, Infrequent Access, Glacier, etc.) are:
- **Provider-specific** — not all S3-compatible providers support them (Hetzner/MinIO don't)
- **Complex lifecycle management** — requires lifecycle policies, retrieval delays, cost modeling
- **Premature optimization** — storage costs are minimal at current scale
- **Retrieval latency** — archived files can take hours to retrieve (bad UX)

**When archive tiers would make sense** (future, not now):
- Tenant storage exceeds cost thresholds (e.g., >100 GB per tenant)
- Legal/compliance requirement to retain old event data cheaply
- Would be a separate "Storage Lifecycle Management" feature

### Blazor WASM/SSR Render Mode Considerations

The presigned URL pattern is **already render-mode safe**:

| Scenario | Flow | Secrets Exposed? |
|----------|------|-------------------|
| **WASM mode** | WASM → API call → presigned URL → direct S3 upload | No — API generates URL server-side |
| **SSR mode** | Server → API call → presigned URL → client redirect to S3 | No — same pattern |
| **Runtime mode switch** | No change — both modes call API for presigned URL | No |

**Key insight**: The `IObjectStorageService` interface is called from CQRS handlers in the API project, never from Blazor directly. The Blazor client only talks to the API, which resolves storage config server-side. Render mode is irrelevant to S3 configuration.

### S3-Compatible Provider Compatibility Matrix

| Provider | ForcePathStyle | Region Format | Presigned URLs | Notes |
|----------|---------------|---------------|----------------|-------|
| **Hetzner** | `true` (required) | `fsn1`, `nbg1` | Full support | Current provider |
| **MinIO** | `true` (required) | Any string | Full support | Docker dev environment |
| **AWS S3** | `false` (default) | Standard AWS regions | Full support | The reference implementation |
| **Backblaze B2** | `true` (required) | `us-west-004` etc. | Full support | Cheapest bulk storage |
| **Cloudflare R2** | `true` (required) | `auto` | Full support, max 7 days | No egress fees |
| **Wasabi** | `true` | Standard format | Full support | No egress fees |
| **DigitalOcean Spaces** | `false` (supports virtual-hosted) | `nyc3`, `ams3` etc. | Full support | CDN built-in |

**Critical AWS SDK detail**: When using a custom `ServiceURL` (any non-AWS provider), you must set `AuthenticationRegion` as a string, NOT `RegionEndpoint`. Setting both throws `AmazonClientException`.

## Implementation Approach

### Mirror the Email Pattern Exactly

The email SMTP abstraction created a proven pattern. S3 will follow the same architecture:

| Email Component | S3 Equivalent |
|----------------|---------------|
| `GovernanceSettingKeys.EmailSmtp*` | `GovernanceSettingKeys.S3*` |
| `SmtpConfiguration` (Application/Models) | `S3Configuration` (Application/Models) |
| `ISmtpConfigResolver` (Application/Contracts) | `IS3ConfigResolver` (Application/Contracts) |
| `SmtpConfigResolver` (Infrastructure/Mail) | `S3ConfigResolver` (Infrastructure/Storage) |
| `SmtpEmailService` (Infrastructure/Mail) | Refactored `ObjectStorageService` (Infrastructure/Services) |
| `SeedIds.SystemSettingEmailSmtp*` | `SeedIds.SystemSettingS3*` |
| 9 email SystemSetting seed entries | 8 S3 SystemSetting seed entries |

### What Does NOT Change

- `IObjectStorageService` interface — consumers (handlers, controller) don't change
- `StorageObjectController` — API endpoints stay the same
- `ImageStorageService` (Blazor client) — still uses presigned URLs via API
- CQRS handlers — still inject `IObjectStorageService`
- `StorageObject` domain entity — file metadata unchanged
- Docker/MinIO setup — still works for local development

### What Changes

1. **S3 config moves from appsettings/env → database** (via `ISettingsResolver`)
2. **Per-tenant S3 resolution** — each tenant can have its own bucket/credentials
3. **Instance admin can lock S3 settings** — same `IsLocked` governance
4. **`IAmazonS3` no longer singleton** — created per-tenant in resolver, cached
5. **Upload URL expiration becomes configurable** — stored in settings

---

## Implementation Phases

### Phase 1: Application Layer — Config POCO & Interface (30 min)

**Goal**: Define the S3 configuration model and resolver contract in the Application layer.

#### Task 1.1: Create `S3Configuration` POCO
- **File**: `Explore.Application/Models/S3Configuration.cs`
- **Pattern**: Mirror `SmtpConfiguration.cs`
- **Properties**:
  - `Region` (string) — S3 region
  - `BucketName` (string, required) — bucket name
  - `AccessKeyId` (string, required) — access key
  - `SecretAccessKey` (string, required) — secret key
  - `Endpoint` (string, required) — S3 endpoint URL (e.g., `https://fsn1.your-objectstorage.com`)
  - `PublicEndpoint` (string?) — separate endpoint for presigned URLs (if different from internal)
  - `ForcePathStyle` (bool, default: true) — path-style vs virtual-hosted
  - `UploadUrlExpirationMinutes` (int, default: 60) — presigned upload URL TTL
  - `DownloadUrlExpirationMinutes` (int, default: 60) — presigned download URL TTL
- **Acceptance**: Compiles; no external dependencies; clean POCO

#### Task 1.2: Create `IS3ConfigResolver` Interface
- **File**: `Explore.Application/Contracts/Infrastructure/IS3ConfigResolver.cs`
- **Pattern**: Exact mirror of `ISmtpConfigResolver`
- **Methods**:
  - `Task<S3Configuration?> ResolveAsync(CancellationToken)` — resolve effective config for current tenant
  - `void InvalidateCache(Guid? tenantId)` — invalidate cached config
- **Acceptance**: Compiles; XML docs match SMTP resolver style

### Phase 2: Domain Layer — Governance Setting Keys (15 min)

**Goal**: Add S3 setting key constants to `GovernanceSettingKeys.cs`.

#### Task 2.1: Add S3 Setting Keys to `GovernanceSettingKeys.cs`
- **File**: `Explore.Domain/Constants/GovernanceSettingKeys.cs`
- **Keys to add** (8 total):
  - `S3Region` = `"s3.region"`
  - `S3BucketName` = `"s3.bucket_name"`
  - `S3AccessKeyId` = `"s3.access_key_id"`
  - `S3SecretAccessKey` = `"s3.secret_access_key"`
  - `S3Endpoint` = `"s3.endpoint"`
  - `S3PublicEndpoint` = `"s3.public_endpoint"`
  - `S3ForcePathStyle` = `"s3.force_path_style"`
  - `S3UploadUrlExpirationMinutes` = `"s3.upload_url_expiration_minutes"`
- **Acceptance**: Compiles; follows email naming convention (`s3.*` prefix)

### Phase 3: Persistence Layer — Seed Data (30 min)

**Goal**: Add seed IDs and SystemSetting entries for S3 configuration.

#### Task 3.1: Add S3 Seed IDs to `SeedIds.cs`
- **File**: `Explore.Persistence/Seed/SeedIds.cs`
- **IDs**: 8 new IDs in range `0530-0537` (email used `0520-0528`)
- **Naming**: `SystemSettingS3RegionId`, `SystemSettingS3BucketNameId`, etc.
- **Acceptance**: Unique GUIDs; follow UUIDv7 pattern; no collision with existing

#### Task 3.2: Add S3 SystemSetting Seed Entries to `LookupTableSeeder.cs`
- **File**: `Explore.Persistence/Seed/LookupTableSeeder.cs`
- **Entries**: 8 SystemSetting rows matching the keys from Phase 2
- **Default values**:
  - Region: `"fsn1"` (Hetzner default)
  - BucketName: empty (must be configured)
  - AccessKeyId: empty (must be configured)
  - SecretAccessKey: empty (must be configured)
  - Endpoint: empty (must be configured)
  - PublicEndpoint: empty (optional)
  - ForcePathStyle: `"true"`
  - UploadUrlExpirationMinutes: `"60"`
- **Category**: `"ObjectStorage"` (consistent section in admin UI)
- **IsLocked**: `false` (tenants can override by default)
- **Acceptance**: Build passes; seeder method called in seed flow

### Phase 4: Infrastructure Layer — S3ConfigResolver (45 min)

**Goal**: Implement the resolver that reads S3 config from the cascading settings engine.

#### Task 4.1: Create `S3ConfigResolver`
- **File**: `Explore.Infrastructure/Storage/S3ConfigResolver.cs`
- **Pattern**: Exact mirror of `SmtpConfigResolver`
- **Dependencies**: `ISettingsResolver`, `ITenantContext`, `IMemoryCache`, `ILogger<S3ConfigResolver>`
- **Logic**:
  1. Check cache (`S3Config:{tenantId}`)
  2. Resolve each setting via `_settingsResolver.GetSettingAsync<T>(key, tenantId, ct)`
  3. If `Endpoint` is empty/null → return null (S3 not configured)
  4. If `BucketName` is empty/null → return null
  5. If `AccessKeyId` is empty/null → return null
  6. Build `S3Configuration` with defaults for optional fields
  7. Cache for 5 minutes
- **Acceptance**: Compiles; follows SmtpConfigResolver exactly; logs missing config

### Phase 5: Infrastructure Layer — Refactor ObjectStorageService (1 hour)

**Goal**: Make `ObjectStorageService` resolve config per-tenant instead of using the singleton.

#### Task 5.1: Refactor `ObjectStorageService` to Use `IS3ConfigResolver`
- **File**: `Explore.Infrastructure/Services/ObjectStorageService.cs`
- **Changes**:
  - Remove `IOptions<S3Settings>` dependency
  - Remove singleton `IAmazonS3` constructor parameter
  - Add `IS3ConfigResolver` dependency
  - Each method resolves config via `_s3ConfigResolver.ResolveAsync()`
  - Create `IAmazonS3` client on-demand from resolved config (cached by resolver)
  - Use `S3Configuration.UploadUrlExpirationMinutes` instead of hardcoded 40
  - Keep `PublicEndpoint` presign client logic (create from resolved config)
- **Key design decision**: The `IAmazonS3` client creation needs caching. Options:
  - **Option A** (Recommended): Cache the `IAmazonS3` client per tenant inside the resolver alongside the config. The resolver returns `(S3Configuration, IAmazonS3 client, IAmazonS3? presignClient)`.
  - **Option B**: ObjectStorageService creates client per call (wasteful TCP connections)
  - **Option C**: Separate `IS3ClientFactory` (over-engineering for now)
- **Acceptance**: All existing IObjectStorageService consumers work without changes; presigned URLs still work

#### Task 5.2: Update DI Registration in `InfrastructureServicesRegistration.cs`
- **File**: `Explore.Infrastructure/InfrastructureServicesRegistration.cs`
- **Changes**:
  - Remove `services.Configure<S3Settings>()` binding
  - Remove `services.AddSingleton<IAmazonS3>()` factory
  - Add `services.AddScoped<IS3ConfigResolver, S3ConfigResolver>()`
  - Change `services.AddTransient<IObjectStorageService, ObjectStorageService>()` to `AddScoped`
- **Keep**: `S3Settings` POCO can remain for backward-compatible env var → initial seed migration (Phase 7)
- **Acceptance**: Build passes; DI resolves correctly

#### Task 5.3: Add `TestConnectionAsync` to `IObjectStorageService`
- **File**: `Explore.Application/Contracts/Infrastructure/IObjectStorageService.cs`
- **New method**: `Task<bool> TestConnectionAsync(CancellationToken ct = default)`
- **Implementation**: Try `ListBucketsAsync()` — validates connectivity + credentials
- **Acceptance**: Returns true if S3 reachable with current config; false otherwise

### Phase 6: Unit Tests (45 min)

**Goal**: Comprehensive tests mirroring the SmtpConfigResolver test suite.

#### Task 6.1: Create `S3ConfigResolverTests`
- **File**: `Event.Application.UnitTests/Infrastructure/S3ConfigResolverTests.cs`
- **Tests** (mirror SmtpConfigResolverTests):
  - `ResolveAsync_EmptyEndpoint_ReturnsNull`
  - `ResolveAsync_NullEndpoint_ReturnsNull`
  - `ResolveAsync_EndpointSetButEmptyBucketName_ReturnsNull`
  - `ResolveAsync_EndpointSetButEmptyAccessKey_ReturnsNull`
  - `ResolveAsync_ValidConfig_ReturnsS3Configuration`
  - `ResolveAsync_DefaultForcePathStyle_True`
  - `ResolveAsync_DefaultUploadExpiration_60`
  - `ResolveAsync_CachesResult_SecondCallSkipsSettings`
  - `InvalidateCache_SpecificTenant_AllowsRefresh`
  - `ResolveAsync_EmptyPublicEndpoint_ReturnsNullPublicEndpoint`
- **Pattern**: TUnit + NSubstitute, same as SmtpConfigResolverTests
- **Acceptance**: All tests pass; covers happy path + edge cases + caching

### Phase 7: Cleanup & Documentation (30 min)

**Goal**: Remove obsolete config patterns; update docs.

#### Task 7.1: Update `docs/CODEBASE_STRUCTURE.md`
- Add new files to the file listing
- Document the S3 configuration hierarchy in the relevant section

#### Task 7.2: Update Environment Variable Backward Compatibility
- `ConfigurationExtensions.cs` env var mapping stays for now (initial seed)
- Document that env vars are the bootstrap mechanism; once admin UI configures settings in DB, env vars are ignored
- Add ABOUTME comments to new files

#### Task 7.3: Report Obsolete Files
- `Explore.Infrastructure/S3Settings.cs` — will be **superseded** by `S3Configuration` in Application layer, but keep during transition for env var bootstrap
- Once admin UI for S3 config exists, `S3Settings.cs` and the env var mapping can be removed

---

## Risk Assessment

| Risk | Likelihood | Impact | Mitigation |
|------|-----------|--------|------------|
| IAmazonS3 client caching complexity | Medium | Medium | Cache client alongside config in resolver; dispose on invalidation |
| Existing tests break during ObjectStorageService refactor | Medium | Low | Interface unchanged; only internal implementation changes |
| Presigned URLs break with per-tenant endpoints | Low | High | Keep same PublicEndpoint logic; just resolve from DB instead of config |
| Migration: existing env var config stops working | Low | High | Keep env vars as bootstrap; seed data populated on first run |

## Success Metrics

- All existing `IObjectStorageService` consumers work without code changes
- S3 config resolves per-tenant from database (verified by unit tests)
- Presigned URLs still work with Hetzner private bucket
- Instance admin can lock S3 settings to enforce all tenants use same config
- Tenants can override S3 config when unlocked
- All existing tests pass + new S3ConfigResolver tests pass

## Estimated Timeline

| Phase | Effort | Cumulative |
|-------|--------|------------|
| Phase 1: Application layer | 30 min | 30 min |
| Phase 2: Domain layer | 15 min | 45 min |
| Phase 3: Persistence layer | 30 min | 1h 15min |
| Phase 4: Infrastructure resolver | 45 min | 2h |
| Phase 5: Refactor ObjectStorageService | 1 hour | 3h |
| Phase 6: Unit tests | 45 min | 3h 45min |
| Phase 7: Cleanup & docs | 30 min | 4h 15min |
| **Total** | **~4 hours** | |
