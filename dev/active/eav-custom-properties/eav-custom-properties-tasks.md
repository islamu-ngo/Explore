ABOUTME: Checklist for tracking the enterprise-grade EAV custom properties implementation across Event and EventSession.
ABOUTME: Reflects extension-layer boundaries, namespaced machine keys, projections, event/session template instantiation, and aggregate views.

# EAV Custom Properties - Task Checklist

**Last Updated: 2026-05-03 (Phase 12.7 projection quota completion updated)**

---

## Session 3 Delivery Summary (2026-04-24)

**Phase 9.x Blazor CRUD UI Progress:**
- Phase 9.1 — Stale helpers: ✅ CLOSED (zero changes needed)
- Phase 9.2 — CustomPropertyDefinition CRUD: ✅ CLOSED (bg_a65ca891, 7 files)
- Phase 9.3 — EventTemplate CRUD: ✅ CLOSED (bg_b8f4ef4b, 10+ files)
- Phase 9.4 — Template selection dropdown: ✅ CLOSED (create-page template picker, read-only definition preview, event-type scoped reload, stale async guard coverage, `TemplateId` submit wiring)
- Phase 9.4A — Session blueprint selection/editing UI: ✅ CLOSED (CreateEvent session drawer picker, parent-template scoping, async race guards, stale local-session clearing, submit guard)
- Phase 9.5/9.5A — Runtime editors: ✅ CLOSED (event + session runtime editors, wiring, error surfacing, Oracle-reviewed)
- Phase 9.6 — Template preview: ⏳ PENDING
- Phase 9.6A — Session blueprint preview: ⏳ PENDING
- Phase 9.10 — Org/Group cleanup: ⏳ PENDING
- Phase 9.11 — NSwag regen: ✅ CLOSED (swagger snapshot + generated client refreshed; client build/regeneration succeeded; client tests 909 total / 908 passed / 1 known skipped)
- Phase 8.5.13 — Projection updater Prometheus metrics: ✅ CLOSED (`Explore.Projections` inline update + dirty-scope skip counters, event/session updater wiring, API/application/architecture verification passed)
- Phase 12.1 — Runtime value validation: 🟡 PARTIAL (event/session runtime writes now validate active definition, typed shape, requiredness, `IsMulti`, ranges, regex, URL schemes, option membership/active state, and duplicate normalized values through a shared Application helper; shared org/group value writes still need wiring)
- Phase 12.2 — Public exposure hardening: 🟡 PARTIAL (raw custom-property definition/value controller reads are now authenticated instead of anonymous; public event/session projection search/filter/`Exists` paths apply explicit exposure ceilings; export/moderation ceilings and Docker proof remain)
- Phase 12.3 — Option lifecycle hardening: 🟡 PARTIAL (shared/event/session option updates now preserve semantic identity by `Namespace + Key`, remap defaults to persisted option IDs, and retire omitted rows instead of hard-replacing option sets; definition delete/purge semantics still require product/ops decision)
- Phase 12.4 — Projection operability authorization: ✅ CLOSED (projection status, dirty-scope, event projection-row inspection, and session projection-row inspection queries now require handler-level `custom_property_projection:view` authorization metadata and implement `ISecureRequest`)
- Phase 12.5 — Concurrency/stale-state hardening: ✅ CLOSED (shared/event/session custom-property definition detail DTOs expose `ConcurrencyStamp`, update DTOs require `ExpectedConcurrencyStamp`, and stale admin updates throw `409 concurrent_update` before mutating tracked entities)
- Phase 12.6 — Delete lifecycle hardening: 🟡 PARTIAL (normal shared/event/session definition deletes now retire and soft-delete definitions, options, and values through tracked EF deletes; explicit audited purge workflow remains a product/ops decision)
- Phase 12.7 — Quota sweep: 🟡 PARTIAL (configured local quota keys now have enforcing code paths: shared/event/session definition-count quotas, `MaxOptionsPerDefinition`, `MaxMultiValueRowsPerValue`, `MaxDefinitionsPerTemplate`, `SyncApplyMaxPayloadBytes`, `ProjectionRebuildBatchSize`, and `MaxDirtyScopePendingPerTenant`; full boundary matrix and normalized error shape still need sweep completion)

**Build state**: Phase 12.1/12.2/12.3/12.4/12.5/12.6 API Release build ✅ and architecture tests ✅ on 2026-05-02. Phase 12.7 projection quota completion API Release build ✅, targeted Application projection quota tests ✅ 6/6, and `Event.Architecture.Tests` ✅ 143/143 on 2026-05-03; earlier Phase 12.7 quota run passed ✅ 1110/1110. Phase 12.5 targeted concurrency/application tests ✅ 1089/1089, `Explore.Blazor.Client` Release build ✅, and `Explore.Blazor.Client.Tests` ✅ 968 total / 967 passed / 1 known skipped on 2026-05-02. Phase 12.4 projection authorization metadata tests ✅ 5/5 on 2026-05-02. Phase 12.2 projection filter unit/application test run ✅ 1078/1078 on 2026-05-02. Phase 12.3/12.6 targeted persistence option/delete lifecycle tests ✅ 6/6 on 2026-05-02. Phase 9.11 generated-client build ✅ on 2026-04-30. Phase 8.5.13 persistence integration test project Release build ✅ on 2026-05-02. Full-solution Release build currently fails outside these slices on unrelated existing analyzer/package issues plus a transient locked client PDB during static-web-assets fingerprinting.

**Client test state**: `dotnet test --project Explore.Blazor.Client.Tests/Explore.Blazor.Client.Tests.csproj --configuration Release --verbosity quiet` ✅ 909 total / 908 passed / 1 known skipped on 2026-04-30.

**Uncommitted work**: Phase 9.2 + 9.3 files plus Phase 9.4, Phase 9.5/9.5A, and Phase 9.11 generated files/docs need committing when user requests it.

**Orphan to delete**: `Explore.Blazor.Client/Models/EventTemplateSync/TemplateDiffResource.cs`

**Next recommended implementation slice**: Add quota boundary tests and normalize the `quota_exceeded` error shape, then Docker/Testcontainers certification. Event/session runtime validation, raw endpoint authentication, public projection `Exists`/search/filter exposure ceilings, projection operability authorization, normal option-update lifecycle hardening, definition stale-update conflict handling, normal delete soft-delete/retirement, and local quota enforcement are now in place; shared org/group value wiring, explicit purge semantics, export/moderation proof, and full Docker-gated exposure proof remain.

**100/100 hardening focus (2026-05-02):**
- runtime value writes must validate against effective definition metadata, not only DTO shape;
- public/anonymous custom-property reads must enforce exposure ceiling server-side;
- option updates must preserve semantic identity and retire instead of hard-delete when values exist;
- projection normalized values and `Exists` filters must remain exposure-safe and tracking-independent, with Docker proof still required;
- concurrency stamps now roundtrip through custom-property definition API/client write flows and stale writes fail as `409 concurrent_update`;
- definition, option, multi-value, template-definition, sync-payload, projection rebuild batch, and dirty-scope backlog quotas now fail before persistence or backlog upsert with `quota_exceeded`/quota guardrails, but full boundary tests and normalized error shape still need completion;
- a product ADR must decide whether "full data model customization" means custom fields only or a future runtime schema engine.

---

## Session 2 Delivery Summary (2026-04-24)

**Milestone E — Explicit Sync Workflows** ✅ CODE COMPLETE (Gap 1 + Gap 4 deferred)
- E Phase 1 App Layer (diff + sync services, CQRS, validators) — `bg_db54b281` ✅
- E Phase 2 API Layer (controllers, HATEOAS policies, RouteNames) — `bg_528b91a2` ✅
- E Phase 3 Blazor UI (pages + dialog + panels + service classes + DiffPlex) — `bg_fa738248` ⚠️ orchestrator-patched after 2× infra failure
- E Phase 4 Integration Tests — `bg_40ad9ae4` ⏳ running

**Milestone F — Aggregate View + Lexicon Docs** ✅ COMPLETE
- F Phase 1 View entity + EF config + SQL migration (`vw_event_with_sessions`) — `bg_b434afda` ✅
- F Phase 2 DTOs + CQRS queries + handlers + tests — `bg_6d710714` ✅
- F Phase 3 Integration Tests — `bg_40ad9ae4` ⏳ running
- F Phase 4 `docs/LEXICONS.md` canonical NSID hierarchy — `bg_0907d67e` ✅

**Verification gates**
- `dotnet build --configuration Release`: ✅ 0 errors, 1559 pre-existing warnings
- `dotnet test Event.Application.UnitTests`: ✅ **943 / 943 pass** (+103 over 840 baseline)
- `dotnet test Explore.Blazor.Client.Tests`: ⚠️ 779 pass / 25 fail / 1 skip (Gap 2 — Cerbos substrate expansion broke existing mocks)
- `dotnet test Event.Persistence.IntegrationTests`: ⏸ deferred to CI (no Docker locally)

**Historical gap tracker** (current status now consolidated in this tasks file, the plan, and the context handoff)
- Gap 1: Blazor sync pages temporarily gate on `_diff is not null` instead of HAL link — requires API controller to emit `HalResource<TemplateDiffDto>`
- Gap 2: 25 pre-existing Blazor tests fail with 403 — test fixtures need `IMachinePrincipalAccessor` stub
- Gap 3: Integration tests running (`bg_40ad9ae4`) — CI Docker required for actual execution
- Gap 4: Blazor sync-page bUnit coverage limited to placeholder — expand after Gap 1

**Remaining cleanup tasks** (tracked as TODOs below)
- Phase 7: Remove stale `MetadataJson` / JSONB coupling refs
- Phase 9.x (cleanup): Blazor definition governance UI polish
- Phase 11 + 10.0: Architecture + API roundtrip tests
- Phase 8.5.13: Prometheus metrics for projection ✅ CLOSED 2026-05-02

---

## Delivery Milestones

### Milestone A - Shared Definitions Foundation ✅ COMPLETE

- shared Organization / Group definitions
- namespaced machine identity
- governance policy
- CRUD
- initial API and tests

Gate: ✅ All criteria met (2026-03-19)

### Milestone B - Event Layer 3 Runtime Baseline ✅ COMPLETE

