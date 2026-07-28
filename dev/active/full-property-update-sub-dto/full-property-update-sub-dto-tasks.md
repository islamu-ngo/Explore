<!-- ABOUTME: Hot execution ledger for exhaustive API update standardization and settings autosave. -->
<!-- ABOUTME: Maps every task to the complete DTO, handler, and endpoint registers. -->

# Full Property Update Sub-DTO Pattern - Task Checklist

Last Updated: 2026-07-28 Europe/Brussels

## Status Summary

- **Overall status:** Implementation in progress; Phases 1-4 and Task 5.1 are complete.
- **Completed:** 16/20 implementation tasks; Tasks 1.1-5.1 are accepted.
- **Current priority:** Begin Task 5.2 settings-control autosave while preserving recorded Docker and phase verification debt.
- **Next recommended slice:** Classify and wire every remaining tenant and instance settings control to its established save boundary.
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

## Phase 1: All Settings Contracts And Policy Autosave - IMPLEMENTATION COMPLETE; RUNTIME TESTS DEFERRED

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
  - **429/quota parity:** The exact provider GET/PATCH operations declare typed 429. Existing setup endpoints and exact provider GET/PATCH requests carrying `X-Setup-Secret` share the `setup:{ip}` 5-per-60-second window; bearer GETs remain outside that bucket and bearer PATCH retains per-user `Write`.
  - **Acceptance:** All 17 instance sub-resources plus onboarding auth/authz duplicates use dedicated partial DTOs; H-006 and duplicate shapes are removed; secret/coupled UI groups remain explicit. Only the exact canonical provider GET/PATCH routes accept active setup-secret or instance-admin authority; unrelated routes remain excluded. One global `setup:{ip}` window covers existing setup routes and canonical provider GET/PATCH attempts without duplicate limiter state, while bearer PATCH uses per-user `Write`. SingleTenant default-tenant capability sync touches only supplied leaves with cancellation. Resolver reads return copies. All value, mixed value-lock, and lock-only notifications from the six transaction-owned PATCH handlers publish only after successful commit; lock sources are `SystemLocked` or `SystemDefault`.
  - **Oracle review:** PASS after GET 429 parity. No scoped remediation remains.
  - **Verification:** Exact setup authority/limiter combined tests passed 13/13; focused metadata passed 16/16; API and generated-client builds passed; the Application source build passed; resolver tests passed 2/2; the inventory writer passed 1/1; scoped architecture passed 4/4; the Persistence integration test project builds; and the scoped diff is clean. Deferred-notification focused runs passed 33 service tests and 26 handler tests. The canonical Release build now passes with 0 errors, and the Phase 1 Blazor suite passes 2,115/2,116 with one governed skip. The full Architecture suite executes 304 tests but has two unrelated non-blocking concurrent failures in event-report consent source matching and the privacy inventory for newly added registration fields. The user explicitly deferred the unavailable Docker stress/race lanes on 2026-07-26 so Task 2.1 can start; they remain required before final workstream completion.
  - **Effort:** XL
  - **Dependencies:** 1.1.

### Phase 1 Verification - RUN ONCE AFTER ALL PHASE TASKS

- [x] `dotnet build --configuration Release --verbosity quiet` - passed with 0 errors and 82 dependency-advisory warnings.
- [x] `dotnet test --project tests/Explore.Blazor.Client.Tests/Explore.Blazor.Client.Tests.csproj --configuration Release --verbosity quiet` - passed 2,115/2,116 with one governed skip.

## Phase 2: Verify Every Existing Canonical Or Focused Surface - IMPLEMENTATION COMPLETE; VERIFICATION DEFERRED

- [x] **2.1 Verify all public canonical grouped entity PATCH endpoints**
  - **Rows:** D-002/D-008/D-012-D-022/D-024; H-002-H-004/H-009/H-012/H-013/H-031/H-034-H-040; A-002/A-019/A-022/A-024/A-025/A-043/A-052/A-056/A-060/A-066/A-067/A-070/A-072/A-076.
  - **Acceptance:** User, Actor, Category, Location, LocationRoom, Organization, Group, Event, EventSession, EventAgendaItem, EventDay, EventSeries, EventRegistration, and EventSessionLanguage satisfy the full canonical checklist.
  - **Implementation:** Actor profile-image authority and field ownership were corrected; EventSeries and EventSessionLanguage now authorize through persisted parent resources and expose authorization-filtered HAL; User now returns HAL with an edit affordance and 404 behavior; the remaining canonical controllers map not-found and permission failures to their declared HTTP contracts. OpenAPI and NSwag clients were regenerated, and Blazor adapters unwrap the HAL contracts.
  - **Oracle review:** PASS after exact Actor 404 matching, handler-level Group/Organization authorization exceptions, and removal of EventSessionLanguage controller-side parent probing.
  - **Verification:** Application, API, Blazor Client, Application unit-test, and Blazor Client test projects compile; `git diff --check` is clean. API integration-test compilation remains blocked by 29 unrelated concurrent Actor/User/Organization fixture errors, and the canonical solution build reaches 84 unrelated test-project errors. Per user direction, Task 2.1 tests were not executed and remain deferred.
  - **Effort:** L
  - **Dependencies:** Phase 1.
