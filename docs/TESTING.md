ABOUTME: Test strategy, framework conventions, and per-project roles for TUnit-based testing.
ABOUTME: Covers all 7 test projects, architecture tests, TDD workflow, and CI integration.

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

Each project has a specific role. Run individually — never use solution-level `dotnet test`.

| Project | Layer | Role | Requires Infra |
|---------|-------|------|----------------|
| `Event.Domain.UnitTests` | Domain | Entity invariants, value objects, domain logic | No |
| `Event.Application.UnitTests` | Application | Handler logic, validation, mapping | No |
| `Event.Architecture.Tests` | Cross-cutting | Convention enforcement via reflection | No |
| `Explore.Secrets.UnitTests` | Infrastructure | Secret provider logic, encryption, rotation | No |
| `Event.Persistence.IntegrationTests` | Persistence | EF Core queries, repository behavior, migrations | PostgreSQL |
| `Event.API.IntegrationTests` | API | HTTP endpoints, middleware, auth flows | Full stack |
| `Explore.Blazor.Client.Tests` | UI | Component rendering, service behavior | No |

### Run Commands

```bash
# Unit tests (no infrastructure needed)
dotnet test --project Event.Domain.UnitTests/Event.Domain.UnitTests.csproj --configuration Release --verbosity quiet
dotnet test --project Event.Application.UnitTests/Event.Application.UnitTests.csproj --configuration Release --verbosity quiet
dotnet test --project Event.Architecture.Tests/Event.Architecture.Tests.csproj --configuration Release --verbosity quiet
dotnet test --project Explore.Secrets.UnitTests/Explore.Secrets.UnitTests.csproj --configuration Release --verbosity quiet

# Integration tests (requires Docker infrastructure running)
dotnet test --project Event.Persistence.IntegrationTests/Event.Persistence.IntegrationTests.csproj --configuration Release --verbosity quiet
dotnet test --project Event.API.IntegrationTests/Event.API.IntegrationTests.csproj --configuration Release --verbosity quiet

# UI tests
dotnet test --project Explore.Blazor.Client.Tests/Explore.Blazor.Client.Tests.csproj --configuration Release --verbosity quiet
```

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
- Run all 7 test projects before submitting a PR
- Keep test output pristine — no unexpected warnings or stack traces

### Do Not

- Delete failing tests to make the suite pass
- Commit with broken tests
- Use mocks in integration/E2E tests
- Create ad-hoc test scripts — use the test projects
- Skip architecture tests — they are CI gates

## Test Data And Fixtures

- **Domain unit tests**: construct entities directly with valid state
- **Application unit tests**: use in-memory fakes or builder patterns for repositories
- **Integration tests**: use the real database with test containers or Docker infrastructure
- **API integration tests**: use `WebApplicationFactory` with the full middleware pipeline

## CI Pipeline Integration

All 7 test projects run in CI on every PR. The pipeline:

1. Restores dependencies
2. Builds in Release configuration
3. Runs each test project sequentially (not solution-level)
4. Fails the PR if any test project reports failures
5. Architecture tests run alongside unit tests (no infrastructure needed)

Rate limiting is automatically disabled in the `Testing` environment — all rate limit policies are replaced with `NoLimiter`.

## Related

- [GETTING_STARTED.md](GETTING_STARTED.md) — setup and first run
- [ARCHITECTURE.md](ARCHITECTURE.md) — layer rules enforced by architecture tests
- [ACCESSIBILITY.md](ACCESSIBILITY.md) — WCAG requirements tested by convention tests
- [CONTRIBUTING.md](../CONTRIBUTING.md) — PR validation checklist
- [QUICK_REFERENCE.md](QUICK_REFERENCE.md) — constraints tested by architecture tests
