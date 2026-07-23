<!-- ABOUTME: Hot execution ledger for the workspace-shell (dynamic event management UI) workstream. -->
<!-- ABOUTME: Mirrors the plan's phases/tasks; implementation agents keep this current during work. -->

# Dynamic Event Management UI (Workspace Shell) — Task Checklist

Last Updated: 2026-07-23 Europe/Brussels

## Status Summary
- **Overall status:** Implementation 29/30 complete; final authenticated browser QA blocked
- **Completed:** 29/30 implementation tasks (Phases 0–8 and Tasks 9.1–9.3 complete; phase verification tracked separately)
- **Current priority:** Task 10.1 final visual/browser scenario matrix — blocked by unavailable authenticated identities/authorities
- **Next recommended slice:** Resume Task 10.1 when usable local seeker/organizer/tenant-admin/instance-admin identities and verified authorities are available

## Implementation Maintenance Rules
- Read the full workstream once at initial implementation start; on resume, read context/tasks first and only relevant plan sections.
- Do not reread unchanged artifacts after every task.
- Mark a substantial task 🟡 IN PROGRESS when it spans multiple edits or a handoff; skip that churn for tiny tasks completed immediately.
- Check a substantial completed task immediately; reconcile small completed tasks no later than phase end.
- Add discovered work under the owning phase with acceptance criteria and dependencies; keep counts/priority/date accurate.
- Check a phase complete only after all implementation AND phase-verification checkboxes pass.
- Update context after a phase, decision, blocker, validation failure, material discovery, or handoff.
- Update the plan only when scope, architecture, sequencing, acceptance criteria, risk, or validation strategy changes.
- Do not run build/tests after individual tasks; verify once at phase end.
- Do not start the app, browser, Docker, Aspire, Playwright, Chrome DevTools MCP, or live services for verification.

## Phase 0: Re-baseline and Governance Foundation 🟡 VERIFICATION BLOCKED
- [x] **0.1 Re-baseline workstream against repository state**
  - **Files:** verification-only (no runtime changes)
  - **Acceptance:** all evidence claims re-verified; `/admin/tenant/navigation` confirmed as `@page` directive; `GetManagedEventsByActorAsync` confirmed in generated client
  - **Effort:** S | **Dependencies:** —
- [x] **0.2 Governance setting definitions registration**
  - **Files:** modify `src/Explore.Domain/Constants/GovernanceSettingKeys.cs`, `src/Explore.Domain/Settings/SettingRegistry.cs` (+ new `UiShellSettingDefinitions.cs` or equivalent per existing layout); modify `docs/CONFIGURATION.md`
  - **Acceptance:** `ui_shell.*` (tenant/instance, lockable) + `ui_shell_preferences.*` (user-only) registered with explicit allowed scopes and defaults; registry parity tests green; lock and single-tenant bypass semantics verifiable for one representative key; failing-first test asserts a representative key is present in `SettingRegistry` before implementation; `docs/CONFIGURATION.md` documents every new key
  - **Effort:** M | **Dependencies:** 0.1

### Phase 0 Verification — RUN ONCE AFTER ALL PHASE TASKS
- [x] `dotnet build --configuration Release --verbosity quiet`
- [ ] `dotnet test --project tests/Event.Architecture.Tests/Event.Architecture.Tests.csproj --configuration Release --verbosity quiet`
  - **Blocked by 4 unrelated existing failures (2026-07-21):** `CurrentSchema_MustRetireLegacyDecentralizationColumnsAndSettingRows`, `AllLinkPoliciesHaveExplicitPermissionActions`, `Repositories_ShouldEndWith_Repository`, and `DomainMustNotIntroduce_OrganizationCentricScopeEntityFiles`. The new focused `UiShellSettingsShouldRegisterExplicitGovernanceAndPreferenceScopes` test passes (1/1).

## Phase 1: Workspace Shell Foundation — Rail, Registry, ADR 🟡 VERIFICATION BLOCKED
- [x] **1.1 ADR-019 workspace shell composition and vocabulary**
  - **Files:** new `docs/adr/ADR-019-workspace-shell-composition.md`
  - **Acceptance:** ADR (Accepted) records D1–D3 + glossary matching code terms
  - **Effort:** S | **Dependencies:** —
- [x] **1.2 Workspace registry, classifier, and shell state**
  - **Files:** new `src/Explore.Blazor.Client/Services/Shell/{WorkspaceKey,WorkspaceDescriptor,IWorkspaceRegistry,WorkspaceRegistry,WorkspaceRouteClassifier,UiShellState}.cs`; modify `Extensions/ServiceCollectionExtensions.cs`
  - **Acceptance:** table-driven classifier test maps every `Routes.razor` route to expected workspace; last-route map restores query strings
  - **Effort:** M | **Dependencies:** —