- [x] **2.2 Verify all Application-only canonical relationship updates**
  - **Rows:** D-010/D-011; H-032/H-033.
  - **Acceptance:** EventCategories and EventTags remain grouped, concurrent, tenant-safe, duplicate-safe, repository-mediated, and correctly cache-invalidating without invented public controllers.
  - **Implementation:** `AuthorizationBehavior` now resolves each persisted link's parent Event/Tenant before authorization and overwrites caller-provided command context. Both handlers fail closed if that persisted context changes, validate target Event/Category/Tag tenancy, retain optimistic concurrency and duplicate checks, update through repositories once, and invalidate current plus previous event caches when a link moves. Composite tenant foreign keys and unique indexes remain the database race-safety boundary; no public controller was added.
  - **Oracle review:** PASS with no scoped remediation.
  - **Verification:** `Explore.Application` and `Event.Application.UnitTests` Release builds pass with zero errors; indexed diagnostics and `git diff --check` are clean. Focused tests compile but were not executed per user direction and remain deferred.
  - **Effort:** M
  - **Dependencies:** 2.1.
- [x] **2.3 Verify the local Event draft update contract**
  - **Rows:** D-030; H-042.
  - **Acceptance:** Event draft remains a local workflow and does not hide an omitted public entity migration.
  - **Implementation:** Repository, controller, OpenAPI, and generated-client scans confirm no public draft-update operation or production command construction. The public Event entity update remains the canonical grouped `PATCH /api/event/{id}`. The local draft command retains narrow scalar-shell validation, event authorization, optimistic concurrency, schedule reprojection, one repository update, and cache invalidation; only misleading `public` wording in mandatory source headers was corrected.
  - **Oracle review:** PASS after aligning the handler header with the DTO/validator/command local-workflow classification.
  - **Verification:** `Explore.Application` and `Event.Application.UnitTests` Release builds pass with zero errors; indexed diagnostics and `git diff --check` are clean. Focused tests compile but were not executed per user direction and remain deferred.
  - **Effort:** S
  - **Dependencies:** 2.2.

### Phase 2 Verification - RUN ONCE AFTER ALL PHASE TASKS

- [ ] `dotnet build --configuration Release --verbosity quiet`
- [ ] `dotnet test --project tests/Event.API.IntegrationTests/Event.API.IntegrationTests.csproj --configuration Release --verbosity quiet`

## Phase 3: Migrate Every Remaining Domain Entity And Definition - IMPLEMENTATION COMPLETE; APPLICATION SUITE HAS UNRELATED FAILURES

- [x] **3.1 Migrate simple tenant/catalog/control-plane entities**
  - **Rows:** D-033/D-034/D-036; H-025/H-050/H-062/H-063; A-011/A-073/A-074/A-085/A-086/A-089.
  - **Acceptance:** Tag, Tenant, TenantNavigationLink, FooterLinkGroup, FooterLink, and ControlPlaneTenantPlanVersionDraft use grouped route-ID PATCH; reorder remains separate.
  - **Implementation:** All six surfaces now use nullable logical groups and route/context-owned identity. Tenant lifecycle, navigation/footer reorder, and tenant-plan publish/archive/clone remain dedicated operations. Relative external links and HTTPS are accepted by default; instance-only `security.require_https_external_urls=false` permits HTTP for an explicitly trusted private network. Oracle remediation refreshes the tenant slug cache after persisted slug changes, preserves footer-link active state through read/edit contracts, maps Tenant/navigation absence to 404, and keeps the immutable plan name read-only during version editing.
  - **Oracle review:** PASS after slug-cache convergence, footer `IsActive` read/client propagation, typed Tenant/navigation 404 responses, plan-name read-only behavior, stale PUT documentation/test cleanup, and HAL method correction.
  - **Verification:** Before concurrent ATProto drift, `Explore.Application`, `Explore.API`, `Explore.Blazor.Client`, `Event.Application.UnitTests`, `Event.API.IntegrationTests`, and `Explore.Blazor.Client.Tests` compiled with zero errors. After Oracle remediation, Application source, Blazor Client, and Blazor Client tests compile with zero errors; API and Application-test recompilation reach only unrelated concurrent ATProto constructor errors and report no Task 3.1 errors. OpenAPI and NSwag were regenerated through their standard tools, scoped diagnostics are clean, and `git diff --check` plus conflict scanning pass. Focused fixtures cover HTTPS-required rejection, explicit HTTP opt-out, omitted tenant-plan groups, and slug-cache refresh. Test execution remains deferred.
  - **Effort:** L
  - **Dependencies:** Phase 2.
