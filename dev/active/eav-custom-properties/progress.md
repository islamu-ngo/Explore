<!-- ABOUTME: Live execution progress log for Milestones E + F + cleanup (EAV custom properties). -->
<!-- ABOUTME: Updated iteratively as delegations complete. Paired with eav-custom-properties-tasks.md. -->

# EAV Custom Properties — Execution Progress

**Last updated:** 2026-04-29 (Session 5 — Phase 9.4A Oracle blocker fixed + verified)
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
| Cleanup | Phase 9.11 NSwag regen | ⏳ PENDING | — |
| Cleanup | Phase 11+10.0 Architecture tests | ⏳ PENDING | — |
| Cleanup | Phase 8.5.13 Prometheus metrics | ⏳ PENDING | — |

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
2. **Phase 9.11** — Regenerate NSwag API clients after accumulated API surface changes and verify client compile/test compatibility.
3. **Phase 11 + 10.0** — Add architecture tests + API roundtrip tests for the custom-property/template flows.
4. **Phase 8.5.13** — Add Prometheus metrics for projection updater observability.
5. **Phase 9.6** — Template preview admin overview for event templates.
6. **Phase 9.6A** — Session blueprint preview admin overview.
7. **Phase 9.10** — Organization/Group page cleanup.

### Recommended Next Execution Slice

**Next implementation target:** Phase 9.11 NSwag regeneration or Phase 9.6/9.6A preview admin overviews.

- Phase 9.4A is closed for CreateEvent's new-session drawer. EventEdit session blueprint selection remains deferred because current event read/update DTOs do not expose the parent event template identity.
- Current verification baseline: C# diagnostics clean for touched files; `dotnet build --configuration Release --verbosity quiet` ✅ 0 errors; `dotnet test --project Explore.Blazor.Client.Tests/Explore.Blazor.Client.Tests.csproj --configuration Release --verbosity quiet` ✅ 835 total / 834 passed / 1 known skipped.

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
