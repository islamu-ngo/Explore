<!-- ABOUTME: Live execution progress log for Milestones E + F + cleanup (EAV custom properties). -->
<!-- ABOUTME: Updated iteratively as delegations complete. Paired with eav-custom-properties-tasks.md. -->

# EAV Custom Properties — Execution Progress

**Last updated:** 2026-05-02 (Phase 12 delete lifecycle hardening recorded)
**Scope:** Cleanup and 100/100 hardening — runtime validation, exposure safety, Blazor/admin cleanup, and Docker-gated proof.
**Mode:** Development (no backward compatibility required).

---

## Milestone Snapshot

| Milestone | Phase | Status | Evidence |
|---|---|---|---|
| A Shared Definitions | — | ✅ 2026-03-19 | Historical |
| B Event L3 Runtime | — | ✅ 2026-03-29 | Historical |
| C Session L3 Parity | — | ✅ 2026-03-29 | 676 unit tests pass |
| D1 Correctness | — | ✅ 2026-04-12 | ConcurrencyStamp rollout + projection updater |
| D2 Operability | — | ✅ 2026-04-12 | Admin endpoints + runbook |
| D3 Consumption | — | ✅ 2026-04-21 | ProjectionFilter spec + 9 factories |
| **E Explicit Sync** | Phase 1a App (event) | ✅ 2026-04-24 | `bg_db54b281` / tests pass |
| E | Phase 1b App (session) | ✅ 2026-04-24 | Same delegation, mirror code |
| E | Phase 2 API Layer | ✅ 2026-04-24 | Controllers + HATEOAS policies on disk |
| E | Phase 3 Blazor UI | ✅ 2026-04-24 | HAL gating applied + bUnit tests passing |
| E | Phase 4 Integration Tests | ⏳ PENDING | `bg_debe3b3b` failed — retry needed |
| **F Aggregate+Lexicon** | Phase 1 View Entity+SQL | ✅ 2026-04-24 | `bg_b434afda` |
| F | Phase 2 DTOs+CQRS | ✅ 2026-04-24 | `bg_6d710714` retry / 59+ new tests |
| F | Phase 3 Integration Tests | ⏳ PENDING | Combined with E Phase 4 retry |
| F | Phase 4 LEXICONS.md | ✅ 2026-04-24 | `bg_0907d67e` / `docs/LEXICONS.md` |
| Cleanup | Phase 7 Stale JSONB refs | ✅ 2026-04-24 | Zero `MetadataJson` refs found, already cleaned |
| Cleanup | Phase 9.1 Stale helpers | ✅ 2026-04-24 | Zero changes needed |
| Cleanup | Phase 9.2 Definition CRUD UI | ✅ 2026-04-24 | bg_a65ca891 — 7 new files |
| Cleanup | Phase 9.3 Template CRUD UI | ✅ 2026-04-24 | bg_b8f4ef4b — 10+ new files |
| Cleanup | Phase 9.4 Template selection | ✅ 2026-04-29 | CreateEvent template picker + read-only preview + stale async guards; build + client tests pass |
| Cleanup | Phase 9.4A Session blueprint selection | ✅ 2026-04-29 | CreateEvent new-session drawer picker + parent-template scoping + stale async/session clearing guards; build + client tests pass |
| Cleanup | Phase 9.5/9.5A Runtime editors | ✅ 2026-04-29 | Event/session runtime editors wired into edit flows; build + client tests pass; final Oracle review safe |
| Cleanup | Phase 9.6A Session blueprint preview | ⏳ PENDING | Mirrors Phase 9.6 for session scope |
| Cleanup | Phase 9.6 Template preview | ⏳ PENDING | — |
| Cleanup | Phase 9.10 Org/Group cleanup | ⏳ PENDING | — |
| Cleanup | Phase 9.11 NSwag regen | ✅ 2026-04-30 | Swagger snapshot + NSwag client regenerated; client build/regeneration succeeded; client tests pass; full solution build blocked by unrelated existing analyzer/package issues + transient locked client PDB |
| Cleanup | Phase 11+10.0 Architecture tests | 🟡 PARTIAL 2026-04-30 | Phase 10.0 boundary guard + Phase 11.2 local identity tests + Phase 11.3 runtime semantics + Phase 11.5 local flag coverage verified; API roundtrips and EF uniqueness proof remain Docker/Testcontainers-gated |
| Cleanup | Phase 8.5.13 Prometheus metrics | ✅ 2026-05-02 | Added updater-level inline update + dirty-scope skip counters to `Explore.Projections`; API build, persistence integration test build, application unit tests, and architecture tests pass |
| Hardening | Phase 12.1 Runtime validation | 🟡 PARTIAL 2026-05-02 | Event/session runtime writes now validate effective definition metadata through shared Application helper; shared org/group value writes still need wiring if/when located |
| Hardening | Phase 12.2 Exposure hardening | 🟡 PARTIAL 2026-05-02 | Raw custom-property definition/value controller reads are authenticated; public projection search/filter/`Exists` paths are exposure-ceiling scoped; export/moderation ceilings still require proof |
| Hardening | Phase 12.3 Option lifecycle | 🟡 PARTIAL 2026-05-02 | Shared/event/session `UpdateWithOptions` now preserves option IDs by `Namespace + Key` and retires omitted options instead of hard-replacing rows; definition hard-delete and purge workflows still need product decision |
| Hardening | Phase 12.4 Projection operability auth | ✅ 2026-05-02 | Projection status, dirty-scope, and row-inspection queries now require handler-level `custom_property_projection:view` authorization metadata, not only controller authentication |
| Hardening | Phase 12.5 Concurrency roundtrip | ✅ 2026-05-02 | Shared/event/session custom-property definition reads expose `ConcurrencyStamp`; updates require `ExpectedConcurrencyStamp` and throw `409 concurrent_update` before mutating tracked entities on stale writes |
| Hardening | Phase 12.6 Delete lifecycle | 🟡 PARTIAL 2026-05-02 | Normal shared/event/session definition deletes now retire and soft-delete definitions, options, and values instead of hard-deleting rows; explicit purge workflow/policy remains open |

