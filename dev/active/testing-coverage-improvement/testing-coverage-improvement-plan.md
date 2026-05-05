<!-- ABOUTME: Implementation plan for enterprise-grade testing coverage improvements across all Explore test layers. -->
<!-- ABOUTME: Captures verified current state, future testing taxonomy, phased tasks, risks, and success metrics. -->

# Testing Coverage Improvement — Implementation Plan

Last Updated: 2026-05-05

## Executive Summary

This plan upgrades the repository's existing TUnit-based test suite from broad-but-uneven coverage to an enterprise-grade, risk-based testing system across Clean Architecture layers. The priority is not raw test count; it is confidence in critical behavior: tenant isolation, authorization, BFF token/session security, CQRS handler rules, PostgreSQL persistence behavior, HAL affordances, idempotency/outbox behavior, Blazor UI interactions, and full-stack browser flows.

Backward compatibility is explicitly out of scope because the project is in development mode. Do not add compatibility-preservation tests, legacy-shape assertions, or tests that freeze incorrect current behavior. Delete, rewrite, or relocate tests that are unnecessary, boundary-violating, brittle, or only assert implementation noise.

## Current State Analysis

### Verified planning and repository conventions

- `dev/active/README.md` defines the active work structure: `dev/active/<task-name>/<task-name>-plan.md`, `<task-name>-context.md`, and `<task-name>-tasks.md`.
- `CLAUDE.md` requires durable planning under `dev/active/`, two-line ABOUTME headers, project-level test commands, and clean architecture discipline.
- `docs/TESTING.md` is the canonical testing guide: TUnit is the standard, test projects must be run individually, architecture tests are CI gates, integration/E2E tests must use real infrastructure where behavior depends on infrastructure, and solution-level `dotnet test` is not used.
- `docs/ARCHITECTURE.md`, `docs/SECURITY.md`, `docs/API.md`, and `docs/OPERATIONS.md` define the critical behavior that testing must protect: CQRS handlers, authorization pipeline, BFF cookie/token forwarding, tenant resolution, API middleware order, PostgreSQL-backed persistence, setup-secret handling, health/readiness, HAL links, idempotency, caching, and rate limiting.

### Verified test project landscape

The repository currently contains these nine test projects:

- `Event.Domain.UnitTests/Event.Domain.UnitTests.csproj`
- `Event.Application.UnitTests/Event.Application.UnitTests.csproj`
- `Event.Persistence.IntegrationTests/Event.Persistence.IntegrationTests.csproj`
- `Event.API.IntegrationTests/Event.API.IntegrationTests.csproj`
- `Event.Architecture.Tests/Event.Architecture.Tests.csproj`
- `Explore.Blazor.Client.Tests/Explore.Blazor.Client.Tests.csproj`
- `Explore.Blazor.IntegrationTests/Explore.Blazor.IntegrationTests.csproj`
- `Explore.Blazor.Client.E2ETests/Explore.Blazor.Client.E2ETests.csproj`
- `Explore.Secrets.UnitTests/Explore.Secrets.UnitTests.csproj`

### Verified strengths to preserve

- `Event.Architecture.Tests/CleanArchitectureTests.cs`, `ProjectionLayerBoundaryTests.cs`, `AuthorizationParityTests.cs`, `AccessibilityConventionTests.cs`, and `BlazorClientArchitectureTests.cs` already provide strong static guardrails.
- `Event.API.IntegrationTests` already has an advanced host-profile model documented in `docs/TESTING.md`: Contract, RealRuntime, and Stress profiles.
- `Event.Persistence.IntegrationTests/Fixtures/PostgreSqlContainerFixture.cs` already uses PostgreSQL Testcontainers, migrations, lookup seeding, and Respawn reset.
- `Explore.Blazor.Client.Tests/Common/BlazorTestContext.cs` already centralizes bUnit/MudBlazor/JSInterop/auth test setup.
- `Explore.Blazor.IntegrationTests/Fixtures/BlazorBffWebApplicationFactory.cs`, `SecurityBlazorBffWebApplicationFactory.cs`, and `BffKeycloakFixture.cs` already support BFF WebApplicationFactory and Keycloak-backed security testing.
- `Explore.Blazor.Client.E2ETests/Fixtures/AppHostFixture.cs`, `PlaywrightFixture.cs`, and `PostgreSqlContainerFixture.cs` already provide the skeleton for Aspire + Playwright + PostgreSQL E2E flows.

