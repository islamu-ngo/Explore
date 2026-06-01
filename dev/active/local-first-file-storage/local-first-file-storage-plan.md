<!-- ABOUTME: CTO-rebaselined implementation plan for making local filesystem storage the default backend. -->
<!-- ABOUTME: Splits provider, quota, API, BFF, admin UI, and operator work into reviewable slices. -->

# Local-First File Storage - Implementation Plan

Last Updated: 2026-05-31 Europe/Brussels

## 0. Planning Metadata

- **Request:** Make server-local file storage the default backend while keeping S3-compatible object storage optional. Instance and tenant admins need controls for provider choice, upload limits, quotas, usage, health, and operator actions.
- **Task directory:** `dev/active/local-first-file-storage/`
- **Planning status:** PR 1 foundation, PR 2 provider/policy foundations, PR 3.1 Application upload-session commands, PR 3.2 API upload-session endpoints, PR 3.3 metadata-driven download/public image handlers, PR 3.4 arbitrary-key route removal, PR 3.5 BFF upload-session/proxy refactor, PR 3.6 OpenAPI/generated-client regeneration, PR 4.1 instance storage admin CQRS/API, PR 4.2 tenant storage admin CQRS/API, PR 4.3 storage readiness health checks, and PR 4.4 storage metrics are implemented after user approval - do not implement remaining work as one mega-PR.
- **Senior CTO decision:** Split before approval. The direction is right, but storage provider selection, persistence/quota integrity, API contract changes, BFF upload security, Blazor admin UX, and operations docs must be delivered in separate reviewable slices.
- **Matched intents:** No exact intent exists for cross-layer storage-provider architecture. Fallback Contract applies. Closest intents: `add-cqrs-handler`, `add-write-endpoint`, `add-get-endpoint`, `add-ef-migration`, `openapi-contract-change`, `add-hal-link`, `blazor-component-affordance`.
- **Relevant skills loaded:** `senior-cto-feedback`, `clean-architecture-rules`, `cqrs-mediatr-guidelines`, `dotnet-efcore-guidelines`, `auth-patterns`, `blazor-bff-patterns`, `blazor-ui-conventions`, `design-system`, `accessibility`, `error-tracking`, `aspire`, `outbox-pattern`.
- **Relevant rules loaded:** `.claude/rules/domain.md`, `application-layer.md`, `efcore-persistence.md`, `efcore-migrations.md`, `api-controllers.md`, `api-hateoas.md`, `blazor-server.md`, `blazor-client.md`, `tests.md`.
- **Primary layers touched:** Domain, Application, Persistence, Infrastructure, API, Blazor BFF, Blazor Client, Docs, Docker Compose, Aspire.
- **Estimated complexity:** XL. The safe path is a sequence of small PRs, not one cross-layer feature branch.
- **Baseline verification:** `dotnet build --configuration Release --verbosity quiet` passed on 2026-05-29 with existing package/deprecation warnings.
- **Latest verification:** `Explore.Application`, `Event.Application.UnitTests`, and `Explore.API` Release builds passed on 2026-05-31 after PR 4.4 with existing warnings. Focused storage metrics, upload-session, and storage content-reader TUnit tests passed individually with `--treenode-filter`. Context7 .NET docs were used for current `System.Diagnostics.Metrics`/OpenTelemetry instrumentation guidance. User reported OpenAPI schema and NSwag client regeneration after PR 4.2; PR 4.3 and PR 4.4 did not add OpenAPI-described controller contracts. Full Architecture was rerun and remains blocked by unrelated active-worktree failures documented in context. Root Release build and `Explore.Blazor.Client.Tests` remain blocked by unrelated active-worktree failures until rechecked.

### Contribution Contract Answers

