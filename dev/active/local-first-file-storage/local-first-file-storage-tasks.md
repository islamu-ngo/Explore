<!-- ABOUTME: Tactical task checklist for the local-first file storage implementation workstream. -->
<!-- ABOUTME: Tracks CTO-required PR slices, acceptance criteria, validation, and remaining deferred work. -->

# Local-First File Storage - Task Checklist

Last Updated: 2026-05-31 Europe/Brussels

## Status Summary

- **Overall status:** PR 4.5 storage/admin HAL affordance implementation is in place; full validation is blocked by unrelated API integration fixture compile drift.
- **Completed:** 30/46 planning/review/implementation tasks.
- **Current priority:** Finish PR 4.5 validation once `Event.API.IntegrationTests` compiles again.
- **Next recommended slice:** Resolve or wait for the unrelated Keycloak fixture compile drift, then run focused storage/admin HATEOAS tests and continue to Phase 5 client affordance gating.

## Implementation Maintenance Rules

- [x] Before starting implementation, read plan/context/tasks.
- [x] Re-read target source files before editing because the worktree is active.
- [x] After each completed task, update this checklist immediately.
- [x] If implementation changes scope or architecture, update the plan before continuing.
- [x] If discoveries affect future work, update the context file.
- [x] Final implementation summary must include Implemented / Verified / Remaining / Next / Docs updated.

## Phase 0: Plan Review And Baseline - AWAITING USER

- [x] Create planning directory and three dev-doc files.
  - **Files:** `dev/active/local-first-file-storage/`
  - **Acceptance:** Plan/context/tasks exist.
  - **Validation:** File presence verified.
  - **Effort:** S
  - **Dependencies:** none
- [x] Complete current-state investigation.
  - **Files:** Evidence listed in plan section 2.
  - **Acceptance:** Current-state report distinguishes verified facts from assumptions.
  - **Validation:** Source files/docs/tests read or searched.
  - **Effort:** M
  - **Dependencies:** none
- [x] Run baseline build.
  - **Files:** solution.
  - **Acceptance:** Build state known before plan edits.
  - **Validation:** `dotnet build --configuration Release --verbosity quiet` passed with existing warnings.
  - **Effort:** S
  - **Dependencies:** none
- [x] Apply Senior CTO feedback.
  - **Files:** plan/context/tasks.
  - **Acceptance:** Workstream split into PR slices; security/operator blockers named.
  - **Validation:** Docs rewritten.
  - **Effort:** M
  - **Dependencies:** current investigation
- [x] Add required ABOUTME comments to dev docs.
  - **Files:** plan/context/tasks.
  - **Acceptance:** Each file starts with two `ABOUTME:` comment lines.
  - **Validation:** Manual read.
  - **Effort:** S
  - **Dependencies:** none
- [x] Capture dirty-worktree caution.
  - **Files:** context/tasks.
  - **Acceptance:** Future agents are told to re-read source before edits and avoid unrelated dirty changes.
  - **Validation:** Context updated.
  - **Effort:** S
  - **Dependencies:** none
- [x] Run focused architecture verification after plan updates.
  - **Files:** `Event.Architecture.Tests`.
  - **Acceptance:** Context/rule test suite remains green after dev-doc updates.
  - **Validation:** `dotnet test --project Event.Architecture.Tests/Event.Architecture.Tests.csproj --configuration Release --verbosity quiet` passed: 180 passed, 1 existing skipped, 0 failed.
  - **Effort:** S
  - **Dependencies:** plan/context/tasks rewrite
- [x] User reviews the CTO split and approves or corrects scope.
  - **Acceptance:** Plan status changes to User-reviewed/Approved or is reworked again.
  - **Validation:** User requested implementation start for this workstream.
  - **Effort:** S
  - **Dependencies:** Phase 0 review docs
- [ ] Consider adding a dedicated `storage-provider-change` intent.
  - **Acceptance:** Either new intent is added with context tests or deferral is recorded.
  - **Validation:** `Event.Architecture.Tests` if contract files change.
  - **Effort:** S
  - **Dependencies:** user approval

## Phase 1: PR 1 - Storage Policy, Metadata, And Reservations