### Verified gaps and implementation targets

These findings are based on direct repository reads/searches and a direct-reference/name inventory heuristic from the prior testing audit. The heuristic identifies likely coverage gaps but must be triaged by risk before creating tests.

1. **Critical E2E coverage is mostly scaffolded, not active.** `Explore.Blazor.Client.E2ETests/Flows/CriticalFlows/TenantIsolationFlowTests.cs`, `RegistrationFlowTests.cs`, `BffTokenForwardingChainFlowTests.cs`, and `AuthorizationEnforcementFlowTests.cs` are skip-gated. `Explore.Blazor.Client.E2ETests/Flows/SmokeTests.cs` has a commented test. `SidebarLayoutVisualTests.cs` contains skipped visual-baseline scaffolding.
2. **Application unit test boundaries are blurred.** `Event.Application.UnitTests/Event.Application.UnitTests.csproj` references `Explore.API` and `Explore.Infrastructure`, which means this project is not a pure Application-layer unit-test boundary.
3. **Infrastructure test intent is inconsistent.** `Explore.Infrastructure/Explore.Infrastructure.csproj` exposes internals to `Explore.Infrastructure.Tests`, but no `Explore.Infrastructure.Tests/Explore.Infrastructure.Tests.csproj` exists.
4. **Persistence integration coverage mixes real PostgreSQL and EF InMemory.** Real PostgreSQL fixtures exist and should remain the standard for relational behavior. EF InMemory may remain only for explicit fast contract/unit-ish tests, not for query filters, constraints, indexes, translation, or transaction semantics.
5. **API smoke coverage is not a substitute for semantic contract coverage.** Broad smoke/status-class tests exist, but high-risk controllers and middleware behavior need targeted assertions around authorization, tenant isolation, HAL links, ProblemDetails, idempotency, rate limiting, ETags/output cache, and route names.
6. **Blazor component coverage is broad but uneven.** Existing bUnit infrastructure is strong, but high-risk dialogs, admin surfaces, wrappers, validators, settings/footer/theme flows, and accessibility behavior need risk-based expansion.
7. **Skip/commented-test governance is weak.** Important tests are disabled because infrastructure is not deterministic yet; this should become an explicit category/nightly policy, not permanent `[Skip]` drift.

## Proposed Future State

### Test taxonomy

Adopt and document a strict taxonomy that maps tests to risk and runtime cost:

| Layer | Project(s) | Purpose | Infrastructure |
|---|---|---|---|
| Domain Unit | `Event.Domain.UnitTests` | Pure invariants, value behavior, domain services | None |
| Application Unit | `Event.Application.UnitTests` | CQRS handlers, validators, policies, cache invalidation, repository-port interactions | Fakes/substitutes only; no API/Infrastructure dependencies |
| Infrastructure Unit/Integration | `Explore.Infrastructure.Tests` if created, existing integration projects where appropriate | provider/resolver/outbox-dispatch behavior that does not belong in Application tests | Fakes for unit behavior; real services only when needed |
| Persistence Integration | `Event.Persistence.IntegrationTests` | EF mappings, migrations, repositories, query filters, constraints, transaction behavior | PostgreSQL Testcontainers + Respawn |
| API Contract | `Event.API.IntegrationTests` Contract host | serialization, HAL, ProblemDetails, content negotiation, route names | WebApplicationFactory + documented Contract profile |
| API RealRuntime | `Event.API.IntegrationTests` RealRuntime host | PostgreSQL-backed tenant/auth/persistence behavior | PostgreSQL Testcontainers |
| API Stress | `Event.API.IntegrationTests` Stress host | rate limiting, retry-after, concurrency-sensitive behavior | PostgreSQL Testcontainers + enabled limiters |
| BFF Integration | `Explore.Blazor.IntegrationTests` | cookie auth, token forwarding, tenant header propagation, YARP/setup-secret behavior | WebApplicationFactory, test auth, Keycloak only for OIDC-specific tests |
| Blazor Component | `Explore.Blazor.Client.Tests` | bUnit rendering, events, services, accessibility structure, HAL-driven UI | `BlazorTestContext`, strict JSInterop, test doubles |
| Full E2E Nightly | `Explore.Blazor.Client.E2ETests` | critical user journeys through Aspire, browser, BFF, API, DB | Aspire AppHost + PostgreSQL + Keycloak + Playwright |
| Manual Visual | `Explore.Blazor.Client.E2ETests` visual suite | approved visual baseline checks | Manual/nightly only after deterministic storage exists |