---

## Verification Gate Results (end of session 2)

- `dotnet build --configuration Release --verbosity minimal`: ✅ **0 errors**, 1559 warnings (all pre-existing CA1707/CA2000 in unrelated test files), 20s
- `dotnet test --project Event.Application.UnitTests/...`: ✅ **943 passed / 0 failed / 0 skipped** (+103 over baseline 840)
- `dotnet test --project Explore.Blazor.Client.Tests/...`: ⚠️ 779 passed / **25 failed** / 1 skipped — all 25 failures are `ApiException Forbidden Status: 403` on pre-existing non-sync component tests from the Cerbos substrate expansion (see Gap 2)

---

## Session Delegation Ledger

| Task ID | Session ID | Scope | Result |
|---|---|---|---|
| `bg_db54b281` | `ses_240a43818ffeZ3xLUwdYMDMzYn` | E App Layer (event + session) | ✅ 41m 18s |
| `bg_b434afda` | `ses_240a2f94bffeiHvSiLDiy6IQks` | F Phase 1 view + SQL migration | ✅ 25m 43s |
| `bg_528b91a2` | `ses_240774bb8ffeTA1tAWBdvbzcjd` | E API layer | ✅ (tracker lost mid-run, work on disk) |
| `bg_6d710714` | `ses_240769e14ffeFdUViFKmofdlX9` | F DTOs + CQRS (2nd attempt) | ✅ (tracker lost mid-run, work on disk) |
| `bg_0907d67e` | `ses_24075d05cffeja74URGht1YwS8` | LEXICONS.md | ✅ 2m 29s |
| `bg_fa738248` | `ses_240371592ffeHNe0uxAkEcfxcL` | Blazor UI (both pages) | ⚠️ Infrastructure-failed 2×; partial work on disk — orchestrator patched pages + test stubs to restore build green |
| `bg_debe3b3b` | `ses_24036519fffey1tW1aPZG5uO5T` | Integration tests (E4 + F3) | ❌ Infrastructure-failed before output; retry required |
| `bg_bdbef870` | `ses_23ee34f13ffeBVti5L95D3bYkg` | Phase 9.1 stale helpers | ✅ Zero changes needed |
| `bg_a65ca891` | `ses_23ee393c6ffeTCcfCIYFe3kwtO` | Phase 9.2 definition CRUD UI | ✅ 7 files + DI reg |
| `bg_b8f4ef4b` | `ses_23ed87d33ffe8sxg39h079k71j` | Phase 9.3 template CRUD UI | ✅ 10+ files + HAL ext + DI |

---

## Known Gaps / Follow-ups

### Gap 1 — Blazor HAL gating deferred (E Phase 3) ✅ CLOSED 2026-04-24
- Resolved via Option A: API controllers now emit `HalResource<TemplateDiffDto>`, and Blazor pages gate the Apply button via `HasHalLink("sync-apply")`.

### Gap 2 — Cerbos substrate expansion side effects ✅ CLOSED 2026-04-24
- The E API delegate expanded the Cerbos authorization substrate (`MachineScopeMapping`, `IMachinePrincipalAccessor`, `CerbosPrincipalBuilder`, `FallbackAuthorizationService`) beyond the `template_admin` policy it was tasked with.
- Resolved in `Explore.Blazor.Client.Tests/Common/BlazorTestContext.cs` by registering a default `IMachinePrincipalAccessor` stub (`Current = null`, `IsMachineCaller = false`) so Blazor test DI stays on the human-user path instead of failing closed for missing machine-principal context.
- No production authorization code was reverted; fix stayed test-only.

