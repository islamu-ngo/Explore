# Plan: Group Actor Feature Implementation

**Last Updated**: 2026-02-19

---

## Executive Summary

Implement a full Group actor capability so event publishing supports three actor paths: User (user-reported), Organization (legal entity), and Group (informal community). The feature must preserve existing Clean Architecture/CQRS conventions, add tenant-governed publishing policies, and deliver adaptive Create Event UX in Blazor for single-tenant and multi-tenant deployments.

This plan is implementation-ready and based on verified codebase state. It includes missing components that must be created, concrete acceptance criteria, dependency order, and risk controls.

---

## Current State Analysis (Verified)

### Verified Existing Domain and Core Files

| Path/Class | Status | Notes |
|---|---|---|
| `Explore.Domain/Actor.cs` | Exists | Includes `UserId`, `OrganizationId`, and `GroupId` optional links.
| `Explore.Domain/Organization.cs` | Exists | Includes approval audit fields (`ApprovedAt`, `ApprovedBy`, `ApprovalNotes`).
| `Explore.Domain/TenantSettings.cs` | Exists | Includes event publishing and org/group registration policy fields.
| `Explore.Domain/Group.cs` | Exists | Lightweight group entity with tenant/approval/actor linkage.
| `Explore.Domain/GroupMember.cs` | Exists | Group-user-role membership link.
| `Explore.Domain/Enums/EventPublishingPolicyEnum.cs` | Exists | `OrganizationAndGroupOnly`, `OrganizationGroupAndUserReported`.
| `Explore.Domain/Enums/ActorTypeEnum.cs` | Exists | Includes `Group = 4`.
| `Explore.Domain/Enums/RoleEnum.cs` | Exists | Includes group role scope constants (30+).
| `Explore.Domain/Enums/RoleScopeEnum.cs` | Exists | Includes `Group = 3`.

### Verified Existing Application/Event Flow

| Path/Class | Status | Notes |
|---|---|---|
| `Explore.Application/DTOs/Event/CreateEventDto.cs` | Exists | Currently supports `OrganizationId`, not `GroupId`.
| `Explore.Application/DTOs/Event/Validators/CreateEventDtoValidator.cs` | Exists | Validates `OrganizationId`; no group validation yet.
| `Explore.Application/Features/Events/Handlers/Commands/CreateEventCommandHandler.cs` | Exists | Org or personal path; no group path/policy enforcement.
| `Explore.Application/Features/Events/Handlers/Commands/CreateEventWithSessionsCommandHandler.cs` | Exists | Same limitation as above.
| `Explore.Application/Features/Users/Requests/Queries/GetUserOrganizationsRequest.cs` | Exists | Reusable member-loading pattern.
| `Explore.Application/DTOs/TenantSettings/TenantSettingsDto.cs` | Exists | Minimal fields only; does not expose new policy fields.

### Verified Existing Persistence/API/UI Baseline

| Path/Class | Status | Notes |
|---|---|---|
| `Explore.Persistence/ExploreDbContext.cs` | Exists | No `DbSet<Group>`/`DbSet<GroupMember>` declared.
| `Explore.Persistence/Configurations/Entities/OrganizationConfiguration.cs` | Exists | Pattern reference for constraints/indexes/defaults.
| `Explore.Persistence/Configurations/Entities/TenantSettingsConfiguration.cs` | Exists | Minimal configuration only.
| `Explore.Persistence/Repositories/OrganizationRepository.cs` | Exists | Pattern for detail/list/paged entity repositories.
| `Explore.Persistence/Repositories/OrganizationMemberRepository.cs` | Exists | Permission check patterns (`HasPermissionInOrganization`).
| `Explore.API/Controllers/OrganizationController.cs` | Exists | HATEOAS + CQRS controller template.
| `Explore.API/Controllers/OrganizationMemberController.cs` | Exists | Membership endpoint pattern.
| `Explore.API/Controllers/EventController.cs` | Exists | Create docs currently mention org/personal only.
| `Explore.API/Controllers/TenantSettingsController.cs` | Exists | CRUD exists; auth conventions need tightening.
| `Explore.API/Hateoas/RouteNames.cs` | Exists | No group route constants.
| `Explore.Blazor.Client/Pages/Event/CreateEvent.razor` | Exists | Publish-as supports personal/organization only.
| `Explore.Blazor.Client/Pages/Event/CreateEvent.razor.cs` | Exists | Loads organizations only; no policy-driven group path.