### Testing principles

- Prefer one behavior per test and descriptive TUnit names.
- Use TUnit-native async assertions and lifecycle patterns; use `ClassDataSource` for expensive fixtures with explicit reset/isolation.
- Use PostgreSQL Testcontainers for relational behavior; do not rely on EF InMemory for query filters, constraints, transactions, or translation.
- Use `WebApplicationFactory` and `ConfigureTestServices` to replace external services at the test boundary.
- Use bUnit semantic/user-visible assertions, `WaitForAssertion`/`WaitForState`, explicit services, and strict/configured JSInterop.
- Use Playwright semantic locators and web-first assertions; collect trace/screenshot/video artifacts for E2E failures or retries only, and close browser contexts so videos flush.
- Use Aspire distributed-application testing patterns for closed-box full-app tests, but keep those suites in nightly/manual lanes rather than the PR-fast lane.
- Evaluate TUnit's dedicated ASP.NET Core, Playwright, and Aspire integrations only where they simplify existing fixtures without weakening repository-specific setup; do not replace proven local fixtures mechanically.
- Preserve only useful tests. Delete or rewrite tests that only assert implementation details, duplicate stronger tests, encode obsolete behavior, or exist only for backward compatibility.

## Implementation Phases

### Phase 0 — Test taxonomy, governance, and cleanup baseline

**Goal:** Establish the rules that prevent future test sprawl before adding more tests.

**Clean Architecture layer:** Cross-cutting governance.

**Tasks:**

1. Update `docs/TESTING.md` with the taxonomy table above, category naming, PR/nightly/manual expectations, and explicit no-backward-compatibility policy.
2. Add or extend architecture/code-hygiene coverage in `Event.Architecture.Tests/CodeHygieneTests.cs` to detect commented-out `[Test]` markers and untracked `[Skip]` usage.
3. Convert existing commented tests, including E2E smoke and route/API-client naming TODO tests, into either active tests, tracked skipped tests with category/expiry rationale, or deleted tasks if obsolete.
4. Define skip governance: every skip must include a concrete blocker, owning phase, and removal condition.
5. Create a traceability matrix section in `docs/TESTING.md` mapping critical risks to projects and representative tests.

**Acceptance criteria:**

- No commented-out `[Test]` remains unless explicitly documented as generated/reference text.
- Every `[Skip]` has a removal condition and category rationale.
- `docs/TESTING.md` explains which test suites run on PR, merge, nightly, and manual visual workflows.
- No backward-compatibility test category is introduced.

**Dependencies:** none.

**Estimated effort:** 1-2 days.

**Skills:** architecture tests, TUnit, documentation.

### Phase 1 — Activate critical E2E/nightly confidence

**Goal:** Turn the existing E2E scaffolding into a deterministic nightly gate for the highest-risk user journeys.

**Clean Architecture layer:** Presentation/BFF/API/Persistence end-to-end.

**Tasks:**