| Question | Answer |
|---|---|
| Intent | Fallback Contract. Add a future `storage-provider-change` intent if this workstream recurs. |
| Authoritative rules | `AGENTS.md`, `docs/QUICK_REFERENCE.md`, `docs/GOVERNANCE.md`, matching `.claude/rules/*`, and this re-baselined plan. |
| Must-read docs | `docs/STORAGE.md`, `docs/CONFIGURATION.md`, `docs/SELF_HOSTING.md`, `docs/BACKUP_RESTORE_UPGRADE.md`, `docs/SECURITY-MODEL.md`, `docs/BLAZOR.md`, `docs/API.md`, `docs/AUTHORIZATION.md`, `docs/MULTI_TENANCY.md`, `docs/ARCHITECTURE.md`, `docs/DOMAIN.md`, `docs/SECRETS.md`, `docs/DEPLOYMENT_MODES.md`, `docs/ADMIN_GUIDE.md`, `docs/ADMIN_HIERARCHY.md`, `docs/OPERATIONS.md`, `docs/ACCESSIBILITY.md`, `docs/DESIGN_SYSTEM.md`, `docs/TESTING.md`. |
| Paths in scope | `Explore.Domain/**`, `Explore.Application/**`, `Explore.Persistence/**`, `Explore.Infrastructure/**`, `Explore.API/**`, `Explore.Blazor/**`, `Explore.Blazor.Client/**`, tests, `docs/**`, `schemas/**`, `docker-compose.yml`, `Explore.AppHost/**`. |
| Minimum tests | Build plus the touched project lanes: Domain, Application, Infrastructure, Persistence integration, API integration, Blazor integration, Blazor client, Architecture. Run each test project individually. |
| Docs to update | `docs/STORAGE.md`, `docs/CONFIGURATION.md`, `docs/SELF_HOSTING.md`, `docs/BACKUP_RESTORE_UPGRADE.md`, `docs/OPERATIONS.md`, `docs/TROUBLESHOOTING.md`, `docs/ADMIN_GUIDE.md`, `docs/SECURITY-MODEL.md`, `docs/BLAZOR.md`, `docs/API.md`, `docs/API_CHANGELOG.md`, `docs/API_CONTRACT_INVENTORY.md`, `docs/index.md`, `schemas/islamu-event.md`. |
| PR checklist | Treat as persistence + API contract + auth/HAL + BFF + Blazor UX + operations change. Regenerate OpenAPI/client only after API contract stabilizes. |
| Forbidden without approval | Making S3/MinIO required, exposing browser tokens/secrets, allowing UI-local role gates for storage actions, bypassing tenant filters, serving the local storage root as static files, letting admins write arbitrary filesystem paths from the UI, deleting bytes without quarantine/recovery, or keeping public arbitrary-key endpoints for local files. |

## 1. Executive Summary

The target is a local-first storage platform: a fresh self-hosted install must upload, download, and display files using an API-owned filesystem data root with no S3, MinIO, or object-storage dependency. S3-compatible storage remains available, but only when explicitly selected and configured.

The current code is S3-shaped from DTOs through BFF transport and admin UI. The safe implementation path is to introduce storage policy and metadata first, then a local provider, then a provider-neutral upload/download contract, then admin/UI/operations. Do not begin by modifying `ObjectStorageService` alone; that would preserve S3 semantics under a new name.

Out of scope for the first release: CDN integration, media transformation, virus-scanner engine selection, archive tiers, cross-region replication, and broad document-management product workflows. Add scanner and cleanup hooks, but keep engine/provider selection deferred.

## 2. Source-Grounded Current State Report

### 2.1 Evidence Log

| Claim | Evidence | Confidence | Notes |
|---|---|---:|---|
| Runtime storage is S3-shaped. | `Explore.Application/Contracts/Infrastructure/IObjectStorageService.cs`; `Explore.Infrastructure/Services/ObjectStorageService.cs`. | High | Interface exposes presigned upload/download URLs and S3 stream retrieval. |
| Missing S3 config fails runtime upload/read paths. | `ObjectStorageService` throws `InvalidOperationException` when `S3ConfigResolver.ResolveAsync()` returns null. | High | Local install without reachable/configured S3 cannot upload through current flow. |
| Compose injects MinIO-oriented S3 defaults even though MinIO is optional. | `docker-compose.yml` `x-s3-env`; `minio` profile is optional and `required: false`. | High | API may have complete S3 settings pointing at an unavailable optional service. |
| Existing storage settings are only `s3.*` plus S3 secrets. | `StorageSettingDefinitions.cs`; `GovernanceSettingKeys.Storage`; `docs/CONFIGURATION.md`. | High | No provider, local root, quota, max-upload, or visibility keys exist. |
| Instance admin UI is S3-only. | `InstanceStorageSection.razor`; `InstanceStorageSettingsDto`; generated client docs. | High | It exposes endpoint, bucket, credentials, region, path style, presigned expiration. |
| Tenant admin storage UI is a placeholder. | `TenantAdminSettingsLayout.razor`. | High | It advertises future tenant storage overrides but has no controls. |
| Browser upload BFF is bound to S3 presigned URLs. | `BffStorageEndpoints.cs`; `IStorageUploadSessionStore`. | High | `/upload-session` calls `api/storageobject/generate-upload-url`; `/upload-proxy` PUTs to a stored URL. |
| BFF upload size is hard-coded. | `BffStorageEndpoints.cs`: `const long maxUploadBytes = 10 * 1024 * 1024`. | High | Not tied to tenant/instance policy. |
| Blazor upload service reads files into memory and has S3 terminology. | `ImageStorageService.cs`, `ImageUploadClient.cs`, tests. | High | Current image flow is acceptable for small images, not generic large files. |
| `ImageUpload.razor` defaults to 5 MB. | `ImageUpload.razor`. | High | UI size limit is local component state, not policy-driven. |
| `StorageObject` metadata is minimal and public-read oriented. | `StorageObject.cs`, `StorageObjectDto.cs`, `StorageObjectController.cs`. | High | No provider, object key, content type, checksum, visibility, lifecycle, owner resource, soft delete. |
| Public arbitrary-key endpoints exist. | `StorageObjectController.GetFile`, `GetPresignedDownloadUrlByKey`; corresponding handlers. | High | These bypass metadata ID ownership/visibility decisions. They must not carry forward for local provider. |
| Generate upload URL has only endpoint auth, not MediatR resource authorization. | `GenerateUploadUrlCommand.cs`; `GenerateUploadUrlCommandHandler.cs`. | High | New upload session command must declare resource/action authorization where appropriate. |
| Delete only deletes metadata. | `DeleteStorageObjectCommandHandler.cs`. | High | Comment claims blob deletion, but no provider delete is called. |
| HAL infrastructure exists but controller does not assemble HAL responses. | `StorageObjectResourceAssembler.cs`, `StorageObjectLinkPolicy.cs`, `StorageObjectController.cs`, `StorageObjectHateoasTests.cs`. | Medium | HATEOAS tests tolerate missing `_links`. |
| Existing docs state object storage/MinIO/S3 behavior. | `docs/STORAGE.md`, `docs/SELF_HOSTING.md`, `docs/BACKUP_RESTORE_UPGRADE.md`. | High | Docs need local data-root backup/restore defaults. |

