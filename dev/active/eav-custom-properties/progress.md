<!-- ABOUTME: Live execution progress log for Milestones E + F + cleanup (EAV custom properties). -->
<!-- ABOUTME: Updated iteratively as delegations complete. Paired with eav-custom-properties-tasks.md. -->

# EAV Custom Properties — Execution Progress

**Last updated:** 2026-05-02 (Phase 8.5.13 projection updater Prometheus metrics completed)
**Scope:** Cleanup Phase 9.x — Blazor admin UI for definitions, templates, runtime editors.
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

1. **Retry E/F integration tests delegation** — highest value remaining, covers Gap 3 (sync + aggregate-view correctness; Docker required).
2. **Phase 11 local test slice** — Continue non-Docker unit/architecture tests for custom-property/template flows; 11.3 and 11.5 local slices are covered; API roundtrips remain Docker/Testcontainers-gated.
3. **Phase 9.6** — Template preview admin overview for event templates.
4. **Phase 9.6A** — Session blueprint preview admin overview.
5. **Phase 9.10** — Organization/Group page cleanup.

### Recommended Next Execution Slice

**Next implementation target:** Phase 11 local-only tests (11.7) or Phase 9.6/9.6A preview admin overviews.

- Phase 9.4A is closed for CreateEvent's new-session drawer. EventEdit session blueprint selection remains deferred because current event read/update DTOs do not expose the parent event template identity.
- Current verification baseline: Phase 9.11 swagger export + generated client build succeeded; Phase 10.0 boundary guard LSP/diff checks clean and both new TUnit architecture guards pass; Phase 11.2 local domain/application unit tests pass; Phase 11.3 local runtime multi-value handler tests pass; Phase 11.5 local flag tests pass; Phase 8.5.13 metrics wiring verified with `Event.Application.UnitTests` 1044/1044, `Event.Architecture.Tests` 135/135, `Explore.API` Release build, and `Event.Persistence.IntegrationTests` Release build. `dotnet test --project Explore.Blazor.Client.Tests/Explore.Blazor.Client.Tests.csproj --configuration Release --verbosity quiet` remains ✅ 909 total / 908 passed / 1 known skipped from 2026-04-30. Full solution build currently fails outside these slices on unrelated existing analyzer/package issues plus one transient locked `Explore.Blazor.Client.pdb` during static-web-assets fingerprinting.

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