- [x] **1.3 `AppWorkspaceRail` + MainLayout shell track + minimal mobile bottom navigation**
  - **Files:** new `Components/Shell/AppWorkspaceRail.razor(.cs/.css)`; modify `Layout/MainLayout.razor(.cs/.css)`, `Components/Docking/DockSideHost.razor.css`, `src/Explore.Blazor/wwwroot/css/components.css`, `tests/Event.Architecture.Tests/DockLayoutArchitectureTests.cs`; new bUnit tests
  - **Acceptance:** rail on all MainLayout routes, never on SetupLayout; `aria-current`/tooltips/focus; Settings at block-end; shared logical Start inset prevents overlap with docked nav; logical CSS only; bUnit proves availability filtering and architecture CSS-contract tests prove the `Xs` bottom projection; rendered breakpoint evidence remains in Phase 9
  - **Effort:** L | **Dependencies:** 1.1, 1.2

### Phase 1 Verification — RUN ONCE AFTER ALL PHASE TASKS
- [x] `dotnet build --configuration Release --verbosity quiet`
- [ ] `dotnet test --project tests/Explore.Blazor.Client.Tests/Explore.Blazor.Client.Tests.csproj --configuration Release --verbosity quiet`
  - **Blocked by 3 unrelated existing failures (2026-07-21):** `GeneratedClient_DoesNotEmitUntypedHalEmbeddedItemCollections` and two `ReportEventDialogTests` (`SubmitAsync_WhenValidationErrorRepeats_RendersOneAssertiveAlertPath`, `Render_CommunicationChoicesAreIndependentUncheckedAndDescribed`). Focused workspace rail, MainLayout, route/state, and dock architecture tests pass.

## Phase 2: Contextual Workspace Navigation 🟡 VERIFICATION BLOCKED
- [x] **2.1 Navigation provider contract + `WorkspaceNavigationHost`**
  - **Files:** new `Contracts/Services/Shell/IWorkspaceNavigationProvider.cs`, `Components/Shell/WorkspaceNavigationHost.razor(.cs/.css)`; modify `WorkspaceDescriptor`, `MainLayout.razor.cs`
  - **Acceptance:** workspace switch swaps nav content without re-registration; no-nav workspace leaves full-width canvas; overlay header owned by host; persisted/user-close state is preserved across no-provider transitions and hydration
  - **Validation:** host 8/8, panel lifecycle/hydration 5/5, AppSideNav parity 5/5, dock architecture 5/5; Release build passed with zero errors (pre-existing repository warnings remain)
  - **Effort:** M | **Dependencies:** 1.2, 1.3
- [x] **2.2 Rename `shell.left-nav`→`shell.workspace-nav`; `AppSideNav`→`EventsWorkspaceNavigation`**
  - **Files:** modify `ShellDockPanels.cs`; new `Components/Shell/Workspaces/EventsWorkspaceNavigation.razor(.cs/.css)`; delete `AppSideNav.razor(.cs/.css)`; update client dock/nav tests
  - **Acceptance:** zero `shell.left-nav`/`AppSideNav` refs outside dev docs; Events nav content parity (org-centric branch + quick links)
  - **Validation:** seven focused Blazor client suites pass 149/149; stale `shell.left-nav` snapshot regression passes; client project compiles through those runs. Release/architecture gates are temporarily blocked by a concurrently edited ATProto workstream and remain unchecked until Phase 2 verification.
  - **Effort:** M | **Dependencies:** 2.1
- [x] **2.3 Delete `SidebarState`; migrate consumers; update DOCK_LAYOUT/BLAZOR docs**
  - **Files:** delete `Services/SidebarState.cs`; modify `NavMenu.razor(.cs)`, `MainLayout.razor.cs`, `TenantAdminSettingsLayout.razor`, `InstanceAdminSettingsLayout.razor`, `ServiceCollectionExtensions.cs`, affected tests, `docs/DOCK_LAYOUT.md`, `docs/BLAZOR.md`; add minimal `SettingsWorkspaceNavigation` placeholder
  - **Acceptance:** zero `SidebarState` refs; obsolete bridge tests deleted (not skipped); docs match shipped panel catalog; `AiAssistantState` untouched
  - **Validation:** affected client suites pass 105/105 and cached `DockLayoutArchitectureTests` pass 5/5; `SidebarState` remains only in dev migration/history notes. Oracle review found and the implementation fixed policy-driven workspace-nav persistence: hidden/no-provider closes no longer autosave, hidden-route round trips preserve explicit user-close intent, and pending user autosaves capture pre-policy state. Oracle follow-up passed the corrected state. Repository-wide Release/architecture rebuild remains blocked by the separately owned ATProto workstream.
  - **Effort:** M | **Dependencies:** 2.2

### Phase 2 Verification — RUN ONCE AFTER ALL PHASE TASKS
- [x] `dotnet build --configuration Release --verbosity quiet`
  - **Validation:** passed on 2026-07-22 with 0 errors. One concurrent-output retry failed with missing Release DLL/PDB copy artifacts; the immediate clean retry passed without source changes.
