# Blazor Enterprise Refactor - Task Checklist

Last Updated: 2026-01-13

## Phase 1: Presentation Foundation (BFF + Render Modes) - COMPLETE
- [x] Task 1.1: Centralize BFF public/protected route mapping
  - Acceptance: public endpoints use RequiredTokenType.None; protected endpoints require auth
- [x] Task 1.2: Consolidate render mode policy in App.razor
  - Acceptance: Routes and HeadOutlet use consistent render mode
- [x] Task 1.3: Align auth state serialization/deserialization
  - Acceptance: custom persistence reduced; auth state flows work in server+WASM

## Phase 2: Feature-Based UI Structure - IN PROGRESS
- [ ] Task 2.1: Create feature folders (Event, Organization, Admin, User, Landing)
  - Acceptance: pages, components, dialogs, view models co-located
- [ ] Task 2.2: Extract shared UI components
  - Acceptance: shared loader, empty state, error state, confirm dialog in shared library
- [ ] Task 2.3: Standardize styling system (IN PROGRESS)
  - Acceptance: inline styles removed from pages; .razor.css or shared styles used

## Phase 3: Client API Abstractions - IN PROGRESS
- [ ] Task 3.1: Implement feature services wrapping IEventApiClient (IN PROGRESS - EventService consolidated for event/session/registration)
  - Acceptance: UI uses services, not raw API client
- [ ] Task 3.2: Add ResultState or ApiResponse model for consistent error handling
  - Acceptance: standardized loading/error/empty handling across features
- [ ] Task 3.3: Add lookup caching service
  - Acceptance: lookup data cached and reused

## Phase 4: UX and Accessibility - NOT STARTED
- [ ] Task 4.1: Accessibility pass on forms and dialogs
  - Acceptance: labels, validation, keyboard navigation verified
- [ ] Task 4.2: Responsive layout cleanup
  - Acceptance: main flows work across breakpoints

## Phase 5: Testing and Documentation - NOT STARTED
- [ ] Task 5.1: bUnit tests for shared components
  - Acceptance: critical shared components covered
- [ ] Task 5.2: Integration tests for BFF proxy behavior
  - Acceptance: public/protected routing verified
- [ ] Task 5.3: Documentation updates
  - Acceptance: architecture and BFF policies documented

## Quick Resume
1. Start with Phase 1 tasks.
2. Update context file after each major change.
3. Mark tasks complete as you finish them.