### Gap 3 — Integration tests never ran
- `bg_debe3b3b` failed infrastructure-side before emitting output.
- Docker unavailable locally → cannot run `Event.Persistence.IntegrationTests` / `Event.API.IntegrationTests` in this environment; must retry via background agent with Docker.
- Scope to cover: 9 scenarios × event + session sync (18 sync cases) + 5 aggregate-view scenarios (correctness, exposure ceilings, module gating, tenant isolation) = 23 minimum cases.

### Gap 4 — Placeholder Blazor test stubs ✅ CLOSED 2026-04-24
- Implemented full bUnit coverage (6 scenarios each) for both sync pages, asserting HAL link presence/absence, slug confirmation, and 409 conflict banners.

---

## Remaining Work Queue (priority-ordered)

Closed cleanup items are no longer listed here; see the milestone snapshot and phase sections below for closure evidence.

1. **Retry E/F integration tests delegation** — highest Docker-gated proof remaining, covers Gap 3 (sync + aggregate-view correctness; Docker required).
2. **Export/moderation exposure proof** — identify any custom-property export/moderation payload composers and prove flags are applied only inside an explicit exposure ceiling; current search found no dedicated custom-property export/moderation endpoint beyond aggregate facets.
3. **Shared org/group value write product slice** — shared `CustomPropertyValue` storage exists, but no Application/API runtime write path was found to wire into the shared runtime validator.
4. **Explicit purge workflow/policy** — normal deletes now retire and soft-delete definitions/options/values; audited hard purge remains a separate product/ops decision.
5. **Phase 9.6/9.6A preview admin overviews** — useful admin polish, lower priority than 100/100 safety gaps.
6. **Phase 9.10 Organization/Group cleanup** — cleanup remains pending.

### Recommended Next Execution Slice

**Next implementation target:** Docker/Testcontainers proof for Phase 12 exposure, projection authorization, option lifecycle, concurrency, and delete lifecycle behavior, then export/moderation proof.

- Phase 9.4A is closed for CreateEvent's new-session drawer. EventEdit session blueprint selection remains deferred because current event read/update DTOs do not expose the parent event template identity.
- Current verification baseline: Phase 12.1/12.2/12.3/12.4/12.5/12.6 local hardening verified with targeted concurrency/application tests ✅ 1089/1089, projection-authorization unit tests ✅ 5/5, projection filter/application tests ✅ 1078/1078, targeted `Event.Persistence.IntegrationTests` option/delete lifecycle tests ✅ 6/6, `Explore.API` Release build ✅, `Explore.Blazor.Client` Release build ✅, `Explore.Blazor.Client.Tests` ✅ 968 total / 967 passed / 1 known skipped, `Event.Architecture.Tests` ✅ 142/142, and targeted `git diff --check` ✅. Phase 9.11 swagger export + generated client build succeeded; Phase 10.0 boundary guard LSP/diff checks clean and both new TUnit architecture guards pass; Phase 11.2 local domain/application unit tests pass; Phase 11.3 local runtime multi-value handler tests pass; Phase 11.5 local flag tests pass; Phase 8.5.13 metrics wiring verified with `Event.Persistence.IntegrationTests` Release build. Full solution build currently fails outside these slices on unrelated existing analyzer/package issues plus one transient locked `Explore.Blazor.Client.pdb` during static-web-assets fingerprinting.

### Phase 12.1 Runtime Value Validation + Phase 12.2 Exposure Hardening — 🟡 PARTIAL 2026-05-02

