# HATEOAS Client Alignment — Task Checklist

> Last Updated: 2026-03-27

---

## Phase 1: API Layer — Collection Policy Gaps ⏳ NOT STARTED

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

## Phase 2: HalResourceExtensions — Refactor & Expand ⏳ NOT STARTED

### 2.1 Extract `HasHalLinkInAdditionalProperties` Private Helper
- [ ] Open `Explore.Blazor.Client/Helpers/HalResourceExtensions.cs`
- [ ] Add `private static bool HasHalLinkInAdditionalProperties(IDictionary<string, object>? additionalProperties, string linkRel)` at the bottom of the `// ========== Helper Methods ==========` section
  - Implementation: check `additionalProperties?.TryGetValue("_links", out var linksObj)` → cast to `JsonElement` → `linksElement.TryGetProperty(linkRel, out _)`
- [ ] Refactor `HasHalLink(this EventDto dto, string linkRel)` to delegate to this helper
- [ ] Refactor `HasHalLink(this EventListDto dto, string linkRel)` to delegate to this helper
- [ ] Run all tests — no behavioral change

### 2.2 Add `HasHalLink` for `OrganizationDto` and `OrganizationListDto`
- [ ] In `HalResourceExtensions.cs`, in the `// ========== Organization Extensions ==========` section, after `ToDto()`:
  - [ ] Add `public static bool HasHalLink(this OrganizationDto dto, string linkRel)` — delegates to `HasHalLinkInAdditionalProperties(dto.AdditionalProperties, linkRel)`
  - [ ] Add `public static bool HasHalLink(this OrganizationListDto dto, string linkRel)` — same
- [ ] Run build — no errors

### 2.3 Add `HasHalLink` for `GroupDto` and `GroupListDto`
- [ ] In `HalResourceExtensions.cs`, add new `// ========== Group Extensions ==========` section
  - [ ] Add `HasHalLink(this GroupDto dto, string linkRel)` delegating to shared helper
  - [ ] Add `HasHalLink(this GroupListDto dto, string linkRel)` delegating to shared helper
  - [ ] (Optional) Add `ToDto()` for `HalResourceOfGroupDto` if not already present
- [ ] Run build — no errors

### 2.4 Add `HasManagementLinks` for `OrganizationListDto` and `GroupListDto`
- [ ] In the `// ========== Organization Extensions ==========` section:
  - [ ] Add `public static bool HasManagementLinks(this OrganizationListDto dto)` → `dto.HasHalLink("edit") || dto.HasHalLink("delete")`
- [ ] In the `// ========== Group Extensions ==========` section:
  - [ ] Add `public static bool HasManagementLinks(this GroupListDto dto)` → same

---

## Phase 3: OrganizationDetails Fix ⏳ NOT STARTED

### 3.1 Remove `CheckEditPermissions()` and Replace with HAL Link Read
- [ ] Open `Explore.Blazor.Client/Pages/Organizations/OrganizationDetails.razor.cs`
- [ ] Verify `GetMembersAsync()` is NOT used elsewhere in this file for non-auth purposes
  - If member data is shown in the UI, check if a separate member-loading path exists or needs to be added back
- [ ] Delete `private int? currentUserRole` field
- [ ] Delete `private async Task CheckEditPermissions()` method
- [ ] In `LoadOrganization()`, remove the `var permissionsTask = CheckEditPermissions()` line
- [ ] After `organization = await OrganizationService.GetOrganizationByIdAsync(Id)` and null-check:
  - [ ] Set `canEdit = organization.HasHalLink("edit");`
- [ ] Adjust `await Task.WhenAll(...)` if needed — remove `permissionsTask` from the list
- [ ] Verify no remaining references to `currentUserRole` or `CheckEditPermissions` in the file
- [ ] Run build — no errors
- [ ] Run `dotnet test --project Explore.Blazor.Client.Tests/... --configuration Release --verbosity quiet`

### 3.2 Verify `OrganizationService.GetOrganizationByIdAsync` Preserves `_links`
- [ ] Confirm `HalResourceExtensions.cs` has `ToDto(this HalResourceOfOrganizationDto)` using JSON round-trip ✅
- [ ] Confirm `OrganizationDto` has `[JsonExtensionData]` on `AdditionalProperties` in generated client ✅
- [ ] No code change needed — document confirmation in context file

---

## Phase 4: Test Coverage ⏳ NOT STARTED

