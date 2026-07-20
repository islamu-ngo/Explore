<!-- ABOUTME: Hot execution ledger for the workspace-shell (dynamic event management UI) workstream. -->
<!-- ABOUTME: Mirrors the plan's phases/tasks; implementation agents keep this current during work. -->

# Dynamic Event Management UI (Workspace Shell) — Task Checklist

Last Updated: 2026-07-21 Europe/Brussels

## Status Summary
- **Overall status:** Draft (awaiting user review)
- **Completed:** 0/24 implementation tasks (phase verification tracked separately)
- **Current priority:** User review of the plan; then Task 1.1
- **Next recommended slice:** Phase 1 — ADR-016 + workspace registry/classifier + AppWorkspaceRail

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

## Phase 1: Workspace Shell Foundation — Rail, Registry, ADR ⏳ NOT STARTED
- [ ] **1.1 ADR-016 workspace shell composition and vocabulary**
  - **Files:** new `docs/adr/ADR-016-workspace-shell-composition.md`
  - **Acceptance:** ADR (Accepted) records D1–D3 + glossary matching code terms
  - **Effort:** S | **Dependencies:** —
- [ ] **1.2 Workspace registry, classifier, and shell state**
  - **Files:** new `src/Explore.Blazor.Client/Services/Shell/{WorkspaceKey,WorkspaceDescriptor,IWorkspaceRegistry,WorkspaceRegistry,WorkspaceRouteClassifier,UiShellState}.cs`; modify `Extensions/ServiceCollectionExtensions.cs`
  - **Acceptance:** table-driven classifier test maps every `Routes.razor` route to expected workspace; last-route map restores query strings
  - **Effort:** M | **Dependencies:** —
- [ ] **1.3 `AppWorkspaceRail` + MainLayout shell track**
  - **Files:** new `Components/Shell/AppWorkspaceRail.razor(.cs/.css)`; modify `Layout/MainLayout.razor(.cs/.css)`, `tests/Event.Architecture.Tests/DockLayoutArchitectureTests.cs`; new bUnit tests
  - **Acceptance:** rail on all MainLayout routes, never on SetupLayout; `aria-current`/tooltips/focus; Settings at block-end; docks + EventList workspace unchanged at 1280px; logical CSS only
  - **Effort:** L | **Dependencies:** 1.2

### Phase 1 Verification — RUN ONCE AFTER ALL PHASE TASKS
- [ ] `dotnet build --configuration Release --verbosity quiet`
- [ ] `dotnet test --project tests/Explore.Blazor.Client.Tests/Explore.Blazor.Client.Tests.csproj --configuration Release --verbosity quiet`

## Phase 2: Contextual Workspace Navigation ⏳ NOT STARTED
- [ ] **2.1 Navigation provider contract + `WorkspaceNavigationHost`**
  - **Files:** new `Contracts/Services/Shell/IWorkspaceNavigationProvider.cs`, `Components/Shell/WorkspaceNavigationHost.razor(.cs/.css)`; modify `WorkspaceDescriptor`, `MainLayout.razor.cs`
  - **Acceptance:** workspace switch swaps nav content without re-registration; no-nav workspace leaves full-width canvas; overlay header owned by host
  - **Effort:** M | **Dependencies:** 1.2, 1.3
- [ ] **2.2 Rename `shell.left-nav`→`shell.workspace-nav`; `AppSideNav`→`EventsWorkspaceNavigation`**
  - **Files:** modify `ShellDockPanels.cs`; new `Components/Shell/Workspaces/EventsWorkspaceNavigation.razor(.cs/.css)`; delete `AppSideNav.razor(.cs/.css)`; update client dock/nav tests
  - **Acceptance:** zero `shell.left-nav`/`AppSideNav` refs outside dev docs; Events nav content parity (org-centric branch + quick links)
  - **Effort:** M | **Dependencies:** 2.1
- [ ] **2.3 Delete `SidebarState`; migrate consumers; update DOCK_LAYOUT/BLAZOR docs**
  - **Files:** delete `Services/SidebarState.cs`; modify `NavMenu.razor(.cs)`, `MainLayout.razor.cs`, `TenantAdminSettingsLayout.razor`, `InstanceAdminSettingsLayout.razor`, `ServiceCollectionExtensions.cs`, affected tests, `docs/DOCK_LAYOUT.md`, `docs/BLAZOR.md`; add minimal `SettingsWorkspaceNavigation` placeholder
  - **Acceptance:** zero `SidebarState` refs; obsolete bridge tests deleted (not skipped); docs match shipped panel catalog; `AiAssistantState` untouched
  - **Effort:** M | **Dependencies:** 2.2

