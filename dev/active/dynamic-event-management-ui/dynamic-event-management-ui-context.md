<!-- ABOUTME: Operational memory for the workspace-shell (dynamic event management UI) workstream. -->
<!-- ABOUTME: Captures verified anchors, decisions, constraints, risks, and resume state for implementation agents. -->

# Dynamic Event Management UI (Workspace Shell) — Context

Last Updated: 2026-07-21 Europe/Brussels

## SESSION PROGRESS (2026-07-21 Europe/Brussels)

### ✅ COMPLETED
- Planning created from `dynamic-event-management-ui-report.md` (product/architecture input) with full repository verification.
- Current-state report completed with evidence (plan §2): shell/dock/NavMenu/routing/AI/settings/preferences anchors all verified from source.
- Overlap check done: `home-discovery-experience` (touches `/home` + shell branch — coordinate on `HomeStart`/Home routes), `onboarding-ux-refactor` (SetupLayout routes stay hide-chrome; no conflict), `session-series-ux` (session editors are link targets from Studio nav only). Webhook redesign intent pins `EventApiClient.g.cs` — coordinate NSwag regen timing (plan risk table).

### 🟡 IN PROGRESS
- Awaiting user review of the implementation plan (especially: Settings route unification scope in Phase 6, deferred navigation-override model, NSwag regen coordination).

### ⏭️ NEXT
1. User reviews/corrects/approves the plan.
2. First implementation agent starts with Task 1.1 (ADR-016) then 1.2 (workspace registry/classifier).
3. Refresh this context after the first implementation slice.

