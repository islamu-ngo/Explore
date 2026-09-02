<!-- ABOUTME: Implementation plan for CTO audit remediation: migration consolidation, NSwag split, controller dedup, warning ratchet, and tenant hardening. -->
<!-- ABOUTME: Covers all P0, P1, and P2 findings from the Senior CTO architecture audit. -->

# CTO Audit Remediation — Implementation Plan

Last Updated: 2026-09-02 Europe/Brussels

## 0. Planning Metadata

- **Original request:** Remediate all P0, P1, and P2 findings from the Senior CTO architecture audit: merge MariaDb into MySql migrations, squash migrations, split monolithic NSwag client, extract shared controller base classes, enable compiler warning ratchet, and harden tenant isolation with PostgreSQL RLS.
- **Task directory:** `dev/active/cto-audit-remediation/`
- **Planning status:** Approved — implementation active
- **Change Classification:** `Non-Behavioral Delta` (Phases 1–5) + `Behavioral Delta` (Phase 6 — PostgreSQL RLS adds a defense-in-depth layer)
- **Matched intents:** `add-ef-migration` (partial), cross-cutting refactor, tenant isolation hardening
- **Relevant skills:** `dotnet-efcore-guidelines`, `refactor-safely`, `clean-architecture-rules`, `blazor-ui-conventions`, `criticality-guardrail`, `auth-patterns`
- **Relevant rules:** `.agents/rules/efcore-migrations.md`, `.agents/rules/blazor-client.md`, `.agents/rules/tests.md`
- **Primary layers touched:** Persistence, API (controllers), Infrastructure (NSwag tooling), Blazor Client (services), Blazor Server (BFF endpoints), Build system
- **Complexity:** **XL (Extra Large)** — 6 phases spanning persistence, API, client, tooling, build, and security layers with 100+ files affected
- **I-VSD Document:** [i-vsd-cto-audit-remediation.md](../../../islamic-value-sensitive-design/i-vsd-cto-audit-remediation.md)
- **I-VSD Reviewed Input Revision:** Planning evidence packet from CTO audit session (2026-09-02)
- **I-VSD Status / Disposition:** `current` + `plan-aligned`
- **CTO Review:** Not reviewed
- **User Approval:** Approved by the explicit implementation request on 2026-09-02
- **Grill-Me Intake:** Not required — user explicitly specified all decisions across P0/P1/P2.

## 1. Executive Summary

This plan remediates all findings from the Senior CTO architecture audit across three priority tiers:

### 🔴 P0 — Critical (Architecture Blockers)
1. **Migration explosion (~1.5M lines):** Merge MariaDb into MySql provider + squash all migrations to `InitialCreate`. **~1.5M → ~200K lines**.
2. **Monolithic API client (182K lines):** Split into ~161 per-tag clients via `MultipleClientsFromFirstTagAndOperationId`.

### 🟠 P1 — High (Maintenance Tax)
3. **Controller base standardization & deduplication:** Standardize root base class as `EventControllerBase` (renaming legacy `ExploreControllerBase`), migrate 17 `TryParseConcurrencyStamp` duplicators, and formally reject generic `CrudControllerBase` and `LookupControllerBase` to preserve route transparency, explicit OpenAPI metadata, and independent controller customization for backlog features.
4. **4,190 compiler warnings ignored:** Enable `TreatWarningsAsErrors: true` with incremental category ratchet.

### 🟡 P2 — Medium-High (Risk Exposure)
5. **Untested tenant isolation gaps:** Add PostgreSQL Row-Level Security policies on tenant tables, leverage the existing `PostgresTenantSessionInterceptor` that already sets `app.current_tenant_id`, and add invariant-breaker tests.

**Non-goals:**
- Removing MariaDb as a supported runtime provider
- Moving to Kiota/Refit
- Removing migration designer files
- Backward compatibility shims (greenfield, pre-release)

## 2. Source-Grounded Current State Report

### 2.0 Pre-Flight Structural Context (Blast Radius)

