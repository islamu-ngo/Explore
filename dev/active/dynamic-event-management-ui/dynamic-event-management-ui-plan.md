<!-- ABOUTME: Implementation plan for the workspace-based dynamic shell: app rail, contextual workspace navigation, Studio, AI workspace, and settings hub. -->
<!-- ABOUTME: Repository-grounded phases, decisions, constraints, and verification for implementation agents. -->

# Dynamic Event Management UI (Workspace Shell) — Implementation Plan

Last Updated: 2026-07-23 Europe/Brussels (Task 9.3 follow-up accepted; final authenticated browser QA blocked)

## 0. Planning Metadata

- **Original request:** Turn the architecture report `dev/active/dynamic-event-management-ui/dynamic-event-management-ui-report.md` into an executable, repo-conventioned implementation plan for a workspace-based shell (Plane-style app rail + contextual secondary navigation + AI dock), Studio organizer workbench, dual-mode AI, scope-aware Settings, and durable layout preferences.
- **Task directory:** `dev/active/dynamic-event-management-ui/`
- **Planning status:** Approved / Implementation started
- **Matched intents:** No single intent covers this cross-cutting UI-platform work. Fallback contract composed from: `add-get-endpoint` (shell-context endpoint), `add-cqrs-handler` (`GetUiShellContextRequest`), `openapi-contract-change` (generated client), `blazor-component-affordance` + `add-hal-link` (HAL-gated Studio navigation), plus canonical docs below. A reusable intent is NOT proposed — this shell restructure is a one-time platform change.
- **Relevant skills:** `implementation-plan` (this plan), `blazor-ui-conventions`, `blazor-css-isolation`, `design-system`, `blazor-bff-patterns`, `cqrs-mediatr-guidelines`, `clean-architecture-rules`, `auth-patterns`.
- **Relevant rules:** `.claude/rules/blazor-client.md`, `.claude/rules/blazor-server.md`, `.claude/rules/application-layer.md`, `.claude/rules/api-controllers.md`, `.claude/rules/api-hateoas.md`, `.claude/rules/tests.md`.
- **Primary layers touched:** Blazor Client (dominant), API (one new read endpoint), Application (one new query + DTOs + governance setting definitions), Domain (governance keys only), Docs (ADR + BLAZOR/DOCK_LAYOUT/API/CONFIGURATION).
- **Complexity:** **XL.** Evidence: the shell is consumed by every page; `NavMenu.razor.cs` (475 lines) mixes eligibility, policy, and dock mirroring; the dock system has 4 production panels with persistence and architecture tests; Studio/AI/Settings each introduce new route groups in the central Blazouter table (`Routes.razor`, 149 lines, 40+ routes).

## 1. Executive Summary

The client today renders one static event-seeker shell everywhere: a discovery sidebar (`AppSideNav` in dock panel `shell.left-nav`), a top bar (`NavMenu`) that mixes role checks and eligibility loads, and an AI dock (`shell.ai-assistant`). Organizer work is scattered under `/events/create`, `/events/:id/edit`, and `/admin/*` pages; the AI assistant exists only as a contextual dock; settings are split across `/settings` and four `/admin/*/settings` pages with no unifying scope model.

This plan converts the shell into a **workspace-based application**:

- a permanent **app rail** (shell chrome, NOT a dock panel) with Explore, Studio, AI, and Settings (bottom);
- **one contextual secondary navigation** (`shell.workspace-nav`, the renamed `shell.left-nav` dock panel) whose content is provided per active workspace;
- the existing **AI dock** retained as the contextual inline-end surface, now sharing conversation state with a full `/ai` workspace;
- a **server-authoritative UI shell context** endpoint so Studio/Settings visibility comes from capabilities, not client role checks;
- **durable layout preferences** for authenticated users on top of the existing `DockLayoutSnapshot`/`UserPreference` machinery;
- **tenant shell governance defaults** wired into the existing 5-tier settings cascade.

**Non-goals:** implementing full Studio operational features (attendee ops, check-in, ticketing, communications — shells and HAL-gated nav only); the tenant navigation item-override table and navigation editor preview modes (deferred, see §12/Deferred); a `BackofficeOnly` experience profile; AI streaming; any backward-compatibility aliases (development mode — old routes and bridge services are removed, not preserved).

## 2. Source-Grounded Current State Report

### 2.1 Evidence Log

| Claim | Evidence | Confidence | Notes |
|---|---|---|---|
| Shell = header (NavMenu) + `DockLayoutHost Scope=Shell` wrapping `@Body`; hide-chrome branch for setup pages | Verified: `src/Explore.Blazor.Client/Layout/MainLayout.razor` | High | Rail must become a new permanent track around/beside `DockLayoutHost` |
| Two shell dock panels exist: `shell.left-nav` (Start, 280px) and `shell.ai-assistant` (End, 360px) | Verified: `src/Explore.Blazor.Client/Components/Shell/ShellDockPanels.cs` | High | Descriptor-owned width/persistence policy |
| Dock engine: descriptors, `DockLayoutState`, `LastChangeReason`, viewport projection, localStorage persistence `dock_layout:v1:` | Verified: `src/Explore.Blazor.Client/Services/Docking/*` and `docs/DOCK_LAYOUT.md` | High | Report's claim "same-side stacking is tab-oriented" confirmed (`StackStrategy: Tabbed` on left-nav) |
| `SidebarState` and `AiAssistantState` are scoped bridge/policy services | Verified: `Services/SidebarState.cs`, `Services/AiAssistantState.cs`, `Extensions/ServiceCollectionExtensions.cs:159-166` | High | Consumers: `NavMenu`, `MainLayout`, `AiAssistantRail`, `SettingsAiAssistant`, `TenantAdminSettingsLayout`, `InstanceAdminSettingsLayout` |
| Discovery sidebar content + org-centric branch + tenant quick links | Verified: `Components/Shell/AppSideNav.razor(.cs)`; `PublicExperienceMode.OrganizationCentric` | High | Becomes `EventsWorkspaceNavigation` |
| Top bar loads role/eligibility state client-side (`GetAdminAuthorityAsync`, onboarding status, `EventCreationEligibilityService`, org/group lists) | Verified: `Layout/NavMenu.razor.cs::LoadDeploymentModeAsync`, `LoadEventCreationEligibilityAsync` | High | Broad menu eligibility by role is allowed; per-resource actions must be HAL-gated |
| Central route table with guards (Blazouter), not Razor `@page` discovery | Verified: `src/Explore.Blazor.Client/Routes.razor` (`RouteConfig` list, `AuthenticatedRouteGuard`, `AdminRouteGuard`, `TenantAdminRouteGuard`, `OrgAdminRouteGuard`, `GroupAdminRouteGuard`) | High | Workspace classifier keys off this table; new route groups are additive rows |
| Experience profile already exists as governance settings | Verified: `GovernanceSettingKeys.PublicExperience.Mode` (`public_experience.mode`), `.PrimaryOrganizationId`; `PublicExperienceMode { DiscoveryCentric, OrganizationCentric }` in `src/Explore.Application/Models/PublicExperienceMode.cs` | High | Report's `Marketplace/OrganizationHub` maps onto existing enum; do not invent a parallel profile |
| Anonymous shell bootstrap exists | Verified: `src/Explore.API/Controllers/PublicExperienceController.cs` — `GET api/PublicExperience/shell` (`RouteNames.GetPublicExperienceShell`, output-cached) | High | Authenticated shell context endpoint does NOT exist |
| Per-user settings storage + API exist | Verified: `Explore.Domain/UserPreference.cs` (tenant+user+key), `IUserPreferenceRepository`, `SettingsController` `GET/PUT api/settings/user/{category}` (`RouteNames.GetUserSettings`, `UpdateUserSettingsBatch`, `UpdateUserSetting`) | High | Reuse for shell layout preferences; no new table needed |
| User AI navbar preference pattern exists | Verified: `NavMenu.razor.cs` reads category `AiAssistantPreferences`, key `ai_assistant_preferences.show_navbar_button`; key constant in `GovernanceSettingKeys` | High | Template for `ui_shell` preference keys |
| AI conversation API is complete and shared-state-ready | Verified: `AiAssistantController` (bootstrap, conversations, messages, proposed-action confirm/reject, run status/cancel); client `AiAssistantConversationState` (scoped), `IAiAssistantClientService`, `AiAssistantRail HostedInDock` dual mode | High | `/ai` workspace can reuse all of it; no new AI API needed |
| My-events management surface exists server-side | Verified: `EventController.cs` — `GetMyEvents` (personal HAL collection) and `GetManagedEventsByActorAsync` (org/group actor-scoped) | High | Studio uses `GetManagedEventsByActorAsync` for selected actors, `GetMyEventsAsync` as personal/unscoped fallback; no new endpoint needed |
| Settings surfaces are fragmented | Verified: `Routes.razor` — `/settings` (`Pages/User/Settings` + `SettingsLayout` tabs), `/admin/tenant/settings`, `/admin/instance/settings`, `/admin/organization/:id/settings`, `/admin/group/:id/settings`; `/admin/tenant/navigation` uses `@page` discovery and was never in `Routes.razor` | High | Settings hub unifies routes and navigation; migration must remove both central route rows and old `@page` directives plus all link producers |
| Governance cascade + locks + single-tenant bypass | Verified: `docs/QUICK_REFERENCE.md` (5-tier cascade), `HierarchicalSettingsResolver`, `SettingRegistry` (`src/Explore.Domain/Settings/SettingRegistry.cs`, frozen definitions) | High | New `ui_shell.*` keys must declare allowed scopes |
| Accessibility conventions are architecture-tested | Verified: `docs/TESTING.md` — `MainLayout_MustContain_NavigationLandmark`, skip-link, live regions, scoped-CSS logical-property checks; `DockLayoutArchitectureTests` | High | Rail/nav landmark changes must keep these green |
| Blazor isolation: only generated `IEventApiClient`, HAL-gated affordances | Verified: `docs/QUICK_REFERENCE.md` rules 21/23, `docs/ARCHITECTURE.md`, memory note (NU1605/WASM prevents Application reference) | High | All new client models mirror generated DTOs or are UI-local |
| Report file is the product/architecture input | Verified: `dev/active/dynamic-event-management-ui/dynamic-event-management-ui-report.md` | High | Product intent; every code claim in it re-verified above |

### 2.2 Existing Implementation (by layer)

- **Blazor Client (shell):** `MainLayout` renders header (`NavMenu` inside `<nav aria-label="Sidebar navigation">`), then `DockLayoutHost Scope=Shell` around `@Body` + `Footer`. `MainLayout.razor.cs` registers shell descriptors, hydrates/persists layout key `shell`, and mirrors dock state into `SidebarState`/`AiAssistantState`. `AppSideNav` is discovery-only navigation with an org-centric branch. `NavMenu` owns search (`/events?q=`), Add Event resolution, admin menu entries from `AdminAuthorityDto` + onboarding status, org/group submenus, AI toggle, profile dropdown.
- **Blazor Client (routing):** Blazouter central table in `Routes.razor` with route guards; `MainLayout` is default layout; `SetupLayout` for setup/onboarding (hide-chrome).
- **Blazor Client (AI):** `AiAssistantRail` (78K, dock-hosted), `AiAssistantConversationState`, `AiAssistantClientService`; policy state in `AiAssistantState` (tenant enablement + anonymous access + user navbar preference).
- **API/Application:** `PublicExperienceController` (anonymous shell), `SettingsController` (user/tenant scoped settings by category), `UserController.GetAdminAuthority`, `EventController.GetMyEvents` (HAL), `AiAssistantController` (full conversation lifecycle), `OrganizationController`/`GroupController` "my" lists. Governance keys in `GovernanceSettingKeys`; definitions in `SettingRegistry`.
- **Persistence:** `UserPreference` (tenant, user, key unique) + repository; tenant navigation links (`TenantNavigationLink` entity, admin editor at `/admin/tenant/navigation` via `@page`); anonymous dock storage gains a tenant discriminator (`dock_layout:v1:{tenantSlug}:`) with no old-key compatibility read.

### 2.3 Existing Tests And Coverage