### 2.2 Existing Implementation

- **Domain:** `StorageObject` is tenant-scoped and auditable, but not soft-deletable. It stores `Uri`, `FullName`, `Extension`, `Size`, `FileTypeId`, `TenantId`, optional `ActorId`.
- **Application:** Storage CQRS handlers map DTOs to entities and call `IObjectStorageService` for presigned URLs/streams. Create/delete have authorization attributes; upload URL generation does not.
- **Persistence:** `StorageObjectRepository` returns entities and uses `AsNoTracking()` for reads. There is no usage/reservation table and no provider/visibility/lifecycle metadata.
- **Infrastructure:** `ObjectStorageService` is AWS SDK S3-compatible only. `S3ConfigResolver` reads `s3.*` database settings, then `S3Settings:*` config fallback.
- **API:** `StorageObjectController` exposes public list/detail/file/public-image/presigned endpoints plus authenticated metadata writes. It returns plain DTOs rather than enforcing HAL response shape.
- **BFF:** The upload proxy correctly rejects caller-supplied destinations, but the trusted destination model is currently a presigned S3 URL.
- **Blazor:** Admin instance storage is S3-only. Tenant storage is placeholder. Upload/display services are image/S3-oriented.
- **Operations:** MinIO is optional, but Compose still injects MinIO S3 defaults into API configuration. No local durable API storage volume exists.

### 2.3 Existing Tests And Verification Coverage

- `Event.API.IntegrationTests/Features/StorageObjectControllerTests.cs`: basic public read and unauthenticated write checks; weak behavior assertions.
- `Event.API.IntegrationTests/Features/Hateoas/StorageObjectHateoasTests.cs`: opportunistic HAL checks; returns early when HAL shape is absent.
- `Explore.Blazor.IntegrationTests/Endpoints/BffStorageUploadProxyTests.cs`: proves arbitrary browser-supplied upload URLs are rejected.
- `Explore.Infrastructure.Tests/Infrastructure/S3ConfigResolverTests.cs`: S3 setting resolution, cache, null behavior, config fallback.
- `Explore.Blazor.Client.Tests/Services/ImageUploadClientTests.cs` and `ImageStorageServiceTests.cs`: current S3/BFF image service behavior.

Missing: local filesystem provider tests, path containment tests, quota reservation concurrency tests, metadata visibility tests, provider-not-supported ProblemDetails tests, no-S3 startup/upload tests, local file range/download tests, storage health checks, admin API tests, HAL tests that require storage links, bUnit admin dashboard tests, and operator docs/context validation.

### 2.4 Current Pain Points / Improvement Areas

