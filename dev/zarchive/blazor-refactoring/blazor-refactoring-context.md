# Context: Blazor Refactoring (Refreshed Baseline)

**Last Updated: 2026-02-15**

---

## SESSION PROGRESS (2026-02-15)

### COMPLETED IN THIS SESSION
- Re-validated API + Blazor current state before trusting old plan docs.
- Verified authz model is now unified around `Role` + `RoleEnum` + `RoleScopeEnum`.
- Verified Blazor auth flow uses BFF shim routes (`/login`, `/logout`) to server endpoints (`/auth/challenge`, `/auth/signout`).
- Verified routing stack is Blazouter (`RouteConfig` + `IRouteGuard`) and not native-only `@page`.
- Identified stale assumptions in old plan/tasks and refreshed all three dev docs.
- Removed cross-user token fallback (`GetAnyValidToken`) from `CircuitAccessTokenService` / `AccessTokenForwardingHandler`.
- Added token isolation tests in `Explore.Blazor.Client.Tests/Services/CircuitAccessTokenServiceTests.cs`.
- Expanded C.2 service tests in `AdminServiceTests`, `UserServiceTests`, and `EventRegistrationServiceTests` for additional success/error-path behavior.
- Verified full client test suite after latest C.2 edits (`413 passed, 0 failed`).
- Expanded C.2 service tests further into `EventServiceTests` and `OrganizationServiceTests` with additional matrix coverage and concrete API-call contract assertions.
- Reduced nullable-warning noise in newly touched service test files by introducing local `CreateApiException(...)` helpers and removing null-literal constructor usage.
- Re-verified full client test suite after expansions (`442 passed, 0 failed`).
- Completed C.2 remaining-wrapper pass by adding `TenantNavigationServiceTests` and `EventSessionSpeakerServiceTests`.
- Re-validated full client test suite after latest C.2 additions (`455 passed, 0 failed`).
- Confirmed C.4 coverage already exists and is now marked complete in tasks (`NavMenuAdminTests` + `AuthRedirectPagesTests`).
- Added first dedicated C.3 auth-sensitive page test file: `MyOrganizationsTests` (loading/error/empty/success states).
- Added additional C.3 auth-sensitive page coverage: `MyEventsTests` and `MyRegistrationsTests` (loading/error/empty/success paths).
- Added `UserProfileTests` coverage for loading/error/sync-fallback/success behavior and profile stats/review rendering.
- Added first admin CRUD/dashboard C.3 coverage with `CategoriesTests` and `AdminListTests`.
- Expanded admin CRUD C.3 coverage further with `TagsTests` and `LocationsTests`.
- Added admin workflow coverage with `LookupTablesTests` for loading/error/success behavior during parallel lookup fetches.
- Implemented D.1 documentation split with explicit API-coupled dependency mapping and a prepared API epic reference.
- Re-validated client tests with no-build run after latest C.3 additions (`489 passed, 0 failed`).
- Fixed role-model drift compile errors after NSwag/API change by replacing stale `OrganizationRoleId` references with `RoleId` across Organization member UI files.
- Restored solution build health (`dotnet build Explore.sln --configuration Release --no-restore /clp:ErrorsOnly` => 0 errors).

### VERIFIED CURRENT TRUTHS (DO NOT ASSUME OLD PLAN)
- **Unified roles**: Domain uses `Explore.Domain/Role.cs` with scope discriminator. Old `OrganizationRole` entity file is gone.
- **Legacy naming still exists in places**: some client/UI references still use `OrganizationRoleId` naming even after role unification (treat as migration residue, not architecture truth).
- **Admin authority is DB-first**: `AdminClaimsTransformation` enriches claims (`explore:admin:*`), then claims are serialized for Blazor WASM.
- **Render mode**: Blazor app runs Hybrid with `InteractiveAuto` (server + WASM path).
- **BFF auth endpoints**: server auth endpoints are `/auth/challenge` and `/auth/signout`; client pages `/login` and `/logout` are redirect shims.
- **Token fallback hardened**: cross-user "any valid token" fallback is removed; forwarding now resolves HttpContext token first, then current-user token-store lookup only.

### WHAT IS STALE FROM OLDER DOCS
- Any section stating render mode decision is still pending.
- Any section assuming `OrganizationRole` is the canonical role model.
- Any section treating Bootstrap replacement as pending in Blazor UI.
- Any section treating Phases 1-6 as broadly untouched.

---

## KEY DECISIONS (CURRENT)

1. **Always verify against code first**
   - Old plan/task docs are historical snapshots and may drift.
   - For each future task, validate file/state before applying plan steps.

2. **Plan execution now focuses on remaining work**
   - Complete test expansion and targeted risk hardening.
   - Do not re-run completed cleanup/refactor phases unless regression is found.

3. **Authz planning must target unified role model**
   - Use `Role`, `RoleEnum`, `RoleScopeEnum`, `RolePermission`, permission-based checks.
   - Track and gradually rename legacy `OrganizationRoleId` UI/API contract fields where safe.

---

## HIGH-LEVERAGE FILES TO CHECK FIRST

### Authz + Role Model
- `Explore.Domain/Role.cs`
- `Explore.Domain/Enums/RoleEnum.cs`
- `Explore.Domain/Enums/RoleScopeEnum.cs`
- `Explore.API/Controllers/RoleController.cs`
- `Explore.Infrastructure/Identity/AdminClaimsTransformation.cs`
- `Explore.Application/Authorization/AdminClaimTypes.cs`

### Blazor Auth/BFF/Routing
- `Explore.Blazor/Program.cs`
- `Explore.Blazor/Components/App.razor`
- `Explore.Blazor/Services/CircuitAccessTokenService.cs`
- `Explore.Blazor.Client/Program.cs`
- `Explore.Blazor.Client/Routes.razor`
- `Explore.Blazor.Client/Pages/Auth/LoginRedirect.razor`
- `Explore.Blazor.Client/Pages/Auth/LogoutRedirect.razor`
- `Explore.Blazor.Client/Routing/Guards/AdminRouteGuard.cs`

### Refactoring Docs (living source of truth)
- `dev/active/blazor-refactoring/blazor-refactoring-plan.md`
- `dev/active/blazor-refactoring/blazor-refactoring-tasks.md`
- `dev/active/blazor-refactoring/blazor-refactoring-context.md`

---

## NEXT EXECUTION FOCUS

1. Continue remaining C.3 page/component coverage on `AdminListDetails` and dialog-heavy CRUD interaction paths (create/edit/delete confirmations).
2. Keep D.1 split synced if API contract decisions change; activate API epic only after approval.
3. Optionally harden/replace static token store design in a future iteration (now non-blocking after fallback removal).

---

## QUICK RESUME

When resuming:
1. Re-read this file first.
2. Read `blazor-refactoring-tasks.md` for current checklist status.
3. Before doing any task, verify target files still match assumptions.
4. If mismatch found, update plan/tasks/context first, then implement.
## Context Reset Session Update (2026-02-15 21:24 Europe/Brussels)

- Current implementation state: No new implementation changes in this session for this track.
- Key decisions made this session: Priority shifted to analytics implementation completion and verification.
- Files modified and why: None in this track during this session.
- Blockers/issues discovered: None newly discovered for this track.
- Next immediate steps: Continue from highest-priority unchecked items in `blazor-refactoring-tasks.md`.