- [x] **1.1 Add provider-neutral storage setting keys and defaults**
  - **Files:** `Explore.Domain/Constants/GovernanceSettingKeys.cs`, `Explore.Domain/Settings/Definitions/StorageSettingDefinitions.cs`, related setting groups/docs.
  - **Acceptance:** Local provider is default; S3 is optional; upload ceiling and quota keys use `long` byte semantics; local root is deployment-managed, not tenant/admin DB setting.
  - **Validation:** Domain setting registry tests passed; full solution build passed.
  - **Effort:** M
  - **Dependencies:** user approval
- [x] **1.2 Design and add storage provider/visibility/lifecycle model**
  - **Files:** `Explore.Domain/StorageObject.cs`, DTOs, EF config, migration.
  - **Acceptance:** Provider, object key, content type, checksum, safe display name, visibility/purpose, lifecycle/delete/quarantine state are modeled.
  - **Validation:** Domain tests, architecture tests, full build passed. Persistence integration lane ran but failed in unrelated email dispatch transition tests.
  - **Effort:** L
  - **Dependencies:** 1.1
- [x] **1.3 Add upload session/reservation entity**
  - **Files:** new Domain entity, repository contract/implementation, EF config/migration.
  - **Acceptance:** Tenant/user scoped reservation tracks expected size, provider, content type, status, expiry, finalized object, and idempotency/failure reason.
  - **Validation:** Domain state transition tests, build, architecture tests.
  - **Effort:** L
  - **Dependencies:** 1.1
- [x] **1.4 Add usage aggregate or ledger model**
  - **Files:** new Domain/Persistence/Application files.
  - **Acceptance:** Used/reserved bytes can be read without scanning every object on every request.
  - **Validation:** Domain tests for reserve/finalize/release/quota rejection passed.
  - **Effort:** L
  - **Dependencies:** 1.3
- [x] **1.5 Add migration/backfill rules**
  - **Files:** EF migration, `schemas/islamu-event.md`, docs notes.
  - **Acceptance:** Existing rows get deterministic legacy/S3/local-safe defaults; Down does not silently delete metadata.
  - **Validation:** Migration generated and compiled; schema docs updated; full build passed. Persistence integration lane failed in unrelated email dispatch transition tests.
  - **Effort:** M
  - **Dependencies:** 1.2-1.4

## Phase 2: PR 2 - Local Provider And Resolver

- [x] **2.1 Create provider-neutral Application contracts**
  - **Files:** new `Explore.Application/Contracts/Infrastructure/*Storage*` contracts/models.
  - **Acceptance:** Application has no AWS/S3 concrete types; providers expose write/read/delete/status/test through provider-neutral models.
  - **Validation:** `Event.Application.UnitTests` and `Event.Architecture.Tests` passed after renaming provider payloads to `*Input` so they do not collide with CQRS `*Request` conventions.
  - **Effort:** M
  - **Dependencies:** Phase 1
- [x] **2.2 Implement deployment-managed local filesystem options**
  - **Files:** Infrastructure options/validator, API/appsettings docs.
  - **Acceptance:** Local root is required/validated when local selected; admin UI cannot save arbitrary root path.
  - **Validation:** `Explore.Infrastructure.Tests` includes `LocalFileStorageOptionsValidatorTests`. Startup/health endpoint coverage remains for Phase 4.3.
  - **Effort:** M
  - **Dependencies:** 2.1
- [x] **2.3 Implement local filesystem provider**
  - **Files:** `Explore.Infrastructure/Storage/LocalFileStorageProvider.cs`, sanitizer/path helper/tests.
  - **Acceptance:** Random keys under safe root; path traversal blocked; temp-to-final write; read streams support range-compatible API use; delete/quarantine idempotent.
  - **Validation:** `Explore.Infrastructure.Tests` covers temp-dir write/read/delete, SHA-256 checksum, tenant-scoped generated object keys, traversal rejection, and provider self-test. Quarantine workflow remains deferred to Phase 6.5.
  - **Effort:** L
  - **Dependencies:** 2.1, 2.2
- [x] **2.4 Adapt S3 as optional provider**
  - **Files:** `ObjectStorageService.cs`, `S3ConfigResolver.cs`, new `S3FileStorageProvider.cs`, DI registration.
  - **Acceptance:** Missing S3 does not break local mode; selected S3 reports `s3_not_configured` through provider status and throws clearly when write/read/delete are invoked without config.
  - **Validation:** S3 provider tests plus existing S3 resolver tests in `Explore.Infrastructure.Tests`.
  - **Effort:** L
  - **Dependencies:** 2.1
