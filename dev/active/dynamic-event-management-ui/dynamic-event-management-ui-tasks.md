<!-- ABOUTME: Hot execution ledger for the workspace-shell (dynamic event management UI) workstream. -->
<!-- ABOUTME: Mirrors the plan's phases/tasks; implementation agents keep this current during work. -->

# Dynamic Event Management UI (Workspace Shell) — Task Checklist

Last Updated: 2026-07-21 Europe/Brussels (re-baselined and approved)

## Status Summary
- **Overall status:** Approved / Implementation started
- **Completed:** 8/27 implementation tasks (Phase 0 implementation, Phase 1 Tasks 1.1–1.3, and Phase 2 Tasks 2.1–2.3 done; phase verification tracked separately)
- **Current priority:** Phase 2 implementation review and deferred Release/architecture verification; Task 3.1 is next
- **Next recommended slice:** Begin the server-authoritative UI shell context application query while the separately owned ATProto workstream restores the repository-wide build gate

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
- [ ] `dotnet build --configuration Release --verbosity quiet`
- [ ] `dotnet test --project tests/Event.Architecture.Tests/Event.Architecture.Tests.csproj --configuration Release --verbosity quiet`

## Phase 3: Server-Authoritative UI Shell Context ⏳ NOT STARTED
- [ ] **3.1 Application query + DTO + handler (group-publisher resolved; managed actors reuse `IAiAssistantActorContextService`)**
  - **Files:** new `src/Explore.Application/Features/UiShell/Requests/Queries/GetUiShellContextRequest.cs`, `Handlers/Queries/GetUiShellContextRequestHandler.cs`, `DTOs/UiShell/UiShellContextDto.cs` (+ `ManagedActorDto`, `SettingsScopeDto`, `WorkspaceAvailabilityDto`); unit tests in `Event.Application.UnitTests`
  - **Acceptance:** report §6 principal scenarios covered (instance-admin-only ⇒ no Studio/no tenant scope; multi-role union; org-centric pinned actor); composes existing authority sources without cross-feature reach-ins; `IAiAssistantActorContextService` is the single source of managed actors; group-publisher finding recorded in context; no HybridCache introduced; failing-first test asserts `StudioWorkspaceAvailability = false` for instance-admin-only principal
  - **Effort:** L | **Dependencies:** 0.2
- [ ] **3.2 API controller + `RouteNames.GetUiShellContext` + OpenAPI/NSwag regen (exact method name verified after regen)**
  - **Files:** new `src/Explore.API/Controllers/UiShellController.cs`; modify `src/Explore.API/Hateoas/RouteNames.cs`, `docs/API.md`, `docs/API_CHANGELOG.md`; regenerate `schemas/openapi_islamu-event.json` + `Clients/EventApiClient.g.cs`
  - **Acceptance:** contract/classification architecture tests pass; generated method name verified after regen with no banned names; changelog entry added; generated-artifact diff serialized and isolated; dirty-file hunk preservation controls explicit (unrelated dirty files not included); never hand-edit generated files
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
- [ ] **4.2 Studio dashboard + events list (actor-scoped via `GetManagedEventsByActorAsync`)**
  - **Files:** new `Pages/Studio/StudioDashboard.razor(.cs/.css)`, `Pages/Studio/StudioEvents.razor(.cs/.css)`, `IStudioEventsService`(+impl); bUnit tests
  - **Acceptance:** HAL-gated row affordances (fabricated `_links` variants tested); eligible-only empty-state Create; `GetManagedEventsByActorAsync` used for org/group actors; `GetMyEventsAsync` used for personal/unscoped fallback; create uses existing `/events/create` publisher picker
  - **Effort:** L | **Dependencies:** 4.1
