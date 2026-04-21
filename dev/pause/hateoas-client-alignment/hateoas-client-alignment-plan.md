# HATEOAS Client Alignment — Implementation Plan

> Last Updated: 2026-03-27
>
> **Parent plan:** `dev/active/api-contract-stabilization/api-contract-stabilization-plan.md`
> **Do not start before:** Phase 4 of parent plan merged (✅ complete as of 2026-04-20).

---

## Executive Summary

A comprehensive audit of the HATEOAS + Cerbos/Fallback authorization implementation revealed that the **API layer is architecturally sound and enterprise-grade**: all 22 resource assemblers and 23 link policies correctly batch authorization decisions through `HateoasAuthorizationEvaluator` → `IAuthorizationProvider` (Cerbos or local RBAC), with fail-closed semantics.

The Blazor client consumes HATEOAS links correctly for the Events domain. However, **one concrete violation** exists in `OrganizationDetails`, plus two secondary risks (fragility in link deserialization, missing per-item management links in collection policies). This plan corrects all identified issues.

**Goal**: Every Blazor UI component that shows or hides management actions (edit, delete, create) must base that decision solely on whether the API-returned HAL `_links` include the corresponding affordance. No client-side role checking, no extra member-list round-trips, no duplicated authorization logic.

---

## Current State Analysis

### ✅ What Works (Do Not Touch)

| Component | Correct Pattern |
|---|---|
| `EventDetail.razor.cs:333` | `_canEdit = _eventDetails.HasHalLink("edit")` |
| `EventList.razor:84` | `_selectedEvent.HasManagementLinks()` / `HasHalLink("edit"/"delete")` |
| `HateoasAuthorizationEvaluator` | Batch evaluation, fail-closed, static checks first |
| All 22 `*ResourceAssembler` classes | Correct HAL assembly with batch authorization |
| All 23 `*LinkPolicy` classes | All write-action links gated by `.RequirePermission()` |
| `NavMenu.razor.cs` admin claims | Justified exception (navigation structure, not resource actions) |
| `EventCreationEligibilityService` | Justified exception (multi-resource cross-concern) |

### ❌ Violation: `OrganizationDetails.razor.cs`

**File**: `Explore.Blazor.Client/Pages/Organizations/OrganizationDetails.razor.cs:103`

```csharp
private async Task CheckEditPermissions()
{
    var members = await MemberService.GetMembersAsync(Id);     // extra API call
    var me = members.FirstOrDefault(m => m.UserId == currentUserId);
    currentUserRole = me?.RoleId;
    canEdit = RoleHelper.CanManage(currentUserRole);           // local role duplication
}
```

**Problems**:
1. Duplicates auth logic already encoded in `OrganizationDetailLinkPolicy.edit` → Cerbos
2. Extra `GetMembersAsync()` round-trip on every page load
3. If Cerbos policy changes (e.g., admins lose edit rights), UI still shows Edit
4. `OrganizationService.GetOrganizationByIdAsync()` already calls `result?.ToDto()` which preserves `_links` via `[JsonExtensionData]` — the data is there, unused

### ⚠️ Fragility Risk: `HalResourceExtensions.DeserializeItems<T>()`

**File**: `Explore.Blazor.Client/Helpers/HalResourceExtensions.cs:500`

`_links` survival in collection items relies on NSwag maintaining `[JsonExtensionData]` on all generated list DTOs. There is no test guard. Silent regression risk: if NSwag regenerates without this annotation, `HasHalLink()` returns `false` everywhere with no error.

### ⚠️ Policy Gap: Collection Policies Missing Per-Item Management Links

| Policy | `GetItemLinks` Missing |
|---|---|
| `OrganizationCollectionLinkPolicy` | `edit`, `delete` per-item links |
| `GroupCollectionLinkPolicy` | `edit`, `delete` per-item links |

`EventCollectionLinkPolicy` already returns `edit`/`delete` per-item. The org and group collection policies do not, making org/group list views unable to surface management affordances via HATEOAS.

### ⚠️ Missing `HasHalLink` Extensions for Non-Event DTOs

`HasHalLink()` exists only for `EventDto` and `EventListDto`. `OrganizationDto`, `OrganizationListDto`, `GroupDto`, `GroupListDto` and others have no equivalent helpers, blocking adoption of the correct pattern in non-event pages.

---

## Proposed Future State

After implementation:

1. **`OrganizationDetails`** reads `organization.HasHalLink("edit")` — zero extra API calls, Cerbos-driven
2. **`HalResourceExtensions`** has a shared private helper `HasHalLinkInAdditionalProperties(IDictionary<string,object>?, string)` that all per-type extensions delegate to — no logic duplication
3. **All primary DTOs** used by Blazor UI pages have typed `HasHalLink()` extensions
4. **`OrganizationCollectionLinkPolicy`** and **`GroupCollectionLinkPolicy`** return `edit`/`delete` per-item with `.RequirePermission()` — matching the EventCollectionLinkPolicy pattern
5. **Integration tests** verify `_links` survive `ToDto()` deserialization for `EventDto`, `EventListDto`, `OrganizationDto`, `OrganizationListDto`
6. **Integration tests** verify collection responses carry per-item management links for authorized users on organization and group endpoints

---

## Implementation Phases

### Phase 1: API Layer — Collection Policy Gaps
*Fix the two collection link policies that are missing per-item management affordances.*

#### Task 1.1: Add Per-Item Management Links to `OrganizationCollectionLinkPolicy`
- **File**: `Explore.API/Hateoas/Policies/OrganizationLinkPolicy.cs`
- **Change**: Add `edit` and `delete` `LinkDefinition` to `GetItemLinks()` matching `EventCollectionLinkPolicy` pattern
- **Acceptance Criteria**:
  - [ ] `GetItemLinks()` returns `edit` link with `RequirePermission(PermissionAction.Update, dto, dto.Id.ToString(), attrs)` where attrs includes `organizationId` and `tenantId`
  - [ ] `GetItemLinks()` returns `delete` link with `RequirePermission(PermissionAction.Delete, dto, dto.Id.ToString(), attrs)`
  - [ ] Both links have `RequiresAuth: true`
  - [ ] Anonymous requests to `GET /api/organization` return collection items **without** `edit`/`delete` links
  - [ ] Authorized org admin requests return collection items **with** `edit` link
- **Effort**: S
- **Skills**: `clean-architecture-rules`
- **Dependency**: None

#### Task 1.2: Add Per-Item Management Links to `GroupCollectionLinkPolicy`
- **File**: `Explore.API/Hateoas/Policies/GroupLinkPolicy.cs`
- **Change**: Add `edit` and `delete` to `GetItemLinks()` matching the GroupDetailLinkPolicy pattern
- **Acceptance Criteria**:
  - [ ] `GetItemLinks()` returns `edit` link with `RequirePermission(PermissionAction.Update, dto, dto.Id.ToString(), attrs)` where attrs includes `groupId` and `tenantId`
  - [ ] `GetItemLinks()` returns `delete` link with `RequirePermission(PermissionAction.Delete, dto, dto.Id.ToString(), attrs)`
  - [ ] Both links have `RequiresAuth: true`
- **Effort**: S
- **Skills**: `clean-architecture-rules`
- **Dependency**: None

---

### Phase 2: Blazor Client — `HalResourceExtensions` Refactor & Expansion
*Extract shared helper, add `HasHalLink` for all primary DTO types used in management UI.*

#### Task 2.1: Extract `HasHalLinkInAdditionalProperties` Private Helper
- **File**: `Explore.Blazor.Client/Helpers/HalResourceExtensions.cs`
- **Change**: Add a private static method that contains the shared link-check logic:
  ```
  private static bool HasHalLinkInAdditionalProperties(
      IDictionary<string, object>? additionalProperties,
      string linkRel)
  ```
  The implementation checks `additionalProperties["_links"]` for a `JsonElement` with the given property name — identical to the logic in both existing `HasHalLink` overloads.
- **Then refactor**: Both existing `EventDto.HasHalLink()` and `EventListDto.HasHalLink()` delegate to this helper instead of repeating the logic.
- **Acceptance Criteria**:
  - [ ] Private helper exists and contains the single canonical implementation
  - [ ] `EventDto.HasHalLink()` and `EventListDto.HasHalLink()` delegate to the helper
  - [ ] All existing `HasHalLink`-based tests still pass (no behavioral change)
- **Effort**: S
- **Skills**: `clean-architecture-rules`
- **Dependency**: None