### 4.1 Add `_links` Deserialization Survival Tests (Regression Guard)
- [ ] Create `Event.API.IntegrationTests/Features/Hateoas/HateoasLinkDeserializationTests.cs`
  - [ ] ABOUTME: two-line header
  - [ ] File-scoped namespace: `namespace Event.Api.IntegrationTests.Features.Hateoas;`
  - [ ] Class: `[ClassDataSource<AuthenticatedApiTestFixture>(Shared = SharedType.PerAssembly)] public class HateoasLinkDeserializationTests`
  - [ ] Test: `EventDto_DeserializedFromHalResource_PreservesEditLink()`
    - Fetch `GET /api/event/{ownedEventId}` with auth → call `ToDto()` → assert `HasHalLink("edit") == true`
  - [ ] Test: `EventListDto_DeserializedFromCollection_PreservesEditLinkForOwnedItem()`
    - Fetch `GET /api/event` with auth → `GetItems()` → find owned item → assert `HasHalLink("edit") == true`
  - [ ] Test: `OrganizationDto_DeserializedFromHalResource_PreservesEditLink()`
    - Fetch `GET /api/organization/{adminOrgId}` with auth as org admin → `ToDto()` → assert `HasHalLink("edit") == true`
  - [ ] Test: `OrganizationListDto_DeserializedFromCollection_PreservesEditLinkForAdminOrg()`
    - Fetch `GET /api/organization` with auth → `GetItems()` → find admin org → assert `HasHalLink("edit") == true`
- [ ] All 4 tests pass green

### 4.2 Extend `OrganizationHateoasTests` for Per-Item Management Links
- [ ] Open `Event.API.IntegrationTests/Features/Hateoas/OrganizationHateoasTests.cs`
- [ ] Add: `GetOrganizations_AsOrgAdmin_EmbeddedItemsShouldIncludeEditLink()`
  - Assert embedded item for admin's org has `_links.edit` present
- [ ] Add: `GetOrganizations_Anonymous_EmbeddedItemsShouldNotIncludeManagementLinks()`
  - Assert no `_links.edit` or `_links.delete` in embedded items for anonymous request
- [ ] Add: `GetOrganizations_AsOrgAdmin_EmbeddedItemsShouldIncludeDeleteLink()`
- [ ] All 3 tests pass green

### 4.3 Add Blazor Component Tests for `OrganizationDetails` HATEOAS Consumption
- [ ] Check `Explore.Blazor.Client.Tests` for existing bUnit setup and patterns
- [ ] Create `Explore.Blazor.Client.Tests/Pages/Organizations/OrganizationDetailsHateoasTests.cs`
  - [ ] ABOUTME header
  - [ ] Test: `WhenApiReturnsEditLink_ShowsEditButton()` — mock `IOrganizationService.GetOrganizationByIdAsync()` returning `OrganizationDto` with `AdditionalProperties["_links"]["edit"]` present → render component → assert Edit button visible
  - [ ] Test: `WhenApiDoesNotReturnEditLink_HidesEditButton()` — mock returns org without `edit` link → assert Edit button absent
  - [ ] Test: `DoesNotCallGetMembersAsync_OnPageLoad()` — mock `IOrganizationMemberService.GetMembersAsync()` → render component → assert method was never called
- [ ] All 3 tests pass green

---

## Phase 5: Documentation ⏳ NOT STARTED

### 5.1 Update `docs/API.md` — Add Blazor Client Consumption Pattern
- [ ] Open `docs/API.md`
- [ ] In the "HAL / HATEOAS Implementation" → "Authorization-Aware Links" section, add subsection:
  **"Blazor Client Consumption Pattern"** with:
  - The canonical pattern: use `HasHalLink(rel)` after `ToDto()` or `GetItems()`
  - `HalResourceExtensions.cs` as the source of helpers
  - The `[JsonExtensionData]` contract requirement
  - Justified exceptions table (NavMenu, EventCreationEligibilityService, route guards)
  - Anti-pattern: never use `RoleHelper`, `IsInRole()`, or member-list fetches for action gating
- [ ] Verify docs still read cleanly

### 5.2 Update `CLAUDE.md` — Add Non-Inferable Rule #12
- [ ] Open `CLAUDE.md`
- [ ] In "Non-Inferable Technical Rules", add:
  > **12.** Blazor UI components derive action affordance (edit/delete/create button visibility) exclusively from HAL `_links` returned by the API (`HalResourceExtensions.HasHalLink()`). Never use `RoleHelper`, `IsInRole()`, or member-list fetches to derive action permissions. Exceptions: NavMenu admin claims (nav structure, not resource actions), `EventCreationEligibilityService` (multi-resource eligibility), page-level `[Authorize]` route guards.

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