- Added `Explore.Application/Features/CustomProperties/CustomPropertyRuntimeValueValidator.cs` as a shared Application-layer validator helper for event and event-session runtime values.
- Event/session single-value and multi-value handlers now validate effective definition metadata before persistence: active definition, requiredness, `IsMulti`, typed value shape, text/url length, regex, URL scheme, number range, datetime range, option membership, active option state, and normalized duplicate values.
- Preserved existing repository contracts and handler flow: DTO validators still handle request shape, handlers still load definitions through repositories, map DTOs in Application, and call projection updaters after successful persistence.
- Added focused unit coverage for a number definition receiving text, inactive definitions rejecting writes, and inactive options rejecting option values.
- Hardened raw custom-property read controllers by replacing anonymous access with authenticated access on shared definition list/details, event definition list/details, event values, session definition list/details, and session values. This prevents public emission of raw definitions, options, and values until dedicated public surfaces apply explicit exposure ceilings.
- Verification: `dotnet test --project Event.Application.UnitTests/Event.Application.UnitTests.csproj --configuration Release --verbosity quiet -- --treenode-filter "/*/*/*SetEventCustomPropertyValueCommandHandlerTests*/*|/*/*/*SetEventCustomPropertyMultiValuesCommandHandlerTests*/*|/*/*/*SetEventSessionCustomPropertyValueCommandHandlerTests*/*|/*/*/*SetEventSessionCustomPropertyMultiValuesCommandHandlerTests*/*" --minimum-expected-tests 1 --no-progress --output Normal` ✅ 1060/1060; `dotnet build Explore.API/Explore.API.csproj --configuration Release --verbosity quiet` ✅; `dotnet test --project Event.Architecture.Tests/Event.Architecture.Tests.csproj --configuration Release --verbosity quiet -- --no-progress --output Normal` ✅ 142/142; targeted `git diff --check` ✅.
- Added explicit public exposure ceilings to event and session projection filter factories. Repository search/filter subqueries now require projection rows to be visible within the filter ceiling, and `Exists` now requires a filterable projection row instead of treating any projection row as public discoverability.
- Replaced enum-order exposure comparisons in projection-row repositories with an explicit visibility hierarchy so `ExposureLevel.Public` cannot accidentally include internal, organizer, or tenant-admin rows.
- Added focused unit coverage proving projection filters default to `ExposureLevel.Public` and preserve caller-provided ceilings.
- Verification for projection exposure slice: `dotnet test --project Event.Application.UnitTests/Event.Application.UnitTests.csproj --configuration Release --verbosity quiet -- --treenode-filter "/*/*/*EventCustomPropertyProjectionFilterTests*/*|/*/*/*EventSessionCustomPropertyProjectionFilterTests*/*" --minimum-expected-tests 1 --no-progress --output Normal` ✅ 1078/1078; `dotnet build Explore.API/Explore.API.csproj --configuration Release --verbosity quiet` ✅; `dotnet test --project Event.Architecture.Tests/Event.Architecture.Tests.csproj --configuration Release --verbosity quiet -- --no-progress --output Normal` ✅ 142/142.
- Hardened normal option update lifecycle in shared, event runtime, and session runtime repositories. `UpdateWithOptions` now merges by normalized `Namespace + Key`, preserves matched option IDs, remaps default-option references to the persisted ID, revives soft-deleted matched rows, and retires omitted options by clearing `IsDefault`/`IsActive` instead of hard-deleting and reinserting the option set.
- Added repository-level regression coverage in `Event.Persistence.IntegrationTests/Repositories/CustomPropertyOptionLifecycleRepositoryTests.cs` proving shared/event/session option updates preserve matched IDs, remap defaults, retire omitted rows, and avoid soft-delete flags.
- Verification for option lifecycle slice: `dotnet test --project Event.Persistence.IntegrationTests/Event.Persistence.IntegrationTests.csproj --configuration Release --verbosity quiet -- --treenode-filter "/*/*/*CustomPropertyOptionLifecycleRepositoryTests*/*" --minimum-expected-tests 1 --no-progress --output Normal` ✅ 3/3; `dotnet build Explore.API/Explore.API.csproj --configuration Release --verbosity quiet` ✅; `dotnet test --project Event.Architecture.Tests/Event.Architecture.Tests.csproj --configuration Release --verbosity quiet -- --no-progress --output Normal` ✅ 142/142; targeted `git diff --check` ✅.
- Hardened projection operability read requests with handler-level authorization metadata. Projection status, dirty-scope backlog, event projection-row inspection, and session projection-row inspection queries now implement `ISecureRequest` and require `custom_property_projection:view` via `[AuthorizeResource(ResourceKinds.CustomPropertyProjection, AuthorizationActions.CustomPropertyProjections.View)]`.
- Added reflection coverage in `Event.Application.UnitTests/Features/EventCustomPropertyProjections/Queries/ProjectionQueryAuthorizationMetadataTests.cs` proving all projection inspection query contracts carry the expected resource/action metadata and implement `ISecureRequest`.
- Verification for projection authorization slice: `dotnet test --project Event.Application.UnitTests/Event.Application.UnitTests.csproj --configuration Release --verbosity quiet -- --treenode-filter "/*/*/*ProjectionQueryAuthorizationMetadataTests*/*" --minimum-expected-tests 1 --no-progress --output Normal` ✅ 5/5; `dotnet build Explore.API/Explore.API.csproj --configuration Release --verbosity quiet` ✅; `dotnet test --project Event.Architecture.Tests/Event.Architecture.Tests.csproj --configuration Release --verbosity quiet -- --no-progress --output Normal` ✅ 142/142; targeted `git diff --check` ✅.
- Hardened custom-property definition stale-update behavior across shared, event runtime, and session runtime admin update flows. Detail DTOs now expose `ConcurrencyStamp`; update DTOs require `ExpectedConcurrencyStamp`; handlers compare the expected stamp before governance mapping or persistence and throw `ConcurrencyConflictException` with `concurrent_update` so API responses map to HTTP 409 instead of generic validation failures.
- Updated the shared Blazor admin detail model and update DTO builder to send the fetched stamp, and patched the generated client update payloads pending the next NSwag regeneration from OpenAPI.
- Added focused stale-write coverage for shared/event/session update handlers and missing-stamp validation coverage for the shared handler.
- Verification for concurrency slice: `dotnet test --project Event.Application.UnitTests/Event.Application.UnitTests.csproj --configuration Release --verbosity quiet -- --treenode-filter "/*/*/*UpdateCustomPropertyDefinitionCommandHandlerTests*/*|/*/*/*UpdateEventCustomPropertyDefinitionConcurrencyTests*/*|/*/*/*UpdateEventSessionCustomPropertyDefinitionConcurrencyTests*/*" --minimum-expected-tests 1 --no-progress --output Normal` ✅ 1089/1089; `dotnet build Explore.API/Explore.API.csproj --configuration Release --verbosity quiet` ✅; `dotnet build Explore.Blazor.Client/Explore.Blazor.Client.csproj --configuration Release --verbosity quiet` ✅; `dotnet test --project Explore.Blazor.Client.Tests/Explore.Blazor.Client.Tests.csproj --configuration Release --verbosity quiet -- --no-progress --output Normal` ✅ 968 total / 967 passed / 1 known skipped; `dotnet test --project Event.Architecture.Tests/Event.Architecture.Tests.csproj --configuration Release --verbosity quiet -- --no-progress --output Normal` ✅ 142/142.
- Hardened normal custom-property definition delete lifecycle across shared, event runtime, and session runtime repositories. Normal deletes now load definitions/options/values as tracked entities with the soft-delete filter ignored, retire definitions/options (`IsActive = false`, default cleared), and use tracked `Remove(...)` so the existing SaveChanges soft-delete/audit interceptor preserves historical rows instead of bypassing it with `ExecuteDeleteAsync`.
- Preserved existing event/session projection cleanup behavior in delete handlers: projection rows are still removed inside the unit-of-work transaction before repository-level definition retirement.
- Expanded repository lifecycle regression coverage in `Event.Persistence.IntegrationTests/Repositories/CustomPropertyOptionLifecycleRepositoryTests.cs` to prove shared/event/session deletes soft-delete definitions, options, and values while clearing active/default state.
- Verification for delete lifecycle slice: `dotnet test --project Event.Persistence.IntegrationTests/Event.Persistence.IntegrationTests.csproj --configuration Release --verbosity quiet -- --treenode-filter "/*/*/*CustomPropertyOptionLifecycleRepositoryTests*/*" --minimum-expected-tests 1 --no-progress --output Normal` ✅ 6/6; `dotnet build Explore.API/Explore.API.csproj --configuration Release --verbosity quiet` ✅; `dotnet test --project Event.Architecture.Tests/Event.Architecture.Tests.csproj --configuration Release --verbosity quiet -- --no-progress --output Normal` ✅ 142/142; targeted `git diff --check` ✅.
- Remaining Phase 12 gaps: shared org/group value writes were not wired because no Application/API write surface was found; export/moderation payload paths still need explicit exposure-ceiling proof; projection authorization/exposure, lifecycle, concurrency, and delete behavior still need Docker/Testcontainers PostgreSQL proof; explicit audited purge semantics and Docker/Testcontainers certification remain open.

