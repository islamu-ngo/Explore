# Enterprise Testing Strategy — Task Checklist

**Last Updated: 2026-02-09**

---

## Phase 1: Anti-Pattern Fixes ✅ COMPLETE

### 1.1 Remove Over-Mocking in Blazor Client Tests ✅
- [x] `EventServiceTests.cs` — Removed `Received()` on GetEventsAsync, GetMyEventsAsync, GetEventByIdAsync, GetEventSessionByIdAsync
- [x] `CategoryServiceTests.cs` — Removed `Received()` on GetCategoriesAsync (2 instances)
- [x] `LocationServiceTests.cs` — Removed `Received()` on GetLocationsAsync (2 instances)
- [x] `AdminServiceTests.cs` — Removed `Received()` on GetOrganizationsAsync, **kept writes**
- [x] `OrganizationServiceTests.cs` — Removed `Received()` on GetMyOrganizationsAsync (2 instances)
- [x] `OrganizationMemberServiceTests.cs` — Removed `Received()` on OrganizationmemberAllAsync
- [x] `OrganizationReviewServiceTests.cs` — Removed `Received()` on OrganizationreviewAllAsync, UserAllAsync
- [x] `UserServiceTests.cs` — Removed `Received()` on UserGETAsync (2 instances), **kept SyncAsync**
- [x] Verified all 230 Blazor tests still pass ✅
- **Total: 18 removals across 8 files, 9 write-operation Received() kept**

### 1.2 Add Error Logging Verification (only for services that actually log today)
- [ ] `EventServiceTests.cs` — Add `_logger.Received(1).Log(...)` to all error-path tests
- [ ] `CategoryServiceTests.cs` — Add logging verification to error paths
- [ ] `LocationServiceTests.cs` — Add logging verification to error paths
- [ ] `AdminServiceTests.cs` — Add logging verification to error paths
- [ ] `OrganizationServiceTests.cs` — Add logging verification to error paths
- [ ] `UserServiceTests.cs` — Add logging verification to error paths
- [ ] Create shared helper: `AssertLoggerReceivedError(ILogger logger)` in `Common/`
- [ ] Verify all tests pass

### 1.3 Review Redundant Tests
- [ ] Review null vs exception test pairs across all service test files
- [ ] Decision: keep separate (documents distinct contract) — no consolidation needed
- [ ] Verify each pair genuinely tests different error handling behavior

### 1.4 Fix Pre-Existing Issues ✅
- [x] HybridCache compilation — already resolved (package reference exists, LSP issue was transient)
- [x] ILogger<> in OrganizationServiceTests — already resolved (compiles and runs clean)
- [x] Verified `dotnet build --configuration Release --verbosity quiet` clean (0 errors, 0 warnings)

---

## Phase 2: Fill Critical Coverage Gaps 🟡 IN PROGRESS (2.1-2.2 DONE)

### 2.1 Event.Domain.UnitTests — Build from Zero ✅
- [x] Replace placeholder `Program.cs` with TUnit test runner
- [x] Add project reference to `Explore.Domain`
- [x] Add Bogus package for test data generation
- [x] Create `EventTests.cs` — entity creation, default values, soft delete
- [x] Create `OrganizationTests.cs` — membership rules, validation
- [x] Create `EventSessionTests.cs` — speaker/language composition
- [x] Create `UserTests.cs` — data carrier validation
- [x] Create `TenantTests.cs` — settings, mode behavior
- [x] Create `ActorTests.cs` — type validation
- [x] Create `LocationTests.cs` — coordinate validation
- [x] Create `StorageObjectTests.cs` — file type validation
- [x] Create `EventIslamicAspectTests.cs` — Islamic aspect composition (NEW)
- [x] Create `EventTechAspectTests.cs` — Tech aspect composition (NEW)
- [x] Create `InterfaceImplementationTests.cs` — IAuditableEntity, ISoftDeletable, ITenantEntity (NEW)
- [x] Fixed namespace collision: bare `Event` resolves to namespace; use `Explore.Domain.Event` FQN
- [x] Verify: `dotnet run --project Event.Domain.UnitTests` — **61/61 tests pass** ✅