- [ ] **4.3 Event-level navigation shell (HAL-driven, replaces actor nav content)**
  - **Files:** new `Pages/Studio/StudioEventShell.razor(.cs/.css)`, `Components/Shell/Workspaces/StudioEventNavigation.razor(.cs/.css)`; bUnit tests
  - **Acceptance:** section visibility flips with `_links` (table-driven test); back-link returns to actor level; `rg "IsInRole" src/Explore.Blazor.Client/Pages/Studio` empty; links target existing editors (`/events/:id/edit`, session pages); group events use existing `/events/create` picker (no new group-specific route)
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
- [ ] **5.2 `/ai` workspace pages + `AiWorkspaceNavigation` + open-in-workspace (authenticated-only)**
  - **Files:** new `Pages/Ai/AiWorkspace.razor(.cs/.css)`, `Pages/Ai/AiConversationPage.razor(.cs/.css)`, `Components/Shell/Workspaces/AiWorkspaceNavigation.razor(.cs/.css)`; modify `Routes.razor`, `WorkspaceRegistry`, `AiAssistantRail` header, `NavMenu` (sparkle unchanged)
  - **Acceptance:** dock↔workspace share one `AiAssistantConversationState` (identical history, no drift); HAL-gated confirm/reject in both surfaces; `/ai` routes reject anonymous users (guard test); anonymous AI access remains dock-only; product label "AI Assistant" (model id only in info popover)
  - **Effort:** L | **Dependencies:** 5.1

### Phase 5 Verification — RUN ONCE AFTER ALL PHASE TASKS
- [ ] `dotnet build --configuration Release --verbosity quiet`
- [ ] `dotnet test --project tests/Explore.Blazor.Client.Tests/Explore.Blazor.Client.Tests.csproj --configuration Release --verbosity quiet`

## Phase 6: Settings Workspace Hub ⏳ NOT STARTED
- [ ] **6.1 Scope-aware `SettingsWorkspaceNavigation` (scopes from shell context)**
  - **Files:** modify `Components/Shell/Workspaces/SettingsWorkspaceNavigation.razor(.cs/.css)`; bUnit tests
  - **Acceptance:** Personal always; org/group/tenant/instance per authority with names; instance-admin-only ⇒ Personal + Instance only
  - **Effort:** M | **Dependencies:** —
- [ ] **6.2 Canonical `/settings/**` routes + link migration + old-route/`@page` removal**
  - **Files:** modify `Routes.razor` (add `/settings/tenant`, `/settings/instance`, `/settings/organization/:organizationId`, `/settings/group/:groupId`, `/settings/tenant/navigation`; delete old `/admin/*/settings` rows); remove old `@page "/admin/tenant/navigation"` directive from `Pages/Admin/Tenant/Navigation.razor` (was never in `Routes.razor`); update every internal link producer; update guard tests
  - **Acceptance:** `rg` sweep for old paths returns zero stale refs; guards enforce identical access on new paths; `@page` directive removed
  - **Effort:** M | **Dependencies:** 6.1
- [ ] **6.3 Single-tenant "Site administration" composition**
  - **Files:** modify `SettingsWorkspaceNavigation`; bUnit table-driven test (deployment mode × authority)
  - **Acceptance:** single-tenant + dual authority ⇒ grouped "Site administration"; multi-tenant ⇒ separate Tenant/Instance scopes
  - **Effort:** S | **Dependencies:** 6.1

### Phase 6 Verification — RUN ONCE AFTER ALL PHASE TASKS
- [ ] `dotnet build --configuration Release --verbosity quiet`
- [ ] `dotnet test --project tests/Explore.Blazor.Client.Tests/Explore.Blazor.Client.Tests.csproj --configuration Release --verbosity quiet`

## Phase 7: Durable Layout Preferences + Tenant Shell Governance ⏳ NOT STARTED
- [ ] **7.1 Shell-context wiring + public-experience governance resolution**
  - **Files:** modify `GetUiShellContextRequestHandler`, public-experience shell handler (org-centric rail visibility); modify `docs/CONFIGURATION.md`; handler tests
  - **Acceptance:** registered D8 keys wired into handler (`navigationDefaults`, `allowUserOverride`, `organizerDefaultWorkspace`, `railPublicVisibility`); lock + single-tenant bypass proven for one representative key; `docs/CONFIGURATION.md` documents every new key
  - **Effort:** M | **Dependencies:** 0.2, 3.1