### Phase 2 Verification — RUN ONCE AFTER ALL PHASE TASKS
- [ ] `dotnet build --configuration Release --verbosity quiet`
- [ ] `dotnet test --project tests/Event.Architecture.Tests/Event.Architecture.Tests.csproj --configuration Release --verbosity quiet`

## Phase 3: Server-Authoritative UI Shell Context ⏳ NOT STARTED
- [ ] **3.1 Application query + DTO + handler (+ group-publisher investigation)**
  - **Files:** new `src/Explore.Application/Features/UiShell/Requests/Queries/GetUiShellContextRequest.cs`, `Handlers/Queries/GetUiShellContextRequestHandler.cs`, `DTOs/UiShell/UiShellContextDto.cs` (+ `ManagedActorDto`, `SettingsScopeDto`, `WorkspaceAvailabilityDto`); unit tests in `Event.Application.UnitTests`
  - **Acceptance:** report §6 principal scenarios covered (instance-admin-only ⇒ no Studio/no tenant scope; multi-role union; org-centric pinned actor); composes existing authority sources without cross-feature reach-ins; group-publisher finding recorded in context
  - **Effort:** L | **Dependencies:** —
- [ ] **3.2 API controller + `RouteNames.GetUiShellContext` + OpenAPI/NSwag regen (discrete commit)**
  - **Files:** new `src/Explore.API/Controllers/UiShellController.cs`; modify `src/Explore.API/Hateoas/RouteNames.cs`, `docs/API.md`, `docs/API_CHANGELOG.md`; regenerate `schemas/openapi_islamu-event.json` + `Clients/EventApiClient.g.cs`
  - **Acceptance:** contract/classification architecture tests pass; generated `UiShell_GetContextAsync` exists with no banned names; changelog entry added; regen isolated in its own commit (webhook workstream coordination noted)
  - **Effort:** M | **Dependencies:** 3.1
- [ ] **3.3 Client `IUiShellContextService` + rail/nav gating + revocation fallback**
  - **Files:** new `Contracts/Services/Shell/IUiShellContextService.cs`, `Services/Shell/UiShellContextService.cs`; modify `WorkspaceRegistry`, `AppWorkspaceRail`, `NavMenu.razor.cs`, `UiShellState`; bUnit tests
  - **Acceptance:** anonymous never calls the authenticated endpoint; Studio item appears only per server data; revoked stored workspace falls back to Events; NavMenu menu gating consumes shell context instead of the four ad-hoc authority loads
  - **Effort:** M | **Dependencies:** 3.2

### Phase 3 Verification — RUN ONCE AFTER ALL PHASE TASKS
- [ ] `dotnet build --configuration Release --verbosity quiet`
- [ ] `dotnet test --project tests/Event.Application.UnitTests/Event.Application.UnitTests.csproj --configuration Release --verbosity quiet`

## Phase 4: Studio Workspace ⏳ NOT STARTED
- [ ] **4.1 Studio routes + workspace registration + actor-level navigation**
  - **Files:** modify `Routes.razor`, `WorkspaceRegistry`; new `Components/Shell/Workspaces/StudioWorkspaceNavigation.razor(.cs/.css)`, `StudioActorSwitcher.razor(.cs/.css)`
  - **Acceptance:** `/studio`, `/studio/events`, `/studio/events/:eventId(+sections)` guarded routes; switcher lists only shell-context actors; pinned mode for `public_experience.primary_organization_id`; no dead nav sections
  - **Effort:** L | **Dependencies:** —
- [ ] **4.2 Studio dashboard + events list (reuses `GetMyEvents` HAL collection)**
  - **Files:** new `Pages/Studio/StudioDashboard.razor(.cs/.css)`, `Pages/Studio/StudioEvents.razor(.cs/.css)`, `IStudioEventsService`(+impl); bUnit tests
  - **Acceptance:** HAL-gated row affordances (fabricated `_links` variants tested); eligible-only empty-state Create
  - **Effort:** L | **Dependencies:** 4.1
- [ ] **4.3 Event-level navigation shell (HAL-driven, replaces actor nav content)**
  - **Files:** new `Pages/Studio/StudioEventShell.razor(.cs/.css)`, `Components/Shell/Workspaces/StudioEventNavigation.razor(.cs/.css)`; bUnit tests
  - **Acceptance:** section visibility flips with `_links` (table-driven test); back-link returns to actor level; `rg "IsInRole" src/Explore.Blazor.Client/Pages/Studio` empty; links target existing editors (`/events/:id/edit`, session pages)
  - **Effort:** L | **Dependencies:** 4.1, 4.2
