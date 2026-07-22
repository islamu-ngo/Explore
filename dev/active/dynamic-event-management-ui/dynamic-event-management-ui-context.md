<!-- ABOUTME: Operational memory for the workspace-shell (dynamic event management UI) workstream. -->
<!-- ABOUTME: Captures verified anchors, decisions, constraints, risks, and resume state for implementation agents. -->

# Dynamic Event Management UI (Workspace Shell) — Context

Last Updated: 2026-07-22 Europe/Brussels

## SESSION PROGRESS (2026-07-22 Europe/Brussels)

### ✅ COMPLETED
- Task 0.1: Documentation re-baseline completed. Plan approved by Oracle directionally (D1–D10 corrections accepted).
- Task 0.2: Registered seven lockable Instance/Tenant `ui_shell.*` definitions and three non-lockable User-only `ui_shell_preferences.*` definitions; documented all keys and added architecture coverage.
- Task 1.1: Added accepted ADR-019 for permanent app-rail chrome, route-derived workspaces, contextual navigation, and shared vocabulary; ADR-016–018 remain reserved for registration-data-collection.
- Task 1.2: Added the compile-time Events/Settings registry, segment-aware route classifier, scoped `UiShellState`, DI registrations, and 56 passing route/state cases.
- Task 1.3: Added the accessible desktop workspace rail and Xs bottom projection, integrated it only in MainLayout chrome, and introduced a shared logical Start inset so fixed dock panels cannot overlap the rail.
- Task 2.1: Added descriptor-selected workspace navigation providers and `WorkspaceNavigationHost`; MainLayout registers the host once, the host owns shared overlay brand/close chrome, and no-provider routes close the Start panel without overriding persisted or explicit user-close state.
- Task 2.2: Cleanly renamed the shell panel to `shell.workspace-nav`, moved discovery content to `Workspaces/EventsWorkspaceNavigation`, updated all runtime/test callsites and shipped dock docs, and verified stale `shell.left-nav` snapshots are ignored without an alias.
- Task 2.3: Deleted `SidebarState` and its DI/test registrations; MainLayout now controls workspace-nav state directly through `DockLayoutState`, while NavMenu derives availability from `UiShellState`/`IWorkspaceRegistry`. `AiAssistantState` remains as policy state.
- Task 3.1: Added the authenticated UI-shell Application request, DTO graph, and aggregation handler. It composes current identity/tenant context, DB-backed admin authority, `IAiAssistantActorContextService`, hierarchical governance settings, AI availability, and deployment mode without HybridCache or repository DTO projection.
- Task 3.1 authority rules are locked by ten focused tests: instance-admin-only, tenant-admin-only, organization organizer, seeker, personal publisher, multi-role union, group organizer, organization-centric pinned actor, missing-setting fail-closed behavior, and explicit organization settings authority without Studio. Instance authority alone grants Instance settings only and never Studio or Tenant settings.
- Task 3.2: Added authenticated `GET /api/ui-shell/context` with route name/operationId `GetUiShellContext`, authenticated endpoint classification, `PrivateNoStore`, and typed 200/401 contracts. The action only dispatches `GetUiShellContextRequest` and returns the plain DTO.
- Task 3.2 regenerated `schemas/openapi_islamu-event.json` and `EventApiClient.g.cs` through the documented API-first workflow. The exact generated method is `GetUiShellContextAsync`; Oracle passed the HTTP and generated-client boundary with high confidence.
- Task 3.3: Added the scoped five-minute `IUiShellContextService`, authenticated cache reads and current-user invalidation, server-driven Studio rail availability, and revoked-workspace reconciliation to Events. NavMenu now consumes the same shell context instead of separate authority calls; managed actors drive organization/group membership rows while matching settings scopes independently gate settings links.
- Task 3.3 focused client tests pass 30/30 across service, rail, revocation, and NavMenu lanes. The focused architecture lane passes 10/10 with one governed skip, the Release build passes with zero errors, and the tenant-filtered `AdminContext` lane passes 18/18.
- Oracle passed Task 3.3 with high confidence and no required fixes after reviewing operation naming, endpoint privacy, cache/logout invalidation, server-authoritative Studio gating, revoked-workspace fallback, managed-actor/settings-scope separation, client isolation, DI placement, and focused coverage.
- Managed actor context now carries its organization/group `ScopeId` alongside `ActorId`; this preserves the actor service as the single source while preventing actor IDs from being misused as settings-route scope IDs.
- Oracle review found and the implementation corrected two Task 3.1 boundary defects: settings scopes now originate from tenant-filtered explicit admin authority rather than event-publisher actors, and actor/member repository cancellation now flows end to end through `IAiAssistantActorContextService`. Oracle follow-up passed both fixes with high confidence.
- `IAdminContext` now exposes tenant-filtered organization/group admin-ID overloads. `AdminContext` filters memberships by `TenantId`, so the tenant-local shell context cannot leak another tenant's settings scopes; managed actors only enrich scope labels.
- Phase 2 deferred Release build and Task 3.1 Release build now pass with zero errors. A transient concurrent-output copy race was resolved by one unchanged retry.
- Phase 2 review fix: policy-driven hidden-chrome/no-provider workspace-nav changes now suppress autosave, hidden-chrome restoration tracks whether policy actually closed the panel, and debounced user saves capture their snapshot before later policy projection. Oracle follow-up passed the corrected state.
- Phase 1 independent review passed all five lanes after fixing query-preserving rail re-entry, ABOUTME headers, ADR numbering, governance deferral wording, and staged descriptor documentation.
- Re-verified repository claims: `/admin/tenant/navigation` confirmed as `@page` directive (never in `Routes.razor`); `GetManagedEventsByActorAsync` confirmed in generated client; `IAiAssistantActorContextService` confirmed as reusable for managed actors.
- Planning artifacts corrected and approved: status set to "Approved / Implementation started"; Phase 0 added; all task descriptions, dependencies, acceptance criteria, and risks updated per Oracle corrections.
- Task 4.1: Added authenticated `/studio` and `/studio/events` route rows, registered the Studio workspace navigation provider, and introduced minimal Studio home/events shells. Event data, detail routes, and durable actor preference remain in Tasks 4.2, 4.3, and Phase 7 respectively.
- `UiShellState` now reconciles the session actor from server-authorized managed actors: authorized pinned actor first, then an authorized current selection, then the first authorized actor. The switcher is read-only for single/pinned modes and selectable only for multiple unpinned actors.
- Studio actor-level navigation renders only Overview and Events, and the `/studio/events` deep link marks Events with `aria-current="page"`. Focused route/state/switcher/navigation/page lanes pass 34/34; Release build passes with zero errors; `git diff --check` is clean.
- Oracle passed Task 4.1 with no required fixes after reviewing actor authorization/reconciliation, pinned locking, authenticated guards, server-context authority, active navigation, accessibility, and acceptance coverage.
- Task 4.2: `IEventService` now exposes a strict managed-actor event page and preserves collection HAL links. Studio pages use `ActorId` for managed reads, my-events for personal fallback, and one shared cancellation/version-fenced lifecycle base for actor changes and disposal.
- `/studio` now renders actor identity plus total/upcoming/editable counts and HAL/eligibility-gated quick actions. `/studio/events` renders accessible management rows whose Edit/Delete actions come only from item `_links`; Create requires both event eligibility and collection `create` and always uses the existing `/events/create` publisher picker.
- Task 4.2 red/green evidence is recorded: baseline 2/2, then 10 expected failures, then 12/12 Studio pages, 69/69 event service, 3/3 switcher, and 9/9 navigation host green. Client Release build has zero warnings/errors; GPT Oracle returned PASS with no required fixes.
- Task 4.3: Added four authenticated `/studio/events/:eventId` section routes, a scoped `StudioEventContextState` that shares one in-flight detail request between route content and sibling navigation, and event navigation that replaces actor navigation on event routes.
- Event sections are rendered only from event HAL relations. Team/Danger use the canonical slug/public-code route when available and fall back to the existing authenticated editor, while Details/Schedule/Publication/Registration shells hand off to existing event/session management surfaces.
- Task 4.3 evidence is green: navigation 8/8, shared state 1/1, workspace host 10/10, routes 17/17, Studio regression 12/12, client Release build 0 warnings/errors, clean diff/authority/RTL sweeps, and GPT-5.5 Oracle PASS.
- Task 4.4: `NavMenu` now derives search and event-primary-action behavior from `UiShellState`. Events keeps public event search and Add Event; Studio searches `/studio/events`, labels the action Create, uses the existing `/events/create` publisher picker, and shows the active actor with bidi isolation.
- Settings and future AI workspaces hide global event search and event creation. Anonymous submission prompts are also Events-only, preventing the old Add Event action from leaking into Settings.
- Task 4.4 evidence is green: valid red 4 total/3 expected failures/1 Events pass, final workspace matrix 5/5, NavMenu admin/context 18/18, client Release build 0 warnings/errors, clean diagnostics/diff, and GPT-5.5 Oracle follow-up PASS.
- Phase 4 verification ran once: the canonical Release build passed with 0 errors and 1,035 existing warnings; the full client suite reproduced the exact known unrelated baseline at 1,932 total, 1,928 passed, 3 failed, and 1 governed skip. Phase 4 implementation is complete but its suite checkbox remains blocked.
- Task 5.1: Extracted `AiConversationTimeline` and `AiConversationComposer` from `AiAssistantRail` while keeping the rail as the orchestration/state owner. The shared components inject no shell state or AI client service, and the composer generates host-unique accessible IDs unless the rail requests its legacy `ai-rail` prefix.
- Existing AI primitives now own their CSS-isolated styles (`AiProposedActionCard`, `AiActionResultCard`, `CreateEventDraftActionPreview`, `AiReferencePicker`, and `AiReferenceChip`), including reduced-motion and MudBlazor `::deep` selectors, so workspace composition does not depend on rail CSS.
- Task 5.1 evidence is green: rail 21/21, timeline 1/1, composer 2/2, proposed-action 3/3, reference-picker 2/2, action-result 7/7, Blazor architecture 17/17, canonical Release build 0 errors, and GPT-5.5 Oracle final PASS.
- Task 5.2: Added authenticated `/ai` and `/ai/chats/:conversationId` routes, server-availability-gated AI workspace registration, searchable recent-conversation navigation, and an authenticated dock header handoff into the full workspace. Anonymous AI remains dock-only.
- The retained dock and workspace mount the same `AiAssistantRail` orchestration controller in mutually exclusive hosts and share scoped `AiAssistantConversationState`; `MainLayout` suppresses the duplicate dock on AI routes without clearing its open intent, then restores it when leaving. Workspace return resolves the source workspace through `UiShellState`'s last-route map.
- Workspace initialization now handles tenant AI policy arriving after first render and serializes reentrant state notifications while HAL conversation affordances load. Failing-first late-policy and deferred-collection tests lock both races, and new-conversation requests are consumed once per bound request cycle.
- Task 5.2 evidence is green: routes/classifier 18/18, rail 27/27, navigation host 11/11, app rail 8/8, MainLayout 34/34, shared AI primitives 3/3, Blazor architecture 17/17, canonical Release build 0 errors, and GPT-5.5 Oracle final PASS. The full client suite still reproduces only the three known unrelated failures at 1,943/1,947 passed with one governed skip.
- Task 6.1: `UiShellState` now distinguishes direct Personal Settings from in-session contextual navigation. Explore/Studio/AI origins remain active with a session-only return route; section changes preserve it; route exit or authority revocation clears it.
- The Settings descriptor no longer owns a workspace-navigation provider, and the three placeholder provider files are deleted. Direct Settings therefore has no duplicate shell sidebar, while contextual Personal Settings retains the origin provider.
- The app rail renders exactly one `aria-current="page"` and uses a separate tokenized, logical-CSS utility marker on Settings. Focused evidence is green at state 10/10, revocation 5/5, rail 9/9, and host 11/11.
- Task 6.2: Added the canonical Settings hub, Personal root/section routes, and guarded Organization/Group/Tenant/Instance routes. Old admin Settings rows/directives, the standalone Tenant navigation route, query-state Personal links, and all source/test link producers were removed without aliases.
- `SettingsScopeSelector` exposes Personal plus only server-returned `SettingsScopeDto` links and fails closed when shell context is unavailable. Existing admin layouts retain their own sidebars and guards; Personal now uses compact semantic path links.
- The workspace gear now always targets `/settings/personal`; `/settings` remains the dedicated scope hub. Tenant-base-path matching uses `NavigationManager.ToBaseRelativePath`.
- Task 6.2 evidence: client 1,957/1,961 passed with only 3 known unrelated failures and 1 skip; architecture 281/286 passed with only 4 known unrelated failures and 1 skip; canonical Release build passed with 0 errors; stale-route/query and diff sweeps are clean.
- Task 6.3: The bottom Settings gear always links to Personal and exposes a native keyboard-accessible scope menu only when server-authorized administrative scopes exist. The menu contains the hub and exact authorized routes without adding another gear or current-workspace state.
- The profile dropdown now always exposes Personal Settings and All Settings, plus named Organization/Group and Tenant/Instance links sourced only from `SettingsScopeDto`. Single-tenant dual authority groups both guarded routes under `Site administration`; instance-only never renders Tenant.
- Phase 6 implementation is complete. The client suite passes 1,960/1,964 with only the three known unrelated failures and one skip; architecture remains at 281/286 with four known unrelated failures and one skip; Release build is green with zero errors.
- Task 7.1: Removed the obsolete `ui_shell.default_nav_mode.settings` constant, definition, DTO member, handler resolution, generated contract member, and configuration documentation without an alias.
- The authenticated shell continues to batch-resolve Events/Studio/AI navigation defaults, user-override policy, and organizer default workspace. A single-tenant/SystemLocked handler case proves resolved lock semantics pass through unchanged.
- The public-experience shell now resolves `ui_shell.rail_public_visibility` through the existing hierarchical cascade, fails closed to `AuthenticatedOnly`, exposes the policy in `PublicExperienceShellDto`, and includes it in the shell revision.
- Task 7.1 evidence is green: focused Application handlers 11/11, governance registry 9/9, Release build 0 errors, generated OpenAPI/client parity clean, and stale-key/diff sweeps clean. Full Application, Domain, and Architecture suites retain only unrelated recorded failures.
- Task 7.2: Authenticated shell layouts now use a decorating `ServerBackedDockLayoutPersistence` over the existing user-settings API; anonymous layouts remain in local storage under tenant-discriminated keys and promote once when an authenticated server value is absent.
- `ShellPreferencesService` revalidates durable workspace, managed actor, and administrative Settings scope against fresh shell authority before restoring them. Invalid values are reset; Personal remains the fallback and its contextual return origin is never durable.
- `MainLayout` hydrates preferences before dock state, debounces selection writes, and preserves dock autosave only for `UserAction`/`Reset`. Governed viewport/policy changes do not persist, and `AllowUserOverride=false` removes workspace-navigation mode from the stored snapshot.
- Task 7.2 evidence is green: focused client 4/4, extended affected fixtures 6/6, focused architecture/governance 17/17, Release build 0 errors, clean diff/stale-key checks, and no Task 7.2 diagnostics. Full suites reproduce only the recorded unrelated baselines.
- Task 7.3: Added a tenant Workspace Shell section backed by the existing `GET api/settings/tenant/UiShell` and single-setting update endpoint through `ITenantShellSettingsService`. The six retained D8 controls render from effective settings and use only server `CanEdit`, `Reason`, source, and lock metadata.
- Task 7.3 evidence is green: focused component 2/2, affected tenant-settings 2/2, focused architecture/governance 17/17, Release build 0 errors, and no role/claim checks. The full API integration lane is environment-blocked by at least 500 Docker/Testcontainers failures plus recorded unrelated baselines.
- Task 8.1: The existing availability-filtered Xs CSS projection remains the only mobile bottom navigation. `DockLayoutState` now accepts a generic per-scope minimum-content hint; MainLayout supplies Events 375, AI 520, Settings 560, or Studio 720 without workspace branching in the dock engine.
- Content-floor changes are `ViewportPolicy`, never enter snapshots, and cannot trigger the `UserAction`/`Reset` persistence lane. Affected shell/dock/rail tests pass 33/33.
- Task 8.2: The Events provider now exposes the distinct `Events workspace navigation` landmark label instead of duplicating MainLayout's `Sidebar navigation`. Existing workspace-switch focus continues through `IAccessibilityFocusService.FocusOnNavigateAsync`; no new focus mechanism or RTL override was added.
- Task 8.2 evidence is green: failing-first label assertion reproduced the duplicate name; workspace host 11/11, MainLayout 39/39, and accessibility architecture/logical-CSS conventions 8/8 pass.
- Task 8.3: Added `WorkspaceShellScenarioMatrixTests`, a nine-row rendered matrix over profile, authentication, capability, workspace, and mobile/desktop semantics. It asserts canonical rail items, contextual Events content, server-authorized Settings links, durable default selection, and revoked-workspace fallback.
- The matrix exposed alphabetical AI-first rail sorting. `AppWorkspaceRail` now preserves the canonical `WorkspaceRegistry` order (Events, Studio, AI, Settings), keeping Settings last without a second ordering rule. The focused matrix passes 1/1 and `DOCK_LAYOUT`, `ACCESSIBILITY`, and `BLAZOR` match the shipped shell.
- Phase 8 verification ran once: the canonical Release build passed with 0 errors and 536 existing warnings; the full client suite passed 1,979/1,983 with the same three unrelated failures and one governed skip. Phase 8 implementation is complete, but its suite checkbox remains blocked rather than greenwashed.