### 2.2 Blazor Client — Lookup Services ✅
- [x] Create `EventTypeServiceTests.cs`
- [x] Create `EventStatusServiceTests.cs`
- [x] Create `EventFormatServiceTests.cs`
- [x] Create `AudienceAgeServiceTests.cs`
- [x] Create `AudienceGenderServiceTests.cs`
- [x] Create `MadhabServiceTests.cs`
- [x] Create `LanguageServiceTests.cs`
- [x] Create `ActorServiceTests.cs` (CRUD — 8 tests)
- [x] Create `TagServiceTests.cs` (CRUD — 8 tests, fixed HAL MasterCode/FullName required)
- [x] Create `PublicExperienceServiceTests.cs`
- [x] Verify all new tests pass — **337/337 Blazor tests pass** ✅ (+107 from 230)

### 2.3 Blazor Client — Complex + Infrastructure Services (Priority: HIGH) 🟡 IN PROGRESS
- [ ] Create `LookupCacheServiceTests.cs` (~12 tests)
  - [ ] Cache hit returns cached data (second call within TTL)
  - [ ] Cache miss fetches from underlying service
  - [ ] InvalidateAll clears cache, next call re-fetches
  - [ ] Concurrent access (SemaphoreSlim prevents double-fetch)
  - [ ] API failure propagates (no try/catch in cache layer)
  - [ ] Multiple lookup types use same pattern
- [ ] Create `EventAspectServiceTests.cs` (~12 tests)
  - [ ] Get Islamic/Tech: success, 404→null, API error→null, exception→null
  - [ ] Upsert Islamic/Tech: success, API error→null
  - [ ] Delete Islamic/Tech: success→true, 404→true (idempotent), error→false
- [ ] Create `ImageStorageServiceTests.cs` (~15 tests)
  - [ ] ReadFileAsync: valid file, null file, oversized file
  - [ ] GetUploadUrlAsync: success, null response, API error
  - [ ] UploadImageFromBytesAsync: success, failure, empty URL, null data
  - [ ] UploadAndCreateRecordFromBytesAsync: full flow success, partial failures
  - [ ] GetImageUrlAsync: success, empty key
  - [ ] GenerateLocalPreviewFromBytes: valid data, null data
- [ ] Create `MapsServiceTests.cs` (~4 tests)
  - [ ] Success (returns URL with quotes stripped)
  - [ ] Empty query → empty string
  - [ ] HTTP error → empty string
  - [ ] Exception → empty string
- [ ] Create `TenantOnboardingServiceTests.cs` (~8 tests)
  - [ ] GetStatus: success, exception→null
  - [ ] GetSettings: success, exception→default model
  - [ ] Complete: success, exception→failure response
  - [ ] UpdateSettings: success, error response
- [ ] Create `InstanceOnboardingServiceTests.cs` (~8 tests)
  - [ ] Same pattern as TenantOnboarding
- [ ] Verify all new tests pass
- [ ] **Verify total Blazor tests: 395+ pass**

### 2.4 SKIPPED — Stub/Low-Value Services
- [x] EventSessionSpeakerService — ALL methods are TODO stubs (hardcoded returns), no testable behavior
- [x] BffClient — Requires IJSRuntime mocking for XSRF cookie, minimal logic (3 one-liners)

### 2.5 Event.Persistence.IntegrationTests — Expand (Priority: HIGH)
- [ ] Expand `EventRepositoryTests.cs` (+10 tests)
  - [ ] Update entity
  - [ ] Soft delete
  - [ ] GetById
  - [ ] Filter by status
  - [ ] Paginate results
  - [ ] Query with includes
- [ ] Create `OrganizationRepositoryTests.cs` (12 tests)
- [ ] Create `UserRepositoryTests.cs` (8 tests)
- [ ] Create `ActorRepositoryTests.cs` (6 tests)
- [ ] Create `CategoryRepositoryTests.cs` (6 tests)
- [ ] Create `TagRepositoryTests.cs` (6 tests)
- [ ] Create `LocationRepositoryTests.cs` (6 tests)
- [ ] Create `GenericRepositoryTests.cs` (8 tests — base CRUD, soft delete filter)
- [ ] Verify: `dotnet run --project Event.Persistence.IntegrationTests` — 50+ tests pass

### 2.6 Event.Application.UnitTests — Fill Gaps (Priority: MEDIUM)
- [ ] Audit which handlers have logic worth testing (skip pure delegation)
- [ ] Add Update handler tests (UpdateEvent, UpdateOrganization, UpdateActor) — 15 tests
- [ ] Add Delete handler tests (DeleteEvent, DeleteOrganization, DeleteActor) — 10 tests
- [ ] Add List/Query handler tests (GetEventList, GetActorList, GetLocationList) — 15 tests
- [ ] Add Validator tests (CreateEventDtoValidator, CreateOrganizationDtoValidator) — 15 tests
- [ ] Verify: `dotnet run --project Event.Application.UnitTests` — 200+ tests pass

