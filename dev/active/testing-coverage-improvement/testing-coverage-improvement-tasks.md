<!-- ABOUTME: Actionable task checklist for implementing testing coverage improvements across repository test layers. -->
<!-- ABOUTME: Tracks phases, dependencies, acceptance criteria, and verification commands for future sessions. -->

# Testing Coverage Improvement — Tasks

Last Updated: 2026-05-05

## Status Legend

- `[ ]` Not started
- `[~]` In progress
- `[x]` Completed
- `[!]` Blocked or needs decision

## Quick Resume

Current phase: Phase 1 — Activate critical E2E/nightly confidence.

Start here:

1. Continue Phase 1 by wiring deterministic Keycloak/BFF-cookie auth state before unskipping authenticated critical flows.
2. Keep the active E2E smoke tests and Playwright artifact fixture green while expanding coverage.
3. Keep updating this file and `testing-coverage-improvement-context.md` after each implementation session.

## Phase 0 — Test taxonomy, governance, and cleanup baseline

### Tasks

- [x] Update `docs/TESTING.md` with the target taxonomy: Domain Unit, Application Unit, Infrastructure, Persistence Integration, API Contract, API RealRuntime, API Stress, BFF Integration, Blazor Component, E2E Nightly, Manual Visual.
- [x] Document PR, merge, nightly, and manual suite expectations using project-level commands only.
- [x] Add the explicit policy: do not add backward-compatibility tests in development mode.
- [x] Add delete/rewrite/relocate criteria for unnecessary tests.
- [x] Extend `Event.Architecture.Tests/CodeHygieneTests.cs` or equivalent guardrail to catch commented-out `[Test]` markers.
- [x] Add skip-governance checks or documentation requiring each `[Skip]` to include blocker, phase/owner, category, and removal condition.
- [x] Convert commented E2E smoke and route/API-client naming tests into active, categorized skipped, or deleted items.
- [x] Add a traceability matrix in `docs/TESTING.md` mapping critical risks to test projects.

### Acceptance Criteria

- [x] No important test remains commented out as `// [Test]`.
- [x] Every skip is intentional, categorized, and has a removal condition.
- [x] Test taxonomy and suite matrix are documented.
- [x] Project-level commands are documented; no solution-level `dotnet test` guidance is added.

### Dependencies

- None.

### Verification

```bash
dotnet build --configuration Release --verbosity quiet
dotnet test --project Event.Architecture.Tests/Event.Architecture.Tests.csproj --configuration Release --verbosity quiet
```

## Phase 1 — Activate critical E2E/nightly confidence

### Tasks

- [x] Add deterministic E2E auth fixture/helper under `Explore.Blazor.Client.E2ETests/Fixtures/`.
- [x] Serialize or otherwise isolate shared-AppHost database resets before unskipping data-dependent E2E flows.
- [x] Wire `AppHostFixture` to the E2E PostgreSQL Testcontainer and inject the connection string into AppHost child projects.
- [x] Copy `docker/keycloak/ISLAMU-realm.test.json` into the E2E test output as the deterministic realm asset.
- [x] Activate meaningful frontend load and anonymous auth-status assertions in `Explore.Blazor.Client.E2ETests/Flows/SmokeTests.cs`.
- [ ] Activate `TenantIsolationFlowTests.cs` with seeded tenant data and cross-tenant denial assertions.
- [ ] Activate `AuthorizationEnforcementFlowTests.cs` for low-privilege browser/API enforcement.
- [ ] Activate `BffTokenForwardingChainFlowTests.cs` for browser cookie → BFF → API token forwarding.
- [ ] Activate `RegistrationFlowTests.cs` after auth state is stable.
- [x] Keep `SidebarLayoutVisualTests.cs` manual/nightly until screenshot baseline storage and approval exist.
- [x] Configure Playwright traces/screenshots/videos for E2E browser contexts.

### Acceptance Criteria

- [ ] Core E2E flows are not permanently skip-gated.
- [x] Nightly E2E uses Aspire AppHost, PostgreSQL Testcontainers, Keycloak, and Playwright for the active smoke lane.
- [x] Shared AppHost database reset callers are serialized with `[NotInParallel("E2EAppHostDb")]`.
- [x] Active smoke tests use readiness waits, semantic locators, and browser-context API assertions where practical.
- [x] E2E failures produce useful artifacts.

### Dependencies

- Docker/Testcontainers.
- Stable Aspire AppHost local/nightly runtime.
- Deterministic Keycloak browser auth state is now available via `BffCookieAuthHelper`; use it before unskipping authenticated critical flows.
- `AppHostFixture` must remain parameterless for TUnit `ClassDataSource`; keep PostgreSQL ownership private and expose wrapper methods only.
- Shared-AppHost E2E flows must not run concurrent database resets against the same PostgreSQL container; use `[NotInParallel("E2EAppHostDb")]` for any caller of `ResetDatabaseAsync()`.

