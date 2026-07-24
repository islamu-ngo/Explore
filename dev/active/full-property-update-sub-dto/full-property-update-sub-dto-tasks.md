<!-- ABOUTME: Hot execution ledger for exhaustive API update standardization and settings autosave. -->
<!-- ABOUTME: Maps every task to the complete DTO, handler, and endpoint registers. -->

# Full Property Update Sub-DTO Pattern - Task Checklist

Last Updated: 2026-07-24 Europe/Brussels

## Status Summary

- **Overall status:** Implementation in progress; Phase 1 active.
- **Completed:** 3/20 implementation tasks; phase verification is tracked separately.
- **Current priority:** Reconcile the five unrelated Blazor failures before closing Phase 1 verification.
- **Next recommended slice:** Phase 1 Blazor verification, then Task 2.1.
- **Coverage contract:** 59 DTO files, 71 update-handler files, and 104 public PUT/PATCH endpoints are individually registered.

## Implementation Maintenance Rules

- Read context/tasks first; use the plan phase and exact inventory rows for current work.
- A task is incomplete if any owned D/H/A row is skipped.
- Add every newly discovered update surface to the inventory before editing it.
- Check substantial tasks immediately; reconcile small tasks by phase end.
- Keep implementation completion separate from phase build/test completion.
- Update context for a decision, blocker, failed validation, material discovery, phase completion, or handoff.
- Update the plan only for scope, architecture, sequencing, acceptance, risk, or validation changes.
- Run the phase build and selected test once after all phase tasks.
- Never add compatibility DTOs, duplicate routes, overloads, or wildcard exception lists.

## Phase 1: All Settings Contracts And Policy Autosave - IN PROGRESS

- [x] **1.1 Standardize hierarchical and tenant-policy settings writes**
  - **Rows:** D-038/D-039; H-007/H-008; A-093-A-097.
  - **Acceptance:** Exact-key/category PUT remains and is authorization-hardened; all eight tenant policy switches autosave one registered key with lock, pending, and failure recovery; policy switches no longer use the broad onboarding write.
  - **Sequencing:** H-019/A-003 still serve non-policy announcement-bar callers and move to Task 5.3 for deletion after those callers migrate.
  - **Verification:** Focused backend suites passed 115/115; focused Blazor suites passed 41/41; Release build passed with 0 errors. User explicitly waived runtime visual QA after Chrome DevTools MCP was unavailable.
  - **Effort:** L
  - **Dependencies:** none.
- [x] **1.2 Migrate every tenant settings document/sub-resource**
  - **Rows:** H-044/H-061; A-054/A-068/A-075.
  - **Acceptance:** Tenant storage, branding document, and footer settings use grouped PATCH/autosave with existing lock, concurrency, audit, validation, and cache behavior.
  - **Verification:** Application unit tests passed 2,999/2,999; Task 1.2 API and Blazor suites passed; architecture passed 299/299 with one governed skip; canonical Release build passed with 0 errors. Five-lane goal/QA/quality/security/context review passed after fixing database-race translation, incompatible branding JSON preservation, explicit storage credential rotation, and one stale PUT fixture. Full API and Blazor sweeps retain unrelated failures recorded in context.
  - **Effort:** L
  - **Dependencies:** 1.1.
- [x] **1.3 Migrate all instance settings and onboarding duplicates**
  - **Rows:** H-006/H-016/H-017/H-045/H-055-H-057/H-064; A-005/A-006/A-026-A-042.
  - **Acceptance:** All 17 instance sub-resources plus onboarding auth/authz duplicates use dedicated partial DTOs; legacy monolith/duplicate shapes are removed; secret/coupled UI groups remain explicit.
  - **Verification:** Focused client service tests passed 38/38; focused instance UI tests passed 32/32; focused Application handler tests passed 22/22; instance OpenAPI architecture tests passed 4/4; canonical Release build passed with 0 errors. Full Blazor client suite passed 2,105/2,111 with one governed skip and the same five unrelated failures recorded in context. Post-review fixes cover redacted S3 credentials, failed-save authoritative reloads, H-006 removal, SMTP/AI read-secret redaction, and explicit grouped AI credential replacement with blank-key preservation. Final Task 1.3 review passed with no focused blockers.
  - **Effort:** XL
  - **Dependencies:** 1.1.

### Phase 1 Verification - RUN ONCE AFTER ALL PHASE TASKS

- [x] `dotnet build --configuration Release --verbosity quiet`
- [ ] `dotnet test --project tests/Explore.Blazor.Client.Tests/Explore.Blazor.Client.Tests.csproj --configuration Release --verbosity quiet`

## Phase 2: Verify Every Existing Canonical Or Focused Surface - NOT STARTED

