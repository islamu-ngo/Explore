# Admin Pages Refactor — Implementation Plan

**Last Updated: 2026-03-22**

---

## Executive Summary

Refactor the administration page UX so it correctly implements two distinct operating modes, and fix two real backend bugs discovered during planning:

| Mode | Description |
|------|-------------|
| **Single-Tenant** | Instance admin = tenant admin of the one auto-created tenant. No concept of control plane. No lock-toggle UI. No self-service tenancy toggle. `/admin/tenant/settings` redirects here. |
| **Multi-Tenant** | Full instance-level control plane. Lock toggles visible. Tenant management is the primary nav item. The instance admin does not auto-join newly created tenants unless they opt in. |

The backend hierarchical settings resolver, deployment mode provider, tenant lifecycle states, and role model are all fully implemented. This refactor is **primarily UI** with three targeted backend fixes.

---

## Current State Analysis

### What Already Works (Do Not Touch)

- Onboarding deployment mode selection (`InstanceOnboarding.razor`) — well implemented
- `CompleteInstanceOnboardingCommandHandler` — auto-creates default tenant + assigns both roles in single-tenant mode. Already transactional.
- `IHierarchicalSettingsResolver` — 5-tier cascade with lock semantics
- `BlockInSingleTenantAttribute` — API-level 404 for single-tenant-hidden endpoints
- `InstanceAdminSettingsLayout.razor` — already has conditional nav per mode
- `InstanceGovernanceSection.razor` — mode-switch dialogs with typed confirmation guard
- `TenantStatusEnum` — `Provisioning`, `Active`, `Suspended`, `Archived`, `Purged`
- `UpdateInstanceGovernanceSettingsCommandHandler` — already has Multi→Single check AND runs in a transaction. The check just has two bugs (see below).

### Confirmed Bugs That Must Be Fixed Before Any UI Work

#### Bug #1 — `GetActiveTenantCountAsync` counts all tenants, not only Active ones
**File**: `Explore.Persistence/Repositories/TenantRepository.cs`

```csharp
// CURRENT (wrong):
public async Task<int> GetActiveTenantCountAsync()
{
    return await _dbContext.Tenants.AsNoTracking().CountAsync();
}

// SHOULD BE:
return await _dbContext.Tenants
    .AsNoTracking()
    .CountAsync(t => t.TenantStatus != null && t.TenantStatus.IsActiveState);
```

This means the guard "you have N active tenants" is counting Archived and Suspended tenants. A tenant admin who has archived their test tenants would be blocked from reverting even though they are legitimately at 1 active tenant.

**Canonical definition**: Active = `TenantStatus.IsActiveState == true`. This definition must be used everywhere without duplication (UI warning count, command handler guard, query handler, tests).

#### Bug #2 — Active tenant count check executes outside the transaction
**File**: `Explore.Application/Features/InstanceOnboarding/Handlers/Commands/UpdateInstanceGovernanceSettingsCommandHandler.cs`

```csharp
// Lines 78-91 (outside transaction):
if (string.Equals(currentMode, "MultiTenant", ...) && deploymentMode == SingleTenant)
{
    var tenantCount = await _tenantRepository.GetActiveTenantCountAsync(); // ← outside tx!
    if (tenantCount > 1) { return failure; }
}

// Line 99 — transaction starts AFTER:
var bootstrapId = await _unitOfWork.ExecuteInTransactionAsync(async ct => { ... });
```

Under concurrent admin operations, another admin could activate a second tenant between the check and the commit. The count check must be moved **inside** `ExecuteInTransactionAsync`.

#### Missing Coverage — No Tests for Multi→Single Guard
**File**: `Event.Application.UnitTests/Features/InstanceOnboarding/Commands/UpdateInstanceGovernanceSettingsCommandHandlerTests.cs`

The existing tests cover: authorization, valid settings, render policy validation. Zero tests cover the tenant-count guard. This is the most important invariant and has no test coverage.

### Current UI Gaps