- [ ] `dotnet test --project tests/Event.Architecture.Tests/Event.Architecture.Tests.csproj --configuration Release --verbosity quiet`

## Phase 3: Server-Authoritative UI Shell Context 🟡 VERIFICATION BLOCKED
- [x] **3.1 Application query + DTO + handler (group-publisher resolved; managed actors reuse `IAiAssistantActorContextService`)**
  - **Files:** new `src/Explore.Application/Features/UiShell/Requests/Queries/GetUiShellContextRequest.cs`, `Handlers/Queries/GetUiShellContextRequestHandler.cs`, `DTOs/UiShell/UiShellContextDto.cs` (+ `ManagedActorDto`, `SettingsScopeDto`, `WorkspaceAvailabilityDto`); unit tests in `Event.Application.UnitTests`
  - **Acceptance:** report §6 principal scenarios covered (instance-admin-only ⇒ no Studio/no tenant scope; multi-role union; org-centric pinned actor); composes existing authority sources without cross-feature reach-ins; `IAiAssistantActorContextService` is the single source of managed actors; group-publisher finding recorded in context; no HybridCache introduced; failing-first test asserts `StudioWorkspaceAvailability = false` for instance-admin-only principal
  - **Validation:** UI-shell handler tests pass 10/10, including the eight report scenarios, fail-closed missing-settings behavior, and explicit org-admin settings without Studio; actor-context tests pass 4/4 including end-to-end CancellationToken forwarding; focused CQRS/Clean Architecture/naming checks pass 5/5; `Explore.Persistence` and `Explore.Infrastructure` production builds pass with 0 warnings/errors; Release build passes; `git diff --check` clean. Oracle found and verified fixes for settings-scope/managed-actor coupling and dropped repository cancellation. The tenant-filtered `AdminContext` lane now passes 18/18, including cross-tenant exclusion.
  - **Effort:** L | **Dependencies:** 0.2
- [x] **3.2 API controller + `RouteNames.GetUiShellContext` + OpenAPI/NSwag regen (exact method name verified after regen)**
  - **Files:** new `src/Explore.API/Controllers/UiShellController.cs`; modify `src/Explore.API/Hateoas/RouteNames.cs`, `docs/API.md`, `docs/API_CHANGELOG.md`; regenerate `schemas/openapi_islamu-event.json` + `Clients/EventApiClient.g.cs`
  - **Acceptance:** contract/classification architecture tests pass; generated method name verified after regen with no banned names; changelog entry added; generated-artifact diff serialized and isolated; dirty-file hunk preservation controls explicit (unrelated dirty files not included); never hand-edit generated files
  - **Validation:** documented single-threaded API build completed with 0 errors; NSwag generation completed successfully; OpenAPI operationId is `GetUiShellContext`; generated client method is `GetUiShellContextAsync`; generated diff is isolated to 186 OpenAPI lines and 254 client lines; API/classification/client architecture lane passed 10 with 1 governed existing skip; Oracle passed the endpoint and generated contract with high confidence.
  - **Effort:** M | **Dependencies:** 3.1
- [x] **3.3 Client `IUiShellContextService` + rail/nav gating + revocation fallback**
  - **Files:** new `Contracts/Services/Shell/IUiShellContextService.cs`, `Services/Shell/UiShellContextService.cs`; modify `WorkspaceRegistry`, `AppWorkspaceRail`, `NavMenu.razor.cs`, `UiShellState`; bUnit tests
  - **Acceptance:** anonymous never calls the authenticated endpoint; Studio item appears only per server data; revoked stored workspace falls back to Events; NavMenu menu gating consumes shell context instead of the four ad-hoc authority loads
  - **Validation:** service tests pass 5/5, shell-context rail tests 4/4, revocation tests 3/3, and NavMenu admin/context tests 18/18. The service uses the generated `GetUiShellContextAsync` contract, rechecks authentication before cached reads, caches for five minutes, and invalidates on `CurrentUserState.OnChanged`. Managed actors populate membership lists while matching `SettingsScopes` independently gate settings actions. The full client suite passes 1891 tests with 3 unrelated failures already recorded under Phase 1. Oracle passed the generated contract, cache/auth boundary, Studio gating, revocation behavior, scope separation, DI placement, and focused coverage with high confidence and no required fixes.
  - **Effort:** M | **Dependencies:** 3.2

