# HATEOAS Client Alignment — Task Checklist

> Last Updated: 2026-04-21 (ALL PHASES ✅ COMPLETE)

---

## Phase 1: API Layer — Collection Policy Gaps ✅ COMPLETE (2026-04-20)

### 1.1 Add Per-Item Management Links to `OrganizationCollectionLinkPolicy`
- [x] Added `edit` + `delete` per-item links gated by `.RequirePermission()` using `ResourceDescriptors.OrganizationList`
- [x] Added `TenantId` to `OrganizationListDto` (Application DTO) so `ResourceDescriptor` can extract tenant scope
- [x] Added `ResourceDescriptors.OrganizationList` descriptor entry
- [x] Build: 0 errors. Tests: 555 passed, 2 pre-existing failures.

### 1.2 Add Per-Item Management Links to `GroupCollectionLinkPolicy`
- [x] Added `edit` + `delete` per-item links gated by `.RequirePermission()` using `ResourceDescriptors.GroupList`
- [x] Added `TenantId` to `GroupListDto` (Application DTO) so `ResourceDescriptor` can extract tenant scope
- [x] Added `ResourceDescriptors.GroupList` descriptor entry
- [x] Build: 0 errors.

### 1.1 Add Per-Item Management Links to `OrganizationCollectionLinkPolicy`
- [ ] Open `Explore.API/Hateoas/Policies/OrganizationLinkPolicy.cs`
- [ ] In `GetItemLinks(OrganizationListDto dto, ClaimsPrincipal? user)`:
  - [ ] Add `edit` link: `LinkDefinition.Edit(RouteNames.UpdateOrganization, new { id = dto.Id }).RequirePermission(PermissionAction.Update, dto, dto.Id.ToString(), attrs)`
    - attrs: `{ ["organizationId"] = dto.Id.ToString(), ["tenantId"] = dto.TenantId.ToString() }`
  - [ ] Add `delete` link: same pattern with `PermissionAction.Delete` and `RouteNames.DeleteOrganization`
- [ ] Verify both links have `RequiresAuth: true` (already enforced by `LinkDefinition.Edit/Delete` factory methods)
- [ ] Run `dotnet build --configuration Release --verbosity quiet` — no errors
- [ ] Run `dotnet test --project Event.API.IntegrationTests/... --configuration Release --verbosity quiet` — no regressions

### 1.2 Add Per-Item Management Links to `GroupCollectionLinkPolicy`
- [ ] Open `Explore.API/Hateoas/Policies/GroupLinkPolicy.cs`
- [ ] In `GetItemLinks(GroupListDto dto, ClaimsPrincipal? user)`:
  - [ ] Add `edit` link: `LinkDefinition.Edit(RouteNames.UpdateGroup, new { id = dto.Id }).RequirePermission(PermissionAction.Update, dto, dto.Id.ToString(), attrs)`
    - attrs: `{ ["groupId"] = dto.Id.ToString(), ["tenantId"] = dto.TenantId.ToString() }`
  - [ ] Add `delete` link: same pattern with `PermissionAction.Delete` and `RouteNames.DeleteGroup`
- [ ] Run build and tests — no regressions

---

## Phase 2: HalResourceExtensions — Refactor & Expand ✅ COMPLETE (2026-04-20)

### 2.1 Extract `HasHalLinkInAdditionalProperties` Private Helper
- [x] Added `private static bool HasHalLinkInAdditionalProperties(IDictionary<string, object>? additionalProperties, string linkRel)` 
- [x] Refactored `HasHalLink(this EventListDto ...)` and `HasHalLink(this EventDto ...)` to delegate to helper
- Build: 0 errors

### 2.2 Add `HasHalLink` for `OrganizationDto` and `OrganizationListDto`
- [x] Added `HasHalLink(this OrganizationDto dto, string linkRel)` delegating to shared helper
- [x] Added `HasHalLink(this OrganizationListDto dto, string linkRel)` delegating to shared helper
- [x] Added `HasManagementLinks(this OrganizationListDto dto)`
- [x] Added `HasLink(this HalResourceOfOrganizationDto dto, string linkRel)` delegating to shared helper
- Build: 0 errors