1. **Public arbitrary-key access is not acceptable for local storage.** A local provider must resolve files through metadata IDs and visibility/authorization, not raw caller-provided keys.
2. **No file visibility/access model exists.** Generic files/documents cannot inherit the current public metadata/list defaults.
3. **Local root must be deployment-managed.** Letting admins save arbitrary filesystem paths from the web UI is an infrastructure escape hatch and a path-exposure risk.
4. **Quota enforcement does not exist.** `Size` is recorded after upload metadata creation, so there is no pre-write reservation or race-safe capacity check.
5. **S3 is embedded in contracts and UX.** DTO names, generated client summaries, tests, docs, BFF flow, and UI copy assume presigned S3 semantics.
6. **Secrets are not redaction-safe enough for the future UI.** Existing DTOs include access and secret keys; future read models must return configured/source/editability flags, not secret values.
7. **Delete is misleading and incomplete.** Metadata deletion does not delete, quarantine, or reconcile backing bytes.
8. **Upload size policy is fragmented.** BFF hard-codes 10 MB and the image component defaults to 5 MB.
9. **Operational backup defaults are wrong for local-first.** PostgreSQL plus local storage root becomes the default backup set; MinIO/S3 is optional.

### 2.5 Unknowns After Investigation

| Unknown | Resolution task |
|---|---|
| Exact visibility states for public images vs private/generic files | Decide in PR 1. Minimum: public image, authenticated tenant file, private owner/admin file, quarantine/deleted lifecycle. |
| Whether direct API multipart upload is required for non-browser callers in first release | Decide during API contract PR. Browser BFF path is mandatory; direct API path may be same endpoint with bearer/API-key auth. |
| How much S3 compatibility to retain | Keep only explicit S3 provider behavior. Existing presigned endpoints may remain S3-only with clear ProblemDetails, but arbitrary-key local access is banned. |
| How to report filesystem free space inside containers | Best-effort `DriveInfo`/volume stat only. Instance UI sees health/status; tenant UI sees quotas and effective limits, not host paths. |
| Secret ownership migration timing for S3 | Align with current secret ownership model. Do not expose raw S3 secrets in read DTOs; migrate to ownership metadata when implementation reaches admin API/UI. |

## 3. Proposed Future State

- Local filesystem storage is the default provider for new installs.
- S3-compatible storage is optional and selected only by explicit provider policy.
- The local storage root is configured by deployment (`Storage:Local:RootPath` or equivalent), validated at startup, mounted as a durable Compose/AppHost volume, and never edited as an arbitrary path from the browser UI.
- All browser uploads use a server-issued upload session bound to tenant, user, content type, expected size, provider, expiry, and reservation.
- Files are served through application endpoints that resolve metadata, tenant context, visibility, authorization, and provider. The storage root is never exposed as static files.
- Public image URLs use stable application URLs by storage object ID. Raw object-key endpoints are removed, deprecated, or S3-only and never enable local-file access.
- Instance admins manage provider policy, upload ceilings, default tenant quotas, delegation lock, health, usage, and reconciliation actions.
- Tenant admins manage only delegated tenant settings within instance ceilings: stricter max upload, tenant quota, and optional S3 override if allowed.
- UI action affordances come from HAL/status links, not role checks in Razor components.

### Control Flow

```text
Browser selects file
  -> Blazor asks API/BFF for effective storage policy
  -> BFF POST /bff/storage/upload-session
  -> API CreateStorageUploadSessionCommand
       validates auth, tenant, visibility/purpose, content type, expected size
       resolves provider and policy
       creates quota reservation + upload session
  -> Browser uploads multipart bytes to BFF/API session endpoint
  -> Provider writes bytes
       Local: random object key under deployment-managed data root
       S3: optional provider implementation, presigned only where selected
  -> Application finalizes metadata and usage in transaction
  -> API returns StorageObject resource/HAL links
  -> UI renders view/delete/retry/recalculate actions from links/status
```

## 4. Non-Negotiable Constraints

- Repositories return entities; handlers map to DTOs.
- Validators are manually instantiated.
- Use `Guid` for aggregate IDs, `int` for lookup IDs, `long` for byte counts, quotas, and cursors.
- Writes/uploads/admin operations are authorized, rate-limited, and return RFC 7807 ProblemDetails for failures.
- Tenant isolation remains API-authoritative through tenant context and EF filters.
- HAL links/status responses drive UI affordances.
- New C# files start with two `ABOUTME:` comments and use file-scoped namespaces.
- Local files live outside web root and are never served through generic static-file middleware.
- Browser filenames, content types, object keys, and paths are untrusted.
- Logs/metrics must not include local paths, raw object keys where avoidable, S3 credentials, presigned URLs, upload bodies, or raw exception text from providers.
- Admin UI may display a safe data-root health summary, but not write arbitrary filesystem paths.

## 5. Architecture And Design Decisions

### Decision 1: Provider-neutral Application contract, provider-specific Infrastructure

