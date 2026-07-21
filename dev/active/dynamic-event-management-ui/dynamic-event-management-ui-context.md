<!-- ABOUTME: Operational memory for the workspace-shell (dynamic event management UI) workstream. -->
<!-- ABOUTME: Captures verified anchors, decisions, constraints, risks, and resume state for implementation agents. -->

# Dynamic Event Management UI (Workspace Shell) — Context

Last Updated: 2026-07-21 Europe/Brussels

## SESSION PROGRESS (2026-07-21 Europe/Brussels)

### ✅ COMPLETED
- Task 0.1: Documentation re-baseline completed. Plan approved by Oracle directionally (D1–D10 corrections accepted).
- Task 0.2: Registered seven lockable Instance/Tenant `ui_shell.*` definitions and three non-lockable User-only `ui_shell_preferences.*` definitions; documented all keys and added architecture coverage.
- Task 1.1: Added accepted ADR-019 for permanent app-rail chrome, route-derived workspaces, contextual navigation, and shared vocabulary; ADR-016–018 remain reserved for registration-data-collection.
- Task 1.2: Added the compile-time Events/Settings registry, segment-aware route classifier, scoped `UiShellState`, DI registrations, and 56 passing route/state cases.
- Task 1.3: Added the accessible desktop workspace rail and Xs bottom projection, integrated it only in MainLayout chrome, and introduced a shared logical Start inset so fixed dock panels cannot overlap the rail.
- Task 2.1: Added descriptor-selected workspace navigation providers and `WorkspaceNavigationHost`; MainLayout registers the host once, the host owns shared overlay brand/close chrome, and no-provider routes close the Start panel without overriding persisted or explicit user-close state.
- Task 2.2: Cleanly renamed the shell panel to `shell.workspace-nav`, moved discovery content to `Workspaces/EventsWorkspaceNavigation`, updated all runtime/test callsites and shipped dock docs, and verified stale `shell.left-nav` snapshots are ignored without an alias.
- Task 2.3: Deleted `SidebarState` and its DI/test registrations; MainLayout now controls workspace-nav state directly through `DockLayoutState`, while NavMenu derives availability from `UiShellState`/`IWorkspaceRegistry`. `AiAssistantState` remains as policy state.
- Phase 2 review fix: policy-driven hidden-chrome/no-provider workspace-nav changes now suppress autosave, hidden-chrome restoration tracks whether policy actually closed the panel, and debounced user saves capture their snapshot before later policy projection. Oracle follow-up passed the corrected state.
- Phase 1 independent review passed all five lanes after fixing query-preserving rail re-entry, ABOUTME headers, ADR numbering, governance deferral wording, and staged descriptor documentation.
- Re-verified repository claims: `/admin/tenant/navigation` confirmed as `@page` directive (never in `Routes.razor`); `GetManagedEventsByActorAsync` confirmed in generated client; `IAiAssistantActorContextService` confirmed as reusable for managed actors.
- Planning artifacts corrected and approved: status set to "Approved / Implementation started"; Phase 0 added; all task descriptions, dependencies, acceptance criteria, and risks updated per Oracle corrections.

### 🟡 READY TO START
- Task 3.1: Add the application UI-shell context query, DTOs, handler, and principal-scenario tests.

### ⏭️ NEXT
1. Run the Phase 2 review gate, then start Task 3.1 (application query + DTO + handler tests).
2. Preserve the recorded Phase 0/1 unrelated verification blockers; do not repair them in this workstream.
3. Keep rendered breakpoint/browser evidence deferred to the Phase 9 visual QA gate.

### ⚠️ BLOCKERS
- None hard. Soft coordination points:
  - Phase 0 architecture verification has four unrelated existing failures: decentralization schema discovery, HATEOAS permission metadata, repository naming, and organization-scope guardrails. The focused UI-shell registry test passes; do not alter unrelated code in this workstream.
  - Phase 1 Blazor client verification has three unrelated existing failures: generated `HalCollectionEmbeddedOfEventLocationManagementDto` typing and two `ReportEventDialogTests`. Focused workspace-shell suites pass.
  - Phase 2 Release/architecture verification is temporarily blocked by a concurrently edited ATProto workstream. Do not inspect or modify that workstream; rerun the gates when its owner restores compilation.
  - `EventApiClient.g.cs` regeneration (Phase 3, Task 3.2) must not collide with the webhook-delivery-redesign workstream's pinned client scope.
  - Unrelated dirty files (OpenAPI/NSwag artifacts, API/config docs, instance-admin settings files/tests) must be preserved; dirty-file hunk preservation controls are explicit in Task 3.2.