---

## Phase 3: Architecture & Integration Hardening ⏳ NOT STARTED

### 3.1 Expand Architecture Tests
- [ ] Add handler interface compliance test (`IRequestHandler<,>`)
- [ ] Add repository interface compliance test (`IGenericRepository<,>`)
- [ ] Add validator base class test (`AbstractValidator<T>`)
- [ ] Add controller attribute test (`[ApiController]`, `[Route]`)
- [ ] Add forbidden namespace tests (Domain must not ref `System.Net`, `Microsoft.EntityFrameworkCore`)
- [ ] Add sealed class recommendation test (handlers, validators)
- [ ] Verify: 35+ architecture tests pass

### 3.2 Blazor Component Tests (bUnit)
- [ ] Create `EventListTests.cs` — renders loading state, displays cards, handles empty
- [ ] Create `EventDetailsTests.cs` — displays details, handles not-found
- [ ] Create `CreateEventTests.cs` — form submission, validation display
- [ ] Create `OrganizationListTests.cs` — lists orgs, pagination
- [ ] Create `AuthStateTests.cs` — auth state changes, redirects
- [ ] Verify: 30+ component tests pass

### 3.3 API Integration Test Improvements
- [ ] Create test data builders (ActorBuilder, OrganizationBuilder, EventBuilder)
- [ ] Add `DbContextExtensions.SeedTestData()` method
- [ ] Add multi-role auth testing (Admin vs User vs Anonymous)
- [ ] Add validation error tests (400/422 responses)
- [ ] Add response body validation (not just status codes)
- [ ] Add missing controller tests: Event, EventRegistration, OrganizationMember, OrganizationReview
- [ ] Verify: 400+ API integration tests pass

---

## Phase 4: Continuous Quality ⏳ NOT STARTED

### 4.1 Test Quality Gates
- [ ] Add architecture test: no `Received()` on read-only API client methods in Blazor tests
- [ ] Add architecture test: all test methods follow `Method_State_Expected` naming
- [ ] Document test patterns in `docs/TESTING.md`
- [ ] Add CI pipeline configuration for ordered test execution

### 4.2 Coverage Analysis
- [ ] Run coverage analysis (Coverlet or dotnet-coverage)
- [ ] Identify remaining gaps above 80% threshold
- [ ] Create follow-up tasks for uncovered areas

---

## Summary Tracker

| Phase | Status | Tests Before | Tests After | Delta |
|-------|--------|-------------|-------------|-------|
| Phase 1: Anti-pattern fixes | ✅ Done | 946 | 946 | 0 (refactor) |
| Phase 2.1: Domain tests | ✅ Done | 946 | 1,007 | +61 |
| Phase 2.2: Blazor lookups | ✅ Done | 1,007 | 1,114 | +107 |
| Phase 2.3: Blazor complex | ⏳ Not Started | 1,114 | ~1,163 | ~+49 |
| Phase 2.4: Blazor infra | ⏳ Not Started | ~1,163 | ~1,189 | ~+26 |
| Phase 2.5: Persistence | ⏳ Not Started | ~1,189 | ~1,251 | ~+62 |
| Phase 2.6: Application | ⏳ Not Started | ~1,251 | ~1,306 | ~+55 |
| Phase 3.1: Architecture | ⏳ Not Started | ~1,306 | ~1,317 | ~+11 |
| Phase 3.2: Blazor components | ⏳ Not Started | ~1,317 | ~1,347 | ~+30 |
| Phase 3.3: API improvements | ⏳ Not Started | ~1,347 | ~1,400 | ~+53 |
| Phase 4: Quality gates | ⏳ Not Started | ~1,400 | ~1,400 | 0 (infra) |
| **TOTAL** | | **946** | **~1,400** | **~+454** |

**Note:** Test counts are estimates. Actual numbers will vary based on per-service complexity analysis (Phase 2.2 services range from 2-8 tests depending on implementation pattern). Adjust totals as services are audited.
## Context Reset Session Update (2026-02-15 21:26 Europe/Brussels)

- Status update: No task-state changes in this session for this track.
- Priority update: Keep existing ordering; analytics work was handled in a separate track.
- Next step: Resume from current in-progress or highest-priority unchecked item.
