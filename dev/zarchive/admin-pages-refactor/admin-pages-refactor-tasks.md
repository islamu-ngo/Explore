# Admin Pages Refactor — Task Checklist

**Last Updated: 2026-03-22**

---

## Phase 1: Backend Bug Fixes ✅ COMPLETE

> These are real bugs. Do these first, before any UI work.

### 1.1 Fix `GetActiveTenantCountAsync` — Add Active Status Filter
**File**: `Explore.Persistence/Repositories/TenantRepository.cs`

- [x] Read the file
- [x] Replace `.CountAsync()` with `.CountAsync(t => t.TenantStatus != null && t.TenantStatus.IsActiveState)`
- [x] Verify the same definition is used everywhere (check all callers — see context file)
- [x] Build passes

**AC**: Archived/Suspended/Provisioning/Purged tenants excluded from count.

---

### 1.2 Move Count Check Inside Transaction
**File**: `Explore.Application/Features/InstanceOnboarding/Handlers/Commands/UpdateInstanceGovernanceSettingsCommandHandler.cs`

- [x] Read the file (lines 78-92 are the guard, line 99 is the transaction start)
- [x] Move the `GetActiveTenantCountAsync` call + early-return logic inside `ExecuteInTransactionAsync`
- [x] Ensure it runs BEFORE `_governanceSettingService.ApplySettingsAsync`
- [x] Verify existing passing tests still pass (mock setup in test file executes the lambda inline)

**AC**: Count check and settings write are atomic. Guard fires from inside the transaction.

---

### 1.3 Add `FailureCode` to `BaseCommandResponse<T>`
**File**: `Explore.Application/Responses/BaseCommandResponse.cs`

- [x] Read the file
- [x] Add `public string? FailureCode { get; set; }` (nullable, no breaking change)
- [x] In `UpdateInstanceGovernanceSettingsCommandHandler`, set `response.FailureCode = "DeploymentModeChangeBlockedByActiveTenants"` when guard fires
- [x] Build passes, no serialization regression

**AC**: `FailureCode` is set on mode-switch block; null everywhere else.

---

### 1.4 Add Missing Guard Tests
**File**: `Event.Application.UnitTests/Features/InstanceOnboarding/Commands/UpdateInstanceGovernanceSettingsCommandHandlerTests.cs`

- [x] Test: MultiTenant → SingleTenant, 3 active tenants → `Success=false`, `FailureCode = "DeploymentModeChangeBlockedByActiveTenants"`
- [x] Test: MultiTenant → SingleTenant, 1 active tenant → `Success=true`
- [x] Test: MultiTenant → SingleTenant, 0 active tenants → `Success=true`
- [x] Test: SingleTenant → MultiTenant → `GetActiveTenantCountAsync` NOT called, `Success=true`
- [x] Test: SingleTenant → SingleTenant (no-change) → `GetActiveTenantCountAsync` NOT called
- [x] Run: `dotnet test --project Event.Application.UnitTests/Event.Application.UnitTests.csproj --configuration Release --verbosity quiet`
- [x] All pass ✓ (510 total, 0 failures)

---

## Phase 2: Backend — Tenant Creation Role Assignment ✅ COMPLETE

### 2.0 Locate CreateTenantCommandHandler
- [x] Read `Explore.API/Controllers/TenantController.cs` — handler was MISSING (dispatched but no handler)
- [x] Created `Explore.Application/Features/Tenants/Handlers/Commands/CreateTenant/CreateTenantCommandHandler.cs`

---

### 2.1 Add `AssignCurrentUserAsTenantAdmin` to Command and Handler
**Files**: `CreateTenantCommand.cs`, `CreateTenantCommandHandler.cs` (new), `TenantController.cs`

- [x] Add `bool AssignCurrentUserAsTenantAdmin` to `CreateTenantDto` (request body field)
- [x] Add `Guid? RequestingUserId { get; init; }` to `CreateTenantCommand`
- [x] Handler: validate DTO, check slug uniqueness, then in transaction:
  - [x] Create Tenant
  - [x] If flag+userId: resolve `tenant.admin` role, check idempotency, create TenantMember
  - [x] If role not found: log warning, do NOT fail the request