## Quick Resume
1. Read this context and `dynamic-event-management-ui-tasks.md`.
2. Read only the current phase + referenced decisions (D1–D10) from `dynamic-event-management-ui-plan.md`; do not reread the full plan on every resume.
3. Start from the first unchecked high-priority task unless the user overrides.
4. Keep `tasks.md` current during implementation; update context/plan only at their defined triggers.
5. The report (`dynamic-event-management-ui-report.md`) is product intent, not repository truth — code claims were re-verified in plan §2.1; trust the plan on conflicts.

## Key Files And Responsibilities

| Path | Existing/New | Layer | Purpose | Notes |
|---|---|---|---|---|
| `src/Explore.Blazor.Client/Layout/MainLayout.razor(.cs/.css)` | Existing | Blazor | Shell composition; gains rail track (P1); loses SidebarState mirroring (P2) | Registers shell dock descriptors; autosave rules live here |
| `src/Explore.Blazor.Client/Components/Shell/ShellDockPanels.cs` | Existing | Blazor | Shell descriptors; workspace navigation is `shell.workspace-nav` | `shell.ai-assistant` unchanged |
| `src/Explore.Blazor.Client/Components/Shell/Workspaces/EventsWorkspaceNavigation.razor(.cs/.css)` | New (P2) | Blazor | Events discovery navigation provider | Org-centric branch + tenant quick links preserved |
| `src/Explore.Blazor.Client/Components/Shell/AppWorkspaceRail.razor(.cs/.css)` | New (P1) | Blazor | Permanent app rail; NOT a dock panel (D1) | `<nav aria-label="Application workspaces">`; logical props |
| `src/Explore.Blazor.Client/Services/Shell/*` | New (P1) | Blazor | `WorkspaceKey/Descriptor/Registry`, `WorkspaceRouteClassifier`, `UiShellState` (D2) | Scoped DI in `ServiceCollectionExtensions.cs` |
| `src/Explore.Blazor.Client/Components/Shell/WorkspaceNavigationHost.razor(.cs/.css)` | New (P2) | Blazor | Renders the active descriptor's navigation component without panel re-registration | Owns shared overlay brand/close chrome; MainLayout owns panel visibility because closed dock content is unmounted |
| `src/Explore.Blazor.Client/Components/Shell/Workspaces/*` | New (P2/P4/P5/P6) | Blazor | Events/Studio/StudioEvent/Ai/Settings navigation providers | Studio event nav is HAL-gated (D5) |
| `src/Explore.Blazor.Client/Services/SidebarState.cs` | Deleted (P2) | Blazor | Removed pure dock mirror; consumers use `DockLayoutState`/`UiShellState` (D6) | `AiAssistantState` STAYS (policy state) |
| `src/Explore.Blazor.Client/Layout/NavMenu.razor(.cs)` | Existing | Blazor | Top bar; edited incrementally in P2.3/P3.3/P4.4 — never rewritten at once | Owns search/AddEvent/profile/AI toggle |
| `src/Explore.Blazor.Client/Routes.razor` | Existing | Blazor | Blazouter central route table + guards; Studio/AI/Settings route rows (P4/P5/P6) | Literal-before-parameterized ordering rule |
| `src/Explore.Application/Features/UiShell/**` | New (P3) | Application | `GetUiShellContextRequest(+Handler)`, `UiShellContextDto` (D4) | Composes AdminAuthority/my-actors/eligibility/AI/deployment mode |
| `src/Explore.API/Controllers/UiShellController.cs` | New (P3) | API | `GET api/ui-shell/context`, `RouteNames.GetUiShellContext`, Authenticated class | Plain DTO (no HAL), per-user, no output cache |
| `src/Explore.API/Controllers/PublicExperienceController.cs` | Existing | API | Anonymous shell bootstrap — unchanged contract; org-centric rail visibility resolved into it (P7) | Output-cached; must not grow private data |
| `src/Explore.Blazor.Client/Clients/EventApiClient.g.cs` + `schemas/openapi_islamu-event.json` | Existing | Contract | Regenerated once in P3.2 as a serialized/isolated generated-artifact diff | Coordinate with webhook workstream |
| `src/Explore.Domain/Constants/GovernanceSettingKeys.cs` + `src/Explore.Domain/Settings/SettingRegistry.cs` | Existing | Domain | `ui_shell.*` (tenant/instance) + `ui_shell_preferences.*` (user) keys (P7, D7/D8) | Explicit allowed scopes; instance-lockable |
| `src/Explore.Domain/UserPreference.cs` + `SettingsController` (`api/settings/user/{category}`) | Existing | Domain/API | Storage + API for durable layout prefs — REUSED, no new table/endpoint (D7) | Pattern precedent: `AiAssistantPreferences` category |
| `src/Explore.Blazor.Client/Services/Interop/LocalStorageDockLayoutPersistence.cs` | Existing | Blazor | Anonymous persistence; wrapped by new `ServerBackedDockLayoutPersistence` (P7) | Restore is defensive: clamps, drops unknown ids |
| `src/Explore.Blazor.Client/Services/Ai/{AiAssistantClientService,AiAssistantConversationState}.cs` | Existing | Blazor | Shared conversation stack for dock + `/ai` workspace (D10) | Scoped; both surfaces share the instance |
| `src/Explore.API/Controllers/EventController.cs` (`GET api/event/my` → `GetMyEventsAsync`; `GET api/event/managed/:actorId` → `GetManagedEventsByActorAsync`) | Existing | API | Studio events list: org/group actors use `GetManagedEventsByActorAsync`; personal/unscoped fallback uses `GetMyEventsAsync` | No new my-events endpoint; no group-specific create route |
| `docs/adr/ADR-019-workspace-shell-composition.md` | New (P1) | Docs | Records D1–D3 + vocabulary | Avoids registration-data-collection's reserved ADR-016–018 range |
| `docs/DOCK_LAYOUT.md`, `docs/BLAZOR.md`, `docs/API.md`, `docs/API_CHANGELOG.md`, `docs/CONFIGURATION.md`, `docs/ACCESSIBILITY.md` | Existing | Docs | Updated in the phases that change the described behavior | Fold into owning tasks |

