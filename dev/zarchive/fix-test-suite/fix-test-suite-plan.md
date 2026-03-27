# Plan: Comprehensive Test Suite Restoration

**Last Updated: 2026-02-05**

## 1. Executive Summary

The solution's test suite is currently in a critical state, with 265 failing tests across four major projects. The failures are systemic, stemming from dependency conflicts, incomplete test environment configuration (Dependency Injection, Authentication), and a clear drift between the application code and the tests themselves.

This plan outlines a phased approach to restore the entire test suite to a passing state. The strategy is to address the foundational issues first (package dependencies, DI container setup) and then move to fixing the individual test cases. This will ensure a stable foundation for future development and prevent regressions.

## 2. Current State Analysis

The investigation has identified the following root causes for the failures:

*   **`Event.Persistence.IntegrationTests` (2 Failures):** A `FileNotFoundException` is caused by a missing direct package reference to `Microsoft.EntityFrameworkCore.Relational`, leading to a runtime assembly loading failure.
*   **`Explore.Secrets.UnitTests` (2 Failures):** Minor assertion errors in `RotationAwareDbContextFactoryTests.cs` due to case-sensitivity mistakes in the expected connection strings.
*   **`Explore.Blazor.Client.Tests` (28 Failures):** Widespread `InvalidOperationException` because the test context is not providing necessary services, specifically `IUserService`. The existing `AddAllCoreMocks` helper is not being utilized in the failing tests.
*   **`Event.API.IntegrationTests` (233 Failures):**
    *   **MediatR Handler Failure:** The primary cause of the numerous `500 Internal Server Error` responses is the test `CustomWebApplicationFactory`'s failure to register application services, including all MediatR handlers.
    *   **Authentication Failure:** A large number of `401 Unauthorized` responses indicate that the test authentication scheme is either missing or misconfigured.
    *   **Outdated Assertions:** Many tests fail with incorrect status code assertions (e.g., expecting `BadRequest` but getting `NotFound`), indicating that API behavior has changed and the tests have not been updated.

## 3. Proposed Future State

*   All 825 tests in the solution pass successfully when `dotnet test` is executed.
*   Test environments (unit, integration, Blazor) are correctly configured with a robust and consistent DI and authentication setup.
*   All tests are refactored to align with the current application architecture, patterns, and API contracts.
*   The test suite is stable and reliable, providing an accurate measure of code quality and regression detection.

## 4. Implementation Phases & Tasks

### Phase 1: Foundational Fixes (Critical Path)

This phase addresses the core configuration issues that cause the majority of cascading failures.

*   **Task 1.1: Fix Persistence Layer Dependency Conflict**
    *   **Project**: `Event.Persistence.IntegrationTests`
    *   **File**: `Event.Persistence.IntegrationTests.csproj`
    *   **Action**: Add a direct `<PackageReference>` for the correct version of `Microsoft.EntityFrameworkCore.Relational`.
    *   **Acceptance Criteria**: The 2 tests in `Event.Persistence.IntegrationTests` pass.
    *   **Effort**: S
    *   **Progress**:
        *   Added the `PackageReference` to `Event.Persistence.IntegrationTests.csproj`.
        *   This resolved the initial `FileNotFoundException` but revealed a `PostgresException: column "key" does not exist`.
        *   Diagnosed this as a keyword collision with PostgreSQL caused by properties named `Key` in several domain entities (`AppSetting`, `ModuleDefinition`, `SystemSetting`, `TenantSetting`).
        *   Refactored the `Key` properties to `ConfigKey`, `ModuleKey`, and `SettingKey` respectively across the domain, configuration, and repository layers.
        *   The build is still failing due to errors in `Explore.Infrastructure`, which are being addressed. The original "key" column error is believed to be resolved, but a new error `column "config_key" does not exist` has appeared, indicating that the database schema is out of sync with the model. A new migration is required.

*   **Task 1.2: Correct Blazor Client Test Context**
    *   **Project**: `Explore.Blazor.Client.Tests`
    *   **Action**: Refactor all failing Blazor component tests to use the `AddAllCoreMocks()` helper from `BlazorTestContext.cs` to ensure `IUserService` and other dependencies are correctly mocked and injected.
    *   **Acceptance Criteria**: The DI-related `InvalidOperationException` failures in `Explore.Blazor.Client.Tests` are resolved.
    *   **Effort**: M

