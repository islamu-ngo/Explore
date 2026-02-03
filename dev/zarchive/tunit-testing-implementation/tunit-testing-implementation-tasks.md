# TUnit Testing Implementation Tasks

## Phase 1: Foundation & Unit Testing (Event.Application.UnitTests)
- [ ] **Scaffold Project**
  - Create Console App: `Event.Application.UnitTests`
  - Add References: `Explore.Application`, `Explore.Domain`
  - Install Packages: `TUnit`, `NSubstitute` (or `Moq`), `Bogus`
- [ ] **Infrastructure Setup**
  - Create `DataBuilder` for generating entities/DTOs
- [ ] **Test Implementation**
  - Create `CreateEventCommandHandlerTests.cs` (Example Command)
  - Create `GetEventDetailQueryHandlerTests.cs` (Example Query)
  - Create `EventValidatorTests.cs` (Example Validator)
  - Verify mocking of `IEventRepository`

## Phase 2: Persistence Integration Testing (Event.Persistence.IntegrationTests)
- [ ] **Scaffold Project**
  - Create Console App: `Event.Persistence.IntegrationTests`
  - Add References: `Explore.Persistence`, `Explore.Domain`
  - Install Packages: `TUnit`, `Testcontainers.PostgreSql`
- [ ] **Infrastructure Setup**
  - Implement `PostgreSqlContainerFixture` (Singleton for Assembly/Class)
  - Configure `DbContext` to use container connection string
- [ ] **Test Implementation**
  - Create `EventRepositoryTests.cs`
  - Verify CRUD operations
  - Verify Transaction/Rollback behavior

## Phase 3: API Integration Testing (Event.Api.IntegrationTests)
- [ ] **Scaffold Project**
  - Create Console App: `Event.Api.IntegrationTests`
  - Add References: `Explore.API`
  - Install Packages: `TUnit`, `Microsoft.AspNetCore.Mvc.Testing`
- [ ] **Infrastructure Setup**
  - Create `CustomWebApplicationFactory`
  - Implement TUnit `IClassFixture` / `[ClassDataSource]` for Factory
- [ ] **Test Implementation**
  - Create `EventsControllerTests.cs`
  - Verify `GET /api/events` (200 OK)
  - Verify `POST /api/events` (Auth/Validation)

## Phase 4: Architecture Testing (Event.Architecture.Tests)
- [ ] **Scaffold Project**
  - Create Console App: `Event.Architecture.Tests`
  - Add References: All Solution Projects
  - Install Packages: `TUnit`, `NetArchTest.eShop` / `NetArchTest.Rules`
- [ ] **Rule Implementation**
  - `Domain_Should_Not_Have_Dependency_On_Application`
  - `Application_Should_Not_Have_Dependency_On_Infrastructure`
  - `Controllers_Should_Inherit_From_BaseController`
  - `Handlers_Should_Be_Sealed` (Optional)

## Phase 5: Verification
- [ ] Run all tests: `dotnet test`
- [ ] Check Test Explorer integration
- [ ] Review Execution Time
