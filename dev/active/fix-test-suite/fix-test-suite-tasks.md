# Task Checklist: Comprehensive Test Suite Restoration

**Last Updated: 2026-02-05**

This checklist is for tracking the progress of fixing the solution's test suite. Mark items as complete as they are finished.

## Phase 1: Foundational Fixes (Critical Path)

- [x] **Task 1.1: Fix Persistence Layer Dependency Conflict**
    - [x] Open `Event.Persistence.IntegrationTests.csproj`.
    - [x] Add a `<PackageReference Include="Microsoft.EntityFrameworkCore.Relational" Version="10.0.2" />`.
    - [ ] ~~Run `dotnet test Event.Persistence.IntegrationTests` and confirm the 2 tests pass.~~ (Blocked)
    - [x] **New**: Rename `Key` properties in domain entities (`AppSetting`, `ModuleDefinition`, `SystemSetting`, `TenantSetting`) to avoid PostgreSQL keyword collision.
    - [x] **New**: Update all corresponding configurations, repositories, and services in `Explore.Persistence` and `Explore.Infrastructure` to use the new property names (`ConfigKey`, `ModuleKey`, `SettingKey`).
    - [ ] **New**: Create a new database migration to apply the property name changes to the schema.
    - [ ] **New**: Change `EnsureCreated()` to `Migrate()` in `PostgreSqlContainerFixture.cs` to ensure test database uses migrations.
    - [ ] **New**: Run `dotnet test Event.Persistence.IntegrationTests` and confirm the 2 tests pass.

- [ ] **Task 1.2: Correct Blazor Client Test Context**
    - [ ] Identify all failing tests in `Explore.Blazor.Client.Tests` that throw `InvalidOperationException` for `IUserService`.
    - [ ] In each failing test, ensure the `BlazorTestContext` is initialized and `AddAllCoreMocks()` is called before rendering the component.
    - [ ] Run `dotnet test Explore.Blazor.Client.Tests` and confirm the number of DI-related errors is zero.

- [ ] **Task 1.3: Fix API Integration Test DI Configuration**
    - [ ] Open `Event.API.IntegrationTests/Fixtures/CustomWebApplicationFactory.cs`.
    - [ ] In the `ConfigureWebHost` method, add the line `builder.Host.ConfigureApplicationServices();`.
    - [ ] Run `dotnet test Event.API.IntegrationTests` and verify that the number of `500 Internal Server Error` responses has decreased substantially.

## Phase 2: Test Logic & Assertion Correction

- [ ] **Task 2.1: Fix Secrets Unit Test Assertions**
    - [ ] Open `Explore.Secrets.UnitTests/Services/RotationAwareDbContextFactoryTests.cs`.
    - [ ] In `CurrentConnectionStringRedacted_ShouldRedactPassword`, change the assertion to expect `"Password=***"`.
    - [ ] In `CurrentConnectionStringRedacted_WithPwd_ShouldRedact`, change the assertion to expect `"Pwd=***"`.
    - [ ] Run `dotnet test Explore.Secrets.UnitTests` and confirm all tests pass.

- [ ] **Task 2.2: Implement API Test Authentication**
    - [ ] Create a `TestAuthHandler.cs` in `Event.API.IntegrationTests/Fixtures/`.
    - [ ] In `CustomWebApplicationFactory.cs`, add services for authentication and register the `TestAuthHandler` as the default scheme for tests.
    - [ ] Add a helper method to the test base or factory to set the specific claims for each test (e.g., `WithTestUser(claims)`).
    - [ ] Run `dotnet test Event.API.IntegrationTests` and confirm the number of `401 Unauthorized` errors has decreased substantially.

- [ ] **Task 2.3: Refactor and Correct API Integration Tests**
    - [ ] Go through each remaining failing test in the `Event.API.IntegrationTests` project.
    - [ ] Analyze the failure (e.g., incorrect status code, bad link, wrong payload).
    - [ ] Update the test's arrangement, action, or assertion to match the current, correct application behavior.
    - [ ] Repeat until all tests in the project pass.

- [ ] **Task 2.4: Refactor and Correct Blazor Client Tests**
    - [ ] Go through each remaining failing test in the `Explore.Blazor.Client.Tests` project.
    - [ ] For tests in `EventServiceTests`, ensure the mock `HttpClient` (via `MockHttp`) is set up correctly to return the expected responses for the API calls being made.
    - [ ] Fix any remaining assertion logic errors.
    - [ ] Repeat until all tests in the project pass.

## Phase 3: Final Verification

- [ ] **Task 3.1: Full Solution Test Run**
    - [ ] Run `dotnet test` from the root of the solution.
    - [ ] Confirm the final output shows all 825 tests passing.
    - [ ] Review the build output for any lingering warnings and address them.