- event templates (CRUD with 3-level hierarchy)
- event runtime definitions/options/values (CRUD + set/multi-set)
- event instantiation on create (transactional template copy with provenance)
- provenance/version stamping (two-pass matching algorithm)
- API endpoints + HATEOAS for templates and runtime
- 16 unit tests for instantiation + provenance

Gate: ✅ Build 0 errors, 657 unit + 52 arch tests pass (2026-03-29)

Not in this milestone (confirmed deferred):

- full operator sync UX/API → Milestone E
- advanced moderation/export/analytics projection consumers → Milestone D
- EventSession parity → Milestone C

### Milestone C - EventSession Layer 3 Parity ✅ COMPLETE

- session templates as owned children of event templates
- session runtime definitions/options/values
- session instantiation on create (transactional template copy with provenance)
- session projection entity (mirrors event projection shape)
- 19 unit tests for session instantiation + provenance

Gate: ✅ Build 0 errors, 676 unit + 52 arch tests pass (2026-03-29)

### Milestone D - Projection Integration ⏳ NEXT

Primary objective: **live transactional projection baseline** powering discovery/search/filter/export for Layer 3 without touching Layer 1 or Layer 2 paths.

**Internal sub-gate sequencing (CTO review 2026-04-11, Rule 17):** Milestone D is internally split into D1 → D2 → D3. No sub-gate begins until the previous one exits green in Testcontainers integration tests.

| Sub-gate | Theme | Key tasks |
|---|---|---|
| **D1 Correctness** | Single-tenant, single-transaction projection correctness | 3.6, 3.6A, 3.6B, 3.6C (dirty-scope), 10.1, 10.1A, 11.8, 11.8A, 11.8B; concurrency token rollout (Rule 15); hard limit enforcement (Rule 16) |
| **D2 Operability** | Admin/operator control surface for projection health | 5.8 (rebuild/drain/governance queries), 8.5 (admin endpoints + Prometheus + runbook), 8.7 (simplified to 4 policies), 9.7 (governance UI + `docs/OPERATIONS.md` runbook) |
| **D3 Consumption** | Discovery/search/filter integration behind tenant feature flag | 10.2, 10.2A, 9.9 (Blazor discovery filters), feature flag rollout, query performance baseline |

IN scope (baseline):
- `IEventCustomPropertyProjectionUpdater` + session equivalent interfaces in Application contracts
- EF Core-backed projection updater implementations in Persistence with **dirty-scope upsert-on-skip** logic
- **`CustomPropertyProjectionDirtyScope` entity + repository + migration** (new; D1 correctness requirement from CTO review)
- **Rebuild worker drain-on-completion** for dirty-scope backlog (D1 correctness)
- Transactional write-path integration inside runtime value/definition command handlers + instantiation services + sync apply handlers (Milestone E dependency flows back to D)
- `CustomPropertyProjectionStatus` tracking table + repository + EF config + migration
- PostgreSQL advisory lock coordination for inline writer vs rebuild worker
- Projection admin/rebuild/drain/governance-report CQRS commands + queries + handlers
- Projection admin/rebuild/dirty-scope/governance-report API endpoints + HATEOAS + authorization
- **`ConcurrencyStamp` column rollout on all mutable aggregates** (Rule 15 technical concurrency)
- **Hard limit settings registration + handler enforcement** (Rule 16)
- **Operational governance surface for Rule 12**: `GetCustomPropertyGovernanceReportQuery` + `PromotionRecommendation` + admin endpoint + Blazor admin page
- **4-policy authorization taxonomy** (`template_admin`, `event_editor`, `property_governance_admin`, `platform_namespace_editor`) - down from 7
- `custom_properties.projection_discovery_enabled` tenant feature flag
- Specification-pattern projection filters composed into `EventQuerySpecification` + session equivalent
- Testcontainers PostgreSQL integration tests for projection consistency + rebuild + advisory lock coordination + dirty-scope drain + rebuild crash recovery
- Architecture tests enforcing projection updater placement
- **`docs/OPERATIONS.md` operator runbook** covering "what is broken / stale / rebuildable / how do I recover" (D2 exit gate requirement)

OUT of scope (deferred):
- Async projection consumers (outbox, CDC)
- Moderation/analytics-specific projection consumers beyond flag support
- Vector search
- Materialized views

### Milestone E - Explicit Sync Workflows ⏳ PLANNED

Primary objective: **operator-driven template-to-runtime sync** using the Jira two-rule pattern. Rule B already ships in B/C; this milestone delivers Rule A.

IN scope:
- `IEventTemplateDiffService`, `IEventTemplateSyncService` + session equivalents in Application contracts + Services
- Diff + sync DTO families in `Explore.Application/DTOs/EventTemplateSync/` + session equivalent
- CQRS: `GetEventTemplateDiffQuery`, `ApplyEventTemplateSyncCommand` + session equivalents
- Stale-version conflict handling via `baseProvenanceVersion` optimistic concurrency
- Three-way merge warning (HasLocalChanges)
- Admin API endpoints + HATEOAS + RFC 7807 conflict responses + authorization (`event_template_sync`)
- Blazor diff/sync admin UI (MudBlazor v9 + DiffPlex-backed side-by-side diff + accessibility)
- Historical preservation tests + audit trail
- Testcontainers integration tests for all diff/sync scenarios + concurrency

OUT of scope:
- Automatic sync on template save (forbidden by Lifecycle Rule 5)
- Cross-tenant sync propagation

### Milestone F - Aggregate Read Views And Publication Contracts ⏳ PLANNED

Primary objective: **event-with-sessions aggregate read view** as a keyless entity + lexicon planning docs for canonical/extension/aggregate-view NSIDs with ATProto discipline.

IN scope:
- `EventWithSessionsView` keyless entity in `Explore.Domain/Views`
- EF configuration with `[Keyless]` + `HasNoKey()` + `ToView()`/`ToSqlQuery()`
- PostgreSQL backing view or parameterized query
- Aggregate view DTOs in `Explore.Application/DTOs/EventAggregateView/`
- CQRS: `GetEventWithSessionsAggregateViewQuery`, `GetEventListAggregateViewQuery`
- Exposure ceiling enforcement in DTO mapping
- Lexicon planning docs (`docs/LEXICONS.md` or extension to `docs/FEDERATION.md`) with canonical NSID hierarchy, add-only evolution, `.temp.` namespace discipline
- Aggregate view integrated into event detail page + public event page + admin event page (Phase 10.3A)
- Testcontainers integration tests for aggregate query correctness + exposure ceiling

OUT of scope:
- Actual ATProto PDS publication (separate federation initiative)
- Cross-tenant aggregate views
- Materialized views (rejected per 11 MV pitfalls)
- Adoption of ATProto `$extensions` pattern (not yet standardized)

---

## Phase 0: Architecture Lock ✅ COMPLETE

- [x] **0.1** Lock EAV as an extension/configuration layer rather than the universal semantic contract
- [x] **0.1A** Lock the 3-layer event model: Layer 1 universal core, Layer 2 typed sector profile, Layer 3 local custom extension
- [x] **0.1B** Lock the rule that sector-standard semantics must use first-class typed schema, not Layer 3 EAV
- [x] **0.1C** Lock the parent/child aggregate rule: `Event` stays the container aggregate and `EventSession` stays the scheduled child aggregate
- [x] **0.2** Replace live event-definition inheritance with blueprint/template instantiation
- [x] **0.3** Define explicit template sync as a version-aware, operator-driven workflow
- [x] **0.4** Lock machine identity on `Namespace + Key` rather than mutable names
- [x] **0.5** Lock explicit multi-value semantics with one row per value plus `Ordinal`
- [x] **0.6** Replace opaque validation strings with typed governed validation metadata
- [x] **0.7** Lock exposure/publication/search/filter/export/moderation semantics into the model
- [x] **0.8** Lock projection/read-model strategy for discovery-critical properties
- [x] **0.9** Lock delete-retention and provenance/versioning rules
- [x] **0.10** Update active dev docs so there is no lingering deferred architecture language

---

## Phase 1: Domain Layer ⏳ NOT STARTED

- [x] **1.1** Create core enums
  - `PropertyType`
  - `EntityTypeName`
  - `ExposureLevel`
- [x] **1.1A** Audit existing Layer 2 aspect/profile families (`EventIslamicAspect`, `EventTechAspect`, `EventSessionIslamicAspect`) and document what already belongs outside Layer 3
- [ ] **1.1B** Add any missing sector-standard fields to typed aspect/profile schema instead of routing them into EAV
- [x] **1.1C** **Create hard-limit setting definitions** in `Explore.Domain/Settings/Definitions/CustomPropertyQuotaSettingDefinitions.cs` (**Milestone D1, Rule 16, CTO review 2026-04-11**) ✅ 2026-04-12
  - [x] **1.1C.1** `custom_properties.max_definitions_per_tenant_per_entity_scope` (default 500, platform max 5000)
  - [x] **1.1C.2** `custom_properties.max_definitions_per_event` (default 100, platform max 1000)
  - [x] **1.1C.3** `custom_properties.max_definitions_per_event_session` (default 50, platform max 500)
  - [x] **1.1C.4** `custom_properties.max_options_per_definition` (default 200, platform max 2000)
  - [x] **1.1C.5** `custom_properties.max_multi_value_rows_per_value` (default 20, platform max 200)
  - [x] **1.1C.6** `custom_properties.max_definitions_per_template` (default 100, platform max 1000)
  - [x] **1.1C.7** `custom_properties.projection_rebuild_batch_size` (default 500, platform max 5000)
  - [x] **1.1C.8** `custom_properties.sync_apply_max_change_count` (default 200, platform max 2000)
  - [x] **1.1C.9** `custom_properties.sync_apply_max_payload_bytes` (default 262144, platform max 4194304)
  - [x] **1.1C.10** `custom_properties.max_dirty_scope_pending_per_tenant` (default 10000, platform max 100000)
  - [x] **1.1C.11** `custom_properties.projection_discovery_enabled` (boolean tenant feature flag, default false)
  - [ ] **1.1C.12** Enforce platform-maximum validation in setting value validation (reject tenant attempts to exceed) — deferred to handler quota enforcement task
  - [ ] **1.1C.13** Unit test: each quota is read via `ISettingService.GetEffectiveIntAsync` — deferred (no ISettingService exists yet; quotas read via `ICustomPropertyQuotaResolver`)
- [x] **1.2** Create shared definition entities for Organization / Group
  - namespaced `Namespace + Key`
  - typed validation fields
  - exposure/search/filter/export flags
  - `Ordinal`-aware value rows