### Phase 8.5.13 Projection Updater Prometheus Metrics — ✅ CLOSED 2026-05-02

- Extended the existing `Explore.Projections` meter rather than introducing a parallel EAV-specific meter family, preserving the repo's current OpenTelemetry/Prometheus registration path.
- Added `explore.projections.inline_updates_total` for inline updater operations that complete without dirty-scope deferral.
- Added `explore.projections.dirty_scope_skips_total` for inline operations deferred into the dirty-scope backlog, currently labelled with the bounded reason `rebuild_in_progress`.
- Kept metric tags low-cardinality: `tenant_id`, `projection_type`, `operation`, and `reason`; no definition IDs, event IDs, session IDs, namespace values, or custom-property keys are emitted.
- Wired counters into both event and event-session projection updaters for value and definition update paths.
- Verification: `dotnet build Event.Persistence.IntegrationTests/Event.Persistence.IntegrationTests.csproj --configuration Release --verbosity quiet` ✅; `dotnet test --project Event.Application.UnitTests/Event.Application.UnitTests.csproj --configuration Release --no-build --verbosity quiet` ✅ 1044/1044; `dotnet build Explore.API/Explore.API.csproj --configuration Release --verbosity quiet` ✅; `dotnet test --project Event.Architecture.Tests/Event.Architecture.Tests.csproj --configuration Release --verbosity quiet` ✅ 135/135.

