# TUnit Testing Implementation Plan

## Executive Summary
This plan details the strategy for implementing "Enterprise Grade" testing for the ISLAMU Event platform using the TUnit framework. The goal is to establish a robust testing pyramid comprising Unit, Integration (API & Persistence), and Architecture tests, replacing any existing ad-hoc testing approaches. We will leverage TUnit's modern features like async lifecycle hooks, parallel execution, and built-in fluent assertions.

## Current State Analysis
- **Framework**: TUnit (latest) is the chosen exclusive testing framework.
- **Project Structure**: Clean Architecture with MediatR.
- **Existing Tests**: Minimal or placeholder testing infrastructure.
- **Goal**: 4 new test projects:
  1. `Event.Application.UnitTests`
  2. `Event.Api.IntegrationTests`
  3. `Event.Persistence.IntegrationTests`
  4. `Event.Architecture.Tests`

## Proposed Future State
- **Console App Projects**: All test projects will be Console Applications (`<OutputType>Exe</OutputType>`).
- **Standardized Assertions**: Fluent assertions via `await Assert.That(...)`.
- **Lifecycle Management**: TUnit hooks (`[Before(Test)]`, `[Before(Assembly)]`) for setup/teardown.
- **Isolation**:
  - Unit Tests: Moq/NSubstitute for dependencies.
  - API Tests: `WebApplicationFactory` + In-Memory/Testcontainers.
  - Persistence Tests: Testcontainers (PostgreSQL).
- **Architecture Enforcement**: NetArchTest rules to ensure layer boundaries.

## Implementation Phases

### Phase 1: Foundation & Unit Testing (Day 1)
Establish the testing infrastructure and core unit tests for business logic.
- **Projects**: `Event.Application.UnitTests`
- **Key Actions**:
  - Create Console App project.
  - Install TUnit & Mocking libs (NSubstitute).
  - Implement reusable `DataBuilder` for entities.
  - Test Handlers (Commands/Queries) and Validators.

### Phase 2: Persistence Integration Testing (Day 1-2)
Verify database interactions using real containerized databases.
- **Projects**: `Event.Persistence.IntegrationTests`
- **Key Actions**:
  - Create Console App project.
  - Setup Testcontainers for PostgreSQL.
  - Implement TUnit Lifecycle hooks for container management (Singleton pattern).
  - Test Repositories and EF Core mappings.

### Phase 3: API Integration Testing (Day 2)
Black-box testing of the API endpoints.
- **Projects**: `Event.Api.IntegrationTests`
- **Key Actions**:
  - Create Console App project.
  - Configure `WebApplicationFactory`.
  - Create TUnit `IClassFixture` or `[ClassDataSource]` for Factory injection.
  - Test Controllers (Status Codes, JSON Bodies).

### Phase 4: Architecture Testing (Day 3)
Ensure code maintainability and adherence to Clean Architecture.
- **Projects**: `Event.Architecture.Tests`
- **Key Actions**:
  - Create Console App project.
  - Install `NetArchTest.eShop` / `NetArchTest.Rules`.
  - Define rules for Layer Dependencies (Domain -> Application -> Infrastructure).
  - Define rules for Naming Conventions (Controllers, Handlers).

## Detailed Tasks

See `tunit-testing-implementation-tasks.md` for the granular checklist.

## Risk Assessment
- **TUnit Maturity**: As a newer framework, some edge cases might lack documentation.
  - *Mitigation*: Fallback to standard patterns if TUnit specifics are unclear; use active community resources.
- **Testcontainer Performance**: Cold starts can be slow.
  - *Mitigation*: Use Singleton container instances per assembly/run, not per test.
- **Parallelism Conflicts**: Database tests running in parallel might conflict.
  - *Mitigation*: Use unique schemas or transactional isolation (rollback after test).

## Success Metrics
- 100% Pass rate for all new test suites.
- < 5s Execution time for Unit Test suite.
- Clear separation of concerns (Unit vs Integ vs Arch).
- CI/CD integration readiness (Tests run via `dotnet test`).

## Required Resources
- TUnit NuGet packages.
- Testcontainers NuGet packages.
- NetArchTest NuGet packages.
- Docker Desktop (for Testcontainers).
