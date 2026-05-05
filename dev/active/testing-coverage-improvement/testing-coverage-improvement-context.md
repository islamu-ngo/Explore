<!-- ABOUTME: Session context and handoff notes for the testing coverage improvement implementation plan. -->
<!-- ABOUTME: Preserves research sources, verified paths, decisions, constraints, and quick-resume steps. -->

# Testing Coverage Improvement — Context

Last Updated: 2026-05-05

## SESSION PROGRESS

### Completed

- Audited current test coverage across Domain, Application, Persistence, API, Architecture, Blazor client, Blazor BFF, E2E, and Secrets test projects.
- Verified the active-work documentation convention in `dev/active/README.md`.
- Verified canonical repo testing rules in `docs/TESTING.md`.
- Verified Clean Architecture and security context from `docs/ARCHITECTURE.md`, `docs/SECURITY.md`, `docs/API.md`, `docs/OPERATIONS.md`, `docs/CONFIGURATION.md`, `docs/DOMAIN.md`, `docs/FEDERATION.md`, and `docs/TROUBLESHOOTING.md`.
- Used Context7 documentation for TUnit, bUnit, ASP.NET Core integration testing, and Testcontainers .NET.
- Used Tavily research for current TUnit/bUnit/.NET/Testcontainers/Aspire testing guidance.
- Launched parallel repository/research agents to verify plan conventions, test targets, and external testing guidance.
- Created `dev/active/testing-coverage-improvement/`.
- Generated and verified the plan, context, and task checklist files.
- Started implementation and completed Phase 0 test-governance baseline.
- Updated `docs/TESTING.md` with TUnit lane taxonomy, `--treenode-filter` examples, disabled-test governance, no-backward-compatibility policy, and critical risk traceability matrix.
- Converted commented `[Test]` markers into explicit `[Skip("Category: ... Removal: ...")]` tests in API contract/stress/onboarding, Blazor API-client naming, and E2E smoke tests.
- Normalized existing skipped E2E, visual, stress, and component-accessibility tests so every skip includes `Category:` and `Removal:` metadata.
- Extended `Event.Architecture.Tests/CodeHygieneTests.cs` with file-scanning governance for commented-out `[Test]` markers and skip metadata.
- Corrected `Event.API.IntegrationTests/Fixtures/TestCategories.cs` documentation to use TUnit `--treenode-filter` instead of VSTest `--filter`.
- Verified Phase 0 with text scans, LSP diagnostics, architecture project build, architecture test suite, and full Release build.
- Started Phase 1 E2E/nightly confidence implementation.
- Activated `Explore.Blazor.Client.E2ETests/Flows/SmokeTests.cs` frontend-load smoke coverage by removing the skip from `BlazorFrontend_Loads_ReturnsHtml`.
- Strengthened `SmokeTests.AuthStatus_Anonymous_ReturnsNotAuthenticated` to call `/auth/status` through Playwright's browser-context API request channel and parse JSON for `isAuthenticated = false`.
- Added Playwright trace, screenshot, and video artifact support to `Explore.Blazor.Client.E2ETests/Fixtures/PlaywrightFixture.cs` and routed E2E page cleanup through `ClosePageAsync` so browser contexts close and videos flush.
- Verified the edited E2E project builds in Release configuration.
- Fixed the first smoke runtime failure by replacing a brittle `#app` selector with a semantic `/events` route assertion against the accessible `Explore Events` heading.
- Verified the filtered E2E smoke lane passes through the Aspire AppHost.
- Wired `AppHostFixture` to an AppHost-owned E2E PostgreSQL container while keeping the fixture parameterless for TUnit `ClassDataSource`.
- Injected the E2E PostgreSQL connection string into `event-migrationservice`, `explore-api`, and `explore-blazor` through Aspire resource environment overrides.
- Added the deterministic Keycloak test realm copy to `Explore.Blazor.Client.E2ETests.csproj` for the next BFF-cookie auth fixture increment.
- Fixed a TUnit/Testcontainers lifecycle issue by exposing database reset/seeding wrapper methods from `AppHostFixture` instead of a public disposable database fixture property.
- Serialized shared AppHost database reset flows with `[NotInParallel("E2EAppHostDb")]` on E2E classes that call `ResetDatabaseAsync()`.
- Added `Explore.Blazor.Client.E2ETests/Fixtures/BffKeycloakFixture.cs` to start deterministic Keycloak 26.1.2 with the imported ISLAMU test realm.
- Wired `AppHostFixture` to initialize Keycloak before the Aspire AppHost and inject deterministic Keycloak configuration into `explore-blazor` and `explore-api`.
- Added `BffCookieAuthHelper` to drive real browser Keycloak login, assert `/auth/status` authenticated, verify the BFF HttpOnly auth cookie, and ensure browser storage does not contain token-shaped values.
- Added and verified `SmokeTests.AuthStatus_KeycloakLogin_ReturnsAuthenticatedWithServerCookieOnly`.
- Isolated E2E Blazor from local Infisical secrets by clearing Infisical bootstrap env vars on the `explore-blazor` AppHost resource so the test realm secret wins.
- Activated `TenantIsolationFlowTests.cs` as an API-boundary tenant isolation E2E through the Aspire AppHost.
- Seeded completed multi-tenant bootstrap/routing state and tenant A/B event data for the tenant isolation scenario.
- Fixed a real cross-tenant cache leak in `Explore.Application/Features/Events/Handlers/Queries/GetEventListRequestHandler.cs` by adding `ITenantContext.TenantId` to the `events:list` HybridCache key.
- Added `GetEventListRequestHandlerTests.Handle_WithSameRequestForDifferentTenants_UsesTenantScopedCacheEntries` so identical list requests from different tenants cannot reuse a cached page.

