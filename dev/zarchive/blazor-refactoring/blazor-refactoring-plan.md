# Plan: Blazor Refactoring (Verified and Updated)

**Last Updated: 2026-02-15**

---

## Executive Summary

The previous Blazor refactoring plan became partially stale because the codebase evolved (especially authorization and role modeling). This refreshed plan keeps completed work intact, removes outdated assumptions, and focuses on remaining high-value work.

### Verified Baseline
- Phases 1-6 from the old effort are largely complete.
- Blazor runtime architecture is Hybrid with `InteractiveAuto`.
- BFF auth flow is client shim routes (`/login`, `/logout`) to server endpoints (`/auth/challenge`, `/auth/signout`).
- Authorization domain is unified around `Role` + `RoleEnum` + `RoleScopeEnum`.
- Remaining meaningful work is risk hardening + test expansion + optional API-driven performance enhancements.

---

## Current State (Verified on 2026-02-15)

| Area | Current State | Risk Level |
|------|---------------|------------|
| Role Model | Unified `Role` entity with scope; old `OrganizationRole` entity removed | Low |
| Claims Enrichment | `AdminClaimsTransformation` adds DB-backed admin claims (`explore:admin:*`) | Low |
| Blazor Routing | Blazouter `RouteConfig` + `IRouteGuard` patterns active | Low |
| BFF Auth Flow | `/login` and `/logout` are Blazor shim routes to `/auth/challenge` and `/auth/signout` | Low |
| Token Forwarding | Cross-user fallback removed; current-user-only resolution enforced | Low-Medium |
| Performance | Client caching improvements landed; server-side filtering depends on API contract | Medium |
| Test Coverage | Existing suite green, but coverage expansion still needed | Medium-High |

---

## Refactoring Objectives (Remaining)

1. Keep token-forwarding behavior deterministic and current-user scoped.
2. Bring test coverage for services/pages/components/layouts to target confidence level.
3. Keep plan execution evidence-driven: verify code state before every task.
4. Separate API-contract-dependent optimization from Blazor-only scope.

---

## Phase A: Planning Hygiene and Truth Sync (Immediate)

**Priority**: High
**Goal**: Prevent stale assumptions from driving incorrect implementation.

### Tasks
- Ensure all role/auth references in dev docs use unified role model terms.
- Keep explicit note where legacy naming (for example `OrganizationRoleId`) still exists in UI/API contracts.
- Require "verify before execute" checkpoints for each subsequent task.

### Acceptance Criteria
- `plan/context/tasks` docs are internally consistent.
- No remaining "decision pending" text for already-resolved render mode/auth flow.
- No primary guidance suggesting `OrganizationRole` as active domain model.

---

## Phase B: Token Service Risk Hardening (Completed Core Slice)

**Priority**: High
**Goal**: Resolve static fallback behavior in `CircuitAccessTokenService` and verify isolation.

### Tasks
1. Audit static store usage and fallback paths. (Done)
2. Remove cross-user/static fallback entirely (`GetAnyValidToken` removed). (Done)
3. Add tests proving token isolation across user contexts. (Done)

### Acceptance Criteria
- No ambiguous token-source behavior.
- Isolation behavior is covered by automated tests.
- Decision and rationale captured in context doc.

---

## Phase C: Test Coverage Expansion (Primary Remaining Work)

**Priority**: High
**Goal**: Increase confidence in refactored Blazor stack and prevent regressions.

### C1. Test Anti-Pattern Cleanup
- Replace `Task.Delay`-based waits with proper bUnit waiting primitives.
- Replace mock-verification-only assertions with behavior/output assertions.

### C2. Service Tests
- Expand tests for high-traffic service wrappers and error paths (401/404/500).

### C3. Page and Component Tests
- Prioritize high-risk UX and auth-sensitive pages/components.
- Cover loading/error/empty/auth-protected states.

### C4. Layout/Auth Flow Tests
- Validate menu visibility by auth/admin claims.
- Validate redirect shim behavior and route guards.

### Acceptance Criteria
- Existing tests remain green.
- New tests cover failure modes, not just happy paths.
- Coverage increase is measurable and documented.

---

## Phase D: Performance Follow-up (API-Coupled)

**Priority**: Medium
**Goal**: Address optimization items that require backend contract support.

### Tasks
- Document which filtering/pagination optimizations are blocked by current API shape.
- If approved, create a separate API+Blazor epic for server-side filtering parameters.

### Acceptance Criteria
- No pseudo-fixes in Blazor that pretend to be server-side filtering.
- Performance roadmap clearly split into Blazor-only vs API-dependent work.

---

## Verification Protocol (Mandatory)

Before implementing any task:
1. Confirm target files still match assumptions.
2. Confirm no newer refactor already solved it.
3. If mismatch is found, update plan/tasks/context first.

After implementing any task:
1. Run relevant build/tests.
2. Update context + tasks immediately.
3. Record pre-existing vs newly introduced issues distinctly.

---

## Risks and Mitigations

| Risk | Impact | Mitigation |
|------|--------|------------|
| Following stale instructions | High | Mandatory verify-before-execute checklist |
| Token fallback ambiguity | High | Phase B decision + isolation tests |
| Test churn from changing architecture assumptions | Medium | Re-baseline docs first, then implement tests |
| Performance work blocked by API contract | Medium | Split into separate API epic |

---

## Success Metrics

- Plan/tasks/context stay synchronized with live codebase.
- Token-forwarding risk is either removed or explicitly justified with tests.
- Test suite coverage and quality materially improve without regressions.
- No new work is launched from unverified assumptions.
