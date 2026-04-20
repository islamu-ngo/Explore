ABOUTME: Live context file for the API contract stabilization effort. Holds session progress, key files, decisions, and quick-resume guidance.
ABOUTME: Update the SESSION PROGRESS section every meaningful step — this is what survives context resets.

# API Contract Stabilization - Context

**Last Updated:** 2026-04-20 (Phase 4 complete; commits `5917b26e` + `c146fb20` landed on `develop`)
**Parent of:** `dev/active/hateoas-client-alignment/`
**Status:** Phase 0 ✅ | Phase 1 ✅ | Phase 2 ✅ | Phase 3 ✅ | Phase 4 ✅ | Phase 5A ⏳ NEXT

---

## SESSION HANDOFF — 2026-04-20 (Phase 4 close-out)

### ✅ Phase 4 Complete

**Commits landed on `develop`:**
1. **`5917b26e`** `refactor(blazor): align client services with regenerated API client`
   - 25 files, 421 insertions / 421 deletions
   - Regenerated `EventApiClient.g.cs` + 22 service files + 1 Razor component + `swagger.json` drift picked up
2. **`c146fb20`** `test(blazor): align test mocks with regenerated API client signatures`
   - 23 test files in `Explore.Blazor.Client.Tests/`, 327 insertions / 327 deletions
   - NSubstitute matchers expanded for new `api_version` / `x_Api_Version` trailing params

**Verification:**
- Full solution build: **0 errors** (2575 pre-existing warnings).
- `ApiClientNamingTests`: **4/4 GREEN** (Phase 0.2 guardrail satisfied — zero `\dAsync`, no banned placeholders).
- `Explore.Blazor.Client.Tests`: 691 passed / 1 failed / 1 skipped. The single failure (`AuthorizationProviderConfigurationTests.SaveLocal_WhenBrowserCommandSucceeds_RedirectsToLogin`) is a pre-existing timing-flaky TUnit `WaitForFailedException`, last touched in `0a0697db` — **not a Phase 4 regression**.

**Non-inferable learnings for future phases:**
- NSwag with `operationGenerationMode: SingleClientFromOperationId` now emits trailing `string? api_version, string? x_Api_Version, CancellationToken ct` on every method. All callers must pass `cancellationToken:` as a named argument; all NSubstitute matchers must insert two `Arg.Any<string?>()` before `Arg.Any<CancellationToken>()`.
- `MockServiceFactory.ITranslationService` mocks target a service interface (not IEventApiClient) and therefore keep the original `(string, CancellationToken)` shape — do not apply the wildcard expansion there.
- The "5 service wrappers" estimate in the original plan was low; the real production blast radius was **23 files** (+ 23 test files).

### 🔜 Next (Phase 5A — Contract-surface hygiene)
See `api-contract-stabilization-tasks.md` Phase 5A for the task list.

---

## SESSION HANDOFF — 2026-04-20

### 🚨 UNCOMMITTED WORKING TREE (read BEFORE resuming)

Branch `develop`, **50 commits ahead of origin/develop**. The working tree carries a large uncommitted Phase 3 + partial Phase 4 payload. Do **not** stash blindly — inspect first.

```
M  39 Explore.API/Controllers/*.cs          (Phase 3.1 — `Name = RouteNames.X` added)
M  Explore.API/Hateoas/RouteNames.cs        (+183 lines, 65+ new constants)
M  Explore.API/swagger.json                 (regenerated; ~46k delete / 20k add — halved)
M  Explore.Blazor.Client/Clients/EventApiClient.g.cs  (regenerated; ~86k → ~54k lines)
M  Explore.Blazor.Client/packages.lock.json
M  Explore.Blazor.Client/Pages/Onboarding/AuthorizationProviderConfiguration.razor
M  Explore.Blazor/Extensions/BffSetupSecretEndpoints.cs
M  Explore.Blazor/Extensions/MiddlewareExtensions.cs
M  Explore.Blazor/Program.cs
M  dev/_journal/journal.md
M  dev/active/api-contract-stabilization/api-contract-stabilization-action-inventory.md  (regenerated)
M  dev/active/api-contract-stabilization/api-contract-stabilization-tasks.md
D  dev/active/blazor-clean-code-refactor/*                (moved to dev/pause/)
?? Event.API.IntegrationTests/Features/SwaggerJsonExportTests.cs  (Phase 4.1 exporter — NEW)
?? dev/pause/blazor-clean-code-refactor/                  (parked — Wave A shipped in `697d2d99`)
```

