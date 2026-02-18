# Enterprise Testing Strategy — Context

## SESSION PROGRESS (2026-02-09)

### ✅ COMPLETED
- Full audit of all 7 test projects via parallel explore agents
- Created comprehensive test-strategy-plan.md, context.md, tasks.md
- Identified all anti-patterns, coverage gaps, and missing tests
- Technology stack decision: keep TUnit assertions (no FluentAssertions migration)
- Momus review passed — all findings addressed
- **Phase 1.4**: Pre-existing issues verified resolved (build clean, 946/946 tests pass)
- **Phase 1.1**: Removed 18 `.Received()` calls on read operations across 8 Blazor test files (230/230 pass)
- **Phase 2.1**: Domain unit tests — 61/61 pass. Created EventTests, OrganizationTests, EventSessionTests, UserTests, TenantTests, ActorTests, LocationTests, StorageObjectTests, EventIslamicAspectTests, EventTechAspectTests, InterfaceImplementationTests. Fixed `Event` namespace collision (FQN `Explore.Domain.Event`).
- **Phase 2.2**: Blazor lookup service tests — 337/337 pass (+107). Created 10 new files: EventTypeServiceTests, EventStatusServiceTests, EventFormatServiceTests, AudienceAgeServiceTests, AudienceGenderServiceTests, MadhabServiceTests, LanguageServiceTests, ActorServiceTests, TagServiceTests, PublicExperienceServiceTests. Fixed TagServiceTests HAL MasterCode/FullName required.
- **Total tests: 1,114 across 7 projects** (was 946)

### 🟡 IN PROGRESS
- **Phase 2.3**: Blazor complex service tests (LookupCacheService, ImageStorageService, EventSessionSpeakerService, EventAspectService, TenantOnboardingService)

### ⚠️ BLOCKERS / PRE-EXISTING ISSUES
- None currently

---

## Key Files

### Test Projects (7 total, 1,114 tests)