### Verified Missing Components (Must Be Created)

| Missing Item | Evidence |
|---|---|
| `IGroupRepository` | No `interface IGroupRepository` match in `Explore.Application/Contracts/Persistence`.
| `IGroupMemberRepository` | No `interface IGroupMemberRepository` match in `Explore.Application/Contracts/Persistence`.
| `GroupRepository` | No `class GroupRepository` match in `Explore.Persistence/Repositories`.
| `GroupMemberRepository` | No `class GroupMemberRepository` match in `Explore.Persistence/Repositories`.
| `GroupController` | No `class GroupController` match in `Explore.API/Controllers`.
| `GroupMemberController` | No `class GroupMemberController` match in `Explore.API/Controllers`.
| Group DTO suite (`GroupDto`, `GroupListDto`, `CreateGroupDto`, etc.) | No class matches in `Explore.Application/DTOs`.
| Group CQRS requests/handlers | No matches for `CreateGroupCommand`, `GetGroupListRequest`, etc.
| Group EF configurations | `GroupConfiguration.cs` and `GroupMemberConfiguration.cs` absent.

---

## Proposed Future State

1. Group is a first-class actor-backed publisher in domain, persistence, application, API, and Blazor.
2. Tenant settings enforce publishing policy and creation/approval rules for organizations and groups.
3. Event creation handlers resolve actor by precedence and policy (organization, group, personal).
4. API exposes Group/GroupMember resources with consistent versioning, auth attributes, and HATEOAS route names.
5. Blazor Create Event screen adapts options dynamically:
   - show/hide personal option by policy,
   - show organization/group selectors only when memberships exist,
   - show CTA guidance when membership is missing and public creation is allowed/blocked.

---

## Implementation Phases

### Phase 1: Domain Hardening and Consistency

**Objective**: Validate and finalize domain model so downstream layers can build on stable contracts.

#### Task 1.1 - Domain Consistency Audit and Corrections
- **Scope**: `Explore.Domain/Actor.cs`, `Explore.Domain/Group.cs`, `Explore.Domain/GroupMember.cs`, `Explore.Domain/TenantSettings.cs`, enums.
- **Acceptance Criteria**:
  - [ ] All new/updated entities follow file-scoped namespace and include required interfaces (`ITenantEntity`, `IAuditableEntity`, `ISoftDeletable`) where applicable.
  - [ ] No domain property default value initializers are introduced where forbidden by project rules.
  - [ ] `Actor` polymorphic rules are documented in context file (exactly one source identity set among user/org/group).
- **Dependencies**: None.
- **Effort**: S.
- **Related Skills**: `clean-architecture-rules`.

#### Task 1.2 - Role/Permission Scope Alignment for Group
- **Scope**: `RoleEnum`, `RoleScopeEnum`, permission constants and seeding references.
- **Acceptance Criteria**:
  - [ ] Group roles map cleanly to authorization checks (creator/admin/moderator/member).
  - [ ] Any required permission master codes for group event publishing are identified and added to backlog or seeded now.
- **Dependencies**: Task 1.1.
- **Effort**: S.
- **Related Skills**: `auth-patterns`, `clean-architecture-rules`.

---

### Phase 2: Application Contracts, DTOs, Validation, Mapping

**Objective**: Introduce Application-layer contracts and transfer objects before CQRS handlers.