- **Decision:** Add Application contracts such as `IFileStorageProvider`, `IStorageProviderResolver`, `IStoragePolicyResolver`, and storage result models. S3 remains an Infrastructure provider/adapter.
- **Why:** Application must not speak in presigned/S3 terminology for local files.
- **Consequence:** `IObjectStorageService` becomes legacy/S3-only or is removed after call sites migrate.

### Decision 2: Deployment-managed local data root

- **Decision:** Configure local root from deployment config and validate it at startup/health check. Admins can test/status it, not set arbitrary paths in UI.
- **Why:** Runtime path editing from a web admin surface can turn storage into arbitrary server filesystem access.
- **Consequence:** Compose/AppHost must mount/create a durable data root. Docs must cover permissions and backup/restore.

### Decision 3: Metadata ID is the public addressing model

- **Decision:** New local-first read/download endpoints resolve `StorageObject` by ID/session/owned resource, not arbitrary object key.
- **Why:** Object keys are provider internals. Public key endpoints become a leakage and traversal surface for local storage.
- **Consequence:** Existing `file/{*fileKey}` and `presigned-url-by-key/{*objectKey}` must be removed, constrained to S3-only, or return provider-not-supported with explicit API changelog.

### Decision 4: Quota reservation before bytes

- **Decision:** Add upload sessions/reservations before accepting bytes, then finalize usage and metadata exactly once.
- **Why:** Post-write size accounting cannot prevent races or disk exhaustion.
- **Consequence:** PR 1 must add domain/persistence model and concurrency tests before broad upload support.

### Decision 5: Visibility/access model before generic files

- **Decision:** Add storage purpose/visibility/lifecycle metadata before enabling generic document upload.
- **Why:** Current public read/list defaults may be acceptable for legacy images but are unsafe for generic files.
- **Consequence:** API tests must prove public, authenticated, wrong-tenant, and admin/operator boundaries.

### Decision 6: Cleanup and destructive delete are later hardening

- **Decision:** First release supports quarantine/idempotent delete semantics, but automated destructive cleanup ships only after dry-run, metrics, docs, and recovery tests.
- **Why:** Local bytes are source data, not cache. Deleting too early creates irreversible operator incidents.
- **Consequence:** Orphan scan can report before it deletes.

## 6. Recommended PR Split And Implementation Phases

### PR 1 - Storage Policy, Metadata, And Reservations

- **Goal:** Add the data model and policy foundation without changing upload transport yet.
- **Relevant files:** `StorageObject.cs`, EF config/migration, `GovernanceSettingKeys`, `StorageSettingDefinitions`, new upload session/reservation entity, repository interfaces/implementations, schema docs.
- **Acceptance criteria:**
  - Provider, object key, content type, checksum, visibility/purpose, lifecycle/delete state, and safe display filename are modeled.
  - Local provider is the default setting for new installs; S3 keys remain optional.
  - Upload session/reservation rows are tenant/user scoped and have expiry/finalization states.
  - Tenant quota/default max upload/instance ceiling settings use `long`.
  - Migrations are reversible and do not delete existing metadata.
- **Validation:** Domain unit tests, Persistence integration tests for reservation concurrency, Architecture tests, build.

### PR 2 - Local Filesystem Provider And Provider Resolver

- **Goal:** Implement local provider as first-class Infrastructure while keeping S3 optional.
- **Relevant files:** new `Explore.Infrastructure/Storage/LocalFileStorageProvider.cs`, provider resolver, options/validator, path sanitizer, DI registration, `S3FileStorageProvider`.
- **Acceptance criteria:**
  - Local writes use random server-generated keys under the configured root.
  - Temp-to-final write is atomic enough for same-volume deployment.
  - Path containment blocks traversal and symlink/root escape scenarios where feasible.
  - Missing S3 config is healthy when local provider is selected.
  - S3 config failure is explicit only when S3 provider is selected.
- **Validation:** `Explore.Infrastructure.Tests` temp-directory tests, path traversal tests, provider selection tests, startup/options validation tests.

### PR 3 - Provider-Neutral Upload And Download API/BFF Contract

- **Goal:** Replace S3-first upload/read semantics with session-based, provider-neutral endpoints.
- **Relevant files:** `StorageObjectController.cs`, `RouteNames.cs`, storage CQRS commands/queries/validators, BFF storage endpoints/session store, OpenAPI/client.
- **Progress:** Phase 3.1 Application upload-session commands/validators, Phase 3.2 API upload-session/create/upload/cancel endpoints, Phase 3.3 download/public image refactor, Phase 3.4 arbitrary-key route removal, Phase 3.5 BFF upload-session/proxy refactor, and Phase 3.6 OpenAPI/client regeneration are implemented.
- **Acceptance criteria:**
  - Upload session endpoint validates auth, tenant, content type, expected size, quota, idempotency, and expiry.
  - Multipart upload path streams bytes and does not read generic files fully into memory.
  - Local download/public image endpoints resolve metadata by ID and enforce visibility/tenant/auth.
  - Existing arbitrary-key endpoints are removed, S3-only, or return `provider_not_supported` ProblemDetails for local provider.
  - OpenAPI/client are regenerated only after route names and operation IDs are stable.
