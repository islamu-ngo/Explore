# Tasks: Group Actor Feature

**Last Updated**: 2026-02-19

---

## Phase 1: Domain Layer - Invariants and Role Scope

- [x] **1.1** Audit and finalize Group-related domain entities (`Actor`, `Group`, `GroupMember`, `TenantSettings`, enums) | **S** | Depends on: none
  - Acceptance:
    - [x] Entity interfaces and file-scoped namespaces are correct
    - [x] No forbidden domain default initializers introduced
    - [x] Actor polymorphism invariants are documented in context

- [x] **1.2** Align group role scope and permission expectations (`RoleEnum`, `RoleScopeEnum`, permission constants/seeding) | **S** | Depends on: 1.1
  - Acceptance:
    - [x] Group role IDs are stable and coherent
    - [x] Permission mapping for group event publishing is defined

---

## Phase 2: Application Contracts, DTOs, Mapping, Validators

- [x] **2.1** Create Group DTO set (`GroupDto`, `GroupListDto`, `CreateGroupDto`, `UpdateGroupDto`) | **M** | Depends on: 1.1
- [x] **2.2** Create GroupMember DTO set (`GroupMemberDto`, `GroupMemberListDto`, `CreateGroupMemberDto`, `UpdateGroupMemberDto`) | **M** | Depends on: 1.1
- [x] **2.3** Add `IGroupRepository` and `IGroupMemberRepository` contracts | **S** | Depends on: 2.1, 2.2
- [x] **2.4** Add validators for Group/GroupMember and update event validator for `GroupId` and publish-as rules | **M** | Depends on: 2.3
- [x] **2.5** Extend `MappingProfile` and tenant settings DTOs with policy fields | **M** | Depends on: 2.1, 2.2
  - Acceptance for Phase 2:
    - [x] Contracts return entities only
    - [x] Validators are designed for manual instantiation
    - [x] Mapping profile compiles with new DTO/domain pairs

---

## Phase 3: CQRS - Group and GroupMember Features

- [x] **3.1** Implement Group requests/handlers (Create, GetById, List/Paged, Update, Delete) | **L** | Depends on: Phase 2
- [x] **3.2** Implement GroupMember requests/handlers (Add, UpdateRole, Remove, List, GetById) | **L** | Depends on: 3.1
  - Acceptance:
    - [x] Command handlers return `BaseCommandResponse<Guid>`
    - [x] Query handlers return DTO/list types, not entities
    - [x] Permission checks exist for write operations

---

## Phase 4: Event Creation and Tenant Policy Enforcement

- [x] **4.1** Add `GroupId` support to event creation DTO(s) and validators | **M** | Depends on: Phase 2
- [x] **4.2** Update `CreateEventCommandHandler` to resolve actor via org/group/personal with tenant policy checks | **L** | Depends on: 4.1, 3.2
- [x] **4.3** Update `CreateEventWithSessionsCommandHandler` with same actor/policy logic | **L** | Depends on: 4.2
- [x] **4.4** Update tenant settings CQRS to round-trip new policy fields | **M** | Depends on: 2.5
  - Acceptance:
    - [x] `OrganizationId` and `GroupId` mutual exclusivity enforced
    - [x] User-reported path blocked when policy forbids it
    - [x] `IsUserReported` set correctly

---

## Phase 5: Persistence and EF Migration

- [x] **5.1** Create `GroupConfiguration` and `GroupMemberConfiguration` | **M** | Depends on: Phase 1
- [x] **5.2** Implement `GroupRepository` and `GroupMemberRepository` | **L** | Depends on: 2.3, 5.1
- [x] **5.3** Update `ExploreDbContext` (`DbSet`s + query filters) and persistence DI registration | **M** | Depends on: 5.2
- [x] **5.4** Update `TenantSettingsConfiguration` for policy fields/relationships | **S** | Depends on: 5.3
- [x] **5.5** Create and validate EF migration | **M** | Depends on: 5.4
  - Acceptance:
    - [x] Migration applies cleanly
    - [x] Group tables and FKs exist as expected
    - [x] Tenant/soft-delete filters include new entities

---

## Phase 6: API + HATEOAS

- [x] **6.1** Create `GroupController` with CQRS endpoints and endpoint metadata | **L** | Depends on: 3.1, 5.3
- [x] **6.2** Create `GroupMemberController` with membership endpoints | **M** | Depends on: 3.2, 5.3
- [x] **6.3** Add Group route constants to `RouteNames` and update HATEOAS policies/assemblers | **M** | Depends on: 6.1, 6.2
- [x] **6.4** Update event and tenant settings endpoint docs/auth conventions | **S** | Depends on: 4.4, 6.1
  - Acceptance:
    - [x] GET endpoints use `AllowAnonymous` where intended
    - [x] Write endpoints use `Authorize`
    - [x] HAL link generation includes group resources

---

## Phase 7: Blazor Create Event Adaptive UX

- [x] **7.1** Extend page state/services to load tenant policy + user groups + user organizations | **M** | Depends on: 4.4, 6.1
- [x] **7.2** Add policy-aware Publish-As UI (personal/org/group options and CTA variants) | **M** | Depends on: 7.1
- [x] **7.3** Ensure submit maps to `OrganizationId` or `GroupId` or personal path correctly | **M** | Depends on: 7.2
  - Acceptance:
    - [x] UI covers single-tenant and multi-tenant mode scenarios
    - [x] Disabled/empty states are explicit and actionable
    - [x] Mobile and desktop usability preserved

---

## Phase 8: Verification and Documentation

- [x] **8.1** Run `lsp_diagnostics` on all modified files | **S** | Depends on: all phases
- [x] **8.2** Build release configuration | **S** | Depends on: 8.1
- [x] **8.3** Run impacted tests individually (Application/Domain/Persistence/API/Blazor) | **M** | Depends on: 8.2
- [x] **8.4** Update feature docs for policy behavior and migration rollout notes | **S** | Depends on: 8.3
  - Acceptance:
    - [x] No new diagnostics errors
    - [x] Build passes
    - [x] Tests pass or pre-existing failures are documented

---

## Phase Summary

| Phase | Status | Done | Total |
|---|---|---:|---:|
| Phase 1 | Complete | 2 | 2 |
| Phase 2 | Complete | 5 | 5 |
| Phase 3 | Complete | 2 | 2 |
| Phase 4 | Complete | 4 | 4 |
| Phase 5 | Complete | 5 | 5 |
| Phase 6 | Complete | 4 | 4 |
| Phase 7 | Complete | 3 | 3 |
| Phase 8 | Complete | 4 | 4 |
| **Total** |  | **29** | **29** |

---

## Follow-Up (Optional Hardening)

- Manual smoke test for create-event policy matrix (personal/org/group paths).
- Regenerate API client to replace manual `CreateEventDto.GroupId` patch in generated client file.

---

## Related Skills to Use During Execution

- `clean-architecture-rules`
- `cqrs-mediatr-guidelines`
- `dotnet-efcore-guidelines`
- `auth-patterns`
- `blazor-ui-conventions`
- `blazor-css-isolation`
- `blazor-bff-patterns`
- `error-tracking`