**Implementation update — 2026-05-05:** The first Phase 1 increment is complete and runtime-verified: `SmokeTests.cs` now has an active `/events` frontend-load smoke test using the accessible `Explore Events` heading, anonymous `/auth/status` is asserted as JSON with `isAuthenticated = false`, and `PlaywrightFixture.cs` records trace/screenshot/video artifacts while closing browser contexts. The filtered smoke lane passes with `dotnet test --project Explore.Blazor.Client.E2ETests/Explore.Blazor.Client.E2ETests.csproj --configuration Release --verbosity quiet --treenode-filter "/*/*/SmokeTests/*"`.

**Implementation update — 2026-05-05:** The AppHost PostgreSQL prerequisite is now wired for E2E. `AppHostFixture` remains parameterless for TUnit `ClassDataSource`, privately owns a `PostgreSqlContainerFixture`, initializes it before the Aspire AppHost, and injects its connection string into `event-migrationservice`, `explore-api`, and `explore-blazor` via resource-specific environment variables. The migration service receives both `ConnectionStrings__DefaultConnection` and `ConnectionStrings__EventMigrationService`; API and Blazor receive `ConnectionStrings__DefaultConnection`. Tests use wrapper methods (`ResetDatabaseAsync`, `CreateDbContext`) rather than a public disposable database property so TUnit cannot double-dispose the container. `Explore.Blazor.Client.E2ETests.csproj` now copies `docker/keycloak/ISLAMU-realm.test.json` into `TestAssets/` for the upcoming deterministic Keycloak/BFF-cookie auth fixture. The E2E project build and filtered smoke lane both pass after this refactor. The remaining Phase 1 blockers are deterministic Keycloak/BFF-cookie auth state and then unskipping tenant/authenticated critical flows.

**Implementation update — 2026-05-05:** Shared AppHost database reset serialization is now in place. E2E classes that call `AppHostFixture.ResetDatabaseAsync()` use `[NotInParallel("E2EAppHostDb")]`, matching the repository's existing `RealRuntimeDb`, `StressDb`, and `PersistenceDb` resource-lock pattern. `BrowserParallelLimit` remains available for browser resource control, while the database-reset lock prevents concurrent Respawn reset/seed/browser flows against the same AppHost-owned PostgreSQL container. The E2E project build and filtered smoke lane both still pass after adding the lock.


**Implementation update — 2026-05-05:** Deterministic Keycloak/BFF-cookie browser auth is now runtime-verified in the E2E smoke lane. `Explore.Blazor.Client.E2ETests/Fixtures/BffKeycloakFixture.cs` starts the deterministic Keycloak 26.1.2 container and imports `TestAssets/ISLAMU-realm.test.json`; `AppHostFixture` initializes Keycloak before the Aspire AppHost and injects Keycloak authority/metadata/client configuration into `explore-blazor` and API validation settings into `explore-api`. `BffCookieAuthHelper` drives the real browser OIDC form login for `test-user`, verifies `/auth/status` returns `isAuthenticated = true`, asserts the BFF issued an HttpOnly `.AspNetCore.Cookies` cookie, and scans browser local/session storage for token-shaped values. The Blazor AppHost resource explicitly clears Infisical bootstrap credentials for E2E so real `/keycloak` secrets cannot override the deterministic test realm secret. `SmokeTests.AuthStatus_KeycloakLogin_ReturnsAuthenticatedWithServerCookieOnly` is active and the filtered smoke lane passes 3/3.

**Implementation update — 2026-05-05:** `TenantIsolationFlowTests.cs` is now active against the Aspire-hosted API tenant boundary. The flow seeds completed multi-tenant bootstrap state plus tenant A/B data, calls `explore-api` directly with `X-Tenant-Slug` for each tenant, and asserts tenant A's public event is absent from tenant B's response. This E2E exposed a real cross-tenant cache leak in `GetEventListRequestHandler`: the `events:list` HybridCache key did not include `ITenantContext.TenantId`, so tenant B could receive tenant A's cached event list. The handler cache key now includes the tenant id, and `GetEventListRequestHandlerTests.Handle_WithSameRequestForDifferentTenants_UsesTenantScopedCacheEntries` protects that behavior. The handler TUnit slice and tenant-isolation E2E both pass.