- **Validation:** API integration tests for 400/401/403/404/413/422/409, BFF integration tests, OpenAPI/client naming tests, local no-S3 upload/download smoke.

### PR 4 - Admin APIs, HAL Links, Health, And Metrics

- **Goal:** Expose instance/tenant storage controls and operator signals through server-authoritative APIs.
- **Relevant files:** instance settings features/controller, tenant settings features/controller, HAL policies/assemblers, health checks, business metrics, docs.
- **Acceptance criteria:**
  - Instance admin can read/update provider policy, upload ceilings, default quota, delegation lock, and safe provider status.
  - Tenant admin can read effective policy and save only allowed overrides within instance ceilings.
  - Secret-bearing S3 fields are write-only/redacted on reads.
  - Health checks distinguish local selected, S3 selected, S3 disabled, root unwritable, quota reservation failure.
  - HAL/status links expose test/recalculate/save/delete affordances.
- **Progress:** Phase 4.1 instance storage admin CQRS/API, Phase 4.2 tenant storage admin CQRS/API, Phase 4.2 OpenAPI/generated-client refresh, Phase 4.3 storage readiness health checks, and Phase 4.4 storage metrics are implemented. HAL policies remain.
- **Validation:** API auth/authorization tests, HAL tests, health tests, metrics tags review.

### PR 5 - Blazor BFF And Client UX

- **Goal:** Make upload components and admin dashboards provider-neutral and HAL/status driven.
- **Relevant files:** `BffStorageEndpoints.cs`, `ImageUploadClient.cs`, `ImageStorageService.cs`, replacement provider-neutral services/components, `InstanceStorageSection.razor`, new tenant storage section, tests.
- **Acceptance criteria:**
  - Browser upload uses provider-neutral BFF session.
  - Component max size/content rules come from policy, not constants.
  - Admin dashboards show local default, optional S3, quotas, used/reserved/free status, and delegation state.
  - Buttons/actions are gated by HAL/status links, not roles.
  - Components meet accessibility and design-system requirements.
- **Validation:** Blazor integration tests, bUnit service/component tests, accessibility architecture tests, manual keyboard/admin smoke.

### PR 6 - Operations, Migration, Cleanup, And Release Hardening

- **Goal:** Make local-first storage operable for self-hosters.
- **Relevant files:** `docker-compose.yml`, `Explore.AppHost/AppHost.cs`, appsettings/options docs, storage docs, backup/restore/upgrade, troubleshooting, API changelog, schema docs.
- **Acceptance criteria:**
  - Compose mounts durable local storage volume by default; MinIO remains optional.
  - Aspire local dev creates/references a local data directory and does not require S3.
  - Backup/restore defaults include PostgreSQL + local data root; S3 bucket only when selected.
  - Reconciliation/orphan/quarantine jobs start in dry-run/report mode before destructive cleanup is enabled.
  - Release notes explain S3 migration and local data-root permissions.
- **Validation:** Docs/context tests, manual fresh no-S3 Compose smoke, optional S3 smoke, backup/restore dry-run checklist.

## 7. Testing Strategy

| Risk / Requirement | Required tests |
|---|---|
| Local provider path safety | Infrastructure temp-dir tests for random keys, traversal, containment, temp-to-final, delete/quarantine. |
| Quota race safety | Persistence integration tests with concurrent reservation/finalization attempts. |
| Tenant isolation | API/Persistence tests for wrong tenant, missing tenant, instance admin aggregate boundaries, single-tenant behavior. |
| Public/private access | API tests for public image, authenticated tenant file, private owner/admin file, unknown/quarantined/deleted object. |
| Upload size and content policy | Application unit tests plus API/BFF tests for 400/413/422 ProblemDetails. |
| Arbitrary-key endpoint removal/constraining | API contract tests proving local provider cannot read by raw key. |
| BFF trust boundary | BFF tests proving browser cannot provide destination, provider, tenant, local path, or upload URL authority. |
| HAL affordance gating | API HATEOAS tests plus bUnit tests proving UI checks `_links`/status only. |
| Local no-S3 self-host | Integration/manual smoke with S3 settings empty or MinIO disabled. |
| OpenAPI/client stability | API contract tests, OpenAPI parity, generated client naming tests, build. |

