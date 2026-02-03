# Plan: Blazor Server + Client Enterprise Refactor

Last Updated: 2026-01-13

## Executive Summary
This plan refactors the Blazor Server (BFF) and Blazor Client (WASM) layers into a clean, scalable, enterprise-grade architecture. The focus is to remove fragile ad-hoc patterns (state persistence hacks, scattered auth and API behaviors, inline styling, and mixed concerns) and replace them with a consistent feature-based structure, centralized BFF policies, standardized UI state patterns, and MudBlazor-aligned components. The outcome is a maintainable, testable, and scalable UI stack that matches Clean Architecture and ISLAMU domain requirements.

## Current State Analysis
Observed issues in the Blazor Server and Blazor Client projects:

1. Rendering + auth state flow is complex and fragile
   - PersistentComponentState usage is scattered (example: LandingPageForUsers) and creates hidden coupling between server and client.
   - Custom auth state providers are maintained in both server and client, instead of using current Blazor Web App auth-state serialization/deserialization patterns.
   - Interactive render mode configuration is applied directly in the App layout without a consistent policy for interactive vs static pages.

2. UI composition and styling are not centralized
   - Large inline style blocks inside pages are hard to reuse and are not componentized.
   - Theme toggling mixes cookie and localStorage with JS eval, which is brittle.
   - MudBlazor providers exist in MainLayout, but there is no clear system for reusable design tokens, typography, or component-level styling conventions.

3. API usage lacks consistent abstraction boundaries
   - Pages and components frequently inject IEventApiClient directly, mixing transport logic with UI logic.
   - Public vs protected request behavior is not centralized in a single policy-based layer across BFF and client usage.
   - Error handling and retry behaviors are inconsistent and scattered.

4. Feature organization does not scale
   - Components, pages, dialogs, and services are mixed by type rather than by feature.
   - Shared UI patterns (tables, dialogs, forms, empty states) are reimplemented in different areas.

5. Testing coverage is thin for UI
   - No clear bUnit test strategy for components.
   - No integration tests verifying BFF proxy behavior for public vs protected routes.

## Proposed Future State

1. Clear feature-based organization
   - Group UI by feature domain (Event, Organization, Admin, User, Landing).
   - Co-locate pages, components, dialogs, and view models for each feature.

2. Standardized UI state patterns
   - Implement a common ViewModel pattern or ResultState wrapper for loading/error/empty states.
   - Avoid PersistentComponentState for UI data; use explicit, testable state services.

3. Centralized BFF policy and API access
   - Centralize BFF routing policies for public vs protected endpoints.
   - Provide a unified client-side API facade (feature services) to hide transport details.

4. MudBlazor-aligned architecture
   - Use MudBlazor Layout, DialogProvider, SnackbarProvider, and Form validation patterns consistently.
   - Move inline page CSS into component-scoped .razor.css files or a shared design system stylesheet.

5. Auth state aligned with Blazor Web App guidance
   - Use the standard auth state serialization/deserialization approach in server/client.
   - Keep authentication logic at the BFF boundary and make client components rely on CascadingAuthenticationState.

## Implementation Phases (Clean Architecture Alignment)

### Phase 1: Presentation Layer Foundation (Blazor Server BFF)
Goal: Centralize policies, rendering, and cross-cutting behaviors.

Key tasks:
1. BFF policy and endpoint routing
   - File: Explore.Blazor/Program.cs
   - Centralize public/protected endpoint lists and proxy mappings.
   - Acceptance:
     - Public endpoints are proxied without token attachment.
     - Protected endpoints require auth and attach user access token.

2. Render mode governance
   - File: Explore.Blazor/Components/App.razor
   - Define a single render mode policy for the app and document exceptions.
   - Align HeadOutlet render mode with Routes render mode.
   - Acceptance:
     - Render mode definition is centralized and consistent.