## Key Decisions (synchronized with plan §5)

- **D1:** App rail = permanent shell chrome, not a dock panel (Start-side stacking is tabbed; rail must not close/resize/persist).
- **D2:** Compile-time `WorkspaceRegistry` + route-prefix classifier; route is the source of active workspace; `UiShellState` holds last-route-per-workspace (session only).
- **D3:** `shell.left-nav` → `shell.workspace-nav`, clean rename, no alias (defensive snapshot restore drops stale ids).
- **D4:** New authenticated `GET api/ui-shell/context` with `[PrivateNoStore]`, `[Authorize]`, `EndpointClass.Authenticated`, plain DTO; anonymous stays on `PublicExperience/shell`; no membership leakage into cacheable anonymous contract.
- **D5:** Studio event-level nav sections gated purely by event resource `_links` (edit/publish-readiness/session/registration/team relations).
- **D6:** Delete `SidebarState`; keep `AiAssistantState` (it computes effective AI availability, not just dock mirroring).
- **D7:** Layout prefs reuse `UserPreference` + `api/settings/user/{category}`; keys `ui_shell_preferences.layout.v1` / `.last_workspace` / `.last_actor`; anonymous storage gains tenant discriminator (`dock_layout:v1:{tenantSlug}:`) with no old-key compatibility read; promote-on-login.
- **D8:** Tenant defaults are governance keys `ui_shell.*` (tenant/instance scope, lockable): rail public visibility, per-workspace default nav mode, user-override allowance, organizer default workspace.
- **D9:** Settings hub = canonical `/settings/**` routes mounting EXISTING admin settings components; old `/admin/*/settings` central route rows deleted; old `@page "/admin/tenant/navigation"` directive removed (it was never in `Routes.razor`); all link producers updated via bounded `rg` sweep; guards unchanged; single-tenant "Site administration" grouping is presentation-only.
- **D10:** `/ai` workspace + dock share `AiAssistantConversationState`/`IAiAssistantClientService`; dock header gets "Open in AI workspace"; product name stays "AI Assistant"; `/ai` routes are authenticated-only (`AuthenticatedRouteGuard`).
- **Managed actors:** reuse `IAiAssistantActorContextService.ListAuthorizedActorContextsAsync` for organization + group actor list; personal events fallback uses `GetMyEventsRequest`.
- **Group-publisher investigation (§2.6):** resolved — group actors are included in the managed-actor list; event creation for groups uses the existing `/events/create` publisher picker (no new group-specific create route).
- **Experience profile:** reuse existing `PublicExperienceMode { DiscoveryCentric, OrganizationCentric }` + `public_experience.primary_organization_id`; do NOT invent Marketplace/OrganizationHub parallel enums.

## Constraints And Rules To Remember