Minimum final command set for the complete workstream:

```bash
dotnet build --configuration Release --verbosity quiet
dotnet test --project Event.Domain.UnitTests/Event.Domain.UnitTests.csproj --configuration Release --verbosity quiet
dotnet test --project Event.Application.UnitTests/Event.Application.UnitTests.csproj --configuration Release --verbosity quiet
dotnet test --project Explore.Infrastructure.Tests/Explore.Infrastructure.Tests.csproj --configuration Release --verbosity quiet
dotnet test --project Event.Persistence.IntegrationTests/Event.Persistence.IntegrationTests.csproj --configuration Release --verbosity quiet
dotnet test --project Event.API.IntegrationTests/Event.API.IntegrationTests.csproj --configuration Release --verbosity quiet
dotnet test --project Explore.Blazor.IntegrationTests/Explore.Blazor.IntegrationTests.csproj --configuration Release --verbosity quiet
dotnet test --project Explore.Blazor.Client.Tests/Explore.Blazor.Client.Tests.csproj --configuration Release --verbosity quiet
dotnet test --project Event.Architecture.Tests/Event.Architecture.Tests.csproj --configuration Release --verbosity quiet
```

## 8. Documentation, Configuration, And Operations Impact

- Add provider-neutral storage config docs. Proposed static keys: `Storage:Provider`, `Storage:Local:RootPath`, `Storage:Local:CreateIfMissing`, `Storage:Limits:*`. Final names must match implementation.
- Keep `S3Settings:*` as optional S3 provider fallback only.
- Update secret-provider docs to distinguish local provider config from S3 credentials.
- Compose must add an API-local durable storage volume by default.
- MinIO remains optional under `storage` profile.
- Backup docs must say default backup set is application PostgreSQL + Keycloak PostgreSQL + local storage root + secrets/release manifest.
- Troubleshooting must cover root unwritable, disk full, quota exceeded, expired session, unsupported presigned endpoint, S3 selected but incomplete, orphan/quarantine states.

## 9. Security, Authorization, Privacy, And Abuse Considerations

- Uploads require authentication, antiforgery in BFF, rate limiting, tenant binding, content policy, quota reservation, and idempotent finalization.
- Direct API upload for API-key/non-browser callers must use bearer/API-key auth, tenant resolution, idempotency keys, and the same policy engine.
- Public file serving must be explicit by metadata visibility, not by raw key possession.
- Generic documents should default private/authenticated until product rules say otherwise.
- Local root must be outside app/webroot with container/filesystem permissions that prevent execution.
- Content type from the browser is advisory; store detected/validated content type and reject disallowed extensions/MIME combinations.
- Introduce scanner hook/status before broad non-image uploads; engine selection remains deferred.
- No local paths, raw object keys, presigned URLs, S3 credentials, or user-provided filenames in logs/metrics.
- Instance admins manage infrastructure and aggregate status, not tenant business-file browsing unless an explicitly audited operator/emergency flow exists.

## 10. Multi-Tenancy, Federation, Localization, Accessibility, And Product Considerations

- **Multi-tenancy:** Applicable. Storage policy resolves instance -> tenant with lock/ceiling rules; reservations, metadata, and usage are tenant-scoped.
- **Federation:** Applicable indirectly. Federated/profile/public image references should use stable application URLs, not provider URLs.
- **Localization:** Applicable. Admin labels/errors for quotas, providers, upload rejection, and provider health need localization keys where UI convention requires them.
- **Accessibility:** Applicable. Upload components and dashboards need labels, keyboard support, focus restoration around dialogs, `role="alert"` errors, and status announcements.
- **Product:** Applicable. Supports community self-host without object storage, enterprise self-host with S3, and SaaS provider delegation with tenant ceilings.

## 11. Observability And Operations

- Health checks:
  - `storage-provider`: selected provider can be resolved.
  - `storage-local`: local root exists/writable/free-space threshold when local selected.
  - `storage-s3`: only active/unhealthy when S3 selected.
  - `storage-quota-reservations`: stale reservation/reconciliation posture.
- Metrics:
  - uploads started/finalized/failed,
  - bytes written/read/quarantined/deleted,
  - quota rejections,
  - reservations expired,
  - provider tests.
- Safe storage metric tags: `provider`, `operation`, `outcome`, `failure_category`, and `visibility` where relevant; no tenant IDs, user IDs, object IDs, upload session IDs, filenames, paths, raw keys, endpoints, subjects, secrets, or exception text.
- Logs: correlation ID, tenant ID, provider, operation, bounded failure code, status. No secrets or paths.
- Admin status: used bytes, reserved bytes, quota, effective max upload, selected provider, provider health, local free bytes best effort, last reconciliation.