### In Progress

- Phase 1 implementation: continue activating the remaining authenticated critical flows incrementally; tenant isolation is now active and passing at the API tenant boundary.

### Blockers

- Remaining Phase 1 work: authorization enforcement, BFF token-forwarding, and registration critical-flow scaffolds still need to be unskipped one at a time using the verified Keycloak/BFF-cookie helper.
- Oracle review note resolved for current E2E reset callers: classes that call `ResetDatabaseAsync()` now use `[NotInParallel("E2EAppHostDb")]`; keep this lock on future shared-AppHost DB tests unless they use an isolated database.

## Key Verified Files

### Planning and repository rules

- `dev/active/README.md`
- `CLAUDE.md`
- `docs/TESTING.md`
- `docs/ARCHITECTURE.md`
- `docs/SECURITY.md`
- `docs/API.md`
- `docs/OPERATIONS.md`
- `docs/CONFIGURATION.md`
- `docs/DOMAIN.md`
- `docs/FEDERATION.md`
- `docs/TROUBLESHOOTING.md`

### Test projects

- `Event.Domain.UnitTests/Event.Domain.UnitTests.csproj`
- `Event.Application.UnitTests/Event.Application.UnitTests.csproj`
- `Event.Persistence.IntegrationTests/Event.Persistence.IntegrationTests.csproj`
- `Event.API.IntegrationTests/Event.API.IntegrationTests.csproj`
- `Event.Architecture.Tests/Event.Architecture.Tests.csproj`
- `Explore.Blazor.Client.Tests/Explore.Blazor.Client.Tests.csproj`
- `Explore.Blazor.IntegrationTests/Explore.Blazor.IntegrationTests.csproj`
- `Explore.Blazor.Client.E2ETests/Explore.Blazor.Client.E2ETests.csproj`
- `Explore.Secrets.UnitTests/Explore.Secrets.UnitTests.csproj`

### Architecture guardrails

- `Event.Architecture.Tests/CleanArchitectureTests.cs`
- `Event.Architecture.Tests/ProjectionLayerBoundaryTests.cs`
- `Event.Architecture.Tests/AccessibilityConventionTests.cs`
- `Event.Architecture.Tests/AuthorizationParityTests.cs`
- `Event.Architecture.Tests/BlazorClientArchitectureTests.cs`
- `Event.Architecture.Tests/CodeHygieneTests.cs`