### Verification

```bash
dotnet test --project Explore.Blazor.Client.E2ETests/Explore.Blazor.Client.E2ETests.csproj --configuration Release --verbosity quiet
dotnet test --project Explore.Blazor.Client.E2ETests/Explore.Blazor.Client.E2ETests.csproj --configuration Release --verbosity quiet --treenode-filter "/*/*/SmokeTests/*"
```

Use the filtered smoke command for the active Phase 1 smoke lane. The AppHost PostgreSQL prerequisite is wired, reset callers are serialized with `E2EAppHostDb`, deterministic Keycloak/BFF-cookie auth is verified, and smoke passes 3/3. Use the full E2E project command only after the remaining skipped critical flows are activated deliberately.

## Phase 2 — Restore Application unit-test purity and handler coverage

### Tasks

- [ ] Inventory `Event.Application.UnitTests` tests that require `Explore.API` or `Explore.Infrastructure`.
- [ ] Move API/HATEOAS tests to `Event.API.IntegrationTests` or an appropriate API test folder.
- [ ] Move Infrastructure-provider tests to `Explore.Infrastructure.Tests` if that project is created, or another correct lower-layer test project.
- [ ] Remove `Explore.API` and `Explore.Infrastructure` project references from `Event.Application.UnitTests/Event.Application.UnitTests.csproj` when no longer needed.
- [ ] Add high-risk command-handler tests for role ownership, tenant/group membership, settings governance, footer governance, event lifecycle mutation, and registration update.
- [ ] Add high-risk query-handler tests for tenant filtering, authorization-sensitive reads, cache behavior, and DTO mapping.
- [ ] Add validator matrix tests for high-risk commands/queries.
- [ ] Ensure handlers use manual validator instantiation and repository-port substitutes only.

### Acceptance Criteria

- [ ] Application unit tests do not depend on API or Infrastructure projects.
- [ ] High-risk handlers have success, not-found, invalid, forbidden/wrong-tenant, side-effect, and cache-invalidation coverage where applicable.
- [ ] Validator tests cover meaningful domain/business constraints.
- [ ] Architecture tests remain green.

### Dependencies

- Test relocation plan from Phase 0 inventory.

### Verification

```bash
dotnet test --project Event.Application.UnitTests/Event.Application.UnitTests.csproj --configuration Release --verbosity quiet
dotnet test --project Event.Architecture.Tests/Event.Architecture.Tests.csproj --configuration Release --verbosity quiet
```

## Phase 3 — API and BFF semantic integration coverage

### Tasks

- [ ] Add API Contract-profile tests for route names, HAL links, ProblemDetails, content negotiation, XML/OpenAPI-sensitive contracts, and `Prefer` handling.
- [ ] Add API RealRuntime-profile tests for tenant isolation, authorization, idempotency, ETag/output cache, and PostgreSQL write-side effects.
- [ ] Add API Stress-profile tests for rate limiting, retry-after headers, setup-secret limiter behavior, and concurrency-sensitive no-5xx guarantees.
- [ ] Reactivate route-name/API-client naming tests after route constants and NSwag operationIds are intentionally stabilized.
- [ ] Add BFF tests for `BffAuthCookieStore`.
- [ ] Add BFF tests for `TokenRefreshCookieEvents`.
- [ ] Add BFF tests for `DynamicAuthSchemeManager`.
- [ ] Add BFF tests for `BffAdminClaimsTransformation`.
- [ ] Add BFF/YARP tests for token forwarding and setup-secret stripping/replacement.
- [ ] Add BFF tenant header and readiness tests.

### Acceptance Criteria

- [ ] API tests assert specific contract/business behavior, not broad non-500 checks.
- [ ] BFF tests prove tokens remain server-side.
- [ ] Setup-secret and tenant-forwarding paths are covered for allowed and denied cases.
- [ ] EF InMemory remains limited to documented Contract/BFF profiles.

### Dependencies

- Existing API/BFF WebApplicationFactory fixtures.
- Route/operationId stabilization for naming tests.

### Verification

```bash
dotnet test --project Event.API.IntegrationTests/Event.API.IntegrationTests.csproj --configuration Release --verbosity quiet
dotnet test --project Explore.Blazor.IntegrationTests/Explore.Blazor.IntegrationTests.csproj --configuration Release --verbosity quiet
```

## Phase 4 — PostgreSQL persistence and infrastructure hardening

### Tasks