#### Task 2.1 - Add Group and GroupMember DTO Families
- **Scope**: Create DTO folders and models in `Explore.Application/DTOs/Group` and `Explore.Application/DTOs/GroupMember`.
- **Acceptance Criteria**:
  - [ ] Add `GroupDto`, `GroupListDto`, `CreateGroupDto`, `UpdateGroupDto`.
  - [ ] Add `GroupMemberDto`, `GroupMemberListDto`, `CreateGroupMemberDto`, `UpdateGroupMemberDto`.
  - [ ] DTOs include fields aligned with domain (no legal-entity fields leaked into Group).
- **Dependencies**: Phase 1.
- **Effort**: M.
- **Related Skills**: `cqrs-mediatr-guidelines`.

#### Task 2.2 - Add Validators (Manual Instantiation Pattern)
- **Scope**: Validator classes under DTO `Validators` folders.
- **Acceptance Criteria**:
  - [ ] Validators check FK existence with repositories (tenant-safe).
  - [ ] Handlers will instantiate validators manually (not DI).
  - [ ] `CreateEventDtoValidator` supports `GroupId` and mutual exclusivity/combination constraints.
- **Dependencies**: Task 2.1.
- **Effort**: M.
- **Related Skills**: `cqrs-mediatr-guidelines`, `clean-architecture-rules`.

#### Task 2.3 - Add Repository Interfaces
- **Scope**: `Explore.Application/Contracts/Persistence`.
- **Acceptance Criteria**:
  - [ ] Add `IGroupRepository : IGenericRepository<Group, Guid>` with details/list/paged/member-scoped methods.
  - [ ] Add `IGroupMemberRepository : IGenericRepository<GroupMember, Guid>` with permission/membership methods mirroring organization patterns.
  - [ ] Repositories return entities only.
- **Dependencies**: Task 2.1.
- **Effort**: S.
- **Related Skills**: `clean-architecture-rules`, `dotnet-efcore-guidelines`.

#### Task 2.4 - Extend MappingProfile and TenantSettings DTOs
- **Scope**: `Explore.Application/Profiles/MappingProfile.cs`, `Explore.Application/DTOs/TenantSettings/*`.
- **Acceptance Criteria**:
  - [ ] Group and GroupMember mapping entries added for DTO and list DTO.
  - [ ] Tenant settings DTOs expose event publishing and org/group creation policy fields.
  - [ ] Mapping for create/update event remains actor-resolution-safe (ActorId ignored in mapping).
- **Dependencies**: Tasks 2.1-2.3.
- **Effort**: M.
- **Related Skills**: `cqrs-mediatr-guidelines`.

---

### Phase 3: CQRS for Group and GroupMember

**Objective**: Provide full command/query coverage equivalent to organization flows.

#### Task 3.1 - Group Commands/Queries/Handlers
- **Scope**: `Explore.Application/Features/Groups/`.
- **Acceptance Criteria**:
  - [ ] Create/GetById/List/Update/Delete requests and handlers implemented.
  - [ ] Commands return `BaseCommandResponse<Guid>`.
  - [ ] Business rules support approval and actor creation linkage.
- **Dependencies**: Phase 2.
- **Effort**: L.
- **Related Skills**: `cqrs-mediatr-guidelines`, `auth-patterns`.

#### Task 3.2 - GroupMember Commands/Queries/Handlers
- **Scope**: `Explore.Application/Features/GroupMembers/`.
- **Acceptance Criteria**:
  - [ ] Add/remove/update-role/list/get flows implemented.
  - [ ] Permission checks mirror organization member flow with group scope.
  - [ ] Handler validation errors return structured command responses.
- **Dependencies**: Task 3.1.
- **Effort**: L.
- **Related Skills**: `cqrs-mediatr-guidelines`, `auth-patterns`.

---

### Phase 4: Event Creation and Tenant Policy Enforcement