- [x] **2.5 Implement provider/policy resolver**
  - **Files:** Infrastructure/Application resolver services.
  - **Acceptance:** Effective tenant policy selects provider through explicit instance/tenant rules; storage delegation lock and instance max-upload ceiling are enforced server-side.
  - **Validation:** `StoragePolicyResolverTests` cover local/S3, locked/unlocked multi-tenant, single-tenant override, unsupported provider fallback, quota, upload ceiling, and provider lookup.
  - **Effort:** M
  - **Dependencies:** 2.3, 2.4

## Phase 3: PR 3 - Provider-Neutral Upload/Download API And BFF

- [x] **3.1 Add upload-session commands/validators**
  - **Files:** `Explore.Application/Features/StorageObjects/**`, DTOs/validators.
  - **Acceptance:** Expected size, content type, purpose/visibility, quota, tenant, expiry, idempotency, and resource authorization are validated. Session creation resolves the effective storage policy, enforces per-file limits and tenant quota reservations transactionally, and returns existing sessions for repeated tenant/idempotency keys without double reservation. Cancel releases reserved bytes for active/expired sessions and rejects finalized sessions.
  - **Validation:** `dotnet build Explore.Application/Explore.Application.csproj --configuration Release --verbosity quiet`; `dotnet test Event.Application.UnitTests/Event.Application.UnitTests.csproj --configuration Release --verbosity quiet`.
  - **Effort:** L
  - **Dependencies:** Phase 1, 2.5
- [x] **3.2 Add API upload-session and byte-stream endpoints**
  - **Files:** `StorageObjectController.cs`, `RouteNames.cs`, request timeout/rate limit attributes as needed.
  - **Acceptance:** Works through provider-neutral Application commands; returns RFC 7807 errors for upload failures; explicit routes/names/response types/classification; byte upload streams request body to server-selected provider; finalization is idempotent for already-finalized sessions.
  - **Validation:** `dotnet build Explore.API/Explore.API.csproj --configuration Release --verbosity quiet`; `dotnet build --configuration Release --verbosity quiet`; `dotnet test --project Event.Application.UnitTests/Event.Application.UnitTests.csproj --configuration Release --verbosity quiet`; focused API smoke `Protected_Endpoints_ReturnUnauthorized_Or_Forbidden`; focused `StorageUploadSessionControllerTests`; `dotnet test --project Event.Architecture.Tests/Event.Architecture.Tests.csproj --configuration Release --verbosity quiet`.
  - **Effort:** L
  - **Dependencies:** 3.1
- [x] **3.3 Refactor download/public image handlers**
  - **Files:** storage query handlers/controller.
  - **Acceptance:** Stable app URLs by metadata ID; no direct local path exposure; visibility and tenant checks applied; range processing still works.
  - **Validation:** `dotnet build Explore.Application/Explore.Application.csproj --configuration Release --verbosity quiet`; `dotnet build Explore.API/Explore.API.csproj --configuration Release --verbosity quiet`; `dotnet build --configuration Release --verbosity quiet`; focused `StorageObjectContentReaderTests`; focused `StorageUploadSessionControllerTests`; `dotnet test --project Event.Application.UnitTests/Event.Application.UnitTests.csproj --configuration Release --verbosity quiet`; `dotnet test --project Event.Architecture.Tests/Event.Architecture.Tests.csproj --configuration Release --verbosity quiet`.
  - **Effort:** L
  - **Dependencies:** 2.5
- [x] **3.4 Remove or constrain arbitrary-key endpoints**
  - **Files:** `StorageObjectController.cs`, handlers, API changelog/docs.
  - **Acceptance:** Local provider cannot read or presign by arbitrary caller-provided key; S3-only compatibility returns clear `provider_not_supported` if retained.
  - **Validation:** `dotnet build Explore.API/Explore.API.csproj --configuration Release --verbosity quiet`; `dotnet build Event.API.IntegrationTests/Event.API.IntegrationTests.csproj --configuration Release --verbosity quiet`; focused `StorageUploadSessionControllerTests` route-surface test passed with 8 tests; focused `ApiEndpointSmokeTests` passed with 3 tests; focused AgentContext/API-contract architecture slices passed. Full root build and full Architecture are blocked by unrelated active-worktree failures noted in context.
  - **Effort:** M
  - **Dependencies:** 3.2, 3.3