- [ ] **4.4 Workspace-aware top bar (search + primary action + acting-actor hint)**
  - **Files:** modify `Layout/NavMenu.razor(.cs)`; bUnit tests
  - **Acceptance:** per-workspace search/action matrix tests pass (Events/Studio/AI/Settings behaviors per plan §6 Phase 4)
  - **Effort:** M | **Dependencies:** 4.1

### Phase 4 Verification — RUN ONCE AFTER ALL PHASE TASKS
- [ ] `dotnet build --configuration Release --verbosity quiet`
- [ ] `dotnet test --project tests/Explore.Blazor.Client.Tests/Explore.Blazor.Client.Tests.csproj --configuration Release --verbosity quiet`

## Phase 5: AI Dual Experience ⏳ NOT STARTED
- [ ] **5.1 Extract shared conversation components from `AiAssistantRail`**
  - **Files:** modify `Components/Shell/AiAssistantRail.razor(.css)`; new/verified shared components under `Components/Shell/AiAssistant/`
  - **Acceptance:** existing full-panel `AiAssistantRail` bUnit suite green unchanged; extracted components rail-agnostic
  - **Effort:** L | **Dependencies:** —
- [ ] **5.2 `/ai` workspace pages + `AiWorkspaceNavigation` + open-in-workspace**
  - **Files:** new `Pages/Ai/AiWorkspace.razor(.cs/.css)`, `Pages/Ai/AiConversationPage.razor(.cs/.css)`, `Components/Shell/Workspaces/AiWorkspaceNavigation.razor(.cs/.css)`; modify `Routes.razor`, `WorkspaceRegistry`, `AiAssistantRail` header, `NavMenu` (sparkle unchanged)
  - **Acceptance:** dock↔workspace share one `AiAssistantConversationState` (identical history, no drift); HAL-gated confirm/reject in both surfaces; anonymous access matches `ai_assistant.allow_anonymous_access`; product label "AI Assistant" (model id only in info popover)
  - **Effort:** L | **Dependencies:** 5.1

### Phase 5 Verification — RUN ONCE AFTER ALL PHASE TASKS
- [ ] `dotnet build --configuration Release --verbosity quiet`
- [ ] `dotnet test --project tests/Explore.Blazor.Client.Tests/Explore.Blazor.Client.Tests.csproj --configuration Release --verbosity quiet`

## Phase 6: Settings Workspace Hub ⏳ NOT STARTED
- [ ] **6.1 Scope-aware `SettingsWorkspaceNavigation` (scopes from shell context)**
  - **Files:** modify `Components/Shell/Workspaces/SettingsWorkspaceNavigation.razor(.cs/.css)`; bUnit tests
  - **Acceptance:** Personal always; org/group/tenant/instance per authority with names; instance-admin-only ⇒ Personal + Instance only
  - **Effort:** M | **Dependencies:** —
- [ ] **6.2 Canonical `/settings/**` routes + link migration + old-route deletion**
  - **Files:** modify `Routes.razor` (add `/settings/tenant`, `/settings/instance`, `/settings/organization/:organizationId`, `/settings/group/:groupId`, `/settings/tenant/navigation`; delete old `/admin/*/settings` + `/admin/tenant/navigation` rows); update every internal link producer; update guard tests
  - **Acceptance:** `rg` sweep for old paths returns zero stale refs; guards enforce identical access on new paths
  - **Effort:** M | **Dependencies:** 6.1
- [ ] **6.3 Single-tenant "Site administration" composition**
  - **Files:** modify `SettingsWorkspaceNavigation`; bUnit table-driven test (deployment mode × authority)
  - **Acceptance:** single-tenant + dual authority ⇒ grouped "Site administration"; multi-tenant ⇒ separate Tenant/Instance scopes
  - **Effort:** S | **Dependencies:** 6.1

### Phase 6 Verification — RUN ONCE AFTER ALL PHASE TASKS
- [ ] `dotnet build --configuration Release --verbosity quiet`
- [ ] `dotnet test --project tests/Explore.Blazor.Client.Tests/Explore.Blazor.Client.Tests.csproj --configuration Release --verbosity quiet`