**Objective**: Enable group publishing in event creation with strict tenant policy checks.

#### Task 4.1 - Extend Event DTOs and Validators
- **Scope**: `CreateEventDto`, `CreateEventWithSessionsDto` (+ validators).
- **Acceptance Criteria**:
  - [ ] Add `GroupId` support.
  - [ ] Enforce valid combinations (`OrganizationId` and `GroupId` cannot both be set).
  - [ ] Validate `GroupId` existence and tenant-scoped access.
- **Dependencies**: Phase 2.
- **Effort**: M.
- **Related Skills**: `cqrs-mediatr-guidelines`.

#### Task 4.2 - Update Event Handlers for Actor Resolution and Policy
- **Scope**: `CreateEventCommandHandler`, `CreateEventWithSessionsCommandHandler`.
- **Acceptance Criteria**:
  - [ ] Resolve actor for organization/group/personal publishing paths.
  - [ ] Enforce tenant `EventPublishingPolicy` before allowing personal user-reported path.
  - [ ] `IsUserReported` behavior remains accurate.
  - [ ] Permission checks use membership repositories (`HasPermissionInOrganization` and group equivalent).
- **Dependencies**: Task 4.1, Phase 3.
- **Effort**: L.
- **Related Skills**: `auth-patterns`, `cqrs-mediatr-guidelines`.

#### Task 4.3 - Tenant Settings CQRS Updates
- **Scope**: Tenant settings requests/handlers and DTO contracts.
- **Acceptance Criteria**:
  - [ ] Read/write flows include all new policy fields.
  - [ ] Existing onboarding policy DTO interoperability preserved.
  - [ ] Single-tenant defaults remain coherent.
- **Dependencies**: Phase 2.
- **Effort**: M.
- **Related Skills**: `clean-architecture-rules`, `cqrs-mediatr-guidelines`.

---

### Phase 5: Persistence, EF Configuration, and Migration

**Objective**: Materialize schema and repositories in persistence layer with tenant-safe behavior.

#### Task 5.1 - Add Group/GroupMember EF Configurations
- **Scope**: `Explore.Persistence/Configurations/Entities/GroupConfiguration.cs`, `GroupMemberConfiguration.cs`.
- **Acceptance Criteria**:
  - [ ] FK relationships configured with explicit `OnDelete` semantics.
  - [ ] Length constraints/indexes set for common queries.
  - [ ] Approval and tenant fields configured consistently.
- **Dependencies**: Phase 1.
- **Effort**: M.
- **Related Skills**: `dotnet-efcore-guidelines`.

#### Task 5.2 - Add Repositories and DbContext Wiring
- **Scope**: repository implementations + registration + DbSets + query filters.
- **Acceptance Criteria**:
  - [ ] Add `GroupRepository` and `GroupMemberRepository` implementations.
  - [ ] Add `DbSet<Group>` and `DbSet<GroupMember>` in `ExploreDbContext`.
  - [ ] Add named query filters for tenant and soft delete where applicable.
  - [ ] Register interfaces in persistence DI extension.
- **Dependencies**: Tasks 2.3, 5.1.
- **Effort**: L.
- **Related Skills**: `dotnet-efcore-guidelines`, `clean-architecture-rules`.

#### Task 5.3 - Migration and Seed Alignment
- **Scope**: new EF Core migration and seed adjustments.
- **Acceptance Criteria**:
  - [ ] Migration creates/updates required tables and columns only.
  - [ ] Role/actor type/approval seed values remain compatible.
  - [ ] Migration script can be generated and applied cleanly.
- **Dependencies**: Tasks 5.1-5.2.
- **Effort**: M.
- **Related Skills**: `dotnet-efcore-guidelines`.

---

### Phase 6: API and HATEOAS Surface

**Objective**: Expose Group and GroupMember operations via API with existing conventions.

