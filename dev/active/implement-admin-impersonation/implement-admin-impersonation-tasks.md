# Tasks: Implement Admin Impersonation

**Last Updated**: 2026-01-30

---

This file provides a checklist for tracking the implementation of the Admin Impersonation feature. Refer to the `plan.md` file for detailed descriptions of each task.

### Phase 1: Domain Layer
- [ ] **Task 1.1**: Create `ImpersonationAuditLog` Entity in `Explore.Domain`.

### Phase 2: Application Layer
- [ ] **Task 2.1**: Create `StartImpersonationCommand` and its handler.
- [ ] **Task 2.2**: Create `StopImpersonationCommand` and its handler.
- [ ] **Task 2.3**: Define `IImpersonationService` interface in `Explore.Application`.

### Phase 3: Infrastructure Layer
- [ ] **Task 3.1**: Add `ImpersonationAuditLog` to `ExploreDbContext` and create EF Core migration.
- [ ] **Task 3.2**: Implement `ImpersonationService` in the `Explore.Blazor` (BFF) project.

### Phase 4: API & Presentation (Blazor BFF)
- [ ] **Task 4.1**: Create API endpoints for starting and stopping impersonation.
- [ ] **Task 4.2**: Update HATEOAS link policies with impersonation-aware `.When()` conditions.
- [ ] **Task 4.3**: Create BFF endpoints to be called by the Blazor Client.

### Phase 5: Blazor UI Layer
- [ ] **Task 5.1**: Create the `ImpersonateTenantModal.razor` component for Instance Admins.
- [ ] **Task 5.2**: Create the global "Impersonation Active" banner in the main layout.

### Phase 6: Testing & Documentation
- [ ] **Task 6.1**: Write comprehensive unit and integration tests for the new logic.
- [ ] **Task 6.2**: Update `docs/MULTI_TENANCY.md` and `docs/SECURITY.md` with details of the new feature.
## Context Reset Session Update (2026-02-15 21:26 Europe/Brussels)

- Status update: No task-state changes in this session for this track.
- Priority update: Keep existing ordering; analytics work was handled in a separate track.
- Next step: Resume from current in-progress or highest-priority unchecked item.

## Context Reset Session Update (2026-02-23 18:12 Europe/Brussels)

- Current implementation state: No new implementation changes in this session for this track.
- Key decisions made this session: Priority focused on admin consolidation handoff in navbar customization track.
- Files modified and why: None in this track during this session.
- Blockers/issues discovered: None newly discovered for this track.
- Next immediate steps: Continue from highest-priority unchecked items in this task file.

## Context Reset Session Update (2026-02-23 18:47 Europe/Brussels)

- Current implementation state: No direct implementation changes in this track during this session.
- Key decisions made this session: Prioritized completion and verification of admin consolidation in the navbar customization track.
- Files modified and why: None for this specific track in this session.
- Blockers/issues discovered: None newly discovered for this track.
- Next immediate steps: Continue from the highest-priority unchecked tasks in this track's tasks file.

---

## Session Checkpoint (2026-02-27 Europe/Brussels)

- [x] Reviewed task continuity status for context reset handoff.
- [ ] Resume implementation work from this task latest documented in-progress section.
- [ ] Re-validate with build/tests once implementation resumes.

## Session Handoff — 2026-05-03 Europe/Brussels

- [x] No task-state changes were made for this workstream during the sidebar dock refactor handoff session.
- [ ] Reconfirm this workstream's current state from its existing context/plan before resuming implementation.