1. **Lock toggles still visible in single-tenant mode** — `InstanceStorageSection`, `InstanceSmtpSection`, `InstanceAnalyticsPrivacySection` all pass `IsSingleTenant` but do not conditionally hide the lock UI.
2. **Self-service tenant registration toggle shows in single-tenant governance** — meaningless in that context.
3. **`/admin/tenant/settings` is accessible in single-tenant mode** — creates duplicate/confusing admin surface.
4. **Force-reload is unconditional** — currently every governance save could trigger a reload; it must only fire when deployment mode actually changed.
5. **Multi-tenant nav buries "Tenants"** — control plane's primary action is not visually primary.
6. **No "assign me as tenant admin" option on tenant creation**.
7. **Nav is a growing Razor `if/else` forest** — needs data-driven extraction.
8. **Section state not normalized after mode switch** — e.g., being on "tenants" then switching to single-tenant should normalize to "governance", not leave an invalid state.
9. **Danger-zone UX relies only on icon color** — structural changes need strong explanatory copy.

---

## Proposed Future State

### Single-Tenant Mode: Merged Administration Surface
```
/admin/instance/settings   (title: "Administration")
├── General                ← governance (NO lock toggles, NO self-service toggle)
│   └── includes branding in single-tenant
├── Members                ← tenant members
├── Organizations
├── Lookup Tables
├── Policies               ← tenant policy settings
├── Appearance             ← render mode preset
├── Auth Providers
├── Modules
├── Domain
├── Object Storage         ← NO lock toggle
├── SMTP                   ← NO lock toggle
├── Analytics & Privacy    ← NO lock toggle
├── [Advanced]
│   ├── Render Policies
│   └── Multi-Tenancy      ← upgrade path, warning icon + full explanatory copy
└── [Developer]
    ├── Access Tokens
    └── Webhooks

/admin/tenant/settings     ← REDIRECTS to /admin/instance/settings in single-tenant
```

### Multi-Tenant Mode: Instance Control Plane
```
/admin/instance/settings   (title: "Instance Administration")
├── Governance             ← full: self-service toggle, lock controls
├── Tenants                ← PRIMARY nav item (second position), most prominent
├── Domain
├── Branding
├── Auth Providers
├── Modules
├── Object Storage         ← WITH lock toggle
├── SMTP                   ← WITH lock toggle
├── Analytics & Privacy    ← WITH lock toggle
└── [Advanced]
    └── Render Policies

/admin/tenant/settings     ← accessible separately for tenant admins
```

---

## Implementation Phases — Ordered by Risk and Value

### Phase 1: Backend Bug Fixes (Non-Negotiable First)

These must be done and tested before any UI work. They are the authoritative invariants.

#### Task 1.1 — Fix `GetActiveTenantCountAsync` Status Filter (Bug Fix)
**File**: `Explore.Persistence/Repositories/TenantRepository.cs`

Replace the `CountAsync()` call with a filter that matches only `TenantStatus.IsActiveState == true`.

**Design constraint**: This is the **canonical definition** of "active tenant count". Every other place that counts active tenants (UI warning badge, query handler, command handler) must use this same method or produce the same result — no parallel filters.

**Acceptance Criteria**:
- [ ] `GetActiveTenantCountAsync` uses `TenantStatus.IsActiveState == true` filter
- [ ] Archived, Suspended, Provisioning, Purged tenants are excluded
- [ ] Build passes

**Effort**: XS
**Skills**: `dotnet-efcore-guidelines`

#### Task 1.2 — Move Count Check Inside Transaction (Race Condition Fix)
**File**: `Explore.Application/Features/InstanceOnboarding/Handlers/Commands/UpdateInstanceGovernanceSettingsCommandHandler.cs`

Move the `GetActiveTenantCountAsync` call and the early-return logic from the pre-transaction section into the `ExecuteInTransactionAsync` lambda, before the settings persist call. The validation and the mode update now execute atomically.

**Acceptance Criteria**:
- [ ] Count check executes inside `ExecuteInTransactionAsync`
- [ ] Handler returns failure if count > 1 before any settings are written
- [ ] Transaction rollback occurs if count check fails
- [ ] Existing passing tests still pass

**Effort**: S
**Skills**: `cqrs-mediatr-guidelines`, `dotnet-efcore-guidelines`

#### Task 1.3 — Add Structured Failure Code for Mode-Switch Block
**File**: `Explore.Application/Responses/BaseCommandResponse.cs` (or create a new `FailureCode` enum/const near `BaseCommandResponse`)
**Related file**: `UpdateInstanceGovernanceSettingsCommandHandler.cs`

Return a machine-readable failure code alongside the human message so UI and future API consumers can branch on it without string-matching.