#### Task 6.1 - Add Group and GroupMember Controllers
- **Scope**: `Explore.API/Controllers/GroupController.cs`, `GroupMemberController.cs`.
- **Acceptance Criteria**:
  - [ ] GET endpoints use `AllowAnonymous` where public read is intended.
  - [ ] Write endpoints use `Authorize`.
  - [ ] Route constraints and endpoint metadata are complete.
  - [ ] Response types follow existing command/query conventions.
- **Dependencies**: Phases 3 and 5.
- **Effort**: L.
- **Related Skills**: `auth-patterns`, `cqrs-mediatr-guidelines`.

#### Task 6.2 - HATEOAS Route and Policy Updates
- **Scope**: `Explore.API/Hateoas/RouteNames.cs` + link policies/assemblers.
- **Acceptance Criteria**:
  - [ ] Group route constants added.
  - [ ] Link policies enforce Cerbos/resource authorization consistently.
  - [ ] HAL responses include discoverable group links.
- **Dependencies**: Task 6.1.
- **Effort**: M.
- **Related Skills**: `auth-patterns`.

#### Task 6.3 - Update Event/Tenant Settings Controller Contracts
- **Scope**: `EventController` descriptions and `TenantSettingsController` auth/read alignment.
- **Acceptance Criteria**:
  - [ ] Event create endpoint docs mention group publishing and policy constraints.
  - [ ] Tenant settings endpoint authorization aligns with governance convention (GET public only where intended).
- **Dependencies**: Task 4.3.
- **Effort**: S.
- **Related Skills**: `auth-patterns`.

---

### Phase 7: Blazor Adaptive Publish-As UX

**Objective**: Make Create Event page policy-aware and actor-path-aware.

#### Task 7.1 - Service and State Expansion
- **Scope**: `CreateEvent.razor.cs` and related client services.
- **Acceptance Criteria**:
  - [ ] Load user groups and tenant policy settings in parallel with organizations.
  - [ ] Compute allowed publish modes from policy and memberships.
  - [ ] Handle single available organization/group auto-selection path.
- **Dependencies**: Phases 3-6.
- **Effort**: M.
- **Related Skills**: `blazor-ui-conventions`, `blazor-bff-patterns`.

#### Task 7.2 - Adaptive UI Rendering
- **Scope**: `CreateEvent.razor`.
- **Acceptance Criteria**:
  - [ ] Publish-As section conditionally displays personal/org/group options.
  - [ ] Membership-empty states display policy-aware CTA text.
  - [ ] Form submits either `OrganizationId`, `GroupId`, or neither (personal) correctly.
  - [ ] Mobile and desktop layouts remain usable.
- **Dependencies**: Task 7.1.
- **Effort**: M.
- **Related Skills**: `blazor-ui-conventions`, `blazor-css-isolation`.

---

### Phase 8: Verification, Testing, Documentation

**Objective**: Prove stability and readiness before merge.

#### Task 8.1 - Diagnostics, Build, and Test Execution
- **Scope**: modified files + related test suites.
- **Acceptance Criteria**:
  - [ ] `lsp_diagnostics` clean for all changed files.
  - [ ] `dotnet build --configuration Release --verbosity quiet` passes.
  - [ ] Related tests pass (Application, Domain, Persistence integration, API integration, Blazor client tests where impacted).
  - [ ] Any pre-existing failures are documented explicitly.
- **Dependencies**: All prior phases.
- **Effort**: M.
- **Related Skills**: `error-tracking`, `cqrs-mediatr-guidelines`.

#### Task 8.2 - Policy and Ops Documentation Update
- **Scope**: docs for API behavior and governance notes.
- **Acceptance Criteria**:
  - [ ] Update docs covering group actor publishing, tenant policy flags, and onboarding behavior.
  - [ ] Add migration note and rollout checklist for existing tenants.
- **Dependencies**: Task 8.1.
- **Effort**: S.
- **Related Skills**: `clean-architecture-rules`.

---

## Risk Assessment and Mitigation