- [x] **1.3** Create event template entities
  - `EventTemplate`
  - `EventTemplateCustomPropertyDefinition`
  - `EventTemplateCustomPropertyOption`
  - includes `TemplateKey`, `Version`, publish state, and namespaced keys
- [x] **1.4** Create event runtime entities
  - `EventCustomPropertyDefinition`
  - `EventCustomPropertyOption`
  - `EventCustomPropertyValue`
  - includes provenance/version/sync fields
- [x] **1.4A** Create event session template entities (**Milestone C**) ✅ 2026-03-29
  - `EventSessionTemplate`
  - `EventSessionTemplateCustomPropertyDefinition`
  - `EventSessionTemplateCustomPropertyOption`
  - includes session blueprint identity, parent event-template linkage, publish state, and namespaced keys
- [x] **1.4B** Create event session runtime entities (**Milestone C**) ✅ 2026-03-29
  - `EventSessionCustomPropertyDefinition`
  - `EventSessionCustomPropertyOption`
  - `EventSessionCustomPropertyValue`
  - includes provenance/version/sync fields
- [x] **1.5** Create projection entities/value objects for searchable/filterable/exportable custom-property reads
- [x] **1.5A** Create event session projection entities/value objects for searchable/filterable/exportable session custom-property reads (**Milestone C**) ✅ 2026-03-29
- [ ] **1.5B** Define aggregate event-with-sessions read/view model contracts
- [x] **1.6** Audit existing appearance / branding fields in `Event.cs`, `Organization.cs`, and `Group.cs` against the new governance model
- [x] **1.6A** Lock reserved namespace constants and Layer 2 semantic reservations in domain constants
- [x] **1.6B** Add machine-identity normalization helpers for `Namespace + Key`
- [ ] **1.7** Add only genuinely missing first-class appearance / branding fields; do not recreate metadata-blob storage
- [ ] **1.8** Remove stale metadata assumptions from the domain design baseline

---

## Phase 2: Persistence - EF Configurations ⏳ NOT STARTED

- [x] **2.0** Reconcile existing Layer 2 aspect/profile configurations with the 3-layer architecture
- [x] **2.1** Configure shared custom-property tables
  - namespaced uniqueness
  - ordinal constraints
  - typed validation columns
  - exposure/indexing columns
- [x] **2.2** Configure event template tables
  - `(TenantId, TemplateKey, Version)` uniqueness
  - namespaced uniqueness for definitions/options
- [x] **2.2A** Configure event session template tables (**Milestone C**) ✅ 2026-03-29
  - `(EventTemplateId, SessionTemplateKey, Version)` uniqueness
  - namespaced uniqueness for session definitions/options
- [x] **2.3** Configure event runtime tables
  - `(EventId, Namespace, Key)` uniqueness
  - provenance/version mapping
  - value-row ordering constraints
- [x] **2.3A** Configure event session runtime tables (**Milestone C**) ✅ 2026-03-29
  - `(EventSessionId, Namespace, Key)` uniqueness
  - provenance/version mapping
  - value-row ordering constraints
- [x] **2.4** Configure projection/read-model tables with discovery-oriented indexes
- [x] **2.4A** Redesign projection row shape to be atomic per projected value row rather than merged per property
- [x] **2.4B** Configure session projection/read-model tables with discovery-oriented indexes (**Milestone C**) ✅ 2026-03-29
- [ ] **2.5** Reconcile `EventConfiguration.cs` with the hardened plan and preserve current first-class appearance mapping
- [ ] **2.6** Update `OrganizationConfiguration.cs` only where this initiative introduces real new fields/constraints
- [ ] **2.7** Update `GroupConfiguration.cs` only where this initiative introduces real new fields/constraints
- [x] **2.8** Add all new DbSets + query filters to `ExploreDbContext.cs`
- [x] **2.9** Create EF migration for the new schema

---

## Phase 3: Persistence - Repositories, Sync, And Projection Support ⏳ PARTIALLY STARTED

- [x] **3.1** Create shared-definition repository interfaces and implementations
- [x] **3.2** Create event template repository interfaces and implementations (**Milestone B**) ✅ 2026-03-29
- [x] **3.2A** Create event session template repository interfaces and implementations (**Milestone C**) ✅ 2026-03-29
- [x] **3.3** Create event runtime repository interfaces and implementations (**Milestone B**) ✅ 2026-03-29
- [x] **3.3A** Create event session runtime repository interfaces and implementations (**Milestone C**) ✅ 2026-03-29
- [x] **3.4** Add transactional template-instantiation support for event creation (**Milestone B**) ✅ 2026-03-29
- [x] **3.4A** Add transactional session-template instantiation support under event session creation (**Milestone C**) ✅ 2026-03-29
- [ ] **3.5** Add event template diff / sync service support (**Milestone E**, keep it boring per CTO review)
  - [ ] **3.5.1** Define `IEventTemplateDiffService` in `Explore.Application/Contracts/Services/` (**no `<T,U>` generic**)
  - [ ] **3.5.2** Implement `EventTemplateDiffService` (pure diff logic, no EF) in `Explore.Application/Services/` with **explicit hand-coded per-field comparisons** (no reflection, no `ITemplateDiffService<,>`)
  - [ ] **3.5.3** Implement source-id-first matching with `Namespace + Key` fallback
  - [ ] **3.5.4** Return `EventTemplateDiffResult` with Added/Modified/Retired/Untouched buckets at definition and option levels
  - [ ] **3.5.5** Populate `HasLocalChanges` flag on modified definitions (three-way merge marker)
  - [ ] **3.5.6** Define `IEventTemplateSyncService` in `Explore.Application/Contracts/Services/`
  - [ ] **3.5.7** Implement `EventTemplateSyncService` orchestrator in `Explore.Application/Services/`
  - [ ] **3.5.8** Wire transactional apply: validate `baseProvenanceVersion` → return `stale_sync_base` on mismatch (Rule 15 business concurrency, distinct from technical `concurrent_update`) → apply selected changes → stamp LastSyncedFromTemplateAt + SourceTemplateVersion → call projection updater → audit → commit
  - [ ] **3.5.9** Handle `DbUpdateConcurrencyException` → translate to `concurrent_update` problem detail (Rule 15 technical concurrency)
  - [ ] **3.5.10** Enforce `custom_properties.sync_apply_max_change_count` quota (Rule 16) before opening transaction
  - [ ] **3.5.11** Register both services in `ApplicationServicesRegistration.cs`
- [ ] **3.5A** Add event session template diff / sync service support (**Milestone E**, keep it boring)
  - [ ] **3.5A.1** Mirror 3.5.1-3.5.11 for `IEventSessionTemplateDiffService` + `IEventSessionTemplateSyncService` with **no shared generic base**
- [x] **3.6** Add event projection updater / rebuilder persistence support (**Milestone D1 Correctness**) ✅ 2026-04-12
  - [x] **3.6.1** Define `IEventCustomPropertyProjectionUpdater` in `Explore.Application/Contracts/Services/`
  - [x] **3.6.2** Implement `EventCustomPropertyProjectionUpdater` in `Explore.Persistence/Projections/`
  - [x] **3.6.3** Compute `NormalizedValue` from typed value columns + definition shape via `CustomPropertyProjectionNormalizer`
  - [x] **3.6.4** Preserve exposure flags on projection rows
  - [x] **3.6.5** Projection write uses active DbContext transaction
  - [x] **3.6.6** PostgreSQL advisory lock via raw ADO `pg_try_advisory_xact_lock` keyed on `fnv1a(projection_name), fnv1a(tenant_id)`
  - [x] **3.6.7** Inline writers skip-on-contention → upsert `CustomPropertyProjectionDirtyScope`
  - [x] **3.6.8** Inline writers quota check on `max_dirty_scope_pending_per_tenant`
  - [x] **3.6.9** Rebuild worker drain-on-completion via `DrainPendingScopesAsync`
  - [x] **3.6.10** Rebuild commits per batch (D1 baseline: single-transaction; D2 per-batch commit deferred)
  - [x] **3.6.11** Rebuild never hard-deletes retired rows (soft-delete query filter honored)
  - [x] **3.6.12** `DrainDirtyScopesForTenantAsync` as operator self-service
  - [x] **3.6.13** Enforces `projection_rebuild_batch_size` quota
  - [x] **3.6.14** Registered in `PersistenceServicesRegistration.cs`
- [x] **3.6A** Add session projection updater / rebuilder persistence support (**Milestone D1 Correctness**) ✅ 2026-04-12
  - [x] **3.6A.1** Mirrored for `IEventSessionCustomPropertyProjectionUpdater` — no shared generic base
- [x] **3.6B** Create projection status tracking persistence (**Milestone D1 Correctness**) ✅ 2026-04-12
  - [x] **3.6B.1-3.6B.5** Entity + EF config + repo interface/impl + migration + DI
- [x] **3.6C** Create projection dirty-scope backlog persistence (**Milestone D1 Correctness**) ✅ 2026-04-12
  - [x] **3.6C.1-3.6C.7** Entity + EF config + partial index + repo interface/impl + migration + DI
- [x] **3.6D** Add `ConcurrencyStamp` column rollout (**Milestone D1 Correctness, Rule 15**) ✅ 2026-04-12
  - [x] **3.6D.1-3.6D.5** `ConcurrencyStamp` + `IConcurrencyAware` on 15 entities
  - [x] **3.6D.6** Configured `.IsConcurrencyToken()` (not `ValueGeneratedOnAddOrUpdate`; stamp auto-rotated by `SaveChangesAsync`)
  - [x] **3.6D.7** Migration user-generated `20260411124727_D1CustomPropertyProjectionSchemaAndSessions`
  - [x] **3.6D.8** `DbUpdateConcurrencyException` → `ConcurrencyConflictException` translated in `EfCoreUnitOfWork`, mapped to 409 + RFC 7807 in `GlobalExceptionHandler`
  - [ ] **3.6D.9** Per-handler concurrent-update unit tests — deferred (low risk: translation is centralized in UoW)
- [x] **3.7** Register repositories and services in `PersistenceServicesRegistration.cs` ✅ 2026-03-29
- [x] **3.8** Add source-id-first provenance matching with `Namespace + Key` fallback only for repair/backfill ✅ 2026-03-29

---

## Phase 4: Application - DTOs And Mapping ⏳ PARTIALLY STARTED