### Phase 3 Verification — RUN ONCE AFTER ALL PHASE TASKS
- [x] `dotnet build --configuration Release --verbosity quiet`
- [ ] `dotnet test --project tests/Event.Application.UnitTests/Event.Application.UnitTests.csproj --configuration Release --verbosity quiet`
  - **Blocked by 2 unrelated failures (2026-07-22):** `UpdateEventLocationPolicyCommandHandlerTests.ValidPolicyWriteCommitsAggregateAndAuditBeforeEvictingProjectionTags` and `BusinessMetricsEmailDispatchTests.RecordEmailDispatchOperationalSignalsUsesOnlyBoundedSafeTags`. The suite passes 2928/2930; the focused UI-shell handler lane passes 10/10.

## Phase 4: Studio Workspace 🟡 VERIFICATION BLOCKED
- [x] **4.1 Studio routes + workspace registration + actor-level navigation**
  - **Files:** modify `Routes.razor`, `WorkspaceRegistry`, `UiShellState`; new `Components/Shell/Workspaces/StudioWorkspaceNavigation.razor(.css)`, `StudioActorSwitcher.razor(.cs/.css)`, `Pages/Studio/StudioHome.razor(.css)`, `StudioEvents.razor(.css)`; focused bUnit/TUnit tests
  - **Acceptance:** authenticated `/studio` and `/studio/events` routes; deep-linked Events item exposes active state; switcher lists only shell-context actors; authorized pinned and single-actor modes are read-only; only Overview/Events render, with event-detail routes deferred to 4.3
  - **Validation:** route/state/switcher/navigation/page lanes pass 34/34; Release build passes with 0 errors; `git diff --check` clean; Oracle PASS with no required fixes
  - **Effort:** L | **Dependencies:** —
- [x] **4.2 Studio dashboard + events list (actor-scoped via `GetManagedEventsByActorAsync`)**
  - **Files:** modify `IEventService`/`EventService`, `HalResourceExtensions`, `Pages/Studio/StudioHome.razor(.css)`, `StudioEvents.razor(.css)`; add `StudioEventPageBase.cs`; focused service/page tests
  - **Acceptance:** HAL-gated row affordances (fabricated `_links` variants tested); empty Create requires eligibility plus collection HAL; strict managed endpoint uses actor ID; personal fallback uses my-events; create uses existing `/events/create` picker
  - **Validation:** failing-first 10 red/2 baseline green; final Studio 12/12, EventService 69/69, switcher 3/3, navigation host 9/9; client build 0 warnings/errors; diff/authority/RTL sweeps clean; GPT Oracle PASS
  - **Effort:** L | **Dependencies:** 4.1
- [x] **4.3 Event-level navigation shell (HAL-driven, replaces actor nav content)**
  - **Files:** new `Pages/Studio/StudioEventShell.razor(.cs/.css)`, `Components/Shell/Workspaces/StudioEventNavigation.razor(.cs/.css)`; bUnit tests
  - **Acceptance:** section visibility flips with `_links` (table-driven test); back-link returns to actor level; `rg "IsInRole" src/Explore.Blazor.Client/Pages/Studio` empty; links target existing editors (`/events/:id/edit`, session pages); group events use existing `/events/create` picker (no new group-specific route)
  - **Validation:** failing-first compile proof captured; final navigation 8/8, shared state 1/1, workspace host 10/10, routes 17/17, and Studio regression 12/12 pass; client Release build 0 warnings/errors; diff/authority/RTL sweeps clean; GPT-5.5 Oracle PASS
  - **Effort:** L | **Dependencies:** 4.1, 4.2
- [x] **4.4 Workspace-aware top bar (search + primary action + acting-actor hint)**
  - **Files:** modify `Layout/NavMenu.razor(.cs)`; bUnit tests
  - **Acceptance:** per-workspace search/action matrix tests pass (Events/Studio/AI/Settings behaviors per plan §6 Phase 4)
  - **Validation:** valid red matrix discovered 4 tests with Events baseline green and 3 expected failures; final workspace matrix 5/5 and existing admin/context regression 18/18 pass; client Release build 0 warnings/errors; scoped diff/diagnostics clean; GPT-5.5 Oracle found and verified anonymous-workspace and bidi fixes, then returned PASS
  - **Effort:** M | **Dependencies:** 4.1

### Phase 4 Verification — RUN ONCE AFTER ALL PHASE TASKS
- [x] `dotnet build --configuration Release --verbosity quiet`
  - **Validation:** passed with 0 errors and 1,035 pre-existing repository warnings; the directly changed client project remains 0 warnings/errors.
- [ ] `dotnet test --project tests/Explore.Blazor.Client.Tests/Explore.Blazor.Client.Tests.csproj --configuration Release --verbosity quiet`
  - **Blocked by the same 3 unrelated failures already recorded under Phase 1:** `GeneratedClient_DoesNotEmitUntypedHalEmbeddedItemCollections`, `ReportEventDialogTests.Render_CommunicationChoicesAreIndependentUncheckedAndDescribed`, and `ReportEventDialogTests.SubmitAsync_WhenValidationErrorRepeats_RendersOneAssertiveAlertPath`. Current result: 1,932 total, 1,928 passed, 3 failed, 1 governed skip; all Task 4.1–4.4 focused lanes pass.