| Risk | Likelihood | Impact | Mitigation |
|---|---|---|---|
| Actor polymorphism ambiguity (user/org/group simultaneously set) | Medium | High | Enforce mutual exclusivity in handlers and add architecture/unit tests.
| Tenant policy drift between onboarding DTOs and `TenantSettings` persistence | High | High | Add mapping tests and endpoint integration tests for policy read/write round-trip.
| Permission leakage between org and group scopes | Medium | High | Separate repository permission methods and explicit role-scope assertions.
| Query filter side effects on required navigations | Medium | Medium | Keep tenant and soft-delete named filters explicit and test include-heavy queries.
| Migration conflicts in environments with existing data | Medium | High | Generate idempotent migration script and run pre-deploy dry-run in staging.
| Blazor UX complexity for policy permutations | High | Medium | Table-driven UI-state tests and manual validation matrix for modes/memberships.

---

## Success Metrics

1. Functional completeness: all seven architecture phases implemented with no placeholder endpoints.
2. Policy correctness: event creation behavior matches tenant policy matrix in integration tests.
3. Security correctness: write operations remain authorized and permission-scoped.
4. Stability: build succeeds and impacted test projects pass.
5. UX correctness: Create Event supports org/group/personal paths and valid CTA fallbacks in both single-tenant and multi-tenant mode.

---

## Required Resources and Dependencies

- Code modules: `Explore.Domain`, `Explore.Application`, `Explore.Persistence`, `Explore.API`, `Explore.Blazor.Client`.
- Database: PostgreSQL with EF Core migration execution path available.
- Authorization: existing Keycloak + Cerbos setup unchanged, with new group resource policies added where needed.
- Documentation references used for this plan:
  - `CLAUDE.md`
  - `docs/ARCHITECTURE.md`, `docs/DOMAIN.md`, `docs/SECURITY.md`, `docs/API.md`, `docs/CONFIGURATION.md`, `docs/OPERATIONS.md`, `docs/GOVERNANCE.md`, `docs/TROUBLESHOOTING.md`, `docs/FEDERATION.md`, `docs/PROJECT.md`
  - `dev/active/README.md`
  - `.claude/skills/clean-architecture-rules/SKILL.md`
  - `.claude/skills/cqrs-mediatr-guidelines/SKILL.md`
  - `.claude/skills/dotnet-efcore-guidelines/SKILL.md`
  - `.claude/skills/auth-patterns/SKILL.md`
  - `.claude/skills/blazor-ui-conventions/SKILL.md`

External references for implementation hygiene:
- EF Core one-to-many relationships: https://learn.microsoft.com/en-us/ef/core/modeling/relationships/one-to-many
- EF Core global query filters: https://learn.microsoft.com/en-us/ef/core/querying/filters
- EF Core migrations overview: https://learn.microsoft.com/en-us/ef/core/managing-schemas/migrations/
- EF Core indexes and uniqueness: https://learn.microsoft.com/en-us/ef/core/modeling/indexes

---

## Effort Estimates

| Phase | Effort |
|---|---|
| Phase 1 - Domain hardening | S |
| Phase 2 - Contracts/DTO/Mapping | M |
| Phase 3 - CQRS | L |
| Phase 4 - Event + policy enforcement | L |
| Phase 5 - Persistence + migration | L |
| Phase 6 - API + HATEOAS | L |
| Phase 7 - Blazor adaptive UX | M |
| Phase 8 - Verification + docs | M |

**Overall**: XL (cross-layer feature touching all architecture layers + migration + UI policy matrix).

---

## Implementation Order (Dependency-Safe)

1. Phase 1 -> 2 -> 3 (establish contracts and use cases)
2. Phase 4 (event flow integration) in parallel with Phase 5 (persistence) after interfaces are stable
3. Phase 6 (API exposure)
4. Phase 7 (Blazor UX)
5. Phase 8 (full verification and docs)