- [x] Update `TenantController.cs` Create method to extract `RequestingUserId` from JWT + pass it to command

**AC**:
- [x] Flag=true, user not member → TenantMember created
- [x] Flag=false → no TenantMember
- [x] Flag=true, user already member → no duplicate, success
- [x] Flag=true, role not found → tenant created, warning logged, success returned

---

### 2.2 Unit Tests — CreateTenant Role Assignment
**File**: `Event.Application.UnitTests/Features/Tenants/Commands/CreateTenantCommandHandlerTests.cs` (created)

- [x] Test: `AssignCurrentUserAsTenantAdmin=true` → `TenantMemberRepository.Create` called once
- [x] Test: `AssignCurrentUserAsTenantAdmin=false` → `TenantMemberRepository.Create` NOT called
- [x] Test: already a member → `Create` NOT called, `Success=true`
- [x] Test: role not found → `Create` NOT called, `Success=true`
- [x] Also fixed pre-existing `GetPublicExperienceSettingsQueryHandlerTests` failures (missing `IFooterLinkGroupRepository` + `IMapper` deps)
- [x] Run unit tests — 516 total, 0 failures ✓

---

## Phase 3: UI — Single-Tenant Simplification ✅ COMPLETE

### 3.1 Redirect `/admin/tenant/settings` in Single-Tenant Mode
**File**: `Explore.Blazor.Client/Pages/Admin/Tenant/TenantAdminSettings.razor`

- [x] Read the file
- [x] Inject `IInstanceOnboardingService`
- [x] In `OnInitializedAsync`, call `GetDeploymentModeAsync()` in a `try/catch`
- [x] If `SingleTenant` → `Navigation.NavigateTo("/admin/instance/settings", replace: true)` and return
- [x] If exception → set `_errorMessage`, do NOT redirect, render error state

**AC**:
- [x] Single-tenant → redirect, no flash of tenant content
- [x] Multi-tenant → renders normally
- [x] Exception → error message shown, no loop

---

### 3.2 bUnit: Tenant Admin Redirect Tests
**File**: `Explore.Blazor.Client.Tests/Pages/Admin/TenantAdminSettingsRedirectTests.cs` (create)

- [x] `TenantAdminSettings_SingleTenantMode_RedirectsToInstanceSettings`
- [x] `TenantAdminSettings_MultiTenantMode_DoesNotRedirect`
- [x] `TenantAdminSettings_WhenModeResolutionThrows_ShowsErrorAndDoesNotRedirect`
- [x] Run bUnit tests, all pass ✓ (593 total, 0 failures)

---

### 3.3 Remove Lock Toggles — InstanceStorageSection
**File**: `Explore.Blazor.Client/Pages/Admin/Instance/Components/InstanceStorageSection.razor`

- [x] Read the file
- [x] `@if (!IsSingleTenant)` guard already present — no changes needed

---

### 3.4 Remove Lock Toggles — InstanceSmtpSection
**File**: `Explore.Blazor.Client/Pages/Admin/Instance/Components/InstanceSmtpSection.razor`

- [x] Read the file
- [x] `@if (!IsSingleTenant)` guard already present — no changes needed

---

### 3.5 Remove Lock Toggles — InstanceAnalyticsPrivacySection
**File**: `Explore.Blazor.Client/Pages/Admin/Instance/Components/InstanceAnalyticsPrivacySection.razor`

- [x] Read the file
- [x] `@if (!IsSingleTenant)` guard already present — no changes needed

---

### 3.6 bUnit: Lock Toggle Absence Tests
**File**: `Explore.Blazor.Client.Tests/Pages/Admin/InstanceSectionLockToggleTests.cs` (create)