- Fallback intent contract (no single matching intent): `add-get-endpoint` + `add-cqrs-handler` + `openapi-contract-change` + `blazor-component-affordance`/`add-hal-link`.
- Client isolation: `Explore.Blazor.Client` consumes ONLY generated `IEventApiClient` models (QUICK_REFERENCE #23; NU1605/WASM memory note).
- HAL links gate per-resource affordances; capabilities/roles gate only broad workspace/menu eligibility (QUICK_REFERENCE #21).
- Controller standard: explicit template + `RouteNames` + `[EndpointClassification]` + `[ProducesResponseType]` + `[PrivateNoStore]`; operationId `UiShell_GetContext`.
- Never hand-edit `EventApiClient.g.cs`; regen via the documented msbuild GenerateApiClient path; exact generated method name verified after regen rather than assumed; generated-artifact diff serialized and isolated; no commit unless explicitly requested.
- Dock invariants: descriptor-owned width/persistence; viewport projection never autosaves; logical Start/End; no central panel enum; no page-level shell compensation; workspace content floors are generic hint (no workspace-specific branching in `DockLayoutState`).
- A11y architecture tests: skip link, main/header/nav landmarks, live regions, `<h1>` per page, logical CSS properties.
- Dev mode: delete removed routes/services/tests — no compatibility shims, no `[Skip]` for obsolete behavior.
- Every new C# file: file-scoped namespace + 2-line ABOUTME; every new `.razor`: BEM `.razor.css`.
- `/ai` routes are authenticated-only; anonymous AI access remains dock-only.
- No HybridCache introduction in shell-context handler.
- Failing-first expectation: every task that changes executable behavior starts with the smallest relevant check that fails before implementation and passes afterward.
- Verification: once per phase — one Release build + the single phase-assigned test project. Never solution-level `dotnet test`, never app/browser/Docker/Aspire startup for intermediate phases. Phase 9 adds a final visual/browser QA gate.

## Validation Baseline

Per phase (see plan §6/§7 for the assigned project):
- `dotnet build --configuration Release --verbosity quiet`
- P0: `Event.Architecture.Tests` (registry parity) · P1: `Explore.Blazor.Client.Tests` · P2: `Event.Architecture.Tests` · P3: `Event.Application.UnitTests` · P4: `Explore.Blazor.Client.Tests` · P5: `Explore.Blazor.Client.Tests` · P6: `Explore.Blazor.Client.Tests` · P7: `Event.API.IntegrationTests` · P8: `Explore.Blazor.Client.Tests` · P9: visual/browser QA gate

Known baseline note: memory records ~15 shared pre-existing test failures from upstream webhook fallout (islamu.ngo import status). If encountered, record them as pre-existing in tasks.md — do not chase them inside this workstream.

## Current Known Risks / Unknowns (owning tasks)

- Rail × dock grid interplay at constrained widths → 1.3 / 8.1.
- Group-actor publisher semantics for managed actors → resolved (Task 3.1; group actors included; create uses existing `/events/create` picker).
- Blazouter metadata support for workspace keys → 1.2 (prefix classification avoids the need).
- Settings link sweep completeness → 6.2 (`rg` sweep includes `@page` directives; `/admin/tenant/navigation` was never in `Routes.razor`).
- NSwag regen collision with webhook workstream → 3.2 (coordinate before landing; dirty-file hunk preservation explicit).

## Handoff Notes

### Handoff — 2026-07-21 Europe/Brussels
- **Current state:** Tasks 0.1, 0.2, Phase 1 Tasks 1.1–1.3, and Phase 2 Tasks 2.1–2.3 are implemented. The shell registers only `shell.workspace-nav`; providers are contextual; `SidebarState` is deleted; AI policy remains in `AiAssistantState`.
- **Next action:** Complete the Phase 2 review gate, then start Phase 3 Task 3.1 (application UI-shell context query/DTO/handler).
- **Blockers:** Phase 0 architecture and Phase 1 Blazor client suites have unrelated existing failures recorded in `tasks.md`; rendered breakpoint QA remains deferred to Phase 9; NSwag regen coordination remains noted; unrelated dirty files must be preserved.
- **Modified files:** `dev/active/dynamic-event-management-ui/dynamic-event-management-ui-plan.md`, `dynamic-event-management-ui-context.md`, `dynamic-event-management-ui-tasks.md`.
- **Validation:** Task 2.2 focused client suites pass 149/149. Task 2.3/review affected suites pass 105/105: MainLayout 33, panel lifecycle 6, NavMenu admin 17, authentication flow 22, and EventList 27; unchanged cached dock architecture tests pass 5/5. The review regressions were red before the policy-state fix (three persistence/provenance failures) and green afterward. The Release/architecture rebuild is temporarily blocked by the separately owned ATProto workstream; do not inspect it. Phase 1 full-suite baseline remains 1,856 passed, 3 unrelated failures, 1 governed skip; Phase 0 architecture remains 280 passed, 4 unrelated failures, 1 governed skip.
- **Documentation impact:** `docs/CONFIGURATION.md` now documents all `ui_shell.*` and `ui_shell_preferences.*` definitions, scopes, locks, defaults, and allowed values.
- **Risks:** See risk register (plan §13).
- **Notes for next contributor/agent:** The report file is input, not truth — plan §2.1 is the verified baseline. Do not rewrite `NavMenu` in one pass; it is edited incrementally in 2.3/3.3/4.4 by design. AnySearch MCP was unavailable during original planning; fallback evidence sources were official Plane/W3C docs plus Context7 MudBlazor docs.