- [x] **3.2 Migrate all notification preference resources**
  - **Rows:** D-028; H-022-H-024/H-046; A-020/A-050/A-058/A-079.
  - **Acceptance:** User, Organization, Group, and ActorSubscription notification preferences patch independently; required/locked cells fail atomically; mute actions remain separate.
  - **Implementation:** Current-user, Organization, and Group matrices now use presence-aware `cells` PATCH bodies; validation completes before one transaction writes supplied cells. ActorSubscription uses route-owned actor identity plus a nullable notification-level group and concurrency stamp. HAL advertises PATCH, missing actor subscriptions return 404, and all three mute routes remain PUT actions.
  - **Oracle review:** PASS after Organization/Group HAL mutation links were bound to exact update permissions and actor PATCH schema requiredness was aligned with runtime validation.
  - **Verification:** Application, API, Blazor Client, Application unit-test, API integration-test, and Blazor Client test projects compile with zero errors. OpenAPI, NSwag, and the API contract inventory were regenerated through standard commands. Focused HAL permission tests passed 2/2, actor schema coverage passed 1/1, and the inventory generator passed 1/1. Broader execution tests remain deferred.
  - **Effort:** L
  - **Dependencies:** 3.1.
- [x] **3.3 Migrate every appearance entity/settings surface**
  - **Rows:** D-027/D-057/D-058; H-069/H-070; A-045/A-046/A-055.
  - **Acceptance:** User appearance preferences, AppearanceProfile, and UiTheme use grouped PATCH; active-profile, mode, and archive remain focused operations.
  - **Implementation:** Current-user PATCH owns only a nullable localization group, while active-profile and current-mode PUT actions remain authoritative. AppearanceProfile uses nullable metadata/palette groups. UiTheme uses route-owned identity, required row version, and nullable metadata/state/palette groups with merged-state validation and one transactional update. OpenAPI/NSwag and existing Blazor/BFF callers are updated without compatibility methods.
  - **Oracle review:** PASS after changing the BFF profile route to PATCH, adding obsolete-PUT regression coverage, and documenting the profile update's typed HTTP 400 response.
  - **Verification:** Application, API, BFF, Blazor Client, and affected test projects compile. Focused current-user preference tests pass 5/5, profile service tests 17/17, UiTheme handler tests 3/3, BFF forwarding/validation tests 5/5, BFF route/antiforgery tests 8/8, appearance architecture tests 8/8, client appearance tests 15/15, and the API contract inventory generator passes 1/1. API, Blazor Client, and BFF Release builds pass with zero errors; scoped diagnostics, whitespace, and conflict checks are clean.
  - **Effort:** L
  - **Dependencies:** 3.2.
