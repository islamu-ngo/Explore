# Enterprise Testing Strategy — ISLAMU Event Platform

**Last Updated: 2026-02-09**

## Executive Summary

The ISLAMU Event platform has **946 tests across 7 projects** — a solid foundation, but with significant coverage gaps and some anti-patterns that undermine maintainability. This plan establishes an enterprise-grade testing strategy following the principle of **"Test Behavior, Not Implementation"** to achieve 80%+ meaningful coverage across all layers.

### Current State (946 tests, uneven coverage)

| Project | Tests | Coverage | Health |
|---------|-------|----------|--------|
| Event.Application.UnitTests | 143 | ~5% handlers (10/207) | ⚠️ Gaps |
| Event.Architecture.Tests | 24 | ~90% rules | ✅ Strong |
| Event.API.IntegrationTests | 357 | ~70% endpoints | ✅ Strong |
| Explore.Blazor.Client.Tests | 230 | 38% services (11/29) | ⚠️ Gaps |
| Explore.Secrets.UnitTests | 190 | ~85% services | ✅ Strong |
| Event.Persistence.IntegrationTests | 2 | ~5% repos | ❌ Critical |
| Event.Domain.UnitTests | 0 | 0% | ❌ Critical |

### Target State (~1400+ tests, 80%+ coverage)

| Project | Current | Target | Delta |
|---------|---------|--------|-------|
| Event.Application.UnitTests | 143 | 200+ | +57 |
| Event.Architecture.Tests | 24 | 35+ | +11 |
| Event.API.IntegrationTests | 357 | 400+ | +43 |
| Explore.Blazor.Client.Tests | 230 | 400+ | +170 |
| Explore.Secrets.UnitTests | 190 | 210+ | +20 |
| Event.Persistence.IntegrationTests | 2 | 64+ | +62 |
| Event.Domain.UnitTests | 0 | 80+ | +80 |

---

## Core Philosophy

### Test Behavior, Not Implementation

> "If I refactor the internal logic of a method but the output remains the same, the test MUST NOT fail."

**Rules:**
1. **Assert on outcomes** (return values, state changes, observable side-effects) — never on internal call sequences
2. **Use `Received()` verification ONLY** when the side-effect IS the behavior (e.g., `CreateAsync()` was called, email was sent)
3. **Use real DTOs and value objects** — only mock volatile external dependencies (DB, HTTP, file system)
4. **AAA pattern** (Arrange–Act–Assert) with clear visual separation
5. **Method naming**: `MethodName_StateUnderTesting_ExpectedBehavior`

### What NOT to Test
- Library internals (NSwag client generation, MediatR pipeline, EF Core queries)
- Auto-generated code (`EventApiClient.g.cs`)
- Simple property getters/setters
- Constructor-only classes with no logic
- Framework behavior (ASP.NET routing, DI container)

### What TO Test
- Business logic and decision branches
- Error handling behavior (what happens when X fails?)
- Data transformation (DTO → Entity, HAL → DTO)
- State management (cache invalidation, auth state)
- Integration points (API responses, DB queries with real data)
- Architecture constraints (layer dependencies, naming conventions)

---

## Technology Stack

| Tool | Purpose | Status |
|------|---------|--------|
| **TUnit** | Test framework (async-first, source-gen runner) | ✅ In use |
| **NSubstitute** | Mocking framework | ✅ In use |
| **Bogus** | Fake data generation (via ComponentDataBuilder) | ✅ In use |
| **bUnit** | Blazor component testing | ✅ In use (limited) |
| **NetArchTest.Rules** | Architecture fitness tests | ✅ In use |
| **TestContainers** | PostgreSQL integration tests | ✅ In use (Persistence only) |
| **TUnit Assertions** | `await Assert.That(x).IsEqualTo(y)` | ✅ In use |

### Decision: TUnit Assertions vs FluentAssertions

**Decision: Keep TUnit assertions.** The entire codebase uses `await Assert.That(...)` consistently. Switching to FluentAssertions would create inconsistency and a migration burden with zero behavioral benefit. TUnit assertions are async-first and sufficient.

---

## Phase 1: Audit & Fix Anti-Patterns (Week 1)

**Goal:** Remove implementation-coupled tests that will break on refactoring.

### 1.1 Remove Over-Mocking in Blazor Client Tests

**Problem:** ~40+ instances of `Received(1)` verification on read operations across Blazor service tests.