- [ ] **7.2 `ServerBackedDockLayoutPersistence` + last workspace/actor persistence (tenant-discriminated anonymous storage)**
  - **Files:** new `Services/Interop/ServerBackedDockLayoutPersistence.cs`, `Services/Shell/ShellPreferencesService.cs`; modify DI, `MainLayout.razor.cs` hydrate path, `UiShellState`/`StudioActorSwitcher`; unit tests
  - **Acceptance:** authenticated cross-device restore via `api/settings/user/{category}` batch (debounced, never per pointer event); anonymous storage uses tenant discriminator (`dock_layout:v1:{tenantSlug}:`) with no old-key compatibility read; promote-on-login; revoked actor/workspace pruned; `UserAction`/`Reset`-only autosave preserved; `allowUserOverride=false` skips nav-mode persistence
  - **Effort:** L | **Dependencies:** 7.1
- [ ] **7.3 Tenant admin "Shell" settings section**
  - **Files:** modify tenant settings page components (`Pages/Admin/Tenant/**`); bUnit tests
  - **Acceptance:** D8 controls rendered with `EffectiveSettingDto.CanEdit`/`Reason` (no client role checks); locked ⇒ read-only with reason
  - **Effort:** M | **Dependencies:** 7.1

### Phase 7 Verification — RUN ONCE AFTER ALL PHASE TASKS
- [ ] `dotnet build --configuration Release --verbosity quiet`
- [ ] `dotnet test --project tests/Event.API.IntegrationTests/Event.API.IntegrationTests.csproj --configuration Release --verbosity quiet`

## Phase 8: Responsive, RTL, Accessibility Hardening + Scenario Matrix ⏳ NOT STARTED
- [ ] **8.1 Mobile bottom navigation + generic workspace canvas floors**
  - **Files:** modify `AppWorkspaceRail.razor(.css)`, `MainLayout.razor.css`; extend `DockLayoutStateTests`; bUnit bottom-nav tests
  - **Acceptance:** `Xs` ⇒ bottom nav (availability-filtered), no start track; generic floor hint (Events 375 / AI 520 / Settings 560 / Studio 720) applied via projection only (never persisted); `DockLayoutState` contains zero workspace-specific branching
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

## Phase 9: Final Visual/Browser QA Gate ⏳ NOT STARTED
- [ ] **9.1 Visual and browser walkthrough of the scenario matrix**
  - **Files:** all shell components, `MainLayout`, `AppWorkspaceRail`, workspace navigation providers, Studio/AI/Settings pages
  - **Acceptance:** rail renders correctly on desktop and mobile for anonymous, authenticated seeker, organizer, tenant-admin, and instance-admin principals; workspace switches animate smoothly; bottom nav is reachable and functional; no layout breakage at 320px, 768px, 1280px, 1920px; focus order is logical; no console errors on workspace navigation; deterministic per-phase tests remain primary, this gate is independent final verification
  - **Effort:** M | **Dependencies:** 1.3, 8.3

### Phase 9 Verification
- [ ] Manual browser walkthrough documented in `docs/DOCK_LAYOUT.md` QA matrix
- [ ] `dotnet build --configuration Release --verbosity quiet`
- [ ] `dotnet test --project tests/Explore.Blazor.Client.Tests/Explore.Blazor.Client.Tests.csproj --configuration Release --verbosity quiet`

## Remaining / Deferred Work
- **Tenant navigation item-override model + editor previews** (report §13) — deferred; trigger: tenant demand for reordering/hiding core items after Phase 8; owner: future plan.
- **Studio operational features** (attendees/check-in/tickets/communications/analytics implementations) — deferred; Phase 4 ships shells + HAL-gated links; owner: per-feature workstreams.
- **"My submissions" reported-listing surface** (report §9) — deferred; trigger: reported-listing UX prioritization.
- **`BackofficeOnly` experience profile** — deferred; no requirement (report §4A).
- **Admin/Control workspace** (moderation queues, webhook ops in rail) — deferred; registry supports adding it later; trigger: operational surface growth.
