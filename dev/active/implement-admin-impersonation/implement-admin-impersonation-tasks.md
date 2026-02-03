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