### Persistence fixtures and targets

- `Event.Persistence.IntegrationTests/Fixtures/PostgreSqlContainerFixture.cs`
- `Event.Persistence.IntegrationTests/Fixtures/ProjectionTestContainerFixture.cs`
- `Event.Persistence.IntegrationTests/DataProtection/DataProtectionKeyPersistenceTests.cs`
- `Event.Persistence.IntegrationTests/Repositories/CustomPropertyOptionLifecycleRepositoryTests.cs`

### API/BFF fixtures and targets

- `Event.API.IntegrationTests/Fixtures/PostgreSqlApiFixtureBase.cs`
- `Event.API.IntegrationTests/Features/ApiEndpointSmokeTests.cs`
- `Event.API.IntegrationTests/Features/RouteNameCoverageTests.cs`
- `Event.API.IntegrationTests/Features/StressRateLimitingTests.cs`
- `Explore.Blazor.IntegrationTests/Fixtures/BffKeycloakFixture.cs`
- `Explore.Blazor.IntegrationTests/Fixtures/BlazorBffWebApplicationFactory.cs`
- `Explore.Blazor.IntegrationTests/Fixtures/SecurityBlazorBffWebApplicationFactory.cs`
- `Explore.Blazor.IntegrationTests/Endpoints/BffSecurityTests.cs`
- `Explore.Blazor.IntegrationTests/Handlers/BffCookieForwardingHandlerTests.cs`
- `Explore.Blazor.IntegrationTests/Handlers/TenantHeaderForwardingHandlerTests.cs`

### Blazor client and E2E targets

- `Explore.Blazor.Client.Tests/Common/BlazorTestContext.cs`
- `Explore.Blazor.Client.Tests/Accessibility/SharedComponentAccessibilityTests.cs`
- `Explore.Blazor.Client.E2ETests/Fixtures/AppHostFixture.cs`
- `Explore.Blazor.Client.E2ETests/Fixtures/PostgreSqlContainerFixture.cs`
- `Explore.Blazor.Client.E2ETests/Fixtures/PlaywrightFixture.cs`
- `Explore.Blazor.Client.E2ETests/Fixtures/BffKeycloakFixture.cs`
- `Explore.Blazor.Client.E2ETests/Fixtures/BffCookieAuthHelper.cs`
- `Explore.Blazor.Client.E2ETests/Seeds/TenantIsolationScenarioSeed.cs`
- `Explore.Blazor.Client.E2ETests/Flows/SmokeTests.cs`
- `Explore.Blazor.Client.E2ETests/Flows/CriticalFlows/TenantIsolationFlowTests.cs`
- `Explore.Blazor.Client.E2ETests/Flows/CriticalFlows/RegistrationFlowTests.cs`
- `Explore.Blazor.Client.E2ETests/Flows/CriticalFlows/BffTokenForwardingChainFlowTests.cs`
- `Explore.Blazor.Client.E2ETests/Flows/CriticalFlows/AuthorizationEnforcementFlowTests.cs`
- `Explore.Blazor.Client.E2ETests/Flows/SidebarLayoutVisualTests.cs`

## Key Decisions

