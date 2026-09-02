<!-- ABOUTME: Decision record for route-derived workspaces, permanent app-rail chrome, and contextual navigation. -->
<!-- ABOUTME: Defines shell vocabulary and keeps application chrome separate from generic dock layout state. -->

# ADR-019: Workspace Shell Composition

| | |
|---|---|
| **Status** | Accepted |
| **Date** | 2026-07-21 |
| **Deciders** | ISLAMU Event Platform — Architecture, Product, Blazor workstreams |
| **Supersedes** | Static discovery sidebar as the application-wide navigation model |
| **Superseded by** | — |

## Context

`MainLayout` currently combines a top bar with a generic `DockLayoutHost`. The Start-side `shell.left-nav` panel always renders event-discovery navigation, while the End-side `shell.ai-assistant` panel provides contextual AI. Organizer and settings routes therefore inherit discovery navigation that does not match their task.

The dock engine owns closable and resizable panels, persistent descriptor state, tab-oriented same-side stacking, and responsive projection. A permanent workspace rail has different semantics: it selects application areas, must always remain reachable, and must not become closable, resizable, or persisted as user dock state.

Routing is centralized in `Routes.razor` through Blazouter. Deep links and browser refreshes make the route the only reliable source of the active workspace; a parallel mutable workspace flag would drift.

## Decision

### Permanent app rail

Add `AppWorkspaceRail` as a fixed logical inline-start track owned by `MainLayout`, outside `DockLayoutHost`.

- The rail is shell chrome, not a dock panel.
- It is not registered with `DockLayoutState` and has no persisted panel state.
- It projects to bottom navigation at the mobile breakpoint.
- Setup/onboarding routes using `SetupLayout` do not render it.

### Compile-time workspace registry

Define workspaces in a compile-time `WorkspaceRegistry`. The Phase 1 descriptor owns a stable key, localized label key, icon, base route, and authentication requirement. Phase 2 adds the optional navigation provider; Phase 3 adds server-authoritative availability evaluation.

`WorkspaceRouteClassifier` derives the active workspace by longest registered route-prefix match. Unknown routes safely fall back to Events. `UiShellState` observes `NavigationManager.LocationChanged`, publishes the active workspace, and remembers the last route per workspace for the current session, including query strings.

The registry is an extension point for code-owned workspaces, not a runtime plugin system or tenant-editable navigation schema.

### One contextual navigation panel

Rename `shell.left-nav` to `shell.workspace-nav`. Its content is supplied by the active workspace through `WorkspaceNavigationHost`.

- Events receives the existing discovery navigation content.
- Studio, AI, and Settings provide their own contextual navigation when implemented.
- A workspace without navigation leaves the canvas full width.
- Event-level Studio navigation replaces actor-level Studio navigation in the same panel; it never creates a third sidebar.

There is no compatibility alias. Defensive snapshot restore already drops unknown panel IDs, so old `shell.left-nav` entries are ignored.

## Vocabulary

| Term | Meaning |
|---|---|
| **Workspace** | Route-addressable application area such as Events, Studio, AI, or Settings. |
| **App rail** | Permanent shell chrome for switching workspaces; not a dock panel. |
| **Workspace navigation** | The single Start-side contextual navigation panel, `shell.workspace-nav`. |
| **Contextual dock** | Closable/resizable task surface managed by `DockLayoutState`, such as `shell.ai-assistant`. |
| **Experience profile** | Tenant public-experience posture represented by the existing `PublicExperienceMode`. |
| **Acting actor** | Server-authorized organization or group currently selected for Studio work; selection grants no authority. |
| **Settings scope** | Personal, organization, group, tenant, or instance settings context supplied by the authenticated shell context. |
| **Policy** | Server-authoritative availability/default/lock rule. |
| **Preference** | User-owned layout or last-selection state within policy boundaries. |

## Alternatives Considered

1. **Model the rail as another Start-side dock panel** — rejected because same-side panels stack as tabs and dock panels can close, resize, and persist.
2. **Add a rail-specific mode to the dock engine** — rejected because it would leak application chrome into a generic layout system.
3. **Store active workspace as mutable scoped state** — rejected because deep links, refreshes, and browser navigation can make it disagree with the route.
4. **Keep `shell.left-nav` as a compatibility alias** — rejected because the old name no longer describes the panel and pre-1.0 development policy favors clean removal.
5. **Use a runtime plugin registry** — rejected as speculative complexity; the application owns a bounded set of compiled workspaces.

## Consequences

- `MainLayout` owns an explicit app-rail track while `DockLayoutHost` remains workspace-agnostic.
- Routes determine active workspace and remain valid on deep link and refresh.
- Workspace navigation can change without re-registering the dock panel.
- Old snapshots lose only the obsolete `shell.left-nav` entry; no migration or cleanup code is required.
- Tenant policy controls workspace availability and defaults, while user preferences control personal layout within those boundaries.
- New workspaces require a descriptor, route classification coverage, and an optional navigation provider rather than conditionals spread across `NavMenu` and `MainLayout`.

## Related

- [Dock Layout](../DOCK_LAYOUT.md)
- [Blazor Architecture](../BLAZOR.md)
- [ADR-004: Accessibility Architecture](ADR-004-accessibility-architecture.md)
- `dev/active/dynamic-event-management-ui/dynamic-event-management-ui-plan.md`