- [x] **3.4 Migrate all remaining program/aspect/relationship entities**
  - **Rows:** D-009/D-035/D-051/D-053/D-056; H-001/H-010/H-011/H-028/H-051/H-068; A-001/A-010/A-013/A-014/A-061/A-078.
  - **Acceptance:** EventLocation disclosure, Islamic/Tech aspects, EventSessionAgendaItem, EventSessionGroup, EventSessionSpeaker, TagTypeTags, and CategoryTypeCategories use canonical grouped partial contracts; aspect create is separate; speaker update uses only its authoritative route ID.
  - **Implementation:** EventLocation disclosure independently patches `Fields` and `Audience`; Islamic/Tech aspects expose separate POST create and grouped PATCH update operations; agenda-item and session-group updates are route-ID PATCH; speaker updates use `management/{id}` with strict optional `If-Match`; TagTypeTags and CategoryTypeCategories remain Application-only grouped commands. Tenant-scoped composite unique indexes protect the two lookup relationships after duplicate preflight checks.
  - **Review:** PASS. The fresh gate rejected an invalid actor-tenant finding because `Actor` is intentionally global, then corrected touched-source ABOUTME headers and typed 403 metadata for all four authorized Event aspect create/update routes.
  - **Verification:** Focused Application tests passed 33/33, API/HAL tests 30/30, migration tests 2/2, and Blazor client tests 28/28. The post-review Event aspect metadata suite passed 5/5. `dotnet build --configuration Release --verbosity quiet -maxcpucount:1` passed with 0 errors; affected API and generated-client builds also pass. Forward/reverse EF migration scripts contain only the guarded two-index transition. Canonical OpenAPI, NSwag, and inventory generation completed without artifact drift; scoped diagnostics, JSON/NUL validation, whitespace, and conflict checks pass. The full Architecture suite has 11 unrelated concurrent failures and one governed skip; Docker-backed runtime lanes remain unavailable.
  - **Effort:** XL
  - **Dependencies:** 3.3.
- [x] **3.5 Migrate all custom-property definition entities**
  - **Rows:** D-052/D-054/D-055; H-065-H-067; A-090/A-101/A-104.
  - **Acceptance:** General/Event/EventSession definitions have grouped metadata/validation/options/relations; exact value replacements A-091/A-092/A-102/A-103 remain PUT.
  - **Implementation:** Shared, Event, and EventSession definitions now use route-ID PATCH with strong quoted-GUID `If-Match`. Shared updates expose relations/metadata/validation/options; runtime definitions expose metadata/validation/options while persisted parent and provenance remain immutable. Authorization binds each persisted definition to its tenant before policy evaluation and handlers recheck that snapshot. Merged candidates receive full create validation; omitted options use ordinary entity update, while supplied options replace atomically. Event/EventSession projection refresh stays inside the transaction, cache invalidation follows commit, and shared relation moves invalidate both previous and current scope lists.
  - **Verification:** Definition handler tests pass 8/8, all AuthorizationBehavior tests pass 32/32, AutoMapper security configuration passes 1/1, API contract/HAL tests pass 10/10, and focused Blazor custom-property tests pass 2/2. The canonical Release build passes with zero errors. API/OpenAPI/NSwag regeneration, scoped diagnostics, whitespace, and conflict checks pass.
  - **Effort:** XL
  - **Dependencies:** 3.4.
- [x] **3.6 Migrate both template entities**
  - **Rows:** D-047-D-050; H-047/H-048; A-084/A-088.
  - **Acceptance:** EventTemplate and EventSessionTemplate use grouped PATCH; definition/options remain atomic; sync/apply actions remain separate.
  - **Implementation:** Both update routes use route-ID PATCH with required strong quoted-GUID `If-Match`, persisted tenant binding, nullable metadata/definitions groups, merged create-contract validation, and immutable EventSessionTemplate parent ownership. Omitted definitions use the ordinary aggregate update; supplied definitions and nested options replace through the existing `IUnitOfWork` transaction. Detail reads expose concurrency, HAL advertises PATCH, EventTemplate invalidates old/new event-type list caches, and sync diff/apply/history remain unchanged.
  - **Verification:** EventTemplate handlers pass 5/5, EventSessionTemplate handlers pass 5/5, AuthorizationBehavior passes 34/34, and template HAL passes 5/5. API, generated NSwag client, Blazor Client, Blazor Client tests, and Application tests compile. OpenAPI and API contract inventory were regenerated; the canonical Release build passes with zero errors.
  - **Effort:** XL
  - **Dependencies:** 3.5.

### Phase 3 Verification - RUN ONCE AFTER ALL PHASE TASKS

- [x] `dotnet build --configuration Release --verbosity quiet` - passed with 0 errors and existing advisory/analyzer warnings.
- [ ] `dotnet test --project tests/Event.Application.UnitTests/Event.Application.UnitTests.csproj --configuration Release --verbosity quiet` - executed 3,167 tests: 3,164 passed and 3 unrelated concurrent failures remain in organizer-claim withdrawal, Organization authorization expectations, and EventLocation disclosure contract tests.

## Phase 4: Migrate Every Operational Editable Resource - IN PROGRESS

