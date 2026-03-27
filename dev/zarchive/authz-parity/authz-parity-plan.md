ABOUTME: Implementation plan for achieving full authorization parity between Cerbos and local fallback.
ABOUTME: Covers missing resource kinds, batch optimization, Cerbos policies, registry fixes, and tests.

# Authorization Parity — Implementation Plan

> **Goal:** Make the platform fully functional with AND without Cerbos PDP.
> Both providers must produce identical allow/deny decisions for all resource kinds.
>
> **Last Updated:** 2026-03-24

---

## Executive Summary

The authorization audit revealed 7 resource kinds missing from one or both providers,
a runtime crash bug in `ResourceDescriptorRegistry`, and a batch performance gap in the
fallback provider. This plan addresses all findings in priority order across 4 phases.

**Total estimated effort:** M–L (2–3 focused sessions)

---

## Current State Analysis

### What works today (both providers)

18 resource kinds fully functional: `instance_setting`, `tenant_setting`, `tenant`,
`tenant_user`, `category`, `tag`, `location`, `organization`, `organization_member`,
`organization_review`, `event`, `event_session`, `event_session_agenda_item`,
`event_registration`, `storage_object`, `user`, `atproto_record`, `indexed_did`.

### What is broken

| Resource Kind | Fallback | Cerbos | Registry | HATEOAS | MediatR |
|---|---|---|---|---|---|
| `tenant_member` | ❌ Missing | ❌ No YAML | ✅ Present | ❌ Denied | ❌ `[AuthorizeResource]` blocked |
| `group` | ❌ Missing | ❌ No YAML | ✅ Present | ❌ Edit/delete denied | ⚠️ Endpoint auth only |
| `group_member` | ❌ Missing | ❌ No YAML | ❌ **Missing** (crash) | ❌ Crash | ⚠️ Endpoint auth only |
| `custom_property_definition` | ❌ Missing | ❌ No YAML | ✅ Present | ❌ Edit/delete denied | ⚠️ Endpoint auth only |
| `event_contact_share_consent` | ❌ Missing | ✅ Present | ➖ N/A | ➖ No links | ❌ Commands blocked in fallback |
| `notification` | ❌ Missing | ❌ No YAML | ➖ N/A | ⚠️ Static auth only | ⚠️ Endpoint auth only |
| `actor` | ❌ Missing | ❌ No YAML | ➖ N/A | ✅ Read-only | ⚠️ Endpoint auth only |

### Key files

| File | Purpose |
|---|---|
| `Explore.Application/Contracts/Infrastructure/IAuthorizationProvider.cs` | Provider contract |
| `Explore.Infrastructure/Services/FallbackAuthorizationService.cs` | Local RBAC provider |
| `Explore.Infrastructure/Services/CerbosAuthorizationService.cs` | Cerbos PDP provider |
| `Explore.Infrastructure/Services/RuntimeAuthorizationProvider.cs` | Routing wrapper |
| `Explore.Application/Authorization/ResourceDescriptorRegistry.cs` | DTO→resource kind mapping |
| `Explore.API/Hateoas/HateoasAuthorizationEvaluator.cs` | HATEOAS batch check |
| `Explore.API/Hateoas/LinkDefinitionPermissionExtensions.cs` | RequirePermission helper |
| `cerbos/policies/*.yaml` | Cerbos resource policies |

---

## Proposed Future State

After implementation:

1. **All 25 resource kinds** have matching policies in both Cerbos YAML and Fallback C#
2. **ResourceDescriptorRegistry** maps all DTOs that appear in HATEOAS `RequirePermission` calls
3. **Fallback batch** is optimized to reduce DB round-trips for common patterns
4. **Architecture tests** enforce that every resource kind in the registry has both a Cerbos policy and a fallback case
5. **No dead code** — obsolete authorization files deleted

---

## Phase 1: Critical Bug Fixes (Priority: Immediate)

### Task 1.1: Fix `GroupMemberDto` Missing from `ResourceDescriptorRegistry`

- **File:** `Explore.Application/Authorization/ResourceDescriptorRegistry.cs`
- **Change:** Add `[typeof(GroupMemberDto)] = "group_member"` and `[typeof(GroupMemberListDto)] = "group_member"` (if `GroupMemberListDto` exists)
- **Acceptance Criteria:**
  - [ ] GroupMember HATEOAS links no longer throw `InvalidOperationException`
  - [ ] Build passes