```yaml
# Phase 1-2: Migration Provider Consolidation
Target: PrimaryDatabaseProviderComposition.GetMigrationsAssemblyName
Callers: PrimaryDatabaseProviderComposition.Configure → ExploreDatabaseMigrator → Event.MigrationService.Worker
Callees: Explore.Persistence.Migrations.{Provider} assemblies (resolved by name at runtime)
Impacted Flows: DatabaseMigration (Tier 1)
Test Coverage: Event.Persistence.IntegrationTests, Event.Architecture.Tests

# Phase 3: NSwag Client Split
Target: IEventApiClient + EventApiClient.g.cs
Callers: 85 service files, 17 BFF endpoints, DI registration, architecture tests
Callees: GeneratedContractPolicy.cs (hardcodes Single() on "IEventApiClient")
Test Coverage: Explore.GeneratedContracts.Tests, Explore.Blazor.Client.Tests, Event.Architecture.Tests

# Phase 4: Controller Base Standardization & Concurrency Stamp Migration
Target: EventControllerBase (renaming ExploreControllerBase) + 17 TryParseConcurrencyStamp duplicators
Callers: ASP.NET Core routing → MediatR handlers
Callees: EventControllerBase.TryParseConcurrencyStamp, User.GetPlatformUserId
Test Coverage: Event.API.IntegrationTests, Event.Architecture.Tests

# Phase 5: Warning Ratchet
Target: Directory.Build.props TreatWarningsAsErrors
Callers: Every project in the solution
Test Coverage: Build pass = verification

# Phase 6: Tenant Isolation Hardening
Target: ExploreDbContext.QueryFilters + PostgresTenantSessionInterceptor
Callers: Every repository query, every tenant-scoped entity
Callees: PostgreSQL session variables, EF Core global query filters
Test Coverage: Event.Persistence.IntegrationTests (new invariant-breaker tests)
```

### 2.1 Evidence Log