## 12. Migration And Compatibility Plan

1. Add nullable/new columns first; backfill deterministic defaults.
2. Existing rows become `Provider=S3Compatible` when they appear to reference S3/object keys; otherwise preserve legacy URI and mark as legacy external metadata.
3. New installs default to local filesystem provider.
4. Existing installs with complete S3 settings may stay on S3 during migration; installs with missing/unreachable S3 get a documented switch path to local.
5. Existing presigned endpoints are S3-only compatibility, not the canonical contract. For local provider, they return `provider_not_supported` or are removed with API changelog.
6. Local stored bytes are outside EF migration rollback. Rollback docs must explain file cleanup/restore separately.
7. `Down()` migrations must not silently delete retained metadata or compliance evidence.

## 13. Risk Register

| Risk | Likelihood | Impact | Mitigation | Detection Signal | Owner/Task |
|---|---:|---:|---|---|---|
| Public key endpoint exposes local files | Medium | Critical | Remove/constrain arbitrary-key endpoints; metadata ID + visibility only. | API contract/security tests. | PR 3 |
| Admin can write arbitrary filesystem path | Medium | Critical | Deployment-managed root; UI status only. | Options validation and admin API tests. | PR 2/4 |
| Quota race overuses tenant/disk capacity | Medium | High | DB reservations, transactions, finalization idempotency. | Concurrency tests, quota metrics. | PR 1/3 |
| Disk fills during upload | Medium | High | Size reservation, free-space guard, temp-to-final, health check. | `storage-local` unhealthy, failed upload metrics. | PR 2/3 |
| S3 users break unexpectedly | Medium | Medium | Backfill S3 provider, S3-only compatibility endpoints, docs. | Optional S3 smoke and API tests. | PR 3/6 |
| Large upload exhausts BFF memory | Medium | High | Stream multipart, avoid `byte[]` path for generic files, enforce request limits. | BFF tests/manual large-file smoke. | PR 3/5 |
| Secrets leak in admin reads/logs | Low | Critical | Write-only DTOs, redacted read models, safe logs. | API tests/security review. | PR 4/5 |
| Cleanup deletes real files | Low | Critical | Quarantine first, dry-run reports, explicit retention config, docs. | Cleanup tests/operator dry-run. | PR 6 |

## 14. Success Metrics And Definition Of Done

- Fresh Compose/self-host install uploads, views, and deletes/quarantines images/files with S3 settings empty and MinIO profile disabled.
- S3 provider remains optional and works when explicitly selected/configured.
- Public image URLs are stable app URLs by ID; local files are never addressed by raw public keys.
- Instance admin can manage provider policy, upload ceilings, default quotas, delegation, and provider health/status without seeing secrets or raw paths.
- Tenant admin can manage delegated settings within ceilings and see effective usage/quota.
- Upload limits are enforced consistently in UI, BFF, API, Application, Persistence, and provider.
- HAL/status responses are the source of action affordances.
- Docs, schema docs, OpenAPI/client, migrations, tests, and dev docs reflect final behavior.

## 15. Implementation Agent Contract - KEEP DEV DOCS CURRENT

Future agents implementing this plan MUST:

1. Read this plan, `local-first-file-storage-context.md`, and `local-first-file-storage-tasks.md` before edits.
2. Start with the first incomplete task in the approved PR slice unless the user overrides.
3. Re-read target source files before editing because the worktree is active and may change.
4. Update this plan if architecture, scope, PR split, risks, or migration strategy changes.
5. Update context with current state, decisions, files changed, blockers, validation, and next step after each meaningful slice.
6. Update tasks immediately when completing or discovering work.
7. Do not report complete unless all three dev docs reflect actual state.
8. Final summaries must teach the implementation: architecture, libraries/infrastructure, important files/classes, data/control flow, conventions, verification, remaining work, next step.

## 16. Progress Reporting Contract

Use this structure after each implementation slice:

- **Implemented:** Developer teaching summary naming patterns, infrastructure, important files/classes, and data/control flow.
- **Verified:** Commands/tests/manual smoke performed.
- **Remaining:** Concrete unchecked work.
- **Next:** Recommended next slice.
- **Docs updated:** yes/no with reason.

## 17. Potential Risks And Unknowns

The hardest part is not writing files to disk. It is removing S3 semantics from the product contract without weakening the BFF trust boundary, tenant isolation, or operator recovery story. The first implementation slice must prove the storage metadata, visibility model, and quota reservation are correct before browser uploads or dashboards depend on them.