**What's safe to commit as one Phase-3 change-set:**
- 39 controllers + `RouteNames.cs` + `swagger.json` + `EventApiClient.g.cs` + `action-inventory.md` + `tasks.md` + new `SwaggerJsonExportTests.cs`.

**What's unrelated drift (keep separate):**
- `.claude/context-state.json` (tooling state).
- 4 Blazor/Explore.Blazor files (onboarding razor, middleware, program, bff setup secret endpoints, packages.lock) — appears unrelated to api-contract-stabilization; inspect before including.
- `dev/active/blazor-clean-code-refactor/` → `dev/pause/` move (Wave A already merged in `697d2d99`).

### ✅ COMPLETED THIS SESSION (Phase 3)

Phase 3.1 — **85 actions named** across 10 controllers via `[HttpVerb("route", Name = RouteNames.X)]`:
- TenantController (11), InstanceSettingsController (32), StorageObjectController (10), InstanceOnboardingController (9), OrganizationMemberController (7), UserController (6), TenantOnboardingController (5), OrganizationReviewController (3), OrganizationController (1), ModuleController (1).
- `:guid` route constraints added wherever `{id}`/`{userId}`/`{organizationId}` were unconstrained.

Phase 3.2 — startup-time **OperationIdInvariantTransformer** already wired (`Explore.API/OpenApi/OperationIdInvariantTransformer.cs:110`, registered in `Program.cs:165`). Throws aggregated `InvalidOperationException` in Development with remediation on any placeholder or missing operationId.

Phase 3.3 — **65+ new `RouteNames.*` constants** added in 10 regions (Tenant Navigation Routes, StorageObject, User, Organization Member/Review/Main, Instance Settings, Instance Onboarding, Tenant Onboarding, Module).

Phase 3.4 — endpoint classification carried forward from Phase 1.5: `0` operations missing `x-endpoint-class`. Stable breakdown `Admin=6, Authenticated=228, Public=129`.

Phase 3.5 — **OpenAPI + inventory regenerated** (2026-04-20 11:24:06Z). Inventory shows `0` `_(missing)_`, `0` placeholder fallbacks. `ContractInvariantsTests` 5/5 GREEN, `RouteNameCoverageTests` 1/1 GREEN.

Phase 4.1 (partial) — `Event.API.IntegrationTests/Features/SwaggerJsonExportTests.cs` created as a **test-based swagger.json refresher** (reuses `ContractApiFixture`, walks up from `AppContext.BaseDirectory` to repo root, pretty-prints, writes `Explore.API/swagger.json`). **Untracked.**

Phase 4.2 (partial) — `EventApiClient.g.cs` **regenerated** from the new swagger. Line count 86 632 → **54 563**. Numeric-suffix fallbacks **464 → 11 unique methods**:
```
BySession2Async, Complete2Async, EventseriesGET2Async, EventsessionagendaitemGET2Async,
Internal2Async, SettingsGET2Async, Status2Async, Status3Async, Status4Async,
StorageobjectGET2Async, Test2Async
```

### 🟡 IN PROGRESS / NEXT STEP

**Phase 4.3 — inspect regeneration diff.** 11 residual numeric-suffix methods remain. Investigation options:
1. Each represents a controller that still has a collision on `(controller, short-verb)` after Phase 3 — likely the 5 controllers NOT in the 10 touched (EventSeries, EventSessionAgendaItem, StorageObject has one left → means 10-controller pass missed an action; verify).
2. May also indicate controllers never touched (EventStatus, RegistrationScope, etc. — `Status2Async/3Async/4Async`).