| Claim | Evidence | Confidence |
|---|---|---|
| MariaDb/MySql snapshots identical (48,677 lines each) | File comparison | High |
| `GetMigrationsAssemblyName` uses `provider.ToString()` for routing | [PrimaryDatabaseProviderComposition.cs:L92-95](../../src/Explore.Persistence/Database/PrimaryDatabaseProviderComposition.cs#L92-L95) | High |
| Zero `.razor` files inject `IEventApiClient` directly | grep + architecture test enforcement | High |
| `GeneratedContractPolicy` hardcodes `Single()` on `IEventApiClient` | [GeneratedContractPolicy.cs:L26-29](../../eng/tools/Explore.GeneratedContracts/GeneratedContractPolicy.cs#L26-L29) | High |
| 23 lookup controllers with identical GetAll/GetById pattern | Research inventory (all AllowAnonymous + OutputCache + MediatR) | High |
| 17 controllers duplicate `TryParseConcurrencyStamp` as private static | grep across src/Explore.API/Controllers | High |
| ~110 controllers inherit `ControllerBase` instead of `ExploreControllerBase` | Research analysis | High |
| `TreatWarningsAsErrors: false` in Directory.Build.props | File content verified | High |
| `WarningsNotAsErrors: nullable;MUD0002` already configured | Directory.Build.props | High |
| `PostgresTenantSessionInterceptor` already sets `app.current_tenant_id` | [PostgresTenantSessionInterceptor.cs](../../src/Explore.Persistence/Schema/ProviderPrimitives/PostgresTenantSessionInterceptor.cs) | High |
| No `CREATE POLICY` / `ENABLE ROW LEVEL SECURITY` in migration scripts | grep returned 0 results | High |
| Privacy erasure authority roles already created with `NOBYPASSRLS` | PrivacyErasureAuthorityDatabaseContract.cs | High |

### 2.2 Existing Base Controller Hierarchy

```
ControllerBase (ASP.NET Core)
  └── ExploreControllerBase (~55 controllers inherit this)
        ├── ConfigurationImportSessionsControllerBase
        ├── InstanceSettingsControllerBase
        ├── RegistrationOrderControllerBase
        ├── WebhooksControllerBase
        └── ... domain-specific bases
  └── RegistrationOrderPaymentControllerBase (inherits ControllerBase directly)

~110 controllers still inherit ControllerBase directly
```

### 2.3 Lookup Controller Pattern (23 controllers)

All lookup controllers share this exact shape:
```csharp
[ApiVersion("0.1")]
[Route("api/[controller]")]
[ApiController]
[EndpointClassification(EndpointClass.Public)]
public class {Entity}Controller(IMediator mediator) : ControllerBase
{
    [HttpGet(Name = RouteNames.Get{Entity}s)]
    [AllowAnonymous]
    [OutputCache(PolicyName = "LookupData")]
    public async Task<ActionResult<List<{Entity}ListDto>>> GetAll(CancellationToken ct) =>
        Ok(await mediator.Send(new Get{Entity}ListRequest(), ct));

    [HttpGet("{id}", Name = RouteNames.Get{Entity}ById)]
    [AllowAnonymous]
    [OutputCache(PolicyName = "DetailData")]
    public async Task<ActionResult<{Entity}Dto>> GetById(int id, CancellationToken ct) =>
        Ok(await mediator.Send(new Get{Entity}DetailsRequest { Id = id }, ct));
}
```

Differences across the 23: some have only `GetAll` (no `GetById`), a few have extra navigation methods (e.g., `GetCategories(int)`). All use `int` IDs, `IMediator`, and `OutputCache`.

### 2.4 TryParseConcurrencyStamp Duplication

Defined once in `ExploreControllerBase.cs` (L28-47) as `protected static`. **17 controllers** copy-paste it as `private static bool TryParseConcurrencyStamp` because they inherit `ControllerBase` instead of `ExploreControllerBase`. Fix: migrate those 17 controllers to inherit `ExploreControllerBase`, delete the duplicated methods.

### 2.5 Warning Configuration

Current `Directory.Build.props`:
- `TreatWarningsAsErrors: false`
- `CodeAnalysisTreatWarningsAsErrors: false`
- `EnforceCodeStyleInBuild: false`
- `EnableNETAnalyzers: true`
- `AnalysisMode: Recommended`
- `WarningsNotAsErrors: nullable;MUD0002`

### 2.6 Tenant Isolation Model

- **EF Core Query Filters:** Named filters (`Tenant`, `SoftDelete`) applied per entity in `ExploreDbContext.QueryFilters.cs`. Bypass requires explicit reason string.
- **Session Variable:** `PostgresTenantSessionInterceptor` already sets `app.current_tenant_id` on every PostgreSQL connection — the infrastructure for RLS is already in place.
- **No RLS Policies:** No `CREATE POLICY` or `ALTER TABLE ... ENABLE ROW LEVEL SECURITY` exists in any migration.
- **Privacy Erasure Authority:** PostgreSQL roles already created with `NOBYPASSRLS`.

## 3. Proposed Future State

### Structural Invariants (Non-Behavioral Delta — Phases 1-5)

1. All 5 database providers remain supported at runtime
2. Release build passes with zero warnings (TreatWarningsAsErrors: true)
3. Generated API client covers all 894 endpoints across ~161 per-tag clients
4. Zero duplicated `TryParseConcurrencyStamp` — root controller base standardized as `EventControllerBase` (renaming `ExploreControllerBase`)
5. Controllers remain concrete and explicit — generic `CrudControllerBase` and `LookupControllerBase` formally rejected to preserve OpenAPI metadata fidelity and backlog customization
6. Zero `.razor` files inject generated client interfaces

### Behavioral Requirement (Phase 6 — Tenant RLS)

- **WHEN** a PostgreSQL query executes with `app.current_tenant_id` set, **THEN** RLS policies enforce that only rows matching the tenant are visible, regardless of whether EF Core query filters are applied.
- **WHEN** `IgnoreQueryFilters()` or raw SQL bypasses EF Core filters, **THEN** RLS still prevents cross-tenant data access on RLS-enabled tables.

## 4. Non-Negotiable Constraints

- **AGENTS.md Rule #7**: EF Core migrations are generated artifacts — never hand-edit
- **AGENTS.md Rule #11**: Greenfield breaking change freedom
- **AGENTS.md Rule #12**: Tests guard true business invariants, not mock-mirroring
- **blazor-client.md**: Never hand-edit `EventApiClient.g.cs`
- **efcore-migrations.md**: Fix the source, then regenerate
- **conventional-commit**: Generated artifacts travel with their triggering commit

## 5. Architecture And Design Decisions

### Decision 1: Route MariaDb to MySql migration assembly via string override
MariaDb and MySql both use Pomelo `UseMySql()` with identical migration output. Route `MariaDb` to `MySql` assembly in `GetMigrationsAssemblyName`. Keep `PrimaryDatabaseProvider.MariaDb` enum for runtime `MariaDbServerVersion` distinction.

### Decision 2: Squash by deleting and regenerating
Greenfield with zero production databases. Delete all, regenerate single `InitialCreate` per provider/context.

### Decision 3: NSwag `MultipleClientsFromFirstTagAndOperationId` with `{controller}Client` naming
Per-tag client classes: `ActorClient`, `EventClient`, `RegistrationFormsClient`, etc. All in one `.g.cs` file.

### Decision 4: Roslyn transformer enumerates all generated client interfaces
Replace `Single()` on `IEventApiClient` with enumeration of all `[GeneratedCode("NSwag", ...)]` interface declarations.

### Decision 5: Rename ExploreControllerBase to EventControllerBase and migrate TryParseConcurrencyStamp duplicators
Rename the legacy `ExploreControllerBase` to `EventControllerBase` to align with canonical ISLAMU Event naming. Migrate all 17 controllers that duplicate `TryParseConcurrencyStamp` to inherit `EventControllerBase` instead of `ControllerBase`, and delete all 17 private copies.

### Decision 6: Formally reject generic CrudControllerBase and LookupControllerBase in favor of concrete controllers and composition
Reject generic CRUD and lookup controller base classes (`CrudControllerBase<...>`, `LookupControllerBase<...>`). Controllers are HTTP transport adapters whose responsibility is declaring explicit route templates, unique `RouteNames`, OpenAPI documentation (`[EndpointSummary]`, `[EndpointDescription]`), and mapping ProblemDetails/HAL responses. Generic base classes create a "framework inside a framework", obfuscate OpenAPI/NSwag client generation, and severely restrict customization as complex, unique backlog requirements emerge. Common behavior belongs in `EventControllerBase` (identity and concurrency stamps), domain-family base classes (when split controllers share an exact domain workflow protocol), or composition (`CommandFailurePolicy`, `IResourceAssembler`, extension methods).

### Decision 7: TreatWarningsAsErrors incremental ratchet
1. Enable `TreatWarningsAsErrors: true` in `Directory.Build.props`
2. Suppress known noisy categories via `<NoWarn>` or `<WarningsNotAsErrors>` temporarily
3. Fix warnings category-by-category, removing suppressions as each is cleared
4. Target: zero suppressions at completion

### Decision 8: PostgreSQL RLS as defense-in-depth for tenant tables
Add `CREATE POLICY` and `ENABLE ROW LEVEL SECURITY` for all tenant-scoped entity tables. Leverage the existing `PostgresTenantSessionInterceptor` that sets `app.current_tenant_id`. The EF Core query filters remain the primary enforcement; RLS is the safety net that catches `IgnoreQueryFilters()`, raw SQL, and direct database access.

## 6. Implementation Phases

---

### Phase 1: Merge MariaDb Migration Projects into MySql Provider

- **Priority:** P0 | **Effort:** S-M | **Depends on:** Nothing
- **Goal:** Delete MariaDb migration projects and route MariaDb to MySql migration assemblies.
- **Files:**
  - [DELETE] `src/Explore.Persistence.Migrations.MariaDb/` (entire project)
  - [DELETE] `src/Explore.Persistence.DataProtection.Migrations.MariaDb/` (entire project)
  - [MODIFY] `Explore.slnx`, `Event.MigrationService.csproj`, 2 Dockerfiles, `.github/workflows/test.yml`, owning test projects/tests/helpers, affected lockfiles, and live agent path contracts
  - [MODIFY] `PrimaryDatabaseProviderComposition.cs` (route MariaDb → MySql assembly names)
  - [MODIFY] `PrimaryDatabaseProviderCompositionTests.cs`, `ProviderMigrationOwnershipTests.cs` (lock routing and runtime-dialect separation)
  - [MODIFY] `docs/internal/CONFIGURATION.md`, `docs/internal/OPERATIONS.md`, `docs/internal/SELF_HOSTING.md`
  - [MODIFY] `docs/public/documentation/readme/configuration-and-operations/backup-restore-upgrade.md` (public operator parity)
- **Acceptance:** `PrimaryDatabaseProvider.MariaDb` enum still exists. `GetMigrationsAssemblyName(MariaDb, Application)` returns `"Explore.Persistence.Migrations.MySql"`. Build passes.
- **Verification:** `dotnet build --configuration Release --verbosity quiet` + `Event.Architecture.Tests`

---

### Phase 2: Squash All Provider Migrations into Single InitialCreate

- **Priority:** P0 | **Effort:** M | **Depends on:** Phase 1
- **Goal:** Replace accumulated migration history with single InitialCreate per provider/context.
- **Execution:** Treat migration deletion and regeneration as one atomic generated-artifact cutover across eight migration projects and eleven provider/context catalogs; no intermediate buildable/committable state is expected.
- **Files:**
  - [DELETE + REGENERATE] All `Migrations/*.cs` across PostgreSQL, MySql, SqlServer, Sqlite (ExploreDbContext + DataProtection + PrivacyErasureAuthority contexts)
  - [NEW] `.gitattributes` entries for `linguist-generated`
  - [MODIFY] `docs/internal/OPERATIONS.md` so every reset/generation command uses the canonical `InitialCreate` migration name
- **Acceptance:** Each provider/context has exactly 1 InitialCreate (3 files: migration, designer, snapshot). Build passes. `Event.Persistence.IntegrationTests` pass.
- **Verification:** `dotnet build --configuration Release --verbosity quiet` + `Event.Persistence.IntegrationTests`

---

### Phase 3: Split NSwag Monolithic Client into Per-Tag Clients

- **Priority:** P0 | **Effort:** L | **Depends on:** Nothing (independent of Phases 1-2)
- **Goal:** Switch NSwag to multi-client generation and update all ~102 consumer files.
- **Files:**
  - [MODIFY] `nswag.json` (operationGenerationMode + className)
  - [REGENERATED] `EventApiClient.g.cs`
  - [MODIFY] `GeneratedContractPolicy.cs` (handle multiple interfaces)
  - [MODIFY] `EventApiClient.cs` (partial hooks → DelegatingHandler or per-client partials)
  - [MODIFY] `Program.cs` + `HttpClientExtensions.cs` (multi-client DI registration)
  - [MODIFY] ~85 service files + ~17 BFF endpoint files (IEventApiClient → I{Tag}Client)
  - [MODIFY] ~9 test files (architecture + contract + naming tests)
- **Acceptance:** `IEventApiClient` no longer exists. ~161 per-tag interfaces generated. All services inject specific client. Architecture test prevents direct injection in components. Build passes.
- **Verification:** `dotnet build --configuration Release --verbosity quiet` + `Event.Architecture.Tests`

---

### Phase 4: Controller Base Standardization — EventControllerBase & Concurrency Stamp Migration

- **Priority:** P1 | **Effort:** M | **Depends on:** Nothing (independent)
- **Goal:** Standardize root API controller base class as `EventControllerBase` (renaming legacy `ExploreControllerBase`), eliminate duplicated `TryParseConcurrencyStamp` methods across 17 controllers, and explicitly preserve concrete controller authoring for all domain and lookup endpoints.

#### Sub-phase 4A: Standardize EventControllerBase and Migrate Duplicators
- **Files:**
  - [RENAME/MODIFY] `src/Explore.API/Controllers/ExploreControllerBase.cs` → `src/Explore.API/Controllers/EventControllerBase.cs`
  - [MODIFY] Existing derived controllers (~55 controllers + family bases like `InstanceSettingsControllerBase`) to inherit `EventControllerBase`
  - [MODIFY] 17 controllers duplicating `TryParseConcurrencyStamp` (change `ControllerBase` → `EventControllerBase`, delete private duplicate method):
    - `CategoryController`, `CustomPropertyDefinitionController`, `EventAgendaItemController`, `EventCustomPropertyController`, `EventDayController`, `EventParticipationController`, `EventSeriesController`, `EventSessionController`, `EventSessionCustomPropertyController`, `EventSessionGroupController`, `EventSessionLanguageController`, `EventSessionSpeakerController`, `EventSessionTemplateController`, `EventTemplateController`, `LocationController`, `LocationRoomController`, `RegistrationFormsController`
  - [MODIFY] `tests/Event.Architecture.Tests/CodeHygieneTests.cs` (update test to verify `EventControllerBase`)
- **Acceptance:**
  - `ExploreControllerBase` renamed to `EventControllerBase`.
  - Zero remaining private `TryParseConcurrencyStamp` duplicate methods (`grep -rn 'private static bool TryParseConcurrencyStamp' src/Explore.API/Controllers` returns 0).
  - All controllers requiring identity or concurrency parsing inherit `EventControllerBase`.

#### Sub-phase 4B & 4C: Architectural Decision — DROPPED / REJECTED
- **Status:** **REJECTED (Design Invariant)**
- **Rationale:**
  - **Controllers are HTTP Adapters:** In Clean Architecture + CQRS, controllers are presentation adapters. They should be declarative and explicit: defining exact routes, unique `RouteNames`, `[EndpointSummary]`, `[EndpointDescription]`, `[ProducesResponseType]`, and dispatching to MediatR.
  - **No "Framework Inside a Framework":** A generic `CrudControllerBase<...>` or `LookupControllerBase<...>` forces domain entities into rigid inheritance hierarchies. As backlog requirements arrive (unique lifecycle state transitions, Cerbos authorization policies, custom HATEOAS link policies, sub-resources, file uploads), generic base classes fight the developer, requiring hacky `base` overrides or `[NonAction]` suppressions.
  - **OpenAPI & NSwag Contract Stability:** Route names (`Name = RouteNames.Xxx`) pin OpenAPI `operationId`s. Placing action definitions on generic base classes breaks route name compile-time uniqueness and degrades OpenAPI documentation.
  - **Composition Over Inheritance:** Reusable mechanics belong in `EventControllerBase` (identity/headers), domain-family base classes (for split controllers sharing an exact domain protocol like guest/authenticated checkout), or composition (`CommandFailurePolicy`, `IResourceAssembler`, extension methods). Domain and lookup controllers remain concrete.

- **Phase verification:** `dotnet build --configuration Release --verbosity quiet` + `Event.API.IntegrationTests` + `Event.Architecture.Tests`

---

### Phase 5: Compiler Warning Ratchet — Enable TreatWarningsAsErrors

- **Priority:** P1 | **Effort:** XL | **Depends on:** Phases 1-4 (to avoid fixing warnings in code that will be refactored)
- **Goal:** Enable `TreatWarningsAsErrors: true` and systematically fix all warnings.
- **Files:**
  - [MODIFY] `Directory.Build.props` (flip `TreatWarningsAsErrors` to `true`, adjust `WarningsNotAsErrors`)
  - [MODIFY] Hundreds of source files across the solution (nullable annotations, CA fixes, IDE suggestions)
- **Strategy:**
  1. Enable `TreatWarningsAsErrors: true`
  2. Build and capture all warning codes
  3. Add temporary `<WarningsNotAsErrors>` for all failing categories
  4. Fix one warning category at a time, removing its suppression when clear
  5. Priority: nullable warnings → security analyzers (CA2xxx) → design analyzers → style
- **Acceptance:** `TreatWarningsAsErrors: true` with zero `<WarningsNotAsErrors>` suppressions remaining (or a documented minimal allowlist with justification). Build passes with zero warnings.
- **Phase verification:** `dotnet build --configuration Release --verbosity quiet` (zero warnings = green)

> [!IMPORTANT]
> Phase 5 is deliberately sequenced AFTER Phases 1-4 to avoid wasting effort fixing warnings in code that will be deleted, refactored, or regenerated. The ratchet locks in the cleaner codebase.

---

### Phase 6: Tenant Isolation Hardening — PostgreSQL Row-Level Security

- **Priority:** P2 | **Effort:** L | **Depends on:** Phase 2 (clean migration baseline needed), Phase 5 (warnings clean)
- **Goal:** Add PostgreSQL RLS policies on tenant-scoped tables as defense-in-depth. Leverage the existing `PostgresTenantSessionInterceptor` that already sets `app.current_tenant_id`.
- **Criticality:** Tier 1 (Security) — requires adversarial invariant-breaker tests
- **Files:**
  - [NEW] EF Core migration adding RLS policies: `CREATE POLICY tenant_isolation ON {table} USING (tenant_id = current_setting('app.current_tenant_id')::uuid)` + `ALTER TABLE {table} ENABLE ROW LEVEL SECURITY` for all tenant-scoped entity tables
  - [NEW] Invariant-breaker tests:
    - Test that `IgnoreQueryFilters()` on a tenant entity STILL returns only the current tenant's rows when running on PostgreSQL with RLS enabled
    - Test that raw SQL without a WHERE clause returns only current tenant's rows
    - Test that setting `app.current_tenant_id` to a different tenant ID isolates correctly
  - [MODIFY] `PrimaryDatabaseProviderComposition.cs` to ensure runtime role uses a PostgreSQL role with `NOBYPASSRLS`
  - [MODIFY] Documentation for tenant isolation architecture

- **Acceptance:**
  - All tenant-scoped entity tables have RLS policies in PostgreSQL
  - `PostgresTenantSessionInterceptor` continues to set `app.current_tenant_id` (already working)
  - Invariant-breaker tests prove that bypassing EF Core filters does NOT bypass RLS
  - Runtime PostgreSQL role does not have `BYPASSRLS` privilege
  - Non-PostgreSQL providers (SQLite, MySQL, SQL Server) continue to rely on EF Core query filters only
- **Phase verification:** `dotnet build --configuration Release --verbosity quiet` + `Event.Persistence.IntegrationTests`

> [!WARNING]
> Phase 6 is Tier 1 Security work. The implementing agent must follow the criticality-guardrail skill, write invariant-breaker tests FIRST (test fails without RLS, passes after), and verify with adversarial scenarios.

## 7. Testing Strategy

| Phase | Test Project | What It Proves |
|---|---|---|
| 1 | `Event.Architecture.Tests` | Project structure valid after MariaDb removal |
| 2 | `Event.Persistence.IntegrationTests` | Database creates from scratch with single InitialCreate |
| 3 | `Event.Architecture.Tests` | Architecture rules + generated contract structure |
| 4 | `Event.API.IntegrationTests` + `Event.Architecture.Tests` | API endpoints work, controller hierarchy valid |
| 5 | Release build (zero warnings) | No warnings remain |
| 6 | `Event.Persistence.IntegrationTests` (new invariant-breakers) | RLS prevents cross-tenant access even with IgnoreQueryFilters |

## 8. Documentation, Configuration, And Operations Impact

### Changelog Strategy
All phases use `Changelog: skip` — this is internal architecture remediation with no user/operator-visible behavior change (except Phase 6 which is defense-in-depth, not a new user feature).

### Documentation Updates
- `docs/internal/CONFIGURATION.md`, `docs/internal/OPERATIONS.md`, `docs/internal/SELF_HOSTING.md`: MariaDb shares MySql migrations (Phase 1)
- `docs/public/documentation/readme/configuration-and-operations/backup-restore-upgrade.md`: public operator parity (Phase 1)
- `.gitattributes`: `linguist-generated` markers (Phase 2)
- `docs/internal/QUICK_REFERENCE.md`: Update tenant isolation architecture (Phase 6)

## 9. I-VSD & Moral Boundaries

- **Report:** [i-vsd-cto-audit-remediation.md](../../../islamic-value-sensitive-design/i-vsd-cto-audit-remediation.md)
- **Phases 1-5:** Non-applicable (pure refactor)
- **Phase 6 (Tenant RLS):**
  - `IVSD-F002`: Provider has an Islamic obligation to protect tenant data from cross-contamination. RLS is the strongest available defense against developer error, raw SQL, and query filter bypass. This aligns with the principle of *amānah* (trustworthiness in safeguarding what is entrusted).
  - `IVSD-M002`: Implement RLS on ALL tenant-scoped tables, not selectively. Verify with invariant-breaker tests.

## 10. Security, Authorization, Privacy, And Abuse Considerations

- **Trust Boundaries:** Controller consolidation (Phase 4) and warning ratcheting (Phase 5) maintain existing authentication and authorization policies (`[Authorize]`, `[AllowAnonymous]`, Cerbos policies).
- **Tenant Isolation Defense-in-Depth (Phase 6):**
  - **Threat:** Developer accidentally uses `IgnoreQueryFilters()` or raw SQL that bypasses tenant isolation → cross-tenant data leak.
  - **Mitigation:** PostgreSQL Row-Level Security (RLS) enforces tenant boundaries at the PostgreSQL engine level, independent of application code.
  - **Database Role Privilege:** Runtime PostgreSQL role must have `NOBYPASSRLS`. Only the migration role may have `BYPASSRLS` for DDL schema management.
  - **Session Setting Injection:** `PostgresTenantSessionInterceptor` uses parameterized `set_config('app.current_tenant_id', ...)` preventing SQL injection in session variables.
- **Auditability & Abuse:** No changes to audit logging or rate-limiting middleware.

## 11. Multi-Tenancy, Federation, Localization, Accessibility, And Product Considerations

| Concern | Status | Rationale |
|---|---|---|
| Multi-Tenancy | **Applicable** | Phase 6 strengthens tenant isolation with PostgreSQL RLS defense-in-depth. Other providers continue with EF Core query filters. |
| Federation | Not Applicable | ATProto federation settings and endpoints are unaffected. |
| Localization | Not Applicable | No localization keys or translation workflows are altered. |
| Accessibility | Not Applicable | No UI components or Razor files are changed. |
| Product | Not Applicable | No user-facing behavior changes; internal architecture refactoring only. |

## 12. Observability And Operations

- **Logging & Tracing:** No change to OpenTelemetry or Serilog instrumentation.
- **Health / Readiness Probes:** Readiness probes (`ExploreApiReadinessProbe`) continue validating database connectivity.
- **Deployment & Recovery:**
  - Migrations are applied via `Event.MigrationService` on startup.
  - Database squash in Phase 2 requires dropping and recreating dev databases (greenfield mode).
  - PostgreSQL RLS policies in Phase 6 execute idempotently via migration scripts.

## 13. Migration And Compatibility Plan

- **Database / Schema Migration:**
  - Phase 1 routes MariaDb runtime to MySql migration assembly.
  - Phase 2 deletes existing development migration history and regenerates single `InitialCreate` per provider. Safe because the platform is pre-release with zero production databases.
  - Phase 6 adds RLS policies via standard EF Core migration for PostgreSQL.
- **Generated Contracts & API Clients:**
  - Phase 3 splits `IEventApiClient` into ~161 per-tag interfaces (`IActorClient`, `IEventClient`, etc.).
  - All consumer services (~85 files) and BFF endpoints (~17 files) are updated in the same phase.
- **Breaking Changes:**
  - `IEventApiClient` is eliminated.
  - Per AGENTS.md Rule #11, backward compatibility shims are explicitly prohibited.

## 14. Risk Register

| Risk | Phase | Likelihood | Impact | Mitigation | Detection Signal | Owner/Task |
|---|---|---|---|---|---|---|
| NSwag tag produces invalid C# identifier | 3 | Low | Medium | Verify after generation; most tags are clean PascalCase | Roslyn build error | Phase 3, Task 3.2 |
| Migration squash generates wrong schema | 2 | Low | High | Compare InitialCreate with model; verify integration tests | `Event.Persistence.IntegrationTests` failure | Phase 2 |
| 4,190 warnings overwhelm Phase 5 | 5 | Medium | Medium | Incremental category ratchet; suppress → fix → unsuppress | Build warning counts | Phase 5 |
| RLS migration breaks non-PostgreSQL providers | 6 | Low | Low | RLS is PostgreSQL-only; migration uses provider-conditional SQL | Provider integration tests | Phase 6, Task 6.3 |

## 15. Success Metrics And Definition Of Done

| Metric | Before | After | Verification |
|---|---|---|---|
| Migration code (lines) | ~1.5M | ~200K | `wc -l` on migration directories |
| API client classes | 1 × 182K lines | ~161 per-tag clients | File inspection of `EventApiClient.g.cs` |
| MariaDb migration projects | 2 | 0 (routes to MySql) | Solution project count |
| Duplicate TryParseConcurrencyStamp | 17 copies | 0 (in `EventControllerBase`) | `grep` check in `src/Explore.API/Controllers` |
| Root controller base | `ExploreControllerBase` | `EventControllerBase` | File & class name check |
| Compiler warnings | 4,190 | 0 | `dotnet build` with zero warnings |
| TreatWarningsAsErrors | false | true | `Directory.Build.props` property check |
| Tenant RLS tables | 0 | All tenant-scoped | Invariant-breaker test pass |

Each phase closes with:
1. `dotnet build --configuration Release --verbosity quiet`
2. At most one selected test project execution
3. Phase-owned Conventional Commit with exact commit contract

## 16. Implementation Agent Contract — KEEP DEV DOCS CURRENT

Future implementation agents MUST:
1. At first implementation start or cold resume, read task-owned context and the current task first; retrieve only the plan heading needed for the current phase.
2. Keep a `path + heading/symbol + revision` ledger. Do not reread unchanged artifacts.
3. Start from the highest-priority unchecked task unless overridden by user.
4. Treat `tasks.md` as the hot execution ledger: check substantial tasks immediately, reconcile small tasks by phase end.
5. Keep implementation-task, phase-verification, and phase-commit checkboxes separate.
6. Update task status summary, completed count, priority, and `Last Updated` when state changes.
7. Update context after a completed phase, decision, blocker, failed validation, or handoff.
8. Update the plan only when scope, architecture, phase order, or validation strategy changes.
9. Before every phase commit, reconcile the phase-owned path list against the dirty tree.
10. Run phase verification only after all phase tasks, with one Release build and at most one test project.
11. Use the approved self-sufficient commit contract directly without loading `conventional-commit`.
12. Never report completion when repository reality, the commit file list, and the task ledger disagree.
13. Every implementation summary must teach what changed, architectural patterns, important files, and verification evidence.

## 17. Progress Reporting Contract

Every implementation response MUST follow this exact shape:

```text
Implemented: developer teaching summary
Verified: exact evidence
Remaining: incomplete or deferred work
Next: recommended next slice
Docs updated: yes/no with reason
```

## 18. Potential Risks & Unknowns

- **Phase 3 (NSwag Split):** The interaction between NSwag's `MultipleClientsFromFirstTagAndOperationId` and the custom Roslyn transformer in `eng/tools/Explore.GeneratedContracts` is the most sensitive seam. The transformer's `Single()` invocation on `IEventApiClient` must be refactored to discover inputs across all generated client interfaces before regeneration.
- **Phase 5 (Warning Ratchet):** Tackling 4,190 warnings can expand if not bounded by strict category suppression. The ratchet pattern (enable `TreatWarningsAsErrors: true`, add temporary suppressions, clear category by category) is mandatory to prevent getting stuck in a massive, unreviewable diff.
- **Phase 6 (PostgreSQL RLS):** Because PostgreSQL RLS applies only to PostgreSQL, migrations must use raw SQL wrapped in provider checks (`if (activeProvider == "Npgsql")`). Any query executing without the session interceptor (e.g. background job without tenant context) must be properly accounted for with explicit system bypass credentials.