### 🟡 READY TO START
- Task 9.1: explicit Personal Settings navigation contract and entry-point parity.

### ⏭️ NEXT
1. Add failing-first interaction coverage proving the profile-dropdown Personal Settings entry preserves the same live Events/Studio/AI origin as the rail gear.
2. Implement the shared capture-before-navigation contract, then build the View all/search/vertical-navigation slice.
3. Run Phase 9 verification once; resume final browser QA as Task 10.1 only after the revised Settings behavior is complete.

### ⚠️ BLOCKERS
- None hard. Soft coordination points:
  - Phase 0 architecture verification has four unrelated existing failures: decentralization schema discovery, HATEOAS permission metadata, repository naming, and organization-scope guardrails. The focused UI-shell registry test passes; do not alter unrelated code in this workstream.
  - Phase 1, Phase 4, and Phase 5 Blazor client verification reproduce three unrelated existing failures: generated `HalCollectionEmbeddedOfEventLocationManagementDto` typing and two `ReportEventDialogTests`. The full Phase 5 run discovered 1,947 tests: 1,943 passed, 3 failed, 1 governed skip; focused workspace-shell and AI suites pass.
  - Phase 2 full architecture verification still has four unrelated existing failures recorded in `tasks.md`; the focused Phase 3 architecture lane passes 10/10 with one governed skip.
  - Full Application verification has two unrelated failures in EventLocation privacy-review expectations and EmailDispatch metric capture. It passes 2,930/2,932; focused Task 7.1 UI-shell/public-experience tests pass 11/11.
  - Full Domain verification has one unrelated ATProto registry-parity failure: five federation definitions are registered while the older test expects three. It passes 452/453; focused UI-shell registry coverage passes in Architecture 9/9.
  - `EventApiClient.g.cs` regeneration (Phase 3, Task 3.2) must not collide with the webhook-delivery-redesign workstream's pinned client scope.
  - Unrelated dirty files (OpenAPI/NSwag artifacts, API/config docs, instance-admin settings files/tests) must be preserved; dirty-file hunk preservation controls are explicit in Task 3.2.