- **Effort:** S
- **Related Skills:** `clean-architecture-rules`

### Task 1.2: Add `NotificationDto` to `ResourceDescriptorRegistry` (Preventive)

- **File:** `Explore.Application/Authorization/ResourceDescriptorRegistry.cs`
- **Change:** Add notification DTO mappings even though current HATEOAS links don't use `RequirePermission` — prevents future crash if notification links add permission checks
- **Acceptance Criteria:**
  - [ ] Registry includes notification DTO types → `"notification"`
  - [ ] Build passes
- **Effort:** S

### Task 1.3: Delete Obsolete Authorization Files

- **Files:**
  - `Explore.Application/Contracts/Infrastructure/ICerbosAuthorizationService.cs`
  - `Explore.Application/Authorization/CerbosAuthorizeAttribute.cs`
  - `Explore.Application/Authorization/CerbosPermissionAction.cs`
- **Acceptance Criteria:**
  - [ ] Files deleted
  - [ ] Build passes
  - [ ] No references remain
- **Effort:** S

---

## Phase 2: Fallback Provider — Add Missing Resource Kinds

All tasks in this phase modify `Explore.Infrastructure/Services/FallbackAuthorizationService.cs`.
Each new case must follow the existing pattern:
- Instance admin bypass already handled at top of `IsAllowedAsync`
- SafeMode already handled at top
- New cases added to the `resourceKind switch`

### Task 2.1: Add `tenant_member` Case

- **Authorization semantics:** Tenant admin can CRUD tenant members within their tenant. Instance admin (already bypassed). All others denied.
- **Implementation:** Delegate to `EvaluateTenantScopedAccessAsync`
- **Acceptance Criteria:**
  - [ ] `"tenant_member"` case in switch delegates correctly
  - [ ] Matches Cerbos policy semantics (to be written in Phase 3)
  - [ ] Unit test: tenant admin can create/update/delete; non-admin denied
- **Effort:** S
- **Related Skills:** `clean-architecture-rules`, `auth-patterns`

### Task 2.2: Add `group` Case

- **Authorization semantics:** Tenant admin or org admin can CRUD groups. All authenticated can view.
- **Implementation:** Delegate to `EvaluateOrgScopedAccessAsync` for mutations; return `true` for `"view"` action
- **Acceptance Criteria:**
  - [ ] `"group"` case in switch
  - [ ] View allowed for all authenticated
  - [ ] CUD requires tenant/org admin
  - [ ] Unit test
- **Effort:** S

### Task 2.3: Add `group_member` Case