## Phase 7: Durable Layout Preferences + Tenant Shell Governance ⏳ NOT STARTED
- [ ] **7.1 Governance keys + `SettingRegistry` definitions + shell-context wiring**
  - **Files:** modify `src/Explore.Domain/Constants/GovernanceSettingKeys.cs`, `src/Explore.Domain/Settings/SettingRegistry.cs` (+ definitions file per existing layout), `GetUiShellContextRequestHandler`, public-experience shell handler (org-centric rail visibility); modify `docs/CONFIGURATION.md`; handler tests
  - **Acceptance:** `ui_shell.*` (tenant/instance, lockable) + `ui_shell_preferences.*` (user-only) registered with explicit scopes; registry parity tests green; lock + single-tenant bypass proven for one representative key
  - **Effort:** M | **Dependencies:** —
- [ ] **7.2 `ServerBackedDockLayoutPersistence` + last workspace/actor persistence**
  - **Files:** new `Services/Interop/ServerBackedDockLayoutPersistence.cs`, `Services/Shell/ShellPreferencesService.cs`; modify DI, `MainLayout.razor.cs` hydrate path, `UiShellState`/`StudioActorSwitcher`; unit tests
  - **Acceptance:** authenticated cross-device restore via `api/settings/user/{category}` batch (debounced, never per pointer event); anonymous stays local; promote-on-login; revoked actor/workspace pruned; `UserAction`/`Reset`-only autosave preserved; `allowUserOverride=false` skips nav-mode persistence
  - **Effort:** L | **Dependencies:** 7.1
- [ ] **7.3 Tenant admin "Shell" settings section**
  - **Files:** modify tenant settings page components (`Pages/Admin/Tenant/**`); bUnit tests
  - **Acceptance:** D8 controls rendered with `EffectiveSettingDto.CanEdit`/`Reason` (no client role checks); locked ⇒ read-only with reason
  - **Effort:** M | **Dependencies:** 7.1

### Phase 7 Verification — RUN ONCE AFTER ALL PHASE TASKS
- [ ] `dotnet build --configuration Release --verbosity quiet`
- [ ] `dotnet test --project tests/Event.API.IntegrationTests/Event.API.IntegrationTests.csproj --configuration Release --verbosity quiet`

## Phase 8: Responsive, RTL, Accessibility Hardening + Scenario Matrix ⏳ NOT STARTED
- [ ] **8.1 Mobile bottom navigation + per-workspace canvas floors**
  - **Files:** modify `AppWorkspaceRail.razor(.css)`, `MainLayout.razor.css`, `Services/Docking/DockLayoutState.cs`; extend `DockLayoutStateTests`; bUnit bottom-nav tests
  - **Acceptance:** `Xs` ⇒ bottom nav (availability-filtered), no start track; floors Events 375 / AI 520 / Settings 560 / Studio 720 applied via projection only (never persisted)
  - **Effort:** L | **Dependencies:** —
- [ ] **8.2 Focus/landmark/RTL polish**
  - **Files:** modify `WorkspaceNavigationHost`, nav providers, `UiShellState` (title/focus on switch), `:dir(rtl)` overrides where needed; a11y architecture tests
  - **Acceptance:** distinct `aria-label` per nav; focus moves to `h1`/main on workspace switch; architecture a11y + logical-CSS tests green
  - **Effort:** M | **Dependencies:** 8.1
- [ ] **8.3 Scenario-matrix suite + docs sync**
  - **Files:** new table-driven bUnit suite in `Explore.Blazor.Client.Tests` (Profile × Auth × Capabilities × Workspace × Viewport ⇒ rail items, nav content, settings scopes, default route, revocation fallback); modify `docs/DOCK_LAYOUT.md` QA matrix, `docs/ACCESSIBILITY.md`
  - **Acceptance:** matrix rows match plan/report §6 for implemented scenarios; docs reflect shipped behavior
  - **Effort:** L | **Dependencies:** 8.1, 8.2

### Phase 8 Verification — RUN ONCE AFTER ALL PHASE TASKS
- [ ] `dotnet build --configuration Release --verbosity quiet`
- [ ] `dotnet test --project tests/Explore.Blazor.Client.Tests/Explore.Blazor.Client.Tests.csproj --configuration Release --verbosity quiet`

## Remaining / Deferred Work
- **Tenant navigation item-override model + editor previews** (report §13) — deferred; trigger: tenant demand for reordering/hiding core items after Phase 8; owner: future plan.
- **Studio operational features** (attendees/check-in/tickets/communications/analytics implementations) — deferred; Phase 4 ships shells + HAL-gated links; owner: per-feature workstreams.
- **"My submissions" reported-listing surface** (report §9) — deferred; trigger: reported-listing UX prioritization.
- **`BackofficeOnly` experience profile** — deferred; no requirement (report §4A).
- **Admin/Control workspace** (moderation queues, webhook ops in rail) — deferred; registry supports adding it later; trigger: operational surface growth.