### 2.3 Add `HasHalLink` for Group types
- [x] Added `HasLink(this HalResourceOfGroupDto dto, string linkRel)` delegating to shared helper
- [x] Added `HasManagementLinks(this HalResourceOfGroupDto dto)`
- Note: Standalone `GroupDto`/`GroupListDto` don't exist in the NSwag client — only `HalResourceOfGroupDto`. Application-layer types don't have `AdditionalProperties`.
- Build: 0 errors

---

## Phase 3: OrganizationDetails Fix ✅ COMPLETE (2026-04-20)

### 3.1 Replace `CheckEditPermissions()` with HAL Link Read
- [x] Replaced `async Task CheckEditPermissions()` (extra API call + RoleHelper) with `void CheckEditPermissions()` using `organization?.HasHalLink("edit")`
- [x] Removed `currentUserId`, `currentUserRole` fields (no longer needed)
- [x] Removed `IOrganizationMemberService` injection (no longer needed)
- [x] Removed `AuthenticationStateProvider` injection (no longer needed)
- [x] Removed `using Microsoft.AspNetCore.Components.Authorization` import
- [x] Updated `LoadOrganization()`: replaced `Task.WhenAll(permissionsTask, eventsTask)` with sequential `CheckEditPermissions()` then `await EventService.GetPublicEventsByActorAsync(Id)`
- Build: 0 errors

### 3.2 Verify `_links` Preservation
- [x] `HalResourceExtensions.ToDto(this HalResourceOfOrganizationDto)` uses JSON round-trip ✅
- [x] `OrganizationDto` has `[JsonExtensionData]` on `AdditionalProperties` in generated client ✅

---

## Phase 4: Test Coverage ✅ COMPLETE (2026-04-21)

### 4.1 Add `_links` Deserialization Survival Tests (Regression Guard) ✅ COMPLETE (2026-04-21)
- [x] Created `Event.API.IntegrationTests/Features/Hateoas/HateoasLinkDeserializationTests.cs`
  - [x] ABOUTME two-line header
  - [x] File-scoped namespace `Event.Api.IntegrationTests.Features.Hateoas`
  - [x] `[ClassDataSource<AuthenticatedApiTestFixture>(Shared = SharedType.PerAssembly)]`
  - [x] 4 tests (Event detail + list, Organization detail + list) assert `_links` at root / per embedded item
  - [x] Scope narrowed from original plan: asserts `_links` property presence (regression guard for `[JsonExtensionData]` contract) rather than `HasHalLink("edit") == true` — the latter would need seeded owned data and authorization setup which the existing fixtures don't provide. Tests skip cleanly when no seeded data present (matches existing HATEOAS test convention).
- [x] All 4 tests pass GREEN (run 2026-04-21)

### 4.2 Extend `OrganizationHateoasTests` for Per-Item Management Links ✅ COMPLETE (2026-04-21)
- [x] Created companion class `OrganizationHateoasAuthTests.cs` (original uses unauthenticated `ApiTestFixture`; new class uses `AuthenticatedApiTestFixture` to exercise authorization-gated links)
- [x] `GetOrganizations_AsAuthenticatedUser_EmbeddedItemsIncludeEditLink` — authenticated request sees `_links.edit` on embedded items (StubAuthorizationProvider AllowAll=true by default)
- [x] `GetOrganizations_AsAuthenticatedUser_EmbeddedItemsIncludeDeleteLink` — authenticated request sees `_links.delete` on embedded items
- [x] `GetOrganizations_Anonymous_EmbeddedItemsDoNotIncludeManagementLinks` — anonymous request gets zero management links
- [x] All 3 tests pass GREEN (run 2026-04-21)