3. Auth state alignment with Blazor Web App guidance
   - Files: Explore.Blazor/Services/PersistingServerAuthenticationStateProvider.cs, Explore.Blazor.Client/Services/BffAuthenticationStateProvider.cs
   - Replace ad-hoc auth state persistence with standardized serialization/deserialization where viable.
   - Acceptance:
     - Auth state propagation works without custom component persistence hacks.

### Phase 2: UI Architecture and Shared Building Blocks
Goal: Standardize UX patterns and enable reusability.

Key tasks:
1. Feature-based folder structure
   - Create feature folders with Pages, Components, Dialogs, and ViewModels.
   - Acceptance:
     - Each feature is self-contained and testable.

2. Shared UI library
   - Create shared components (Loaders, EmptyState, ErrorState, ConfirmDialog).
   - Standardize MudBlazor dialog patterns and form validation usage.
   - Acceptance:
     - No duplicate dialog logic across features.

3. Theming and styling system
   - Move inline CSS from pages to .razor.css or shared styles.
   - Standardize palette, typography, spacing tokens.
   - Acceptance:
     - Theme changes propagate without inline overrides.

### Phase 3: Client API Abstractions and Services
Goal: Keep transport concerns out of UI and stabilize API usage.

Key tasks:
1. Feature service layer
   - Add feature-specific services wrapping IEventApiClient.
   - Hide endpoint paths and error mapping from components.
   - Acceptance:
     - Components call feature services, not raw IEventApiClient.

2. Consistent error handling
   - Use a shared Result or ApiResponse pattern with standardized error mapping.
   - Acceptance:
     - All feature services return consistent error states.

3. Caching and performance
   - Add caching for lookup tables and low-churn data.
   - Acceptance:
     - Cached lookups do not trigger repeated API calls.

### Phase 4: UX and Accessibility Refinement
Goal: Ensure enterprise-grade UX, accessibility, and responsiveness.

Key tasks:
1. Accessibility and UX standards
   - Ensure forms include labels, validation messages, and ARIA where needed.
   - Acceptance:
     - Critical UI flows are accessible and keyboard-friendly.

2. Responsive design
   - Use MudGrid and responsive breakpoints consistently.
   - Acceptance:
     - Core pages function on mobile and desktop.

### Phase 5: Testing, Observability, and Documentation
Goal: Maintain quality as complexity grows.

Key tasks:
1. bUnit component tests
   - Create tests for shared components and critical feature dialogs.
   - Acceptance:
     - Tests validate render and interaction behaviors.

2. Integration tests for BFF proxy
   - Validate anonymous vs authenticated requests through BFF.
   - Acceptance:
     - Public endpoints work anonymously; protected endpoints return 401 without auth.

3. Developer documentation
   - Document patterns for feature structure, UI state, and service usage.
   - Acceptance:
     - New feature onboarding is self-explanatory.

## Detailed Tasks (Actionable)

