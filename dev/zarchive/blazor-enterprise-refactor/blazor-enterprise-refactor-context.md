# Blazor Enterprise Refactor - Context

Last Updated: 2026-01-13

## SESSION PROGRESS (2026-01-13)

### COMPLETED
- Aligned Blazor auth state with serialization/deserialization in server/client.
- Switched app render mode to InteractiveAuto (prerender disabled) consistently.
- Removed PersistentComponentState usage from LandingPageForUsers.
- Introduced CSS isolation files for all Razor components and migrated inline style blocks.
- Refactored admin/tag/location/session components to use feature services (no direct IEventApiClient).
- Renamed ProgramService to EventService (partial), consolidated event/session/registration operations.
- Updated event UI to use event naming (no program terms), fixed registration to pass event session id, and corrected session selection dialog output.
- Fixed AdminService method brace errors and added detail fetch methods for edit dialogs.
- Added Duende.Bff.Yarp using in BFF endpoint mapping to resolve MapRemoteBffApiEndpoint.
- Build now succeeds (warnings remain).

### IN PROGRESS
- Continue service-layer refactors and shared UI state patterns.
- Consolidate theme tokens and reduce inline style attributes in markup.

### BLOCKERS
- None.

## Scope
Refactor Explore.Blazor (server BFF) and Explore.Blazor.Client (WASM) for enterprise-grade architecture, consistency, and scalability.

## Key Files (Current State)

- Explore.Blazor/Components/App.razor
  - Sets render mode and HeadOutlet; currently uses InteractiveServerRenderMode with prerender disabled.

- Explore.Blazor/Program.cs
  - BFF setup, auth, antiforgery, proxy routing, and render-mode configuration.

- Explore.Blazor/Services/PersistingServerAuthenticationStateProvider.cs
  - Custom auth state persistence for server to WASM hydration.

- Explore.Blazor.Client/Program.cs
  - Client service registrations and auth state provider registration.

- Explore.Blazor.Client/Services/BffAuthenticationStateProvider.cs
  - Custom auth state provider that calls /bff/me.

- Explore.Blazor.Client/Layout/MainLayout.razor
  - MudBlazor providers, layout structure, theme handling, and user sync.

- Explore.Blazor.Client/Pages/Landing/LandingPageForUsers.razor
  - Large inline styles and PersistentComponentState usage.

## Key Decisions (Planned)
- Centralize BFF routing policies for public vs protected endpoints.
- Replace scattered PersistentComponentState usage with consistent state patterns.
- Use feature-based folder structure and shared components for reuse.
- Implement feature services to hide IEventApiClient from UI.
- Align auth state with Blazor Web App guidance (serialization/deserialization).

## References
# Project
@docs/PROJECT.md

## Architecture & Technical Stack
@docs/ARCHITECTURE.md

## Domain Model & Business Logic
@docs/DOMAIN.md

## Security Architecture (AuthN/AuthZ)
@docs/SECURITY.md

## API
@docs/API.md

## Federation (W3C ATProto & ActivityPub)
@docs/FEDERATION.md

## Configuration
@docs/CONFIGURATION.md

## Operations (Deployment, Env Vars)
@docs/OPERATIONS.md

## Governance (Contributing)
@docs/GOVERNANCE.md

## Troubleshooting
@docs/TROUBLESHOOTING.md

- .claude/skills/blazor-mudblazor-guidelines/SKILL.md
- Context7 mcp: MudBlazor and ASP.NET Core Blazor Web App guidance.

## Quick Resume
1. Read the plan file.
2. Start with Phase 1 tasks in the plan.
3. Track progress in the tasks checklist.