1. **No backward compatibility tests.** The plan explicitly avoids tests that preserve legacy shapes or obsolete behavior.
2. **Risk-based coverage beats raw coverage counts.** The prior audit's direct-reference/name inventory is useful for triage but must not become a mechanical target list.
3. **E2E is a Nightly gate, not a PR gate.** Full Aspire/browser/Keycloak/PostgreSQL flows are too expensive and infrastructure-sensitive for every PR.
4. **EF InMemory is allowed only for documented Contract/BFF or unit-ish scenarios.** It is not acceptable for relational persistence behavior.
5. **Application unit tests must become layer-pure.** API/Infrastructure dependencies should be moved, removed, or justified outside Application unit testing.
6. **Visual tests stay manual/nightly until deterministic baseline storage and approval exist.** Core functional E2E flows come first.
7. **No shared test-support project is assumed.** The repo does not currently verify one; create one only if implementation duplication justifies it.
8. **Disabled tests are explicit, not hidden.** Commented `[Test]` markers are banned; temporary skips must name the suite category and removal condition.
9. **TUnit filtering uses tree-node filters.** Documentation now uses `--treenode-filter`, not VSTest `--filter`, for category lane examples.
10. **`AppHostFixture` owns the E2E database.** It must stay parameterless for TUnit and expose only wrapper methods (`ResetDatabaseAsync`, `CreateDbContext`) so tests can seed/reset the same database without TUnit double-disposing the container.
11. **Shared E2E database resets must be serialized before more flows are enabled.** Oracle approved the increment but warned that parallel tests can race if they share the AppHost/database and call `ResetDatabaseAsync()` concurrently.
12. **Shared E2E database reset callers use `E2EAppHostDb`.** This follows existing repo lock names like `RealRuntimeDb`, `StressDb`, and `PersistenceDb` while keeping browser parallelism separate.
13. **E2E Keycloak auth uses real browser OIDC, not token injection.** The helper drives Keycloak UI login, verifies the BFF HttpOnly cookie, and scans browser storage for token-shaped values.
14. **E2E Blazor clears Infisical bootstrap credentials.** This prevents local/real `/keycloak` secrets from overriding the deterministic test realm secret.
15. **Tenant-sensitive query caches must include tenant identity.** The tenant-isolation E2E exposed that `events:list` cache entries were shared across tenants until `ITenantContext.TenantId` was added to the cache key.

## Phase 0 Verification Results

- `git grep -n -E '^\s*//\s*\[(Test|Fact|Theory)\]' -- '*.cs' || true` — no commented test attributes found.
- Python skip-governance scan — no `[Skip]` attributes missing `Category:` or `Removal:` metadata.
- `lsp_diagnostics` on `Event.Architecture.Tests/CodeHygieneTests.cs` — no diagnostics.
- `dotnet build Event.Architecture.Tests/Event.Architecture.Tests.csproj --configuration Release --verbosity quiet` — passed; pre-existing NuGet warnings only.
- `dotnet test --project Event.Architecture.Tests/Event.Architecture.Tests.csproj --configuration Release --verbosity quiet` — passed, 145 total / 145 succeeded / 0 failed / 0 skipped.
- `dotnet build --configuration Release --verbosity quiet` — passed, 23 projects / 0 errors; pre-existing NuGet warnings only.

## Phase 1 Verification Results

- Context7 Playwright .NET documentation confirmed trace collection via `context.Tracing.StartAsync(...)` / `StopAsync(...)` and browser-context video recording with `RecordVideoDir` plus context close.
- Context7/TUnit and external docs research confirmed `--treenode-filter` category selection for E2E/nightly lanes.
- `lsp_diagnostics` on `Explore.Blazor.Client.E2ETests/Fixtures/PlaywrightFixture.cs` — no diagnostics.
- `lsp_diagnostics` on `Explore.Blazor.Client.E2ETests/Flows/SmokeTests.cs` — no diagnostics.
- `dotnet build Explore.Blazor.Client.E2ETests/Explore.Blazor.Client.E2ETests.csproj --configuration Release --verbosity quiet` — passed, 12 projects / 0 errors; pre-existing NuGet warnings only.
- `dotnet test --project Explore.Blazor.Client.E2ETests/Explore.Blazor.Client.E2ETests.csproj --configuration Release --verbosity quiet --treenode-filter "/*/*/SmokeTests/*"` — passed, 2 total / 2 succeeded / 0 failed / 0 skipped; pre-existing warnings only.
- `dotnet build Explore.Blazor.Client.E2ETests/Explore.Blazor.Client.E2ETests.csproj --configuration Release --verbosity quiet` after AppHost-owned database refactor — passed, 12 projects / 0 errors; pre-existing warnings only.
- `dotnet test --project Explore.Blazor.Client.E2ETests/Explore.Blazor.Client.E2ETests.csproj --configuration Release --verbosity quiet --treenode-filter "/*/*/SmokeTests/*"` after AppHost-owned database refactor — passed, 2 total / 2 succeeded / 0 failed / 0 skipped; pre-existing warnings only.
- `dotnet build Explore.Blazor.Client.E2ETests/Explore.Blazor.Client.E2ETests.csproj --configuration Release --verbosity quiet` after adding `[NotInParallel("E2EAppHostDb")]` locks — passed, 12 projects / 0 errors; pre-existing warnings only.
- `dotnet test --project Explore.Blazor.Client.E2ETests/Explore.Blazor.Client.E2ETests.csproj --configuration Release --verbosity quiet --treenode-filter "/*/*/SmokeTests/*"` after adding `[NotInParallel("E2EAppHostDb")]` locks — passed, 2 total / 2 succeeded / 0 failed / 0 skipped; pre-existing warnings only.
- Verified `Explore.Blazor.Client.E2ETests/bin/Release/net10.0/TestAssets/ISLAMU-realm.test.json` is copied for the upcoming Keycloak-backed E2E auth fixture.