**Before (brittle):**
```csharp
var result = await _service.GetAllEventsAsync();
await Assert.That(result.Count).IsEqualTo(3);
// ANTI-PATTERN: Tests HOW, not WHAT
await _apiClient.Received(1).GetEventsAsync(1, 100, Arg.Any<CancellationToken>());
```

**After (behavioral):**
```csharp
var result = await _service.GetAllEventsAsync();
await Assert.That(result.Count).IsEqualTo(3);
// Only assert the outcome — implementation is free to change
```

**Keep `Received()` ONLY for:**
- Write operations: `Received(1).CreateEventAsync(...)` — the side-effect IS the behavior
- Retry logic: `Received(3).SyncUserAsync(...)` — verifying retry count IS the behavior
- Logging in error paths: `_logger.Received(1).Log(...)` — if logging is a requirement

**Pagination parameter verification — use outcome-based testing instead:**
Instead of `Received(1).GetEventsAsync(1, 100, ...)` (couples test to constants), configure the mock to return different results for different inputs and assert on the output:
```csharp
// GOOD: Mock returns specific data for correct pagination, assert outcome
_apiClient.GetEventsAsync(Arg.Any<int?>(), Arg.Any<int?>(), Arg.Any<CancellationToken>())
    .Returns(halResponse);
var result = await _service.GetAllEventsAsync();
await Assert.That(result.Count).IsEqualTo(3); // Outcome proves it called the right thing
```

**Files to fix:**
- `EventServiceTests.cs` — Remove Received() on read operations
- `CategoryServiceTests.cs` — Remove Received() on GetAll/GetById
- `LocationServiceTests.cs` — Remove Received() on read operations
- `AdminServiceTests.cs` — Remove Received() on read operations, keep on writes
- `OrganizationServiceTests.cs` — Remove Received() on reads
- `OrganizationMemberServiceTests.cs` — Remove Received() on reads
- `OrganizationReviewServiceTests.cs` — Remove Received() on reads
- `EventRegistrationServiceTests.cs` — Remove Received() on reads
- `LandingPageServiceTests.cs` — Remove Received() on reads
- `UserServiceTests.cs` — Keep Received() on SyncUser (side-effect), remove on reads

**Acceptance:** All 230 existing tests still pass after changes. No `Received()` on read-only API calls.

### 1.2 Add Error Logging Verification

**Problem:** Logger is mocked in all tests but never verified. Silent failures go undetected.

**Action:** For every test that exercises an error path (`ThrowsAsync`, returns null), verify that `_logger` received appropriate log call:
```csharp
_logger.Received(1).Log(
    LogLevel.Error,
    Arg.Any<EventId>(),
    Arg.Any<object>(),
    Arg.Any<Exception?>(),
    Arg.Any<Func<object, Exception?, string>>()
);
```

