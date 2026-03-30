# HATEOAS Client Alignment — Context

> Last Updated: 2026-03-27

---

## SESSION PROGRESS (2026-03-27)

### ✅ COMPLETED
- Full audit of API-side HATEOAS (all 22 assemblers, 23 link policies, HateoasAuthorizationEvaluator)
- Full audit of Blazor client HATEOAS consumption (EventDetail, EventList, OrganizationDetails, NavMenu, HalResourceExtensions)
- Identified all violations and gaps
- Created implementation plan, context, and tasks files

### 🟡 IN PROGRESS
- Nothing yet — planning phase complete, implementation not started

### ⚠️ BLOCKERS
- None

---

## What This Work Is About

A HATEOAS + Cerbos/Fallback authorization audit found that the API layer is correctly implemented but the Blazor client has one concrete violation and two secondary issues:

1. **Primary violation**: `OrganizationDetails.razor.cs` uses `RoleHelper.CanManage(currentUserRole)` instead of reading the `edit` HAL link from the API response
2. **Fragility risk**: `HalResourceExtensions.DeserializeItems<T>()` has no test guard for `[JsonExtensionData]` survival
3. **Policy gap**: `OrganizationCollectionLinkPolicy` and `GroupCollectionLinkPolicy` don't return `edit`/`delete` per-item links in `GetItemLinks()`
4. **Missing helpers**: `HasHalLink()` only exists for `EventDto`/`EventListDto`, not for `OrganizationDto`, `GroupDto`, etc.

---

## Key Files

### API Layer (Correct — Read for Pattern Reference)

**`Explore.API/Hateoas/HateoasAuthorizationEvaluator.cs`**
- Batch evaluates all link permissions in one `IAuthorizationProvider.IsAllowedBatchAsync()` call
- Static checks (RequiresAuth, RequiredRoles, Condition) run first without I/O
- Fail-closed: on provider failure, all permission-bound links denied

**`Explore.API/Hateoas/ResourceAssemblerBase.cs`**
- Base class for all 22 assemblers
- `BuildListResourcesWithBatch()` flattens ALL items' link definitions into a single batch
- `IsMinimalResponse()` checks `Prefer: return=minimal` header

**`Explore.API/Hateoas/Policies/EventLinkPolicy.cs`**
- **Gold standard pattern** for both detail and collection link policies
- `EventCollectionLinkPolicy.GetItemLinks()` returns `edit`/`delete` per-item with `RequirePermission()`
- All write-action links use `.RequirePermission(PermissionAction.X, dto, id, attrs)` where attrs include `tenantId`/`actorId`

**`Explore.API/Hateoas/Policies/OrganizationLinkPolicy.cs`**
- `OrganizationDetailLinkPolicy` has correct `edit`/`delete` with `RequirePermission()`
- `OrganizationCollectionLinkPolicy.GetItemLinks()` is **MISSING** `edit`/`delete` — fix in Task 1.1

**`Explore.API/Hateoas/Policies/GroupLinkPolicy.cs`**
- `GroupDetailLinkPolicy` has correct `edit`/`delete` with `RequirePermission()`
- `GroupCollectionLinkPolicy.GetItemLinks()` is **MISSING** `edit`/`delete` — fix in Task 1.2

**`Explore.API/Hateoas/LinkDefinitionPermissionExtensions.cs`**
- `RequirePermission<TResource>(this LinkDefinition, PermissionAction, TResource, resourceId?, attrs?)` — uses `ResourceDescriptorRegistry.ResolveResourceKind(typeof(TResource))`
- Overload: `RequirePermission(this LinkDefinition, PermissionAction, Type, resourceId?, attrs?)` — for collection-level links with no resource instance

**`Explore.Application/Authorization/ResourceDescriptorRegistry.cs`**
- Maps DTO types → resource kind strings ("organization", "group", "event", etc.)
- `OrganizationListDto` → `"organization"` (for collection item edit/delete links)
- `GroupListDto` → `"group"`

**`Explore.Application/Hateoas/LinkDefinition.cs`**
- Record with `Rel`, `RouteName`, `RouteValues`, `Method`, `Title`, `RequiresAuth`, `RequiredRoles`, `Condition`, `PermissionResourceKind`, `PermissionAction`, `PermissionResourceId`, `PermissionResourceAttributes`
- Factory methods: `LinkDefinition.Edit(routeName, routeValues?)`, `LinkDefinition.Delete(...)`, `LinkDefinition.Create(...)`

### Blazor Client Layer

**`Explore.Blazor.Client/Helpers/HalResourceExtensions.cs`**
- `HasHalLink(this EventDto dto, string linkRel)` — checks `dto.AdditionalProperties["_links"][linkRel]`
- `HasHalLink(this EventListDto dto, string linkRel)` — same pattern
- `HasManagementLinks(this EventListDto dto)` — checks `edit || delete`
- **MISSING**: same helpers for `OrganizationDto`, `OrganizationListDto`, `GroupDto`, `GroupListDto`
- `ToDto()` extension: `HalResourceOfEventDto → JSON → EventDto` — preserves `_links` via `[JsonExtensionData]`
- `ToDto()` extension: `HalResourceOfOrganizationDto → JSON → OrganizationDto` — same, **already correct**
- `DeserializeItems<T>()` — handles `ICollection<object>` from NSwag, deserializes with `JsonElement` path