**Verify before Phase 4.4 build:** Run `grep -nE "[A-Za-z]+[0-9]+Async\b" Explore.Blazor.Client/Clients/EventApiClient.g.cs | sort -u -t: -k2,2` then map each to the originating controller action. Add `Name = RouteNames.X` + register constant for each. Regenerate `swagger.json` via `SwaggerJsonExportTests` then `dotnet nswag run` in `Explore.Blazor.Client`.

**Then Phase 4.4 + 4.5a-e.** Build is expected to fail in the 5 service wrappers (UserService, EventRegistrationService, EventSeriesService, EventSessionAgendaItemService, OrganizationMemberService) — they call the old suffixed method names. Rename call sites; no logic changes.

### ⏳ NOT STARTED

- Phase 5A — contract-surface hygiene (verify classification, review test-connection endpoints)
- Phase 5B — client-consumer hygiene + smoke test (`GeneratedClientSmokeTests.cs`)
- Phase 5C — UI cleanups (delete legacy `InstanceSettings.razor` / `TenantPolicySettings.razor` — note: `b50264ab` already removed these per commit log; **re-verify before Phase 5C**)
- Phase 6 — fold `hateoas-client-alignment` (add parent-pointer header only)
- Phase 7 — CI wiring, schema-diff job, forward standard in `docs/QUICK_REFERENCE.md`, new `ApiContractArchitectureTests`

### 🚩 KNOWN FLAGS

- `InstanceSettingsController` declared `Authenticated` but every action runtime-checks `IsInstanceAdmin`. True class is **Admin**. Phase 5A review target.
- `TenantController` writes declared `Authenticated` (no `Roles=` attribute anywhere in codebase). Arguably Admin. Same review bucket.
- Zero `[Authorize(Roles=...)]` attributes codebase-wide — auth is inline runtime checks. Separate workstream.
- **Wave A Blazor clean-code refactor merged (`697d2d99`).** `dev/active/blazor-clean-code-refactor/` moved to `dev/pause/`. Formal Phase 3 (BFF endpoint decomposition) + Phase 4 (IMiddleware + per-handler timeouts) still open. Not a blocker for this plan.

### ⚠️ BLOCKERS / DECISIONS NEEDED (carried forward)

- **Phase 4.3:** 11 residual collisions — need one-pass fix before Phase 4.4 build.
- **Phase 5A.3:** Storage/SMTP/Localization test-connection endpoints — Authenticated Admin vs Internal.
- **Phase 5C.3:** `ImageStorageService` SRP split approval (nice-to-have).
- **Pre-commit:** Split the working-tree into three commit groups: (a) Phase 3 + Phase 4 partials, (b) unrelated Blazor drift (onboarding/middleware/bff), (c) blazor-clean-code-refactor archival move. Ask user which grouping to adopt before `git add`.

---

## Key Files

### The root-cause evidence (post-regeneration)
- **`Explore.Blazor.Client/Clients/EventApiClient.g.cs`** (54 563 lines, down from 86 632)
  - Unique `\dAsync` methods: **11** (down from 464). Remaining residue is Phase 4.3 target.
- **`Explore.API/swagger.json`** — regenerated via `SwaggerJsonExportTests`. Zero `/api/v0.1/...` paths, zero missing operationIds, zero placeholder patterns.
- **`Explore.Blazor.Client/nswag.json`** — unchanged. `operationGenerationMode: SingleClientFromOperationId`.

### Controllers — Phase 3 references
- **`Explore.API/Controllers/TenantController.cs`** — first controller converted; 11 actions named `Tenant_GetAll`/`Tenant_GetById`/`Tenant_Update`/… See commit log for the exact constant naming.
- **`Explore.API/Controllers/ActorController.cs`** — baseline good citizen; explicit action names, already Route-constant-friendly pre-Phase-3.
- **`Explore.API/Controllers/InstanceSettingsController.cs`** — largest single-controller change (32 actions named).

### The versioning plumbing (Phase 2 — LOCKED IN)
- **`Explore.API/Extensions/ApiVersioningExtensions.cs`** — multi-reader (media-type + query-string `?api-version=0.1` + header `X-Api-Version: 0.1`). `VersionedRouteConvention` deleted.