### ⚠️ BLOCKERS
- None hard. Soft coordination point: `EventApiClient.g.cs` regeneration (Phase 3, Task 3.2) must not collide with the webhook-delivery-redesign workstream's pinned client scope.

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
| `src/Explore.Blazor.Client/Components/Shell/ShellDockPanels.cs` | Existing | Blazor | Panel descriptors; `shell.left-nav` → `shell.workspace-nav` (P2) | `shell.ai-assistant` unchanged |
| `src/Explore.Blazor.Client/Components/Shell/AppSideNav.razor(.cs/.css)` | Existing → deleted (P2) | Blazor | Content moves to `EventsWorkspaceNavigation` | Org-centric branch + tenant quick links preserved |
| `src/Explore.Blazor.Client/Components/Shell/AppWorkspaceRail.razor(.cs/.css)` | New (P1) | Blazor | Permanent app rail; NOT a dock panel (D1) | `<nav aria-label="Application workspaces">`; logical props |
| `src/Explore.Blazor.Client/Services/Shell/*` | New (P1) | Blazor | `WorkspaceKey/Descriptor/Registry`, `WorkspaceRouteClassifier`, `UiShellState` (D2) | Scoped DI in `ServiceCollectionExtensions.cs` |
| `src/Explore.Blazor.Client/Components/Shell/WorkspaceNavigationHost.razor` | New (P2) | Blazor | Renders active workspace's nav provider inside `shell.workspace-nav` | Owns overlay header/close |
| `src/Explore.Blazor.Client/Components/Shell/Workspaces/*` | New (P2/P4/P5/P6) | Blazor | Events/Studio/StudioEvent/Ai/Settings navigation providers | Studio event nav is HAL-gated (D5) |
| `src/Explore.Blazor.Client/Services/SidebarState.cs` | Existing → deleted (P2) | Blazor | Pure dock mirror; consumers migrate to `DockLayoutState`/`UiShellState` (D6) | `AiAssistantState` STAYS (policy state) |
| `src/Explore.Blazor.Client/Layout/NavMenu.razor(.cs)` | Existing | Blazor | Top bar; edited incrementally in P2.3/P3.3/P4.4 — never rewritten at once | Owns search/AddEvent/profile/AI toggle |
| `src/Explore.Blazor.Client/Routes.razor` | Existing | Blazor | Blazouter central route table + guards; Studio/AI/Settings route rows (P4/P5/P6) | Literal-before-parameterized ordering rule |
| `src/Explore.Application/Features/UiShell/**` | New (P3) | Application | `GetUiShellContextRequest(+Handler)`, `UiShellContextDto` (D4) | Composes AdminAuthority/my-actors/eligibility/AI/deployment mode |
| `src/Explore.API/Controllers/UiShellController.cs` | New (P3) | API | `GET api/ui-shell/context`, `RouteNames.GetUiShellContext`, Authenticated class | Plain DTO (no HAL), per-user, no output cache |
| `src/Explore.API/Controllers/PublicExperienceController.cs` | Existing | API | Anonymous shell bootstrap — unchanged contract; org-centric rail visibility resolved into it (P7) | Output-cached; must not grow private data |
| `src/Explore.Blazor.Client/Clients/EventApiClient.g.cs` + `schemas/openapi_islamu-event.json` | Existing | Contract | Regenerated once in P3.2 as a discrete commit | Coordinate with webhook workstream |
| `src/Explore.Domain/Constants/GovernanceSettingKeys.cs` + `src/Explore.Domain/Settings/SettingRegistry.cs` | Existing | Domain | `ui_shell.*` (tenant/instance) + `ui_shell_preferences.*` (user) keys (P7, D7/D8) | Explicit allowed scopes; instance-lockable |
| `src/Explore.Domain/UserPreference.cs` + `SettingsController` (`api/settings/user/{category}`) | Existing | Domain/API | Storage + API for durable layout prefs — REUSED, no new table/endpoint (D7) | Pattern precedent: `AiAssistantPreferences` category |
| `src/Explore.Blazor.Client/Services/Interop/LocalStorageDockLayoutPersistence.cs` | Existing | Blazor | Anonymous persistence; wrapped by new `ServerBackedDockLayoutPersistence` (P7) | Restore is defensive: clamps, drops unknown ids |
| `src/Explore.Blazor.Client/Services/Ai/{AiAssistantClientService,AiAssistantConversationState}.cs` | Existing | Blazor | Shared conversation stack for dock + `/ai` workspace (D10) | Scoped; both surfaces share the instance |
| `src/Explore.API/Controllers/EventController.cs` (`GET api/event/my`, `RouteNames.GetMyEvents`) | Existing | API | Studio events list source (HAL collection) | No new my-events endpoint |
| `docs/adr/ADR-016-workspace-shell-composition.md` | New (P1) | Docs | Records D1–D3 + vocabulary | Next free ADR number after ADR-015 |
| `docs/DOCK_LAYOUT.md`, `docs/BLAZOR.md`, `docs/API.md`, `docs/API_CHANGELOG.md`, `docs/CONFIGURATION.md`, `docs/ACCESSIBILITY.md` | Existing | Docs | Updated in the phases that change the described behavior | Fold into owning tasks |

## Key Decisions (synchronized with plan §5)

- **D1:** App rail = permanent shell chrome, not a dock panel (Start-side stacking is tabbed; rail must not close/resize/persist).
- **D2:** Compile-time `WorkspaceRegistry` + route-prefix classifier; route is the source of active workspace; `UiShellState` holds last-route-per-workspace (session only).
- **D3:** `shell.left-nav` → `shell.workspace-nav`, clean rename, no alias (defensive snapshot restore drops stale ids).
- **D4:** New authenticated `GET api/ui-shell/context`; anonymous stays on `PublicExperience/shell`; no membership leakage into cacheable anonymous contract.
- **D5:** Studio event-level nav sections gated purely by event resource `_links` (edit/publish-readiness/session/registration/team relations).
- **D6:** Delete `SidebarState`; keep `AiAssistantState` (it computes effective AI availability, not just dock mirroring).
- **D7:** Layout prefs reuse `UserPreference` + `api/settings/user/{category}`; keys `ui_shell_preferences.layout.v1` / `.last_workspace` / `.last_actor`; anonymous stays local; promote-on-login.
- **D8:** Tenant defaults are governance keys `ui_shell.*` (tenant/instance scope, lockable): rail public visibility, per-workspace default nav mode, user-override allowance, organizer default workspace.
- **D9:** Settings hub = canonical `/settings/**` routes mounting EXISTING admin settings components; old `/admin/*/settings` + `/admin/tenant/navigation` rows deleted with a bounded link sweep; guards unchanged; single-tenant "Site administration" grouping is presentation-only.
- **D10:** `/ai` workspace + dock share `AiAssistantConversationState`/`IAiAssistantClientService`; dock header gets "Open in AI workspace"; product name stays "AI Assistant".
- **Experience profile:** reuse existing `PublicExperienceMode { DiscoveryCentric, OrganizationCentric }` + `public_experience.primary_organization_id`; do NOT invent Marketplace/OrganizationHub parallel enums.