1. Add a deterministic E2E auth fixture/helper under `Explore.Blazor.Client.E2ETests/Fixtures/` to create or reuse a seeded Keycloak browser state without leaking tokens to the browser outside the BFF model.
2. Activate `Explore.Blazor.Client.E2ETests/Flows/SmokeTests.cs` with meaningful assertions for frontend load and anonymous `/auth/status` returning `isAuthenticated = false`.
3. Activate `TenantIsolationFlowTests.cs` with seeded tenant data and assertions that browser-visible data cannot cross tenant boundaries.
4. Activate `AuthorizationEnforcementFlowTests.cs` with a low-privilege authenticated state and assertions for UI affordance removal plus API/BFF enforcement.
5. Activate `BffTokenForwardingChainFlowTests.cs` to verify browser → BFF cookie → YARP token forwarding → API authorization without exposing tokens to WASM.
6. Activate `RegistrationFlowTests.cs` only after the auth/session fixture is stable; assert user-visible registration outcome and database/API side effects.
7. Keep `SidebarLayoutVisualTests.cs` manual or nightly-only until approved screenshot storage and baseline review rules exist.
8. Configure Playwright artifacts: trace on retry/failure, screenshots, videos where useful, and CI artifact retention.

**Acceptance criteria:**

- Core E2E tests are not permanently skip-gated; they run under an explicit Nightly category.
- E2E uses `AppHostFixture`, `PlaywrightFixture`, and `PostgreSqlContainerFixture` rather than mocks.
- Tests wait for Aspire resource readiness and use semantic Playwright locators where possible.
- Failure artifacts are available for triage.

**Dependencies:** stable Aspire local/nightly environment, deterministic Keycloak seed/login state, PostgreSQL container readiness.

**Estimated effort:** 3-5 days.

**Skills:** Aspire, Playwright, BFF auth, Testcontainers.

### Phase 2 — Restore Application unit-test purity and high-risk handler coverage

**Goal:** Make Application tests fast, layer-pure, and focused on business behavior.

**Clean Architecture layer:** Application.

**Tasks:**

1. Split tests in `Event.Application.UnitTests` that directly depend on `Explore.API` or `Explore.Infrastructure` into the proper API, BFF, Infrastructure, or integration test project.
2. Remove `Explore.API` and `Explore.Infrastructure` references from `Event.Application.UnitTests/Event.Application.UnitTests.csproj` once relocated tests no longer require them.
3. Prioritize handler/validator coverage for role ownership, tenant/group membership, settings lock/reset/unlock/batch updates, footer governance, event lifecycle mutation, registration update, and authorization decisions.
4. For each high-risk command/query handler, add tests for success, not found, invalid request, forbidden/wrong tenant, repository effects, domain event/outbox behavior where applicable, and cache invalidation.
5. For each high-risk validator, add valid/invalid matrix tests with domain-specific edge cases.
6. Use manual validator instantiation and repository-port substitutes; do not introduce `ExploreDbContext`, API controllers, or Infrastructure internals into Application tests.

**Acceptance criteria:**

- Application unit tests do not reference API or Infrastructure projects.
- Handler tests assert behavior and side effects through Application-owned contracts.
- Validator tests cover meaningful business constraints, not only null/empty happy paths.
- Architecture tests remain green after moving boundary-violating tests.

**Dependencies:** inventory of tests currently relying on API/Infrastructure types.

**Estimated effort:** 5-8 days.

**Skills:** CQRS/MediatR, Clean Architecture, TUnit, NSubstitute.

### Phase 3 — API and BFF semantic integration coverage

**Goal:** Replace broad safety-net assertions with targeted semantic checks at HTTP and BFF boundaries.

**Clean Architecture layer:** API and Blazor BFF composition roots.

**Tasks:**