## Quick Resume
1. Read this context and `dynamic-event-management-ui-tasks.md`.
2. Read only Phase 9 + referenced decisions D9 and D11 from `dynamic-event-management-ui-plan.md`; do not reread the full plan on every resume.
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
| `src/Explore.API/Controllers/PublicExperienceController.cs` | Existing | API | Anonymous shell bootstrap; P7 exposes resolved rail visibility without private data | Output-cached; must not grow private data |
| `src/Explore.Blazor.Client/Clients/EventApiClient.g.cs` + `schemas/openapi_islamu-event.json` | Existing | Contract | Regenerated in P3.2 and Task 7.1 through the documented build workflow | Coordinate with webhook workstream |
| `src/Explore.Domain/Constants/GovernanceSettingKeys.cs` + `src/Explore.Domain/Settings/SettingRegistry.cs` | Existing | Domain | `ui_shell.*` (tenant/instance) + `ui_shell_preferences.*` (user) keys (P7, D7/D8) | Explicit allowed scopes; instance-lockable |
| `src/Explore.Domain/UserPreference.cs` + `SettingsController` (`api/settings/user/{category}`) | Existing | Domain/API | Storage + API for durable layout prefs — REUSED, no new table/endpoint (D7) | Pattern precedent: `AiAssistantPreferences` category |
| `src/Explore.Blazor.Client/Services/Interop/LocalStorageDockLayoutPersistence.cs` | Existing | Blazor | Anonymous persistence; wrapped by new `ServerBackedDockLayoutPersistence` (P7) | Restore is defensive: clamps, drops unknown ids |
| `src/Explore.Blazor.Client/Services/Ai/{AiAssistantClientService,AiAssistantConversationState}.cs` | Existing | Blazor | Shared conversation stack for dock + `/ai` workspace (D10) | Scoped; both surfaces share the instance |
| `src/Explore.API/Controllers/EventController.cs` (`GET api/event/my` → `GetMyEventsAsync`; `GET api/event/managed/:actorId` → `GetManagedEventsByActorAsync`) | Existing | API | Studio events list: org/group actors use `GetManagedEventsByActorAsync`; personal/unscoped fallback uses `GetMyEventsAsync` | No new my-events endpoint; no group-specific create route |
| `docs/adr/ADR-019-workspace-shell-composition.md` | New (P1) | Docs | Records D1–D3 + vocabulary | Avoids registration-data-collection's reserved ADR-016–018 range |
| `docs/DOCK_LAYOUT.md`, `docs/BLAZOR.md`, `docs/API.md`, `docs/API_CHANGELOG.md`, `docs/CONFIGURATION.md`, `docs/ACCESSIBILITY.md` | Existing | Docs | Updated in the phases that change the described behavior | Fold into owning tasks |