## Constraints And Rules To Remember

- Fallback intent contract (no single matching intent): `add-get-endpoint` + `add-cqrs-handler` + `openapi-contract-change` + `blazor-component-affordance`/`add-hal-link`.
- Client isolation: `Explore.Blazor.Client` consumes ONLY generated `IEventApiClient` models (QUICK_REFERENCE #23; NU1605/WASM memory note).
- HAL links gate per-resource affordances; capabilities/roles gate only broad workspace/menu eligibility (QUICK_REFERENCE #21).
- Controller standard: explicit template + `RouteNames` + `[EndpointClassification]` + `[ProducesResponseType]`; operationId `UiShell_GetContext`.
- Never hand-edit `EventApiClient.g.cs`; regen via the documented msbuild GenerateApiClient path, discrete commit.
- Dock invariants: descriptor-owned width/persistence; viewport projection never autosaves; logical Start/End; no central panel enum; no page-level shell compensation.
- A11y architecture tests: skip link, main/header/nav landmarks, live regions, `<h1>` per page, logical CSS properties.
- Dev mode: delete removed routes/services/tests — no compatibility shims, no `[Skip]` for obsolete behavior.
- Every new C# file: file-scoped namespace + 2-line ABOUTME; every new `.razor`: BEM `.razor.css`.
- Verification: once per phase — one Release build + the single phase-assigned test project. Never solution-level `dotnet test`, never app/browser/Docker/Aspire startup.

## Validation Baseline

Per phase (see plan §6/§7 for the assigned project):
- `dotnet build --configuration Release --verbosity quiet`
- P1: `Explore.Blazor.Client.Tests` · P2: `Event.Architecture.Tests` · P3: `Event.Application.UnitTests` · P4: `Explore.Blazor.Client.Tests` · P5: `Explore.Blazor.Client.Tests` · P6: `Explore.Blazor.Client.Tests` · P7: `Event.API.IntegrationTests` · P8: `Explore.Blazor.Client.Tests`

Known baseline note: memory records ~15 shared pre-existing test failures from upstream webhook fallout (islamu.ngo import status). If encountered, record them as pre-existing in tasks.md — do not chase them inside this workstream.

## Current Known Risks / Unknowns (owning tasks)

- Rail × dock grid interplay at constrained widths → 1.3 / 8.1.
- Group-actor publisher semantics for managed actors → 3.1 (investigate inside handler design).
- Blazouter metadata support for workspace keys → 1.2 (prefix classification avoids the need).
- Settings link sweep completeness → 6.2 (`rg` sweep is an acceptance criterion).
- NSwag regen collision with webhook workstream → 3.2 (coordinate before landing).

## Handoff Notes

### Handoff — 2026-07-21 Europe/Brussels
- **Current state:** Planning complete (plan/context/tasks written). No runtime code changed.
- **Next action:** User review; then Task 1.1 (ADR-016) + 1.2 (registry/classifier).
- **Blockers:** None hard; NSwag regen coordination noted.
- **Modified files:** Only `dev/active/dynamic-event-management-ui/*` planning artifacts.
- **Validation:** Not run (planning-only change; no build/test required by the skill for artifact-only work).
- **Documentation impact:** Deferred to implementation phases (each doc update is folded into its owning task).
- **Risks:** See risk register (plan §13).
- **Notes for next contributor/agent:** The report file is input, not truth — plan §2.1 is the verified baseline. Do not rewrite `NavMenu` in one pass; it is edited incrementally in 2.3/3.3/4.4 by design.