### Phase 11.3 Multi-Value Semantics — ✅ LOCAL COVERAGE ADDED 2026-04-30

- Added service-level definition checks in event and session runtime value handlers so single-value definitions reject second rows (`Ordinal > 0` on single-value writes or multiple replacement values on bulk replacement) before persistence.
- Added shared normalized duplicate-value rejection for event and session bulk replacement plus single-value upsert handlers; text values compare case-insensitively after trimming, while option/number/bool/date values use typed identity keys and same-ordinal overwrites remain valid.
- Added handler tests in `Event.Application.UnitTests/Features/EventCustomProperties/Commands/SetEventCustomPropertyMultiValuesCommandHandlerTests.cs`, `SetEventCustomPropertyValueCommandHandlerTests.cs`, `Event.Application.UnitTests/Features/EventSessionCustomProperties/Commands/SetEventSessionCustomPropertyMultiValuesCommandHandlerTests.cs`, and `SetEventSessionCustomPropertyValueCommandHandlerTests.cs` proving ordinal assignment, input-order preservation, single-value rejection, and duplicate normalized-value rejection.
- Verification: LSP diagnostics clean for all modified handlers and new tests; `Event.Application.UnitTests` passes 1021/1021 after Oracle follow-up fixes.

### Phase 11.5 Exposure / Export / Moderation Flags — ✅ LOCAL COVERAGE ADDED 2026-04-30

- Added governance report coverage proving `ExposureLevel`, `IsSearchable`, `IsFilterable`, `IsExportable`, `IsModerationRelevant`, and `IsAnalyticsRelevant` pass from `GovernanceDefinitionRow` into `CustomPropertyGovernanceRowDto`.
- Added aggregate list coverage proving searchable facets honor `IsSearchable`, preserve public export/moderation metadata, and exclude internal exportable facets under a public exposure ceiling.
- Added aggregate detail coverage proving event and session public facets carry `IsExportable`/`IsModerationRelevant` while internal exportable facets are excluded from public payloads.
- Current implementation search found no separate custom-property export composer or moderation queue service in `Explore.Application`; this slice locks the local DTO/mapper boundaries where those flags currently flow.
- Verification: C# LSP clean for modified test files; `rtk git diff --check` clean; `Event.Application.UnitTests` passes 1029/1029.

### Phase 9.4 Template Selection — ✅ CLOSED 2026-04-29

- Wired `Explore.Blazor.Client/Pages/Events/CreateEvent.razor` and `.razor.cs` to load published/active event templates through `IEventTemplateService`, scope reloads by selected `EventTypeId`, clear stale template selections when the event type changes, and ignore stale async list/preview completions.
- Added optional `TemplateId` selection to the Event Options flow so submitting the existing generated `CreateEventDto.TemplateId` creates either a template-backed event or a vanilla event when empty; failed preview loads now clear `TemplateId`, and submit is blocked while preview is still in flight.
- Added a read-only template definition preview with loading, warning, description, definition count, property type, required, multi-value, and options-count chips; preview uses `aria-live="polite"` and a labelled region for assistive technology.
- Added BEM CSS for `create-event__template-*` preview layout and mobile stacking.
- Updated `Explore.Blazor.Client.Tests/Pages/Event/CreateEventTests.cs` and `Explore.Blazor.Client.Tests/GlobalUsings.cs` with a deterministic `IEventTemplateService` mock returning an empty page by default plus focused coverage for event-type scoped loads, preview loads, event-type change clearing, vanilla submit, selected-template submit, stale async completion guards, and in-flight preview submit blocking.
- Oracle review initially blocked handoff on async state races and insufficient coverage, then found one remaining submit race for in-flight preview loads; those blockers are now fixed in `CreateEvent.razor(.cs)` and covered by bUnit component-state tests.
- Verification: LSP diagnostics clean for `CreateEvent.razor.cs` and `CreateEventTests.cs` (`.razor` LSP unavailable locally); `dotnet build --configuration Release --verbosity quiet` ✅ 0 errors; `dotnet test --project Explore.Blazor.Client.Tests/Explore.Blazor.Client.Tests.csproj --configuration Release --verbosity quiet` ✅ 828 total / 827 passed / 1 known skipped.

### Phase 9.4A Session Blueprint Selection — ✅ CLOSED 2026-04-29