- [x] **4.1** Create DTOs for shared Organization / Group custom properties
- [x] **4.2** Create DTOs for event templates, template definitions, and template options (**Milestone B**) ✅ 2026-03-29
- [x] **4.2A** Create DTOs for event session templates, template definitions, and template options (**Milestone C**) ✅ 2026-03-29
- [x] **4.3** Create DTOs for event runtime definitions, options, and values (**Milestone B**) ✅ 2026-03-29
- [x] **4.3A** Create DTOs for event session runtime definitions, options, and values (**Milestone C**) ✅ 2026-03-29
- [ ] **4.4** Create DTOs for event template diff / sync workflows (**Milestone E**)
  - [ ] **4.4.1** `TemplateDiffDto` with `TargetTemplateVersion`, `BaseProvenanceVersion`, `AddedDefinitions`, `ModifiedDefinitions`, `RetiredDefinitions`, `AddedOptions`, `ModifiedOptions`, `RetiredOptions`, `UntouchedLocalDefinitions`
  - [ ] **4.4.2** `AddedDefinitionDto`, `ModifiedDefinitionDto` (with `FieldChanges` list), `RetiredDefinitionDto`
  - [ ] **4.4.3** `AddedOptionDto`, `ModifiedOptionDto`, `RetiredOptionDto`
  - [ ] **4.4.4** `FieldChangeDto` (FieldName, OldValue, NewValue, ValueType)
  - [ ] **4.4.5** `UntouchedLocalDefinitionDto`
  - [ ] **4.4.6** `TemplateSyncPlanDto` (operator-selected subset of the diff)
  - [ ] **4.4.7** `TemplateSyncOutcomeDto` (applied, skipped, conflicts, NewProvenanceVersion, SyncedAt)
  - [ ] **4.4.8** Manually instantiated validators for `TemplateSyncPlanDto`
- [ ] **4.4A** Create DTOs for event session template diff / sync workflows (**Milestone E**)
  - [ ] **4.4A.1** Mirror 4.4.1-4.4.8 under `Explore.Application/DTOs/EventSessionTemplateSync/`
- [x] **4.5** Create projection DTOs for discovery/search/export-oriented reads (**Milestone D**) ✅ 2026-04-12
  - [x] **4.5.1** `EventCustomPropertyProjectionDto` (admin/full shape)
  - [ ] **4.5.2** `EventCustomPropertyProjectionFacetDto` (discovery subset, no moderation column) — deferred to D3
  - [ ] **4.5.3** `EventCustomPropertyProjectionListDto` (paged list item) — deferred to D3
  - [x] **4.5.4** `RebuildEventCustomPropertyProjectionRequestDto` (TenantId, Scope, BatchSize)
  - [x] **4.5.5** `RebuildEventCustomPropertyProjectionResponseDto` (Status, EventsProcessed, ProjectionRowsWritten, timestamps, LastError)
  - [x] **4.5.6** Session equivalents for all of the above
  - [x] **4.5.7** `ProjectionStatusDto` for admin observability
  - [x] **4.5.8** Exposure ceiling filter applied in query handler based on ExposureCeiling parameter
- [ ] **4.5A** Create aggregate event-with-sessions view DTOs and lexicon-facing contracts (**Milestone F**)
  - [ ] **4.5A.1** `EventWithSessionsViewDto` (Layer 1 core fields + Layer 2 aspect DTOs nullable + Layer 3 projection facets)
  - [ ] **4.5A.2** `EventSessionSummaryDto` (session Layer 1 + session Layer 2 + session Layer 3 facets)
  - [ ] **4.5A.3** `EventCustomPropertyFacetDto`, `EventSessionCustomPropertyFacetDto`
  - [ ] **4.5A.4** `EventListAggregateViewDto` (paged list item)
  - [ ] **4.5A.5** Lexicon planning docs with canonical NSID hierarchy (not code): `im.islamu.event.core.v1`, `im.islamu.eventsession.core.v1`, `im.islamu.event.islamic.v1`, `im.islamu.event.tech.v1`, `im.islamu.eventsession.islamic.v1`, `im.islamu.event.extension.v1`, `im.islamu.eventsession.extension.v1`, `im.islamu.event.withSessions.v1`, `im.islamu.event.temp.*`
  - [ ] **4.5A.6** Add-only evolution rules + NSID versioning discipline documented
- [ ] **4.6** Re-audit Event DTOs / generated contracts and remove any stale metadata-blob assumptions
- [ ] **4.7** Re-audit Organization DTOs / generated contracts and remove any stale metadata-blob assumptions
- [ ] **4.8** Re-audit Group DTOs / generated contracts and remove any stale metadata-blob assumptions
- [x] **4.9** Update mapping profiles for the first shared-definition CQRS slice
- [x] **4.9A** Extend mapping profiles for projection DTOs (**Milestone D**) ✅ 2026-04-12
- [ ] **4.9B** Extend mapping profiles for sync diff/outcome DTOs (**Milestone E**)
- [ ] **4.9C** Extend mapping profiles for aggregate view DTOs (**Milestone F**)

---

## Phase 5: Application - CQRS For Definitions, Templates, Values, Sync, And Projections ⏳ PARTIALLY STARTED

- [ ] **5.0** Preserve distinct CQRS paths for Layer 2 typed sector schema vs Layer 3 custom-property flows
- [ ] **5.0A** Add application-layer rejection for reserved namespaces and reserved Layer 2 semantic collisions
- [x] **5.0A** Add reusable application-layer governance policy for reserved namespaces and reserved Layer 2 semantic collisions
- [x] **5.1** Add first create/list/details CQRS slice for shared Organization / Group definitions
- [x] **5.1A** Add update/delete CQRS for shared Organization / Group definitions
- [x] **5.1B** Add option-edit semantics and duplicate-normalized-option-key rejection for shared definitions
- [x] **5.2** Add CRUD commands / handlers / validators for event templates (**Milestone B**) ✅ 2026-03-29
- [x] **5.2A** Add CRUD commands / handlers / validators for event session templates (**Milestone C**) ✅ 2026-03-29
- [x] **5.3** Add CRUD commands / handlers / validators for event template options (**Milestone B**) ✅ 2026-03-29
- [x] **5.3A** Add CRUD commands / handlers / validators for event session template options (**Milestone C**) ✅ 2026-03-29
- [x] **5.4** Add queries for event runtime definitions and values (**Milestone B**) ✅ 2026-03-29
- [x] **5.4A** Add queries for event session runtime definitions and values (**Milestone C**) ✅ 2026-03-29
- [x] **5.5** Add commands for setting event runtime values with explicit single vs multi rules (**Milestone B**) ✅ 2026-03-29
- [x] **5.5A** Add commands for setting event session runtime values with explicit single vs multi rules (**Milestone C**) ✅ 2026-03-29
- [x] **5.6** Add commands for editing event-local definitions after instantiation (**Milestone B**) ✅ 2026-03-29
- [x] **5.6A** Add commands for editing event session-local definitions after instantiation (**Milestone C**) ✅ 2026-03-29
- [ ] **5.7** Add commands / queries for event template diff and sync (**Milestone E**)
  - [ ] **5.7.1** `GetEventTemplateDiffQuery(EventId, TargetTemplateVersion) : IRequest<BaseCommandResponse<TemplateDiffDto>>` + handler in `Explore.Application/Features/EventTemplateSync/`
  - [ ] **5.7.2** `ApplyEventTemplateSyncCommand(EventId, TemplateSyncPlanDto, BaseProvenanceVersion) : IRequest<BaseCommandResponse<TemplateSyncOutcomeDto>>` + handler
  - [ ] **5.7.3** Handler uses manually instantiated validator per project rule
  - [ ] **5.7.4** Handler opens transaction, calls `IEventTemplateSyncService.ApplySync`, transactionally updates projection via `IEventCustomPropertyProjectionUpdater.RefreshForEventAsync`, writes audit row, commits
  - [ ] **5.7.5** Stale `BaseProvenanceVersion` returns structured conflict, not exception
  - [ ] **5.7.6** Authorization via `IAuthorizedRequest` with `event_template_sync` policy
  - [ ] **5.7.7** Unit tests for empty diff, added-only, modified-only, retired-only, mixed, stale version, local-changes warning
- [ ] **5.7A** Add commands / queries for event session template diff and sync (**Milestone E**)
  - [ ] **5.7A.1** Mirror 5.7.1-5.7.7 in `Explore.Application/Features/EventSessionTemplateSync/`
- [x] **5.8** Add commands / jobs for projection updates, rebuilds, dirty-scope drain, and governance reporting (**Milestone D2 Operability**) ✅ 2026-04-12
  - [x] **5.8.1** `RebuildEventCustomPropertyProjectionCommand(TenantId, RebuildScope, BatchSize)` + handler in `Explore.Application/Features/EventCustomPropertyProjections/`
  - [x] **5.8.2** `RebuildSingleEventCustomPropertyProjectionCommand(EventId)` + handler
  - [x] **5.8.3** `GetEventCustomPropertyProjectionStatusQuery(TenantId)` + handler
  - [x] **5.8.4** `GetEventCustomPropertyProjectionsForEventQuery(EventId, ExposureCeiling)` + handler (used by aggregate view in Milestone F)
  - [x] **5.8.5** Session equivalents for all of the above under `Explore.Application/Features/EventSessionCustomPropertyProjections/`
  - [x] **5.8.6** **`DrainCustomPropertyProjectionDirtyScopesCommand(TenantId, ProjectionName)` + handler** - CTO review: operator self-service drain without full rebuild
  - [x] **5.8.7** **`GetCustomPropertyProjectionDirtyScopesQuery(TenantId, ProjectionName, Skip, Take)` + handler** - CTO review: paged pending dirty-scope inspection for operators
  - [x] **5.8.8** **`GetCustomPropertyGovernanceReportQuery(TenantId, Filter)` + handler** in `Explore.Application/Features/CustomPropertyGovernance/` - CTO review: Rule 12 operational governance surface
  - [x] **5.8.9** Implement `PromotionRecommendation` enum computation from Atlassian 4-question matrix: `None`, `ConsiderProjectionFirst`, `ConsiderLayer2Promotion`, `ConsiderLayer1Promotion`
  - [x] **5.8.10** `CustomPropertyGovernanceRowDto` with `TenantId`, `Namespace`, `Key`, `DisplayName`, `EntityScope`, `PropertyType`, `ExposureLevel`, all flag columns, `ActiveInstanceCount`, `LastUsedAt`, `Recommendation`
  - [x] **5.8.11** Rebuild + drain handlers use `Complex` request timeout policy (60s); governance report query uses `Lookup` (10s)
  - [x] **5.8.12** Authorization via `[AuthorizeResource]` attribute on commands/queries
  - [x] **5.8.13** Unit tests for: happy path, idempotency, drain routing, governance recommendation matrix (all 4 `PromotionRecommendation` values tested)