- [ ] Add PostgreSQL integration tests for OutboxRepository behavior.
- [ ] Add PostgreSQL integration tests for IdempotencyRepository behavior.
- [ ] Add PostgreSQL integration tests for ExternalApiKeyRepository behavior.
- [ ] Add PostgreSQL integration tests for footer/settings repositories.
- [ ] Add PostgreSQL integration tests for role/organization membership repositories.
- [ ] Add PostgreSQL integration tests for registration policy/template/custom-property governance repositories.
- [ ] Add EF metadata tests for tenant filters, soft-delete filters, required properties, unique indexes, cascade behavior, conversions, and lookup seeds.
- [ ] Add migration smoke coverage against PostgreSQL Testcontainers.
- [ ] Decide whether to create `Explore.Infrastructure.Tests/Explore.Infrastructure.Tests.csproj` or remove stale `InternalsVisibleTo` from `Explore.Infrastructure/Explore.Infrastructure.csproj`.
- [ ] If created, add focused Infrastructure tests for provider/resolver/outbox-dispatch behavior.
- [ ] Reclassify or port EF InMemory persistence tests that claim relational behavior.

### Acceptance Criteria

- [ ] Relational behavior uses PostgreSQL Testcontainers and Respawn reset.
- [ ] EF InMemory is not used for relational claims.
- [ ] Infrastructure internals exposure matches an existing test project or is removed.
- [ ] Persistence tests are deterministic under project-level execution.

### Dependencies

- Docker/Testcontainers.
- Repository risk triage.

### Verification

```bash
dotnet test --project Event.Persistence.IntegrationTests/Event.Persistence.IntegrationTests.csproj --configuration Release --verbosity quiet
```

If `Explore.Infrastructure.Tests` is created:

```bash
dotnet test --project Explore.Infrastructure.Tests/Explore.Infrastructure.Tests.csproj --configuration Release --verbosity quiet
```

## Phase 5 — Blazor component, accessibility, and design-system coverage

### Tasks

- [ ] Add bUnit tests for high-risk event/session dialogs.
- [ ] Add bUnit tests for admin API key, footer, theme, and settings pages.
- [ ] Add bUnit tests for login prompts, review flows, and language picker behavior.
- [ ] Add wrapper/design-system tests for AppButton/AppCard/AppTextField/AppDialogShell contracts where behavior is project-owned.
- [ ] Add validator tests for Blazor-facing validators.
- [ ] Restore or replace the skipped retry-button accessibility test in `SharedComponentAccessibilityTests.cs`.
- [ ] Evaluate an axe-compatible accessibility engine for a small stable component/page set; otherwise strengthen structural accessibility tests.
- [ ] Keep JSInterop strict and explicitly configured.

### Acceptance Criteria

- [ ] High-risk UI flows have render, validation, submit/cancel, loading/error, and auth/HAL-state tests.
- [ ] Tests assert user-visible behavior and semantics.
- [ ] Tests avoid private reflection and brittle MudBlazor internals unless the wrapper contract requires it.
- [ ] Accessibility coverage includes roles, labels, alert/error semantics, and key affordances.

### Dependencies

- Stable MudBlazor v9 wrappers.
- Component risk inventory.

### Verification

```bash
dotnet test --project Explore.Blazor.Client.Tests/Explore.Blazor.Client.Tests.csproj --configuration Release --verbosity quiet
```

## Phase 6 — CI matrix, artifacts, and durable reporting

### Tasks

- [ ] Add/document PR test matrix: build, architecture, Domain, Application, Secrets, API Contract, BFF, selected bUnit.
- [ ] Add/document merge/nightly test matrix: Persistence, API RealRuntime, API Stress, E2E Nightly, expensive browser/component suites.
- [ ] Keep visual baseline tests manual until deterministic baseline storage and approval exist.
- [ ] Add TRX/log artifacts for test projects.
- [ ] Add Playwright trace/screenshot/video artifacts for E2E.
- [ ] Document flake quarantine rules with owner, issue/removal condition, category, and expiration.
- [ ] Update this context file after every implementation session.

### Acceptance Criteria

- [ ] CI uses project-level commands only.
- [ ] Expensive suites are categorized instead of permanently skipped.
- [ ] Artifacts make failures diagnosable.
- [ ] The traceability matrix maps critical requirements to active tests.

### Dependencies

- CI provider support for Docker and browser artifacts.

### Verification

```bash
dotnet build --configuration Release --verbosity quiet
```

Then run each project-level test command required by the changed phase.

## Overall Done Criteria

- [ ] All P0 critical flows are active under an explicit Nightly category or documented manual visual category.
- [ ] Application unit tests are layer-pure.
- [ ] Infrastructure test project/exposure mismatch is resolved.
- [ ] PostgreSQL covers relational behavior for high-risk persistence paths.
- [ ] API/BFF tests cover semantic security, tenant, HAL, ProblemDetails, and rate-limit behavior.
- [ ] Blazor tests cover high-risk UI flows semantically.
- [ ] `docs/TESTING.md` and CI documentation are updated.
- [ ] No unnecessary or backward-compatibility tests are introduced.