## Phase 5: AI Dual Experience 🟡 VERIFICATION BLOCKED
- [x] **5.1 Extract shared conversation components from `AiAssistantRail`**
  - **Files:** modify `Components/Shell/AiAssistantRail.razor(.css)`; new/verified shared components under `Components/Shell/AiAssistant/`
  - **Acceptance:** existing full-panel `AiAssistantRail` bUnit suite green unchanged; extracted components rail-agnostic
  - **Validation:** `AiAssistantRailTests` 21/21, timeline 1/1, composer 2/2, proposed-action 3/3, reference-picker 2/2, action-result 7/7, Blazor architecture 17/17; canonical Release build 0 errors; CSS ownership/ID/authority/diff sweeps clean; GPT-5.5 Oracle final PASS after resolving native boolean binding, host-unique IDs, and CSS-isolation ownership/deep-selector findings
  - **Effort:** L | **Dependencies:** —
- [x] **5.2 `/ai` workspace pages + `AiWorkspaceNavigation` + open-in-workspace (authenticated-only)**
  - **Files:** new `Pages/Ai/AiWorkspace.razor(.css)` and `Components/Shell/Workspaces/AiWorkspaceNavigation.razor(.css)`; modify `Routes.razor`, `WorkspaceRegistry`, `AiAssistantRail`, shared AI state, and `MainLayout`
  - **Acceptance:** dock↔workspace share one `AiAssistantConversationState` (identical history, no drift); HAL-gated confirm/reject in both surfaces; `/ai` routes reject anonymous users (guard test); anonymous AI access remains dock-only; product label "AI Assistant" (model id only in info popover)
  - **Validation:** authenticated routes/classifier 18/18, AI rail 27/27, workspace navigation host 11/11, app workspace rail 8/8, MainLayout 34/34, shared timeline 1/1, composer 2/2, and Blazor architecture 17/17 pass; canonical Release build has 0 errors; late-policy and reentrant HAL-load tests fail before their fixes and pass after; GPT-5.5 Oracle final PASS
  - **Effort:** L | **Dependencies:** 5.1

### Phase 5 Verification — RUN ONCE AFTER ALL PHASE TASKS
- [x] `dotnet build --configuration Release --verbosity quiet`
- [ ] `dotnet test --project tests/Explore.Blazor.Client.Tests/Explore.Blazor.Client.Tests.csproj --configuration Release --verbosity quiet`
  - **Blocked by the same 3 unrelated failures already recorded under Phase 1:** generated EventLocation HAL collection typing and two `ReportEventDialogTests`. Current result: 1,947 total, 1,943 passed, 3 failed, 1 governed skip; all Task 5.1–5.2 focused lanes pass.

## Phase 6: Hybrid Settings Architecture 🟡 VERIFICATION BLOCKED
- [x] **6.1 Contextual Personal Settings state and provider removal**
  - **Files:** modify `UiShellState`, `WorkspaceRegistry`, `WorkspaceNavigationHost`, `AppWorkspaceRail`, and focused tests; delete `SettingsWorkspaceNavigation.razor(.cs/.css)`
  - **Acceptance:** in-session Explore/Studio/AI → Personal retains source workspace/navigation; direct Personal load/refresh activates dedicated Settings; leaving clears origin; one `aria-current`; no Settings provider remains; no return URL input/persistence
  - **Validation:** state 10/10, revocation 5/5, app rail 9/9, navigation host 11/11; failing-first absent-property proof; scoped source/CSS/diff checks clean; direct verification disclosure recorded because further agents are prohibited
  - **Effort:** M | **Dependencies:** —
- [x] **6.2 Canonical hub/personal/admin routes and page composition**
  - **Files:** modify `Routes.razor`, Personal Settings page/layout/CSS, existing guarded admin settings pages/layouts, and tests; add shared `SettingsScopeSelector`; remove old admin settings rows and `/admin/tenant/navigation` directive
- **Acceptance:** `/settings`, `/settings/personal`, `/settings/personal/:section`, `/settings/organization/:id`, `/settings/group/:id`, `/settings/admin`, `/settings/instance` work with existing guards; selector is server-scope-driven; Personal uses compact path sections; admin layouts retain their own sidebars; stale-route sweep is zero
  - **Validation:** failing-first client run had 5 expected Task 6.2 failures plus the 3 known unrelated failures; final client run passes 1,957/1,961 with only those 3 unrelated failures and 1 governed skip; architecture passes 281/286 with the same 4 unrelated blockers and 1 governed skip; Release build 0 errors; diff/stale-route sweeps clean; direct verification disclosure recorded because further agents are prohibited
  - **Effort:** L | **Dependencies:** 6.1