- [x] **3.5 Refactor BFF storage upload session/proxy**
  - **Files:** `Explore.Blazor/Extensions/BffStorageEndpoints.cs`, upload session store/tests.
  - **Acceptance:** Browser cannot choose provider, tenant, destination URL, object key, or local path; max size comes from policy/session.
  - **Validation:** `dotnet build Explore.Blazor/Explore.Blazor.csproj --configuration Release --verbosity quiet`; `dotnet build Explore.Blazor.Client/Explore.Blazor.Client.csproj --configuration Release --verbosity quiet`; full `Explore.Blazor.IntegrationTests` passed; focused `BffStorageUploadProxyTests` passed with 7 tests; focused `StorageUploadSessionStoreTests` passed with 4 tests.
  - **Effort:** L
  - **Dependencies:** 3.2
- [x] **3.6 Regenerate OpenAPI and NSwag client after contract stabilizes**
  - **Files:** `schemas/openapi.json`, `Explore.Blazor.Client/Clients/EventApiClient.g.cs`, `docs/API_CONTRACT_INVENTORY.md`.
  - **Acceptance:** Generated client builds; no bad operation IDs; storage methods are provider-neutral.
  - **Validation:** User reported OpenAPI schema and NSwag client were generated; build verification continues in Phase 4.1.
  - **Effort:** M
  - **Dependencies:** 3.2-3.4

## Phase 4: PR 4 - Admin APIs, HAL, Health, Metrics

- [x] **4.1 Add instance storage admin CQRS/API**
  - **Files:** instance settings features/controller/DTOs.
  - **Acceptance:** Provider policy, quotas, max upload, usage, provider health, delegation lock, test/recalculate exposed to instance admins; secrets redacted.
  - **Validation:** `dotnet build Explore.Application/Explore.Application.csproj --configuration Release --verbosity quiet`; `dotnet build Explore.Persistence/Explore.Persistence.csproj --configuration Release --verbosity quiet`; `dotnet build Explore.API/Explore.API.csproj --configuration Release --verbosity quiet`; focused TUnit controller tests for instance storage provider test/recalculate admin gating passed individually with `--treenode-filter`.
  - **Effort:** L
  - **Dependencies:** Phase 2
- [x] **4.2 Add tenant storage admin CQRS/API**
  - **Files:** `TenantStorageSettingsController`, tenant storage settings CQRS requests/handlers, `TenantStorageSettingsDto`, `TenantStorageSettingService`, DI registration, route names, docs, and focused API integration tests.
  - **Acceptance:** Locked tenants are read-only; unlocked tenants can override only `local` or `s3_compatible` within the instance max-upload ceiling; S3 secrets are redacted on reads and preserved unless explicitly replaced.
  - **Validation:** `dotnet build Event.API.IntegrationTests/Event.API.IntegrationTests.csproj --configuration Release --verbosity quiet` passed with existing suite warnings; seven focused TUnit tests for lock, ceiling, cross-tenant denial, and controller result mapping passed individually with `--treenode-filter`.
  - **Effort:** L
  - **Dependencies:** 4.1
- [x] **4.3 Add storage health checks**
  - **Files:** `Explore.API/HealthChecks/StorageReadinessHealthCheck.cs`, `Explore.API/Program.cs`, API integration health tests, storage/operations docs.
  - **Acceptance:** Local default is checked through the selected local provider without requiring S3; S3-compatible provider is only tested when instance policy selects it; local root unwritable/disk-full failures surface as bounded `local_storage_unavailable` readiness data without leaking paths or secrets.
  - **Validation:** `dotnet build Explore.API/Explore.API.csproj --configuration Release --verbosity quiet`; `dotnet build Event.API.IntegrationTests/Event.API.IntegrationTests.csproj --configuration Release --verbosity quiet`; four focused storage health TUnit tests passed individually with `--treenode-filter`.
  - **Effort:** M
  - **Dependencies:** 2.5
