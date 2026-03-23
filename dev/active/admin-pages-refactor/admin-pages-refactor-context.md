# Admin Pages Refactor — Context

**Last Updated: 2026-03-22**

---

## SESSION PROGRESS (2026-03-22)

### ✅ COMPLETED
- Planning phase complete (v2 — post architect review)
- All three plan files updated with reviewer feedback

### 🟡 IN PROGRESS
- Nothing yet — awaiting implementation start

### ⚠️ BLOCKERS
- Must verify `CreateTenantCommandHandler.cs` location before Task 2.1. The glob for `Explore.Application/Features/Tenants/Handlers/Commands/*.cs` returned only nav-link handlers. The `CreateTenantCommand` exists but the handler may be in a nested subfolder or handled via a thin controller action. Check `Explore.API/Controllers/TenantController.cs`.

---

## Critical Pre-Implementation Research Findings

### Finding 1: `GetActiveTenantCountAsync` is BUGGY — no status filter
**File**: `Explore.Persistence/Repositories/TenantRepository.cs`

```csharp
// CURRENT (wrong):
return await _dbContext.Tenants.AsNoTracking().CountAsync();
```

Counts ALL tenants including Archived, Suspended, etc. Must filter on `TenantStatus.IsActiveState == true`.
This affects: command handler guard, UI warning badge, GetActiveTenantCountQueryHandler.

### Finding 2: Count check is OUTSIDE the transaction (race condition)
**File**: `Explore.Application/Features/InstanceOnboarding/Handlers/Commands/UpdateInstanceGovernanceSettingsCommandHandler.cs`

Lines 78-91: count check runs BEFORE `ExecuteInTransactionAsync` on line 99.
Must move the check INSIDE the transaction so validation and write are atomic.

### Finding 3: The handler already HAS the Multi→Single guard
The guard code exists at lines 78-91. It just has the two bugs above.
Do NOT rewrite the handler from scratch — fix the two bugs only.

### Finding 4: Zero test coverage on the guard
`UpdateInstanceGovernanceSettingsCommandHandlerTests.cs` has 3 tests:
1. Not-admin case
2. Valid settings pass
3. Render policy validation

None test the Multi→Single tenant count guard. Add at least 5 tests (see plan).

### Finding 5: `CreateTenantCommandHandler.cs` not found via glob
`Explore.Application/Features/Tenants/Handlers/Commands/` only has nav link handlers.
Before implementing Task 2.1, read `TenantController.cs` to understand how `CreateTenantCommand` is dispatched.

### Finding 6: bUnit test infrastructure already exists
`Explore.Blazor.Client.Tests/` is a full bUnit project with `BlazorTestContext`, `MockServiceFactory`, etc.
Pattern to follow: `InstanceGovernanceSectionTests.cs` — uses `DynamicComponent` reflection to render components.

---

## Architecture Decisions (Final — Do Not Revisit Without Good Reason)

### Decision 1: Canonical active-tenant definition = `TenantStatus.IsActiveState == true`
**Rationale**: `IsActiveState` is already the discriminating field on `TenantStatus`. Using it is consistent with the entity model. Avoid hardcoding `TenantStatusEnum.Active` integer in queries — use the navigation property.
**Rule**: No parallel filter logic. One repository method. All code calls that method.

### Decision 2: Count check inside transaction (not before)
**Rationale**: Without a transaction, a concurrent admin action could activate a second tenant between check and commit. Though the probability is low in a small self-hosted instance, the invariant must be enforced atomically. This is the difference between "a guard" and "an authoritative invariant".