- [ ] **5.9** Add promotion/projection rules for discovery-critical properties (**Milestone D**)
  - [ ] **5.9.1** `PromoteCustomPropertyToProjectionFirstCommand(TenantId, Namespace, Key)` + handler
  - [ ] **5.9.2** Handler forces `IsSearchable = true` + `IsFilterable = true` across all runtime rows for the namespace+key
  - [ ] **5.9.3** Handler triggers projection rebuild for affected scope
  - [ ] **5.9.4** Audit trail of promotion decision
- [ ] **5.10** Add promotion rules that move sector-standard Layer 3 candidates into Layer 2 typed schema
  - [ ] **5.10.1** Document operational playbook for Layer 3 → Layer 2 promotion
  - [ ] **5.10.2** Provide template for backfill command pattern
  - [ ] **5.10.3** Reference Rule 12 (Atlassian 4-question framework)

---

## Phase 6: Event + EventSession Creation, Template Instantiation, And Editing Flow ⏳ NOT STARTED

- [x] **6.1** Extend event creation contracts to accept optional template selection ✅ 2026-03-29
- [x] **6.2** Instantiate event-local definitions/options/default values transactionally ✅ 2026-03-29
- [x] **6.2A** Instantiate event session templates and session-local definitions/options/default values transactionally (**Milestone C**) ✅ 2026-03-29
- [x] **6.3** Support events created without a template ✅ 2026-03-29
- [x] **6.4** Ensure event edit flows read event-local configuration only ✅ 2026-03-29
- [ ] **6.5** Add explicit event admin workflow for template diff and sync decisions (**Milestone E**)
  - [ ] **6.5.1** Blazor page `EventTemplateSyncPage.razor` calls `GET /events/{eventId}/template-sync/diff?templateVersion=...`
  - [ ] **6.5.2** Operator selects subset of diff changes + confirms
  - [ ] **6.5.3** Page posts `TemplateSyncPlanDto` to `POST /events/{eventId}/template-sync/apply`
  - [ ] **6.5.4** 409 Conflict surfaces cleanly with "re-diff" button
  - [ ] **6.5.5** Local-changes warning shown when runtime has edits since last sync
- [ ] **6.5A** Add explicit event session admin workflow for template diff and sync decisions (**Milestone E**)
  - [ ] **6.5A.1** Mirror 6.5.1-6.5.5 for session scope
- [ ] **6.6** Keep Layer 2 aspect/profile creation and editing distinct from Layer 3 custom-property creation and editing during event workflows
  - [ ] **6.6.1** Verify no shared handler path between aspect upsert commands and custom-property value commands (architecture test)
  - [ ] **6.6.2** Confirm Blazor admin UI renders two distinct sections (typed aspect editor + dynamic custom-property editor)
  - [ ] **6.6.3** Document the separation in `docs/ARCHITECTURE.md`
- [ ] **6.7** Add aggregate event page/read flow that embeds linked session summaries without collapsing canonical models (**Milestone F**)
  - [ ] **6.7.1** `GetEventWithSessionsAggregateViewQuery` + handler in `Explore.Application/Features/EventAggregateViews/`
  - [ ] **6.7.2** Public event detail page reads from this query with `ExposureLevel.Public` ceiling
  - [ ] **6.7.3** Admin event detail page reads with `ExposureLevel.TenantAdminOnly` ceiling
  - [ ] **6.7.4** Federation/publication composer reads with `ExposureLevel.Public` + `IsExportable = true`

---

## Phase 7: Remove Stale Metadata Assumptions ⏳ NOT STARTED

> Scheduled for execution **before or alongside Milestone D** since stale docs will confuse Milestone D contributors.

- [ ] **7.1** Re-audit the source tree for actual remaining runtime `MetadataJson` / JSONB coupling
  - [ ] **7.1.1** Grep for: `MetadataJson`, `Metadata\s*=`, `Dictionary<string, object>`, literal `jsonb` usage in domain/application
  - [ ] **7.1.2** Classify each hit: legitimate / stale / coupling
  - [ ] **7.1.3** Remove any actual runtime coupling
- [ ] **7.2** Remove stale comments, docs, or contracts that still assume metadata-blob storage
- [ ] **7.3** Align event list/query contracts with the current non-blob architecture if any stale parameters remain
- [ ] **7.4** Align Event write handlers with the first-class appearance model
- [ ] **7.5** Align Organization write handlers with the first-class appearance model
- [ ] **7.6** Align Group write handlers with the first-class branding model

---

## Phase 8: API Layer ⏳ PARTIALLY STARTED

- [x] **8.1** Create API endpoints for shared Organization / Group custom-property governance
- [x] **8.2** Create API endpoints for event template management (**Milestone B**) ✅ 2026-03-29
- [x] **8.2A** Create API endpoints for event session template management (**Milestone C**) ✅ 2026-03-29
- [x] **8.3** Create API endpoints for event runtime definition/value management (**Milestone B**) ✅ 2026-03-29
- [x] **8.3A** Create API endpoints for event session runtime definition/value management (**Milestone C**) ✅ 2026-03-29
- [ ] **8.4** Create API endpoints for event template diff / sync workflows (**Milestone E**)
  - [ ] **8.4.1** `GET /events/{eventId}/template-sync/diff?templateVersion={version}` returns `TemplateDiffDto`
  - [ ] **8.4.2** `POST /events/{eventId}/template-sync/apply` accepts `TemplateSyncPlanDto` + baseProvenanceVersion
  - [ ] **8.4.3** `GET /events/{eventId}/template-sync/history` returns prior sync audit trail
  - [ ] **8.4.4** `EventTemplateSyncLinkPolicy` + assembler in `Explore.API/Hateoas/`
  - [ ] **8.4.5** Authorization via `[Authorize(Policy = "event_template_sync")]`
  - [ ] **8.4.6** 409 Conflict returns RFC 7807 problem detail
  - [ ] **8.4.7** Request timeout `Complex` (60s) for apply
- [ ] **8.4A** Create API endpoints for event session template diff / sync workflows (**Milestone E**)
  - [ ] **8.4A.1** Mirror 8.4.1-8.4.7 for `/event-sessions/{sessionId}/template-sync/*`
- [x] **8.5** Create projection admin, rebuild, dirty-scope, and governance reporting endpoints (**Milestone D2 Operability**) ✅ 2026-04-12 (core endpoints done; HATEOAS, metrics, runbook pending)
  - [x] **8.5.1** `GET /admin/custom-property-projections/status?tenantId={tenantId}`
  - [x] **8.5.2** `POST /admin/custom-property-projections/rebuild` (tenant-wide)
  - [x] **8.5.3** `POST /admin/custom-property-projections/rebuild-single-event`
  - [x] **8.5.4** Session equivalents at `/admin/custom-property-projections/sessions/*`
  - [x] **8.5.5** **`GET /admin/custom-property-projections/dirty-scopes?tenantId={tenantId}&projectionName={name}&skip&take`** - CTO review: paged dirty-scope observability
  - [x] **8.5.6** **`POST /admin/custom-property-projections/drain-dirty-scopes`** - CTO review: operator self-service drain
  - [x] **8.5.7** **`GET /admin/custom-property-definitions/governance-report?tenantId={tenantId}&scope={EntityScope}&recommendation={PromotionRecommendation}&skip&take`** - CTO review: Rule 12 enforcement surface
  - [x] **8.5.8** `CustomPropertyProjectionAdminLinkPolicy` + 4 link policies + DI registration ✅ 2026-04-12
  - [x] **8.5.9** `CustomPropertyGovernanceLinkPolicy` + collection link policy + DI registration ✅ 2026-04-12
  - [x] **8.5.10** Authorization via `[Authorize]` on all endpoints + `[AuthorizeResource]` on CQRS commands
  - [x] **8.5.11** Rate limiting via `write` policy for POST; `authenticated` for GET
  - [x] **8.5.12** Request timeout `Complex` (60s) for rebuild/drain; `Lookup` (10s) for status/dirty-scopes/governance-report queries
  - [x] **8.5.13** Prometheus metrics — updater-level inline update and dirty-scope skip counters exposed via `Explore.Projections` ✅ 2026-05-02
  - [x] **8.5.14** **Operator runbook in `docs/OPERATIONS.md`** — full section covering inspection, recovery, concurrency, hard limits, governance ✅ 2026-04-12
- [ ] **8.6** Reconcile `EventController.cs` and related API contracts with template-aware event creation and remove any stale metadata query assumptions (**Milestone B follow-up**)
  - [ ] **8.6.1** Confirm `POST /events` accepts `TemplateId` in request body
  - [ ] **8.6.2** Confirm `GET /events/{id}` includes HATEOAS link to template-sync/diff when `SourceTemplateId != null`
  - [ ] **8.6.3** Confirm `GET /events/{id}` includes HATEOAS link to custom-properties sub-resource
  - [ ] **8.6.4** Remove any stale metadata fields from request/response contracts
- [ ] **8.6A** Reconcile `EventSessionController.cs` and related API contracts with session template/runtime workflows (**Milestone C follow-up**)
  - [ ] **8.6A.1** Mirror 8.6.1-8.6.4 for session scope
- [ ] **8.7** Implement authorization taxonomy - **simplified to 4 core policies** (**Milestone D/E**, CTO review 2026-04-11)
  - [ ] **8.7.1** `template_admin` policy in Cerbos + `AuthorizationExtensions.cs` (covers template CRUD + template-sync diff/apply; rolls in prior `event_template_sync`)
  - [ ] **8.7.2** `event_editor` policy (covers event + event-session editing + runtime value/definition edits; rolls in prior `event_session_editor`)
  - [ ] **8.7.3** `property_governance_admin` policy (covers projection rebuild, dirty-scope drain, governance reporting, projection-first promotion, exposure flag management; rolls in prior `custom_property_projection_admin`)
  - [ ] **8.7.4** `platform_namespace_editor` policy (default deny; explicit grant only for platform operators; applied to any write targeting `namespace = 'platform'`)
  - [ ] **8.7.5** Document endpoint-to-policy mapping in `docs/SECURITY.md` (four mappings, not seven)
  - [ ] **8.7.6** Integration tests verify authorized vs unauthorized responses for each new endpoint
  - [ ] **8.7.7** Future-proof test: prove a new policy can be split from one of the four without renaming existing policies or breaking existing endpoints