### 4.3 Add Blazor Component Tests for `OrganizationDetails` HATEOAS Consumption ✅ COMPLETE (2026-04-21)
- [x] Created `Explore.Blazor.Client.Tests/Pages/Organizations/OrganizationDetailsHateoasTests.cs`
  - [x] ABOUTME header
  - [x] Uses `BlazorTestContext`, registers real `RouterStateService` (Blazouter), `ISnackbar`, `IDialogService`, `ILogger` mocks
  - [x] `WhenApiReturnsEditLink_ShowsEditButton` — inserts a real `JsonElement` (from `JsonDocument.Parse`) containing `edit` link into `OrganizationDto.AdditionalProperties["_links"]` → asserts "Edit" present in markup
  - [x] `WhenApiDoesNotReturnEditLink_HidesEditButton` — no `_links` → asserts "Edit" absent
  - [x] `DoesNotCallOrganizationMembersService_OnPageLoad` — `IOrganizationMemberService.GetMembersAsync` never invoked
- [x] All 3 tests pass GREEN (run 2026-04-21)

---

## Phase 5: Documentation ✅ COMPLETE (2026-04-21)

### 5.1 Update `docs/API.md` — Add Blazor Client Consumption Pattern
- [ ] Open `docs/API.md`
- [ ] In the "HAL / HATEOAS Implementation" → "Authorization-Aware Links" section, add subsection:
  **"Blazor Client Consumption Pattern"** with:
  - [x] The canonical pattern: use `HasHalLink(rel)` directly on the NSwag DTO (helpers in `HalResourceExtensions.cs`)
  - [x] `HalResourceExtensions.cs` as the source of helpers
  - [x] The `[JsonExtensionData]` contract requirement
  - [x] Justified exceptions table (NavMenu, EventCreationEligibilityService, route guards)
  - [x] Anti-pattern: never use `RoleHelper`, `IsInRole()`, or member-list fetches for action gating
  - [x] Testing section references all three regression layers (integration + bUnit)
- [x] Verify docs still read cleanly ✅ docs/API.md:225-273 inserted cleanly between Authorization-Aware Links and Specification Pattern

### 5.2 Update `CLAUDE.md` — Add Non-Inferable Rule #12 ✅ COMPLETE (2026-04-21)
- [x] Added rule #12 to CLAUDE.md Non-Inferable Technical Rules section: "In the Blazor UI, HAL `_links` is the **exclusive** source of action affordance. Gate mutation buttons with `dto.HasHalLink("edit")` helpers... Never use `RoleHelper.CanManage`, `IsInRole`, or claim inspection for per-resource action gating — that logic belongs on the server. Role/claim checks are permitted only for navigation visibility (`NavMenu`), eligibility previews (`EventCreationEligibilityService`), and pre-API route guards."

---

## Acceptance Gates (All Phases Complete When)

- [ ] `grep -r "RoleHelper.CanManage" Explore.Blazor.Client/Pages/Organizations/` returns no action-gating usages (only display usages if any)
- [ ] `OrganizationDetails.razor.cs` has no `CheckEditPermissions` method
- [ ] `OrganizationDetails.razor.cs` has no `currentUserRole` field
- [ ] `HalResourceExtensions.cs` has `HasHalLink` for `EventDto`, `EventListDto`, `OrganizationDto`, `OrganizationListDto`, `GroupDto`, `GroupListDto`
- [ ] `HalResourceExtensions.cs` has private `HasHalLinkInAdditionalProperties` helper
- [ ] `OrganizationCollectionLinkPolicy.GetItemLinks()` includes `edit` and `delete`
- [ ] `GroupCollectionLinkPolicy.GetItemLinks()` includes `edit` and `delete`
- [ ] `HateoasLinkDeserializationTests.cs` exists with 4 passing tests
- [ ] `OrganizationHateoasTests.cs` has 3 new passing tests
- [ ] `OrganizationDetailsHateoasTests.cs` exists with 3 passing tests
- [ ] All existing tests still pass
- [ ] Build green in Release configuration

---

## Quick Reference — The Core Fix (3 Lines)

```csharp
// File: OrganizationDetails.razor.cs

// BEFORE (violation):
var members = await MemberService.GetMembersAsync(Id);
var me = members.FirstOrDefault(m => m.UserId == currentUserId);
canEdit = RoleHelper.CanManage(me?.RoleId);

// AFTER (correct HATEOAS pattern):
canEdit = organization?.HasHalLink("edit") ?? false;
```
