<!-- ABOUTME: Hot execution ledger for the optional Event.Standalone combined host implementation. -->
<!-- ABOUTME: Separates implementation acceptance from one-time phase verification and explicit deferrals. -->

# Event Standalone Combined Host — Task Checklist

Last Updated: 2026-08-02 Europe/Brussels

## Status Summary

- **Overall status:** Implementation in progress; Phase 3 complete, with topology selection and Docker phases deferred.
- **Completed:** 8/15 implementation tasks (phase verification tracked separately)
- **Current priority:** Task 4.2 architecture and operations documentation.
- **Next recommended slice:** Document the Split-default and Standalone opt-in topology before locking the remaining Phase 4 invariants.

## Implementation Maintenance Rules

- Read the full workstream once at initial implementation start; on resume, read context/tasks first and only relevant plan sections.
- Do not reread unchanged artifacts after every task.
- Mark substantial work `🟡 IN PROGRESS`; check it immediately when acceptance is met. Reconcile small tasks no later than phase end.
- Add discovered work under its owning phase and keep count, priority, next slice, deferrals, and date accurate.
- Check a phase complete only after all implementation and verification boxes pass.
- Update context after a phase, decision, blocker, validation failure, material discovery, or handoff.
- Update the plan only for scope, architecture, sequence, acceptance, risk, or validation changes.
- Do not run build/tests after individual tasks; verify once at phase end.
- Do not use app/browser/Docker/Aspire/Playwright/Chrome live runs as phase verification.
- Do not absorb unrelated baseline fixes into this ledger. Phase 5 coordinates with `multi-database-support` for SQLite; Phase 6 owns Docker packaging.

## Implementation Prerequisite ✅ COMPLETE

- [x] **P.1 Confirm a green repository baseline**
  - **Files:** No Standalone files; external owning workstreams fix existing compile errors.
  - **Acceptance:** `dotnet build --configuration Release --verbosity quiet` no longer reports the pre-existing 13 errors. If still red, record exact remaining baseline failures in context and stop before runtime edits.
  - **Effort:** External
  - **Dependencies:** Owning fixes outside this workstream.

## Phase 1: Reusable API Host Composition 🟡 VERIFICATION OPEN

- [x] **1.1 Extract API host composition**
  - **Files:** `src/Explore.API/Program.cs` (existing); `src/Explore.API/Hosting/ApiHostServiceCollectionExtensions.cs` (new); `src/Explore.API/Hosting/ApiHostStartupExtensions.cs` (new); `src/Explore.API/Hosting/ApiHostApplicationExtensions.cs` (new); affected `tests/Event.API.IntegrationTests/**`.
  - **Acceptance:** API registration/startup/pipeline/endpoints are reusable and invoked once; exact middleware/startup behavior is preserved; Program is a thin caller; focused regression assertions cover extraction gaps.
  - **Effort:** XL
  - **Dependencies:** P.1.

### Phase 1 Verification — RUN ONCE AFTER ALL PHASE TASKS

- [x] `dotnet build --configuration Release --verbosity quiet`
- [x] `dotnet test --project tests/Event.API.IntegrationTests/Event.API.IntegrationTests.csproj --configuration Release --verbosity quiet`

## Phase 2: Reusable Blazor/BFF Host Composition ✅ COMPLETE

- [x] **2.1 Extract profile-aware Blazor/BFF composition**
  - **Files:** `src/Explore.Blazor/Program.cs` (existing); `src/Explore.Blazor/Hosting/BlazorHostProfile.cs` (new); `src/Explore.Blazor/Hosting/BlazorHostServiceCollectionExtensions.cs` (new); `src/Explore.Blazor/Hosting/BlazorHostApplicationExtensions.cs` (new); affected `tests/Explore.Blazor.IntegrationTests/**`.
  - **Acceptance:** Split retains current YARP/remote-readiness behavior; Combined excludes them; dynamic auth, BFF endpoints, static assets, SignalR, server/WASM Razor modes, and the existing route-aware render-policy fallback/overrides are reusable; profiles cannot mix accidentally.
  - **Effort:** XL
  - **Dependencies:** 1.1.

- [x] **2.2 Extend architecture coverage for reusable hosts**
  - **Files:** affected `tests/Event.Architecture.Tests/**`.
  - **Acceptance:** Reusable modules are callable from composition roots; existing Blazor/Client lower-layer prohibitions remain; no circular reference appears.
  - **Effort:** M
  - **Dependencies:** 1.1, 2.1.