- [ ] **2.1 Verify all public canonical grouped entity PATCH endpoints**
  - **Rows:** D-002/D-008/D-012-D-022/D-024; H-002-H-004/H-009/H-012/H-013/H-031/H-034-H-040; A-002/A-019/A-022/A-024/A-025/A-043/A-052/A-056/A-060/A-066/A-067/A-070/A-072/A-076.
  - **Acceptance:** User, Actor, Category, Location, LocationRoom, Organization, Group, Event, EventSession, EventAgendaItem, EventDay, EventSeries, EventRegistration, and EventSessionLanguage satisfy the full canonical checklist.
  - **Effort:** L
  - **Dependencies:** Phase 1.
- [ ] **2.2 Verify all Application-only canonical relationship updates**
  - **Rows:** D-010/D-011; H-032/H-033.
  - **Acceptance:** EventCategories and EventTags remain grouped, concurrent, tenant-safe, duplicate-safe, repository-mediated, and correctly cache-invalidating without invented public controllers.
  - **Effort:** M
  - **Dependencies:** 2.1.
- [ ] **2.3 Verify the local Event draft update contract**
  - **Rows:** D-030; H-042.
  - **Acceptance:** Event draft remains a local workflow and does not hide an omitted public entity migration.
  - **Effort:** S
  - **Dependencies:** 2.2.

### Phase 2 Verification - RUN ONCE AFTER ALL PHASE TASKS

- [ ] `dotnet build --configuration Release --verbosity quiet`
- [ ] `dotnet test --project tests/Event.API.IntegrationTests/Event.API.IntegrationTests.csproj --configuration Release --verbosity quiet`

## Phase 3: Migrate Every Remaining Domain Entity And Definition - NOT STARTED

- [ ] **3.1 Migrate simple tenant/catalog/control-plane entities**
  - **Rows:** D-033/D-034/D-036; H-025/H-050/H-062/H-063; A-011/A-073/A-074/A-085/A-086/A-089.
  - **Acceptance:** Tag, Tenant, TenantNavigationLink, FooterLinkGroup, FooterLink, and ControlPlaneTenantPlanVersionDraft use grouped route-ID PATCH; reorder remains separate.
  - **Effort:** L
  - **Dependencies:** Phase 2.
- [ ] **3.2 Migrate all notification preference resources**
  - **Rows:** D-028; H-022-H-024/H-046; A-020/A-050/A-058/A-079.
  - **Acceptance:** User, Organization, Group, and ActorSubscription notification preferences patch independently; required/locked cells fail atomically; mute actions remain separate.
  - **Effort:** L
  - **Dependencies:** 3.1.
- [ ] **3.3 Migrate every appearance entity/settings surface**
  - **Rows:** D-027/D-057/D-058; H-069/H-070; A-045/A-046/A-055.
  - **Acceptance:** User appearance preferences, AppearanceProfile, and UiTheme use grouped PATCH; active-profile, mode, and archive remain focused operations.
  - **Effort:** L
  - **Dependencies:** 3.2.
- [ ] **3.4 Migrate all remaining program/aspect/relationship entities**
  - **Rows:** D-009/D-035/D-051/D-053/D-056; H-001/H-010/H-011/H-028/H-051/H-068; A-001/A-010/A-013/A-014/A-061/A-078.
  - **Acceptance:** EventLocation disclosure, Islamic/Tech aspects, EventSessionAgendaItem, EventSessionGroup, EventSessionSpeaker, TagTypeTags, and CategoryTypeCategories use canonical grouped partial contracts; aspect create is separate; speaker update uses only its authoritative route ID.
  - **Effort:** XL
  - **Dependencies:** 3.3.
- [ ] **3.5 Migrate all custom-property definition entities**
  - **Rows:** D-052/D-054/D-055; H-065-H-067; A-090/A-101/A-104.
  - **Acceptance:** General/Event/EventSession definitions have grouped metadata/validation/options/relations; exact value replacements A-091/A-092/A-102/A-103 remain PUT.
  - **Effort:** XL
  - **Dependencies:** 3.4.
- [ ] **3.6 Migrate both template entities**
  - **Rows:** D-047-D-050; H-047/H-048; A-084/A-088.
  - **Acceptance:** EventTemplate and EventSessionTemplate use grouped PATCH; definition/options remain atomic; sync/apply actions remain separate.
  - **Effort:** XL
  - **Dependencies:** 3.5.

### Phase 3 Verification - RUN ONCE AFTER ALL PHASE TASKS

- [ ] `dotnet build --configuration Release --verbosity quiet`
- [ ] `dotnet test --project tests/Event.Application.UnitTests/Event.Application.UnitTests.csproj --configuration Release --verbosity quiet`

## Phase 4: Migrate Every Operational Editable Resource - NOT STARTED

- [ ] **4.1 Migrate StorageObject and both webhook resources**
  - **Rows:** D-003/D-004/D-029; H-014/H-015/H-029; A-015/A-016/A-100.
  - **Acceptance:** StorageObject metadata, WebhookEndpoint, and webhook provider mode use grouped PATCH; upload/signing/rotation/delivery actions and secrets stay separate.
  - **Effort:** L
  - **Dependencies:** Phase 3.
