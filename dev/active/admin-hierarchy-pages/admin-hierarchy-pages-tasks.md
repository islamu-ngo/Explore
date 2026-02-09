# Task Checklist: Admin Hierarchy Pages (Instance, Tenant, Organization)

Last Updated: 2026-02-08

## Status

Current state: Planning complete, implementation not started.

## Milestones

- [ ] M1: Contract and domain foundations complete
- [ ] M2: Scope-specific admin APIs + policies complete
- [ ] M3: Scope-specific Blazor pages complete
- [ ] M4: Tests + docs + ops readiness complete

## Phase 0: Contract and Governance Alignment

- [ ] T0.1 Freeze role ownership matrix
  - Effort: M
  - Depends on: none
  - Acceptance:
    - [ ] Every capability has owner scope and delegation rule.
    - [ ] Doc conflicts resolved or explicitly tracked.

- [ ] T0.2 Freeze governance setting key catalog
  - Effort: M
  - Depends on: T0.1
  - Acceptance:
    - [ ] Key names, value types, allowed values finalized.
    - [ ] Lock/delegation semantics finalized.

## Phase 1: Domain Layer

- [ ] T1.1 Add governance enums and key constants
  - Effort: M
  - Depends on: T0.2
  - Acceptance:
    - [ ] New enums cover deployment, onboarding, submission, verification policy states.
    - [ ] Key constants map to persisted settings.

- [ ] T1.2 Fix tenant settings model consistency (`Guid` ID path)
  - Effort: M
  - Depends on: T0.2
  - Acceptance:
    - [ ] `TenantSettingsDto.Id` changed to `Guid`.
    - [ ] Mapping profile and handlers updated consistently.

- [ ] T1.3 Add configuration change log entity
  - Effort: M
  - Depends on: T0.1
  - Acceptance:
    - [ ] Audit entity supports actor, scope, key, before/after.

## Phase 2: Application Layer (CQRS/MediatR)

- [ ] T2.1 Implement instance admin queries/commands
  - Effort: L
  - Depends on: T1.1, T1.3
  - Acceptance:
    - [ ] Query returns effective instance governance state.
    - [ ] Commands support lock-aware updates.

- [ ] T2.2 Implement tenant admin queries/commands
  - Effort: L
  - Depends on: T1.1, T1.2
  - Acceptance:
    - [ ] Query returns source metadata (default/override/locked).
    - [ ] Lock denial behavior enforced.

- [ ] T2.3 Implement organization admin queries/commands
  - Effort: L
  - Depends on: T2.2
  - Acceptance:
    - [ ] Membership/role operations enforce organization scope.
    - [ ] Verification submission/status available.

- [ ] T2.4 Implement unified admin context query
  - Effort: M
  - Depends on: T2.1, T2.2, T2.3
  - Acceptance:
    - [ ] UI can resolve allowed admin scopes from one response.

- [ ] T2.5 Implement shared application authorization guard abstractions
  - Effort: M
  - Depends on: T2.1, T2.2, T2.3
  - Acceptance:
    - [ ] Role/claim/scope checks centralized for handlers.

## Phase 3: Infrastructure and Persistence

- [ ] T3.1 Add schema/config/seed updates + migration
  - Effort: L
  - Depends on: T1.1, T1.3
  - Acceptance:
    - [ ] Governance keys seeded with constraints.
    - [ ] Migration verified.

- [ ] T3.2 Harden settings resolver and repositories
  - Effort: M
  - Depends on: T3.1
  - Acceptance:
    - [ ] Effective source + lock metadata available for UI/API.
    - [ ] Cache invalidation verified.

- [ ] T3.3 Resolve runtime deployment mode from settings
  - Effort: L
  - Depends on: T2.1, T3.2
  - Acceptance:
    - [ ] Runtime mode switch effective without app restart.
    - [ ] Fallback behavior logged and deterministic.

- [ ] T3.4 Persist governance change logs from write paths
  - Effort: M
  - Depends on: T1.3, T2.1, T2.2
  - Acceptance:
    - [ ] Every governance command records audit event.

## Phase 4: API and BFF