- Added typed Blazor client session-template service/model layer: `IEventSessionTemplateService`, `EventSessionTemplateService`, `Models/EventSessionTemplates/*`, HAL mapping extensions, DI registration, and test global usings.
- Wired `SessionEditorPanel.razor` so new sessions in the CreateEvent drawer can optionally choose a session blueprint after the parent event-template preview resolves. The selector is scoped by parent `EventTemplateId`, hidden for edit flows without a parent template, and keeps vanilla session creation when empty.
- Added read-only session blueprint preview with loading/warning/detail states, definition metadata chips, BEM CSS, and `aria-live`/labelled preview region.
- Added stale async protection: parent changes clear selected blueprint/list/detail; list and preview completions only apply when request version and current parent/selection still match; failed preview clears `SessionTemplateId`; drawer save is blocked while preview is loading.
- Fixed final Oracle blocker: parent event-template changes now clear `SessionTemplateId` on all already-added local CreateEvent sessions plus the open drawer model, so subsequent `CreateSessionAsync` payloads cannot submit a stale session blueprint from the old parent template.
- `SessionEditorModel.ToCreateDto()` now carries `SessionTemplateId` into the generated `CreateEventSessionDto`; update DTO remains unchanged because generated `UpdateEventSessionDto` has no session-template field.
- Tests added/updated: `SessionEditorPanelTests.cs` covers scoped load, parent clearing, preview failure, stale preview race, stale list race, and in-flight preview save blocking; `SessionEditorModelTests.cs` covers `SessionTemplateId` mapping; `SessionEditorWorkflowTests.cs` covers copy preservation; `CreateEventTests.cs` covers clearing stale local session `SessionTemplateId` values before submit.
- Verification: C# LSP diagnostics clean for touched C# files; Razor LSP unavailable locally but Release build compiles Razor; `dotnet build --configuration Release --verbosity quiet` ✅ 0 errors; `dotnet test --project Explore.Blazor.Client.Tests/Explore.Blazor.Client.Tests.csproj --configuration Release --verbosity quiet` ✅ 835 total / 834 passed / 1 known skipped.

### Phase 9.5/9.5A Runtime Editors — ✅ CLOSED 2026-04-29

- Implemented reusable dynamic field rendering in `Explore.Blazor.Client/Components/CustomProperties/CustomPropertyFieldEditor.razor` for Text, Number, Option, Boolean, DateTime, and Url property types.
- Added event and event-session runtime editors (`EventCustomPropertyRuntimeEditor.razor`, `EventSessionCustomPropertyRuntimeEditor.razor`) with runtime value loading/saving, multi-value cleanup/reordering, null-response failure handling, required single/multi validation, and load-error save guards.
- Added runtime value client service/contract/model and DI registration for `ICustomPropertyValueService`.
- Extended `ICustomPropertyDefinitionService`, `CustomPropertyDefinitionService`, and HAL mapping helpers to load event-local and session-local definition details from generated API endpoints.
- Wired event runtime custom properties into `EventEdit.razor` and persisted-session custom properties into `SessionEditorPanel.razor`.
- Fixed Oracle blockers: parameter-change reload prevents stale session drawer values; definition-load failures surface as visible alerts; runtime editors no longer nest child `MudForm`s inside parent edit forms.
- Verification: `dotnet build --configuration Release --verbosity quiet` ✅ 0 errors; `dotnet test --project Explore.Blazor.Client.Tests/Explore.Blazor.Client.Tests.csproj --configuration Release --verbosity quiet` ✅ 820 total / 819 passed / 1 known skipped. LSP unavailable locally (`csharp-ls` missing; no Razor LSP).
- Final Oracle review `bg_ffcde561` reported safe to hand back with no blocking issues. Non-blocking notes: custom-property saves remain separate from parent event/session saves; runtime definition detail 404s are treated as stale/missing definitions.

### Phase 9.11 NSwag Regeneration — ✅ CLOSED 2026-04-30

- Restored the local NSwag toolchain with `dotnet tool restore` (`nswag.consolecore` 14.6.3).
- Exported the governed OpenAPI snapshot through the targeted TUnit swagger exporter:
  `dotnet run --project "Event.API.IntegrationTests/Event.API.IntegrationTests.csproj" --configuration Release -- --treenode-filter "/*/*/*/SwaggerJson_Export_WritesPrettyPrintedDocToExploreApi" --minimum-expected-tests 1 --no-progress`.