- [x] **6.3 One-gear/profile entry points and authorized scope presentation**
  - **Files:** modify `AppWorkspaceRail`, `NavMenu`, hub/selector, CSS, and authority/deployment matrix tests
  - **Acceptance:** one bottom gear; primary always `/settings/personal`; gear menu + profile dropdown retain hub and authorized scope access; contextual-open differs from active; instance-only never renders Tenant; single-tenant dual authority presents Site administration without merging guards/APIs
  - **Validation:** failing-first client run had 5 expected Task 6.3 failures plus the 3 known unrelated failures; final client run passes 1,960/1,964 with only those 3 unrelated failures and 1 governed skip; authority/CSS/diff sweeps clean; direct verification disclosure recorded because further agents are prohibited
  - **Effort:** M | **Dependencies:** 6.1, 6.2

### Phase 6 Verification — RUN ONCE AFTER ALL PHASE TASKS
- [x] `dotnet build --configuration Release --verbosity quiet`
- [ ] `dotnet test --project tests/Explore.Blazor.Client.Tests/Explore.Blazor.Client.Tests.csproj --configuration Release --verbosity quiet`
  - **Blocked by the same 3 unrelated failures recorded under Phase 1:** generated EventLocation HAL collection typing and two `ReportEventDialogTests`. Current result: 1,964 total, 1,960 passed, 3 failed, 1 governed skip; all Task 6.1–6.3 behavior passes.

## Phase 7: Durable Layout Preferences + Tenant Shell Governance 🟡 IN PROGRESS
- [x] **7.1 Shell-context wiring + public-experience governance resolution**
  - **Files:** modify `GetUiShellContextRequestHandler`, public-experience shell handler (org-centric rail visibility); modify `docs/CONFIGURATION.md`; handler tests
  - **Acceptance:** retained D8 keys wired into handler (`navigationDefaults`, `allowUserOverride`, `organizerDefaultWorkspace`, `railPublicVisibility`); unused `ui_shell.default_nav_mode.settings` definition/docs removed; lock + single-tenant bypass proven for one representative key; `docs/CONFIGURATION.md` documents every retained key
  - **Validation:** failing-first Application compile failed on absent `RailPublicVisibility`; focused handler tests pass 11/11; focused governance registry tests pass 9/9; Release build passes with 0 errors; OpenAPI/generated client parity and stale-key/diff sweeps are clean; full suites retain only unrelated recorded blockers
  - **Effort:** M | **Dependencies:** 0.2, 3.1
- [x] **7.2 Server-backed dock persistence + last workspace/actor/settings scope (tenant-discriminated anonymous storage)**
  - **Files:** new `Services/Interop/ServerBackedDockLayoutPersistence.cs`, `Services/Shell/ShellPreferencesService.cs`; modify DI, `MainLayout.razor.cs` hydrate path, `UiShellState`/`StudioActorSwitcher`; unit tests
  - **Acceptance:** authenticated cross-device restore via `api/settings/user/{category}` batch (debounced, never per pointer event); anonymous storage uses tenant discriminator (`dock_layout:v1:{tenantSlug}:`) with no old-key compatibility read; promote-on-login; revoked actor/workspace/settings scope pruned; `ui.settings.last_scope.v1` affects only the dedicated hub/selector and never the gear Personal target; Personal origin is never persisted; `UserAction`/`Reset`-only autosave preserved; `allowUserOverride=false` skips nav-mode persistence
  - **Validation:** failing-first coverage established absent persistence/preference contracts; focused Task 7.2 client tests pass 4/4, extended affected client fixtures pass 6/6, and focused client architecture/governance tests pass 17/17; Release build passes with 0 errors; `git diff --check` and production stale-key sweeps are clean; full client and Architecture suites retain only the 3 and 4 unrelated recorded failures respectively
  - **Effort:** L | **Dependencies:** 7.1
- [x] **7.3 Tenant admin "Shell" settings section**
  - **Files:** modify tenant settings page components (`Pages/Admin/Tenant/**`); bUnit tests
  - **Acceptance:** D8 controls rendered with `EffectiveSettingDto.CanEdit`/`Reason` (no client role checks); locked ⇒ read-only with reason
  - **Validation:** failing-first client compile failed on the absent `ITenantShellSettingsService`; focused component tests pass 2/2, affected tenant-settings tests pass 2/2, focused Blazor architecture/governance tests pass 17/17, and Release build passes with 0 errors; source sweep finds no role/claim authority checks
  - **Effort:** M | **Dependencies:** 7.1

### Phase 7 Verification — RUN ONCE AFTER ALL PHASE TASKS
- [x] `dotnet build --configuration Release --verbosity quiet`
- [ ] `dotnet test --project tests/Event.API.IntegrationTests/Event.API.IntegrationTests.csproj --configuration Release --verbosity quiet`
  - **Blocked by environment and unrelated baselines:** 1,899 total, 1,384 passed, 514 failed, 1 skipped; at least 500 failures are `DockerUnavailableException` because Testcontainers cannot reach `/var/run/docker.sock`, with the remaining visible failures in previously unrelated EventLocation privacy, email health, and ATProto routes. Task 7.3 focused client/architecture lanes are green.