- `Explore.Blazor.Client.Tests`: `BlazorTestContext` harness, `AddShellStateMocks()`, `NavMenuTestServices.Register(ctx)`, dock suites (`DockLayoutStateTests`, `DockHostTests`, `DockRegistrationTests`, `LocalStorageDockLayoutPersistenceTests`), full `AiAssistantRail` bUnit coverage.
- `Event.Architecture.Tests`: `DockLayoutArchitectureTests` (no central panel enum, no shell compensation hacks), MainLayout accessibility landmark tests, scoped-CSS logical property checks, ABOUTME enforcement.
- `Event.API.IntegrationTests`: contract/RealRuntime profiles, HAL policy tests, auth-family matrix.
- `Event.Application.UnitTests`: handler-level coverage incl. `GetEventPublishReadinessRequestHandlerTests` pattern.
- **Gaps:** no tests describe workspace visibility, rail composition, settings scope resolution, or server-side shell capability aggregation (they don't exist yet).

### 2.4 Existing Documentation And Contracts

`docs/DOCK_LAYOUT.md` (authoritative dock behavior incl. compatibility-bridge rule), `docs/BLAZOR.md` (project roles, BFF endpoint families, service/state patterns), `docs/ARCHITECTURE.md`, `docs/DESIGN_SYSTEM.md`, `docs/ACCESSIBILITY.md`, `docs/LOCALIZATION.md` (RTL), `docs/GOVERNANCE.md` (API contract rules, NSwag conventions), `docs/API.md`/`API_CHANGELOG.md`, `docs/CONFIGURATION.md` (settings keys), `schemas/openapi_islamu-event.json` + generated `EventApiClient.g.cs`. ADR-016–018 are reserved by the active registration-data-collection workstream; this work adds **ADR-019**.

### 2.5 Current Pain Points

1. **One static seeker sidebar everywhere** — `AppSideNav` content is irrelevant on admin/settings pages (report anti-pattern #1; verified content is discovery-only).
2. **Role-based shell gating** — `NavMenu` decides admin menu items from client-aggregated role state across four service calls; Studio-grade surfaces need capability-driven visibility (QUICK_REFERENCE #21 allows role checks only for broad menu eligibility, and there is no server capability aggregate today).
3. **No organizer workbench** — event management is reached from profile menus and scattered `/admin/*` pages; `GetMyEvents` exists but has no dedicated surface.
4. **AI is dock-only** — history, long workflows, and drafts have no full-page home; conversation state/service are already reusable.
5. **Layout preferences are device-local** — `LocalStorageDockLayoutPersistence` only; `DockLayoutSnapshot` is explicitly designed for future user-preference storage (docs/DOCK_LAYOUT.md) but the server path does not exist.
6. **Settings fragmentation** — five entry points, no scope selector, single-tenant operators see instance/tenant split unnecessarily.

### 2.6 Unknowns After Investigation

| Unknown | Searched | Resolving task |
|---|---|---|
| Exact CSS interplay of a fixed rail track with `MudLayout`/`DockLayoutHost` grid and EventList workspace dock at constrained widths | `MainLayout.razor.css`, `DockLayoutHost.razor.css` grid rules read; no rail precedent exists | Task 1.3 (implement + bUnit + responsive matrix in Phase 8) |
| Whether `Blazouter.Components.Router` supports per-route-group metadata we can attach workspace keys to, or whether classification must be prefix-based | `Routes.razor` uses `RouteConfig { Path, Component, Layout, Guards }`; no metadata dictionary observed | Task 1.2 (classifier is prefix-based first; extend `RouteConfig` only if Blazouter exposes it) |
| Which internal links point at `/admin/*/settings` routes (for Phase 6 route unification) | Not exhaustively enumerated | Task 6.2 bounded `rg` sweep + link update |
| Group actors as Studio publishers | Resolved: `IAiAssistantActorContextService` supplies authorized organization/group actors; group event creation uses the existing `/events/create` publisher picker | Task 3.1 reuses the actor service; no group-specific route |

## 3. Proposed Future State

Desktop shell composition (logical directions):

- **Inline start:** permanent `AppWorkspaceRail` (56–64px, shell chrome, not closable) → `shell.workspace-nav` dock panel (contextual navigation, 240–280px, collapsible/persisted) → main canvas → **inline end:** `shell.ai-assistant` dock (unchanged behavior).
- **Route normally decides workspace:** `/events|/home|/organization/...` → Explore; `/studio/**` → Studio; `/ai/**` → AI; `/settings` and administrative `/settings/**` routes → Settings. Personal Settings is the one session-scoped utility exception: navigation from Explore/Studio/AI to `/settings/personal/**` preserves the originating workspace and its contextual navigation; a direct load or refresh has no trusted origin and resolves to dedicated Settings presentation. Hide-chrome routes (`/setup`, `/onboarding/*`, `/startup`) render no rail (already `SetupLayout`).
- **Visibility rule:** `Visible(workspace) = FeatureAvailable AND ExperienceProfileAllows AND AuthRequirementSatisfied AND ServerCapabilityAllows`, resolved from `GET /api/ui-shell/context` (authenticated) or the existing anonymous public-experience shell.
- **Studio:** actor-level navigation (actor switcher listing only server-provided managed actors; pinned when `public_experience.primary_organization_id` is set), event-level navigation replacing (not stacking) the secondary nav, sections gated by the event resource's HAL `_links` (`edit`, `publish-readiness`, session/registration/team relations).
- **AI:** `/ai` + `/ai/chats/{id}` full workspace sharing `AiAssistantConversationState` + `IAiAssistantClientService` with the dock; dock header gains "Open in AI workspace"; product label stays "AI Assistant" (never the model id).
- **Settings:** one gear whose primary action always opens contextual Personal Settings; a dedicated `/settings` hub and canonical administrative routes expose only server-authorized scopes. Personal sections use compact in-page navigation, existing administrative layouts retain their own internal sidebars, and no shell-level Settings sidebar exists.
- **Preferences:** tenant governs defaults/availability (`ui_shell.*` governance keys, tenant/instance scopes, lockable); the user owns personal layout (`ui_shell_preferences.layout.v1` etc. through the existing user-settings API); viewport projection never writes durable state (existing `LastChangeReason` contract).
- **Mobile:** rail projects to bottom navigation; workspace nav becomes temporary drawer; AI dock becomes full-screen temporary chrome (existing dock projection).

## 4. Non-Negotiable Constraints

1. `Explore.Blazor.Client` must not reference Domain/Application/Infrastructure/Persistence; backend data flows only through generated `IEventApiClient` (QUICK_REFERENCE #23; memory: NU1605/WASM).
2. Per-resource action affordances (event edit/publish/attendees/etc.) are gated by HAL `_links` presence only (CRITICAL RULE #6 / QUICK_REFERENCE #21). Role/capability data may gate only broad workspace/menu eligibility.
3. New API endpoint follows the controller-authoring standard: explicit route template, `Name = RouteNames.X`, `[EndpointClassification]`, `[ProducesResponseType]`, thin action dispatching MediatR (GOVERNANCE API Contract Rules; `.claude/rules/api-controllers.md`).
4. Handler conventions: repository returns entities, manual validator instantiation, `BaseCommandResponse` for commands (read-only here, so queries return DTOs), CancellationToken flow (`.claude/rules/application-layer.md`).
5. `EventApiClient.g.cs` is never hand-edited; regeneration is a discrete tracked step (GOVERNANCE).
6. Dock invariants: descriptor-owned width/persistence, viewport changes never autosave, logical `Start/End` sides only, no central panel enum, no page-level shell compensation hacks (`docs/DOCK_LAYOUT.md`, `DockLayoutArchitectureTests`).
7. Accessibility invariants: skip link, `<main>`/`<header>`/`<nav aria-label>` landmarks, ARIA live regions, `<h1>` per routable page, logical CSS properties (architecture tests).
8. Governance settings must be registered in `SettingRegistry` with explicit allowed scopes; user-overridable shell defaults must respect instance locks; `experience`/`allow_user_override` style keys are tenant/instance-only, layout state is user-only (report §11 + existing cascade).
9. Personal Settings origin is trusted only when captured from `NavigationManager.LocationChanged` in the current scoped `UiShellState`; no caller-provided `returnUrl`, route parameter, query parameter, or browser-persisted origin may drive return navigation.
10. New C# files: file-scoped namespaces + two-line ABOUTME header. Every `.razor` gets a BEM `.razor.css`.
11. Development mode: **no compatibility shims** — removed routes, bridge services, and renamed panel ids are deleted, not aliased. Obsolete tests are deleted, not skipped (docs/TESTING.md disabled-test governance).

## 5. Architecture And Design Decisions

### D1 — The app rail is permanent shell chrome, not a dock panel
- **Why:** `DockSide.Start` stacking is tab-oriented (`shell.left-nav` uses `StackStrategy.Tabbed`); two start panels would compete as tabs, never render as `rail + sidebar`. The rail must not be closable/resizable/persisted.
- **Alternatives:** (a) second Start dock panel — rejected, wrong stacking semantics; (b) new `DockMode.Rail` — rejected, pollutes a generic engine with app-specific chrome.
- **Consequences:** `MainLayout` gains a shell grid track owning `AppWorkspaceRail` before `DockLayoutHost`; `DockLayoutArchitectureTests` "no shell compensation hacks" guard must recognize the rail as sanctioned shell chrome (test update in Phase 1).
- **Files:** `MainLayout.razor(.cs/.css)`, new `Components/Shell/AppWorkspaceRail.razor(.cs/.css)`.

### D2 — Compile-time workspace registry + route-derived active workspace, with one contextual utility exception
- **Why:** report §5/§15; route is the only truthful activity source; registry gives a stable extension point without runtime plugins.
- **Alternatives:** booleans in a scoped service (rejected — loses deep links/refresh truth); per-page layout attributes (rejected — Blazouter routes are table-driven, not attribute-driven).
- **Consequences:** Phase 1 creates `Services/Shell/` with `WorkspaceDescriptor` (`Key`, `LabelKey`, `Icon`, `BaseRoute`, `RequiresAuthentication`), `IWorkspaceRegistry`/`WorkspaceRegistry`, `WorkspaceRouteClassifier` (longest-prefix match over registered base routes), and `UiShellState` (active workspace + last-route-per-workspace session map). Phase 6 extends `UiShellState` with `IsPersonalSettingsOpen` and a session-only Personal Settings origin: an in-session transition from Explore/Studio/AI to `/settings/personal/**` preserves the origin as `ActiveWorkspace`, while initialization/refresh on that route has no origin and uses Settings. Navigating elsewhere clears the utility state. Phase 7 adds effective navigation-mode policy/preferences; it does not persist a Personal return route.
- **Files:** new `src/Explore.Blazor.Client/Services/Shell/*.cs`; `Routes.razor` untouched for classification (prefix-based).

### D3 — Rename `shell.left-nav` → `shell.workspace-nav`; content becomes provider-driven
- **Why:** the panel stops being "the seeker sidebar" and becomes the single contextual navigation surface; provider-per-workspace avoids a NavMenu-style god component.
- **Alternatives:** keep id and swap content only — rejected: persisted snapshots and tests would keep a misleading id forever; dev mode allows the clean rename (stale localStorage snapshots are ignored by design: "unknown panel ids" are dropped on restore).
- **Consequences:** `WorkspaceNavigationHost.razor` renders the active `IWorkspaceNavigationProvider` fragment; `AppSideNav` content moves to `EventsWorkspaceNavigation`; `SidebarState` bridge is deleted after consumer migration (D6).
- **Files:** `ShellDockPanels.cs`, new `Components/Shell/Workspaces/*.razor`, `MainLayout.razor.cs`, dock tests.

### D4 — One authenticated shell-context endpoint; anonymous stays on public-experience shell
- **Why:** report §14; capabilities must be server-authoritative; anonymous surface must not leak memberships.
- **Alternatives:** keep client-side aggregation of 4+ calls (rejected — races, role-inference, N requests); extend `PublicExperienceShellDto` (rejected — anonymous cacheable contract must not grow private data).
- **Consequences:** Application: `GetUiShellContextRequest` + `UiShellContextDto` (workspaces availability, managed actors, settings scopes, navigation defaults) composing existing sources (`AdminAuthorityDto` logic, my-orgs/groups with management authority, event-creation eligibility, AI enablement, deployment mode). API: `UiShellController` — `[Route("api/ui-shell")]`, `[HttpGet("context", Name = RouteNames.GetUiShellContext)]`, `[Authorize]`, `[EndpointClassification(EndpointClass.Authenticated)]`, `[PrivateNoStore]` (per-user, no HTTP caching), plain DTO (no HAL: it is a capability projection, not an addressable resource — same posture as `PublicExperienceController`).
- **Files:** new Application feature folder `Features/UiShell/`, new controller, `RouteNames.cs`, OpenAPI + NSwag regen, new client `IUiShellContextService`.

### D5 — Studio event-level navigation consumes event HAL links
- **Why:** CRITICAL RULE #6; the API already emits `edit`, `publish-readiness`, session/registration/team relations; MCP `event_management_context` already derives capabilities from `_links` — same pattern client-side.
- **Alternatives:** capability strings in shell context per event — rejected: shell context is workspace-level; per-resource truth is the resource's own `_links`.
- **Consequences:** `StudioEventNavigation` receives the loaded event `HalResource` and renders only sections whose relation exists; no third sidebar — event navigation replaces actor navigation content within `shell.workspace-nav`.

### D6 — Delete `SidebarState`; keep `AiAssistantState` as policy state
- **Why:** `SidebarState` is a pure dock mirror (open/has-sidebar booleans) — all six consumers can read `DockLayoutState`/`UiShellState` directly. `AiAssistantState` is NOT just a bridge: it computes effective AI availability from tenant policy + auth + user preference and is needed by both dock and `/ai` workspace gating.
- **Consequences:** consumer migration + service removal + DI cleanup + `docs/DOCK_LAYOUT.md` update (its "keep compatibility services" sentence becomes obsolete and is rewritten).

### D7 — Durable layout preferences reuse `UserPreference` via the existing user-settings API
- **Why:** `(tenant_id, user_id, setting_key)` storage, batch GET/PUT endpoints, and the `AiAssistantPreferences` category pattern already exist; `DockLayoutSnapshot` is schema-versioned and restore is defensive (clamping, unknown-id dropping).
- **Alternatives:** new `/api/me/ui-preferences` endpoint pair (rejected — duplicates `api/settings/user/{category}`); new table (rejected — `UserPreference` is exactly this).
- **Consequences:** new preference keys `ui_shell_preferences.layout.v1` (JSON snapshot envelope), `ui_shell_preferences.last_workspace`, `ui_shell_preferences.last_actor` registered in `GovernanceSettingKeys` + `SettingRegistry` (User scope only); client `ServerBackedDockLayoutPersistence` decorating the localStorage implementation (anonymous = tenant-discriminated localStorage key `dock_layout:v1:{tenantSlug}:` with no old-key compatibility read; authenticated = server, promote local on first login); existing autosave rules unchanged (UserAction/Reset only, 500ms debounce). Phase 7 also owns optional durable `ui.settings.last_scope.v1` persistence for the dedicated hub/scope selector; it stores only a normalized authorized scope key, never the Personal return route, and falls back to Personal when the scope is absent or revoked.

### D8 — Tenant shell defaults are governance settings under `ui_shell.*`
- **Why:** the 5-tier cascade, locks, and single-tenant bypass already exist; report §11 requires explicit allowed scopes per key.
- **Keys (tenant/instance scope, instance-lockable):** `ui_shell.rail_public_visibility` (`AuthenticatedOnly|Always`), `ui_shell.default_nav_mode.events|studio|ai` (`Docked|Collapsed`), `ui_shell.allow_user_nav_override` (bool), `ui_shell.organizer_default_workspace` (`Events|Studio`).
- **Consequences:** `GovernanceSettingKeys` + `SettingRegistry` additions must be registered before the shell-context handler finalizes (Phase 0 registration); `GetUiShellContextRequest` resolves them into `navigationDefaults`; anonymous rail visibility resolved into the public-experience shell response for the org-centric public case; tenant admin settings page gains a small "Shell" section; `docs/CONFIGURATION.md` documents keys. Phase 0 already registered `ui_shell.default_nav_mode.settings` under the older dedicated-sidebar design; Task 7.1 removes that unused definition/documentation rather than creating a Settings provider solely to consume it.

### D9 — Hybrid Settings: contextual Personal utility plus dedicated administrative hub
- **Why:** Personal preferences are frequently opened mid-task and should not erase Explore/Studio/AI context, while administrative configuration needs a stable, deep-linkable destination with explicit scope. One shell-level scope sidebar would duplicate the substantial navigation already owned by the existing admin layouts.
- **Interaction model:** the rail contains one bottom-pinned gear. Its primary action always opens `/settings/personal`, never the last administrative scope. When more than Personal is authorized, a gear menu and the profile dropdown expose the dedicated hub and authorized administrative scopes. The gear receives a distinct utility-open visual/data state during contextual Personal Settings, but only the originating workspace keeps `aria-current="page"`.
- **Personal presentation:** in-session navigation from Explore/Studio/AI preserves that workspace and its `shell.workspace-nav`; `SettingsLayout` supplies a compact top-level section bar and a safe return action backed by session-only `UiShellState`. A direct load/refresh of a Personal route renders as dedicated Settings because no origin is inferred. No `returnUrl` is accepted. The empty `SettingsWorkspaceNavigation` registration/component/CSS is removed.
- **Dedicated presentation:** `/settings` is a scope hub with a shared `SettingsScopeSelector`. Organization, Group, Tenant, and Instance pages mount the existing guarded settings components and retain their existing internal sidebars. The scope selector changes scope; it does not duplicate each layout's section navigation.
- **Routes:** `/settings`, `/settings/personal`, `/settings/personal/:section`, `/settings/organization/:id`, `/settings/group/:id`, `/settings/admin`, and `/settings/instance`. Existing `?section=` Personal links migrate to path sections. `/admin/tenant/navigation` producers migrate to the Tenant settings page's navigation section rather than creating a second canonical route. Old `/admin/*/settings` central rows and the old navigation `@page` directive are deleted; all producers are updated by a bounded sweep. No aliases; guards remain unchanged.
- **Authority and single-tenant presentation:** `SettingsScopeDto` from `IUiShellContextService` is the only scope-menu source. Personal is always present for authenticated users; organization/group/tenant/instance entries appear only when returned by the server. Instance authority never implies Tenant authority. In single-tenant mode, callers holding both authorities see one "Site administration" presentation entry that composes links to the still-separate guarded Tenant and Instance routes/API boundaries.
- **Persistence boundary:** Phase 6 stores only the Personal origin in scoped memory. Durable `ui.settings.last_scope.v1` is owned by Task 7.2 and may influence the dedicated hub/scope selector only after revalidation; it never changes the gear's Personal primary action.

### D10 — AI dual experience shares one conversation stack
- **Why:** report §2/anti-pattern #9; `AiAssistantConversationState` + `IAiAssistantClientService` are scoped services already consumed by the rail — pages can consume the same instances in the same circuit/runtime scope.
- **Consequences:** new pages `Pages/Ai/AiWorkspace.razor` (+ `AiConversationPage`) reuse rail sub-components (message list, composer, proposed-action cards are extracted into shared components under `Components/Shell/AiAssistant/` where not already reusable); dock header gets "Open in AI workspace" navigating to `/ai/chats/{id}`; `AiWorkspaceNavigation` lists conversations (existing `GetAiConversations`).

### D11 — Personal Settings uses one explicit entry contract and a searchable vertical information architecture
- **Why:** Browser evidence shows the rail gear preserves the Events origin while the profile-dropdown Personal Settings anchor can lose it. The current page relies on `LocationChanged` timing, always renders the scope selector, exposes a wrapping horizontal section bar, and mounts only one section at a time.
- **Entry contract:** `UiShellState` gains one explicit Personal Settings navigation method that captures a live Events/Studio/AI origin before navigating. Every first-party Personal Settings entry point (rail gear, profile dropdown, appearance quick switcher, registration connected-app links, and future links) routes through that contract instead of relying on anchor interception timing. New-tab/direct loads remain dedicated Settings because session origin is intentionally not durable.
- **Information architecture:** `/settings/personal` becomes **View all** and renders every Personal Settings section in canonical order. `/settings/personal/:section` remains the focused deep-link form. The vertical navigation starts with View all, then Personal info, Security, Privacy, Notifications, Connected apps, Appearance, AI Assistant, API keys, and Webhooks. No compatibility aliases are added for renamed section slugs.
- **Search:** Search is local, case-insensitive, and metadata-driven over section labels and declared keywords. It filters the View all section cards without mutating the URL or searching hidden DOM text. Empty results render visible status text and are announced politely.
- **Layout:** Desktop uses a two-column CSS grid with sticky vertical navigation below the navbar and a scrollable content column. Narrow layouts stack the navigation above content and disable stickiness; semantic links, focus order, logical CSS, reduced motion, and target sizes remain intact.
- **Scope selector:** hide `SettingsScopeSelector` entirely when Personal is the only available scope. When administrative scopes exist, retain the server-authoritative selector above the Personal Settings layout; it remains scope navigation, not section navigation.

## 6. Implementation Phases

> Effort scale: S ≤ ~1h, M ≤ ~half day, L ≤ ~day, XL > day (per task, for a competent implementation agent).

### Phase 0: Re-baseline and Governance Foundation 🟡 IN PROGRESS
- **Goal:** Re-baseline the workstream against current repository state; register governance setting definitions so shell-context handler can resolve them.
- **Depends on:** —
- **Relevant files:** `src/Explore.Domain/Constants/GovernanceSettingKeys.cs`, `src/Explore.Domain/Settings/SettingRegistry.cs` (+ definitions files), `docs/CONFIGURATION.md`.
- **Acceptance criteria:** governance keys `ui_shell.*` (tenant/instance) and `ui_shell_preferences.*` (user) are registered with explicit allowed scopes and defaults; registry parity tests green after implementation.

#### Task 0.1: Re-baseline workstream against repository state
- **Type:** verify | **Layer:** Docs | **Effort:** S
- **Description:** Re-read `Routes.razor`, `ShellDockPanels.cs`, `NavMenu.razor.cs`, `EventService.cs`, `GovernanceSettingKeys.cs`, `SettingRegistry.cs`, and confirm plan/context/tasks reflect current source. Record findings. Note: AnySearch MCP was unavailable during original planning; fallback evidence sources were official Plane/W3C docs plus Context7 MudBlazor docs.
- **Acceptance:** [ ] all evidence claims in plan §2.1 re-verified; [ ] `/admin/tenant/navigation` confirmed as `@page` directive, not `Routes.razor` row; [ ] `GetManagedEventsByActorAsync` confirmed in generated client.
- **Dependencies:** —

#### Task 0.2: Governance setting definitions registration
- **Type:** create/modify | **Layer:** Domain | **Effort:** M
- **Files:** modify `src/Explore.Domain/Constants/GovernanceSettingKeys.cs`, `src/Explore.Domain/Settings/SettingRegistry.cs` (+ definitions file per existing layout), `docs/CONFIGURATION.md`.
- **Description:** Add D8 keys (`ui_shell.rail_public_visibility`, `ui_shell.default_nav_mode.*`, `ui_shell.allow_user_nav_override`, `ui_shell.organizer_default_workspace`) and D7 user-preference keys (`ui_shell_preferences.layout.v1`, `.last_workspace`, `.last_actor`) with explicit allowed scopes, defaults, and instance-lockability. Follow existing definition-file layout (e.g. `UiShellSettingDefinitions`) and document every key.
- **Acceptance:** [ ] a failing-first registry test covers one representative key; [ ] registry parity/architecture tests green; [ ] lock and single-tenant bypass semantics verifiable for one representative key; [ ] `docs/CONFIGURATION.md` documents every new key; [ ] definitions exist before shell-context handler consumes them.
- **Dependencies:** 0.1

### Phase 1: Workspace Shell Foundation — Rail, Registry, ADR
- **Goal:** Permanent app rail + workspace registry/classifier with behavior parity; Explore and Settings destinations only; ADR-019 records the composition.
- **Depends on:** Task 0.1. Tasks 1.1 and 1.2 may run alongside Task 0.2 in Wave 1; Phase 1 verification waits for Phase 0 verification.
- **Relevant files:** existing `MainLayout.razor(.cs/.css)`, `Routes.razor` (read-only), `tests/Event.Architecture.Tests/DockLayoutArchitectureTests.cs`; new `Services/Shell/{WorkspaceDescriptor,WorkspaceKey,IWorkspaceRegistry,WorkspaceRegistry,WorkspaceRouteClassifier,UiShellState}.cs`, `Components/Shell/AppWorkspaceRail.razor(.cs/.css)`, `docs/adr/ADR-019-workspace-shell-composition.md`.
- **Related skills/rules:** `blazor-ui-conventions`, `blazor-css-isolation`, `.claude/rules/blazor-client.md`.
- **Acceptance criteria:** rail renders on all `MainLayout` routes and never on `SetupLayout` routes; active item derives from URL (deep link + refresh correct); rail items have accessible labels, tooltips, `aria-current`, visible focus; Settings sits at the bottom after flexible space; logical properties only (RTL-ready); existing dock panels and EventList workspace docks behave unchanged; architecture + client dock tests green.

#### Task 1.1: ADR-019 workspace shell composition and vocabulary
- **Type:** create | **Layer:** Docs | **Effort:** S
- **Files:** new `docs/adr/ADR-019-workspace-shell-composition.md`
- **Description:** Record decisions D1–D3 (+ glossary: workspace, app rail, workspace navigation, contextual dock, experience profile → maps to `PublicExperienceMode`, acting actor, settings scope, policy vs preference). Follow existing ADR format (see ADR-010 for depth). Link from `docs/DOCK_LAYOUT.md` and `docs/BLAZOR.md` in the phase that edits them (Phase 2).
- **Acceptance:** [ ] ADR exists with status Accepted, decision, consequences, alternatives; [ ] glossary terms match the ones used in code.
- **Dependencies:** —

#### Task 1.2: Workspace registry, classifier, and shell state
- **Type:** create | **Layer:** Blazor | **Effort:** M
- **Files:** new `src/Explore.Blazor.Client/Services/Shell/WorkspaceKey.cs`, `WorkspaceDescriptor.cs`, `IWorkspaceRegistry.cs`, `WorkspaceRegistry.cs`, `WorkspaceRouteClassifier.cs`, `UiShellState.cs`; modify `Extensions/ServiceCollectionExtensions.cs` (scoped registrations).
- **Description:** `WorkspaceKey` is a strongly-typed key (`events`, `studio`, `ai`, `settings`) — string-backed record like `DockPanelId`, no central enum growth beyond the registry itself. `WorkspaceRegistry` registers Events (`BaseRoute="/"` prefixes: `/`, `/home`, `/events`, `/organization`, `/group`, `/my`, `/notifications`, plus public info pages) and Settings (`/settings`) in this phase. `WorkspaceRouteClassifier` resolves workspace by longest-prefix match; unknown routes fall back to Events. `UiShellState` subscribes to `NavigationManager.LocationChanged`, exposes `ActiveWorkspace`, per-workspace last-route map (session-only), and raises change events. bUnit tests for classifier precedence and state transitions.
- **Acceptance:** [ ] classifier resolves every current route in `Routes.razor` to the expected workspace (table-driven test); [ ] last-route map restores `/events/search?...`-style URLs on workspace re-entry.
- **Dependencies:** —

#### Task 1.3: `AppWorkspaceRail` + MainLayout shell track + minimal mobile bottom navigation
- **Type:** create/modify | **Layer:** Blazor | **Effort:** L
- **Files:** new `Components/Shell/AppWorkspaceRail.razor(.cs/.css)`; modify `Layout/MainLayout.razor(.cs/.css)`, `Components/Docking/DockSideHost.razor.css`, and `src/Explore.Blazor/wwwroot/css/components.css`; modify `tests/Event.Architecture.Tests/DockLayoutArchitectureTests.cs` (sanction the rail as shell chrome and verify the shared Start inset); new bUnit tests.
- **Description:** Wrap `DockLayoutHost` with a shell grid whose inline-start track is the rail (`inline-size: 64px`, logical properties, `<nav aria-label="Application workspaces">`). Rail renders registry workspaces filtered by availability (this phase: Events always; Settings authenticated-only via `AuthenticationStateProvider`), Settings pinned to block-end. Items are real links (`href`) with icon + tooltip + visually-hidden label + `aria-current="page"`. Hide-chrome routes unaffected (rail lives inside the `!_hideChrome` branch). Do not register the rail with `DockLayoutState`. **Mobile:** at `Breakpoint.Xs` the rail projects to a minimal bottom navigation (Events | Studio | AI | Settings, availability-filtered) via CSS/media-query only; no start track; workspace nav becomes temporary drawer (existing dock projection).
- **Acceptance:** [ ] rail visible with docked nav + AI dock simultaneously at 1280px without breaking EventList workspace docks; [ ] keyboard traversal and focus ring verified in bUnit markup assertions; [ ] `DockLayoutArchitectureTests` green with explicit shell-chrome, shared-inset, and Xs CSS-contract coverage; [ ] bUnit proves workspace availability filtering (bUnit does not evaluate media queries); final rendered breakpoint evidence remains in Phase 9.
- **Dependencies:** 1.1, 1.2

### Phase 1 Verification (run once)
- `dotnet build --configuration Release --verbosity quiet`
- `dotnet test --project tests/Explore.Blazor.Client.Tests/Explore.Blazor.Client.Tests.csproj --configuration Release --verbosity quiet`
- **Rollback:** rail is additive chrome; revert `MainLayout` grid change to restore the previous shell. No persisted-state impact (rail persists nothing).

### Phase 2: Contextual Workspace Navigation
- **Goal:** `shell.left-nav` becomes `shell.workspace-nav` hosting per-workspace navigation providers; `SidebarState` bridge deleted; docs updated.
- **Depends on:** Phase 1
- **Relevant files:** existing `ShellDockPanels.cs`, `MainLayout.razor(.cs)`, `AppSideNav.razor(.cs/.css)`, `NavMenu.razor.cs`, `Services/SidebarState.cs`, `Extensions/ServiceCollectionExtensions.cs`, `Pages/Admin/Tenant/Components/TenantAdminSettingsLayout.razor`, `Pages/Admin/Instance/Components/InstanceAdminSettingsLayout.razor`, `docs/DOCK_LAYOUT.md`; new `Contracts/Services/Shell/IWorkspaceNavigationProvider.cs`, `Components/Shell/WorkspaceNavigationHost.razor(.cs/.css)`, `Components/Shell/Workspaces/EventsWorkspaceNavigation.razor(.cs/.css)`, `Components/Shell/Workspaces/SettingsWorkspaceNavigation.razor(.cs/.css)` (minimal placeholder linking existing settings entries).
- **Related skills/rules:** `blazor-ui-conventions`, `.claude/rules/blazor-client.md`, `docs/DOCK_LAYOUT.md`.
- **Acceptance criteria:** panel id `shell.workspace-nav` replaces `shell.left-nav` everywhere (code, tests, docs) with no alias; Events workspace shows the current discovery nav (content parity with `AppSideNav`, incl. org-centric branch and tenant quick links); the temporary Settings placeholder proves provider swapping and is explicitly removed by Task 6.1 after the hybrid architecture is locked; workspaces without nav render no start panel; `SidebarState` type no longer exists; `rg "shell.left-nav|SidebarState"` returns only migration-note hits in dev docs.

#### Task 2.1: Navigation provider contract + host
- **Type:** create | **Layer:** Blazor | **Effort:** M
- **Files:** new `Contracts/Services/Shell/IWorkspaceNavigationProvider.cs`, `Components/Shell/WorkspaceNavigationHost.razor(.cs/.css)`; modify `WorkspaceDescriptor` (nav provider reference), `MainLayout.razor.cs` (panel content = host).
- **Description:** Provider exposes `HasNavigation` and a `RenderFragment` (aria-label per workspace, e.g. "Event discovery navigation"). Host subscribes to `UiShellState`, swaps content on workspace change, and closes/hides the start panel when the active workspace has no navigation. Overlay header behavior (close button + brand) moves from `AppSideNav` into the host so every provider inherits it.
- **Acceptance:** [ ] switching Events→Settings swaps nav content without panel re-registration; [ ] no-nav workspace leaves canvas full width.
- **Dependencies:** 1.2, 1.3

#### Task 2.2: Rename panel id and migrate `AppSideNav` → `EventsWorkspaceNavigation`
- **Type:** modify/create/delete | **Layer:** Blazor | **Effort:** M
- **Files:** modify `ShellDockPanels.cs` (`WorkspaceNavId = new("shell.workspace-nav")`); new `Components/Shell/Workspaces/EventsWorkspaceNavigation.razor(.cs/.css)` (content moved from `AppSideNav`); delete `AppSideNav.razor(.cs/.css)`; update `Explore.Blazor.Client.Tests` dock/registration/nav tests.
- **Description:** Pure content move — keep `PublicExperienceService`/`TenantNavLinksState` wiring, org-centric branch, quick links. Old persisted `shell.left-nav` snapshot entries are dropped automatically by defensive restore (verified behavior).
- **Acceptance:** [ ] no `shell.left-nav` or `AppSideNav` references outside dev docs; [ ] Events nav renders identically (bUnit content assertions).
- **Dependencies:** 2.1

#### Task 2.3: Delete `SidebarState`; migrate consumers; update docs
- **Type:** modify/delete | **Layer:** Blazor + Docs | **Effort:** M
- **Files:** delete `Services/SidebarState.cs`; modify `NavMenu.razor(.cs)` (toggle via `DockLayoutState.Toggle(ShellDockPanels.WorkspaceNavId)` — already partially true), `MainLayout.razor.cs` (remove mirroring), `TenantAdminSettingsLayout.razor`, `InstanceAdminSettingsLayout.razor`, `ServiceCollectionExtensions.cs`, affected tests; modify `docs/DOCK_LAYOUT.md` (panel table, id rename, bridge-sentence rewrite, ADR-019 link) and `docs/BLAZOR.md` service/state table.
- **Description:** Replace every `SidebarState` read with `DockLayoutState`/`UiShellState`. `AiAssistantState` stays (policy state, D6). Delete obsolete bridge tests instead of skipping them (TESTING.md governance).
- **Acceptance:** [ ] solution has zero `SidebarState` references; [ ] docs match shipped panel catalog.
- **Dependencies:** 2.2

### Phase 2 Verification (run once)
- `dotnet build --configuration Release --verbosity quiet`
- `dotnet test --project tests/Event.Architecture.Tests/Event.Architecture.Tests.csproj --configuration Release --verbosity quiet`
- **Rollback:** revert the rename commit; localStorage snapshots are forward-compatible in both directions because unknown ids are ignored.

### Phase 3: Server-Authoritative UI Shell Context
- **Goal:** One authenticated endpoint returns workspace availability, managed actors, settings scopes, and navigation defaults; rail/nav gate from it.
- **Depends on:** Phase 2 (consumers exist)
- **Relevant files:** existing `Features/PublicExperience/**` (pattern), `UserController.cs::GetAdminAuthority`, `EventController.cs::GetMyEvents`, `OrganizationController`/`GroupController` my-lists, `GovernanceSettingKeys.cs`, `RouteNames.cs`, `schemas/openapi_islamu-event.json`, `Clients/EventApiClient.g.cs`; new `src/Explore.Application/Features/UiShell/Requests/Queries/GetUiShellContextRequest.cs`, `Handlers/Queries/GetUiShellContextRequestHandler.cs`, `src/Explore.Application/DTOs/UiShell/UiShellContextDto.cs` (+ nested `ManagedActorDto`, `SettingsScopeDto`, `WorkspaceAvailabilityDto`), new `src/Explore.API/Controllers/UiShellController.cs`, new client `Contracts/Services/Shell/IUiShellContextService.cs` + `Services/Shell/UiShellContextService.cs`.
- **Related skills/rules:** `cqrs-mediatr-guidelines`, `auth-patterns`, `.claude/rules/application-layer.md`, `.claude/rules/api-controllers.md`.
- **Acceptance criteria:** endpoint returns 401 anonymous; authenticated response contains only the caller's authorities (instance admin without org membership gets NO Studio and NO tenant scope — report §6 rules 1–2 encoded in handler tests); Studio availability = at least one managed actor OR personal event-creation eligibility; settings scopes = Personal + each org/group with admin authority + Tenant (tenant-admin) + Instance (instance-admin); response carries resolved `ui_shell` navigation defaults from the registered governance keys (Phase 0.2); handler falls back to safe defaults when keys are unset.

#### Task 3.1: Application query + DTO + handler (group-publisher resolved; managed actors reuse `IAiAssistantActorContextService`)
- **Type:** create/investigate | **Layer:** Application | **Effort:** L
- **Description:** Compose existing repositories/services already used by `GetAdminAuthorityRequest`, my-organizations/groups, and event-creation eligibility — do not duplicate their logic; extract shared internals only if reuse requires it (no cross-feature reach-ins). **Managed actors:** reuse `IAiAssistantActorContextService.ListAuthorizedActorContextsAsync` to produce the actor list (organization + group actors) for Studio navigation; personal events fallback uses `GetMyEventsRequest`. **Group-publisher investigation (§2.6):** resolved — group actors are included in the managed-actor list; event creation for groups uses the existing `/events/create` publisher picker (no new group-specific create route). No HybridCache introduction; capability aggregation is computed per request (drift tolerance documented). Unit tests: instance-admin-only, tenant-admin-only, organizer, seeker, multi-role union, org-centric pinned actor. **Failing-first:** write a test that asserts `StudioWorkspaceAvailability = false` for an instance-admin-only principal before the handler implements the rule.
- **Acceptance:** [ ] handler tests cover the eight report §6 scenarios that are representable today; [ ] no repository returns DTOs; [ ] CancellationToken flows; [ ] `IAiAssistantActorContextService` is the single source of managed actors; [ ] group-publisher finding recorded in context.
- **Dependencies:** 0.2

#### Task 3.2: API controller + route name + OpenAPI/NSwag regeneration (exact method name verified after regen)
- **Type:** create/modify | **Layer:** API | **Effort:** M
- **Files:** new `UiShellController.cs`; modify `RouteNames.cs`, `docs/API.md`, `docs/API_CHANGELOG.md`; regenerate `schemas/openapi_islamu-event.json` + `EventApiClient.g.cs` via the documented msbuild target (`dotnet msbuild src/Explore.Blazor.Client/Explore.Blazor.Client.csproj /t:GenerateApiClient ...`).
- **Description:** `[Route("api/ui-shell")]`, `GET context`, `Name = RouteNames.GetUiShellContext`, `[Authorize]`, `[EndpointClassification(EndpointClass.Authenticated)]`, `[PrivateNoStore]`, `[ProducesResponseType]` for 200/401, `authenticated` rate-limit family (default), no output cache. OperationId `GetUiShellContext` follows the generated-client naming architecture rule and emits `GetUiShellContextAsync`. **NSwag:** exact generated method name is verified after documented regeneration rather than assumed; if the name differs, update the plan/context immediately. Generated OpenAPI/client files are one serialized lane (coordinate with webhook workstream to avoid collision).
- **Acceptance:** [ ] controller passes contract architecture tests; [ ] generated client exposes the verified method name with no banned names; [ ] API changelog entry added; [ ] dirty-file hunk preservation controls are explicit (unrelated dirty files must not be committed with the regen).
- **Dependencies:** 3.1

#### Task 3.3: Client service + rail/nav gating
- **Type:** create/modify | **Layer:** Blazor | **Effort:** M
- **Files:** new `IUiShellContextService`/`UiShellContextService` (5-min cached like `PublicExperienceService`, invalidated on `CurrentUserState.OnChanged`); modify `WorkspaceRegistry` availability delegates, `AppWorkspaceRail`, `NavMenu.razor.cs` (replace the four ad-hoc authority loads used for menu gating with shell context; keep `EventCreationEligibilityService` only where per-action semantics are needed), `UiShellState` (revocation rule: stored last-workspace falls back to Events when no longer available).
- **Acceptance:** [ ] anonymous shell never calls the authenticated endpoint; [ ] Studio rail item appears only per server data; [ ] a revoked workspace preference silently falls back (bUnit test).
- **Dependencies:** 3.2

### Phase 3 Verification (run once)
- `dotnet build --configuration Release --verbosity quiet`
- `dotnet test --project tests/Event.Application.UnitTests/Event.Application.UnitTests.csproj --configuration Release --verbosity quiet`
- **Rollback:** endpoint is additive; client falls back to previous role-based gating by reverting Task 3.3 changes. Contract regen produces a serialized/isolated generated-artifact diff; no commit unless explicitly requested.

### Phase 4: Studio Workspace
- **Goal:** `/studio` route group with actor-level navigation + dashboard + events list; event-level navigation gated by HAL links; create/edit/session flows reachable from Studio.
- **Depends on:** Phase 3
- **Relevant files:** existing `Routes.razor`, `Pages/Events/{CreateEvent,EventEdit}.razor`, session pages, `EventController` HAL policies (read-only), `NavMenu.razor(.cs)` (Add Event resolution); new `Pages/Studio/StudioDashboard.razor(.cs/.css)`, `Pages/Studio/StudioEvents.razor(.cs/.css)`, `Pages/Studio/StudioEventShell.razor(.cs/.css)` (loads the event resource once, cascades it), `Components/Shell/Workspaces/StudioWorkspaceNavigation.razor(.cs/.css)`, `Components/Shell/Workspaces/StudioEventNavigation.razor(.cs/.css)`, `Components/Shell/Workspaces/StudioActorSwitcher.razor(.cs/.css)`.
- **Related skills/rules:** `blazor-ui-conventions`, `.claude/rules/blazor-client.md`, `.claude/rules/api-hateoas.md` (consume-side).
- **Acceptance criteria:** `/studio` (dashboard), `/studio/events`, `/studio/events/:eventId` (+ `/schedule`, `/registration`, `/publication` section routes as shells) registered with `AuthenticatedRouteGuard`; actor switcher lists only shell-context managed actors and is pinned/locked when `public_experience.primary_organization_id` matches (org-centric); event navigation renders a section link only when the event resource `_links` contains the mapped relation; "← All events" returns to actor level (content replacement, never a third sidebar); Add Event action resolves per report §16 (organizer → `/studio` create for actor; personal → personal create; none → hidden) using eligibility + shell context; existing `/events/create`, `/events/:id/edit`, and session routes remain the working editors (Studio links to them; route moves are NOT part of this phase); reported-event/user-submission surfaces are NOT added to Studio (report §9 — deferred).
- **Phase-end verification (run once):**
  - `dotnet build --configuration Release --verbosity quiet`
  - `dotnet test --project tests/Explore.Blazor.Client.Tests/Explore.Blazor.Client.Tests.csproj --configuration Release --verbosity quiet` (justified repeat: all new Studio components are bUnit-covered here)
- **Rollback / failure handling:** Studio routes are additive rows in `Routes.razor`; removing them restores the previous experience; no API change in this phase.
- **Phase verification result (2026-07-22):** Canonical Release build passed with 0 errors. The full Blazor client suite reproduced the three previously recorded unrelated failures (1 generated EventLocation HAL typing contract and 2 report-dialog rendering tests): 1,932 total, 1,928 passed, 3 failed, 1 governed skip. Phase 4 implementation is complete; verification remains blocked without broadening scope.

#### Task 4.1: Studio routes + workspace registration + actor-level navigation
- **Type:** create/modify | **Layer:** Blazor | **Effort:** L
- **Description:** Register Studio in `WorkspaceRegistry` (available per shell context), add route rows, build `StudioWorkspaceNavigation` (Overview, Events; other report §8 sections rendered only when their target exists — no dead links) with `StudioActorSwitcher` header (logo, name, switcher when >1 actor; last actor kept in `UiShellState`, durable persistence arrives in Phase 7).
- **Acceptance:** [x] deep link `/studio/events` renders Studio nav + active state; [x] single-actor and pinned-actor modes render identity header without switcher.
- **Validation:** Focused route/state/switcher/navigation/page lanes pass 34/34; `/studio/events` exposes `aria-current="page"`; Release build passes with zero errors; `git diff --check` is clean; Oracle passed actor authorization/reconciliation, pinned locking, authenticated guards, server-context authority, active navigation, accessibility, and test coverage with no required fixes.
- **Dependencies:** —

#### Task 4.2: Studio dashboard + events list (actor-scoped via `GetManagedEventsByActorAsync`)
- **Type:** create | **Layer:** Blazor | **Effort:** L
- **Description:** `StudioEvents` consumes the actor-scoped event list: organization/group actors use `GetManagedEventsByActorAsync(actorId)` (generated client method); personal/unscoped fallback uses `GetMyEventsAsync` (existing HAL collection). Dashboard shows actor identity + counts + quick actions gated by returned links. Row actions (edit/publish/…) rendered only from item `_links`. **Create action:** uses the existing `/events/create` publisher picker (no new group-specific create route).
- **Acceptance:** [x] list renders with HAL-gated row affordances (bUnit with fabricated `_links` variants); [x] empty state offers Create only when eligible and the collection exposes `create`; [x] `GetManagedEventsByActorAsync` called for org/group actors; [x] `GetMyEventsAsync` called for personal fallback.
- **Validation:** Baseline Studio pages passed 2/2 before the failing-first suite produced 10 expected failures. Green lanes pass 12/12 Studio pages, 69/69 event service, 3/3 actor switcher, and 9/9 navigation host. The client Release build passes with zero warnings/errors; diff, local-authority, and logical-CSS sweeps are clean. GPT Oracle confirmed actor routing, stale/disposal fences, HAL gates, focus restoration, accessibility, and scope boundaries with no required fixes.
- **Dependencies:** 4.1

#### Task 4.3: Event-level navigation shell (HAL-driven)
- **Type:** create | **Layer:** Blazor | **Effort:** L
- **Description:** `StudioEventShell` loads event detail once (existing detail service), cascades the resource; `StudioEventNavigation` replaces actor nav content: back link, title, status chip, and section links mapped `edit→Details`, `publish-readiness→Publication`, session relations→Schedule/Sessions, registration relations→Registration, team relation→Team, delete→Danger zone. Sections without implemented pages link to the existing editor surfaces (`/events/:id/edit` etc.). **Group events:** creation/editing flows through the existing `/events/create` publisher picker; no new group-specific route or editor.
- **Acceptance:** [x] section visibility flips with `_links` presence (table-driven bUnit test); [x] no role/claim inspection anywhere in Studio components (`rg "IsInRole" src/Explore.Blazor.Client/Pages/Studio` empty); [x] group event links target existing `/events/create` picker.
- **Validation:** Failing-first compilation stopped on the absent Task 4.3 types. Green lanes pass 8/8 event navigation, 1/1 shared event state, 10/10 workspace host, 17/17 routes, and 12/12 Studio regressions. The client Release build passes with 0 warnings/errors; scoped diff, authority, raw-GUID-link, and logical-CSS sweeps are clean. GPT-5.5 Oracle returned PASS with no required fixes.
- **Dependencies:** 4.1, 4.2

#### Task 4.4: Top-bar workspace awareness (search + primary action)
- **Type:** modify | **Layer:** Blazor | **Effort:** M
- **Description:** `NavMenu` consumes `UiShellState`: Studio active → search targets managed events (client-side filter on the Studio list route: navigate `/studio/events?q=`), primary action "Create" with visible acting-actor hint; Events active → current behavior; AI/Settings → hide global search (AI workspace nav owns conversation search later).
- **Acceptance:** [x] per-workspace search/action matrix covered by bUnit tests.
- **Validation:** The corrected failing-first matrix discovered four tests: Events stayed green while Studio routing and Settings suppression failed as expected. The final matrix passes 5/5, including anonymous Settings suppression and `bdi dir="auto"` actor isolation; the existing NavMenu admin/context lane passes 18/18. Client Release build passes with 0 warnings/errors, diagnostics/diff checks are clean, and GPT-5.5 Oracle follow-up returned PASS.
- **Dependencies:** 4.1

### Phase 5: AI Dual Experience
- **Goal:** Full `/ai` workspace sharing conversation state with the retained contextual dock.
- **Depends on:** Phase 3 (AI workspace availability from shell context/`AiAssistantState`)
- **Relevant files:** existing `AiAssistantRail.razor(.css)`, `Services/Ai/{AiAssistantClientService,AiAssistantConversationState}.cs`, `Components/Shell/AiAssistant/**`; new `Pages/Ai/AiWorkspace.razor(.cs/.css)`, `Pages/Ai/AiConversationPage.razor(.cs/.css)`, `Components/Shell/Workspaces/AiWorkspaceNavigation.razor(.cs/.css)`; modify `Routes.razor`, `WorkspaceRegistry`, `AiAssistantRail` (header "Open in AI workspace"), `NavMenu` (sparkle button unchanged as dock toggle).
- **Related skills/rules:** `blazor-ui-conventions`; AI proposal/confirmation invariants from `docs/OPERATIONS.md` (HAL-gated proposed actions, no client-side authority).
- **Acceptance criteria:** `/ai` and `/ai/chats/:conversationId` use `AuthenticatedRouteGuard`; anonymous AI access remains dock-only; one conversation history for dock and workspace (opening a dock conversation in the workspace shows identical messages without refetch drift); proposed-action cards keep HAL-gated confirm/reject in both surfaces; dock stays available on non-AI pages; workspace title uses product naming ("AI Assistant"), model id only in an info popover from bootstrap data; rail AI item visible only when `AiAssistantState.IsAvailable`.
- **Phase-end verification (run once):**
  - `dotnet build --configuration Release --verbosity quiet`
  - `dotnet test --project tests/Explore.Blazor.Client.Tests/Explore.Blazor.Client.Tests.csproj --configuration Release --verbosity quiet` (justified repeat: shared conversation components + new pages)
- **Rollback / failure handling:** dock experience is untouched baseline; removing `/ai` rows restores it fully.

#### Task 5.1: Extract shared conversation components
- **Type:** modify | **Layer:** Blazor | **Effort:** L
- **Description:** Factor message list, composer, reference picker usage, and action cards out of `AiAssistantRail` into reusable components under `Components/Shell/AiAssistant/` (several already exist: `AiProposedActionCard`, `AiReferencePicker`, `AiActionResultCard` — verify and reuse; extract only what the rail still inlines). Rail behavior must remain pixel/behavior-equivalent (existing full-panel bUnit suite is the guard).
- **Acceptance:** [x] `AiAssistantRail` bUnit suite green unchanged; [x] extracted components have no rail-specific assumptions.
- **Validation (2026-07-22):** Extracted reusable timeline/composer surfaces and moved every existing AI subcomponent's styles to its owning CSS-isolated file. Rail regression 21/21 and shared component lanes 15/15 pass; Blazor architecture 17/17 and canonical Release build pass with 0 errors. Static sweeps confirm no shell/service/dock dependency in the extracted components and no child-component CSS remains rail-owned. GPT-5.5 Oracle returned PASS after three review/fix cycles.
- **Dependencies:** —

#### Task 5.2: AI workspace pages + navigation + open-in-workspace (authenticated-only)
- **Type:** create/modify | **Layer:** Blazor | **Effort:** L
- **Description:** Pages compose shared components against the same scoped `AiAssistantConversationState`; `AiWorkspaceNavigation` = New conversation, Recent (list from state/service), conversation search; dock header button navigates to `/ai/chats/{selectedId}` (and workspace "return to page" uses `UiShellState` last-route map). Register AI workspace + routes. **`/ai` routes are authenticated-only** (`AuthenticatedRouteGuard`); anonymous AI access remains dock-only via the existing `ai_assistant.allow_anonymous_access` policy.
- **Acceptance:** [x] dock→workspace→dock round trip preserves selected conversation and draft input state where the state object holds it; [x] `/ai` routes reject anonymous users (guard test); [x] anonymous AI access remains dock-only.
- **Validation (2026-07-22):** Added one guarded workspace page for both routes, one searchable navigation provider, server-gated registry visibility, and mutually exclusive dock/workspace hosting of the existing rail controller. Focused routes/classifier 18/18, rail 27/27, navigation host 11/11, app rail 8/8, MainLayout 34/34, shared components 3/3, and Blazor architecture 17/17 pass; Release build has 0 errors. Failing-first tests prove late policy and reentrant HAL collection loading no longer lose or duplicate new-conversation requests. GPT-5.5 Oracle final review returned PASS.
- **Dependencies:** 5.1

### Phase 6: Hybrid Settings Architecture
- **Goal:** Contextual Personal Settings over the originating workspace, plus a dedicated authorized Settings hub and administrative routes, with one gear and no duplicate shell sidebar.
- **Depends on:** Phase 3 (scopes from shell context)
- **Relevant files:** existing `Services/Shell/UiShellState.cs`, `WorkspaceRegistry.cs`, `Components/Shell/AppWorkspaceRail.razor(.cs/.css)`, `Layout/NavMenu.razor(.cs)`, `Routes.razor`, `Pages/User/Settings.razor`, `Pages/User/Components/SettingsLayout.razor(.css)`, `Pages/Admin/{Tenant,Instance,Organization,Group}` settings pages/layouts, and `Pages/Admin/Tenant/Navigation.razor`; new shared `Pages/User/Components/SettingsScopeSelector.razor(.css)`; delete `Components/Shell/Workspaces/SettingsWorkspaceNavigation.razor(.cs/.css)`.
- **Related skills/rules:** `blazor-ui-conventions`, `blazor-css-isolation`, `design-system`; `.claude/rules/blazor-client.md`; route guards unchanged.
- **Acceptance criteria:** contextual Personal navigation retains the source workspace/navigation only for the current session; direct Personal deep links use dedicated Settings; exactly one rail item owns `aria-current`; scope UI comes only from shell context; canonical routes and old-link migration are complete; existing admin sidebars remain the section-navigation owners; single-tenant grouping changes presentation only; operational surfaces (moderation queues, attendee/check-in operations, failed delivery queues, approvals) are NOT moved into Settings.
- **Phase-end verification (run once):**
  - `dotnet build --configuration Release --verbosity quiet`
  - `dotnet test --project tests/Explore.Blazor.Client.Tests/Explore.Blazor.Client.Tests.csproj --configuration Release --verbosity quiet` (justified repeat: contextual Personal state, authorized scope entry points, and route-table behavior)
- **Rollback / failure handling:** keep route guards and existing admin components unchanged; if contextual utility state misbehaves, fall back to dedicated `/settings/personal` presentation without introducing a return URL or route alias. Missed old links are fixed at their producers, not by restoring aliases.

#### Task 6.1: Contextual Personal Settings state and provider removal
- **Type:** modify/delete | **Layer:** Blazor | **Effort:** M
- **Files:** modify `UiShellState.cs`, `WorkspaceRegistry.cs`, `WorkspaceNavigationHost.razor.cs`, `AppWorkspaceRail.razor(.cs/.css)`, and focused shell tests; delete `SettingsWorkspaceNavigation.razor(.cs/.css)` and its registrations/references.
- **Description:** Capture a Personal Settings origin only when a live Explore/Studio/AI session navigates to `/settings/personal/**`. Preserve that origin as the active workspace and keep its provider mounted; expose `IsPersonalSettingsOpen` for the gear's distinct utility state. Constructor/deep-link/refresh entry has no origin and resolves to dedicated Settings. Clear origin on any non-Personal route and never serialize it or accept a return URL.
- **Acceptance:** [x] Explore/Studio/AI → Personal preserves the source workspace, last route, and navigation; [x] direct `/settings/personal` and `/settings/personal/appearance` loads activate Settings; [x] leaving Personal clears utility state; [x] only one `aria-current="page"` renders; [x] no Settings workspace navigation provider remains.
- **Validation (2026-07-22):** Failing-first tests stopped on the absent utility-state properties. Final lanes pass `UiShellStateTests` 10/10, revocation 5/5, app rail 9/9, and navigation host 11/11; scoped diff/source/CSS/diagnostic checks are clean. Independent agent review was unavailable after the user prohibited further agent use due the monthly limit; direct fresh-state verification is recorded in `.omo/start-work/artifacts/task-6.1/done-claim.md`.
- **Dependencies:** —

#### Task 6.2: Canonical hub/personal/admin routes and page composition
- **Type:** create/modify/delete | **Layer:** Blazor | **Effort:** L
- **Files:** modify `Routes.razor`, `Pages/User/Settings.razor`, `SettingsLayout.razor(.css)`, existing admin settings pages/layouts, and route/component tests; add `SettingsScopeSelector.razor(.css)`; remove `@page "/admin/tenant/navigation"` from `Pages/Admin/Tenant/Navigation.razor`.
- **Description:** Add `/settings`, `/settings/personal`, `/settings/personal/:section`, `/settings/organization/:id`, `/settings/group/:id`, `/settings/admin`, and `/settings/instance`. The hub and selector consume only server-authorized scopes. Convert Personal's large sidebar/query-state navigation to compact path-based in-page sections. Mount existing guarded admin components without replacing their internal sidebars. Migrate Tenant navigation into the existing Tenant settings section. Remove old route rows/directives and update every producer by bounded source/test sweeps; do not add aliases.
- **Acceptance:** [x] every canonical route mounts the intended component with the existing guard; [x] Personal path sections support deep link/refresh and invalid sections fall back safely; [x] the shared selector renders Personal plus exactly authorized named scopes; [x] admin layouts retain one internal section sidebar; [x] old route/directive/link sweep returns zero stale runtime references.
- **Validation (2026-07-22):** Full client run passes 1,957/1,961 with only three known unrelated failures and one governed skip; architecture retains the four known unrelated blockers; Release build and stale-route/diff sweeps are clean. Direct verification is recorded in `.omo/start-work/artifacts/task-6.2/done-claim.md`.
- **Dependencies:** 6.1

#### Task 6.3: One-gear/profile entry points and authorized scope presentation
- **Type:** modify | **Layer:** Blazor | **Effort:** M
- **Files:** modify `AppWorkspaceRail.razor(.cs/.css)`, `NavMenu.razor(.cs/.css)`, the hub/scope selector, and focused bUnit tests.
- **Description:** Keep one bottom-pinned gear. Its primary link is always `/settings/personal`; when additional scopes exist, expose a keyboard-accessible menu to `/settings` and authorized scope routes. Keep direct Personal and authorized administrative links in the profile dropdown for rail-hidden layouts. In single-tenant mode, collapse authorized Tenant+Instance choices into "Site administration" presentation while preserving separate links/guards internally; multi-tenant mode lists them separately.
- **Acceptance:** [x] one gear only and primary action is always Personal; [x] contextual-open and workspace-active states are visually/semantically distinct; [x] profile access remains when the app rail is absent; [x] instance-only never renders Tenant; [x] deployment-mode × authority table covers Personal, org/group names, Tenant, Instance, and combined Site administration.
- **Validation (2026-07-22):** Valid red run produced five Task 6.3 failures plus the three known unrelated failures. Final client run passes 1,960/1,964 with only the unrelated baseline; architecture remains at its four unrelated blockers; Release build, authority/CSS scans, AFT diagnostics, and diff check are clean. Direct verification is recorded in `.omo/start-work/artifacts/task-6.3/done-claim.md`.
- **Dependencies:** 6.1, 6.2

### Phase 7: Durable Layout Preferences + Tenant Shell Governance
- **Goal:** Authenticated users get cross-device shell layout persistence; tenants get shell defaults with locks; anonymous behavior unchanged (localStorage).
- **Depends on:** Phases 2–4 (panels/workspaces exist), Phase 3 (context carries defaults)
- **Relevant files:** existing `GovernanceSettingKeys.cs`, `src/Explore.Domain/Settings/SettingRegistry.cs` (+ its definitions files), `SettingsController` (no change expected), `Services/Interop/LocalStorageDockLayoutPersistence.cs`, `MainLayout.razor.cs` autosave path, `Pages/Admin/Tenant` settings sections, `docs/CONFIGURATION.md`; new `Services/Interop/ServerBackedDockLayoutPersistence.cs`, client `ShellPreferencesService`.
- **Related skills/rules:** `dotnet-efcore-guidelines` not needed (no migration — `UserPreference` reused; if `SettingRegistry` requires seeded definitions verify seeding path in Task 7.1), `.claude/rules/application-layer.md` for definition placement.
- **Acceptance criteria:** authenticated resize/collapse persists across devices via `api/settings/user/{category}` batch writes (one PUT per debounce window, never per pointer event); restore clamps via descriptors and drops unknown ids/revoked actors/workspaces/settings scopes; viewport-driven projection never writes; anonymous users keep tenant-scoped localStorage and promote it on first authenticated hydrate when the server value is absent; tenant defaults (`ui_shell.*`, D8) resolve through the cascade with instance locks honored; user override disabled ⇒ tenant-forced mode wins and the client does not persist overridden values; Personal return origin is never persisted; `docs/CONFIGURATION.md` documents every retained/new key.
- **Phase-end verification (run once):**
  - `dotnet build --configuration Release --verbosity quiet`
  - `dotnet test --project tests/Event.API.IntegrationTests/Event.API.IntegrationTests.csproj --configuration Release --verbosity quiet` (covers settings round-trip, shell-context contract incl. Phase 3 endpoint, and registry-driven governance keys end-to-end)
- **Rollback / failure handling:** server persistence is a decorating `IDockLayoutPersistence`; DI swap back to localStorage restores prior behavior; governance keys default to current behavior (`Docked` events nav, override allowed, rail authenticated-only for org-centric public), so unset tenants see no change.

#### Task 7.1: Shell-context wiring + public-experience governance resolution
- **Type:** create/modify | **Layer:** Domain + Application | **Effort:** M
- **Description:** Wire registered D8 keys (from Phase 0.2) into `GetUiShellContextRequestHandler` (`navigationDefaults`, `allowUserOverride`, `organizerDefaultWorkspace`, `railPublicVisibility`); org-centric anonymous rail visibility resolved into the public-experience shell handler. Remove the already-registered `ui_shell.default_nav_mode.settings` definition/documentation because hybrid Settings has no shell navigation provider. Update `docs/CONFIGURATION.md` to document every retained key.
- **Acceptance:** [ ] handler tests prove lock and single-tenant bypass semantics for one representative key; [ ] no Settings navigation-default key remains; [ ] `docs/CONFIGURATION.md` documents every retained key.
- **Dependencies:** 0.2, 3.1

#### Task 7.2: Server-backed dock persistence + last workspace/actor/settings scope (tenant-discriminated anonymous storage)
- **Type:** create/modify | **Layer:** Blazor | **Effort:** L
- **Description:** `ServerBackedDockLayoutPersistence` implements `IDockLayoutPersistence`: authenticated → read/write the versioned snapshot JSON under `ui_shell_preferences.layout.v1` via `UserSettingsService` (batch PUT), anonymous → delegate to `LocalStorageDockLayoutPersistence` with a tenant discriminator (`dock_layout:v1:{tenantSlug}:`). No old-key compatibility read — stale anonymous snapshots from pre-discriminator keys are ignored. Promote local→server on first authenticated hydrate when server value absent. Persist `last_workspace`/`last_actor` on navigation/actor change (debounced). Add `ui.settings.last_scope.v1` only in this preference slice for the dedicated Settings hub/selector; normalize and re-authorize it before use, never store a Personal return URL, and never let it change the gear's `/settings/personal` primary action. Honor `allowUserOverride=false` by skipping nav-mode persistence.
- **Acceptance:** [x] unit tests cover authenticated/anonymous/promotion/revoked-actor and revoked-settings-scope pruning; [x] autosave still fires only for `UserAction`/`Reset` reasons; [x] anonymous storage key includes tenant slug; [x] no old-key compatibility read path exists; [x] last Settings scope affects only the dedicated hub/selector and safely falls back to Personal.
- **Dependencies:** 7.1

#### Task 7.3: Tenant admin "Shell" settings section
- **Type:** modify | **Layer:** Blazor | **Effort:** M
- **Description:** Add the D8 controls to the tenant settings page using the existing `EffectiveSettingDto.CanEdit`/`Reason` pattern (no client role checks). Preview modes from report §13 are deferred.
- **Acceptance:** [ ] locked settings render read-only with reason; [ ] saving updates effective defaults on next shell-context fetch.
- **Dependencies:** 7.1

### Phase 8: Responsive, RTL, Accessibility Hardening + Scenario Matrix
- **Goal:** Mobile bottom-nav projection of the rail, workspace canvas minimums, landmark/focus polish, and the table-driven scenario suite.
- **Depends on:** Phases 1–7
- **Relevant files:** `AppWorkspaceRail.razor(.css)`, `MainLayout.razor.css`, `DockLayoutState.cs` (generic caller-supplied content-floor hint), `WorkspaceNavigationHost`, `docs/DOCK_LAYOUT.md` (responsive matrix additions), `docs/ACCESSIBILITY.md` (landmark inventory); tests in `Explore.Blazor.Client.Tests` + `Event.Architecture.Tests` a11y conventions.
- **Related skills/rules:** `docs/ACCESSIBILITY.md`, `docs/LOCALIZATION.md`, dock responsive policy.
- **Acceptance criteria:** ≤ `Breakpoint.Xs` the rail renders as bottom navigation (Events | Studio | AI | Settings, availability-filtered) and never as a start track; workspace nav opens as temporary drawer (existing dock projection); workspace-specific canvas minimums applied via a per-workspace content-floor hint consumed by the projection rule (Events 375, AI 520, Settings 560, Studio 720) without persisting projected state; document title + focus move to `h1`/main on workspace switch; rail and each nav provider expose distinct `aria-label`s; RTL mirrors rail/nav/dock via logical properties (`:dir(rtl)` overrides only where animation direction requires); scenario matrix implemented as table-driven bUnit tests over Profile(Discovery/OrganizationCentric) × Auth(anon/user) × Capabilities(seeker/organizer/tenant-admin/instance-admin/multi-role) × Workspace × Viewport(mobile/desktop) asserting rail items, nav content, settings scopes, default route, and revocation fallback; `docs/DOCK_LAYOUT.md` QA matrix extended with rail scenarios.
- **Phase-end verification (run once):**
  - `dotnet build --configuration Release --verbosity quiet`
  - `dotnet test --project tests/Explore.Blazor.Client.Tests/Explore.Blazor.Client.Tests.csproj --configuration Release --verbosity quiet` (justified repeat: this phase's scenario matrix lives here)
- **Rollback / failure handling:** responsive changes are CSS/projection-rule scoped; the per-workspace floor is a pure function change with unit tests; revert restores the global 375px floor.

#### Task 8.1: Mobile bottom navigation + generic workspace canvas floors — **Effort:** L
- **Description:** ≤ `Breakpoint.Xs` the rail projects to bottom navigation (existing Phase 1.3 mobile behavior refined). Workspace content floors are supplied as a generic hint (Events 375, AI 520, Settings 560, Studio 720) consumed by the projection rule; `DockLayoutState` contains no workspace-specific branching — the floor is a pure function of the active workspace key passed from `UiShellState`.
- **Acceptance:** [ ] `DockLayoutStateTests` extended for generic floor hint; [ ] bottom nav bUnit coverage incl. availability filtering; [ ] `DockLayoutState` has zero workspace-specific conditional logic.
#### Task 8.2: Focus/landmark/RTL polish — **Effort:** M
- **Acceptance:** [ ] architecture a11y tests green; [ ] workspace switch moves focus (bUnit assertion on focus service calls).
#### Task 8.3: Scenario-matrix suite + docs sync — **Effort:** L
- **Acceptance:** [ ] matrix rows match report §6 table where implemented; [ ] DOCK_LAYOUT/ACCESSIBILITY/BLAZOR docs updated in the same tasks that changed behavior (fold-in rule).

### Phase 9: Personal Settings Entry Parity And Information Architecture 🟡 IMPLEMENTATION COMPLETE / VERIFICATION BLOCKED
- **Goal:** Make every first-party Personal Settings entry preserve live workspace context consistently and replace the horizontal single-section page with the requested sticky, searchable vertical Settings experience.
- **Depends on:** Phase 6 and Task 7.2
- **Relevant files:** existing `Services/Shell/UiShellState.cs`, `Components/Shell/AppWorkspaceRail.razor(.cs)`, `Layout/NavMenu.razor(.cs)`, `Layout/ThemeQuickSwitcher.razor`, `Pages/Events/Components/EventRegistration.razor`, `Pages/User/Settings.razor`, `Pages/User/Components/SettingsLayout.razor(.css)`, `SettingsScopeSelector.razor(.css)`, Personal Settings section components, and focused client tests.
- **Related skills/rules:** `blazor-ui-conventions`, `blazor-css-isolation`, `design-system`, `accessibility`; `.claude/rules/blazor-client.md`, `.claude/rules/tests.md`.
- **Acceptance criteria:** profile, rail, appearance, and connected-app entries preserve a live Events/Studio/AI origin through one explicit contract; direct/new-tab loads remain dedicated Settings; Personal-only users see no scope selector; `/settings/personal` defaults to View all; focused section routes remain deep-linkable; search filters View all by declared metadata; desktop navigation is sticky below the navbar; narrow layouts stack without overflow; keyboard, announcements, logical CSS, and one-`h1` rules hold.
- **Phase-end verification (run once):**
  - `dotnet build --configuration Release --verbosity quiet`
  - `dotnet test --project tests/Explore.Blazor.Client.Tests/Explore.Blazor.Client.Tests.csproj --configuration Release --verbosity quiet`
- **Rollback / failure handling:** entry parity can fall back to dedicated Settings without accepting/persisting a return URL; the layout can fall back to stacked non-sticky navigation without restoring horizontal tabs. Fix unsafe section composition at the owning section component rather than hiding it from View all.

#### Task 9.1: Explicit Personal Settings navigation contract and entry-point parity
- **Type:** modify | **Layer:** Blazor | **Effort:** M
- **Files:** modify `UiShellState.cs`, `AppWorkspaceRail.razor(.cs)`, `NavMenu.razor(.cs)`, `ThemeQuickSwitcher.razor`, `EventRegistration.razor`, and focused `UiShellState`/entry-point tests.
- **Description:** Add one intent-revealing navigation method that captures the current contextual workspace and route before navigating to a normalized Personal Settings route. Replace first-party Personal Settings navigation producers with this method while keeping real link semantics where possible. Do not persist the origin or accept `returnUrl`.
- **Acceptance:** [x] clicking profile Personal Settings from Events retains Events navigation and return route; [x] rail/profile/theme/connected-app paths share the contract; [x] direct construction on a Personal route remains dedicated Settings; [x] leaving Personal clears the origin; [x] no compatibility route or durable origin is introduced.
- **Validation (2026-07-22):** Added `UiShellState.NavigateToPersonalSettings` with canonical route normalization, modifier-click preservation, capture-before-navigation, and `forceLoad: false`. Rendered profile/rail/theme/registration interactions pass. Focused lanes: `UiShellStateTests` 18/18, `NavMenuWorkspaceTests` 7/7, and `EventRegistrationTests` 5/5; changed-file diagnostics and diff whitespace are clean.
- **Dependencies:** 6.1

#### Task 9.2: View all, section registry, search, and conditional scope selector
- **Type:** modify | **Layer:** Blazor | **Effort:** L
- **Files:** modify `Settings.razor`, `SettingsLayout.razor(.css)`, `SettingsScopeSelector.razor(.css)`, section components as required for safe multi-mount composition, and focused Settings tests.
- **Description:** Make `/settings/personal` the View all state. Define one canonical section metadata registry (key, label, keywords, renderer) used by navigation, focused routes, and search. Render all matching sections in View all and one section on deep links. Hide the scope selector when its only destination is Personal; retain it when server-authorized administrative scopes exist.
- **Acceptance:** [x] View all is first/default and renders all nine sections in canonical order; [x] focused routes render one section; [x] search is case-insensitive and metadata-driven with visible/announced empty state; [x] no duplicate IDs or duplicate page headings appear when all sections mount; [x] Personal-only scope selector emits no navigation landmark; [x] invalid section slugs fall back to View all without aliases.
- **Validation (2026-07-22):** One local section registry now drives canonical navigation, focused routes, search metadata, and `DynamicComponent` rendering. `SettingsLayoutTests` pass 7/7 and `SettingsScopeSelectorTests` pass 4/4; changed-file diagnostics and `git diff --check` are clean.
- **Dependencies:** 9.1

#### Task 9.3: Sticky responsive vertical navigation and documentation sync
- **Type:** modify | **Layer:** Blazor + Docs | **Effort:** M
- **Files:** modify `SettingsLayout.razor.css`, `SettingsScopeSelector.razor.css`, focused accessibility/layout tests, `docs/BLAZOR.md`, `docs/ACCESSIBILITY.md`, and `docs/DOCK_LAYOUT.md` where the QA matrix describes Settings.
- **Description:** Implement the token-driven two-column Settings grid, sticky sidebar offset below the navbar, scrollable content flow, narrow stacked projection, logical CSS, visible focus, reduced-motion behavior, and dynamic search announcements. Update canonical docs in the same slice.
- **Acceptance:** [x] desktop sidebar remains visible while content scrolls; [x] 320/375px layout stacks with no horizontal overflow; [x] DOM/focus order matches visual order; [x] one Settings scope nav and one Personal section nav have distinct labels when both render; [x] docs and scenario matrix describe View all/search/responsive behavior.
- **Validation (2026-07-23):** `SettingsLayout` now uses a token-driven `minmax(11rem, 15rem) minmax(0, 1fr)` grid with a navbar-offset sticky section nav; below `59.997em` it becomes a non-sticky stacked flow. Focus, logical-property, minimum-width, separator, scope-selector stacking, and reduced-motion contracts are covered. `SettingsLayoutTests` pass 8/8, `SettingsScopeSelectorTests` pass 4/4, the affected shell entry lane passes 11/11, and `AccessibilityConventionTests` pass 8/8. `docs/BLAZOR.md`, `docs/ACCESSIBILITY.md`, and `docs/DOCK_LAYOUT.md` describe the shipped behavior.
- **Follow-up validation (2026-07-23):** Personal Settings subscribes to Blazouter route changes, UI-shell dependencies execute sequentially on the scoped `DbContext`, `/settings/admin` replaces `/settings/tenant` without an alias, profile Settings chooses instance authority before tenant authority, and compact server-authorized selectors render beside admin headings. Focused application and client lanes pass 12/12 and 21/21; the Release build passes with zero errors. The full client run passes 2,015/2,020 with unrelated generated-client, tri-state filter, and report-dialog failures plus one governed skip.
- **Dependencies:** 9.2

### Phase 10: Final Visual/Browser QA Gate 🟡 BLOCKED
- **Goal:** Direct visual and browser QA verifying the integrated shell and revised Settings experience across workspaces, viewports, and auth states; independent agent review is unavailable because further agent use is prohibited.
- **Depends on:** Phases 1–9
- **Relevant files:** all shell components, `MainLayout`, `AppWorkspaceRail`, workspace navigation providers, Studio/AI/Settings pages.
- **Acceptance criteria:** rail renders correctly on desktop and mobile for anonymous, authenticated seeker, organizer, tenant-admin, and instance-admin principals; Settings entry parity, View all, search, sticky/stacked navigation, and conditional scope selector work; bottom nav is reachable; no layout breakage at 320px, 375px, 768px, 1280px, 1920px; focus order is logical; no console errors on workspace navigation. Existing anonymous evidence may be reused only where behavior is unchanged.
- **Phase-end verification:**
  - `dotnet build --configuration Release --verbosity quiet`
  - `dotnet test --project tests/Explore.Blazor.Client.Tests/Explore.Blazor.Client.Tests.csproj --configuration Release --verbosity quiet`
  - Manual browser walkthrough of the scenario matrix (documented in `docs/DOCK_LAYOUT.md` QA matrix).

#### Task 10.1: Final visual/browser scenario-matrix walkthrough
- **Type:** verify | **Layer:** Blazor + Docs | **Effort:** M
- **Description:** Complete the remaining authenticated persona matrix and re-run Settings-specific rows after Phase 9. Preserve the existing anonymous 320/375/768/1280/1920 evidence as partial baseline, but do not treat it as proof of the revised Settings page.
- **Acceptance:** [ ] anonymous and authenticated persona evidence is recorded; [ ] Settings-specific screenshots cover Personal-only and administrative-scope states; [ ] console, focus, RTL, sticky/stacked behavior, and horizontal-overflow findings are recorded honestly.
- **Blocker evidence (2026-07-23):** the running `local-lite` environment reports `isAuthenticated=false`; `/settings/personal` redirects to `/`, so revised Personal Settings cannot be rendered for browser evidence. The anonymous public root is console-clean and showed no horizontal overflow at 1760px, but no usable local authenticated seeker/organizer/tenant-admin/instance-admin identities or verified authorities are available. Do not infer completion from deterministic tests or unchanged anonymous-shell screenshots.
- **Dependencies:** 9.3

## 7. Testing Strategy

One build + at most one test project per phase (assignments above). `Explore.Blazor.Client.Tests` repeats in Phases 1/4/5/6/8 because each phase adds new bUnit-covered components — it is the fastest deterministic project owning that surface. Intent-mandated projects distributed: `Event.Architecture.Tests` (Phase 2), `Event.Application.UnitTests` (Phase 3), `Event.API.IntegrationTests` (Phase 7). No E2E/Playwright/browser/Docker-startup verification is planned for intermediate phases; `Explore.Blazor.Client.E2ETests` is intentionally unused. **Failing-first test expectation:** every task that changes executable behavior starts with the smallest relevant check that fails before implementation and passes afterward (e.g. guard rejection, actor-list source, tenant-discriminator key, generic floor hint). Delete obsolete tests (old routes, `SidebarState`, `shell.left-nav`) — never skip them.

## 8. Documentation, Configuration, And Operations Impact

| Artifact | Change | Phase |
|---|---|---|
| `docs/adr/ADR-019-workspace-shell-composition.md` | new | 1 |
| `docs/DOCK_LAYOUT.md` | panel rename, bridge removal, rail note, responsive matrix rows | 2, 8 |
| `docs/BLAZOR.md` | service/state table, workspace shell section, settings routes | 2, 6 |
| `docs/API.md` + `docs/API_CHANGELOG.md` | `GET api/ui-shell/context` | 3 |
| `schemas/openapi_islamu-event.json` + `EventApiClient.g.cs` | regeneration (serialized/isolated diff) | 3 |
| `docs/CONFIGURATION.md` | `ui_shell.*` + `ui_shell_preferences.*` keys, scopes, locks, defaults; wiring notes when consumed | 0, 7 |
| `docs/ACCESSIBILITY.md` | landmark inventory update | 8 |
| No Compose/Aspire/deployment/secret changes | — | — |

## 9. Security, Authorization, Privacy, And Abuse

- Shell context endpoint is `[Authorize]`, per-user, uncached at HTTP layer; it exposes only the caller's own authorities — never other users' or cross-tenant data; anonymous surface stays on the existing cacheable public shell (no membership leakage).
- All Studio per-resource affordances remain HAL-gated; workspace visibility is UX only — the API stays authoritative (hiding Studio is not publishing enforcement; publisher policy remains server-side, report §7).
- Instance admin ≠ tenant/organizer authority is encoded in handler tests (report §6 rules).
- Acting-actor selection is a request hint only; no client-selected actor grants authority (existing API authorization unchanged).
- AI workspace inherits existing AI authorization, quotas, consent, and proposal/confirmation invariants; no new AI surface area.
- Preference writes go through the existing authenticated user-settings endpoints with the `write` rate-limit family; debounce rules prevent write amplification (anti-pattern #10).

## 10. Multi-Tenancy, Federation, Localization, Accessibility, Product

| Concern | Status | Reason |
|---|---|---|
| Multi-tenancy | Applicable | `ui_shell.*` cascade + locks (Phase 7); shell context is tenant-resolved; single-tenant settings composition (Phase 6) |
| Federation | Not Applicable | No ATProto/ActivityPub surface touched |
| Localization/RTL | Applicable | Rail labels via translation keys; logical properties; RTL mirroring (Phases 1, 8) |
| Accessibility | Applicable | Landmarks, focus management, bottom-nav semantics, architecture tests (Phases 1, 2, 8) |
| Product/experience profiles | Applicable | Reuses `PublicExperienceMode`; org-centric pinning + public rail visibility (Phases 4, 7) |
| Observability | Not Applicable (bounded) | Client-side feature; no new server telemetry beyond standard request metrics; no PII in any new logs |
| Migration/compatibility | Applicable (negative) | No DB migration; no compatibility shims — removals are permanent (see §12) |

## 11. Observability And Operations

No new operational surface. The shell-context endpoint participates in standard API telemetry/rate limiting. Client failures degrade to anonymous shell composition (rail shows public workspaces only). No health checks, background services, or runbooks change.

## 12. Migration And Compatibility Plan

- **No EF migration.** Storage reuses `UserPreference`; governance keys are registry-defined.
- **Removed without aliases (dev mode, explicitly approved by request):** `shell.left-nav` panel id, `AppSideNav` component, `SidebarState` service, `/admin/tenant/settings`, `/admin/instance/settings`, `/admin/organization/:id/settings`, `/admin/group/:id/settings` central route rows; `/admin/tenant/navigation` `@page` directive (it was never in `Routes.razor`). Components live on under `/settings/**`.
- **Generated contract:** one additive endpoint; regeneration produces a serialized/isolated generated-artifact diff; no commit unless explicitly requested.
- **Stale localStorage snapshots** referencing `shell.left-nav` are dropped by the defensive restore path (verified behavior) — no cleanup code needed.

## 13. Risk Register

| Risk | Likelihood | Impact | Mitigation | Detection Signal | Owner/Task |
|---|---|---|---|---|---|
| Rail track breaks dock grid math at constrained widths (shell + workspace docks + rail) | Medium | High | Rail is a fixed shell track outside `DockLayoutHost`; extend responsive matrix; per-workspace floors in Phase 8 | `DockHostTests` failures; visual QA matrix | 1.3, 8.1 |
| `NavMenu` refactor regressions (it owns search, eligibility, admin menu, AI toggle) | Medium | Medium | Incremental edits per phase (2.3, 3.3, 4.4) each with bUnit coverage; never one big rewrite | NavMenu test failures | 2.3/3.3/4.4 |
| Shell-context handler quietly duplicates authority logic and drifts from `GetAdminAuthority` | Medium | High | Compose existing handlers/repos; parity unit tests comparing both outputs for the same principal | Application unit tests | 3.1 |
| Settings route removal misses internal links | Medium | Medium | Bounded source/test sweep is an acceptance criterion; producers migrate directly with no aliases | 404s in dev; failing guard tests | 6.2 |
| Personal Settings loses or forges workspace origin | Medium | High | Origin exists only in scoped `UiShellState`; Phase 9 routes every first-party entry through one explicit capture-before-navigation contract; origin clears on exit and is never accepted from URL/browser persistence | profile-vs-rail parity failure; duplicate `aria-current`; wrong workspace nav | 6.1, 9.1 |
| Scope selector or gear leaks unauthorized administration links | Low | High | Render exclusively from `SettingsScopeDto`; revalidate durable last scope; instance authority never implies tenant authority | authority-matrix bUnit failures | 6.2, 6.3, 7.2 |
| Personal/admin navigation duplicates into two sidebars | Medium | Medium | Delete shell Settings provider; Personal uses compact in-page sections; existing admin layouts remain sole section-navigation owners | navigation-host/component structure tests | 6.1, 6.2 |
| View all mounts sections that assume they are the only page content | Medium | High | Audit IDs/headings/lifecycle before enabling multi-mount; fix ownership in the section component and cover the composed render | duplicate IDs, duplicate h1, repeated API calls, bUnit failures | 9.2 |
| Sticky Settings navigation obscures focus or overflows on narrow screens | Medium | Medium | Tokenized navbar offset, logical CSS grid, stacked narrow projection, focus-order and overflow assertions | focus-obscured checks; 320/375px QA | 9.3, 10.1 |
| NSwag regen churn colliding with parallel workstreams (webhooks intent pins `EventApiClient.g.cs`) | Medium | Medium | Regen is a serialized/isolated generated-artifact diff; coordinate with active webhook workstream before Phase 3 lands | git conflicts on `.g.cs` | 3.2 |
| Studio scope creep (building features instead of shells) | High | Medium | Phase 4 acceptance explicitly limits to shells + links to existing editors; deferred list is authoritative | Task ledger growth | 4.x |
| `Blazouter` router limitations for workspace metadata | Low | Medium | Prefix classification needs no router changes; extension only if evidence appears | classifier tests | 1.2 |
| Architecture a11y tests break on landmark moves | Medium | Low | Update tests in the same task as the structural change (Phases 1–2), never delete coverage | `Event.Architecture.Tests` | 1.3, 2.1 |

## 14. Success Metrics And Definition Of Done

- Route-driven workspaces: deep links and refresh land in the correct workspace with correct nav (scenario matrix green).
- Capability truth: no `IsInRole`/claim inspection in any new shell/Studio/Settings component; Studio visibility flips with server data alone.
- One conversation system: dock and `/ai` provably share history/state.
- Hybrid Settings: every first-party Personal entry uses one explicit context-preserving contract; direct/new-tab loads remain dedicated; Personal-only users see no scope selector; `/settings/personal` defaults to searchable View all with sticky responsive vertical navigation; hub/admin links remain server-authoritative without duplicate sidebars.
- Preferences: cross-device layout restore for authenticated users; tenant defaults + locks effective; projection never persists.
- All eleven phase gates green: each = one Release build + the phase's single test project run. Phase 10 adds the final visual/browser QA gate.

## 15. Implementation Agent Contract — KEEP DEV DOCS CURRENT

1. First implementation start: read plan, context, and tasks once. Cold resume: read context + tasks, then only the current-phase plan sections.
2. Uninterrupted session: do not reread unchanged artifacts after every task.
3. Start from the highest-priority unchecked task unless the user overrides.
4. `tasks.md` is the hot ledger: check substantial tasks immediately; reconcile small ones by phase end.
5. Implementation checkboxes and phase-verification checkboxes are separate; a phase completes only when its build + selected test pass.
6. Keep status summary, completed count, current priority, next slice, discovered tasks, deferred work, and `Last Updated` accurate on every change.
7. Update context after a phase, meaningful decision, blocker, failed validation, discovery, or handoff — not for trivia.
8. Update the plan only for scope/architecture/sequence/acceptance/risk/validation changes.
9. Record failed validation with cause + recovery in tasks/context without marking the phase complete.
10. Before pause/compaction/transfer/PR: reconcile tasks, add a dated handoff, flag unrelated dirty files.
11. Phase verification runs once after all phase tasks: one Release build + the single selected test project. Never start the app, browser, Docker, Aspire, or Playwright.
12. Never report completion while repository reality and the ledger disagree.

Every implementation summary must teach: what changed and why; patterns/libraries used (dock descriptors, workspace registry, CQRS query, HAL gating, settings cascade); key files/classes and responsibilities; data/control flow; conventions honored; verification performed; remaining work; dev-doc status.

## 16. Progress Reporting Contract

```text
Implemented: developer teaching summary
Verified: exact evidence
Remaining: incomplete or deferred work
Next: recommended next slice
Docs updated: yes/no with reason (tasks.md reconciliation confirmed; context/plan updated or unchanged with reason)
```

## 17. Potential Risks & Unknowns

The most likely failure point is **Phase 3's capability aggregation**. The client currently derives Studio-grade eligibility from four independent calls whose semantics were never designed to compose (`AdminAuthorityDto`, onboarding status, my-orgs/my-groups, event-creation eligibility). Managed organization/group actors are resolved through `IAiAssistantActorContextService`, and group creation uses the existing `/events/create` publisher picker; the remaining risk is reproducing the broader eligibility semantics imperfectly, which would make workspace visibility silently wrong for edge principals (instance-admin-who-is-also-organizer, group-only organizers, org-centric pinned tenants). The mitigation is real but not free: parity tests against `GetAdminAuthority` outputs and the eight scenario rows. Second-order risk: the Settings route unification (Phase 6) touches many link producers; the `rg` sweep is bounded, but SSR-prerendered links or string-built URLs could hide from the pattern — the NotFound page plus guard tests are the backstop. Finally, the deferred navigation-override model (report §13) is intentionally out of scope; if the user expected the tenant navigation editor in this workstream, that is a scope decision to make at review time.

## Deferred Work (explicit)

| Item | Reason | Trigger to schedule |
|---|---|---|
| Tenant navigation item-override model (code-owned keys + override rows + editor previews) — report §13 | High-churn schema + editor UX; custom links already work via `TenantNavigationLink` | After Phase 8 ships and tenant feedback demands reordering/hiding core items |
| Studio operational features (attendees, check-in, tickets, communications, analytics implementations) | Separate product workstreams; Phase 4 ships shells + HAL-gated links | Per-feature plans |
| User-reported submission surface ("My submissions") — report §9 | Depends on registration-data workstream surfaces | When reported-listing management UX is prioritized |
| `BackofficeOnly` experience profile | No real requirement yet (report §4A) | Tenant demand |
| Admin/Control workspace (moderation queues, webhook ops) | Rail must not hard-code four-forever, registry allows it later | Operational surface growth |