- [x] **4.4 Add storage metrics**
  - **Files:** `Explore.Application/Telemetry/BusinessMetrics.cs`, storage upload/cancel/finalize/delete handlers, `StorageObjectContentReader`, `InstanceStorageSettingService`, `Event.Application.UnitTests/Telemetry/BusinessMetricsStorageTests.cs`, storage command/content-reader tests, `docs/OPERATIONS.md`, `docs/STORAGE.md`.
  - **Acceptance:** Upload-session, upload-byte, read, read-byte, delete, quota reservation/byte, and provider-test metrics use bounded provider/operation/outcome/failure-category/visibility tags. Metrics intentionally exclude tenant IDs, user IDs, object keys, storage object IDs, upload session IDs, filesystem paths, filenames, endpoints, buckets, access keys, secrets, raw exception text, and provider response bodies.
  - **Validation:** `dotnet build Explore.Application/Explore.Application.csproj --configuration Release --verbosity quiet`; `dotnet build Event.Application.UnitTests/Event.Application.UnitTests.csproj --configuration Release --verbosity quiet`; focused `BusinessMetricsStorageTests`; focused upload-session/content-reader tests; `dotnet build Explore.API/Explore.API.csproj --configuration Release --verbosity quiet`.
  - **Effort:** M
  - **Dependencies:** 3.2
- [ ] **4.5 Add HAL policies for storage/admin affordances**
  - **Files:** `StorageObjectLinkPolicy`, `StorageAdminLinkPolicy`, storage admin resource assemblers, HATEOAS assembler registration, storage object/admin controllers, JSON source generation context, storage/admin HATEOAS contract tests, focused controller tests, `docs/STORAGE.md`.
  - **Acceptance:** Storage object collection/detail responses emit active-object read links and authorization-filtered create/upload-session/edit/delete links; instance storage settings emit edit/provider-test/recalculate affordances; tenant storage settings emit edit only when delegation is unlocked and effective settings are not read-only. UI action buttons can be gated by `_links`/status, not roles.
  - **Validation:** `dotnet build Explore.Application/Explore.Application.csproj --configuration Release --verbosity quiet` passed; `dotnet build Explore.API/Explore.API.csproj --configuration Release --verbosity quiet` passed with existing package warnings; touched-file diff/trailing-whitespace checks passed. `Event.API.IntegrationTests` build is blocked outside this slice by `KeycloakTokenClient` `CS1503` overload mismatches, so focused HATEOAS tests and bUnit/client affordance tests remain pending.
  - **Effort:** M
  - **Dependencies:** 4.1, 4.2

## Phase 5: PR 5 - Blazor BFF And Client UX

- [ ] **5.1 Refactor image/file storage client services**
  - **Files:** `ImageStorageService.cs`, `ImageUploadClient.cs`, new provider-neutral services/tests.
  - **Acceptance:** Service naming and flow are provider-neutral; small-image preview remains safe; generic file path streams where required.
  - **Validation:** `Explore.Blazor.Client.Tests`.
  - **Effort:** L
  - **Dependencies:** 3.6, 3.5
- [ ] **5.2 Update upload/display components**
  - **Files:** `ImageUpload.razor`, `S3Image.razor` or replacements, CSS/tests.
  - **Acceptance:** Policy-driven size/content rules; accessible errors/status; local provider works; no S3 naming in user-facing UI.
  - **Validation:** bUnit accessibility/component tests.
  - **Effort:** M
  - **Dependencies:** 5.1
- [ ] **5.3 Build instance admin storage dashboard**
  - **Files:** `InstanceStorageSection.razor`, models/services/tests.
  - **Acceptance:** Manage local default, optional S3, quotas/upload size, usage/free/reserved status, delegation, test/recalculate actions.
  - **Validation:** bUnit tests.
  - **Effort:** L
  - **Dependencies:** 4.1, 4.5
- [ ] **5.4 Build tenant admin storage dashboard**
  - **Files:** `TenantAdminSettingsLayout.razor`, new tenant storage section/tests.
  - **Acceptance:** Placeholder replaced; locked/unlocked states clear; tenant cannot exceed ceilings.
  - **Validation:** bUnit tests.
  - **Effort:** L
  - **Dependencies:** 4.2, 4.5
- [ ] **5.5 Run manual UI smoke**
  - **Files:** context/tasks updates.
  - **Acceptance:** Keyboard-only upload/admin paths pass basic smoke; errors announced; no overlapping UI.
  - **Validation:** Manual notes in context.
  - **Effort:** M
  - **Dependencies:** 5.2-5.4