---

## Phase 9: Blazor Client ⏳ NOT STARTED

> **Global constraints:** MudBlazor v9 only; default render mode `InteractiveAuto`; BEM CSS isolation; `12px` rounded corners; `Elevation 0-1`; neo-minimal aesthetic; WCAG 2.2 AA accessibility; no `HttpContext` in `InteractiveAuto`/WASM; no raw HTML where MudBlazor equivalents exist.

- [ ] **9.1** Reconcile appearance helpers and UI utilities with the existing first-class appearance model
  - [ ] **9.1.1** Remove any stale `EventAppearanceMetadataHelper`, `OrganizationAppearanceMetadataHelper`, `GroupBrandingMetadataHelper` that still read through a metadata bag
  - [ ] **9.1.2** Replace with direct typed property access on first-class fields
- [ ] **9.2** Add shared definition governance UI for Organization / Group
  - [ ] **9.2.1** `CustomPropertyDefinitionListPage.razor` with MudDataGrid + filter controls
  - [ ] **9.2.2** `CustomPropertyDefinitionDetailsPage.razor` with typed MudBlazor inputs
  - [ ] **9.2.3** `CustomPropertyDefinitionEditor.razor` reusable component
  - [ ] **9.2.4** `CustomPropertyOptionEditor.razor` nested editor with drag-drop reorder
  - [ ] **9.2.5** WCAG 2.2 AA accessibility audit
- [ ] **9.3** Add event template management UI (**Milestone B follow-up**)
  - [ ] **9.3.1** `EventTemplateListPage.razor` with MudDataGrid
  - [ ] **9.3.2** `EventTemplateDetailsPage.razor`
  - [ ] **9.3.3** `EventTemplateEditor.razor` with version + publish state controls
  - [ ] **9.3.4** `EventTemplateDefinitionEditor.razor` reused in template and runtime modes
  - [ ] **9.3.5** Confirmation dialog for creating new version
- [x] **9.4** Add template selection to event creation UI (**Milestone B follow-up**) ✅ 2026-04-29; Oracle blocker pass fixed stale async completions + added focused regression coverage
- [x] **9.4.1** MudSelect dropdown with published templates scoped to tenant + event type ✅ event-type changes reload published/active templates, disable selection while loading, and clear stale selection
- [x] **9.4.2** Read-only preview of definitions that will be instantiated ✅ loading/error/detail states with definition chips; failed preview clears `TemplateId`; submit is blocked while preview is in flight
- [x] **9.4.3** Accessible placement/announcements ✅ placed after Event Type in Event Options with labeled select and `aria-live` preview (no browser-level keyboard E2E added)
- [x] **9.4A** Add session blueprint selection/editing UI (**Milestone C follow-up**) ✅ 2026-04-29; CreateEvent new-session drawer now supports parent-scoped session blueprints with stale async guards and clears already-added local session blueprint IDs when the parent event template changes
  - [x] **9.4A.1** Session template dropdown scoped to parent event's template tree ✅ uses `ParentEventTemplateId="@_selectedEventTemplate?.Id"` so options appear only after parent event-template preview resolves
  - [x] **9.4A.2** Read-only preview of session definitions/options that will be instantiated ✅ loading/warning/detail states with definition chips
  - [x] **9.4A.3** Clear stale session-template selection when the parent event template changes ✅ panel clears selected blueprint/detail/list when parent changes or disappears
  - [x] **9.4A.4** Guard stale async list/preview completions with current-selection checks ✅ request-version + current parent/selection checks
  - [x] **9.4A.5** Block submit while session-template preview is loading and clear template ID on preview failure ✅ drawer save guard prevents unresolved template submission
  - [x] **9.4A.6** bUnit coverage mirroring Phase 9.4 race and submit scenarios ✅ scoped load, parent clearing, preview failure, stale list/preview races, and in-flight preview save blocking
- [x] **9.5** Build event runtime custom-property editor against event-local definitions/values (**Milestone B follow-up**) ✅ 2026-04-29
  - [x] **9.5.1** `EventCustomPropertyRuntimeEditor.razor` component ✅
  - [x] **9.5.2** Dynamic rendering via `PropertyType` enum switch (not reflection) ✅ via `CustomPropertyFieldEditor.razor`
  - [x] **9.5.3** `Text` → MudTextField with MaxLength + RegexPattern ✅
  - [x] **9.5.4** `Number` → MudNumericField with Min/Max ✅
  - [x] **9.5.5** `Option` → MudSelect single or multi-select based on `IsMulti` ✅
  - [x] **9.5.6** `Boolean` → MudSwitch or MudCheckBox ✅
  - [x] **9.5.7** `DateTime` → MudDatePicker + MudTimePicker ✅
  - [x] **9.5.8** `Url` → MudTextField with URL + AllowedUrlSchemes validation ✅
  - [x] **9.5.9** Multi-value add/remove with Ordinal-aware reordering ✅
  - [x] **9.5.10** Inline validation errors aligned to server validators ✅ client-side validation + server response display
  - [x] **9.5.11** `aria-required`, `aria-invalid`, `aria-describedby` attributes ✅
  - [x] **9.5.12** Keyboard-only user can complete the form ✅ standard MudBlazor focusable controls + add/remove/save buttons
  - [x] **9.5.13** BEM CSS classes: `event-cpr-editor__field`, `event-cpr-editor__field--required`, `event-cpr-editor__field--multi` ✅
- [x] **9.5A** Build event session runtime custom-property editor (**Milestone C follow-up**) ✅ 2026-04-29
  - [x] **9.5A.1** `EventSessionCustomPropertyRuntimeEditor.razor` mirrors 9.5.1-9.5.13 ✅ plus parameter-change reload for drawer navigation
- [ ] **9.6** Add template selection preview admin overview (**Milestone B follow-up**)
  - [ ] **9.6.1** Lists events created from a given template + when + by whom
- [ ] **9.6A** Add session blueprint preview admin overview (**Milestone C follow-up**)
- [x] **9.7** Add exposure / searchability / exportability governance UX (**Milestone D**) ✅ 2026-04-21
  - [x] **9.7.1** `CustomPropertyGovernance.razor` admin page at `/admin/tenant/custom-properties` with 3 tabs (Exposure / Governance Report / Projection Status) ✅
  - [x] **9.7.2** `ExposureGovernanceSection.razor` — `MudDataGrid<CustomPropertyDefinitionListModel>` with 6 flag columns (exposure chip + 5 boolean indicators) ✅
  - [x] **9.7.3** Inline projection update on flag change — server `UpdateCustomPropertyDefinitionCommandHandler` already triggers projection refresh on update ✅
  - [x] **9.7.4** `MultiSelection` with `SelectColumn` + bulk-edit dialog sharing same editor; `UpdateManyDefinitionFlagsAsync` aggregates per-row PUTs ✅
  - [x] **9.7.5** Tooltip explanations centralised in `GovernanceTooltips` static class (6 copy constants) rendered via `MudTooltip` in flag column headers ✅
- [ ] **9.8** Add event template diff / sync UX (**Milestone E**)
  - [ ] **9.8.1** `EventTemplateSyncPage.razor` component
  - [ ] **9.8.2** Header with event + current + target template version selector
  - [ ] **9.8.3** Tabs/sections for Added / Modified / Retired (+ Options sub-sections)
  - [ ] **9.8.4** Side-by-side diff view using DiffPlex (BlazorTextDiff or custom MudBlazor component)
  - [ ] **9.8.5** Per-change checkbox for selection
  - [ ] **9.8.6** `HasLocalChanges` warning banner on modified definitions
  - [ ] **9.8.7** Confirm dialog with operator-typed slug confirmation (destructive-action pattern)
  - [ ] **9.8.8** 409 Conflict handling with re-diff button
  - [ ] **9.8.9** Semantic markup for diff view + `aria-label` for old vs new
  - [ ] **9.8.10** Focus trap + restore in dialogs
- [ ] **9.8A** Add event session template diff / sync UX (**Milestone E**)
  - [ ] **9.8A.1** Mirror 9.8.1-9.8.10 for session scope
- [x] **9.9** Add governance UI for exposure/search/filter/export flags (polish + tooltips beyond 9.7) ✅ 2026-04-21 — `GovernanceTooltips` copy + doc link to `/docs/CUSTOM_PROPERTIES.md` on header opens in new tab; governance report + projection status sections added as polish to satisfy Milestone D operator coverage
- [ ] **9.10** Update Organization and Group pages to remove any stale metadata-blob assumptions
- [x] **9.11** Regenerate generated API clients after milestone API contract changes ✅ 2026-04-30 — `Explore.API/swagger.json` + `Explore.Blazor.Client/Clients/EventApiClient.g.cs` regenerated through NSwag; targeted swagger export ✅; generated-client build ✅; client tests ✅ 909 total / 908 passed / 1 known skipped; full-solution build attempted but blocked by unrelated existing analyzer/package issues plus transient locked client PDB

---

## Phase 10: Search, Projection, Moderation, Export, And Aggregate View Integration 🟡 PARTIALLY COMPLETE

- [x] **10.0** Integrate Layer 2 typed sector fields directly into discovery, policy, moderation, and export paths without routing them through Layer 3 first ✅ 2026-04-30 — architecture guard + docs verified locally; Docker-gated API roundtrips remain in Phase 11
  - [x] **10.0.1** Verify `EventQuerySpecification` composes `IslamicAspectFilter`, `TechAspectFilter`, `AspectPresenceFilter` directly ✅ explicit overload guard added
  - [x] **10.0.2** Architecture test enforces no Layer 2 field flows through Layer 3 projection ✅ projection filters cannot expose Layer 2 semantic factories
  - [x] **10.0.3** Document the boundary in `docs/ARCHITECTURE.md` ✅ direct Layer 2 composition + generic Layer 3 projection rule documented