- [x] `StorageSection_SingleTenant_NoLockToggle`
- [x] `SmtpSection_SingleTenant_NoLockToggle`
- [x] `AnalyticsSection_SingleTenant_NoLockToggle`
- [x] `StorageSection_MultiTenant_HasLockToggle` (regression guard)
- [x] Run bUnit tests, all pass ✓ (593 total, 0 failures)

---

### 3.7 Hide Self-Service Toggle in Governance (Single-Tenant)
**File**: `Explore.Blazor.Client/Pages/Admin/Instance/Components/InstanceGovernanceSection.razor`

- [x] Read the file
- [x] Found `AllowTenantSelfServiceRegistration` `MudSwitch` at line 147
- [x] Wrapped with `@if (ShowHomePage && !IsSingleTenant)`

---

### 3.8 bUnit: Self-Service Toggle Visibility Tests
**File**: `Explore.Blazor.Client.Tests/Pages/Admin/InstanceGovernanceSectionTests.cs` (add tests)

- [x] `GovernanceSection_SingleTenant_NoSelfServiceRegistrationToggle`
- [x] `GovernanceSection_MultiTenant_HasSelfServiceRegistrationToggle`
- [x] Run, all pass ✓ (593 total, 0 failures)

---

## Phase 4: UI — Control Plane Improvements ✅ COMPLETE

### 4.1 Data-Driven Nav Refactor in InstanceAdminSettingsLayout
**File**: `Explore.Blazor.Client/Pages/Admin/Instance/Components/InstanceAdminSettingsLayout.razor`

- [x] Read the file (full read)
- [x] Define private `NavItem` record: `(string Key, string Icon, string Label, Color IconColor = Color.Default, string? Group = null)`
- [x] Extract `BuildNavItems(bool isSingleTenant)` → `IReadOnlyList<NavItem>`
- [x] Render sidebar from the list, grouping by `Group` with `MudText` separator labels
- [x] Multi-tenant nav order: `governance, tenants, domain, branding, auth-providers, modules, storage, smtp, analytics-privacy, [Advanced] render-policies`
- [x] Single-tenant nav order: (preserve current), `[Advanced] render-policies, multi-tenancy (Color.Warning)`, `[Developer] access-tokens, webhooks`
- [x] Add multi-tenant `render-policies` content case to the content area (removed `&& IsSingleTenantMode` guard)
- [x] Verify all existing section content still renders

**AC**:
- [x] Nav built from list, no if/else forest
- [x] Multi-tenant "Tenants" is second nav item
- [x] Single-tenant "Multi-Tenancy" has warning icon color
- [x] All sections functional

---

### 4.2 Conditional Reload After Mode Change
**File**: `Explore.Blazor.Client/Pages/Admin/Instance/Components/InstanceAdminSettingsLayout.razor`

- [x] Add `_loadedDeploymentMode` field (set once in `OnInitializedAsync`)
- [x] In `SaveAsync`, after successful save, compare `_deploymentMode` vs `_loadedDeploymentMode`
- [x] If changed: `Snackbar.Add("Mode changed. Reloading...", Severity.Info)`, `await Task.Delay(800)`, `Navigation.NavigateTo(Navigation.Uri, forceLoad: true)`
- [x] If not changed: `Snackbar.Add("Settings saved.", Severity.Success)` only

**AC**:
- [x] Mode unchanged → no reload
- [x] Mode changed → reload after snackbar

---

### 4.3 Section Normalization After Mode Switch
**File**: `Explore.Blazor.Client/Pages/Admin/Instance/Components/InstanceAdminSettingsLayout.razor`

- [x] Extract `NormalizeSection(string section, bool isSingleTenant)` method
- [x] Single-tenant invalid sections (that belong to multi-tenant-only) → return `"governance"`
- [x] Multi-tenant invalid sections (single-tenant-only) → return `"governance"`
- [x] Use in `OnDeploymentModeChanged` (replaces old inline if/else logic)

---