#### Task 2.2: Add `HasHalLink` for `OrganizationDto` and `OrganizationListDto`
- **File**: `Explore.Blazor.Client/Helpers/HalResourceExtensions.cs`
- **Change**: Add two extension methods in the `// ========== Organization Extensions ==========` section:
  ```
  public static bool HasHalLink(this OrganizationDto dto, string linkRel)
  public static bool HasHalLink(this OrganizationListDto dto, string linkRel)
  ```
  Both delegate to the private helper from Task 2.1.
- **Acceptance Criteria**:
  - [ ] `HasHalLink(this OrganizationDto, string)` returns `true` when `_links` contains the given rel
  - [ ] `HasHalLink(this OrganizationListDto, string)` same
  - [ ] Returns `false` when `AdditionalProperties` is null or `_links` absent
- **Effort**: S
- **Dependency**: Task 2.1

#### Task 2.3: Add `HasHalLink` for `GroupDto` and `GroupListDto`
- **File**: `Explore.Blazor.Client/Helpers/HalResourceExtensions.cs`
- **Change**: Add two extension methods in a new `// ========== Group Extensions ==========` section
- **Acceptance Criteria**:
  - [ ] `HasHalLink(this GroupDto, string)` and `HasHalLink(this GroupListDto, string)` implemented and delegating to shared helper
- **Effort**: S
- **Dependency**: Task 2.1

#### Task 2.4: Add `HasManagementLinks` Helpers for Organization and Group
- **File**: `Explore.Blazor.Client/Helpers/HalResourceExtensions.cs`
- **Change**: Add convenience methods:
  ```
  public static bool HasManagementLinks(this OrganizationListDto dto)
  public static bool HasManagementLinks(this GroupListDto dto)
  ```
  Matching the existing `EventListDto.HasManagementLinks()` pattern.
- **Acceptance Criteria**:
  - [ ] Returns `true` if `edit` or `delete` link present
- **Effort**: XS
- **Dependency**: Tasks 2.2, 2.3

---

### Phase 3: Blazor Client — Fix `OrganizationDetails` Violation
*Remove the duplicated role-based permission check and replace with HATEOAS link reading.*

#### Task 3.1: Remove `CheckEditPermissions()` from `OrganizationDetails`
- **File**: `Explore.Blazor.Client/Pages/Organizations/OrganizationDetails.razor.cs`
- **Change**:
  1. Delete the `CheckEditPermissions()` method entirely
  2. Delete the `currentUserRole` field (`private int? currentUserRole`)
  3. Remove the `CheckEditPermissions()` call from `LoadOrganization()`
  4. In `LoadOrganization()`, after `organization = await OrganizationService.GetOrganizationByIdAsync(Id)`, set:
     ```csharp
     canEdit = organization?.HasHalLink("edit") ?? false;
     ```
  5. Remove the `await Task.WhenAll(permissionsTask, eventsTask)` orchestration if `permissionsTask` was the only concurrent task; adjust to just run `eventsTask`
- **Acceptance Criteria**:
  - [ ] No reference to `RoleHelper.CanManage` in `OrganizationDetails.razor.cs`
  - [ ] No reference to `currentUserRole` field
  - [ ] No `GetMembersAsync()` call in `OrganizationDetails` (for the purpose of permission checking)
  - [ ] `canEdit` is set by reading `organization.HasHalLink("edit")`
  - [ ] An org admin who loads the page sees the Edit button (API returns `edit` link)
  - [ ] An org member (non-admin) who loads the page does not see the Edit button (API does not return `edit` link)
  - [ ] Page load no longer makes the extra members API call for authorization purposes
- **Effort**: S
- **Skills**: `blazor-ui-conventions`
- **Dependency**: Task 2.2

#### Task 3.2: Verify `OrganizationService.GetOrganizationByIdAsync` Preserves `_links`
- **File**: `Explore.Blazor.Client/Services/OrganizationService.cs`
- **Verification only** (read, no change needed if already correct): Confirm `result?.ToDto()` uses the JSON round-trip that preserves `_links` in `OrganizationDto.AdditionalProperties`.
  - The implementation at line 183-184 already calls `result?.ToDto()` which is `HalResourceOfOrganizationDto.ToDto()` — confirmed to use JSON serialization preserving `[JsonExtensionData]`
  - **No code change needed** — just document the confirmation in context file
- **Effort**: XS
- **Dependency**: None

---

### Phase 4: Test Coverage — Integration Tests