### Phase 2 Verification — RUN ONCE AFTER ALL PHASE TASKS

- [x] `dotnet build --configuration Release --verbosity quiet`
- [x] `dotnet test --project tests/Explore.Blazor.IntegrationTests/Explore.Blazor.IntegrationTests.csproj --configuration Release --verbosity quiet`

## Phase 3: Single-Process Host And Security Bridge ✅ COMPLETE

- [x] **3.1 Create the standalone composition root**
  - **Files:** new `src/Event.Standalone/{Event.Standalone.csproj,Program.cs,appsettings.json,Properties/launchSettings.json}`; optional new environment settings only when required; `Explore.slnx` (existing).
  - **Acceptance:** Independent .NET 10 startup project references API/Blazor host assemblies, uses 5180/7180, composes common services/startup once, and has no self-proxy or persistence-provider additions.
  - **Effort:** L
  - **Dependencies:** 1.1, 2.1, 2.2.

- [x] **3.2 Share trusted BFF enrichment and add the in-process bridge**
  - **Files:** owning services under `src/Event.Web.BffHosting/**`; affected YARP transforms under `src/Explore.Blazor/**`; new `src/Event.Standalone/Middleware/CombinedApiBridgeMiddleware.cs`; affected standalone tests.
  - **Acceptance:** Browser `/api/*` requests explicitly authenticate the cookie scheme without installing its principal, enforce XSRF for unsafe methods, retrieve usable server-held tokens, strip/reconstruct privileged headers, and use existing bearer validation as the sole API principal. Missing/unrefreshable token fails `401/403` before controllers and never falls through; external bearer/API-key calls stay independent.
  - **Effort:** XL
  - **Dependencies:** 3.1.

- [x] **3.3 Compose the unified middleware and endpoint graph**
  - **Files:** `src/Event.Standalone/Program.cs`; Phase 1 API/Blazor host extensions; affected standalone tests.
  - **Acceptance:** The plan's surface ownership table is implemented; `/api/*` maps once; API, BFF, UI/static/Razor/SignalR, API tooling, and health middleware/endpoints are explicitly scoped; referenced Web SDK assets are discovered without copying; startup/workers execute once.
  - **Effort:** XL
  - **Dependencies:** 3.2.

- [x] **3.4 Add standalone-host integration coverage**
  - **Files:** new `tests/Event.Standalone.IntegrationTests/**`; `Explore.slnx`.
  - **Acceptance:** TUnit/WebApplicationFactory tests cover API/Razor/SignalR endpoint discovery, browser GET and mutation XSRF paths, fail-closed missing/expired/unrefreshable token and cookie-principal isolation, external auth paths, header sanitization, local readiness, referenced static assets, no loopback, and singleton startup/worker registration.
  - **Effort:** XL
  - **Dependencies:** 3.1, 3.2, 3.3.

### Phase 3 Verification — RUN ONCE AFTER ALL PHASE TASKS

- [x] `dotnet build --configuration Release --verbosity quiet`
- [x] `dotnet test --project tests/Event.Standalone.IntegrationTests/Event.Standalone.IntegrationTests.csproj --configuration Release --verbosity quiet`

## Phase 4: Optional Aspire Topology And Operator Contract ⏳ NOT STARTED

- [x] **4.1 Add explicit Aspire topology selection**
  - **Files:** `src/Explore.AppHost/AppHost.cs`; exact AppHost settings/tests only when repository convention requires them.
  - **Acceptance:** `Hosting:Topology` accepts Split/Standalone, defaults Split, rejects unknown values; current API/Blazor AppHost inputs are inventoried and each required key is forwarded once; Standalone registers migration prerequisites plus exactly one web resource and points callbacks/references to it.
  - **Effort:** L
  - **Dependencies:** 3.4.

- [x] **4.2 Update architecture and operations documentation**
  - **Files:** `docs/ARCHITECTURE.md`, `docs/CODEBASE_STRUCTURE.md`, `docs/BLAZOR.md`, `docs/SECURITY-MODEL.md`, `docs/OPERATIONS.md`, `docs/CONFIGURATION.md`, `docs/SELF_HOSTING.md`, `docs/TROUBLESHOOTING.md`.
  - **Acceptance:** Docs consistently explain three composition roots, Split default, standalone ports/config/startup, one-process trust flow, readiness/startup ownership, limitations/rollback, `/api/...` versioning, and explicit SQLite/Compose exclusions.
  - **Effort:** L
  - **Dependencies:** 4.1.