Pattern: add a nullable `string? FailureCode` property to `BaseCommandResponse<T>` (no breaking changes — it's nullable). Set it to `"DeploymentModeChangeBlockedByActiveTenants"` when the guard fires.

**Acceptance Criteria**:
- [ ] `BaseCommandResponse<T>` has nullable `string? FailureCode` property
- [ ] Handler sets `FailureCode = "DeploymentModeChangeBlockedByActiveTenants"` on Multi→Single count failure
- [ ] UI can check `FailureCode` to show a specific action-oriented error (e.g., "Go to Tenants to archive extras")
- [ ] All existing serialization passes (nullable field with default null)

**Effort**: S

#### Task 1.4 — Add Missing Tests for Multi→Single Guard
**File**: `Event.Application.UnitTests/Features/InstanceOnboarding/Commands/UpdateInstanceGovernanceSettingsCommandHandlerTests.cs`

**Acceptance Criteria**:
- [ ] Test: `MultiTenant → SingleTenant` with 3 active tenants → `Success=false`, `FailureCode = "DeploymentModeChangeBlockedByActiveTenants"`
- [ ] Test: `MultiTenant → SingleTenant` with 1 active tenant → `Success=true`
- [ ] Test: `MultiTenant → SingleTenant` with 0 active tenants → `Success=true`
- [ ] Test: `SingleTenant → MultiTenant` → no count check called, `Success=true`
- [ ] Test: `SingleTenant → SingleTenant` (no change) → no count check called

**Effort**: S

---

### Phase 2: Backend — Tenant Creation Side Effect

#### Task 2.1 — Add `AssignCurrentUserAsTenantAdmin` to `CreateTenantCommand`
**File**: `Explore.Application/Features/Tenants/Requests/Commands/CreateTenantCommand.cs`

Add `bool AssignCurrentUserAsTenantAdmin { get; init; }` and `Guid? RequestingUserId { get; init; }` to the command.

**File**: Locate the `CreateTenantCommandHandler.cs` (may be in `Explore.Application/Features/Tenants/Handlers/Commands/` or routed through `TenantController`). If no handler exists, create it following the CQRS pattern established by other tenant-adjacent handlers.

After tenant creation, if `AssignCurrentUserAsTenantAdmin && RequestingUserId.HasValue`:
1. Resolve `tenant.admin` role via `IRoleRepository.GetByMasterCodeAsync("tenant.admin")`
2. Check for existing `TenantMember` (idempotency)
3. Create `TenantMember` in the same transaction

**Acceptance Criteria**:
- [ ] `AssignCurrentUserAsTenantAdmin=true` → `TenantMember` created in same transaction
- [ ] `AssignCurrentUserAsTenantAdmin=false` → no `TenantMember`
- [ ] Already-a-member → idempotent, no duplicate
- [ ] Role resolution fails → tenant creation succeeds, no assignment (logged as warning) — do not roll back tenant creation for an optional side effect
- [ ] Unit tests for all branches
- [ ] Only instance admins can call this endpoint (authorization already enforced by `[AuthorizeResource]`)

**Effort**: M
**Skills**: `cqrs-mediatr-guidelines`, `clean-architecture-rules`

---

### Phase 3: UI — Single-Tenant Simplification

#### Task 3.1 — Redirect `/admin/tenant/settings` in Single-Tenant Mode
**File**: `Explore.Blazor.Client/Pages/Admin/Tenant/TenantAdminSettings.razor`

In `OnInitializedAsync`:
1. Inject `IInstanceOnboardingService`
2. Call `GetDeploymentModeAsync()` — wrapped in `try/catch`
3. If single-tenant → `Navigation.NavigateTo("/admin/instance/settings", replace: true)` and return
4. On catch → render a small error state, do NOT loop (no redirect on failure)

**Acceptance Criteria**:
- [ ] Single-tenant mode → immediate redirect, no flash of tenant admin content
- [ ] Multi-tenant mode → renders normally
- [ ] If mode resolution throws → shows error message, does not redirect or loop
- [ ] bUnit test added in `Explore.Blazor.Client.Tests/Pages/Admin/` verifying redirect behavior

**Effort**: S

#### Task 3.2 — Remove Lock Toggles in Single-Tenant (Three Components)
**Files**:
- `Explore.Blazor.Client/Pages/Admin/Instance/Components/InstanceStorageSection.razor`
- `Explore.Blazor.Client/Pages/Admin/Instance/Components/InstanceSmtpSection.razor`
- `Explore.Blazor.Client/Pages/Admin/Instance/Components/InstanceAnalyticsPrivacySection.razor`

Read each file. Find the "lock for tenants" toggle rendering. Wrap with `@if (!IsSingleTenant)`.

**Acceptance Criteria**:
- [ ] All three: `IsSingleTenant=true` → no lock toggle in rendered output
- [ ] All three: `IsSingleTenant=false` → lock toggle visible
- [ ] bUnit tests added in `Explore.Blazor.Client.Tests/Pages/Admin/` for each component (can be one test class `InstanceSectionLockToggleTests.cs` covering all three)

**Effort**: S

#### Task 3.3 — Hide Self-Service Tenant Toggle in Single-Tenant Governance
**File**: `Explore.Blazor.Client/Pages/Admin/Instance/Components/InstanceGovernanceSection.razor`

The `AllowTenantSelfServiceRegistration` `MudSwitch` must not render when in single-tenant mode.

**Acceptance Criteria**:
- [ ] Single-tenant mode (`DeploymentMode="SingleTenant"`) → self-service switch not in markup
- [ ] Multi-tenant mode → switch renders
- [ ] Add test in `InstanceGovernanceSectionTests.cs` covering both branches

**Effort**: XS

---

### Phase 4: UI — Control Plane Improvements

#### Task 4.1 — Promote "Tenants" and Add Render Policies in Multi-Tenant Nav
**File**: `Explore.Blazor.Client/Pages/Admin/Instance/Components/InstanceAdminSettingsLayout.razor`

**Nav extraction**: Before editing, extract the nav list construction into a private method `BuildNavItems(bool isSingleTenant)` that returns a `List<NavItem>` (define a private record). The template iterates the list. This makes future reordering a one-line change and eliminates the growing Razor if/else forest.

Nav item record:
```csharp
private sealed record NavItem(string Key, string Icon, string Label, Color IconColor = Color.Default, string? Group = null);
```

Single-tenant nav groups: `null` (default), `"Advanced"`, `"Developer"`.
Multi-tenant nav groups: `null` (default), `"Advanced"`.

Multi-tenant order: `governance, tenants, domain, branding, auth-providers, modules, storage, smtp, analytics-privacy, [Advanced] render-policies`.
Single-tenant order: (current order preserved), `[Advanced] render-policies, multi-tenancy`, `[Developer] access-tokens, webhooks`.

"Multi-Tenancy" nav item: `IconColor = Color.Warning`, label stays "Multi-Tenancy".

**Acceptance Criteria**:
- [ ] Nav is built from a list, not inline Razor branches
- [ ] Multi-tenant "Tenants" is second nav item (after Governance)
- [ ] Multi-tenant has Render Policies under Advanced
- [ ] Single-tenant Multi-Tenancy item has warning icon color
- [ ] Existing content area sections all still render correctly
- [ ] Add multi-tenant `render-policies` content case to the content area switch

**Effort**: M

#### Task 4.2 — Add "Assign Me as Tenant Admin" to Tenant Creation UI
**File**: `Explore.Blazor.Client/Pages/Admin/Instance/Components/InstanceTenantsSection.razor`

In the Create Tenant dialog:
- Add `MudCheckBox` with label "Assign me as administrator of this tenant"
- Bind to `_assignSelfAsTenantAdmin` bool field (default: `false`)
- Pass to `CreateTenantDto` or request field when calling the API

**Acceptance Criteria**:
- [ ] Checkbox present in create dialog
- [ ] Default: unchecked
- [ ] When checked: request carries `AssignCurrentUserAsTenantAdmin=true`
- [ ] On success, snackbar mentions admin assignment if opted in

**Depends on**: Task 2.1
**Effort**: S

#### Task 4.3 — Conditional Force-Reload After Mode Switch (Not Always)
**File**: `Explore.Blazor.Client/Pages/Admin/Instance/Components/InstanceAdminSettingsLayout.razor`

The reload must only trigger when the deployment mode value actually changed between load and save.

Approach:
1. Track `_loadedDeploymentMode` (set once in `OnInitializedAsync`)
2. In `SaveAsync`, after successful save of any section, compare `_deploymentMode` vs `_loadedDeploymentMode`
3. If changed: `Snackbar.Add("Mode changed. Reloading...", Severity.Info)` then `await Task.Delay(800)` then `Navigation.NavigateTo(Navigation.Uri, forceLoad: true)`
4. If not changed: normal "Settings saved." snackbar only

**Acceptance Criteria**:
- [ ] Mode unchanged: no reload, snackbar says "Settings saved."
- [ ] Mode changed: snackbar, delay, full reload
- [ ] After reload, nav structure reflects new mode
- [ ] `BffAdminClaimsTransformation` runs on reload (claims refresh automatic on force load)

**Effort**: S

#### Task 4.4 — Normalize Section State After Mode Switch
**File**: `Explore.Blazor.Client/Pages/Admin/Instance/Components/InstanceAdminSettingsLayout.razor`

After a mode switch (detected before reload triggers), normalize `_currentSection` to a valid section for the new mode.

Single-tenant → Multi-tenant invalid sections (redirect to `"governance"`):
- `"members"`, `"organizations"`, `"lookups"`, `"policies"`, `"appearance"`, `"access-tokens"`, `"webhooks"`, `"render-policies"`, `"multi-tenancy"`

Multi-tenant → Single-tenant invalid sections (redirect to `"governance"`):
- `"tenants"`

This already partially exists in `OnDeploymentModeChanged` — consolidate the logic into one canonical `NormalizeSection(string section, bool isSingleTenant)` method.

**Acceptance Criteria**:
- [ ] User on "tenants", switches to single-tenant, reloads → lands on "governance"
- [ ] User on "members", switches to multi-tenant, reloads → lands on "governance"
- [ ] Valid sections are not affected

**Effort**: XS

#### Task 4.5 — Strengthen Danger-Zone UX for Multi-Tenancy Section
**File**: `Explore.Blazor.Client/Pages/Admin/Instance/Components/InstanceGovernanceSection.razor`

When `DisplayMode == "multi-tenancy"`, add strong explanatory copy **inside the section content** (not just nav styling):
- A `MudAlert Severity="Warning"` header explaining what changes structurally
- Bullet points: settings split into global/per-tenant, lock controls appear, tenant onboarding becomes separate, tenants get their own admin panel
- The typed confirmation ("ENABLE MULTI-TENANCY") remains as the actual safeguard
- For multi→single: similarly explain that tenant panels merge back, lock controls disappear

**Acceptance Criteria**:
- [ ] Warning alert visible when `DisplayMode == "multi-tenancy"` and in single-tenant mode
- [ ] Bullet list of structural changes rendered
- [ ] Does not appear in other display modes

**Effort**: S

---

### Phase 5: Testing Completion

#### Task 5.1 — bUnit: Lock Toggle Absence in Single-Tenant Mode
**File**: `Explore.Blazor.Client.Tests/Pages/Admin/InstanceSectionLockToggleTests.cs` (create)

Three component tests (one class, three test methods):
- `StorageSection_SingleTenant_DoesNotRenderLockToggle`
- `SmtpSection_SingleTenant_DoesNotRenderLockToggle`
- `AnalyticsSection_SingleTenant_DoesNotRenderLockToggle`

Follow the pattern in `InstanceGovernanceSectionTests.cs` (use `BlazorTestContext`, `DynamicComponent`).

**Effort**: S

#### Task 5.2 — bUnit: Governance Self-Service Toggle Visibility
**File**: `Explore.Blazor.Client.Tests/Pages/Admin/InstanceGovernanceSectionTests.cs` (add tests)

- `GovernanceSection_SingleTenant_DoesNotRenderSelfServiceRegistrationToggle`
- `GovernanceSection_MultiTenant_RendersSelfServiceRegistrationToggle`

**Effort**: XS

#### Task 5.3 — bUnit: Tenant Admin Page Redirect in Single-Tenant Mode
**File**: `Explore.Blazor.Client.Tests/Pages/Admin/TenantAdminSettingsRedirectTests.cs` (create)

- `TenantAdminSettings_SingleTenantMode_RedirectsToInstanceSettings`
- `TenantAdminSettings_MultiTenantMode_DoesNotRedirect`
- `TenantAdminSettings_WhenModeResolutionFails_ShowsErrorNotRedirect`

Mock `IInstanceOnboardingService.GetDeploymentModeAsync`.

**Effort**: S

#### Task 5.4 — Application Unit Tests: Tenant Creation Role Assignment
**File**: `Event.Application.UnitTests/Features/Tenants/Commands/CreateTenantCommandHandlerTests.cs` (create)

- `CreateTenant_WithAssignSelf_CreatesTenantMember`
- `CreateTenant_WithoutAssignSelf_DoesNotCreateTenantMember`
- `CreateTenant_WithAssignSelf_AlreadyMember_IsIdempotent`
- `CreateTenant_WithAssignSelf_RoleNotFound_TenantCreatedWithoutMember_NoError`

**Effort**: S

#### Task 5.5 — Final Build and Full Test Run
```bash
dotnet build --configuration Release --verbosity quiet
dotnet test --project Event.Application.UnitTests/Event.Application.UnitTests.csproj --configuration Release --verbosity quiet
dotnet test --project Event.Domain.UnitTests/Event.Domain.UnitTests.csproj --configuration Release --verbosity quiet
dotnet test --project Event.Architecture.Tests/Event.Architecture.Tests.csproj --configuration Release --verbosity quiet
dotnet test --project Explore.Blazor.Client.Tests/Explore.Blazor.Client.Tests.csproj --configuration Release --verbosity quiet
```

---

## Watch Items (Not Blocking, Track During Implementation)

### Claims Refresh After Tenant Admin Assignment
If the instance admin creates a tenant and opts in as tenant admin, their session principal may not reflect the new `TenantMember` role until the next login/refresh. Assess whether `BffAdminClaimsTransformation` re-runs on the next request or only on login. If it re-runs on the next full page load (force-load after creation), claims will be fresh. If not, a targeted `IAdminCacheInvalidator.InvalidateUser(userId)` call may be needed in the UI service layer.

### Default Tenant ID in UI vs Backend
Do not scatter `018e4e5c-7f00-7000-8000-000000000001` into UI logic. Tenant resolution belongs to the API. If the UI needs to reference the default tenant, it must do so through a typed service response, not a hardcoded GUID.

### Cache Invalidation After Mode Switch
`IDeploymentModeProvider` has `InvalidateCacheAsync()`. Verify it is called in `UpdateInstanceGovernanceSettingsCommandHandler` after a mode change (it is currently called in `CompleteInstanceOnboardingCommandHandler` but not in the governance update handler). Add it if missing.

---

## Risk Assessment

| Risk | Likelihood | Impact | Mitigation |
|------|-----------|--------|------------|
| `GetActiveTenantCountAsync` fix breaks existing tests | High | Medium | Update test mocks to return correct count type; check all callers |
| Force-reload `Task.Delay(800)` drops snackbar before display | Medium | Low | Acceptable UX trade-off; snackbar duration > delay |
| Section normalization misses an edge-case section key | Low | Low | `NormalizeSection` defaults to `"governance"` for any unknown key |
| Claims not refreshed after mode switch (stale nav) | Medium | Medium | Force-load handles this; document watch item |
| Nav data-driven refactor introduces a regression | Medium | Medium | bUnit nav item tests catch this |
| Tenant creation handler doesn't exist yet | Unknown | High | Verify in TenantController before implementing; may be inline in controller |

---

## Success Metrics (All Must Pass Before Merge)

1. **Single-tenant mode**: Zero lock toggle UI in any section
2. **Single-tenant mode**: `/admin/tenant/settings` always redirects, never shows tenant admin content
3. **Single-tenant mode**: No self-service tenant registration toggle visible
4. **Multi-tenant mode**: "Tenants" is the second nav item, prominently accessible
5. **Multi-tenant mode**: Lock toggles visible and functional with save wiring
6. **Multi-tenant mode**: Render Policies accessible from nav
7. **Mode switch**: Full page reload fires ONLY when mode actually changed
8. **Backend invariant**: Multi→Single blocked when >1 active tenant — enforced inside transaction, tested
9. **Active tenant definition**: Canonical — `IsActiveState == true`, used everywhere, not duplicated
10. **All tests pass** — `Event.Application.UnitTests`, `Explore.Blazor.Client.Tests`, `Event.Domain.UnitTests`, `Event.Architecture.Tests`
