# Context: Test Suite Restoration

**Last Updated: 2026-02-05**

This document contains the essential context and key file locations required to execute the plan for restoring the test suite. The information is derived from the initial `dotnet test` output and the subsequent `codebase_investigator` analysis.

## 1. Key Findings Summary

*   **Dependency Conflicts**: There is a clear version mismatch for `Microsoft.EntityFrameworkCore.Relational` affecting `Event.Persistence.IntegrationTests`. The build resolves to `10.0.1.0` while the runtime tries to load `10.0.2.0`.
*   **Incomplete DI in Tests**: Both Blazor client and API integration tests are failing because their respective test environments are not configured to register all necessary application services.
*   **Missing Test Authentication**: The API integration tests lack a proper test authentication scheme, causing failures on all protected endpoints.
*   **Test-Code Drift**: Assertions and test logic are outdated and do not reflect the current state of the application's behavior and API contracts.

## 2. Relevant File Paths & Purpose

### Phase 1: Foundational Fixes

*   **`Event.Persistence.IntegrationTests/Event.Persistence.IntegrationTests.csproj`**
    *   **Purpose**: This project file needs to be modified to add a direct package reference to `Microsoft.EntityFrameworkCore.Relational` to resolve the `FileNotFoundException`.

*   **`Explore.Blazor.Client.Tests/Common/BlazorTestContext.cs`**
    *   **Purpose**: This file provides the `AddAllCoreMocks()` helper method. Failing component tests need to be updated to use this method to correctly inject mocked services like `IUserService`.

*   **`Explore.Blazor.Client.Tests/Common/MockServiceFactory.cs`**
    *   **Purpose**: Confirms that the mocking infrastructure for `IUserService` and other core services already exists and is ready to be used via the `BlazorTestContext`.

*   **`Event.API.IntegrationTests/Fixtures/CustomWebApplicationFactory.cs`**
    *   **Purpose**: The central configuration point for API integration tests. It needs to be modified to:
        1.  Call the `ConfigureApplicationServices()` extension method to register MediatR handlers and other application services.
        2.  Register a `TestAuthHandler` to provide a simulated user principal for authenticated test runs.

*   **`Explore.Application/ApplicationServicesRegistration.cs`**
    *   **Purpose**: Contains the `ConfigureApplicationServices` extension method. This is the source of truth for application layer DI registration that needs to be mirrored in the integration test setup.

*   **`Explore.Domain/AppSetting.cs`**
    *   **Purpose**: Modified to rename the `Key` property to `ConfigKey`.

*   **`Explore.Persistence/Configurations/Entities/AppSettingConfiguration.cs`**
    *   **Purpose**: Updated to reflect the property rename in `AppSetting`.

*   **`Explore.Domain/Modules/ModuleDefinition.cs`**
    *   **Purpose**: Modified to rename the `Key` property to `ModuleKey`.

*   **`Explore.Persistence/Configurations/Entities/ModuleDefinitionConfiguration.cs`**
    *   **Purpose**: Updated to reflect the property rename in `ModuleDefinition`.

*   **`Explore.Domain/SystemSetting.cs`**
    *   **Purpose**: Modified to rename the `Key` property to `SettingKey`.

*   **`Explore.Persistence/Configurations/Entities/SystemSettingConfiguration.cs`**
    *   **Purpose**: Updated to reflect the property rename in `SystemSetting`.

*   **`Explore.Domain/TenantSetting.cs`**
    *   **Purpose**: Modified to rename the `Key` property to `SettingKey`.

*   **`Explore.Persistence/Configurations/Entities/TenantSettingConfiguration.cs`**
    *   **Purpose**: Updated to reflect the property rename in `TenantSetting`.

*   **`Explore.Persistence/Repositories/TenantSettingRepository.cs`**
    *   **Purpose**: Updated to use the new `SettingKey` property.

*   **`Explore.Persistence/Repositories/TenantCapabilityRepository.cs`**
    *   **Purpose**: Updated to use the new `ModuleKey` property.

*   **`Explore.Persistence/Repositories/AppSettingRepository.cs`**
    *   **Purpose**: Updated to use the new `ConfigKey` property.

*   **`Explore.Persistence/Repositories/SystemSettingRepository.cs`**
    *   **Purpose**: Updated to use the new `SettingKey` property.

*   **`Explore.Persistence/Repositories/ModuleDefinitionRepository.cs`**
    *   **Purpose**: Updated to use the new `ModuleKey` property.

*   **`Explore.Application/Contracts/Infrastructure/IModuleService.cs`**
    *   **Purpose**: Modified to rename the `Key` property to `ModuleKey` in the `ModuleInfo` DTO.

*   **`Explore.Infrastructure/Services/ModuleService.cs`**
    *   **Purpose**: Updated to use the new `ModuleKey` property from `ModuleInfo`.

*   **`Explore.Infrastructure/Services/SettingsResolver.cs`**
    *   **Purpose**: Updated to use the new `SettingKey` and `ConfigKey` properties.

*   **`Explore.Infrastructure/Strategies/StrategyResolver.cs`**
    *   **Purpose**: Updated to use the new `ModuleKey` property from `ModuleInfo`.

### Phase 2: Test Logic Correction

*   **`Explore.Secrets.UnitTests/Services/RotationAwareDbContextFactoryTests.cs`**
    *   **Purpose**: Contains simple assertion errors. The expected strings for the redacted connection string tests need to be corrected.

*   **`Event.API.IntegrationTests/**/*.cs` (All test files)**
    *   **Purpose**: These files contain the 233 failing tests that need systematic review and refactoring. Assertions for status codes, HATEOAS links, and JSON payloads must be updated.

*   **`Explore.Blazor.Client.Tests/**/*.cs` (All test files)**
    *   **Purpose**: These files contain the 28 failing tests. After fixing the DI issues, the remaining failures (e.g., in `EventServiceTests`) will need to be addressed by correcting mock setups and assertions.