- **Authorization semantics:** Same as `organization_member` — tenant admin or org admin (via group's org) can manage. All authenticated can view.
- **Implementation:** Delegate to `EvaluateOrgScopedAccessAsync` for mutations; return `true` for `"view"` and `"create"` action
- **Acceptance Criteria:**
  - [ ] `"group_member"` case in switch
  - [ ] Matches org member pattern
  - [ ] Unit test
- **Effort:** S

### Task 2.4: Add `custom_property_definition` Case

- **Authorization semantics:** Tenant admin can CRUD custom property definitions. All authenticated can view.
- **Implementation:** Delegate to `EvaluateTenantScopedAccessAsync` for mutations; return `true` for `"view"` action
- **Acceptance Criteria:**
  - [ ] `"custom_property_definition"` case in switch
  - [ ] Unit test
- **Effort:** S

### Task 2.5: Add `event_contact_share_consent` Case

- **Authorization semantics:** Must match existing Cerbos policy (`event_contact_share_consent.yaml`):
  - Instance admin: all actions
  - Tenant admin: `viewsharedcontacts`, `exportsharedcontacts`
  - Org admin: `viewsharedcontacts`, `exportsharedcontacts`
- **Implementation:** New private method `EvaluateContactShareConsentAccessAsync` checking action names
- **Acceptance Criteria:**
  - [ ] `"event_contact_share_consent"` case in switch
  - [ ] Tenant/org admin can view and export
  - [ ] Non-admin denied
  - [ ] Unit test
- **Effort:** S

### Task 2.6: Add `notification` Case

- **Authorization semantics:** Personal data — users can manage their own notifications. Tenant admin for administrative actions.
- **Implementation:** New private method; action `"view"`, `"update"`, `"create"`, `"delete"` allowed for all authenticated (notifications are personal)
- **Acceptance Criteria:**
  - [ ] `"notification"` case in switch
  - [ ] All authenticated users can manage own notifications
  - [ ] Unit test
- **Effort:** S

### Task 2.7: Add `actor` Case

- **Authorization semantics:** Actors are system-managed (created via user/org registration). Read-only for all authenticated; write only by tenant/instance admin.
- **Implementation:** `"view"` → true; mutations → `EvaluateTenantScopedAccessAsync`
- **Acceptance Criteria:**
  - [ ] `"actor"` case in switch
  - [ ] Read allowed; write requires admin
  - [ ] Unit test
- **Effort:** S

---

## Phase 3: Cerbos Policies — Add Missing Resource Kind YAML Files

All tasks create new files in `cerbos/policies/`. Each policy must:
- Use `apiVersion: api.cerbos.dev/v1`
- Import `explore_admin_roles` derived roles
- Follow the established pattern from existing policies (e.g., `event.yaml`)

### Task 3.1: Create `tenant_member.yaml`

- **File:** `cerbos/policies/tenant_member.yaml`
- **Rules:**
  - `instance_admin`: all actions
  - `tenant_admin`: view, create, update, delete
  - `authenticated_user`: view (own tenant members)
- **Acceptance Criteria:**
  - [ ] Policy file created with correct YAML structure
  - [ ] `cerbos test` passes (if test fixtures exist)
- **Effort:** S

### Task 3.2: Create `group.yaml`

- **File:** `cerbos/policies/group.yaml`
- **Rules:**
  - `instance_admin`: all actions
  - `tenant_admin`: view, create, update, delete
  - `org_admin`: view, create, update, delete
  - `authenticated_user`: view
- **Effort:** S

### Task 3.3: Create `group_member.yaml`

- **File:** `cerbos/policies/group_member.yaml`
- **Rules:**
  - `instance_admin`: all actions
  - `tenant_admin`: view, create, update, delete
  - `org_admin`: view, create, update, delete
  - `authenticated_user`: view, create
- **Effort:** S

### Task 3.4: Create `custom_property_definition.yaml`

- **File:** `cerbos/policies/custom_property_definition.yaml`
- **Rules:**
  - `instance_admin`: all actions
  - `tenant_admin`: view, create, update, delete
  - `authenticated_user`: view
- **Effort:** S

### Task 3.5: Create `notification.yaml`

- **File:** `cerbos/policies/notification.yaml`
- **Rules:**
  - `instance_admin`: all actions
  - `tenant_admin`: view, create, update, delete
  - `authenticated_user`: view, create, update, delete (personal notifications)
- **Effort:** S

### Task 3.6: Create `actor.yaml`

- **File:** `cerbos/policies/actor.yaml`
- **Rules:**
  - `instance_admin`: all actions
  - `tenant_admin`: view, create, update, delete
  - `org_admin`: view
  - `authenticated_user`: view
- **Effort:** S

---

## Phase 4: Batch Optimization & Guardrails

### Task 4.1: Optimize `FallbackAuthorizationService.IsAllowedBatchAsync`

- **File:** `Explore.Infrastructure/Services/FallbackAuthorizationService.cs`
- **Current:** Sequential loop calling `IsAllowedAsync` N times
- **Optimization strategy:**
  1. Pre-resolve admin context ONCE: `isInstanceAdmin`, `isTenantAdmin(currentTenantId)`, and `orgMemberships`
  2. Pass pre-resolved context to each evaluation instead of re-querying DB
  3. Group checks by resource kind to minimize branch evaluation
- **Design constraint:** Must not change the `IAuthorizationProvider` contract
- **Acceptance Criteria:**
  - [ ] Admin context resolved once per batch, not per check
  - [ ] Functional behavior unchanged (same allow/deny decisions)
  - [ ] Performance measurably improved for batches > 5 checks
  - [ ] Existing unit tests still pass
- **Effort:** M
- **Related Skills:** `clean-architecture-rules`, `cqrs-mediatr-guidelines`

### Task 4.2: Architecture Test — Resource Kind Parity

- **File:** `Event.Architecture.Tests/AuthorizationParityTests.cs` (new)
- **Tests:**
  1. Every resource kind string in `ResourceDescriptorRegistry` has a matching case in `FallbackAuthorizationService`
  2. Every resource kind string in `ResourceDescriptorRegistry` has a matching `*.yaml` file in `cerbos/policies/`
  3. Every HATEOAS link policy file that calls `RequirePermission` references a DTO type that exists in `ResourceDescriptorRegistry`
- **Acceptance Criteria:**
  - [ ] Tests created and passing
  - [ ] Tests catch future drift (adding a new resource kind without updating both providers)
- **Effort:** M
- **Related Skills:** `clean-architecture-rules`

### Task 4.3: Unit Tests for All New Fallback Cases

- **File:** `Event.Application.UnitTests/Services/FallbackAuthorizationServiceTests.cs` (new or extend)
- **Coverage:**
  - Each new resource kind from Phase 2
  - Instance admin bypass
  - Tenant admin access
  - Org admin access (where applicable)
  - Non-admin denial
  - SafeMode denial
- **Acceptance Criteria:**
  - [ ] Each resource kind has ≥3 test cases (admin allow, non-admin deny, SafeMode deny)
  - [ ] All tests pass
- **Effort:** M

### Task 4.4: Integration Test — Fallback Provider End-to-End

- **File:** `Event.API.IntegrationTests/Authorization/FallbackAuthorizationIntegrationTests.cs` (new)
- **Tests:**
  - With `authorization.provider` set to `"local"`, verify key HATEOAS responses include correct links
  - Verify tenant_member CRUD works via API when using fallback
- **Acceptance Criteria:**
  - [ ] Tests confirm fallback mode produces correct API behavior
  - [ ] Tests pass in CI
- **Effort:** M

---

## Risk Assessment

| Risk | Likelihood | Impact | Mitigation |
|---|---|---|---|
| Notification authorization semantics wrong (personal vs tenant-scoped) | Medium | Medium | Review notification command handlers for ownership checks; align fallback with actual business rules |
| Group authorization needs org context not available in attributes | Low | Medium | Verify group HATEOAS links pass `organizationId` in attributes; add if missing |
| Batch optimization changes functional behavior | Low | High | Run full test suite before and after; keep existing sequential path as fallback |
| New Cerbos policies conflict with scoped tenant overrides | Low | Medium | Use `version: "default"` consistently; test with tenant scoping |
| Missing PermissionAction variants (e.g., `viewsharedcontacts`) in fallback | Medium | High | Verify all action strings used in `[AuthorizeResource]` attributes are handled |

---

## Success Metrics

1. **Zero runtime crashes** — `ResourceDescriptorRegistry` resolves all DTO types used in HATEOAS
2. **Parity test green** — Architecture test confirms every resource kind in registry has both providers covered
3. **Fallback mode functional** — All API integration tests pass with `authorization.provider = "local"`
4. **Batch performance** — Fallback batch check for 20 items completes in <50ms (vs current ~500ms)
5. **No dead code** — Obsolete authorization files deleted

---

## Critique: Potential Risks & Unknowns

The **most likely point of failure** is Task 2.6 (notification authorization semantics). Notifications
are personal data, but the exact ownership model (user-scoped vs tenant-scoped) needs verification
against the notification command handlers. If notifications have no `[AuthorizeResource]` attribute
today, adding a fallback case is low-risk — but if someone later adds resource-level auth to
notification commands, the semantics must match exactly.

The **batch optimization** (Task 4.1) carries architectural risk: pre-resolving admin context
assumes all checks in a batch share the same principal context (which is true for HATEOAS but
could be violated if batch is used elsewhere in the future). The optimization should be documented
as assuming single-principal batches.

The **`group_member` authorization** depends on resolving the group's parent organization to
determine org admin access. If the HATEOAS link attributes don't include `organizationId`, the
fallback will incorrectly deny org admins. Verify the `GroupMemberLinkPolicy` passes this attribute.