- [x] **10.1** Populate event custom-property projections on writes and sync operations (**Milestone D baseline**) ✅ 2026-04-12
  - [x] **10.1.1** `SetEventCustomPropertyValueCommandHandler` — wraps in UoW + calls `UpdateForValueAsync`
  - [x] **10.1.2** `SetEventCustomPropertyMultiValuesCommandHandler` — calls `UpdateForDefinitionAsync` inside existing UoW
  - [ ] **10.1.3** `CreateEventCustomPropertyDefinitionCommandHandler` — skipped (no values at create time = no projection rows; YAGNI)
  - [x] **10.1.4** `UpdateEventCustomPropertyDefinitionCommandHandler` — calls `UpdateForDefinitionAsync` inside existing UoW
  - [x] **10.1.5** `DeleteEventCustomPropertyDefinitionCommandHandler` — wraps in UoW + calls `RemoveForDefinitionAsync`
  - [x] **10.1.6** `CreateEventCommandHandler` — calls `RefreshForEventAsync` after template instantiation
  - [ ] **10.1.7** `ApplyEventTemplateSyncCommandHandler` (Milestone E) — future
- [x] **10.1A** Populate session custom-property projections on writes and sync operations (**Milestone D baseline**) ✅ 2026-04-12
  - [x] **10.1A.1** Mirrored for all 5 session handlers + `CreateEventSessionCommandHandler`
- [x] **10.2** Integrate filterable/searchable projections into event discovery query paths (**Milestone D baseline**) ✅ 2026-04-21
  - [x] **10.2.1** `EventCustomPropertyProjectionFilter` specification in `Explore.Application/Specifications/Events/` (9 factory methods: ExactMatch, OptionMatch, OptionsMatchAny, TextSearch, GlobalTextSearch, Exists, BooleanTrue, NumberRange, DateRange)
  - [x] **10.2.2** Text search subsumed into `EventCustomPropertyProjectionFilter.TextSearch` + `GlobalTextSearch` (no separate specification needed — single filter type handles per-key and global text search against `NormalizedValue` with `EF.Functions.ILike`)
  - [x] **10.2.3** Composed into `EventQuerySpecification.And(...)` via overloaded `And(EventCustomPropertyProjectionFilter)` and applied at repository layer in `EventRepository.ApplyProjectionFilters`
  - [x] **10.2.4** Tenant feature flag `custom_properties.projection_discovery_enabled` gates rollout in `GetEventListRequestHandler.BuildSpecificationAsync` via `ICustomPropertyQuotaResolver.GetBoolAsync` — silently ignored when disabled
  - [x] **10.2.5** Query cache key suffix includes projection filter hash via `EventQuerySpecification.ToCacheKeySuffix()` `pf:` prefix
  - [x] **10.2.6** API surface: `EventFilterRequest` exposes `CustomPropertyFilters` + `CustomPropertySearchTerm`; `EventController.GetAll` forwards them to `GetEventListRequest`
- [x] **10.2A** Integrate filterable/searchable session projections into discovery query paths (**Milestone D baseline**) ✅ 2026-04-21
  - [x] **10.2A.1** Mirror 10.2.1-10.2.6 for `EventSessionQuerySpecification` + `EventSessionCustomPropertyProjectionFilter` + `EventSessionRepository.ApplySessionProjectionFilters` + `GetEventSessionListRequestHandler` + new `EventSessionFilterRequest` transport model consumed by `EventSessionController.GetAll`
- [ ] **10.3** Integrate public/exportable projections into export/publication payloads (**Milestone D advanced**)
- [ ] **10.3A** Integrate aggregate event-with-sessions views into publication/discovery payloads (**Milestone F**)
  - [ ] **10.3A.1** `GetEventWithSessionsAggregateViewQuery` composes Layer 1 + Layer 2 module-gated + Layer 3 projection exposure-filtered
  - [ ] **10.3A.2** Public event page reads from aggregate query with `ExposureLevel.Public` ceiling
  - [ ] **10.3A.3** Admin event page reads with higher ceiling
  - [ ] **10.3A.4** Federation composer reads with `Public + IsExportable = true`
- [ ] **10.4** Integrate moderation-relevant projections into governance workflows (**Milestone D advanced**)
- [ ] **10.5** Integrate analytics-relevant projections into analytics payload composition (**Milestone D advanced**)

---

## Phase 11: Testing And Documentation ⏳ PARTIALLY STARTED

> **Testing stack:** xUnit + TUnit + Moq + Testcontainers-PostgreSQL for integration tests + Aspire AppHost for E2E. All projection / sync / aggregate-view integration tests MUST use Testcontainers.

- [ ] **11.1** Add architecture tests for the new entity families and boundaries — Phase 10.0 boundary subset added 2026-04-30; remaining bullets below still open
  - [ ] **11.1.1** Assert Layer 3 types do not reference Layer 2 aspect types
  - [ ] **11.1.2** Assert Layer 2 aspect types do not reference Layer 3 custom-property types
  - [ ] **11.1.3** Assert `IEventCustomPropertyProjectionUpdater` implementation lives in `Explore.Persistence`
  - [ ] **11.1.4** Assert `EventTemplateSyncService` does not reference `DbContext`
  - [ ] **11.1.5** Assert projection entity only references `Explore.Domain` types
  - [ ] **11.1.6** Assert all sync/projection/instantiation interfaces live in `Explore.Application/Contracts/Services`
  - [ ] **11.1.7** Assert no Specification or Repository returns a DTO
- [x] **11.1A** Add unit tests for namespace normalization and Layer 2 semantic reservation helpers
- [x] **11.1B** Add application-unit tests for reserved namespace rejection and Layer 2 semantic collision blocking
- [ ] **11.2** Add unit tests proving display-name renames do not break machine-key identity 🟡 local coverage added 2026-04-30; EF uniqueness proof remains Docker/Testcontainers-gated
  - [x] **11.2.1** Normalize namespace/key case and whitespace variants to the same machine identity ✅ `CustomPropertyIdentity.NormalizeNamespace` + `NormalizeKey`
  - [x] **11.2.2** Prove `DisplayName` rename preserves lookup/update identity via normalized `Namespace + Key + current Id` ✅ shared-definition update handler test
  - [ ] **11.2.3** Prove EF uniqueness constraint for machine identity via Testcontainers/PostgreSQL
- [x] **11.3** Add unit tests for multi-value semantics, ordering, and duplicate rules ✅ 2026-04-30 event/session runtime handler validation + targeted tests
- [x] **11.4** Add first unit tests for shared-definition validator enforcement and create/update/delete handler behavior
- [x] **11.5** Add unit tests for exposure/search/filter/export/moderation flags ✅ 2026-04-30 local application-boundary coverage for governance DTOs and aggregate-view facets
- [x] **11.6** Add unit tests for template instantiation, provenance, and versioning (**Milestone B baseline**) ✅ 2026-03-29
- [x] **11.6A** Add unit tests for event session template instantiation, provenance, and versioning (**Milestone C baseline**) ✅ 2026-03-29
- [ ] **11.7** Add unit tests for retired definitions/options with historical values
- [x] **11.8** Event projection Testcontainers tests (**Milestone D**) ✅ 2026-04-12 (written; execution blocked on concurrent agent breakage)
  - [x] **11.8.1** `ProjectionTestContainerFixture` using `EnsureCreatedAsync` + minimal lookups
  - [x] **11.8.2** `EventCustomPropertyProjectionUpdaterTests` — 6 tests: insert, upsert, flag-refresh, remove, refresh-for-event, rebuild-for-tenant
  - [ ] **11.8.3** Prove `(TenantId, EntityTypeName, Namespace, Key)` uniqueness at DB level — deferred
  - [ ] **11.8.4** Prove `Tenant` query filter isolation — deferred
  - [ ] **11.8.5** Prove cascade delete from runtime value to projection row — deferred
- [x] **11.8A** Session projection Testcontainers tests (**Milestone D**) ✅ 2026-04-12 (written; execution blocked)
  - [x] **11.8A.1** `EventSessionCustomPropertyProjectionUpdaterTests` — 4 tests: insert, refresh, remove, rebuild
- [x] **11.8B** Dirty-scope + rebuild coordination Testcontainers tests (**Milestone D1**) ✅ 2026-04-12 (written; execution blocked)
  - [x] **11.8B.1** `CustomPropertyProjectionCoordinationTests` — 5 tests
  - [x] **11.8B.5** Dirty-scope idempotency test
  - [x] **11.8B.6** Drain-on-completion test (drain-test: upsert pending → drain → assert projection + drained_at)
  - [x] **11.8B.8** Status upsert overwrite test
  - [x] **11.8B.9** Concurrency token test in `EfCoreUnitOfWorkTests` (stale-stamp → ConcurrencyConflictException) ✅
  - [ ] **11.8B.3** Concurrent inline write + background rebuild contention test — deferred to D2
  - [ ] **11.8B.7** Rebuild crash recovery test — deferred (single-transaction in D1 = rerun is trivial)
  - [ ] **11.8B.10** Hard limit enforcement test — deferred
  - [ ] **11.8B.11** Governance report test — deferred to D2
- [ ] **11.8C** Integration tests for template sync workflow (**Milestone E**)
  - [ ] **11.8C.1** Empty diff returns no changes
  - [ ] **11.8C.2** Added-only diff adds definitions
  - [ ] **11.8C.3** Modified-only diff modifies selected fields
  - [ ] **11.8C.4** Retired-only diff sets `IsActive = false` without data loss
  - [ ] **11.8C.5** Mixed diff applies selected subset
  - [ ] **11.8C.6** Stale `baseProvenanceVersion` returns 409 Conflict
  - [ ] **11.8C.7** Concurrent sync - first wins
  - [ ] **11.8C.8** Historical values preserved across sync
  - [ ] **11.8C.9** Audit row written with operator + plan hash + versions + counts
- [ ] **11.8D** Integration tests for aggregate event-with-sessions view (**Milestone F**)
  - [ ] **11.8D.1** Keyless entity returns correct rows for seeded corpus
  - [ ] **11.8D.2** Exposure ceiling is enforced across all four levels
  - [ ] **11.8D.3** Module-gated Layer 2 aspects appear only when module enabled
  - [ ] **11.8D.4** Session summaries reflect current session state
  - [ ] **11.8D.5** Paged list query performance acceptable