### Phase 4.3-4.5 targets (5 service wrappers)
- `Explore.Blazor.Client/Services/UserService.cs`
- `Explore.Blazor.Client/Services/EventRegistrationService.cs`
- `Explore.Blazor.Client/Services/EventSeriesService.cs`
- `Explore.Blazor.Client/Services/EventSessionAgendaItemService.cs`
- `Explore.Blazor.Client/Services/OrganizationMemberService.cs`

### Phase 5C cleanup targets
- `Explore.Blazor/Components/Pages/Admin/InstanceSettings.razor` — **MAY BE DELETED already** (commit `b50426ac: refactor(blazor/ui): remove legacy InstanceSettings and TenantPolicySettings pages`). Verify with `git log --all -- <path>`.
- `Explore.Blazor/Components/Pages/Admin/TenantPolicySettings.razor` — same (above commit).
- `Explore.Blazor.Client/Services/ImageStorageService.cs` — SRP split candidate (deferrable).

### Guardrail tests
- `Event.API.IntegrationTests/Features/ContractInvariantsTests.cs` (Phase 0.1 — 5/5 GREEN after Phase 3)
- `Event.API.IntegrationTests/Features/RouteNameCoverageTests.cs` (Phase 1.4 — 1/1 GREEN)
- `Event.API.IntegrationTests/Features/ApiContractInventoryGeneratorTests.cs` (Phase 1.1 — generator)
- `Event.API.IntegrationTests/Features/SwaggerJsonExportTests.cs` (Phase 4.1 — untracked, refresher)
- `Event.Architecture.Tests/EndpointClassificationArchitectureTests.cs` (Phase 1.5 — 75/75 project green)
- `Explore.Blazor.Client.Tests/ApiClientNamingTests.cs` (Phase 0.2 — still RED while 11 residues exist)
- `Explore.Blazor.Client.Tests/GeneratedClientSmokeTests.cs` (Phase 5B.4 — NOT YET CREATED)
- `Event.Architecture.Tests/ApiContractArchitectureTests.cs` (Phase 7.4 — NOT YET CREATED)

### Documentation (updated in prior commits)
- `docs/ARCHITECTURE.md` line 58 — multi-reader versioning documented.
- `docs/API.md` lines 48-55 — three-reader (non-URL) versioning.
- `docs/GOVERNANCE.md` — "API Contract Rules" subsection (lines 321-373 approx).
- `docs/NAMING_CONVENTIONS.md` — "API Contract Naming" section with format `{ControllerShortName}_{ActionName}`.
- `docs/QUICK_REFERENCE.md` — rule 20 (named routes match `[HttpGet(Name=...)]`). Phase 7.6 adds forward standard.

---

## Important Decisions (stable — reference only)

### D1 — Do NOT set `useOperationIds=false` (Oracle — CTO-approved)
### D2 / D15 (user m0044) — Multi-reader versioning: media-type + query-string + custom-header, NO URL-segment (LOCKED IN — implemented Phase 2)
### D3 — One OpenAPI document
### D4 — Guardrails first, surgery second (Phase 0 non-negotiable)
### D5 — `hateoas-client-alignment` is downstream (Phase 6 adds parent-pointer)
### D6 — Route Name and Operation Id intentionally aligned, not inherently identical
### D7 — Preferred Phase 2 mechanism: delete alias strategy (done)
### D8 — No domain changes
### D9 — No backwards compatibility (CTO-approved)
### D10 — OpenAPI is a governed product artifact
### D11 — Every action has an endpoint class (Phase 1.5 done; 0 unclassified)
### D12 — Schema-diff visible in CI, not blocking (pre-1.0) — Phase 7.5 pending
### D13 — Generated-client ergonomics bar — Phase 0.2 asserts; `ApiClientNamingTests` flips green after Phase 4.3-4.5
### D14 — Action inventory generated, not hand-curated (Phase 1.1 done)

---

## Technical Constraints (from CLAUDE.md + docs/QUICK_REFERENCE.md)