1. Expand API Contract-profile tests for route names, HAL links, ProblemDetails shape, content negotiation, XML docs/OpenAPI-sensitive contracts, and `Prefer` handling.
2. Expand RealRuntime-profile tests for tenant isolation, EF-backed authorization decisions, idempotency, output cache/ETag behavior, and write-side persistence effects.
3. Expand Stress-profile tests for rate limiting, retry-after headers, setup-secret limiter behavior, and concurrency-sensitive no-5xx guarantees.
4. Reactivate route-name/API-client naming tests only after route constants and NSwag operationIds are intentionally stabilized; do not add backward-compatibility assertions.
5. Add BFF integration tests for `BffAuthCookieStore`, `TokenRefreshCookieEvents`, `DynamicAuthSchemeManager`, `BffAdminClaimsTransformation`, `YarpProxyExtensions`, setup-secret stripping/replacement, tenant header forwarding, logout, `/auth/status`, and readiness behavior.
6. Use EF InMemory only where `docs/TESTING.md` documents the Contract or BFF profile as intentionally not testing relational behavior. Use PostgreSQL-backed tests whenever persistence semantics matter.

**Acceptance criteria:**

- API tests assert exact business/contract outcomes, not only non-500 or broad status classes.
- BFF tests prove tokens remain server-side and proxy behavior is enforced server-side.
- Setup-secret and tenant forwarding tests cover both allowed and rejected paths.
- Route/HAL tests fail when route names or affordances drift from the intended contract.

**Dependencies:** stabilized route constants/operationIds for naming tests; existing WebApplicationFactory fixtures.

**Estimated effort:** 5-8 days.

**Skills:** ASP.NET Core integration testing, WebApplicationFactory, auth/BFF, HAL, rate limiting.

### Phase 4 — PostgreSQL persistence and infrastructure hardening

**Goal:** Cover relational behavior with the same database engine used in production-like paths.

**Clean Architecture layer:** Persistence and Infrastructure.

**Tasks:**

1. Add PostgreSQL integration tests for high-risk repositories and services not yet covered deeply: outbox, idempotency, external API keys, footer/settings, role/organization membership, registration policies, templates, custom-property governance, and tenant settings.
2. Add EF metadata tests for tenant filters, soft-delete filters, required properties, unique indexes, cascade behaviors, owned/value conversions, and lookup seed expectations.
3. Add migration smoke coverage that runs migrations against PostgreSQL Testcontainers and verifies no `EnsureCreated()` shortcut is used in migration-backed fixtures.
4. Keep `DataProtectionKeyPersistenceTests.cs` EF InMemory usage only if documented as unit-ish data-protection persistence behavior; add PostgreSQL coverage if schema/provider behavior matters.
5. Port `CustomPropertyOptionLifecycleRepositoryTests.cs` behavior to PostgreSQL if it is intended to validate repository/query-filter behavior; otherwise relocate/rename it as non-relational unit-style coverage.
6. Resolve the stale `InternalsVisibleTo` mismatch in `Explore.Infrastructure/Explore.Infrastructure.csproj`: either create `Explore.Infrastructure.Tests/Explore.Infrastructure.Tests.csproj` for infrastructure unit/provider tests or remove the stale exposure if unnecessary.

**Acceptance criteria:**

- Relational tests use PostgreSQL Testcontainers and Respawn reset.
- EF InMemory is not used for relational behavior, query filters, indexes, constraints, or translation.
- Infrastructure internals exposure matches an existing test project or is removed.
- Persistence tests remain deterministic under project-level test runs.

**Dependencies:** Docker/Testcontainers availability; repository risk triage.

**Estimated effort:** 5-10 days.

**Skills:** EF Core, PostgreSQL, Testcontainers, Respawn, infrastructure provider testing.

### Phase 5 — Blazor component, accessibility, and design-system coverage

**Goal:** Improve UI confidence without turning bUnit tests into brittle MudBlazor snapshots.

**Clean Architecture layer:** Blazor client presentation.

**Tasks:**