- [ ] T4.1 Add scope-specific admin controllers
  - Effort: L
  - Depends on: T2.1, T2.2, T2.3
  - Acceptance:
    - [ ] Separate endpoint groups for instance/tenant/organization admin.
    - [ ] OpenAPI metadata complete.

- [ ] T4.2 Register scope policies (`InstanceAdminPolicy`, `TenantAdminPolicy`, `OrganizationAdminPolicy`)
  - Effort: M
  - Depends on: T2.5
  - Acceptance:
    - [ ] Endpoints protected by policy instead of coarse role checks where applicable.

- [ ] T4.3 Harden tenant propagation in BFF/API boundary
  - Effort: L
  - Depends on: T3.3
  - Acceptance:
    - [ ] Multi-tenant requests do not rely on static default tenant header.
    - [ ] Tenant mismatch rejection behavior tested.

- [ ] T4.4 Add Cerbos policy artifacts (docs-only future readiness)
  - Effort: S
  - Depends on: T4.1, T4.2
  - Acceptance:
    - [ ] Policy drafts exist and docs clarify Cerbos is not runtime-integrated yet.

## Phase 5: Blazor UI (Hybrid)

- [ ] T5.1 Refactor admin routing + navigation architecture
  - Effort: M
  - Depends on: T4.1
  - Acceptance:
    - [ ] Route groups exist for instance/tenant/organization admin.
    - [ ] Menu is scope-aware.

- [ ] T5.2 Build instance admin pages
  - Effort: L
  - Depends on: T4.1, T4.3
  - Acceptance:
    - [ ] Runtime deployment mode page implemented.
    - [ ] Global module/policy lock pages implemented.

- [ ] T5.3 Build tenant admin pages
  - Effort: L
  - Depends on: T4.1, T4.3, T5.2
  - Acceptance:
    - [ ] User-submitted event policy page implemented.
    - [ ] Organization verification policy page implemented.

- [ ] T5.4 Build organization admin pages
  - Effort: L
  - Depends on: T2.3, T4.1, T5.3
  - Acceptance:
    - [ ] Membership/role admin page implemented.
    - [ ] Verification status/submission page implemented.

- [ ] T5.5 Build shared governance components
  - Effort: M
  - Depends on: T5.2, T5.3, T5.4
  - Acceptance:
    - [ ] Lock badge/source chip components reused across pages.
    - [ ] Consistent UX patterns and styles.

- [ ] T5.6 Implement single-tenant merged UX behavior
  - Effort: M
  - Depends on: T3.3, T5.1
  - Acceptance:
    - [ ] Single-tenant mode can present merged admin UX.
    - [ ] Scope rules still enforced in backend.

## Phase 6: Testing, Operations, and Documentation

- [ ] T6.1 Add unit tests for application/domain governance logic
  - Effort: L
  - Depends on: Phases 1-3
  - Acceptance:
    - [ ] Positive and negative cases for lock/authorization covered.

- [ ] T6.2 Add API integration tests for scope matrix and tenant isolation
  - Effort: L
  - Depends on: Phase 4
  - Acceptance:
    - [ ] 200/401/403 matrix complete for admin scope endpoints.
    - [ ] Tenant mismatch cases covered.

- [ ] T6.3 Add Blazor tests for role-aware nav and page gating
  - Effort: M
  - Depends on: Phase 5
  - Acceptance:
    - [ ] Correct menu/page visibility per scope.
    - [ ] Lock/read-only control behavior covered.

- [ ] T6.4 Update docs and runbooks
  - Effort: M
  - Depends on: Phases 3-5
  - Acceptance:
    - [ ] Admin hierarchy docs reflect implemented behavior.
    - [ ] Runtime mode switch + rollback documented.

## Release Readiness Checklist

- [ ] API endpoints and contracts finalized.
- [ ] Blazor UI routes and scope gating finalized.
- [ ] Migrations applied and validated.
- [ ] Integration/unit/UI tests passing in CI.
- [ ] Operational docs updated.
- [ ] Security review completed.

## Quick Resume

- Next task to start: `T0.1`
- Next tasks in sequence: `T0.1 -> T0.2 -> T1.1`
- Reference plan: `dev/active/admin-hierarchy-pages/admin-hierarchy-pages-plan.md`
- Reference context: `dev/active/admin-hierarchy-pages/admin-hierarchy-pages-context.md`