- Regenerated `Explore.API/swagger.json` and `Explore.Blazor.Client/Clients/EventApiClient.g.cs` mechanically through the existing `Explore.Blazor.Client` NSwag build target; no generated client edits were hand-written.
- Generated diff summary: 168 insertions / 3 deletions, including `/sitemap.xml`, generated `GetSitemapAsync(...)`, generated `FileResponse : IDisposable`, and the refreshed role description text.
- Verified generated-client coverage still includes the expected custom-property projection, template, sync, and runtime DTO/method surface. No obvious aggregate-view endpoint is exposed under an `Aggregate` / `AggregateView` name in the current OpenAPI snapshot, so there was nothing aggregate-specific for NSwag to generate yet.
- Verification: targeted swagger export ✅; `rtk dotnet build "Explore.Blazor.Client/Explore.Blazor.Client.csproj" --configuration Release --verbosity quiet` ✅; `dotnet test --project Explore.Blazor.Client.Tests/Explore.Blazor.Client.Tests.csproj --configuration Release --verbosity quiet` ✅ 909 total / 908 passed / 1 known skipped.
- Full-solution `rtk dotnet build --configuration Release --verbosity quiet` was attempted and is not a Phase 9.11 blocker: it failed on unrelated existing analyzer/package issues plus a transient locked `Explore.Blazor.Client/obj/Release/net10.0/Explore.Blazor.Client.pdb` during static-web-assets fingerprinting.

### Phase 10.0 Layer 2/3 Boundary Verification — ✅ CLOSED 2026-04-30

- Added Phase 10.0 architecture guards in `Event.Architecture.Tests/ProjectionLayerBoundaryTests.cs`:
  - `EventQuerySpecification_ShouldCompose_Layer2Filters_SeparatelyFromLayer3ProjectionFilters` requires explicit `EventQuerySpecification.And(...)` overloads for `IslamicAspectFilter`, `TechAspectFilter`, `AspectPresenceFilter`, and `EventCustomPropertyProjectionFilter`.
  - `Layer3ProjectionFilters_ShouldNotExpose_Layer2SemanticFactories` blocks Layer 2 semantic factory names (`Islamic`, `Madhab`, `Gender`, `Prayer`, `Tech`, `Skill`, `Aspect`) on event/session custom-property projection filters.
- Tightened `docs/ARCHITECTURE.md` so Layer 2 typed filters compose directly while Layer 3 projection filters stay generic and custom properties that become sector-standard are promoted to typed Layer 2 schema.
- Verification: `lsp_diagnostics` clean for `ProjectionLayerBoundaryTests.cs`; `git diff --check` clean for the architecture test + architecture doc; both new TUnit guards pass with `--treenode-filter` and `--minimum-expected-tests 1`.
- Scope note: Phase 11 API roundtrip tests (`11.9`, `11.9B`) still require Docker/Testcontainers and remain locally blocked.

### Phase 11.2 Machine Identity Local Coverage — 🟡 LOCAL COVERAGE ADDED 2026-04-30

- Added `Event.Domain.UnitTests/CustomProperties/CustomPropertyGovernanceTests.cs` coverage proving namespace/key case and whitespace variants normalize to the same machine identity.
- Added `Event.Application.UnitTests/Features/CustomPropertyDefinitions/Commands/UpdateCustomPropertyDefinitionCommandHandlerTests.cs` coverage proving a `DisplayName` rename still uses normalized `Namespace + Key + current Id` for duplicate lookup and update identity.
- Verification: domain test LSP clean; application test LSP has warnings only; `git diff --check` clean for both test files; both targeted TUnit tests pass with `--treenode-filter` and `--minimum-expected-tests 1`.
- Scope note: EF uniqueness enforcement for the machine identity remains a Docker/Testcontainers PostgreSQL proof and stays pending.

---

## Final Verification Plan

Once backlog is resolved, run the canonical verification suite:

```bash
dotnet build --configuration Release --verbosity quiet
dotnet test --project Event.Application.UnitTests/Event.Application.UnitTests.csproj --configuration Release
dotnet test --project Event.Domain.UnitTests/Event.Domain.UnitTests.csproj --configuration Release
dotnet test --project Event.Architecture.Tests/Event.Architecture.Tests.csproj --configuration Release
dotnet test --project Explore.Secrets.UnitTests/Explore.Secrets.UnitTests.csproj --configuration Release
dotnet test --project Event.Persistence.IntegrationTests/Event.Persistence.IntegrationTests.csproj --configuration Release
dotnet test --project Event.API.IntegrationTests/Event.API.IntegrationTests.csproj --configuration Release
dotnet test --project Explore.Blazor.IntegrationTests/Explore.Blazor.IntegrationTests.csproj --configuration Release
dotnet test --project Explore.Blazor.Client.Tests/Explore.Blazor.Client.Tests.csproj --configuration Release
```

`Explore.Blazor.Client.E2ETests` requires running infrastructure (Aspire AppHost) and is not included in the standard pass.