## Key Decisions (synchronized with plan §5)

- **D1:** App rail = permanent shell chrome, not a dock panel (Start-side stacking is tabbed; rail must not close/resize/persist).
- **D2:** Compile-time `WorkspaceRegistry` + route-prefix classifier; route normally owns active workspace. `UiShellState` holds last-route-per-workspace and the one session-only Personal Settings utility exception; direct Personal loads still classify as Settings.
- **D3:** `shell.left-nav` → `shell.workspace-nav`, clean rename, no alias (defensive snapshot restore drops stale ids).
- **D4:** New authenticated `GET api/ui-shell/context` with `[PrivateNoStore]`, `[Authorize]`, `EndpointClass.Authenticated`, plain DTO; anonymous stays on `PublicExperience/shell`; no membership leakage into cacheable anonymous contract.
- **D5:** Studio event-level nav sections gated purely by event resource `_links` (edit/publish-readiness/session/registration/team relations).
- **D6:** Delete `SidebarState`; keep `AiAssistantState` (it computes effective AI availability, not just dock mirroring).
- **D7:** Layout prefs reuse `UserPreference` + `api/settings/user/{category}`; keys `ui_shell_preferences.layout.v1` / `.last_workspace` / `.last_actor`; anonymous storage gains tenant discriminator (`dock_layout:v1:{tenantSlug}:`) with no old-key compatibility read; promote-on-login. Task 7.2 optionally adds `ui.settings.last_scope.v1` for the re-authorized dedicated hub/selector only; Personal origin is never durable.
- **D8:** Tenant defaults are governance keys `ui_shell.*` (tenant/instance scope, lockable): rail public visibility, Events/Studio/AI default nav mode, user-override allowance, organizer default workspace. Task 7.1 removes the already-registered Settings default-nav key because hybrid Settings has no shell navigation provider.
- **D9:** Hybrid Settings = one bottom gear whose primary action always opens `/settings/personal`; Personal is contextual over the in-session Explore/Studio/AI origin but direct load/refresh is dedicated Settings. `/settings` is the authorized scope hub; canonical routes are `/settings/personal`, `/settings/personal/:section`, `/settings/organization/:id`, `/settings/group/:id`, `/settings/tenant`, and `/settings/instance`. Delete the placeholder `SettingsWorkspaceNavigation`; Personal owns compact in-page sections and existing admin layouts keep their internal sidebars. Gear menu/profile links/selector use only `SettingsScopeDto`; single-tenant "Site administration" is presentation-only. Old admin routes/directives are removed without aliases.
- **D10:** `/ai` workspace + dock share `AiAssistantConversationState`/`IAiAssistantClientService`; dock header gets "Open in AI workspace"; product name stays "AI Assistant"; `/ai` routes are authenticated-only (`AuthenticatedRouteGuard`).
- **D11:** every first-party Personal Settings entry uses one explicit `UiShellState` capture-before-navigation contract; `/settings/personal` is searchable View all; section navigation is sticky vertical on desktop and stacked on narrow layouts; Personal-only scope selection is hidden.
- **Managed actors:** reuse `IAiAssistantActorContextService.ListAuthorizedActorContextsAsync` for organization + group actor list; personal events fallback uses `GetMyEventsRequest`.
- **Actor provenance:** `AiAssistantActorContextDto.ScopeId` carries the organization/group aggregate ID separately from `ActorId`; UI-shell settings scopes and primary-organization pinning must use `ScopeId`.
- **Group-publisher investigation (§2.6):** resolved — group actors are included in the managed-actor list; event creation for groups uses the existing `/events/create` publisher picker (no new group-specific create route).
- **Experience profile:** reuse existing `PublicExperienceMode { DiscoveryCentric, OrganizationCentric }` + `public_experience.primary_organization_id`; do NOT invent Marketplace/OrganizationHub parallel enums.