### 4.4 Add "Assign Me as Tenant Admin" Checkbox to Tenant Creation UI
**File**: `Explore.Blazor.Client/Pages/Admin/Instance/Components/InstanceTenantsSection.razor`

- [x] Read the file
- [x] Add `private bool _assignSelfAsTenantAdmin;` field (default: false)
- [x] Add `MudCheckBox T="bool" @bind-Value="_assignSelfAsTenantAdmin"` in create dialog
- [x] Label: "Assign me as administrator of this tenant"
- [x] Pass flag via `dto.AdditionalProperties["assignCurrentUserAsTenantAdmin"]` (JsonExtensionData)
- [x] Reset field in `OpenCreateDialog()`
- [x] On success with flag true: mention in snackbar

**Depends on**: Tasks 2.1 ✅ (backend exists)

---

### 4.5 Strengthen Danger-Zone Copy in Multi-Tenancy Section
**File**: `Explore.Blazor.Client/Pages/Admin/Instance/Components/InstanceGovernanceSection.razor`

- [x] When `DisplayMode == "multi-tenancy"` and `IsSingleTenant`: added `MudAlert Severity="Warning"` header
- [x] Bullet list of 4 structural change warnings
- [x] When `DisplayMode == "multi-tenancy"` and not `IsSingleTenant`: added info copy for reverting
- [x] Does not affect other display modes

---

## Phase 5: Final Verification ✅ COMPLETE

### 5.1 Check `IDeploymentModeProvider.InvalidateCacheAsync` in Governance Handler
**File**: `UpdateInstanceGovernanceSettingsCommandHandler.cs`

- [x] Was NOT called — added `IDeploymentModeProvider` injection + `InvalidateCacheAsync()` after transaction
- [x] Updated unit test constructor to pass new `IDeploymentModeProvider` mock
- [x] 516 unit tests still pass ✓

---

### 5.2 Full Test Suite
```bash
dotnet build --configuration Release --verbosity quiet
```
- [x] Build passes with 0 errors

```bash
dotnet test --project Event.Application.UnitTests/Event.Application.UnitTests.csproj --configuration Release --verbosity quiet
```
- [x] 516 total, 0 failures ✓

```bash
dotnet test --project Event.Domain.UnitTests/Event.Domain.UnitTests.csproj --configuration Release --verbosity quiet
```
- [x] 100 total, 0 failures ✓

```bash
dotnet test --project Event.Architecture.Tests/Event.Architecture.Tests.csproj --configuration Release --verbosity quiet
```
- [x] 40 total, 0 failures ✓

```bash
dotnet test --project Explore.Blazor.Client.Tests/Explore.Blazor.Client.Tests.csproj --configuration Release --verbosity quiet
```
- [x] 593 total, 0 failures ✓

---

## Completion Gate (All Must Be ✅ Before PR)

| # | Criterion |
|---|-----------|
| 1 | `GetActiveTenantCountAsync` filters on `IsActiveState == true` only |
| 2 | Count check runs inside the transaction in the governance handler |
| 3 | `FailureCode = "DeploymentModeChangeBlockedByActiveTenants"` returned on block |
| 4 | 5 new guard tests pass in `UpdateInstanceGovernanceSettingsCommandHandlerTests` |
| 5 | `CreateTenantCommand` has opt-in role assignment, tested |
| 6 | `/admin/tenant/settings` redirects in single-tenant, has error fallback |
| 7 | Zero lock toggles visible in any single-tenant section |
| 8 | Self-service toggle hidden in single-tenant governance |
| 9 | Nav is data-driven (BuildNavItems method) |
| 10 | Multi-tenant "Tenants" is second nav item |
| 11 | Render Policies accessible in multi-tenant nav |
| 12 | Force-reload only when mode actually changed |
| 13 | Section normalized to "governance" when current section invalid for new mode |
| 14 | Danger-zone explanatory copy in Multi-Tenancy section |
| 15 | bUnit tests: redirect (3), lock toggles (4), self-service (2) — all pass |
| 16 | Full build + all 4 test projects pass with zero failures |
