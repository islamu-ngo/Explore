# Tasks: Blazor Feature Parity

This checklist outlines the tasks required to implement the missing Blazor features.

## Phase 1: Event Session Management

- [ ] **Task 1.1:** Create `CreateSessionDialog.razor` component for adding a new session to an event.
- [ ] **Task 1.2:** Create `EditSessionDialog.razor` component for editing an existing session.
- [ ] **Task 1.3:** Create `EventSessionManager.razor` component to list sessions for an event and provide buttons for create, edit, and delete operations.
- [ ] **Task 1.4:** Integrate `EventSessionManager.razor` into the `EventDetail.razor` page.

## Phase 2: Admin Core Data Management

- [ ] **Task 2.1:** Create `Categories.razor` admin page with a data grid to display, create, edit, and delete categories.
- [ ] **Task 2.2:** Create `Tags.razor` admin page with a data grid to display, create, edit, and delete tags.
- [ ] **Task 2.3:** Create `Locations.razor` admin page with a data grid to display, create, edit, and delete locations.
- [ ] **Task 2.4:** Add links to the new admin pages in `NavMenu.razor` (visible to admins only).

## Phase 3: Enhanced Registration Management

- [ ] **Task 3.1:** Create `RegistrationManagerDialog.razor` to display a list of registrations for an event session.
- [ ] **Task 3.2:** Add functionality to the `RegistrationManagerDialog` to allow event organizers to approve or reject pending registrations.
- [ ] **Task 3.3:** Replace the existing `EventRegistrationsDialog.razor` with the new `RegistrationManagerDialog` or integrate the new functionality into it.