**`Explore.Blazor.Client/Pages/Events/EventDetail.razor.cs:324`**
- **CORRECT** pattern: `_canEdit = _eventDetails.HasHalLink("edit")`
- `CheckAuthorizationFromHalLinks()` — pure HATEOAS, no extra API calls

**`Explore.Blazor.Client/Pages/Organizations/OrganizationDetails.razor.cs:103`**
- **VIOLATION**: `CheckEditPermissions()` fetches members, uses `RoleHelper.CanManage()`
- Fix: delete `CheckEditPermissions()`, set `canEdit = organization?.HasHalLink("edit") ?? false`

**`Explore.Blazor.Client/Layout/NavMenu.razor.cs:179`**
- `HasAnyAdminAuthority()`, `IsInstanceAdmin()`, `IsTenantAdmin()` use admin claims
- **JUSTIFIED EXCEPTION**: Claims set server-side by `BffAdminClaimsTransformation` via `api/User/admin-authority`
- Navigation structure visibility ≠ resource action authorization — do not touch

**`Explore.Blazor.Client/Services/EventCreationEligibilityService.cs`**
- **JUSTIFIED EXCEPTION**: Multi-resource cross-concern (tenant policy + org/group membership)
- Do not touch

### Generated Client

**`Explore.Blazor.Client/Clients/EventApiClient.g.cs`**
- `EventDto` (line 62122): has `[JsonExtensionData]` on `AdditionalProperties` ✅
- `EventListDto` (line 62452): has `[JsonExtensionData]` on `AdditionalProperties` ✅
- `OrganizationDto`: has `[JsonExtensionData]` on `AdditionalProperties` ✅ (verified in audit)
- `OrganizationListDto`: has `[JsonExtensionData]` on `AdditionalProperties` ✅

### Integration Tests (Existing)

**`Event.API.IntegrationTests/Features/Hateoas/HateoasAuthorizationIntegrationTests.cs`**
- Tests anonymous vs. authenticated link presence for collection endpoints
- Pattern to follow for new tests

**`Event.API.IntegrationTests/Features/Hateoas/OrganizationHateoasTests.cs`**
- Existing org HATEOAS tests — extend in Task 4.2

---

## Key Decisions

### Decision 1: Extract Private Helper in HalResourceExtensions
Instead of copy-pasting the `HasHalLink` implementation 10+ times, extract a private static `HasHalLinkInAdditionalProperties(IDictionary<string,object>?, string)` and have all per-type extensions delegate. This keeps public API per-type (for discoverability) but eliminates logic duplication.

### Decision 2: Typed Extensions Per DTO, Not Generic
A generic `HasHalLink<T>(this T dto, string rel) where T : ???` would require an interface constraint that all generated DTOs implement. NSwag-generated types are `partial` but we cannot add interface implementations without modifying the NSwag template or using a source generator. Typed per-DTO extensions are simpler and explicit.

### Decision 3: Delete `CheckEditPermissions()` Entirely
The method's sole purpose was to derive `canEdit`. After the fix, there is no use for `currentUserRole` either. Delete both rather than leaving dead code. The `GetMembersAsync()` call may still be needed if other parts of the page use member data — verify at implementation time.

### Decision 4: Collection Policy Per-Item Links — ABAC Attributes
For `OrganizationListDto`, the available attributes are `organizationId` (the resource id) and `tenantId`. No `actorId` is available on the list DTO (unlike EventListDto which has `actorId`). Confirm the `OrganizationListDto` properties before writing the policy code.

---

## Technical Constraints

1. **CLAUDE.md Rule #1**: No backwards-compat code — delete `CheckEditPermissions()` and `currentUserRole` in one change
2. **CLAUDE.md Rule #5**: No default values in domain entities — not applicable here (Blazor layer only)
3. **CLAUDE.md Rule #9**: File-scoped namespaces for any new C# files
4. **CLAUDE.md ABOUTME rule**: All new files must start with two-line `ABOUTME:` summary
5. **NSwag generated file**: Never modify `EventApiClient.g.cs` directly — it is regenerated on build
6. **TDD**: Write failing test first, implement, verify green — required by project standards
7. **Integration tests**: Must use `AuthenticatedApiTestFixture` pattern, not mocks

---

## Quick Resume Instructions

To resume this work:

1. Read this file for current state
2. Read `hateoas-client-alignment-tasks.md` to see what's done/next
3. Start with **Phase 1** (API collection policies) — quickest win, no dependencies
4. Then **Phase 2** (HalResourceExtensions refactor) — required before Phase 3
5. Then **Phase 3** (OrganizationDetails fix) — the primary violation correction
6. Then **Phase 4** (tests) — verify everything works
7. Then **Phase 5** (docs) — close the loop

**To verify the violation yourself**:
```
// OrganizationDetails.razor.cs:121 — THE LINE TO FIX
canEdit = RoleHelper.CanManage(currentUserRole);
// Should become:
canEdit = organization?.HasHalLink("edit") ?? false;
```

**To verify OrganizationDto already preserves _links**:
- `OrganizationService.GetOrganizationByIdAsync()` line 183-184: calls `result?.ToDto()`
- `ToDto()` for `HalResourceOfOrganizationDto` is in `HalResourceExtensions.cs` around line 163-167
- It does `JsonSerializer.Serialize(halResource) → JsonSerializer.Deserialize<OrganizationDto>()` — `_links` lands in `AdditionalProperties`