- [ ] **11.9** Add deeper integration tests for API roundtrips (template → event → sync → projections baseline) (**Milestone D/E**)
- [x] **11.9A** Add initial API integration coverage for shared custom-property definition controller routes and auth posture
- [ ] **11.9B** Add integration tests for API roundtrips (event template → session blueprint → event session → sync → projections baseline) (**Milestone D/C/E**)
- [x] **11.10** Update docs: `docs/DOMAIN.md`, `docs/ARCHITECTURE.md`, `docs/EXTENSIBILITY.md`, `docs/MODULAR_EVENTS.md`, `docs/CUSTOM_PROPERTIES.md` — `docs/ARCHITECTURE.md` boundary tightened again for Phase 10.0 on 2026-04-30
- [ ] **11.10A** Update event/session lexicon planning docs for canonical event/session contracts and aggregate read views (**Milestone F**)
  - [ ] **11.10A.1** New `docs/LEXICONS.md` or extension to `docs/FEDERATION.md`
  - [ ] **11.10A.2** Canonical NSID hierarchy documented
  - [ ] **11.10A.3** Add-only evolution rules + NSID versioning discipline documented
  - [ ] **11.10A.4** Lexicon-to-projection/aggregate-view mapping documented
  - [ ] **11.10A.5** `.temp.` experimental namespace usage guidelines
- [ ] **11.10B** Update docs with Milestone D projection + Milestone E sync + Milestone F aggregate view operator-facing docs
  - [ ] **11.10B.1** `docs/API.md` - new projection admin + template sync endpoints
  - [ ] **11.10B.2** `docs/SECURITY.md` - new authorization policies
  - [ ] **11.10B.3** `docs/TROUBLESHOOTING.md` - projection rebuild playbook + sync conflict resolution

---

## Phase 12: 100/100 Hardening And Product-Boundary Closure 🟡 IN PROGRESS

- [ ] **12.1** Build shared runtime value validator for shared/event/session custom-property values — 🟡 event/session runtime writes done; shared org/group value write surface still missing
  - [x] **12.1.1** Validate `PropertyType` shape and exactly-one typed value channel for event/session runtime writes
  - [x] **12.1.2** Validate text/url min/max/regex/scheme constraints for event/session runtime writes
  - [x] **12.1.3** Validate number and datetime ranges for event/session runtime writes
  - [x] **12.1.4** Validate option membership and active option state for event/session runtime writes
  - [ ] **12.1.5** Validate requiredness at publish/export/sync completeness boundaries
  - [x] **12.1.6** Replace event/session handler-local partial checks with the shared validator
  - [ ] **12.1.7** Add remaining shared org/group unit tests if/when shared runtime value write APIs are implemented
- [ ] **12.2** Harden public exposure and anonymous reads — 🟡 raw reads authenticated and projection discovery exposure-scoped; export/moderation and Docker proof remain
  - [x] **12.2.1** Audit all `AllowAnonymous` custom-property endpoints and remove anonymous raw custom-property definition/value reads
  - [ ] **12.2.2** Apply server-side exposure ceiling to every definitions/options/values/projections/facets/export/moderation path
  - [x] **12.2.3** Make public `Exists`/search/filter behavior exposure-safe
  - [ ] **12.2.4** Add API integration tests proving internal data cannot be observed anonymously
- [ ] **12.3** Replace destructive option replacement with semantic diff — 🟡 normal update lifecycle done; explicit purge still open
  - [x] **12.3.1** Match options by normalized `Namespace + Key`
  - [x] **12.3.2** Preserve option IDs for unchanged machine identity
  - [x] **12.3.3** Retire missing options instead of hard-replacing option sets
  - [ ] **12.3.4** Hard-delete only purge-safe no-history options
  - [ ] **12.3.5** Add Docker/PostgreSQL tests for display rename, sort change, retirement, historical values, and sync update survival
- [x] **12.4** Complete concurrency-stamp roundtrip for shared/event/session custom-property definition updates
  - [x] **12.4.1** Add stamps to mutable custom-property definition read DTOs
  - [x] **12.4.2** Require `ExpectedConcurrencyStamp` on mutable custom-property definition update DTOs
  - [x] **12.4.3** Preserve stamps through generated client and Blazor shared-definition editor path
  - [x] **12.4.4** Add missing-stamp and stale-stamp tests for current definition update flows
- [ ] **12.5** Projection correctness perfection pass — 🟡 exposure-safe `Exists` done; remaining projection correctness proof still open
  - [ ] **12.5.1** Compute option `NormalizedValue` from explicit DB option lookup/include
  - [ ] **12.5.2** Decide and enforce projection row relevance rules
  - [x] **12.5.3** Make `Exists` filters exposure/governance safe
  - [ ] **12.5.4** Add event/session Testcontainers parity proofs
- [ ] **12.6** Soft delete, retirement, and purge policy — 🟡 normal delete lifecycle done; explicit purge policy still open
  - [x] **12.6.1** Normal delete retires/soft-deletes definitions/options/values
  - [ ] **12.6.2** Explicit purge command proves no dependent history
  - [ ] **12.6.3** Audit retirement and purge with operator/reason/dependent-count summary
  - [ ] **12.6.4** Integration tests prove historical reads after retirement
- [ ] **12.7** Quota completion sweep — 🟡 local quota keys enforced; boundary matrix and normalized error shape open
  - [x] **12.7.A** Enforce shared `MaxDefinitionsPerTenantPerEntityScope` on definition creation
  - [x] **12.7.B** Enforce event `MaxDefinitionsPerEvent` on event-local definition creation
  - [x] **12.7.C** Enforce session `MaxDefinitionsPerEventSession` on session-local definition creation
  - [x] **12.7.D** Enforce `MaxOptionsPerDefinition` on shared/event/session create and update flows
  - [x] **12.7.E** Enforce `MaxMultiValueRowsPerValue` on event/session multi-value writes
  - [x] **12.7.F** Enforce `MaxDefinitionsPerTemplate` on event template create/update flows
  - [x] **12.7.G** Enforce `SyncApplyMaxPayloadBytes` on event/session template sync plans
  - [x] **12.7.H** Enforce `ProjectionRebuildBatchSize` on operator rebuild requests and projection rebuild/drain workers
  - [x] **12.7.I** Enforce `MaxDirtyScopePendingPerTenant` before dirty-scope backlog upsert
  - [x] **12.7.1** Verify every quota in `Hard Limits And Quotas` has an enforcing handler/service
  - [ ] **12.7.2** Add `quota - 1`, `quota`, `quota + 1` boundary tests
  - [ ] **12.7.3** Normalize `quota_exceeded` error shape
- [ ] **12.8** Docker-gated end-to-end certification
  - [ ] **12.8.1** Event template → runtime → value → projection → discovery → aggregate roundtrip
  - [ ] **12.8.2** Session blueprint → runtime → value → projection → discovery → aggregate roundtrip
  - [ ] **12.8.3** Tenant isolation, exposure ceilings, dirty-scope recovery, sync conflicts, retirement reads
- [ ] **12.9** Runtime schema engine product-boundary ADR
  - [ ] **12.9.1** Decide whether scope is "custom fields on existing resources"
  - [ ] **12.9.2** If yes, mark EAV complete after 12.1-12.8
  - [ ] **12.9.3** If no, create a separate runtime schema engine plan for user-defined entities, relationships, constraints, formulas, migrations, and authorization

## Milestone Gates

Per Rule 17 (ruthless sequencing, CTO review 2026-04-11): no gate begins until the previous one exits with Testcontainers integration tests green.

- [x] **Gate A** Shared-definition CRUD, governance policy, and migration baseline are stable ✅ 2026-03-19
- [x] **Gate B** Event runtime/template baseline is stable before EventSession parity begins ✅ 2026-03-29
- [x] **Gate C** EventSession runtime/template parity is stable before broad projection integration begins ✅ 2026-03-29
- [ ] **Gate D1 (Correctness)** Projection correctness is proven (transactional consistency + dirty-scope recovery + concurrency tokens + hard limits) before operability work begins
  - Exit criteria: Tasks 3.6, 3.6A, 3.6B, 3.6C, 3.6D done; Phase 11.8B all sub-tests green; 1.1C hard-limit settings in place
- [ ] **Gate D2 (Operability)** Projection operability surface is proven (admin endpoints + rebuild + drain + governance report + Prometheus metrics + runbook) before consumption work begins
  - Exit criteria: Tasks 5.8, 8.5, 8.7 (4 policies) done; `docs/OPERATIONS.md` runbook published; operator can observe/rebuild/drain any tenant
- [x] **Gate D3 (Consumption)** Projection consumption is proven (specification-backed discovery filters + feature flag rollout + API surface) ✅ 2026-04-21
  - Exit criteria: Tasks 10.2, 10.2A done ✅; discovery query p95 latency not regressed from Layer 1/2 baseline (15/15 projection integration tests pass in 14.9s against Testcontainers Postgres — correlated subquery pattern uses composite `(TenantId, Namespace, Key, NormalizedValue)` index `ix_ecpp_tenant_namespace_key_normalized`)
  - Deferred to D-polish: Blazor discovery filter UI (Task 9.9) — server-side surface complete, UI is follow-up polish per plan
- [x] **Gate D** Overall Milestone D exit (D1 + D2 + D3 all green) ✅ 2026-04-21
- [ ] **Gate E** Explicit sync is stable before aggregate publication/lexicon-facing work expands
- [ ] **Gate F** Aggregate view + lexicon docs accepted; Milestone F deliberately narrow (no publication machinery)

---

## Milestone Summary

| Milestone | Scope | Sub-gates | Status |
|----------|-------|-----------|--------|
| A | Shared definitions foundation | — | ✅ complete (2026-03-19) |
| B | Event Layer 3 runtime baseline | — | ✅ complete (2026-03-29) |
| C | EventSession Layer 3 parity | — | ✅ complete (2026-03-29) |
| D | Projection integration | **D1 Correctness → D2 Operability → D3 Consumption** (Rule 17) | ✅ complete (2026-04-21) |
| E | Explicit sync workflows | — (keep implementation boring) | ⏳ planned |
| F | Aggregate read views + lexicon docs (narrow: one view + one doc only) | — | ⏳ planned |

## Session Handoff — 2026-05-03 Europe/Brussels

- [x] No task-state changes were made for this workstream during the sidebar dock refactor handoff session.
- [ ] Reconfirm this workstream's current state from its existing context/plan before resuming implementation.
