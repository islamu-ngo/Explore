# Tasks: Blazor Refactoring (Verified Current State)

**Last Updated: 2026-02-15**

---

## Rule Zero (Mandatory for Every Task)

- [ ] Verify target files and assumptions before executing any checklist item.
- [ ] If code reality differs from plan text, update `plan/context/tasks` first.
- [ ] Do not execute stale tasks blindly.

---

## Phase A: Planning Hygiene and Truth Sync

### A.1 Re-baseline docs against current code [S] -- DONE
- [x] Refresh `blazor-refactoring-context.md` with verified current truths.
- [x] Refresh `blazor-refactoring-plan.md` to remove outdated assumptions.
- [x] Ensure architecture statements match current code paths.

### A.2 Confirm render mode baseline [S] -- DONE
- [x] Verify `Explore.Blazor/Components/App.razor` uses `InteractiveAuto` for `HeadOutlet` and `Routes`.
- [x] Remove/avoid any task text implying `InteractiveServer` is current default.
- [x] Keep note that auth/BFF flow is still server-backed via BFF endpoints.

---

## Phase B: Token Service Risk Hardening

### B.1 Validate current token fallback behavior [M] -- DONE
- [x] Inspect `Explore.Blazor/Services/CircuitAccessTokenService.cs` static store usage.
- [x] Document exact fallback order and risk conditions.
- [x] Decide keep-with-guards vs remove-static-fallback.

### B.2 Enforce token isolation confidence [M] -- DONE
- [x] Add/extend tests to assert no cross-user token usage.
- [x] Verify deterministic behavior for HttpContext token vs current-user token-store lookup.
- [x] Record decision and test evidence in context file.

Evidence:
- `Explore.Blazor/Services/CircuitAccessTokenService.cs`: removed `GetAnyValidToken` fallback path.
- `Explore.Blazor.Client.Tests/Services/CircuitAccessTokenServiceTests.cs`: added 4 token isolation tests.
- `dotnet test --project Explore.Blazor.Client.Tests/Explore.Blazor.Client.Tests.csproj`: 410 passed, 0 failed.

---

## Phase C: Test Coverage Expansion (Main Remaining Work)

### C.1 Fix test anti-patterns [S] -- DONE
- [x] Replace `Task.Delay` waiting with deterministic bUnit waiting APIs.
- [x] Replace mock-verification-only tests with behavior/assertion-focused tests.

Evidence:
- `Explore.Blazor.Client.Tests/Pages/HomeTests.cs`: all `Task.Delay` waits removed; replaced with `WaitForState` checks.
- `Explore.Blazor.Client.Tests/Pages/Event/CreateEventTests.cs`: removed `Task.Delay`; replaced setup-only mock tests with render-driven behavior interaction tests.
- `dotnet test --project Explore.Blazor.Client.Tests/Explore.Blazor.Client.Tests.csproj --configuration Debug`: 405 passed, 0 failed.

### C.2 Add service tests [L] -- DONE
- [x] Prioritize AdminService, UserService, and EventRegistrationService for the first expansion pass.
- [x] Add targeted success and error-path tests for AdminService/UserService/EventRegistrationService (including 200/204 success handling, 401, 404, and null/empty responses).
- [x] Expand same edge-case matrix to additional high-traffic wrappers (`EventService`, `OrganizationService`) and tighten API-call contract assertions.
- [x] Continue expansion to remaining service wrappers not yet covered in this pass.

Evidence:
- `Explore.Blazor.Client.Tests/Services/AdminServiceTests.cs`: added pagination assertion and approval-flow success-path tests for 200/204 responses.
- `Explore.Blazor.Client.Tests/Services/UserServiceTests.cs`: added 404 + sync-fallback resilience tests (including retry-failure null outcome).
- `Explore.Blazor.Client.Tests/Services/EventRegistrationServiceTests.cs`: added 401 cancel path and null response handling for by-session fetch.
- `Explore.Blazor.Client.Tests/Services/EventServiceTests.cs`: added paged-events failure/contract tests, delete 500-path test, and converted placeholder call-contract tests into concrete assertions.
- `Explore.Blazor.Client.Tests/Services/OrganizationServiceTests.cs`: added null/500 edge tests for user-org retrieval, 401 status-type path, and concrete pagination contract assertions.
- `Explore.Blazor.Client.Tests/Services/TenantNavigationServiceTests.cs`: added endpoint-contract, failure-status, exception-fallback, and reorder/update/delete behavior coverage.
- `Explore.Blazor.Client.Tests/Services/EventSessionSpeakerServiceTests.cs`: added deterministic tests for current API-regeneration stub behavior (empty/null/false outputs).
- Reduced nullable-warning noise in newly touched service test files by centralizing `ApiException` creation helpers in:
  - `Explore.Blazor.Client.Tests/Services/AdminServiceTests.cs`
  - `Explore.Blazor.Client.Tests/Services/UserServiceTests.cs`
  - `Explore.Blazor.Client.Tests/Services/EventRegistrationServiceTests.cs`
  - `Explore.Blazor.Client.Tests/Services/EventServiceTests.cs`
  - `Explore.Blazor.Client.Tests/Services/OrganizationServiceTests.cs`