*   **Task 1.3: Fix API Integration Test DI Configuration**
    *   **Project**: `Event.API.IntegrationTests`
    *   **File**: `Event.API.IntegrationTests/Fixtures/CustomWebApplicationFactory.cs`
    *   **Action**: Modify the `ConfigureServices` method to include the registration of application services by calling `builder.Host.ConfigureApplicationServices()`.
    *   **Acceptance Criteria**: The number of `500 Internal Server Error` failures in `Event.API.IntegrationTests` is significantly reduced. MediatR handlers are correctly resolved.
    *   **Effort**: M

### Phase 2: Test Logic & Assertion Correction

With the foundations fixed, this phase focuses on fixing the logic within the tests themselves.

*   **Task 2.1: Fix Secrets Unit Test Assertions**
    *   **Project**: `Explore.Secrets.UnitTests`
    *   **File**: `Services/RotationAwareDbContextFactoryTests.cs`
    *   **Action**: Correct the expected redacted strings in the two failing tests to match the actual output, respecting case sensitivity (`Password` and `Pwd`).
    *   **Acceptance Criteria**: The 2 tests in `Explore.Secrets.UnitTests` pass.
    *   **Effort**: S

*   **Task 2.2: Implement API Test Authentication**
    *   **Project**: `Event.API.IntegrationTests`
    *   **File**: `Event.API.IntegrationTests/Fixtures/CustomWebApplicationFactory.cs`
    *   **Action**: Implement a test authentication handler (`TestAuthHandler`) and register it in the test server to simulate authenticated users with specific claims (e.g., admin role, standard user).
    *   **Acceptance Criteria**: The `401 Unauthorized` failures are resolved. Tests requiring auth can now run with a simulated user principal.
    *   **Effort**: L

*   **Task 2.3: Refactor and Correct API Integration Tests**
    *   **Project**: `Event.API.IntegrationTests`
    *   **Action**: Systematically work through the remaining failing tests. Update assertions for status codes, HATEOAS links, and response payloads to match the current API behavior. Remove or refactor tests that are no longer relevant due to application changes.
    *   **Acceptance Criteria**: All 233 tests in `Event.API.IntegrationTests` pass.
    *   **Effort**: XL

*   **Task 2.4: Refactor and Correct Blazor Client Tests**
    *   **Project**: `Explore.Blazor.Client.Tests`
    *   **Action**: Address the remaining non-DI failures, particularly the `ApiException` and assertion issues in `EventServiceTests`. Ensure mock API setups are correct and assertions are valid.
    *   **Acceptance Criteria**: All 28 tests in `Explore.Blazor.Client.Tests` pass.
    *   **Effort**: M

### Phase 3: Final Verification

*   **Task 3.1: Full Solution Test Run**
    *   **Action**: Execute `dotnet test` on the entire solution.
    *   **Acceptance Criteria**: The command completes with 100% of tests passing. There are no build warnings related to dependency conflicts.
    *   **Effort**: S

## 5. Risk Assessment and Mitigation

*   **Risk**: Fixing one issue reveals deeper, unknown problems (e.g., database schema mismatches in integration tests).
    *   **Mitigation**: The phased approach is designed to uncover foundational issues first. If major new problems arise, this plan will be updated, and the new issues will be prioritized.
*   **Risk**: High effort required for refactoring the large number of API tests could lead to delays.
    *   **Mitigation**: Prioritize fixing tests by feature area. Focus on getting critical paths (e.g., event creation, auth endpoints) working first.

## 6. Success Metrics

*   **Primary Metric**: 100% of tests passing in the final `dotnet test` run.
*   **Secondary Metric**: A significant reduction in build warnings, especially `NU1608` (dependency conflict) and `MSB3277` (assembly conflict).
*   **Qualitative Metric**: The test suite is demonstrably faster and more stable.

## 7. Required Resources

*   **Primary**: Developer time.
*   **Tools**: .NET 10 SDK, IDE (Visual Studio/Rider), Git.
*   **Documentation**: Existing project documentation, `dev-docs.md` command output, MediatR and bUnit documentation if needed.