#### Task 4.1: Add `_links` Deserialization Survival Tests (Fragility Guard)
- **File**: `Event.API.IntegrationTests/Features/Hateoas/HateoasLinkDeserializationTests.cs` *(new file)*
- **Purpose**: Guard against NSwag regeneration silently removing `[JsonExtensionData]` from generated DTOs, which would break `HasHalLink()` across all pages
- **Tests to add**:
  - `EventDto_DeserializedFromHalResource_PreservesLinks()`: Fetch `GET /api/event/{id}` as authenticated owner → call `ToDto()` → assert `dto.HasHalLink("edit")` is `true`
  - `EventListDto_DeserializedFromCollection_PreservesLinks()`: Fetch `GET /api/event` as authenticated owner with owned event → get items → assert item `HasHalLink("edit")` is `true` for owned item
  - `OrganizationDto_DeserializedFromHalResource_PreservesLinks()`: Same pattern for `GET /api/organization/{id}` as org admin
  - `OrganizationListDto_DeserializedFromCollection_PreservesLinks()`: Same for collection response
- **Acceptance Criteria**:
  - [ ] All four tests pass green in CI
  - [ ] Tests use `AuthenticatedApiTestFixture` (existing pattern)
  - [ ] Test names clearly describe the regression being guarded
  - [ ] ABOUTME header at top of file
- **Effort**: M
- **Skills**: `cqrs-mediatr-guidelines`
- **Dependency**: Tasks 1.1, 2.2

#### Task 4.2: Add `OrganizationCollectionLinkPolicy` Per-Item Link Tests
- **File**: `Event.API.IntegrationTests/Features/Hateoas/OrganizationHateoasTests.cs` *(extend existing)*
- **Tests to add**:
  - `GetOrganizations_AsOrgAdmin_EmbeddedItemsShouldIncludeEditLink()`
  - `GetOrganizations_Anonymous_EmbeddedItemsShouldNotIncludeEditLink()`
  - `GetOrganizations_AsOrgAdmin_EmbeddedItemsShouldIncludeDeleteLink()`
- **Acceptance Criteria**:
  - [ ] Tests follow the existing `OrganizationHateoasTests` pattern
  - [ ] Tests pass green
- **Effort**: S
- **Dependency**: Task 1.1

#### Task 4.3: Add Blazor Component Test for `OrganizationDetails` HATEOAS Consumption
- **File**: `Explore.Blazor.Client.Tests/Pages/Organizations/OrganizationDetailsHateoasTests.cs` *(new file)*
- **Purpose**: Verify `canEdit` is driven by HAL link, not by role check
- **Tests to add**:
  - `OrganizationDetails_WhenApiReturnsEditLink_ShowsEditButton()`
  - `OrganizationDetails_WhenApiDoesNotReturnEditLink_HidesEditButton()`
- **Approach**: Use bUnit with mocked `IOrganizationService` returning `OrganizationDto` with/without `_links.edit` in `AdditionalProperties`
- **Acceptance Criteria**:
  - [ ] Tests verify that the Edit UI control visibility is purely determined by `HasHalLink("edit")`
  - [ ] No `GetMembersAsync` call is made during rendering
  - [ ] ABOUTME header at top of file
- **Effort**: M
- **Skills**: `blazor-ui-conventions`
- **Dependency**: Task 3.1

---

### Phase 5: Documentation Update

#### Task 5.1: Update `docs/API.md` HATEOAS Section
- **File**: `docs/API.md`
- **Change**: Add a subsection under "Authorization-Aware Links" titled **"Blazor Client Consumption Pattern"** that documents:
  1. The canonical pattern: read `_links` via `HasHalLink(rel)`, not client-side role checks
  2. The `HalResourceExtensions` helpers and when to add new ones
  3. The `ToDto()` JSON round-trip contract (requires `[JsonExtensionData]` on generated DTOs)
  4. The legitimate exceptions: NavMenu admin claims, EventCreationEligibilityService, route guards
- **Acceptance Criteria**:
  - [ ] New subsection is clear and actionable
  - [ ] References concrete file paths
  - [ ] Explicitly calls out `OrganizationDetails`-style pattern as the anti-pattern to avoid
- **Effort**: S
- **Dependency**: Tasks 3.1, 4.1

#### Task 5.2: Update `CLAUDE.md` Non-Inferable Technical Rules
- **File**: `CLAUDE.md`
- **Change**: Add rule under "Non-Inferable Technical Rules":
  > **Rule #12**: Blazor UI components must determine action affordance (edit/delete/create visibility) exclusively from HAL `_links` returned by the API. Never use `RoleHelper`, `IsInRole()`, or member-list fetches to derive action permissions in UI components. Exceptions: NavMenu admin claims (navigation structure), EventCreationEligibilityService (multi-resource eligibility), page-level `[Authorize]` route guards.