- `dotnet test --project Explore.Blazor.Client.Tests/Explore.Blazor.Client.Tests.csproj --configuration Debug --verbosity quiet`: 455 passed, 0 failed.

### C.3 Add page/component tests [L]
- [ ] Cover auth-sensitive pages and common failure states (loading/error/empty).
- [x] Add `MyOrganizations` page coverage for loading/error/empty/success auth flow states.
- [ ] Cover route guard behavior and redirect outcomes where relevant.

Evidence:
- `Explore.Blazor.Client.Tests/Pages/Organization/MyOrganizationsTests.cs`: adds auth-sensitive loading/error/empty/success-state coverage for `MyOrganizations` page.
- `Explore.Blazor.Client.Tests/Pages/Event/MyEventsTests.cs`: adds auth-sensitive loading/error/empty/success-state coverage for `MyEvents` page.
- `Explore.Blazor.Client.Tests/Pages/User/MyRegistrationsTests.cs`: adds user registration loading/error/empty/success-state coverage with session/event enrichment path assertions.
- `Explore.Blazor.Client.Tests/Pages/User/UserProfileTests.cs`: adds profile loading/error/sync-fallback/success-state coverage including stats and recent review rendering.
- `Explore.Blazor.Client.Tests/Pages/Admin/CategoriesTests.cs`: adds admin categories loading/error/empty/success-state coverage.
- `Explore.Blazor.Client.Tests/Pages/Admin/AdminListTests.cs`: adds admin dashboard loading/error/success-summary coverage.
- `Explore.Blazor.Client.Tests/Pages/Admin/TagsTests.cs`: adds admin tags loading/error/empty/success-state coverage.
- `Explore.Blazor.Client.Tests/Pages/Admin/LocationsTests.cs`: adds admin locations loading/error/empty/success-state coverage.
- `Explore.Blazor.Client.Tests/Pages/Admin/LookupTablesTests.cs`: adds admin workflow loading/error/success-state coverage for parallel lookup-table loading.
- `dotnet test --project Explore.Blazor.Client.Tests/Explore.Blazor.Client.Tests.csproj --configuration Debug --no-build -- --disable-logo --no-progress --log-level Error`: 489 passed, 0 failed.

### C.4 Add layout/auth flow tests [M]
- [x] Validate menu visibility by auth/admin claims.
- [x] Validate login/logout shim flow behavior (`/login` and `/logout` pages).

Evidence:
- `Explore.Blazor.Client.Tests/Layout/NavMenuAdminTests.cs`: validates menu visibility for anonymous, authenticated non-admin, instance-admin, tenant-admin, org-admin-only, and route links.
- `Explore.Blazor.Client.Tests/Pages/Auth/AuthRedirectPagesTests.cs`: validates `/login` and `/logout` redirect shims to `/auth/challenge` and `/auth/signout`, including query forwarding.

---

## Phase D: Performance Follow-up (API-Coupled)

### D.1 Clarify server-side filtering dependency [S]
- [x] Document which performance tasks are blocked by API contract shape.
- [x] Open or reference separate API epic for filter/query expansion if approved.

Evidence:
- `dev/active/blazor-refactoring/blazor-refactoring-performance-dependencies.md`: documents Blazor-only vs API-coupled optimization boundaries and includes a prepared API epic reference for approval-based activation.

---

## Verification Commands

```bash
dotnet build Explore.Blazor
dotnet build Explore.Blazor.Client
dotnet test Explore.Blazor.Client.Tests --configuration Release
```

Optional checks:

```bash
# Verify InteractiveAuto remains active
rg "InteractiveAuto|InteractiveServer" Explore.Blazor/Components/App.razor

# Verify known token service risk points
rg "static|GetAnyValidToken|ConcurrentDictionary" Explore.Blazor/Services/CircuitAccessTokenService.cs
```
## Context Reset Session Update (2026-02-15 21:26 Europe/Brussels)

- Status update: No task-state changes in this session for this track.
- Priority update: Keep existing ordering; analytics work was handled in a separate track.
- Next step: Resume from current in-progress or highest-priority unchecked item.