- [x] **4.1 Migrate StorageObject and both webhook resources**
  - **Rows:** D-003/D-004/D-029; H-014/H-015/H-029; A-015/A-016/A-100.
  - **Acceptance:** StorageObject metadata, WebhookEndpoint, and webhook provider mode use grouped PATCH; upload/signing/rotation/delivery actions and secrets stay separate.
  - **Evidence:** StorageObject exposes only metadata/access/ownership groups with route-owned identity and preserves provider-owned fields; WebhookEndpoint merges omitted destination/subscription/delivery-policy groups from the locked aggregate while retaining configuration-version, pending-work, capability, audit, and transaction safeguards; provider mode remains one governed group. OpenAPI/NSwag regenerated; focused Application tests passed 5/5, webhook/storage API contracts passed 78/78, and webhook Blazor component tests passed 11/11.
  - **Effort:** L
  - **Dependencies:** Phase 3.
- [x] **4.2 Migrate Listmonk, localization, and external API-key policy**
  - **Rows:** D-005/D-042/D-046; H-018/H-020/H-030; A-007/A-057/A-059.
  - **Acceptance:** All three use partial policy/config contracts; provider tests, credential issuance/revoke/rotate, and key material remain outside generic PATCH.
  - **Evidence:** Listmonk connection/behavior groups validate against effective persisted settings and write only supplied keys; localization TMS/languages/runtime groups upsert only supplied families; external API-key metadata/access-policy groups use route-owned identity and exclude key/owner/tenant/status/usage fields. OpenAPI/NSwag and production callers regenerated. Focused Application tests passed 16/16, route metadata 3/3, external-key database integration 5/5, and generated-client forwarding 1/1.
  - **Effort:** L
  - **Dependencies:** 4.1.
- [x] **4.3 Migrate every reporting policy/consent resource**
  - **Rows:** D-001/D-006/D-007; H-005/H-026/H-027; A-008/A-009/A-017.
  - **Acceptance:** Communication consent, provider locks, and routing groups update independently; privacy/audit/concurrency remain; secrets are explicit and never exposed.
  - **Evidence:** Reporter consent requires one explicit group and retains ownership, privacy-erasure fencing, audit-neutral no-op behavior, one transaction, and cache invalidation. Instance general/Osprey/Coop locks update independently. Tenant policy/Osprey/Coop routing groups preserve omitted settings and credentials; the general lock blocks all routing writes while provider locks block only supplied matching groups. HAL/OpenAPI/NSwag advertise PATCH with no flat compatibility surface. Application handlers passed 9/9, API route/HAL tests passed 7/7, and the client service passed 13/13; all affected source and test projects compile through no-dependency builds. The canonical build remains blocked by unrelated concurrent registration-domain errors in `MinorUnitMath` and `PlatformContributionOption`.
  - **Effort:** L
  - **Dependencies:** 4.2.

### Phase 4 Verification - RUN ONCE AFTER ALL PHASE TASKS

- [ ] `dotnet build --configuration Release --verbosity quiet`
- [ ] `dotnet test --project tests/Event.Application.UnitTests/Event.Application.UnitTests.csproj --configuration Release --verbosity quiet`

## Phase 5: Remove Unsafe Generic Writes And Complete UI Autosave - IN PROGRESS

- [x] **5.1 Remove generic IndexedDid, SyncState, ActorKeyStore, and UserExternalLogin writes**
  - **Rows:** D-031/D-037/D-044/D-059; H-049/H-052/H-058/H-071; A-044/A-071/A-077/A-098.
  - **Acceptance:** Generic public provider/index/cursor/key/identity updates are absent; safe sync/rotation/link/unlink workflows own mutation; bodies cannot set provider-owned IDs/timestamps/key material/cursors.
  - **Evidence:** All four public CRUD surfaces and their API-only DTO/CQRS slices are absent. OpenAPI, NSwag, route constants, mappings, and source-generated JSON registrations no longer expose them. Runtime tests require 404 for every CRUD verb; architecture coverage guards source/OpenAPI absence while retaining verified ATProto bootstrap and private fenced Jetstream cursor authority. Internal entities, repositories, and persistence registrations remain available to dedicated workflows.
  - **Verification:** Federation API absence tests passed 26/26, the runtime OpenAPI absence invariant passed 1/1, the architecture authority-boundary guard passed 1/1, and the API contract inventory generator passed 1/1. Application, API, Blazor Client, API test, and architecture test projects compile with zero errors; the canonical Release build passes with zero errors. Existing package-advisory and analyzer warnings remain unrelated debt.
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