- **Acceptance Criteria**:
  - [ ] Rule is in the Non-Inferable Technical Rules section
  - [ ] References `HalResourceExtensions.HasHalLink()` as the mechanism
- **Effort**: XS
- **Dependency**: Task 3.1

---

## Risk Assessment

| Risk | Likelihood | Impact | Mitigation |
|---|---|---|---|
| `OrganizationDetails` edit visibility breaks for users who previously saw Edit (Cerbos policy already denied them) | Low | Medium | The change makes behavior *correct*, not stricter. An org admin's Cerbos decision allows edit → link present → Edit button shown. Same outcome. |
| `HasHalLink` returns false for org when `ToDto()` drops `_links` | Low | High | Task 3.2 confirms the existing `ToDto()` chain is correct. Task 4.1 adds a regression test. |
| NSwag regeneration removes `[JsonExtensionData]` from future DTO types | Medium | High | Task 4.1 regression tests; document the requirement in `CONTRIBUTING.md` |
| Collection policy changes (Tasks 1.1, 1.2) affect output cache | Low | Low | Cache already varies by `Authorization` header for `ListData` policy — no new variance needed |
| `CheckEditPermissions()` removal breaks other callers | None | None | Method is `private` — no external callers |

---

## Success Metrics

1. **Zero client-side role checks** for action affordance in Blazor components — `grep -r "RoleHelper.Can" Pages/` returns only display-label usages, not action-gating
2. **All tests green** including new integration tests and Blazor component tests
3. **One fewer API call** on `OrganizationDetails` page load (member list no longer fetched for auth)
4. **`HasHalLink`** available for `OrganizationDto`, `OrganizationListDto`, `GroupDto`, `GroupListDto`
5. **Collection responses** for `/api/organization` and `/api/group` include per-item `edit`/`delete` links for authorized users

---

## Effort Summary

| Phase | Tasks | Total Effort |
|---|---|---|
| Phase 1: API Collection Policies | 2 tasks | ~1 hour |
| Phase 2: HalResourceExtensions | 4 tasks | ~1 hour |
| Phase 3: OrganizationDetails Fix | 2 tasks | ~30 min |
| Phase 4: Test Coverage | 3 tasks | ~3 hours |
| Phase 5: Documentation | 2 tasks | ~30 min |
| **Total** | **13 tasks** | **~6 hours** |

---

## Dependencies Between Tasks

```
Task 1.1 (Org collection policy) ──────────────────────────┐
Task 1.2 (Group collection policy)                         │
                                                            ▼
Task 2.1 (shared helper) ──► Task 2.2 (OrgDto extensions) ──► Task 3.1 (OrganizationDetails fix) ──► Task 4.3 (Blazor test)
Task 2.1              ──────► Task 2.3 (GroupDto extensions)
Task 2.2, 2.3         ──────► Task 2.4 (HasManagementLinks)
Task 1.1, 2.2         ──────► Task 4.1 (deserialization guard tests)
Task 1.1              ──────► Task 4.2 (org collection tests)
Task 3.1              ──────► Task 5.1 (API.md update)
Task 3.1              ──────► Task 5.2 (CLAUDE.md rule)
```

---

## Potential Risks & Unknowns

The most likely source of friction is **Task 4.3** (Blazor component test for `OrganizationDetails`). Constructing a `OrganizationDto` with `_links` populated in `AdditionalProperties` in a bUnit test requires manually building the `Dictionary<string, object>` with a serialized `JsonElement` matching the structure that `HasHalLink()` expects — this is non-obvious setup. The existing `Explore.Blazor.Client.Tests` project should be checked for how it currently mocks HAL responses before writing new tests. If no HAL-response test helpers exist, a small test helper factory will need to be added.

The second source of risk is **Task 1.1/1.2** (collection policy per-item links): adding `edit`/`delete` to collection item policies means every page request for `/api/organization` or `/api/group` now batches more permission checks with Cerbos. With a page size of 20 organizations and 2 new links per item, that is 40 additional checks per batch call. This is by design (the batch semantics exist precisely for this), but Cerbos performance with large batches should be validated under load if organizations/groups lists are frequently accessed.