- [x] **4.3 Lock composition-root and topology invariants**
  - **Files:** affected `tests/Event.Architecture.Tests/**`; affected AppHost static/config test files if present.
  - **Acceptance:** Standalone is allowed as a composition root without broadening Blazor/Client access; solution membership/reference direction are protected; Split default and mutually exclusive Standalone opt-in are asserted without launching Aspire.
  - **Effort:** M
  - **Dependencies:** 4.1, 4.2.

### Phase 4 Verification — RUN ONCE AFTER ALL PHASE TASKS

- [ ] `dotnet build --configuration Release --verbosity quiet`
- [ ] `dotnet test --project tests/Event.Architecture.Tests/Event.Architecture.Tests.csproj --configuration Release --verbosity quiet`

## Phase 5: SQLite Default Persistence And Provider Override ⏳ NOT STARTED

- [ ] **5.1 Integrate SQLite default provider with standalone composition**
  - **Files:** `src/Event.Standalone/Program.cs` (existing); `src/Event.Standalone/appsettings.json` (existing); persistence registration from `multi-database-support` workstream.
  - **Acceptance:** Default startup with no database configuration uses SQLite at `/app/data/event.db` with WAL mode; `DATABASE_PROVIDER=PostgreSQL` with structured fields switches provider; invalid config fails-fast with actionable diagnostics; busy-timeout prevents `SQLITE_BUSY`.
  - **Effort:** L
  - **Dependencies:** 4.3, multi-database-support Phase 1.

- [ ] **5.2 Add provider-override integration tests**
  - **Files:** affected `tests/Event.Standalone.IntegrationTests/**`.
  - **Acceptance:** Tests prove SQLite default, WAL activation, PostgreSQL override, fail-fast on invalid config, and single-replica enforcement.
  - **Effort:** M
  - **Dependencies:** 5.1.

### Phase 5 Verification — RUN ONCE AFTER ALL PHASE TASKS

- [ ] `dotnet build --configuration Release --verbosity quiet`
- [ ] `dotnet test --project tests/Event.Standalone.IntegrationTests/Event.Standalone.IntegrationTests.csproj --configuration Release --verbosity quiet`

## Phase 6: Docker Packaging ⏳ NOT STARTED

- [ ] **6.1 Create multi-stage Dockerfile**
  - **Files:** `src/Event.Standalone/Dockerfile` (new).
  - **Acceptance:** Multi-stage build produces a working image under 250MB; runtime exposes port 8080; `/app/data` directory exists with write permissions; SQLite native binaries present.
  - **Effort:** M
  - **Dependencies:** 5.2.

- [ ] **6.2 Create standalone Docker Compose file**
  - **Files:** `docker-compose.standalone.yml` (new).
  - **Acceptance:** `docker compose -f docker-compose.standalone.yml up` starts with SQLite default; data volume persists; commented PostgreSQL override examples included.
  - **Effort:** S
  - **Dependencies:** 6.1.

- [ ] **6.3 Update self-hosting documentation**
  - **Files:** `docs/SELF_HOSTING.md` (existing); `docs/CONFIGURATION.md` (existing).
  - **Acceptance:** `docker run` one-liner documented; provider override examples cover PostgreSQL/SQL Server/MariaDB; SQLite backup/restore documented; single-replica constraint explicit.
  - **Effort:** M
  - **Dependencies:** 6.1, 6.2.

### Phase 6 Verification — RUN ONCE AFTER ALL PHASE TASKS

- [ ] `dotnet build --configuration Release --verbosity quiet`
- [ ] Manual: `docker build -t islamu/event-standalone -f src/Event.Standalone/Dockerfile .` completes successfully.

## Remaining / Deferred Work

- **Privacy-erasure authority SQLite file:** The embedded `privacy_erasure_authority.db` must remain separate from the primary `event.db` file. Restore lifecycle independence is a hard constraint.
- **Multi-architecture Docker images:** Initial Dockerfile targets `linux/amd64`; `linux/arm64` support is a follow-up after base image verification.
- **Kubernetes / Helm packaging:** Excluded; Compose is the initial packaging target.
- **New API/UI behavior:** Excluded; this topology must be behaviorally compatible.
- **Current compile failures:** Owned outside this workstream; do not hide, suppress, or opportunistically fix them here.