1. Add bUnit tests for high-risk event/session dialogs, admin API key/footer/theme/settings pages, login prompts, review flows, language picker behavior, wrapper components, and validators.
2. Assert user-visible behavior, semantic roles, labels, validation messages, disabled/loading states, HAL-gated affordances, and callbacks.
3. Use `Explore.Blazor.Client.Tests/Common/BlazorTestContext.cs` and explicit service registration; keep JSInterop strict/configured.
4. Prefer semantic assertions and targeted `Find`/`FindAll` checks; use `MarkupMatches` only for stable semantic fragments, not whole MudBlazor-generated DOM snapshots.
5. Restore or replace the skipped retry-button accessibility test in `Explore.Blazor.Client.Tests/Accessibility/SharedComponentAccessibilityTests.cs` after resolving the AppButton/MudBlazor v9 wrapper issue.
6. Evaluate adding an axe-compatible accessibility engine for a small set of stable rendered pages/components; if not compatible, strengthen structural accessibility tests instead.

**Acceptance criteria:**

- High-risk UI flows have interaction tests for render, validation, submit/cancel, loading/error, and auth/HAL affordance states.
- Tests do not rely on private reflection or brittle third-party CSS internals except documented wrapper/design-system contracts.
- Accessibility checks cover roles, labels, focus-relevant affordances, alert/error semantics, and core wrapper behavior.

**Dependencies:** MudBlazor v9 wrapper stability; component risk inventory.

**Estimated effort:** 4-7 days.

**Skills:** bUnit, MudBlazor, accessibility, design system, Blazor BFF service-layer patterns.

### Phase 6 — CI matrix, artifacts, and durable reporting

**Goal:** Make the improved test suite operationally maintainable.

**Clean Architecture layer:** Cross-cutting delivery.

**Tasks:**

1. Define a project-level CI matrix; never use solution-level `dotnet test`.
2. PR gate: build, architecture tests, Domain unit tests, Application unit tests, Secrets unit tests, API Contract tests, BFF integration tests, and selected bUnit suites.
3. Merge/nightly gate: Persistence integration, API RealRuntime, API Stress, full E2E Nightly, and any expensive browser/component suites.
4. Manual gate: visual baseline tests until deterministic storage, review, and artifact approval are implemented.
5. Emit TRX/test logs and Playwright traces/screenshots/videos as artifacts.
6. Add flake policy: quarantine only with owner, issue/removal condition, category, and expiration; flaky tests must not be silently skipped.
7. Update `dev/active/testing-coverage-improvement/testing-coverage-improvement-context.md` after each implementation session.

**Acceptance criteria:**

- CI commands are documented as project-level commands.
- Expensive suites are separated by category rather than permanently skipped.
- Artifacts make E2E and integration failures diagnosable.
- The traceability matrix maps critical requirements to active tests.

**Dependencies:** CI provider configuration and Docker/browser availability.

**Estimated effort:** 2-4 days.

**Skills:** CI/CD, TUnit filtering, Playwright artifacts, Aspire operations.

## Delete / Rewrite / Relocate Policy

Delete or rewrite tests when they meet any of these criteria:

- They only preserve backward-compatible behavior that the product no longer needs.
- They assert implementation details where a stronger behavior-level test exists.
- They duplicate coverage without adding a new failure mode.
- They use EF InMemory to claim relational behavior.
- They keep important coverage commented out indefinitely.
- They depend on test order or shared mutable state without a documented fixture reset strategy.
- They live in the wrong layer and force forbidden project references.

Relocate tests rather than delete them when the behavior is valuable but the current project violates Clean Architecture test boundaries.

## Verification Strategy

Use project-level commands only. Do not run solution-level `dotnet test`.

Baseline commands after implementation phases:

```bash
dotnet build --configuration Release --verbosity quiet
dotnet test --project Event.Architecture.Tests/Event.Architecture.Tests.csproj --configuration Release --verbosity quiet
dotnet test --project Event.Domain.UnitTests/Event.Domain.UnitTests.csproj --configuration Release --verbosity quiet
dotnet test --project Event.Application.UnitTests/Event.Application.UnitTests.csproj --configuration Release --verbosity quiet
dotnet test --project Explore.Secrets.UnitTests/Explore.Secrets.UnitTests.csproj --configuration Release --verbosity quiet
dotnet test --project Event.API.IntegrationTests/Event.API.IntegrationTests.csproj --configuration Release --verbosity quiet
dotnet test --project Explore.Blazor.IntegrationTests/Explore.Blazor.IntegrationTests.csproj --configuration Release --verbosity quiet
dotnet test --project Explore.Blazor.Client.Tests/Explore.Blazor.Client.Tests.csproj --configuration Release --verbosity quiet
dotnet test --project Event.Persistence.IntegrationTests/Event.Persistence.IntegrationTests.csproj --configuration Release --verbosity quiet
dotnet test --project Explore.Blazor.Client.E2ETests/Explore.Blazor.Client.E2ETests.csproj --configuration Release --verbosity quiet
```

Use category filters for nightly/manual suites once the exact TUnit filtering syntax is finalized in CI.

## Risk Assessment and Mitigations

| Risk | Impact | Mitigation |
|---|---|---|
| E2E flakiness from Aspire/Keycloak/browser state | Nightly failures become noisy | Deterministic seed/auth fixture, readiness waits, Playwright artifacts, narrow journeys |
| Direct-reference inventory false positives | Wasted test work | Risk-triage each target before writing tests; do not chase raw counts |
| Application test boundary refactor churn | Temporary broken tests/project references | Move behavior-preserving tests in small slices; run architecture and affected project tests after each slice |
| PostgreSQL Testcontainers runtime cost | Slow PR feedback | Keep relational suites nightly/merge where appropriate; keep unit tests fast |
| bUnit brittleness around MudBlazor internals | High maintenance cost | Assert semantics/user-visible behavior; only test wrapper internals where wrappers are the contract |
| Missing `Explore.Infrastructure.Tests` intent | Either dead exposure or missing test suite | Decide explicitly: create the project for provider tests or remove stale `InternalsVisibleTo` |
| CI category syntax drift | Tests skipped unintentionally | Document exact TUnit filters in `docs/TESTING.md` and CI scripts together |

## Potential Risks & Unknowns

- The direct-reference/name inventory is a heuristic, not a coverage tool. It highlights likely weak areas but cannot prove behavior is untested.
- Some EF InMemory usages are intentional Contract/BFF-profile shortcuts; they should not be blanket-deleted without checking the behavior being asserted.
- The deterministic Keycloak browser-state strategy is the largest unknown for activating E2E flows.
- The repository currently has no verified shared test-support project; this plan avoids inventing one unless implementation proves duplication is harmful.
- Exact TUnit category filtering syntax in CI should be verified before hard-coding workflow commands.
- TUnit's optional ASP.NET Core, Playwright, and Aspire helper packages may simplify some future fixtures, but they should be evaluated against the existing project-specific `WebApplicationFactory`, `AppHostFixture`, and Playwright setup before adoption.

## Success Metrics

- Zero untracked commented-out tests.
- Zero permanent critical-flow skips; expensive flows are categorized and scheduled.
- `Event.Application.UnitTests` no longer needs API/Infrastructure references for Application behavior.
- `Explore.Infrastructure/Explore.Infrastructure.csproj` no longer references a missing test project, or that project exists and has focused coverage.
- Critical BFF token/session/setup-secret/tenant tests are active.
- Critical tenant isolation, registration, authorization enforcement, and BFF forwarding E2E flows run in the Nightly category.
- PostgreSQL-backed tests cover relational behavior for high-risk repositories/configurations.
- Blazor component tests cover high-risk admin/user flows with semantic assertions.
- `docs/TESTING.md` and the CI matrix clearly state PR, merge, nightly, and manual suites.

## Timeline Estimate

- Phase 0: 1-2 days
- Phase 1: 3-5 days
- Phase 2: 5-8 days
- Phase 3: 5-8 days
- Phase 4: 5-10 days
- Phase 5: 4-7 days
- Phase 6: 2-4 days

Total: approximately 25-44 engineering days depending on E2E auth fixture complexity and repository coverage triage outcomes.
