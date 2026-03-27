ABOUTME: Task checklist for authorization parity implementation.
ABOUTME: Tracks progress across 4 phases — bug fixes, fallback cases, Cerbos policies, and optimization.

# Authorization Parity — Task Checklist

**Last Updated:** 2026-03-24

---

## Phase 1: Critical Bug Fixes ✅ COMPLETE

- [x] **1.1** Fix `GroupMemberDto` missing from `ResourceDescriptorRegistry` (S)
  - Added `GroupMemberDto`, `GroupMemberListDto` → `"group_member"`
  - Also added `NotificationDto/ListDto` → `"notification"` and `ActorDto/ListDto` → `"actor"`
  - Also fixed `ToActionString` missing `ViewSharedContacts` and `ExportSharedContacts` mappings

- [x] **1.2** Add `NotificationDto` to `ResourceDescriptorRegistry` — preventive (S)
  - Done in 1.1

- [x] **1.3** Delete obsolete authorization files (S)
  - Deleted `ICerbosAuthorizationService.cs`, `CerbosAuthorizeAttribute.cs`, `CerbosPermissionAction.cs`

---

## Phase 2: Fallback Provider — Missing Resource Kinds ✅ COMPLETE

- [x] **2.1** Add `tenant_member` case — delegates to `EvaluateTenantScopedAccessAsync`
- [x] **2.2** Add `group` case — view open, mutations via `EvaluateViewableOrgResourceAccessAsync`
- [x] **2.3** Add `group_member` case — view/create open, mutations via `EvaluateGroupMemberAccessAsync`
- [x] **2.4** Add `custom_property_definition` case — view open, mutations via `EvaluateViewableTenantResourceAccessAsync`
- [x] **2.5** Add `event_contact_share_consent` case — new `EvaluateContactShareConsentAccessAsync`
- [x] **2.6** Add `notification` case — all CRUD allowed for authenticated users
- [x] **2.7** Add `actor` case — view open, mutations via `EvaluateActorAccessAsync`

---

## Phase 3: Cerbos Policies — Missing YAML Files ✅ COMPLETE

- [x] **3.1** Created `cerbos/policies/tenant_member.yaml`
- [x] **3.2** Created `cerbos/policies/group.yaml`
- [x] **3.3** Created `cerbos/policies/group_member.yaml`
- [x] **3.4** Created `cerbos/policies/custom_property_definition.yaml`
- [x] **3.5** Created `cerbos/policies/notification.yaml`
- [x] **3.6** Created `cerbos/policies/actor.yaml`

---

## Phase 4: Optimization & Guardrails ✅ COMPLETE

- [x] **4.1** Optimize `FallbackAuthorizationService.IsAllowedBatchAsync` (M)
  - Pre-resolves `AuthorityProfile` (isInstanceAdmin, isTenantAdmin, adminOrgIds) once per batch
  - Synchronous `EvaluateWithProfile` mirrors all switch cases without async overhead
  - Small batches (≤2) still use the sequential async path to avoid overhead

- [x] **4.2** Architecture test — resource kind parity (M)
  - Created `Event.Architecture.Tests/AuthorizationParityTests.cs`
  - 4 tests: registry→fallback, registry→cerbos, PermissionAction completeness, cerbos→fallback
  - All 44 architecture tests pass

- [x] **4.3** Unit tests for all new fallback cases (M)
  - Extended `FallbackAuthorizationServiceTests.cs` with 20+ new test methods
  - Covers: tenant_member, group, group_member, custom_property_definition,
    event_contact_share_consent, notification, actor, SafeMode, batch optimization
  - All 535 application unit tests pass

- [ ] **4.4** Integration test — fallback mode end-to-end (M)
  - Deferred: requires running API with specific SystemSetting configuration
  - Can be added when integration test infrastructure supports auth provider switching

---

## Test Results Summary

| Test Project | Total | Pass | Fail | Notes |
|---|---|---|---|---|
| Event.Application.UnitTests | 535 | 535 | 0 | All new authz tests pass |
| Event.Architecture.Tests | 44 | 44 | 0 | New parity tests pass |
| Event.Domain.UnitTests | 100 | 100 | 0 | No regression |
| Explore.Secrets.UnitTests | 190 | 190 | 0 | No regression |
| Explore.Blazor.Client.Tests | 593 | 592 | 1 | Pre-existing failure (EventList_HidesNoEventsState_WhenResultsExist) |
