# Code Conventions & Governance

> **Standards and Conventions for .NET Clean Architecture Projects**
>
> This document defines naming conventions, organizational patterns, and architectural decisions.
> For code examples and implementation patterns, see the relevant skills in `.claude/skills/`.

**Last Updated**: January 2026

---

## Table of Contents

1. [Critical Rules Summary](#critical-rules-summary)
2. [Naming Conventions](#naming-conventions)
3. [File Organization](#file-organization)
4. [Layer Responsibilities](#layer-responsibilities)
5. [Design Principles](#design-principles)
6. [Pattern Selection Guide](#pattern-selection-guide)
7. [Decision Framework](#decision-framework)

---

## Critical Rules Summary

These rules are **non-negotiable**. Violations break architectural integrity.

| # | Rule | Rationale |
|---|------|-----------|
| 1 | Repositories return **entities**, never DTOs | Single responsibility; mapping belongs in handlers |
| 2 | Validators use **manual instantiation**, not DI | Fine-grained control; consistent pattern |
| 3 | Navigation properties are **readonly for writes** | Tenant isolation; explicit repository operations |
| 4 | Use `int` for IDs (except where `Guid`/`long` needed) | Consistency; sufficient for most scenarios |
| 5 | No default values in entity properties | Explicit initialization; clear value origins |
| 6 | Keep all using statements | Build system dependencies; prevents issues |
| 7 | Commands return `BaseCommandResponse<T>` | Consistent error handling; structured responses |
| 8 | GET = AllowAnonymous, Write = Authorize | Public discovery; protected writes |
| 9 | UserId extraction uses fallback pattern | Provider compatibility; claim name variations |
| 10 | Use file-scoped namespaces | C# 10+ convention; cleaner code |

**For detailed examples**: See `QUICK_REFERENCE.md` and relevant skills.

---

## Naming Conventions

### General Rules

| Element | Convention | Example |
|---------|------------|---------|
| Public members | PascalCase | `Title`, `CreatedAt` |
| Private fields | _camelCase with underscore | `_eventRepository` |
| Parameters | camelCase | `eventId`, `createDto` |
| Constants | PascalCase | `DefaultPageSize` |
| Interfaces | IPascalCase | `IEventRepository` |
| Async methods | Suffix with Async | `GetEventsAsync()` |

### Entity Naming

| Type | Pattern | Example |
|------|---------|---------|
| Core entity | Singular noun | `Event`, `Organization` |
| Lookup/enum entity | Singular noun | `EventType`, `ApprovalStatus` |
| Join table | Combined names | `EventCategories`, `EventTags` |
| Aspect table | Parent + Details | `EventIslamicDetails` |

### DTO Naming

| Type | Pattern | Purpose |
|------|---------|---------|
| Full details | `{Entity}Dto` | Read operations with all fields |
| List view | `{Entity}ListDto` | Minimal fields for lists |
| Create payload | `Create{Entity}Dto` | Write operations (no ID) |
| Update payload | `Update{Entity}Dto` | Write operations (ID required) |

### Handler Naming

| Type | Pattern | Example |
|------|---------|---------|
| Command | `Create{Entity}Command` | `CreateEventCommand` |
| Command Handler | `Create{Entity}CommandHandler` | `CreateEventCommandHandler` |
| Query | `Get{Entity}ListRequest` | `GetEventListRequest` |
| Query Handler | `Get{Entity}ListRequestHandler` | `GetEventListRequestHandler` |

### Repository Naming

| Type | Pattern | Example |
|------|---------|---------|
| Interface | `I{Entity}Repository` | `IEventRepository` |
| Implementation | `{Entity}Repository` | `EventRepository` |
| Generic base | `IGenericRepository<T, TId>` | Base interface |

---

## File Organization

### Solution Structure

```
{Project}.sln
├── src/
│   ├── {Project}.Domain/           # Entities, Enums, Interfaces
│   ├── {Project}.Application/      # CQRS, DTOs, Validators
│   ├── {Project}.Persistence/      # DbContext, Repositories
│   ├── {Project}.Infrastructure/   # External services
│   ├── {Project}.API/              # REST endpoints
│   ├── {Project}.Blazor/           # BFF (Blazor Server)
│   └── {Project}.Blazor.Client/    # UI (Blazor WASM)
├── tests/
│   ├── {Project}.Domain.UnitTests/
│   ├── {Project}.Application.UnitTests/
│   ├── {Project}.API.IntegrationTests/
│   └── {Project}.Persistence.IntegrationTests/
└── docs/
```

### Application Layer Organization

```
{Project}.Application/
├── Features/{Entity}/
│   ├── Requests/
│   │   ├── Commands/           # Create, Update, Delete
│   │   └── Queries/            # Get, List, Search
│   └── Handlers/
│       ├── Commands/           # Command handlers
│       └── Queries/            # Query handlers
├── DTOs/{Entity}/
│   ├── {Entity}Dto.cs
│   ├── {Entity}ListDto.cs
│   ├── Create{Entity}Dto.cs
│   ├── Update{Entity}Dto.cs
│   └── Validators/
├── Contracts/
│   ├── Persistence/            # Repository interfaces
│   ├── Infrastructure/         # Service interfaces
│   └── Identity/               # User context interfaces
├── Responses/                  # BaseCommandResponse, etc.
├── Exceptions/                 # Custom exceptions
└── Profiles/                   # AutoMapper profiles
```

### Domain Layer Organization

```
{Project}.Domain/
├── Entities/                   # Core business entities
├── Enums/                      # Enum definitions
├── Interfaces/                 # Domain interfaces
│   ├── ITenantEntity.cs
│   ├── IAuditableEntity.cs
│   └── ISoftDeletable.cs
└── ValueObjects/               # Value objects (if any)
```

### Persistence Layer Organization

```
{Project}.Persistence/
├── Configurations/
│   └── Entities/               # EF Core configurations
├── Repositories/               # Repository implementations
├── Migrations/                 # Database migrations
└── {DbContext}.cs              # Main DbContext
```

---

## Layer Responsibilities

### What Goes Where

| Concern | Layer | Location |
|---------|-------|----------|
| Entity definition | Domain | `{Project}.Domain/` |
| Business rules | Domain | Entity methods, value objects |
| Repository interface | Application | `Contracts/Persistence/` |
| Command/Query | Application | `Features/{Entity}/Requests/` |
| Handler logic | Application | `Features/{Entity}/Handlers/` |
| DTO definition | Application | `DTOs/{Entity}/` |
| Validation rules | Application | `DTOs/{Entity}/Validators/` |
| Repository implementation | Persistence | `Repositories/` |
| EF configuration | Persistence | `Configurations/Entities/` |
| External service impl | Infrastructure | Service classes |
| API endpoint | Presentation | `Controllers/` |
| UI component | Presentation | `Components/` |

### Dependency Rules

| Layer | Can Reference | Cannot Reference |
|-------|--------------|------------------|
| Domain | Nothing | Application, Infrastructure, Presentation |
| Application | Domain | Infrastructure, Presentation |
| Infrastructure | Domain, Application | Presentation |
| Presentation | All | (Entry point) |

---

## Design Principles

### SOLID Application

| Principle | How We Apply It |
|-----------|-----------------|
| **S**ingle Responsibility | One handler per command/query; one repository per entity |
| **O**pen/Closed | Extend via new handlers, not modifying existing |
| **L**iskov Substitution | All repository implementations honor interface contracts |
| **I**nterface Segregation | Small, focused interfaces per entity type |
| **D**ependency Inversion | Application defines interfaces; Infrastructure implements |

### Additional Principles

| Principle | Application |
|-----------|-------------|
| **DRY** | Generic repository base; shared validation patterns |
| **KISS** | Simple handlers; one responsibility per class |
| **YAGNI** | Don't add abstractions until needed |
| **Explicit > Implicit** | Clear initialization; obvious dependencies |
| **Fail Fast** | Validate early; return errors immediately |

---

## Pattern Selection Guide

### When to Use Each Pattern

| Scenario | Pattern | Reasoning |
|----------|---------|-----------|
| Simple CRUD | CQRS + Generic Repository | Standard approach |
| Complex query | Custom repository method | Optimize for specific needs |
| Cross-entity operation | Domain service | Coordinate multiple entities |
| External integration | Infrastructure service | Isolate external dependencies |
| Validation | FluentValidation | Consistent, testable rules |
| Mapping | AutoMapper | Convention-based, less boilerplate |

### When NOT to Use Patterns

| Anti-Pattern | Why to Avoid |
|--------------|--------------|
| Repository for simple lookups | Over-engineering |
| Domain service for single-entity ops | Should be in handler |
| Generic repository for complex queries | Loses optimization |
| MediatR behaviors for one-off logic | Use handler directly |

---

## Decision Framework

### Choosing ID Types

| Type | Use When | Examples |
|------|----------|----------|
| `Guid` | Main entities, distributed systems | Event, Organization, User |
| `int` | Lookup tables, enums, sequential IDs | EventType, ApprovalStatus |
| `long` | Large sequences, file sizes, cursors | Pagination, storage metrics |

### Choosing Validation Location

| Location | Use When |
|----------|----------|
| DTO Validator | Input validation, format checks |
| Handler | Business rules requiring data access |
| Entity | Invariants that must always hold |
| Controller | Quick format/auth checks |

### Choosing Query Strategy

| Strategy | Use When |
|----------|----------|
| Repository method | Standard queries, reusable |
| Handler-specific query | One-off, optimized for use case |
| Specification pattern | Complex, composable filters |

---

## Code Quality Standards

### Required in Every File

- File-scoped namespace
- All necessary using statements preserved
- Async suffix on async methods
- CancellationToken passed through

### Required in Every Entity

- Primary key property
- TenantId (for tenant-scoped entities)
- Audit fields (CreatedAt, UpdatedAt)
- Soft delete support (IsDeleted)

### Required in Every Controller

- Route attribute with version
- OpenAPI attributes on actions
- Authorization attributes
- Proper HTTP status codes

---

## Related Documentation

- **[QUICK_REFERENCE.md](QUICK_REFERENCE.md)** - Critical rules with examples
- **[ARCHITECTURE.md](ARCHITECTURE.md)** - System architecture overview
- **[TEMPLATE_GLOSSARY.md](TEMPLATE_GLOSSARY.md)** - Placeholder definitions

## Implementation Reference

For detailed code patterns and examples, see:

| Skill | Content |
|-------|---------|
| `clean-architecture-rules` | Layer boundaries, dependency rules |
| `cqrs-mediatr-guidelines` | Commands, queries, handlers |
| `dotnet-efcore-guidelines` | DbContext, repositories, queries |
| `blazor-ui-conventions` | Component patterns, state management |
| `auth-patterns` | User ID extraction, authorization |