### Decision 3: `FailureCode` as a nullable string on `BaseCommandResponse<T>`
**Rationale**: Adding a typed enum introduces a shared dependency that can grow out of control. A string constant that both API and UI agree on is simpler and backward-compatible (no breaking change since it's nullable with no default).
**Value**: `"DeploymentModeChangeBlockedByActiveTenants"`

### Decision 4: Force-reload ONLY when deployment mode value actually changed
**Rationale**: Reloading on every governance save would be jarring and incorrect. The reload is specifically to refresh `BffAdminClaimsTransformation` and render the new nav structure — neither of which is needed for non-mode-change saves.
**Implementation**: Track `_loadedDeploymentMode` separately from `_deploymentMode`. Compare after save.

### Decision 5: Nav list is data-driven via a private `BuildNavItems` method
**Rationale**: The current Razor `if/else` block has 30+ lines of duplicated structure for two modes. It will grow. A list-based model makes reordering a one-line change, enables easier tests, and eliminates rendering bugs from copy-paste nav items.
**Implementation**: Private record `NavItem(string Key, string Icon, string Label, Color IconColor, string? Group)`. Method returns `IReadOnlyList<NavItem>`.

### Decision 6: `/admin/tenant/settings` redirect has error fallback, no loop
**Rationale**: If mode resolution fails (service exception), a redirect loop to the same page would be catastrophic. On error, render an inline error state and stop.

### Decision 7: `AssignCurrentUserAsTenantAdmin` is optional and non-blocking on failure
**Rationale**: Tenant creation is the primary action. Admin assignment is a convenience side effect. If role resolution fails (e.g., missing role seed), the tenant must still be created. Log a warning, return success. Do not roll back tenant creation for a failed optional assignment.

### Decision 8: Claims refresh after tenant admin assignment — watch item, not action
**Rationale**: The force-load already triggers `BffAdminClaimsTransformation`. If the admin opts in as tenant admin and then force-reloads (from the mode-switch save flow), their claims will include the new role. If there is no mode change, assess whether `IAdminCacheInvalidator.InvalidateUser` is needed in the service layer.

---

## Key Files

### Backend — Fix First
| File | What to Do |
|------|-----------|
| `Explore.Persistence/Repositories/TenantRepository.cs` | Fix `GetActiveTenantCountAsync` status filter |
| `Explore.Application/Features/InstanceOnboarding/Handlers/Commands/UpdateInstanceGovernanceSettingsCommandHandler.cs` | Move count check inside transaction; add `FailureCode` |
| `Explore.Application/Responses/BaseCommandResponse.cs` | Add `string? FailureCode` property |
| `Event.Application.UnitTests/Features/InstanceOnboarding/Commands/UpdateInstanceGovernanceSettingsCommandHandlerTests.cs` | Add 5 guard tests |

### Backend — New Feature
| File | What to Do |
|------|-----------|
| `Explore.Application/Features/Tenants/Requests/Commands/CreateTenantCommand.cs` | Add `bool AssignCurrentUserAsTenantAdmin` + `Guid? RequestingUserId` |
| `[Find CreateTenantCommandHandler location]` | Add conditional `TenantMember` creation in same transaction |
| `Event.Application.UnitTests/Features/Tenants/Commands/CreateTenantCommandHandlerTests.cs` | Create with 4 tests |

### Frontend — Simplification
| File | What to Do |
|------|-----------|
| `Explore.Blazor.Client/Pages/Admin/Tenant/TenantAdminSettings.razor` | Add redirect in single-tenant mode with error fallback |
| `Explore.Blazor.Client/Pages/Admin/Instance/Components/InstanceStorageSection.razor` | Wrap lock toggle with `@if (!IsSingleTenant)` |
| `Explore.Blazor.Client/Pages/Admin/Instance/Components/InstanceSmtpSection.razor` | Same as above |
| `Explore.Blazor.Client/Pages/Admin/Instance/Components/InstanceAnalyticsPrivacySection.razor` | Same as above |
| `Explore.Blazor.Client/Pages/Admin/Instance/Components/InstanceGovernanceSection.razor` | Hide self-service toggle in single-tenant; add danger-zone copy |

### Frontend — Control Plane
| File | What to Do |
|------|-----------|
| `Explore.Blazor.Client/Pages/Admin/Instance/Components/InstanceAdminSettingsLayout.razor` | Data-driven nav; conditional reload; section normalization; force-reload on mode change |
| `Explore.Blazor.Client/Pages/Admin/Instance/Components/InstanceTenantsSection.razor` | Add "assign me as admin" checkbox |

### Tests
| File | What to Do |
|------|-----------|
| `Explore.Blazor.Client.Tests/Pages/Admin/InstanceGovernanceSectionTests.cs` | Add self-service toggle tests |
| `Explore.Blazor.Client.Tests/Pages/Admin/InstanceSectionLockToggleTests.cs` | Create: 3 lock toggle tests |
| `Explore.Blazor.Client.Tests/Pages/Admin/TenantAdminSettingsRedirectTests.cs` | Create: 3 redirect tests |

---

## What NOT to Change

- `CompleteInstanceOnboardingCommandHandler` — already correct and transactional
- `IHierarchicalSettingsResolver` and `HierarchicalSettingsResolver` — fully implemented
- `BlockInSingleTenantAttribute` — already correct
- `IDeploymentModeProvider` — already correct
- The onboarding stepper steps (3 for multi, 4 for single) — already correct
- `InstanceGovernanceSection.razor` deployment mode switch dialogs — already correct with typed confirmation
- All existing passing unit tests — must not regress

---

## Quick Resume

To continue:
1. Read this file + `admin-pages-refactor-tasks.md`
2. Start Phase 1: Fix `GetActiveTenantCountAsync` (5 min), then move count check inside tx (15 min)
3. Then add the missing tests (30 min)
4. Then Phase 2: Find CreateTenant handler, add flag
5. Then Phase 3+4: UI simplification (lock toggles, redirect, nav refactor)
6. Run full test suite after each phase