- `lsp_diagnostics` on `Explore.Blazor.Client.E2ETests/Fixtures/AppHostFixture.cs`, `BffKeycloakFixture.cs`, and `BffCookieAuthHelper.cs` — no diagnostics.
- `dotnet build Explore.Blazor.Client.E2ETests/Explore.Blazor.Client.E2ETests.csproj --configuration Release --verbosity quiet` after Keycloak/BFF-cookie auth wiring — passed, 12 projects / 0 errors; pre-existing warnings only.
- `timeout 180s dotnet test --no-build --project Explore.Blazor.Client.E2ETests/Explore.Blazor.Client.E2ETests.csproj --configuration Release --verbosity normal --treenode-filter "/*/*/SmokeTests/AuthStatus_KeycloakLogin_ReturnsAuthenticatedWithServerCookieOnly"` — passed, 1 total / 1 succeeded / 0 failed / 0 skipped, `EXIT:0`.
- `timeout 300s dotnet test --no-build --project Explore.Blazor.Client.E2ETests/Explore.Blazor.Client.E2ETests.csproj --configuration Release --verbosity quiet --treenode-filter "/*/*/SmokeTests/*"` — passed, 3 total / 3 succeeded / 0 failed / 0 skipped, `EXIT:0`.
- Verified no Keycloak or PostgreSQL Testcontainers remain running after the passing smoke lane.
- `lsp_diagnostics` on `Explore.Application/Features/Events/Handlers/Queries/GetEventListRequestHandler.cs`, `Event.Application.UnitTests/Features/Events/Queries/GetEventListRequestHandlerTests.cs`, and tenant E2E files — no diagnostics.
- `dotnet test --project Event.Application.UnitTests/Event.Application.UnitTests.csproj --configuration Release --verbosity quiet --treenode-filter "/*/*/GetEventListRequestHandlerTests/*"` — passed, 10 total / 10 succeeded / 0 failed / 0 skipped.
- `dotnet build Explore.Blazor.Client.E2ETests/Explore.Blazor.Client.E2ETests.csproj --configuration Release --verbosity quiet` after tenant cache fix — passed, 12 projects / 0 errors; pre-existing warnings only.
- `timeout 360s dotnet test --no-build --project Explore.Blazor.Client.E2ETests/Explore.Blazor.Client.E2ETests.csproj --configuration Release --verbosity quiet --treenode-filter "/*/*/TenantIsolationFlowTests/*"` — passed, 1 total / 1 succeeded / 0 failed / 0 skipped, `EXIT:0`.

## Constraints

- Use project-level test commands only; never use solution-level `dotnet test`.
- Preserve Clean Architecture dependency direction.
- Validators are manually instantiated in handlers.
- Repositories return entities, not DTOs.
- BFF keeps tokens server-side and strips/replaces sensitive headers.
- UI action affordances are HAL-link driven.
- Integration and E2E tests must avoid mocks where real infrastructure is the behavior under test.
- Every new file needs two ABOUTME lines.