## Constraints And Rules To Remember

- Fallback intent contract (no single matching intent): `add-get-endpoint` + `add-cqrs-handler` + `openapi-contract-change` + `blazor-component-affordance`/`add-hal-link`.
- Client isolation: `Explore.Blazor.Client` consumes ONLY generated `IEventApiClient` models (QUICK_REFERENCE #23; NU1605/WASM memory note).
- HAL links gate per-resource affordances; capabilities/roles gate only broad workspace/menu eligibility (QUICK_REFERENCE #21).
- Controller standard: explicit template + `RouteNames` + `[EndpointClassification]` + `[ProducesResponseType]` + `[PrivateNoStore]`; operationId `GetUiShellContext`.
- Never hand-edit `EventApiClient.g.cs`; regen via the documented msbuild GenerateApiClient path; exact generated method name verified after regen rather than assumed; generated-artifact diff serialized and isolated; no commit unless explicitly requested.
- Dock invariants: descriptor-owned width/persistence; viewport projection never autosaves; logical Start/End; no central panel enum; no page-level shell compensation; workspace content floors are generic hint (no workspace-specific branching in `DockLayoutState`).
- A11y architecture tests: skip link, main/header/nav landmarks, live regions, `<h1>` per page, logical CSS properties.
- Dev mode: delete removed routes/services/tests — no compatibility shims, no `[Skip]` for obsolete behavior.
- Personal Settings origin is session-only `UiShellState` derived from actual navigation; never accept `returnUrl` or persist the origin. Only one rail item may own `aria-current="page"`; the Settings gear uses a separate utility-open state.
- Durable `ui.settings.last_scope.v1` belongs to Task 7.2, is re-authorized before use, and can influence only the dedicated hub/selector—not the gear primary action.
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
- Settings origin/active-state correctness → 6.1 (in-session origin versus direct load/refresh; one `aria-current`; origin clears on exit).
- Settings link sweep and scope-authority completeness → 6.2–6.3 (source/test sweep includes `@page` directives; `/admin/tenant/navigation` was never in `Routes.razor`; table proves instance authority never implies Tenant).
- Profile-vs-rail contextual entry parity and safe multi-section composition → 9.1–9.2.
- Sticky offset, focus visibility, and 320/375px responsive projection → 9.3 / 10.1.
- NSwag regen collision with webhook workstream → 3.2 (coordinate before landing; dirty-file hunk preservation explicit).

## Handoff Notes

### Handoff — 2026-07-22 Europe/Brussels (Settings information architecture re-baseline)
- **Current state:** 26/30 implementation tasks are complete. Existing anonymous browser evidence remains partial baseline; no revised Settings runtime code was changed by this re-baseline.
- **Next action:** Start Task 9.1 with a failing profile-dropdown interaction test, then add the shared Personal Settings navigation contract.
- **Blockers:** No hard implementation blocker. Authenticated browser personas remain unavailable until usable local identities/authority are provided; this affects Task 10.1, not Phase 9 deterministic work.
- **Modified files:** the three Dynamic Event Management UI planning artifacts and `.omo/plans/dynamic-event-management-ui.md` only.
- **Validation:** repository/runtime state inspected; no Aspire AppHost is running; current Settings routes, components, tests, and entry producers were read directly.
- **Documentation impact:** D11 and new Phase 9 define the revised Settings UX; prior final browser QA moves to Phase 10.
- **Risks:** rendering all nine Settings sections together may expose duplicate IDs, repeated page headings, or lifecycle assumptions; Task 9.2 must fix those at the owning component rather than suppressing sections.
- **Notes for next contributor/agent:** preserve all unrelated dirty files; do not add compatibility aliases or persist Personal Settings origin.

### Handoff — 2026-07-22 Europe/Brussels (Task 8.1 complete)
- **Current state:** 24/27 implementation tasks are complete. The Xs rail projects to one bottom nav; generic per-scope content floors preserve workspace canvas width by overlaying constrained Start panels without mutating durable state.
- **Next action:** Start Task 8.2 with focus, landmark, navigation-label, and RTL architecture characterization.
- **Blockers:** None for Task 8.1. Phase 7 API integration remains environment/unrelated-baseline blocked.
- **Modified files:** `DockLayoutState`, `DockLayoutHost`, `MainLayout`, dock/layout tests, and evidence ledgers.
- **Validation:** AppWorkspaceRail characterization 9/9; combined affected shell/dock/rail lane 33/33; snapshot equality and `ViewportPolicy` classification explicitly asserted.
- **Documentation impact:** workstream ledgers advance to Task 8.2; responsive QA matrix documentation remains owned by Task 8.3.
- **Risks:** Workspace-switch focus must not double-announce route content already focused by the shell.

### Handoff — 2026-07-22 Europe/Brussels (Task 7.3 complete)
- **Current state:** 23/27 implementation tasks are complete. Tenant administrators can configure all retained D8 workspace-shell defaults through the existing settings API; every control follows server-effective editability and lock reasons.
- **Next action:** Start Task 8.1 with failing-first Xs bottom-navigation and generic canvas-floor projection tests.
- **Blockers:** No Task 7.3 blocker. Phase 7 API integration is environment-blocked by unavailable Docker/Testcontainers and unrelated recorded suites; focused Task 7.3 lanes and Release build are green.
- **Modified files:** new tenant-shell settings service contract/adapter/component/CSS/tests, tenant settings layout/DI, and workstream evidence ledgers.
- **Validation:** component 2/2; affected tenant settings 2/2; architecture/governance 17/17; Release build 0 errors; API integration 1,384/1,899 passed with 514 environment/unrelated failures and 1 skip.
- **Documentation impact:** tasks/context/orchestration ledgers advance to Task 8.1; no public API/configuration contract changed.
- **Risks:** Responsive projection must not leak workspace-specific branching into `DockLayoutState` or persist viewport policy.

### Handoff — 2026-07-22 Europe/Brussels (Task 7.2 complete)
- **Current state:** 22/27 implementation tasks are complete. Authenticated layout and shell selections persist through the existing user-settings API; anonymous layouts are tenant-discriminated and promote once; every restored authority-bearing selection is revalidated.
- **Next action:** Start Task 7.3 with failing-first tenant Shell settings rendering/editability coverage, then reuse the existing effective-setting controls and lock-reason pattern.
- **Blockers:** No Task 7.2 blocker. Full client retains three unrelated HAL/report-dialog failures; Architecture retains four unrelated schema/HATEOAS/naming/organization-guardrail failures.
- **Modified files:** dock persistence and DI, shell preference contracts/service/state, `MainLayout`, `UiShellState`, Settings scope selector, governance definition/configuration docs, focused client/architecture tests, and evidence ledgers.
- **Validation:** focused client 4/4; extended affected fixtures 6/6; focused architecture/governance 17/17; Release build exit 0 with 0 errors; full client 1,969/1,973 passed with 3 unrelated failures and 1 skip; full Architecture 281/286 passed with 4 unrelated failures and 1 skip; diff and production stale-key sweeps clean.
- **Documentation impact:** `ui.settings.last_scope.v1` is documented as a User-only preference; task/context/orchestration ledgers advance to Task 7.3.
- **Risks:** Task 7.3 must display server-provided editability and lock reasons without local authority inference; rendered browser evidence remains deferred to Phase 9.

### Handoff — 2026-07-22 Europe/Brussels (Task 7.1 complete)
- **Current state:** 21/27 implementation tasks are complete. Retained D8 governance is projected through the existing cascade, public rail visibility is in the anonymous shell contract/revision, and the obsolete Settings navigation key is absent.
- **Next action:** Start Task 7.2 with failing-first persistence/promotion/pruning coverage, then decorate the existing dock persistence with the existing user-settings API.
- **Blockers:** No Task 7.1 blocker. Full Application has two unrelated failures, Domain has one unrelated ATProto registry mismatch, Architecture has the same four unrelated failures, and Phase 6 client failures remain recorded.
- **Modified files:** UI-shell/public-experience DTOs and handlers, UI-shell governance constants/definitions, focused tests, `docs/CONFIGURATION.md`, generated OpenAPI/client artifacts, and workstream evidence ledgers.
- **Validation:** focused Application 11/11; governance Architecture 9/9; Release build exit 0 with 0 errors; `git diff --check`, generated-contract parity, and production stale-key sweeps clean.
- **Documentation impact:** configuration docs now describe active runtime resolution and only retained D8 keys; task/context/orchestration ledgers advance to Task 7.2.
- **Risks:** Task 7.2 must honor `AllowUserOverride=false`, keep anonymous storage tenant-discriminated, re-authorize durable actor/workspace/settings scope state, and never persist Personal return origin.

### Handoff — 2026-07-22 Europe/Brussels (Hybrid Settings re-baseline)
- **Current state:** Phase 5 implementation is complete. Phase 6 has been re-baselined to the user-approved hybrid Settings architecture; no runtime code was changed by this planning update.
- **Next action:** Start Task 6.1 with failing-first `UiShellState`, app-rail, registry, and navigation-host tests for contextual Personal Settings and direct-load behavior; then remove the placeholder Settings provider.
- **Blockers:** No hard blocker. Existing unrelated client/architecture failures remain recorded; unrelated ATProto and generated-artifact changes must remain untouched.
- **Modified files:** `.omo/plans/dynamic-event-management-ui.md` and this workstream's plan/context/tasks documentation only.
- **Validation:** Documentation sections were synchronized; no build or product test is required for a plan-only re-baseline.
- **Documentation impact:** D2/D7/D8/D9, Phase 6, Task 7.2, risks, checklist, and orchestration summary now encode the hybrid architecture.
- **Risks:** Do not reintroduce a shell Settings sidebar, infer a return origin from URL input, let a durable last scope redirect the gear away from Personal, or duplicate existing admin section sidebars.
- **Notes for next contributor/agent:** The older report statement that the rail gear opens the broader hub is superseded by D9: gear primary is always Personal; the hub and authorized scopes remain available through the gear menu and profile dropdown.

### Handoff — 2026-07-22 Europe/Brussels
- **Current state:** Task 4.1 is complete. Studio has authenticated `/studio` and `/studio/events` shells, server-context actor reconciliation, pinned/single read-only identity modes, multi-actor switching, and Overview/Events workspace navigation.
- **Next action:** Start Task 4.2 by adding the smallest actor-scoped Studio event service/list and HAL-gated dashboard/list affordances; do not add event-detail navigation before Task 4.3.
- **Blockers:** No hard blocker. Existing unrelated Phase 0/1/2/3 suite failures remain recorded in `tasks.md`; browser evidence remains deferred to Phase 9; unrelated dirty files must be preserved.
- **Modified files:** `Routes.razor`, `UiShellState.cs`, `WorkspaceRegistry.cs`, new Studio navigation/switcher/page components and scoped CSS, focused client tests, and this workstream's three dev-doc ledgers.
- **Validation:** Five focused client lanes pass 34/34; `/studio/events` active state is asserted through `aria-current="page"`; Release build passes with 0 errors; `git diff --check` is clean; Oracle returned PASS with no required fixes. C#/Razor LSP was unavailable, so the Release compiler was the authoritative diagnostic gate.
- **Documentation impact:** Task 4.1 completion and the corrected Task 4.1/4.3 route boundary are recorded in plan/context/tasks; product/API docs are unchanged because this task adds no API or public configuration contract.
- **Risks:** Task 4.2 must preserve actor IDs as event-query keys while continuing to use `ScopeId` only for settings routes; all event actions must remain HAL-affordance gated.

### Handoff — 2026-07-21 Europe/Brussels
- **Current state:** Tasks 0.1, 0.2, Phase 1 Tasks 1.1–1.3, and Phase 2 Tasks 2.1–2.3 are implemented. The shell registers only `shell.workspace-nav`; providers are contextual; `SidebarState` is deleted; AI policy remains in `AiAssistantState`.
- **Next action:** Complete the Phase 2 review gate, then start Phase 3 Task 3.1 (application UI-shell context query/DTO/handler).
- **Blockers:** Phase 0 architecture and Phase 1 Blazor client suites have unrelated existing failures recorded in `tasks.md`; rendered breakpoint QA remains deferred to Phase 9; NSwag regen coordination remains noted; unrelated dirty files must be preserved.
- **Modified files:** `dev/active/dynamic-event-management-ui/dynamic-event-management-ui-plan.md`, `dynamic-event-management-ui-context.md`, `dynamic-event-management-ui-tasks.md`.
- **Validation:** Task 2.2 focused client suites pass 149/149. Task 2.3/review affected suites pass 105/105: MainLayout 33, panel lifecycle 6, NavMenu admin 17, authentication flow 22, and EventList 27; unchanged cached dock architecture tests pass 5/5. The review regressions were red before the policy-state fix (three persistence/provenance failures) and green afterward. The Release/architecture rebuild is temporarily blocked by the separately owned ATProto workstream; do not inspect it. Phase 1 full-suite baseline remains 1,856 passed, 3 unrelated failures, 1 governed skip; Phase 0 architecture remains 280 passed, 4 unrelated failures, 1 governed skip.
- **Documentation impact:** `docs/CONFIGURATION.md` now documents all `ui_shell.*` and `ui_shell_preferences.*` definitions, scopes, locks, defaults, and allowed values.
- **Risks:** See risk register (plan §13).
- **Notes for next contributor/agent:** The report file is input, not truth — plan §2.1 is the verified baseline. Do not rewrite `NavMenu` in one pass; it is edited incrementally in 2.3/3.3/4.4 by design. AnySearch MCP was unavailable during original planning; fallback evidence sources were official Plane/W3C docs plus Context7 MudBlazor docs.