**Explore.Blazor.Client.Tests/** (337 tests)
- `Services/EventServiceTests.cs` — THE reference pattern for all Blazor service tests
- `Services/AdminServiceTests.cs` — Largest service test (~35 tests)
- `Services/AuthStateServiceTests.cs` — Good claim extraction testing
- `Services/UserServiceTests.cs` — Good retry/sync testing
- `Services/CategoryServiceTests.cs` — NEW (created in prior session)
- `Services/LocationServiceTests.cs` — NEW (created in prior session)
- `Common/ComponentDataBuilder.cs` — Bogus faker for test data
- `Common/MockServiceFactory.cs` — Pre-configured mock creation
- `Common/BlazorTestContext.cs` — bUnit test context wrapper
- `GlobalUsings.cs` — TUnit, NSubstitute, bUnit imports

**Event.Application.UnitTests/** (143 tests)
- `Features/Events/Commands/CreateEventCommandHandlerTests.cs` — Command handler pattern
- `Features/Events/Queries/GetEventDetailsRequestHandlerTests.cs` — Query + cache pattern
- `Features/Organizations/Commands/CreateOrganizationCommandHandlerTests.cs` — Multi-step creation
- `Features/Actors/Commands/CreateActorCommandHandlerTests.cs` — Validation logic
- `Features/Users/Commands/DeleteUserCommandHandlerTests.cs` — DataBuilder usage
- `Common/DataBuilder.cs` — Bogus faker for domain entities

**Event.API.IntegrationTests/** (357 tests)
- `Fixtures/CustomWebApplicationFactory.cs` — In-memory EF Core DB setup
- `Fixtures/ApiTestFixture.cs` — TUnit IAsyncInitializer, manages HttpClient
- `Fixtures/TestAuthHandler.cs` — Auth bypass with hardcoded Admin claims
- `Features/ApiEndpointSmokeTests.cs` — Reflection-based endpoint discovery
- `Features/Hateoas/HateoasIntegrationTests.cs` — HAL+JSON RFC validation
- `Features/ActorControllerTests.cs` — CRUD + auth assertions
- `Features/OrganizationControllerTests.cs` — Pagination + auth testing
- `appsettings.test.json` — Mock Keycloak/S3 endpoints

**Explore.Secrets.UnitTests/** (190 tests)
- `Services/KeyRotationServiceTests.cs` — Re-encryption workflows (475 lines)
- `Services/AesEncryptionServiceTests.cs` — Encryption round-trips (573 lines)
- `Services/RotationAwareDbContextFactoryTests.cs` — DB rotation + redaction
- `Services/RotationAwareHttpClientFactoryTests.cs` — HTTP client rotation
- `Providers/InfisicalSecretProviderTests.cs` — Config validation
- `Providers/AuditingSecretProviderDecoratorTests.cs` — Decorator pattern
- `Validation/SecretProviderOptionsValidatorTests.cs` — All provider types

**Event.Architecture.Tests/** (24 tests)
- `CleanArchitectureTests.cs` — Layer dependency rules (NetArchTest)
- `CqrsPatternTests.cs` — Command/Query separation enforcement
- `NamingConventionTests.cs` — Handler, DTO, repository naming

**Event.Persistence.IntegrationTests/** (2 tests)
- `Fixtures/PostgreSqlContainerFixture.cs` — TestContainers PostgreSQL (postgres:18-alpine)
- `Repositories/EventRepositoryTests.cs` — Create + GetWithDetails only

**Event.Domain.UnitTests/** (61 tests)
- `Entities/EventTests.cs`, `OrganizationTests.cs`, `EventSessionTests.cs`, `UserTests.cs`, `TenantTests.cs`, `ActorTests.cs`, `LocationTests.cs`, `StorageObjectTests.cs`
- `Aspects/EventIslamicAspectTests.cs`, `EventTechAspectTests.cs`
- `Interfaces/InterfaceImplementationTests.cs`
- NOTE: Bare `Event` resolves to namespace — must use `Explore.Domain.Event` FQN

### Source Code Under Test

**Explore.Blazor.Client/Services/** (29 services)
- 11 with tests: Admin, AuthState, Category, Event, EventRegistration, LandingPage, Location, OrganizationMember, OrganizationReview, Organization, User
- 8 without tests: BffClient, EventAspect, EventSessionSpeaker, ImageStorage, InstanceOnboarding, LookupCache, Maps, TenantOnboarding

**Explore.Application/Features/** (~207 handlers)
- ~10 handlers have tests (4.8% coverage)
- Tested: CreateEvent, GetEventDetails, CreateOrganization, GetOrganizationList, CreateActor, GetActorDetails, CreateEventSession, GetEventSessionDetails, CreateLocation, DeleteUser
- NOT tested: All Update handlers, most Delete handlers, most List/Query handlers, all federation handlers, all lookup handlers

**Explore.API/Controllers/** (~43 controllers)
- 9 with dedicated integration tests (~21%)
- Tested: Actor, Category, EventSession, Location, Organization, StorageObject, Tag, Tenant, User
- NOT tested: Event (only smoke), EventRegistration, EventSessionAgendaItem, OrganizationMember, OrganizationReview, TenantSettings, TenantUser, all lookup controllers, all onboarding controllers

---

## Important Decisions Made

### 1. TUnit Assertions (KEEP)
**Decision:** Do not migrate to FluentAssertions. Entire codebase uses `await Assert.That(...)` consistently. Migration would create inconsistency with zero behavioral benefit.

### 2. Received() Policy
**Decision:** Remove `Received()` on read operations. Keep ONLY for:
- Write operations where the side-effect IS the behavior
- Retry logic verification
- Error logging verification

### 3. Separate Tests vs Parameterized
**Decision:** Keep separate null/exception tests. Each documents a distinct contract even if current implementation is identical. They guard against future divergence.

### 4. Test Execution Order
**Decision:** Architecture tests first (fast, catches structure issues), then unit tests (fast, pure logic), then integration tests (slow, external deps).

### 5. In-Memory DB vs TestContainers for API Tests
**Observation:** API tests use in-memory EF Core. Persistence tests use TestContainers PostgreSQL. Both approaches coexist. Consider migrating API tests to TestContainers long-term for schema validation, but not in this sprint.

---

## Technical Constraints

1. **NSwag-generated client** (`EventApiClient.g.cs`) — DO NOT MODIFY, ~28k lines
2. **Domain entities** in `Explore.Domain/` — DO NOT MODIFY (per user constraint)
3. **TUnit runs as exe** — use `dotnet run --project <TestProject>` not `dotnet test`
4. **French-locale Windows** — `findstr /i` unreliable, use exact case
5. **HAL response mocking** — Must set `MasterCode` on `CategoryDto` (Required.Always in NSwag)
6. **NSubstitute + ILogger** — Cannot mock `LogError()` directly (extension method); must mock `Log()` with verbose signature
7. **CancellationToken mocking** — NSwag methods have optional CancellationToken; must mock BOTH `Method(arg, Arg.Any<CancellationToken>())` AND `Method(arg)` for NSubstitute to match

---

## Anti-Pattern Inventory (from audit)

### Blazor Client Tests
- ~40 instances of `Received()` on read operations
- Logger mocked but never verified in error paths
- 38% service coverage (11/29)

### Application Unit Tests
- IMapper mocked everywhere (mapping logic untested)
- `Received(1)` on repository calls (brittle)
- Only 4.8% handler coverage (10/207)
- HybridCache compilation errors (pre-existing)

### API Integration Tests
- No test data seeding (in-memory DB starts empty)
- `SharedType.PerAssembly` — no test isolation
- No multi-role auth testing (always Admin)
- Only status code assertions (no body validation)
- 21% controller coverage (9/43)

### Persistence Integration Tests
- Only 2 tests for 1 repository
- No Update/Delete/Filter/Paginate tests
- Uses `EnsureCreated()` instead of `Migrate()`

### Domain Unit Tests
- 0% coverage — placeholder only

---

## Quick Resume

To continue work on this task:
1. Read this file for current state
2. Read `test-strategy-tasks.md` for remaining checklist
3. Read `test-strategy-plan.md` for overall strategy
4. Start with Phase 1 (anti-pattern fixes) — it has the highest ROI with no new code
5. Then Phase 2 (coverage gaps) — prioritize Domain and Blazor services
## Context Reset Session Update (2026-02-15 21:25 Europe/Brussels)

- Current implementation state: No new implementation changes in this session for this track.
- Key decisions made this session: Priority shifted to analytics implementation completion and verification.
- Files modified and why: None in this track during this session.
- Blockers/issues discovered: None newly discovered for this track.
- Next immediate steps: Continue from highest-priority unchecked items in `test-strategy-tasks.md`.
