ABOUTME: Test strategy, framework conventions, host profiles, and per-project roles for TUnit-based testing.
ABOUTME: Covers 3 host profiles, fixture model, builders/seeds, database lifecycle, and CI integration.

# Testing

## Framework

The project uses [TUnit](https://github.com/thomhurst/TUnit) — a modern, fast, parallel .NET testing framework.

Key TUnit features used:

| Feature | Usage |
|---------|-------|
| `[Test]` | Test method marker |
| `[Before(Test)]` / `[After(Test)]` | Per-test setup/teardown |
| `[Before(Class)]` / `[After(Class)]` | Per-class lifecycle hooks |
| `[NotInParallel]` | Serialize tests sharing resources |
| `Assert.That(x).IsEqualTo(y)` | Fluent async assertions |
| `Assert.Multiple()` | Group multiple assertions |

## Test Projects

Each project has a specific role. Run individually — never use solution-level `dotnet test`. Currently 10 projects.

| Project | Layer | Role | Requires Infra |
|---------|-------|------|----------------|
| `Event.Domain.UnitTests` | Domain | Entity invariants, value objects, domain logic | No |
| `Event.Application.UnitTests` | Application | Handler logic, validation, mapping | No |
| `Event.Architecture.Tests` | Cross-cutting | Convention enforcement via reflection | No |
| `Explore.Secrets.UnitTests` | Infrastructure | Secret provider logic, encryption, rotation | No |
| `Explore.Infrastructure.Tests` | Infrastructure | Provider adapters, configuration resolvers, authorization fallback behavior | No |
| `Event.Persistence.IntegrationTests` | Persistence | EF Core queries, repository behavior, migrations | PostgreSQL |
| `Event.API.IntegrationTests` | API | HTTP endpoints, middleware, auth flows | Full stack |
| `Explore.Blazor.IntegrationTests` | BFF | Middleware pipeline, auth endpoints, delegating handlers | No |
| `Explore.Blazor.Client.Tests` | UI | Component rendering, service behavior | No |
| `Explore.Blazor.Client.E2ETests` | E2E | Browser smoke tests, auth redirects, JS-dependent flows | Yes (full Aspire stack) |

### Run Commands

```bash
# Unit tests (no infrastructure needed)
dotnet test --project Event.Domain.UnitTests/Event.Domain.UnitTests.csproj --configuration Release --verbosity quiet
dotnet test --project Event.Application.UnitTests/Event.Application.UnitTests.csproj --configuration Release --verbosity quiet
dotnet test --project Event.Architecture.Tests/Event.Architecture.Tests.csproj --configuration Release --verbosity quiet
dotnet test --project Explore.Secrets.UnitTests/Explore.Secrets.UnitTests.csproj --configuration Release --verbosity quiet
dotnet test --project Explore.Infrastructure.Tests/Explore.Infrastructure.Tests.csproj --configuration Release --verbosity quiet

# Integration tests (requires Docker infrastructure running)
dotnet test --project Event.Persistence.IntegrationTests/Event.Persistence.IntegrationTests.csproj --configuration Release --verbosity quiet
dotnet test --project Event.API.IntegrationTests/Event.API.IntegrationTests.csproj --configuration Release --verbosity quiet

# BFF integration tests (no infrastructure needed — uses WebApplicationFactory with in-memory services)
dotnet test --project Explore.Blazor.IntegrationTests/Explore.Blazor.IntegrationTests.csproj --configuration Release --verbosity quiet

# UI tests
dotnet test --project Explore.Blazor.Client.Tests/Explore.Blazor.Client.Tests.csproj --configuration Release --verbosity quiet

# E2E browser tests (manual/nightly only, requires Aspire AppHost infrastructure)
dotnet test --project Explore.Blazor.Client.E2ETests/Explore.Blazor.Client.E2ETests.csproj --configuration Release --verbosity quiet
```

### Test Taxonomy And CI Lanes

Use TUnit metadata to route tests into the smallest lane that proves the behavior. TUnit uses `--treenode-filter` for metadata filtering; do not use VSTest-style `--filter` examples for TUnit projects.

| Lane | TUnit Metadata | Projects | Purpose | Default Frequency |
|------|----------------|----------|---------|-------------------|
| Unit | `[Category("Unit")]` or project-level unit suite | `Event.Domain.UnitTests`, `Event.Application.UnitTests`, `Explore.Secrets.UnitTests`, `Explore.Infrastructure.Tests` | Fast domain, handler, validator, mapping, service, provider, configuration, and fallback behavior | Every PR |
| Architecture | project-level architecture suite | `Event.Architecture.Tests` | Clean Architecture, naming, accessibility structure, authorization parity, and test-suite governance | Every PR |
| Component | `[Category("Component")]` or project-level UI suite | `Explore.Blazor.Client.Tests` | bUnit component, service, accessibility, wrapper, and design-system behavior | Every PR |
| API Contract | `[Category(TestCategories.Fast)]`, `[Category(TestCategories.Security)]`, `[Category(TestCategories.PolicyContract)]` | `Event.API.IntegrationTests` | HTTP serialization, HAL, ProblemDetails, auth matrix, Cerbos contract, and API surface rules | Every PR where possible |
| Real Runtime | `[NotInParallel("RealRuntimeDb")]` / PostgreSQL fixtures | `Event.Persistence.IntegrationTests`, real-runtime API tests | Provider-specific EF Core, migrations, query filters, tenant isolation, and repository behavior | Merge/nightly |
| Stress | stress fixture/category | `Event.API.IntegrationTests` | Rate limiting, retry headers, timeout, and high-volume middleware behavior | Nightly/manual |
| BFF Integration | BFF integration suite/categories | `Explore.Blazor.IntegrationTests` | Cookie auth, token refresh, YARP forwarding, tenant hints, and BFF middleware | Every PR for no-infra tests; nightly for Keycloak-backed tests |
| E2E | `[Category("E2E")]` or E2E project | `Explore.Blazor.Client.E2ETests` | Browser-only critical journeys through Aspire | Nightly/manual |
| Manual Visual | `[Category("Manual visual")]` or visual baseline tests | `Explore.Blazor.Client.E2ETests` | Screenshot baseline review and layout regressions | Manual/approved baseline lane |

Example TUnit filters:

```bash
dotnet test --project Event.API.IntegrationTests/Event.API.IntegrationTests.csproj --configuration Release --verbosity quiet -- --treenode-filter "////[Category=Security]"
dotnet test --project Explore.Blazor.Client.E2ETests/Explore.Blazor.Client.E2ETests.csproj --configuration Release --verbosity quiet -- --treenode-filter "////[Category=E2E]"
dotnet test --project Explore.Blazor.Client.E2ETests/Explore.Blazor.Client.E2ETests.csproj --configuration Release --verbosity quiet -- --treenode-filter "////[Category!=Manual visual]"
```

The nightly E2E runtime workflow intentionally runs the full `Explore.Blazor.Client.E2ETests` project without a category filter. Some critical browser flows are not tagged with `[Category("E2E")]` yet, while manual visual flows are explicitly skipped until baseline storage is approved.

### Disabled Test Governance

Disabled tests are allowed only when the test still expresses required future behavior and cannot run in the current lane. Do not comment out `[Test]`; either keep the test active, mark it with `[Skip("Category: ... Removal: ...")]`, or delete it when the behavior is obsolete or unnecessary.

Skip reason requirements:

- `Category:` names the owning suite or lane (`E2E`, `Stress`, `API contract`, `Component accessibility`, `Manual visual`, etc.).
- `Removal:` states the concrete condition for re-enabling or deleting the skip.
- No permanent skips. Infrastructure-gated tests move to a nightly/manual lane through category filters; they do not remain hidden from governance.
- No backward-compatibility preservation tests while the project is in development mode. If a test only protects an obsolete API, DTO shape, route alias, or UI behavior, delete it instead of skipping it.

`Event.Architecture.Tests.CodeHygieneTests` enforces that test source files do not contain commented-out `[Test]` markers and that every `[Skip]` includes both `Category:` and `Removal:`.

### Critical Risk Traceability Matrix

| Risk | Primary Test Project(s) | Required Coverage |
|------|--------------------------|-------------------|
| Tenant isolation leaks | `Event.Persistence.IntegrationTests`, `Event.API.IntegrationTests`, `Explore.Blazor.Client.E2ETests` | EF named filters, repository queries, API tenant binding, browser-visible cross-tenant denial |
| Authorization drift | `Event.Application.UnitTests`, `Event.API.IntegrationTests`, `Event.Architecture.Tests` | Handler authorization outcomes, endpoint 401/403/ProblemDetails, Cerbos/fallback parity |
| Infrastructure provider/config drift | `Explore.Infrastructure.Tests`, `Event.Architecture.Tests` | Provider adapters, deployment-mode settings, configuration resolvers, fallback authorization behavior, and governance keys |
| BFF token/header boundary failure | `Explore.Blazor.IntegrationTests`, `Explore.Blazor.Client.E2ETests` | Cookie-only browser state, server-side token forwarding, setup-secret stripping/replacement, trusted tenant hint forwarding |
| HAL/UI action mismatch | `Event.API.IntegrationTests`, `Explore.Blazor.Client.Tests` | HATEOAS link policies, response contracts, UI affordances gated by `_links` |
| Relational persistence regression | `Event.Persistence.IntegrationTests` | PostgreSQL migrations, query translation, constraints, soft delete, tenant filters, Respawn reset |
| Rate limiting/timeout/idempotency regressions | `Event.API.IntegrationTests` | Stress host policies, Retry-After metadata, ProblemDetails, idempotency and request-timeout middleware behavior |
| Critical browser journeys fail | `Explore.Blazor.Client.E2ETests` | Aspire-backed smoke, registration, tenant isolation, authorization enforcement, and BFF forwarding flows |
| Accessibility/design-system drift | `Explore.Blazor.Client.Tests`, `Event.Architecture.Tests` | bUnit semantic component checks, structural accessibility guardrails, wrapper behavior |

## E2E Browser Tests (Explore.Blazor.Client.E2ETests)

- Uses Playwright for real browser automation.
- Starts the Aspire AppHost through `Aspire.Hosting.Testing` inside the test fixture; do not start a second external AppHost for this suite.
- Requires Docker on the runner because the fixture starts Testcontainers for PostgreSQL and Keycloak.
- Not run in normal PR CI; `.github/workflows/e2e.yml` runs it manually and nightly until reliability is proven.
- Installs Chromium dependencies in CI before running tests and uploads TRX, Playwright traces, screenshots, videos, test logs, and Docker diagnostics.
- Keep this suite intentionally small; only add flows that require a real browser.

### Generating TRX Reports

When debugging failures, generate detailed reports:

```bash
dotnet test --project <ProjectPath> --configuration Release -- --report-trx --report-trx-filename results.trx
```

## Architecture Tests

`Event.Architecture.Tests` enforces project-wide conventions through reflection-based tests. These are not optional — they are CI gates.

### Convention Categories

| Category | What It Enforces |
|----------|-----------------|
| **Layer Dependencies** | Clean Architecture rules: Domain has no upstream references, Application references only Domain |
| **Naming Conventions** | Handler suffixes, validator suffixes, specification naming |
| **Accessibility** | Routable pages contain `<h1>`, MainLayout has skip-link, landmarks, ARIA live regions |
| **CSS Direction** | Scoped CSS avoids physical direction properties (`margin-left` → `margin-inline-start`) — advisory |
| **Authorization Parity** | Every resource kind has a Cerbos policy and fallback case |
| **ABOUTME Headers** | All C# files start with `ABOUTME:` comments |
| **Test Suite Governance** | Disabled tests use explicit skip metadata, never commented-out `[Test]` markers |
| **Documentation Quality** | Canonical docs include metadata/source anchors, stale placeholders are blocked in new operator docs, and unsupported TUnit filter commands are rejected |

### Documentation Quality Tests

Run documentation quality checks through the architecture test project:

```bash
dotnet test --project Event.Architecture.Tests/Event.Architecture.Tests.csproj --configuration Release --verbosity quiet
```

These checks intentionally run as part of the whole project because this repository uses TUnit. Do not use VSTest-style `--filter` for docs checks.

### Accessibility Convention Tests

These tests verify structural accessibility requirements:

- `RoutablePages_MustContainH1Heading` — every routable page has an `<h1>` (excludes settings wrapper pages where `<h1>` is in the active tab)
- `MainLayout_MustContainSkipLink` — skip navigation link present
- `MainLayout_MustContain_MainLandmark` — `<main>` element exists
- `MainLayout_MustContain_HeaderLandmark` — `<header>` element exists
- `MainLayout_MustContain_NavigationLandmark` — `<nav>` with `aria-label` exists
- `MainLayout_MustContain_AriaLiveRegions` — polite and assertive live regions present
- `ScopedCss_MustNotUsePhysicalDirectionProperties` — advisory, tracks RTL readiness
- `ScopedCss_MustNotUsePhysicalPositionProperties` — advisory, tracks RTL readiness

### Authorization Parity Tests

- `RegisteredResourceKinds_ShouldHave_FallbackCase` — every resource kind handles unknown permissions
- `RegisteredResourceKinds_ShouldHave_CerbosPolicy` — policy file exists per resource
- `AllPermissionActions_ShouldBe_MappedInToActionString` — enum-to-string mapping is complete
- `CerbosPolicies_ShouldHave_FallbackCase` — Cerbos YAML includes default deny

## TDD Workflow

TDD is the default unless explicitly allowed to skip.

1. **Write a failing test** — define expected behavior
2. **Run to confirm failure** — test must fail for the right reason
3. **Write minimal code** — just enough to pass
4. **Run tests** — all must pass
5. **Refactor** — improve code with tests green

## Test Conventions

### Do

- Test one behavior per test method
- Use descriptive test names: `MethodName_Condition_ExpectedResult`
- Use real data and APIs in integration tests — avoid mocks in end-to-end flows
- Run all required project-level test projects before submitting a PR; never use solution-level `dotnet test`
- Keep test output pristine — no unexpected warnings or stack traces
- Prefer deleting unnecessary or backward-compatibility-only tests over preserving obsolete behavior

### Do Not

- Delete failing tests to make the suite pass
- Commit with broken tests
- Use mocks in integration/E2E tests
- Create ad-hoc test scripts — use the test projects
- Skip architecture tests — they are CI gates
- Comment out `[Test]` attributes to hide failing tests
- Add backward-compatibility tests for obsolete behavior while the project is in development mode

## Test Data And Fixtures

- **Domain unit tests**: construct entities directly with valid state
- **Application unit tests**: use in-memory fakes or builder patterns for repositories
- **Integration tests**: use the real database with test containers or Docker infrastructure
- **API integration tests**: use `WebApplicationFactory` with the full middleware pipeline

## CI Pipeline Integration

The standard CI pipeline runs the fast non-E2E test projects on every PR; integration-enabled callers run PostgreSQL-backed suites separately. GitHub Actions restore steps use `dotnet restore --locked-mode`, so package input changes must include matching `packages.lock.json` updates. The pipeline:

1. Restores dependencies
2. Builds in Release configuration
3. Runs each fast, non-E2E test project sequentially (not solution-level), including `Explore.Infrastructure.Tests`
4. Publishes TRX evidence for CI troubleshooting
5. Fails the PR if any test project reports failures
6. Architecture tests run alongside unit tests (no infrastructure needed)

Nightly/manual runtime lanes are intentionally advisory until reliability data proves they can be merge-blocking:

- `.github/workflows/e2e.yml` runs the Aspire-backed Playwright E2E project manually and on schedule.
- `.github/workflows/security-tests.yml` and `.github/workflows/cerbos-policy-check.yml` also run on schedule so auth, Keycloak, Cerbos, and policy contracts are exercised even when no matching path changes occur.
- Runtime-lane failures retain artifacts for debugging rather than forcing an immediate local rerun.

Required vs advisory gate policy, branch-protection guidance, and artifact retention expectations are maintained in [CI_CD_GOVERNANCE.md](CI_CD_GOVERNANCE.md).

Rate limiting is automatically disabled in the `Testing` environment — all rate limit policies are replaced with `NoLimiter`.

## Related

- [GETTING_STARTED.md](GETTING_STARTED.md) — setup and first run
- [ARCHITECTURE.md](ARCHITECTURE.md) — layer rules enforced by architecture tests
- [ACCESSIBILITY.md](ACCESSIBILITY.md) — WCAG requirements tested by convention tests
- [CONTRIBUTING.md](CONTRIBUTING.md) — PR validation checklist
- [QUICK_REFERENCE.md](QUICK_REFERENCE.md) — constraints tested by architecture tests
- [DOCUMENTATION_ARCHITECTURE.md](DOCUMENTATION_ARCHITECTURE.md) — documentation metadata, source anchors, and quality gates

## API Integration Test Host Profiles

The API integration tests use a **three-host-profile model** to balance speed, fidelity, and isolation.

| Profile | Database | Rate Limiting | Purpose |
|---|---|---|---|
| **Contract** | EF InMemory | Disabled | Fast API surface validation: serialization, HAL structure, ProblemDetails, content-type, Prefer headers |
| **RealRuntime** | PostgreSQL (Testcontainers) | Disabled | Production-faithful behavior: persistence, tenant isolation, auth families, migrations |
| **Stress** | PostgreSQL (Testcontainers) | Enabled (low thresholds) | Timing-sensitive: rate limiting enforcement, 429 response format |

These profiles are correctness tests, not performance benchmarks. Runtime benchmark runs live in [BENCHMARKS.md](BENCHMARKS.md) and use BenchmarkDotNet so contributors can compare relative endpoint cost, allocations, and diagnoser output under controlled runs.

### Fixture Architecture

```
Event.API.IntegrationTests/
├── Builders/           # Fluent entity builders (TenantBuilder, UserBuilder, ActorBuilder, EventBuilder)
├── Fixtures/           # WebApplicationFactory + TUnit fixtures
│   ├── ContractApiFixture.cs             (InMemory, fast)
│   ├── RealRuntimeApiFixture.cs          (PostgreSQL, production-faithful)
│   ├── StressApiFixture.cs               (PostgreSQL, rate limiting enabled)
│   ├── PostgreSqlApiFixtureBase.cs       (abstract base for PG fixtures)
│   ├── PostgreSqlApiWebApplicationFactory.cs
│   ├── TestDatabaseReset.cs              (Respawn wrapper)
│   └── TestHostProfile.cs               (profile constants)
├── Features/           # Test classes organized by feature
│   ├── Hateoas/        # HAL contract + scenario tests
│   └── ...
├── Helpers/            # Assertion helpers (ProblemDetailsAssertions)
└── Seeds/              # Business-readable scenario seeds
```

### Database Lifecycle (PostgreSQL Fixtures)

```
Container Start → MigrateAsync → LookupTableSeeder (27 tables) → Respawner.CreateAsync

Per Test: ResetAsync (Respawn) → Seed scenario data → Execute test
```

Respawn resets ALL tables except `__EFMigrationsHistory` and 27 lookup tables.

### Testcontainers Benchmarks Versus Tests

`Event.Benchmarks` has a PostgreSQL/Testcontainers API benchmark lane that intentionally borrows the integration-test infrastructure shape without becoming a test project:

| Concern | API Integration Tests | PostgreSQL API Benchmarks |
|---|---|---|
| Goal | Prove correctness: status codes, HAL contracts, auth behavior, tenant isolation, migrations, and persistence rules. | Compare endpoint cost through ASP.NET Core `TestServer`, EF Core, Npgsql, and PostgreSQL for performance work. |
| Runner | TUnit via `dotnet test --project ...`; never solution-level `dotnet test`. | BenchmarkDotNet via `dotnet run --configuration Release --project Event.Benchmarks -- --filter "*ApiEndpointPostgreSqlBenchmarks*"`. |
| Database setup | PostgreSQL Testcontainer, `MigrateAsync()`, lookup seeding, Respawn reset per scenario. | PostgreSQL Testcontainer in BenchmarkDotNet `GlobalSetup`, current EF model schema via `EnsureCreatedAsync()`, PostgreSQL model constraints, lookup and benchmark-owned event seed data. |
| Measured body | Assertions and scenario behavior; setup and assertions are part of the test. | Timed method only sends the HTTP request and reads the response; container start, schema creation, and seeding are outside measured iterations. |
| Caching/auth | Uses profile-specific auth, Cerbos, rate-limit, and reset conventions according to the test purpose. | Uses benchmark auth and allow-all authorization; PostgreSQL suite disables output-cache replay with a no-op store so controller/MediatR/EF/Npgsql/PostgreSQL work remains visible. |

Use integration tests when behavior must be proven. Use the PostgreSQL API benchmark when the question is whether an implementation change makes representative read endpoints faster, slower, or more allocation-heavy. Benchmark numbers are relative evidence from one controlled run, not production SLOs or load-test proof.

### Builders vs Seeds

| Concept | Purpose | Location |
|---|---|---|
| **Builders** | Low-level entity construction with fluent API and sensible defaults | `Builders/` |
| **Seeds** | Business-readable named scenarios that compose builders and persist to DB | `Seeds/` |

Tests call seeds (not builders directly) for readability. Seeds handle circular dependency resolution (e.g., User → Actor → User.ActorId).

### Writing New API Integration Tests

```csharp
[ClassDataSource<RealRuntimeApiFixture>(Shared = SharedType.PerAssembly)]
[NotInParallel("RealRuntimeDb")]
public class MyFeatureTests(RealRuntimeApiFixture fixture)
{
    [Test]
    public async Task MyScenario()
    {
        await fixture.ResetDatabaseAsync();
        await using var scope = fixture.Factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<ExploreDbContext>();
        var tenant = await TenantScenarioSeed.SeedActiveTenantWithUserAsync(db);

        var response = await fixture.Client.GetAsync("/api/event");
        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.OK);
    }
}
```

Key conventions:
- **`[NotInParallel]`** — Required for PostgreSQL tests. Use `"RealRuntimeDb"` or `"StressDb"`.
- **Primary constructors** — Prefer `public class MyTests(FixtureType fixture)`.
- **Reset before seed** — Every test calls `ResetDatabaseAsync()` then seeds its own data.
- **Auth helpers** — `CreateAuthenticatedRequest()`, `CreateInstanceAdminRequest()`, `CreateTenantAdminRequest()` on all fixtures.

### Anti-Patterns

- Do not share data across tests — each test resets and seeds its own.
- Do not assert `IsNotEqualTo(InternalServerError)` — assert the exact expected status code.
- Do not guard assertions with `if (items.Length > 0)` — seed data to guarantee items exist.
- Do not use `EnsureCreated()` for PostgreSQL test fixtures — use `MigrateAsync()`. The BenchmarkDotNet PostgreSQL harness is the intentional exception because it measures active-development current-model API performance rather than migration correctness.
- Do not test non-existent endpoints.
- Do not test auth gates in isolation when covered by auth-family matrix tests.

### API Contract Snapshot Tests

Contract snapshots live in the **Contract** host profile and use `Verify.TUnit` against the EF InMemory API surface. They are for stable HTTP response contracts such as HAL links, collection envelopes, and RFC 7807 ProblemDetails shapes.

Snapshot rules:

- Commit `.verified.*` files under the feature snapshot directory; these are the reviewed API contract baselines.
- Never commit `.received.*` files. Treat them as review artifacts generated when a response contract changes.
- Scrub volatile fields before verification (`traceId`, `timestamp`, `correlationId`, generated cache-busters, and similar request-specific values).
- Update snapshots only when the API contract change is intentional and reviewed alongside the endpoint implementation.
- Keep Docker/PostgreSQL-dependent snapshots separate from Contract-profile snapshots; real-runtime snapshot coverage is optional and should be added only when the environment can run the required infrastructure.

Run focused snapshot tests with TUnit tree filters. Example:

```bash
dotnet test --project Event.API.IntegrationTests/Event.API.IntegrationTests.csproj \
  --configuration Release --no-build \
  --treenode-filter "/*/*/*/EventList_AnonymousContract_MatchesSnapshot" \
  --minimum-expected-tests 1
```

## Blazor Client Tests (Explore.Blazor.Client.Tests)

Tests Blazor component rendering, service behavior, accessibility compliance, and UI interaction patterns using [bUnit](https://bunit.dev/).

### Test Harness Architecture

The shared `BlazorTestContext` extends `Bunit.TestContext` with:

| Feature | Description |
|---------|-------------|
| **Strict JSInterop** | bUnit default — throws on unconfigured JS invocations. Only approved MudBlazor and app-specific handlers are pre-configured. |
| **MudBlazor service mocks** | Concrete mock classes (`MudBlazorTestMocks.cs`) registered BEFORE `AddMudServices()` so `TryAdd*` skips JS-dependent services. |
| **Opt-in service groups** | `AddShellStateMocks()`, `AddGroupServiceMock()`, `AddAllDefaultMocks()` — test files opt in to services they need. |
| **Auth builders** | `AuthenticationTestBuilder` (fluent), `AuthenticationScenarios` (factory), `AuthenticationTestConstants` (IDs/roles). |
| **Settings builder** | `PublicExperienceSettingsBuilder` for feature flags, branding, analytics, and module configuration. |

### Choosing the Right Test Layer

| Question | If Yes → | If No → |
|----------|----------|---------|
| Does the test need a real browser (JS execution, cookies, redirects)? | E2E | ↓ |
| Does the test need the full BFF middleware pipeline (auth, tenant routing)? | `Explore.Blazor.IntegrationTests` | ↓ |
| Does the test exercise component rendering, service behavior, or accessibility? | `Explore.Blazor.Client.Tests` | ↓ |
| Does the test verify domain logic or handler behavior? | `Event.Application.UnitTests` or `Event.Domain.UnitTests` | Reconsider if a test is needed |

### Writing New Blazor Component Tests

```csharp
public class MyComponentTests : IDisposable
{
    private readonly BlazorTestContext _ctx;

    public MyComponentTests()
    {
        _ctx = new BlazorTestContext();
        _ctx.AddShellStateMocks();  // opt in to what you need
    }

    public void Dispose() => _ctx.Dispose();

    [Test]
    public async Task Render_WhenAuthenticated_ShowsUserName()
    {
        _ctx.SetAuthenticatedUser(Guid.NewGuid(), "Jane Doe");
        var cut = _ctx.RenderMudComponent<MyComponent>();

        await Assert.That(cut.Markup).Contains("Jane Doe");
    }
}
```

### Approved Patterns

- **Interaction-driven tests** — click buttons, fill inputs, trigger callbacks via rendered elements
- **Public API verification** — assert via `cut.Instance.PublicMethod()` or rendered markup
- **Behavioral assertions** — test what the user sees, not implementation details
- **`WaitForAssertion`** — for async lifecycle (service calls in `OnAfterRenderAsync`)
- **`SetParametersAndRender`** — re-render with new parameter values to test reactivity
- **Fluent auth builders** — `AuthenticationScenarios.Admin().Build(ctx)` for auth setup
- **Settings builder** — `new PublicExperienceSettingsBuilder().WithBranding("My Brand").WithIslamicModule(true)`
- **`NavMenuTestServices.Register(ctx)`** — for tests rendering components that depend on NavMenu services

### Anti-Patterns (Disallowed)

| Pattern | Why It's Disallowed | What To Do Instead |
|---------|--------------------|--------------------|
| `JSInterop.Mode = JSRuntimeMode.Loose` | Masks missing JS call handlers; failures appear only at runtime | Keep strict mode; add explicit `SetupVoid` / `Setup<T>` handlers |
| `GetMethod("Private", BindingFlags.NonPublic)` | Couples tests to implementation; breaks on refactoring | Test through UI interaction or public API |
| `GetField("_privateField", BindingFlags.NonPublic)` | Same as above | Verify via rendered markup or public state |
| Ambient service registration in constructor | Hides dependencies; tests pass for wrong reasons | Use opt-in helpers (`AddShellStateMocks()`) |
| `if (condition) throw` assertions | Not TUnit-native; no structured failure reporting | Use `await Assert.That(x).Contains(y)` |
| Snapshot-style markup equality | Brittle; breaks on any MudBlazor version update | Assert specific elements, classes, or text content |

### Exceptions (Documented Workarounds)

These reflection uses are accepted with justification:

| Pattern | Justification | Location |
|---------|---------------|----------|
| `SimulateTagToggle(cut, tagId)` | MudPopover content not rendered with mock services; only used for setup, not verification | `TriStateTagFilterDropdownTests.cs` |
| `InvokeLoadEventsAsync(cut)` | bUnit cannot trigger `Virtualize<T>.ItemsProvider` delegate directly; documented workaround | `EventListTests.cs` |
| Type-name assembly lookup | `AnalyticsInitializer` Razor component not referenceable from `.cs` files; Razor tooling limitation | `AnalyticsInitializerTests.cs` |

## BFF Integration Tests (Explore.Blazor.IntegrationTests)

Tests the Blazor Server BFF layer: middleware pipeline, auth endpoints, and delegating handler chain.

### Architecture

```
Explore.Blazor.IntegrationTests/
├── Fixtures/           # WebApplicationFactory + test auth handler
│   ├── BlazorBffWebApplicationFactory.cs  (InMemory DB, mock auth, mock resolver)
│   ├── TestAuthHandler.cs                 (X-Test-Auth header → ClaimsPrincipal)
│   └── TenantTestController.cs            (test-only controller for middleware assertions)
├── Endpoints/          # BFF auth endpoint tests
│   └── BffAuthStatusTests.cs
├── Handlers/           # DelegatingHandler unit-style tests
│   ├── AccessTokenForwardingHandlerTests.cs
│   ├── CorrelationIdDelegatingHandlerTests.cs
│   ├── SetupSecretForwardingHandlerTests.cs
│   ├── TenantHeaderForwardingHandlerTests.cs
│   └── CapturingHandler.cs               (shared test double)
└── Middleware/          # Middleware pipeline tests
    └── PathTenantResolverMiddlewareTests.cs
```

### Test Host

`BlazorBffWebApplicationFactory` uses `WebApplicationFactory<Program>` with:

- **Environment**: `Testing` (disables rate limiting)
- **Auth**: Cookie + OIDC replaced with `TestAuthHandler` (header-based claims)
- **Database**: EF InMemory replacing Npgsql
- **Cache**: `DistributedMemoryCache` replacing Redis
- **Auth schemes**: Mock `IDynamicAuthSchemeManager` (empty provider list)
- **Tenant resolver**: Mock `IResolverConfigService` (PathEnabled=true, PathPrefix="/t")

### Handler Tests

Handler tests use `CapturingHandler` — a test double that captures outgoing requests and returns `200 OK`. Each handler is tested in isolation with `HttpMessageInvoker`:

```csharp
var handler = new TenantHeaderForwardingHandler(mockAccessor, mockHttpContextAccessor)
{
    InnerHandler = new CapturingHandler()
};
var invoker = new HttpMessageInvoker(handler);
var response = await invoker.SendAsync(request, CancellationToken.None);
// Assert captured request headers
```

### Writing New BFF Integration Tests

```csharp
public class MyBffTests
{
    private readonly BlazorBffWebApplicationFactory _factory = new();

    [Test]
    public async Task MyEndpoint_WhenAuthenticated_ReturnsExpected()
    {
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Add("X-Test-Auth",
            TestAuthHandler.CreateAuthHeaderValue(Guid.NewGuid(), "Test User"));

        var response = await client.GetAsync("/auth/status");
        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.OK);
    }
}
```