## Phase 6: PR 6 - Operations, Docs, Cleanup, Release Hardening

- [ ] **6.1 Update Compose local data volume**
  - **Files:** `docker-compose.yml`, `.env.example` if present, docs.
  - **Acceptance:** API has durable local storage volume/path by default; MinIO remains optional.
  - **Validation:** Build and manual Compose smoke.
  - **Effort:** M
  - **Dependencies:** 2.2
- [ ] **6.2 Update Aspire local storage setup**
  - **Files:** `Explore.AppHost/AppHost.cs`, appsettings/options.
  - **Acceptance:** Local Aspire dev does not require S3 and has a predictable data directory/resource.
  - **Validation:** AppHost build/manual smoke when feasible.
  - **Effort:** M
  - **Dependencies:** 2.2
- [ ] **6.3 Update storage/operator docs**
  - **Files:** `docs/STORAGE.md`, `CONFIGURATION.md`, `SELF_HOSTING.md`, `BACKUP_RESTORE_UPGRADE.md`, `OPERATIONS.md`, `TROUBLESHOOTING.md`, `ADMIN_GUIDE.md`, `SECURITY-MODEL.md`, `BLAZOR.md`, `docs/index.md`.
  - **Acceptance:** Docs state local default, optional S3, data-root backup, quotas, health, recovery, and endpoint compatibility.
  - **Validation:** Architecture/docs tests.
  - **Effort:** L
  - **Dependencies:** functional implementation
- [ ] **6.4 Update schema/API docs**
  - **Files:** `schemas/islamu-event.md`, `docs/API.md`, `docs/API_CHANGELOG.md`, `docs/API_CONTRACT_INVENTORY.md`.
  - **Acceptance:** Public contract and DB schema changes documented.
  - **Validation:** Docs/API tests.
  - **Effort:** M
  - **Dependencies:** migrations/API final
- [ ] **6.5 Add reconciliation/orphan/quarantine workflow**
  - **Files:** worker/service/settings/health/tests/docs.
  - **Acceptance:** Dry-run/reporting available before destructive mode; destructive cleanup idempotent and policy-controlled.
  - **Validation:** Unit/integration tests and operations docs.
  - **Effort:** XL
  - **Dependencies:** 1.4, 2.3, 4.3
- [ ] **6.6 Full validation and manual smoke**
  - **Files:** context/tasks updates.
  - **Acceptance:** Required tests pass or failures are documented with recovery; no-S3 local upload/download/admin smoke passes; optional S3 smoke recorded.
  - **Validation:** Commands in plan section 7.
  - **Effort:** L
  - **Dependencies:** all implementation phases

## Verification Checklist

- [ ] LSP diagnostics clean for modified files where available.
- [ ] `dotnet build --configuration Release --verbosity quiet` passes.
  - Not rerun for Phase 4.5; API integration test-project build is currently blocked outside storage by unrelated Keycloak fixture compile drift documented in context.
- [x] `Event.Domain.UnitTests` passes.
- [x] `Event.Application.UnitTests` passes.
- [x] `Explore.Infrastructure.Tests` passes.
- [ ] `Event.Persistence.IntegrationTests` passes.
- [ ] `Event.API.IntegrationTests` passes.
- [ ] `Explore.Blazor.IntegrationTests` passes.
- [ ] `Explore.Blazor.Client.Tests` passes.
- [ ] `Event.Architecture.Tests` passes.
  - Latest Phase 4.4 attempt failed in unrelated active-worktree checks: Blazor client JS/runtime boundary, interface-file model declarations, raw HTTP JSON helper use, existing HATEOAS explicit-permission metadata, and the active AI CQRS namespace issue.
- [ ] OpenAPI and generated client refreshed if API contracts changed.
  - Needs regeneration after Phase 4.5 because storage object/admin GET response shapes now return HAL resources.
- [x] Docs updated where behavior/config/operations/API changed.
- [x] Dev docs refreshed with final state and remaining work.

## Remaining / Deferred Work

- Malware scanner engine/provider selection is deferred until upload hooks/status exist.
- CDN, media transformation, thumbnails, cross-region replication, and archive tiers are deferred.
- Destructive orphan cleanup is deferred until dry-run reporting, metrics, and retention docs exist.
- Broader document-management product rules are deferred until storage visibility and authorization are implemented.