**Note:** NSubstitute with `ILogger<T>` requires the verbose `Log()` method signature — not `LogError()` directly (it's an extension method).

**Scope:** Only services that inject `ILogger<T>` and call `_logger.LogError()` / `_logger.LogWarning()` in their catch blocks. If a service doesn't log on error paths, don't add logging verification — add it to the service first if logging is desired.

**Acceptance:** Every error-path test in services that log verifies logging occurred.

### 1.3 Consolidate Redundant Tests

**Problem:** Many services have separate tests for "returns empty when null" and "returns empty when exception" that are effectively identical from a behavioral perspective.

**Action:** Keep both tests but ensure they test genuinely different behavior. If the service behaves identically, combine using `[MethodDataSource]` or a shared helper:
```csharp
[Test]
public async Task GetAllEventsAsync_ReturnsEmptyList_WhenApiReturnsNull() { ... }

[Test]
public async Task GetAllEventsAsync_ReturnsEmptyList_WhenApiThrowsApiException() { ... }
```

**Decision:** Keep separate — each documents a distinct contract even if implementation is same. They serve as regression guards if error handling diverges later.

---

## Phase 2: Fill Critical Coverage Gaps (Weeks 2–4)

### 2.1 Event.Domain.UnitTests — FROM ZERO (Priority: CRITICAL)

**Current state:** Placeholder file only. 0 tests.

**Domain entities to test (in priority order):**

| Entity | Logic Complexity | Tests Needed |
|--------|-----------------|--------------|
| Event | HIGH — status transitions, validation | 15-20 |
| Organization | MEDIUM — membership rules | 8-12 |
| EventSession | MEDIUM — speaker/language composition | 8-10 |
| User | LOW — mostly data carrier | 5-8 |
| Actor | LOW — type validation | 3-5 |
| Tenant | LOW — settings, mode | 5-8 |
| Location | LOW — coordinate validation | 3-5 |
| StorageObject | LOW — file type validation | 3-5 |

**Test patterns for domain entities:**
```csharp
public class EventTests
{
    [Test]
    public async Task Create_WithValidData_ShouldSetDefaultValues()
    {
        var @event = new Event { Title = "Test", ... };
        await Assert.That(@event.IsDeleted).IsFalse();
        await Assert.That(@event.CreatedAt).IsNotNull();
    }

    [Test]
    public async Task SoftDelete_ShouldSetIsDeletedTrue()
    {
        // Test if domain has soft-delete logic
    }
}
```

**What to test:**
- Entity creation with valid/invalid data
- Business rule enforcement (if domain has rich logic)
- Enum value mappings (EventTypeEnum → EventType lookup)
- Audit field behavior (CreatedAt, UpdatedAt)
- Soft delete behavior
- Navigation property integrity

**What NOT to test:**
- EF Core mapping (that's persistence layer)
- Simple property getters/setters
- Framework-generated equality

**Acceptance:** 80+ tests covering all entities with logic. `dotnet run --project Event.Domain.UnitTests` passes.

### 2.2 Blazor Client — 18 Untested Services (Priority: HIGH)

**Services grouped by implementation pattern:**

#### Group A: Simple Lookup Services (10 services, ~5 tests each = 50 tests)

These all follow the same pattern: wrap NSwag client call → parse HAL response → return DTO list.

| Service | API Method | Expected Tests |
|---------|-----------|----------------|
| EventTypeService | GetEventTypesAsync | 5 |
| EventStatusService | GetEventStatusesAsync | 5 |
| EventFormatService | GetEventFormatsAsync | 5 |
| AudienceAgeService | GetAudienceAgesAsync | 5 |
| AudienceGenderService | GetAudienceGendersAsync | 5 |
| MadhabService | GetMadhabsAsync | 5 |
| LanguageService | GetLanguagesAsync | 5 |
| ActorService | GetActorsAsync + CRUD | 8 |
| TagService | GetTagsAsync + CRUD | 8 |
| PublicExperienceService | GetPublicExperiencesAsync | 5 |

**IMPORTANT:** Not all lookup services are identical. Before writing tests, read each service implementation to determine which pattern it follows:

| Pattern | Services | Tests |
|---------|----------|-------|
| **HAL collection → DTO list** (with null/exception fallback) | Category, Location, Organization (reference pattern) | 5: success, null, exception, getById, getById-notFound |
| **Direct ICollection passthrough** (no HAL, no fallback) | Some lookups return `ICollection<T>` directly from NSwag | 3: success, exception, empty collection |
| **Simple one-liner passthrough** (no error handling) | EventType, EventStatus, EventFormat etc. | 2: success, exception (verify service doesn't silently swallow) |
| **CRUD (GetAll + Create/Update/Delete)** | Actor, Tag | 8: above + create success, create throws, update, delete |

**Per-service workflow:**
1. Read `Explore.Blazor.Client/Services/{Service}.cs` — identify which pattern above
2. Read its interface `I{Service}.cs` — identify all methods to test  
3. Create test file at `Explore.Blazor.Client.Tests/Services/{Service}Tests.cs`
4. Follow `EventServiceTests.cs` constructor pattern (mock `IEventApiClient` + `ILogger<T>`)
5. Write only tests that match the service's actual error handling behavior
6. Do NOT write 5 tests for a 3-line passthrough — match test depth to service complexity

**Optimization:** Create a shared test base or helper to reduce boilerplate across these 10 services.

#### Group B: Complex Services (5 services, ~15 tests each = 75 tests)

| Service | Complexity | Key Behaviors |
|---------|-----------|---------------|
| LookupCacheService | HIGH | Cache invalidation, thread safety (SemaphoreSlim), expiration |
| ImageStorageService | HIGH | Pre-signed URL generation, file validation, upload streaming |
| MapsService | MEDIUM | Geolocation parsing, coordinate validation |
| TenantOnboardingService | MEDIUM | Multi-step onboarding, state management |
| InstanceOnboardingService | MEDIUM | Instance setup, configuration validation |

**LookupCacheService tests (highest priority):**
1. `GetLookupData_ReturnsCachedData_WhenCacheIsValid`
2. `GetLookupData_FetchesFromApi_WhenCacheExpired`
3. `GetLookupData_FetchesFromApi_WhenCacheEmpty`
4. `InvalidateCache_ClearsAllCachedData`
5. `GetLookupData_HandlesApiFailure_ReturnsCachedData`
6. `GetLookupData_ThreadSafe_ConcurrentAccess`
7. `GetLookupData_DoesNotDeadlock_WhenCalledRecursively`

**ImageStorageService tests:**
1. `GetPresignedUploadUrlAsync_ReturnsUrl_WhenApiSucceeds`
2. `GetPresignedUploadUrlAsync_Throws_WhenApiReturnsError`
3. `UploadImageAsync_SucceedsWithValidFile`
4. `UploadImageAsync_Throws_WhenFileTooLarge`
5. `DeleteImageAsync_Succeeds_WhenImageExists`

#### Group C: Infrastructure Services (3 services, ~10 tests each = 30 tests)

| Service | Complexity | Key Behaviors |
|---------|-----------|---------------|
| BffClient | MEDIUM | XSRF token handling, credential passing |
| EventSessionSpeakerService | LOW | CRUD for session speakers |
| EventAspectService | LOW | Aspect composition for events |

**Total new Blazor tests: ~155 tests (50 + 75 + 30)**

**Acceptance:** Service coverage rises from 38% (11/29) to 100% (29/29). All 400+ tests pass.

### 2.3 Event.Persistence.IntegrationTests — Expand (Priority: HIGH)

**Current state:** 2 tests for EventRepository only.

**Repositories to test:**

| Repository | Tests Needed | Key Operations |
|-----------|-------------|----------------|
| EventRepository | +10 | Update, Delete, Filter, Paginate, Include |
| OrganizationRepository | 12 | Full CRUD, member queries |
| UserRepository | 8 | CRUD, tenant-scoped queries |
| ActorRepository | 6 | CRUD, type filtering |
| CategoryRepository | 6 | Hierarchical queries |
| TagRepository | 6 | Tag CRUD, type filtering |
| LocationRepository | 6 | Geo queries (PostGIS) |
| GenericRepository<T> | 8 | Base CRUD, soft delete filter |

**Test patterns:**
```csharp
[Test]
[ClassDataSource<PostgreSqlContainerFixture>(Shared = SharedType.PerAssembly)]
public async Task GetById_ReturnsEntity_WhenExists(PostgreSqlContainerFixture fixture)
{
    await using var context = fixture.CreateDbContext();
    var repo = new EventRepository(context);
    // Seed → Query → Assert
}
```

**Key scenarios per repository:**
1. Create entity → verify persisted
2. GetById → verify includes/navigation properties
3. Update entity → verify changes persisted
4. Soft delete → verify IsDeleted flag
5. Query with filter → verify correct results
6. Paginate → verify page size and offset
7. Concurrent operations → verify no data corruption

**Acceptance:** 50+ tests covering all repositories. All pass against TestContainers PostgreSQL.

### 2.4 Event.Application.UnitTests — Fill Gaps (Priority: MEDIUM)

**Current state:** 143 tests covering most handlers.

**Gaps to fill (based on handler inventory):**

| Feature Area | Handlers Without Tests | Tests Needed |
|-------------|----------------------|--------------|
| Event aspects | Aspect handlers | 10-15 |
| Event sessions | Session handlers | 10-15 |
| Lookup management | CRUD handlers for lookups | 10-15 |
| Validation | Validator tests | 15-20 |

**Focus areas:**
- Any handler that does conditional logic (not just delegation)
- Validators with complex rules (cross-field validation, async DB checks)
- Pipeline behaviors (validation, logging, transaction)

**Acceptance:** 200+ tests. All handlers with business logic have tests.

---

## Phase 3: Architecture & Integration Hardening (Weeks 5–6)

### 3.1 Expand Architecture Tests

**New rules to enforce:**

| Rule | Description |
|------|-------------|
| Handler interface compliance | All handlers implement `IRequestHandler<,>` |
| Repository interface compliance | All repos implement `IGenericRepository<,>` or entity-specific interface |
| Validator base class | All validators extend `AbstractValidator<T>` |
| Controller attribute | All controllers have `[ApiController]` and `[Route]` |
| No forbidden namespaces | Domain must not reference `System.Net`, `Microsoft.EntityFrameworkCore` |
| Sealed classes | Handlers, validators should be sealed (performance) |

**Acceptance:** 35+ architecture tests.

### 3.2 Blazor Component Tests (bUnit)

**Priority components to test:**

| Component | Test Type | Key Behaviors |
|-----------|----------|---------------|
| EventList page | Rendering | Loads data, displays cards, handles empty state |
| EventDetails page | Rendering | Displays details, handles not-found |
| CreateEvent form | Interaction | Form submission, validation, error display |
| OrganizationList page | Rendering | Lists orgs, pagination |
| Login/Auth flows | State | Auth state changes, redirects |

**Pattern:**
```csharp
[Test]
public async Task EventList_ShowsLoadingState_Initially()
{
    using var ctx = new BlazorTestContext();
    var cut = ctx.RenderComponent<EventList>();
    await Assert.That(cut.Find(".loading-indicator")).IsNotNull();
}
```

**Acceptance:** 30+ component tests covering critical UI paths.

### 3.3 API Integration Test Improvements

**Current gaps in 357 tests:**
- Auth bypass may not test real authorization logic
- Some tests may be too tightly coupled to response shape

**Actions:**
- Audit existing tests for behavioral compliance
- Add negative auth tests (unauthorized, forbidden)
- Add content negotiation tests
- Add rate limiting tests (if middleware exists)

---

## Phase 4: Continuous Quality (Ongoing)

### 4.1 Test Quality Gates

| Gate | Threshold | Enforced By |
|------|-----------|-------------|
| No `Received()` on read operations | 0 violations | Code review + architecture test |
| All error paths verify logging | 100% | Code review |
| New services require tests | 100% | PR policy |
| Test naming convention | `Method_State_Expected` | Architecture test |
| No `#pragma warning disable` in tests | 0 violations | Architecture test |

### 4.2 CI/CD Integration

```yaml
# Recommended test execution order in CI
steps:
  - dotnet build --configuration Release --verbosity quiet
  - dotnet run --project Event.Architecture.Tests        # Fast, catches structure issues
  - dotnet run --project Event.Domain.UnitTests          # Fast, pure logic
  - dotnet run --project Event.Application.UnitTests     # Fast, handler logic
  - dotnet run --project Explore.Secrets.UnitTests       # Fast, library tests
  - dotnet run --project Explore.Blazor.Client.Tests     # Medium, service + component
  - dotnet run --project Event.Persistence.IntegrationTests  # Slow, TestContainers
  - dotnet run --project Event.API.IntegrationTests      # Slow, WebApplicationFactory
```

### 4.3 Test Maintenance

- **Monthly:** Review and remove tests that no longer provide value
- **Per PR:** Ensure new code has tests, existing tests not broken
- **Per Sprint:** Review test execution time, optimize slow tests
- **Quarterly:** Run coverage analysis, identify new gaps

---

## Risk Assessment

| Risk | Likelihood | Impact | Mitigation |
|------|-----------|--------|------------|
| Over-testing simple lookups | HIGH | LOW | Use shared test bases, don't test library behavior |
| Test fragility from mock coupling | MEDIUM | HIGH | Phase 1 anti-pattern removal |
| TestContainers CI flakiness | MEDIUM | MEDIUM | Retry policies, container health checks |
| Large test suite slowing CI | LOW | MEDIUM | Parallel execution, test categorization |
| Domain entities have no testable logic | MEDIUM | LOW | Focus on entities WITH logic, skip pure data carriers |

---

## Timeline Estimate

| Phase | Duration | Tests Added | Cumulative |
|-------|----------|-------------|------------|
| Phase 1: Anti-pattern fixes | Week 1 | 0 (refactor) | 946 |
| Phase 2.1: Domain tests | Week 2 | +80 | 1,026 |
| Phase 2.2: Blazor service tests | Weeks 2–3 | +155 | 1,181 |
| Phase 2.3: Persistence tests | Week 3 | +48 | 1,229 |
| Phase 2.4: Application tests | Week 4 | +57 | 1,286 |
| Phase 3.1: Architecture tests | Week 5 | +11 | 1,297 |
| Phase 3.2: Blazor component tests | Week 5 | +30 | 1,327 |
| Phase 3.3: API test improvements | Week 6 | +43 | 1,370 |
| **Total** | **6 weeks** | **+424** | **1,370** |

---

## Success Metrics

| Metric | Current | Target | Deadline |
|--------|---------|--------|----------|
| Total tests | 946 | 1,370+ | Week 6 |
| Blazor service coverage | 38% | 100% | Week 3 |
| Domain test coverage | 0% | 80%+ | Week 2 |
| Persistence test coverage | 5% | 60%+ | Week 3 |
| `Received()` on read operations | ~40 | 0 | Week 1 |
| All error paths verify logging | 0% | 100% | Week 1 |
| CI green rate | Unknown | 95%+ | Week 6 |