- [ ] **4.2 Migrate Listmonk, localization, and external API-key policy**
  - **Rows:** D-005/D-042/D-046; H-018/H-020/H-030; A-007/A-057/A-059.
  - **Acceptance:** All three use partial policy/config contracts; provider tests, credential issuance/revoke/rotate, and key material remain outside generic PATCH.
  - **Effort:** L
  - **Dependencies:** 4.1.
- [ ] **4.3 Migrate every reporting policy/consent resource**
  - **Rows:** D-001/D-006/D-007; H-005/H-026/H-027; A-008/A-009/A-017.
  - **Acceptance:** Communication consent, provider locks, and routing groups update independently; privacy/audit/concurrency remain; secrets are explicit and never exposed.
  - **Effort:** L
  - **Dependencies:** 4.2.

### Phase 4 Verification - RUN ONCE AFTER ALL PHASE TASKS

- [ ] `dotnet build --configuration Release --verbosity quiet`
- [ ] `dotnet test --project tests/Event.Application.UnitTests/Event.Application.UnitTests.csproj --configuration Release --verbosity quiet`

## Phase 5: Remove Unsafe Generic Writes And Complete UI Autosave - NOT STARTED

- [ ] **5.1 Remove generic IndexedDid, SyncState, ActorKeyStore, and UserExternalLogin writes**
  - **Rows:** D-031/D-037/D-044/D-059; H-049/H-052/H-058/H-071; A-044/A-071/A-077/A-098.
  - **Acceptance:** Generic public provider/index/cursor/key/identity updates are absent; safe sync/rotation/link/unlink workflows own mutation; bodies cannot set provider-owned IDs/timestamps/key material/cursors.
  - **Effort:** L
  - **Dependencies:** Phase 4.
- [ ] **5.2 Apply autosave to every remaining tenant and instance settings control**
  - **Rows:** All UI callers of Phase 1/3/4 settings contracts.
  - **Acceptance:** Every control is immediate, blur/debounce, atomic batch, or explicit action; HAL/locks gate writes; pending/error/saved state is accessible.
  - **Effort:** XL
  - **Dependencies:** 5.1 and all API migration tasks.
- [ ] **5.3 Remove obsolete broad Save fan-out and wire DTOs**
  - **Rows:** H-019/A-003 plus all old tenant/instance broad client calls and local wire DTOs superseded by prior tasks.
  - **Acceptance:** No generic page Save, broad onboarding settings route/handler, or old generated-client call remains; local form models are non-wire only.
  - **Effort:** M
  - **Dependencies:** 5.2.

### Phase 5 Verification - RUN ONCE AFTER ALL PHASE TASKS

- [ ] `dotnet build --configuration Release --verbosity quiet`
- [ ] `dotnet test --project tests/Explore.Blazor.Client.Tests/Explore.Blazor.Client.Tests.csproj --configuration Release --verbosity quiet`

## Phase 6: Verify Every Semantic Exception And Enforce Exhaustiveness - NOT STARTED

- [ ] **6.1 Verify all exact-replacement and action endpoints**
  - **Rows:** A-004/A-012/A-018/A-021/A-023/A-047-A-049/A-051/A-053/A-062-A-065/A-069/A-080-A-083/A-087/A-091-A-099/A-102/A-103 plus every D/H `A`, `S`, and `N` row.
  - **Acceptance:** Each exception has route-specific semantic rationale/tests; any independent property mutation is reclassified and migrated before completion; no wildcard exemption exists.
  - **Effort:** L
  - **Dependencies:** Phase 5.
- [ ] **6.2 Add exhaustive contract guards and finalize artifacts**
  - **Rows:** D-001-D-059, H-001-H-071, A-001-A-104.
  - **Acceptance:** All `M` rows implemented, `C` rows canonical, `R` routes absent, `A`/`S` exact; new unlisted surfaces fail architecture/contract tests; OpenAPI/HAL/client/docs agree.
  - **Effort:** L
  - **Dependencies:** 6.1.

### Phase 6 Verification - RUN ONCE AFTER ALL PHASE TASKS

- [ ] `dotnet build --configuration Release --verbosity quiet`
- [ ] `dotnet test --project tests/Event.Architecture.Tests/Event.Architecture.Tests.csproj --configuration Release --verbosity quiet`

## Remaining / Deferred Work

- Nothing in the current 59 DTO, 71 handler-file, or 104 endpoint scans is deferred or unclassified.
- Exact-key/category/value/content replacement and true business/operational actions remain semantic PUT/PATCH exceptions only where individually listed.
- Persistence integration replaces a phase's selected test project if that phase adds an EF concurrency migration; it does not add a third phase command.
- Any newly introduced update surface is in scope automatically and blocks completion until added to all affected registers.