## Phase 8: Responsive, RTL, Accessibility Hardening + Scenario Matrix 🟡 IN PROGRESS
- [x] **8.1 Mobile bottom navigation + generic workspace canvas floors**
  - **Files:** modify `AppWorkspaceRail.razor(.css)`, `MainLayout.razor.css`; extend `DockLayoutStateTests`; bUnit bottom-nav tests
  - **Acceptance:** `Xs` ⇒ bottom nav (availability-filtered), no start track; generic floor hint (Events 375 / AI 520 / Settings 560 / Studio 720) applied via projection only (never persisted); `DockLayoutState` contains zero workspace-specific branching
  - **Validation:** existing Xs rail characterization passes 9/9; failing-first compile proved absent floor overload/mapping; final affected `DockLayoutStateTests`/`MainLayoutTests`/`AppWorkspaceRailTests` lane passes 33/33; per-scope floors are caller-supplied, snapshot-equivalent, and classified `ViewportPolicy`
  - **Effort:** L | **Dependencies:** —
- [x] **8.2 Focus/landmark/RTL polish**
  - **Files:** modify `WorkspaceNavigationHost`, nav providers, `UiShellState` (title/focus on switch), `:dir(rtl)` overrides where needed; a11y architecture tests
  - **Acceptance:** distinct `aria-label` per nav; focus moves to `h1`/main on workspace switch; architecture a11y + logical-CSS tests green
  - **Validation:** failing-first host test received the duplicate `Sidebar navigation` label; final workspace host passes 11/11, MainLayout focus/landmark behavior passes 39/39, and `AccessibilityConventionTests` passes 8/8. Events now exposes `Events workspace navigation`; no RTL override or broader shell change was needed because the architecture scan confirms logical CSS.
  - **Effort:** M | **Dependencies:** 8.1
- [x] **8.3 Scenario-matrix suite + docs sync**
  - **Files:** new table-driven bUnit suite in `Explore.Blazor.Client.Tests` (Profile × Auth × Capabilities × Workspace × Viewport ⇒ rail items, nav content, settings scopes, default route, revocation fallback); modify `docs/DOCK_LAYOUT.md` QA matrix, `docs/ACCESSIBILITY.md`
  - **Acceptance:** matrix rows match plan/report §6 for implemented scenarios; docs reflect shipped behavior
  - **Validation:** the nine-row bUnit matrix passes 1/1 and covers Discovery/OrganizationCentric profiles, anonymous/authenticated users, seeker/organizer/tenant-admin/instance-admin/multi-role capabilities, mobile/desktop semantic rail rows, provider content, authorized Settings links, durable defaults, and revocation fallback. Its failing-first run exposed alphabetical AI-first rail ordering; `AppWorkspaceRail` now preserves canonical `WorkspaceRegistry` order. `DOCK_LAYOUT`, `ACCESSIBILITY`, and `BLAZOR` now describe the shipped behavior.
  - **Effort:** L | **Dependencies:** 8.1, 8.2

### Phase 8 Verification — RUN ONCE AFTER ALL PHASE TASKS
- [x] `dotnet build --configuration Release --verbosity quiet`
  - **Validation:** passed on 2026-07-22 with 0 errors and 536 pre-existing warnings.
- [ ] `dotnet test --project tests/Explore.Blazor.Client.Tests/Explore.Blazor.Client.Tests.csproj --configuration Release --verbosity quiet`
  - **Blocked by the same 3 unrelated failures recorded under Phase 1:** generated EventLocation HAL collection typing and two `ReportEventDialogTests`. Current result: 1,983 total, 1,979 passed, 3 failed, 1 governed skip; the Task 8.3 matrix passes.

## Phase 9: Personal Settings Entry Parity And Information Architecture 🟡 IMPLEMENTATION COMPLETE / VERIFICATION BLOCKED
- [x] **9.1 Explicit Personal Settings navigation contract and entry-point parity**
  - **Files:** `UiShellState.cs`, `AppWorkspaceRail.razor(.cs)`, `NavMenu.razor(.cs)`, `ThemeQuickSwitcher.razor`, `EventRegistration.razor`, focused shell/entry tests
  - **Acceptance:** profile, rail, theme, and connected-app entries share one capture-before-navigation contract; live Events/Studio/AI origins persist; direct/new-tab loads remain dedicated; no return URL or durable origin
  - **Validation:** failing profile-vs-rail interaction reproduced the defect; final state 18/18, navigation 7/7, and registration 5/5 focused tests pass; changed-file diagnostics/diff checks are clean
  - **Effort:** M | **Dependencies:** 6.1