1. Repositories return **entities**, never DTOs; mapping in handlers.
2. Validators manually instantiated (no DI).
3. Commands return `BaseCommandResponse<TId>`.
4. GET = `[AllowAnonymous]`, write = `[Authorize]`, admin = roles (runtime checks only today).
5. UserId fallback: `sub` → `nameidentifier` → `sid`.
6. File-scoped namespaces for new C# files.
7. Named route constants in `RouteNames` must match `[HttpGet(Name = "...")]` values. (RouteNameCoverageTests enforces.)
8. All files start with `ABOUTME:` two-line summary.
9. Auditing fields: `CreatedAt/By`, `UpdatedAt/By`, `IsDeleted`.
10. EF soft-delete filter named `SoftDelete`.

---

## Build / Test Baseline

**Build:**
```
dotnet build --configuration Release --verbosity quiet
```

**Current expected state post-Phase 3 (verified 2026-04-20):**
- Build: **0 errors** (72 pre-existing warnings).
- `Event.API.IntegrationTests` ContractInvariantsTests: **5/5 GREEN**.
- `Event.API.IntegrationTests` RouteNameCoverageTests: **1/1 GREEN**.
- `Event.Architecture.Tests`: **75/75 GREEN**.
- `Explore.Blazor.Client.Tests` ApiClientNamingTests: still **RED** (11 residual `\dAsync` offenders — Phase 4.3 target).

**Test projects run individually:**
- `Event.Application.UnitTests`
- `Event.Domain.UnitTests`
- `Event.Architecture.Tests`
- `Explore.Secrets.UnitTests`
- `Event.Persistence.IntegrationTests`
- `Event.API.IntegrationTests`
- `Explore.Blazor.IntegrationTests`
- `Explore.Blazor.Client.Tests`

---

## Quick Resume (2026-04-20 handoff)

1. **Inspect uncommitted tree first.** `git status` — you should see ~45 modified + 1 untracked `SwaggerJsonExportTests.cs` + `dev/pause/blazor-clean-code-refactor/` relocation + 4 unrelated Blazor drift files (onboarding razor, middleware, bff setup secret, program, packages.lock).
2. **Decide commit grouping with the user** before any `git add`. Three natural groups: (a) Phase 3 + Phase 4.1-4.2 payload, (b) unrelated Blazor drift, (c) blazor-clean-code-refactor archival move.
3. **Phase 4.3** — identify the 11 residual numeric-suffix collisions and resolve:
   ```
   grep -oE "[A-Za-z]+[0-9]+Async\b" Explore.Blazor.Client/Clients/EventApiClient.g.cs | sort -u
   ```
   Map each to originating controller action, add `[HttpVerb("route", Name = RouteNames.X)]`, add constant to `RouteNames.cs`, regenerate `swagger.json` (run `SwaggerJsonExportTests`), regenerate client (`dotnet nswag run nswag.json` in `Explore.Blazor.Client`).
4. **Phase 4.4** build. Expect compile errors only in the 5 service wrappers.
5. **Phase 4.5a-e** — rename call sites in the 5 wrapper files. `lsp_diagnostics` clean after each.
6. **Re-run** `ApiClientNamingTests` — expect all 6 tests GREEN when Phase 4 complete.
7. Flag Phase 5C verification: check if `InstanceSettings.razor`/`TenantPolicySettings.razor` still exist (commit `b50426ac` may have already deleted them).

---

## Session IDs (earlier context resets — do not reuse unless continuing that specific thread)

- ApiClient audit: `ses_25e130f03ffeDihkcd5maMz6AO`
- Services map: `ses_25e12def5ffeOYPGSjO79aOl8w`
- Pages/admin: `ses_25e12aca8ffejWZhSWfxR7hvIQ`
- BFF/auth: `ses_25e127029ffeGJyd6Qwqzd7R6J`
- Controllers/coverage: `ses_25e124572ffebG5WiOkvvAlAGh`
- Oracle strategy: `ses_25d39daafffeI8vNfafL8RSPdR`