## External Research Notes

### TUnit

- Prefer TUnit-native `[Test]`, async assertions, and lifecycle hooks.
- Use `ClassDataSource` for expensive shared fixtures with explicit `SharedType` and reset/isolation strategy.
- Use categories/properties for suite selection.
- Avoid `async void`; lifecycle hooks cannot be `async void`.
- Avoid unnecessary global serialization; use dependencies or explicit non-parallel constraints only where required.

### bUnit

- Use `BunitContext`/repo wrapper context and explicit `Services` registration.
- Prefer semantic markup/user-visible assertions.
- Use `WaitForAssertion` or `WaitForState` for asynchronous rendering.
- Configure JSInterop explicitly; strict mode catches missing JS contracts.
- Avoid brittle assertions against generated third-party component internals.

### ASP.NET Core Integration Testing

- Use `WebApplicationFactory` and `WithWebHostBuilder`/`ConfigureTestServices` for replacing DBs, auth, external services, clocks, and current-user context.
- Use test authentication schemes except when specifically validating real OIDC/Keycloak behavior.
- Assert black-box HTTP behavior plus side effects at the correct layer.

### Testcontainers / EF Core

- Use PostgreSQL Testcontainers with wait strategies and dynamic connection strings.
- Use Respawn or equivalent deterministic reset between tests.
- Prefer production-like providers for relational behavior.
- Avoid EF InMemory for constraints, query translation, query filters, transactions, and provider-specific behavior.

### Playwright / Aspire

- Keep E2E small and high-value.
- Use semantic locators and web-first assertions.
- Capture trace/screenshots/videos for diagnosis, preferably on failure or retry only.
- Close browser contexts so Playwright videos are reliably written.
- Wait for Aspire resources before browser actions.
- Use distributed application testing patterns for closed-box full-app tests; keep expensive full-stack suites in nightly/manual lanes.

## Source Reference Index

- ASP.NET Core integration testing: `https://learn.microsoft.com/en-us/aspnet/core/test/integration-tests?view=aspnetcore-10.0`
- EF Core testing strategy: `https://learn.microsoft.com/en-us/ef/core/testing/choosing-a-testing-strategy`
- EF Core testing against production database systems: `https://learn.microsoft.com/en-us/ef/core/testing/testing-with-the-database`
- Testcontainers PostgreSQL module: `https://dotnet.testcontainers.org/modules/postgres/`
- bUnit semantic HTML comparison: `https://bunit.dev/docs/verification/semantic-html-comparison.html`
- TUnit ASP.NET Core examples: `https://tunit.dev/docs/examples/aspnet/`
- TUnit Playwright examples: `https://tunit.dev/docs/examples/playwright/`
- TUnit Aspire examples: `https://tunit.dev/docs/examples/aspire/`
- Playwright .NET trace viewer: `https://playwright.dev/dotnet/docs/trace-viewer`
- Playwright .NET videos: `https://playwright.dev/dotnet/docs/videos`
- .NET Aspire testing overview: `https://learn.microsoft.com/en-us/dotnet/aspire/testing/overview`
- .NET Aspire AppHost test management: `https://learn.microsoft.com/en-us/dotnet/aspire/testing/manage-app-host`

## Quick Resume

1. Read `dev/active/testing-coverage-improvement/testing-coverage-improvement-plan.md`.
2. Continue with Phase 1 in `testing-coverage-improvement-tasks.md`.
3. Next implementation step: continue Phase 1 by unskipping authorization enforcement or BFF token-forwarding with the verified Keycloak/BFF-cookie helper. Tenant isolation is already active at the API boundary; keep `[NotInParallel("E2EAppHostDb")]` on any shared database reset callers.
4. Update this context file after each implementation session.
5. Run only project-level verification commands from `docs/TESTING.md` and the plan.