- [x] **9.2 View all, section registry, search, and conditional scope selector**
  - **Files:** `Settings.razor`, `SettingsLayout.razor(.css)`, `SettingsScopeSelector.razor(.css)`, section components as needed, focused Settings tests
  - **Acceptance:** `/settings/personal` defaults to View all; all nine sections share one metadata registry; focused routes render one section; search filters/announces results; Personal-only selector is absent; invalid slugs fall back to View all without aliases; composed render has no duplicate IDs/h1
  - **Validation:** focused `SettingsLayoutTests` pass 7/7 and `SettingsScopeSelectorTests` pass 4/4; the View all render exposes nine ordered sections with unique IDs and one page `h1`; search and empty-state announcements are covered; changed-file diagnostics and `git diff --check` are clean
  - **Effort:** L | **Dependencies:** 9.1
- [x] **9.3 Sticky responsive vertical navigation and documentation sync**
  - **Files:** Settings scoped CSS/tests plus `docs/BLAZOR.md`, `docs/ACCESSIBILITY.md`, `docs/DOCK_LAYOUT.md`
  - **Acceptance:** desktop sticky vertical nav sits below navbar; narrow layout stacks without overflow; focus/DOM order, logical CSS, reduced motion, distinct nav labels, and docs are correct
  - **Validation:** token-driven `minmax(11rem, 15rem) minmax(0, 1fr)` desktop grid; navbar-offset stickiness; non-sticky stacked projection below `59.997em`; focused Settings layout 8/8, scope selector 4/4, affected shell entries 11/11, and accessibility conventions 8/8 pass; BLAZOR/ACCESSIBILITY/DOCK_LAYOUT synchronized
  - **Follow-up validation (2026-07-23):** live Personal section changes now re-render through `RouterStateService.OnRouteChanged`; UI-shell scoped dependencies are awaited sequentially to avoid shared `DbContext` overlap; tenant administration is canonical at `/settings/admin` with no alias; server-authorized profile precedence selects instance before tenant; compact scope selectors sit beside all admin headings. Focused application tests pass 12/12, focused client tests pass 21/21, and the Release build passes with zero errors. The full client run passes 2,015/2,020 with four unrelated failures and one governed skip.
  - **Effort:** M | **Dependencies:** 9.2

### Phase 9 Verification — RUN ONCE AFTER ALL PHASE TASKS
- [x] `dotnet build --configuration Release --verbosity quiet`
  - **Validation:** passed with 0 errors and 544 existing warnings.
- [ ] `dotnet test --project tests/Explore.Blazor.Client.Tests/Explore.Blazor.Client.Tests.csproj --configuration Release --verbosity quiet`
  - **Blocked by the same 3 unrelated failures recorded under Phase 1:** generated EventLocation HAL collection typing and two `ReportEventDialogTests`. Current result: 2,016 total, 2,012 passed, 3 failed, 1 governed skip; all Task 9.1–9.3 focused lanes pass.

## Phase 10: Final Visual/Browser QA Gate 🟡 BLOCKED
- [ ] **10.1 Final visual/browser scenario-matrix walkthrough**
  - **Files:** all shell/Settings surfaces and `docs/DOCK_LAYOUT.md` QA evidence
  - **Acceptance:** remaining authenticated personas plus revised Settings Personal-only/admin-scope states are evidenced at 320/375/768/1280/1920; console, focus, RTL, sticky/stacked, and overflow findings are recorded honestly
  - **Blocker evidence:** `local-lite` reports `isAuthenticated=false`, and `/settings/personal` redirects to `/`. The anonymous root is console-clean and had no horizontal overflow at 1760px, but no usable authenticated persona identities or verified authorities are available; deterministic tests and unchanged anonymous screenshots are not accepted as revised Settings browser proof.
  - **Effort:** M | **Dependencies:** 9.3

### Phase 10 Verification
- [ ] Manual browser walkthrough documented in `docs/DOCK_LAYOUT.md` QA matrix
- [ ] `dotnet build --configuration Release --verbosity quiet`
- [ ] `dotnet test --project tests/Explore.Blazor.Client.Tests/Explore.Blazor.Client.Tests.csproj --configuration Release --verbosity quiet`

## Remaining / Deferred Work
- **Tenant navigation item-override model + editor previews** (report §13) — deferred; trigger: tenant demand for reordering/hiding core items after Phase 8; owner: future plan.
- **Studio operational features** (attendees/check-in/tickets/communications/analytics implementations) — deferred; Phase 4 ships shells + HAL-gated links; owner: per-feature workstreams.
- **"My submissions" reported-listing surface** (report §9) — deferred; trigger: reported-listing UX prioritization.
- **`BackofficeOnly` experience profile** — deferred; no requirement (report §4A).
- **Admin/Control workspace** (moderation queues, webhook ops in rail) — deferred; registry supports adding it later; trigger: operational surface growth.