### Phase 1 Tasks
1.1 Centralize BFF route policies
- Files: Explore.Blazor/Program.cs, Explore.Blazor/Extensions/*
- Acceptance:
  - [ ] Public-only endpoints use RequiredTokenType.None
  - [ ] Protected-only endpoints use RequiredTokenType.User and RequireAuthorization
  - [ ] Default /api/v1 mapping uses RequiredTokenType.UserOrNone
- Effort: M
- Skills: blazor-mudblazor-guidelines, clean-architecture-rules

1.2 Align render modes and auth state handling
- Files: Explore.Blazor/Components/App.razor, Explore.Blazor/Services/PersistingServerAuthenticationStateProvider.cs, Explore.Blazor.Client/Program.cs
- Acceptance:
  - [ ] Routes and HeadOutlet share render mode
  - [ ] Auth state serialization/deserialization aligns with Blazor Web App guidance
- Effort: M
- Skills: blazor-mudblazor-guidelines

### Phase 2 Tasks
2.1 Feature-based folders
- Files: Explore.Blazor.Client/Features/*
- Acceptance:
  - [ ] Event, Organization, Admin, User, Landing each have Pages + Components
- Effort: L
- Skills: blazor-mudblazor-guidelines

2.2 Shared UI components
- Files: Explore.Blazor.Client/Components/Shared/*
- Acceptance:
  - [ ] Common dialogs, loaders, empty/error states reused by features
- Effort: M
- Skills: blazor-mudblazor-guidelines

2.3 Styling system
- Files: Explore.Blazor.Client/Styles/*, *.razor.css
- Acceptance:
  - [ ] Inline styles removed from pages
  - [ ] Theme tokens centralized
- Effort: L
- Skills: blazor-mudblazor-guidelines

### Phase 3 Tasks
3.1 Feature services
- Files: Explore.Blazor.Client/Services/Feature/*
- Acceptance:
  - [ ] UI components use feature services instead of raw API client
- Effort: L
- Skills: clean-architecture-rules

3.2 Error/result standardization
- Files: Explore.Blazor.Client/Models/ResultState.cs
- Acceptance:
  - [ ] UI uses ResultState for loading/error/success flows
- Effort: M
- Skills: blazor-mudblazor-guidelines

3.3 Lookup cache
- Files: Explore.Blazor.Client/Services/LookupCacheService.cs
- Acceptance:
  - [ ] Lookup calls cached with invalidation policy
- Effort: M
- Skills: blazor-mudblazor-guidelines

### Phase 4 Tasks
4.1 Accessibility upgrades
- Files: Feature pages and shared components
- Acceptance:
  - [ ] Form fields have labels and validation summaries
  - [ ] Dialogs are keyboard accessible
- Effort: M
- Skills: blazor-mudblazor-guidelines

4.2 Responsive UX pass
- Files: Feature pages
- Acceptance:
  - [ ] Core flows usable on mobile breakpoints
- Effort: M
- Skills: blazor-mudblazor-guidelines

### Phase 5 Tasks
5.1 Component tests (bUnit)
- Files: tests/Explore.Blazor.Client.Tests/*
- Acceptance:
  - [ ] Shared components covered by tests
- Effort: M
- Skills: blazor-mudblazor-guidelines

5.2 BFF integration tests
- Files: tests/Explore.Integration.Tests/*
- Acceptance:
  - [ ] Public/protected proxy behaviors validated
- Effort: M
- Skills: clean-architecture-rules

5.3 Documentation updates
- Files: docs/ARCHITECTURE.md, docs/SECURITY.md, docs/API.md, docs/PROJECT.md
- Acceptance:
  - [ ] UI architecture and BFF policies documented
- Effort: S
- Skills: blazor-mudblazor-guidelines

## Risk Assessment and Mitigation

1. Auth state regressions
- Mitigation: Incremental rollout, test with real auth flows, fallback toggle.

2. UI regression from refactoring
- Mitigation: bUnit tests + manual smoke tests per feature.

3. Breaking API expectations
- Mitigation: Keep transport contract stable and wrap with feature services.

4. Performance issues
- Mitigation: Use caching for lookup data and avoid excessive StateHasChanged calls.

## Success Metrics
- 100% of feature pages use standardized UI state and shared components.
- All API calls go through feature services (no direct IEventApiClient usage in pages).
- BFF policy enforcement is centralized and testable.
- UX passes mobile and desktop smoke tests.

## Required Resources and Dependencies
- .NET 10 SDK
- MudBlazor and current UI theme assets
- BFF proxy and auth configuration in Explore.Blazor
- bUnit and test harness for UI

## Effort Estimates
- Phase 1: M
- Phase 2: L
- Phase 3: L
- Phase 4: M
- Phase 5: M

## References
- MudBlazor docs (forms, dialogs, layout, grid)
- Blazor Web App render modes and auth state serialization
- Project docs: docs/ARCHITECTURE.md, docs/SECURITY.md, docs/DOMAIN.md
- Skills: blazor-mudblazor-guidelines, clean-architecture-rules, cqrs-mediatr-guidelines
