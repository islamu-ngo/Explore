ABOUTME: Project governance rules that normalize architecture, naming, and contribution standards for this repository.
ABOUTME: Replaces generic template guidance with repo-specific constraints and links to the authoritative docs.

# Code Conventions & Governance

> **Standards and Conventions for .NET Clean Architecture Projects**
>
> This document defines naming conventions, organizational patterns, and architectural decisions.
> For code examples and implementation patterns, see the relevant skills in `.agents/skills/`.

**Last Updated**: 2026-04-19

---

## Table of Contents

1. [Critical Rules Summary](#critical-rules-summary)
2. [Naming Conventions](#naming-conventions)
3. [File Organization](#file-organization)
4. [Layer Responsibilities](#layer-responsibilities)
5. [Design Principles](#design-principles)
6. [Pattern Selection Guide](#pattern-selection-guide)
7. [Decision Framework](#decision-framework)
8. [API Contract Rules](#api-contract-rules)
9. [CI/CD Governance](#cicd-governance)

---

## Critical Rules Summary

These rules are **non-negotiable**. Violations break architectural integrity.

| # | Rule | Rationale |
|---|------|-----------|
| 1 | Repositories return **entities**, never DTOs | Single responsibility; mapping belongs in handlers |
| 2 | Validators use **manual instantiation**, not DI | Fine-grained control; consistent pattern |
| 3 | Navigation properties are **readonly for writes** | Tenant isolation; explicit repository operations |
| 4 | Use `Guid` for core aggregates, `int` for most lookups, `long` only for large size/cursor fields | Matches current domain and persistence conventions |
| 5 | No default values in entity properties | Explicit initialization; clear value origins |
| 6 | Do not delete seemingly unused `using` statements blindly | Verify with build/tests before cleanup |
| 7 | Create/update commands typically return `BaseCommandResponse<Guid>` | Matches current CQRS response contracts |
| 8 | GET = AllowAnonymous, Write = Authorize | Public discovery; protected writes |
| 9 | UserId extraction fallback is `sub` -> `nameidentifier` -> `sid` | Provider compatibility; claim name variations |
| 10 | Use file-scoped namespaces | C# 10+ convention; cleaner code |

**For detailed examples**: See `QUICK_REFERENCE.md` and relevant skills.

---

## Naming Conventions

### General Rules

| Element | Convention | Example |
|---------|------------|---------|
| Public members | PascalCase | `Title`, `CreatedAt` |
| Private fields | _camelCase with underscore | `_eventRepository`, `_resourceAssembler`, `_mediator`, `_userContext`, `_cache` |
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
Event.sln
├── Event.Domain/
├── Event.Application/
├── Explore.Persistence/
├── Explore.Infrastructure/
├── Explore.API/
├── Explore.Blazor/
├── Explore.Blazor.Client/
├── Explore.AppHost/
├── Event.Domain.UnitTests/
├── Event.Application.UnitTests/
├── Event.Architecture.Tests/
├── Explore.Secrets.UnitTests/
├── Event.Persistence.IntegrationTests/
├── Event.API.IntegrationTests/
├── Explore.Blazor.IntegrationTests/
├── Explore.Blazor.Client.Tests/
├── docs/
└── dev/
```

See [CODEBASE_STRUCTURE.md](CODEBASE_STRUCTURE.md) for the full directory map and notable subfolders.

### Application Layer Organization

```
Event.Application/
├── Features/{Feature}/
│   ├── Requests/
│   │   ├── Commands/           # Create, Update, Delete
│   │   └── Queries/            # Get, List, Search
│   └── Handlers/
│       ├── Commands/           # Command handlers
│       └── Queries/            # Query handlers
├── Behaviors/
├── Authorization/
├── Contracts/
├── DTOs/
├── Responses/
├── Telemetry/
└── Profiles/
```

### Domain Layer Organization

```
Event.Domain/
├── Entities/                   # Core business entities
├── Enums/                      # Enum definitions
├── Interfaces/                 # Domain interfaces
│   ├── ITenantEntity.cs
│   ├── IAuditableEntity.cs
│   └── ISoftDeletable.cs
├── Specifications/
└── ValueObjects/
```

### Persistence Layer Organization

```
Explore.Persistence/
├── Configurations/
├── QueryFilters/
├── Repositories/
├── Services/
└── ExploreDbContext.cs
```

---

## Layer Responsibilities

### What Goes Where

| Concern | Layer | Location |
|---------|-------|----------|
| Entity definition | Domain | `Event.Domain/` |
| Business rules | Domain | Entity methods, value objects |
| Repository interface | Application | `Contracts/Persistence/` |
| Command/Query | Application | `Features/{Feature}/Requests/` |
| Handler logic | Application | `Features/{Feature}/Handlers/` |
| DTO definition | Application | `DTOs/` and feature folders |
| Validation rules | Application | Feature validators or DTO validator folders |
| Repository implementation | Persistence | `Repositories/` |
| EF configuration | Persistence | `Configurations/` |
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

## API Contract Rules

> **Scope**: Every controller action in `Explore.API` is part of a **governed product artifact** — the OpenAPI document at `/openapi/event-api.json`. Both server consumers (integrations, webhooks) and the generated `IEventApiClient` (used by `Explore.Blazor` and `Explore.Blazor.Client`) depend on this contract being stable, unambiguous, and ergonomic.
>
> These rules are enforced by tests in `Event.API.IntegrationTests/Features/ContractInvariantsTests.cs` and `Explore.Blazor.Client.Tests/ApiClientNamingTests.cs`. Violations fail CI.

### Versioning Strategy (Multi-Reader, Non-URL)

Version is negotiated via **three equal-status readers**, never via URL segment:

| Reader | Syntax | Primary consumer |
|---|---|---|
| Media type | `Accept: application/json;v=0.1` | REST-pure clients, HATEOAS navigation |
| Query string | `?api-version=0.1` | Webhooks, ad-hoc tools, browser debugging |
| Custom header | `X-Api-Version: 0.1` | Service-to-service integrations that cannot set media type |

The URL-segment pattern (`/api/v0.1/...`) is **banned**. It is the root cause of duplicate OpenAPI operations, it clutters HATEOAS links, and it prevents content negotiation from being the source of truth. Controllers carry exactly one `[Route("api/[controller]")]` attribute. Runtime requests to `/api/v{n}/...` must return 404.

### Endpoint Classification

Every controller action must carry exactly one of the following classifications, enforced by an architecture test:

| Class | Semantics | Authorization | Rate limit baseline |
|---|---|---|---|
| **Public** | Safe for unauthenticated read. No tenant mutation. | `[AllowAnonymous]` | `global` |
| **Authenticated** | Any logged-in user. Tenant-scoped or user-scoped write, or privileged read. | `[Authorize]` (no roles required) | `authenticated` or `write` |
| **Admin** | Operator / setup / diagnostics. Not exposed to the generated client. | `[Authorize(Roles=...)]` or `[SetupSecretRequired]` | `setup_secret` or `authenticated` |
| **PublicTransactional** | Anonymous tenant mutation for narrowly scoped guest flows. Unsafe verbs only. | `[AllowAnonymous]`; browser traffic is protected at the BFF boundary, not with API antiforgery metadata. | `public_transactional` |

The classification lives in controller action metadata via the `[EndpointClassification(EndpointClass.X)]` attribute (`Explore.API.Attributes`) and is the single source of truth for OpenAPI tagging (injected as `x-endpoint-class` operation extension by `EndpointClassificationTransformer`), client-generation filters, and Cerbos policy scaffolding. Every controller action must carry exactly one classification (class-level attribute is inherited by actions; action-level attribute overrides). Enforced by `EndpointClassificationArchitectureTests` in `Event.Architecture.Tests`.

`PublicTransactional` endpoints must explicitly use `[EnableRateLimiting("public_transactional")]`. The policy is a fixed window of 10 requests per 60 seconds per effective remote IP, with queue limit 0. `POST` actions also require `[RequireIdempotencyKey]`; the API receives no antiforgery metadata because only browser traffic that crosses the BFF is subject to BFF antiforgery validation.

### Operation IDs

- **Every action has an `operationId`.** `operationId` is the filename of the generated client method (plus `Async` suffix). Missing IDs cause NSwag to emit placeholder names (`GET`, `GET2`, `TenantDELETE2`) that break ergonomics and block the HATEOAS link-name alignment.
- **Operation IDs are unique across the whole document.** Uniqueness is a tested invariant, not an emergent behavior of good naming.
- **Naming pattern:** `{ControllerShortName}_{ActionName}` (PascalCase, underscore-separated). Example: `Tenant_GetById`, `EventRegistration_ListByEvent`. This is policy, not physics — see [NAMING_CONVENTIONS.md](NAMING_CONVENTIONS.md) for rationale and exceptions.
- **Route Name ↔ Operation ID alignment is intentional.** `[HttpGet(Name = "Tenant_GetById")]` and its derived `operationId` should match. They are kept aligned by convention, not by runtime equality — both are allowed to drift independently if a specific action needs it, but drift must be documented on the action.

### Banned Names

The following method names on `IEventApiClient` are always defects. CI fails if any appear:

- Any method whose name matches the regex `\d+Async$` (e.g. `Foo2Async`, `TenantGET3Async`).
- Any method whose name equals one of `GETAsync`, `POSTAsync`, `PUTAsync`, `DELETEAsync`, `PATCHAsync` — with or without a digit suffix.
- Any operationId equal to a raw HTTP verb (`GET`, `POST`, etc.) or a raw verb followed by digits.

These are NSwag's collision-disambiguation fallbacks and carry zero semantic information.

### Client-Ergonomics Bar

Generated client methods must be **readable without the OpenAPI doc**:

- **Collections vs single**: `ListEventsAsync` not `GetEventsAsync`; `GetEventAsync(id)` not `GetEventsAsync(id)`.
- **Mutations are business actions**: `PublishEventAsync`, `ApproveRegistrationAsync` — not `PutEvent2Async`.
- **No verb-only names**: A method called `GetAsync` on a 100+ entity client is a contract defect regardless of whether the compiler accepts it.

The ergonomics bar is a governed quality gate. Violations block release, though they do not block routine dev builds (they surface as schema-diff warnings in CI and fail only the explicit `ApiClientNamingTests`).

### NSwag Trailing-Parameter Convention

Every method generated by NSwag (`SingleClientFromOperationId` mode) ends with three optional parameters:

```csharp
string? api_version = null, string? x_Api_Version = null, CancellationToken cancellationToken = default
```

Callers passing `CancellationToken` must use a **named argument** (`cancellationToken: ct`) to avoid routing the token into the `api_version` string parameter. NSubstitute mock setups must insert `Arg.Any<string?>(), Arg.Any<string?>()` before `Arg.Any<CancellationToken>()`. This convention is stable across regenerations and must not be hand-modified in `EventApiClient.g.cs`.

**Stabilization plan**: `dev/active/api-contract-stabilization/api-contract-stabilization-plan.md`.

### Contract Ownership & Change Control

- **OpenAPI changes require PR review.** The checked-in `schemas/openapi.json` is an artifact of CI regeneration — it must never be hand-edited. Any PR touching controller signatures, route attributes, `[ApiVersion]`, or `[ProducesResponseType]` is implicitly a contract change.
- **Schema-diff is surfaced in CI.** Before 1.0 it is non-blocking (visibility only). At 1.0 it flips to blocking for breaking diffs.
- **Regeneration is a discrete step.** Do not regenerate `Explore.Blazor.Client/Clients/EventApiClient.g.cs` casually. Regenerate only when the API-side contract is stable and a tracked change-set justifies it.

### Authoring Checklist (new controller action)

1. Add `[HttpGet(Name = "X_Y")]` (or equivalent verb attribute) with a stable name.
2. Add `[ProducesResponseType]` for every possible response shape, including 400/401/403/404.
3. Pick an **Endpoint Classification** and apply the matching authorization attribute(s).
4. Confirm API contract tests still pass (`dotnet test --project Event.API.IntegrationTests/Event.API.IntegrationTests.csproj --configuration Release --verbosity quiet`).
5. If the generated client needs regeneration, open a separate contract PR — do not mix contract and feature work.

## CI/CD Governance

GitHub Actions gates, branch-protection policy, environment settings, artifact retention, fork PR rules, and generated-artifact review rules live in [CI_CD_GOVERNANCE.md](CI_CD_GOVERNANCE.md).

Key rules:

- Keep required workflow/job names stable unless branch protection is migrated in the same change.
- Required checks must be always-present or have a documented no-op path; do not make a path-skipped workflow required without a wrapper gate.
- OpenAPI and NSwag client drift is governed CI evidence. Contributors regenerate through the documented command path and commit the generated artifacts.
- Production deployment approval, branch restrictions, environment secrets, secret scanning, and push protection are GitHub repository or organization settings, not application configuration.

---

## Related Documentation

- **[QUICK_REFERENCE.md](QUICK_REFERENCE.md)** - Critical rules with examples
- **[ARCHITECTURE.md](ARCHITECTURE.md)** - System architecture overview
- **[NAMING_CONVENTIONS.md](NAMING_CONVENTIONS.md)** - OperationId naming policy and exceptions
- **[TEMPLATE_GLOSSARY.md](TEMPLATE_GLOSSARY.md)** - Placeholder definitions

## Code Review Checklist

### Command Handler PR Review

Before approving any command handler PR, verify:

- **Multi-step writes → `IUnitOfWork`**: If the handler performs more than one write operation (across any repository or service), confirm `IUnitOfWork.ExecuteInTransactionAsync` wraps all writes.
- **Pre-validation outside lambda**: Validation, authorization checks, and read-only pre-fetches must be outside the transaction lambda.
- **Post-commit side effects after lambda**: Cache invalidation, metrics, external notifications must happen *after* `ExecuteInTransactionAsync` returns — never inside the lambda.
- **Retry-safety**: All `Guid.NewGuid()` / timestamps used as IDs must be generated before the lambda (captured via closure). No HTTP calls, broker publishes, or emails inside the lambda.
- **No nested transactions**: Handlers must not call services that internally use `IUnitOfWork` while already inside a transaction.

See `dev/active/unitofwork-pattern/unitofwork-pattern-context.md` for the canonical reference implementation.

---

## Implementation Reference

For detailed code patterns and examples, see:

| Skill | Content |
|-------|---------|
| `clean-architecture-rules` | Layer boundaries, dependency rules |
| `cqrs-mediatr-guidelines` | Commands, queries, handlers |
| `dotnet-efcore-guidelines` | DbContext, repositories, queries |
| `blazor-ui-conventions` | Component patterns, state management |
| `auth-patterns` | User ID extraction, authorization |

---

## AI Contribution Routing

Every change — human or agent — routes through the Contribution Contract before editing. The contract answers **eight** deterministic questions (intent, rules, must-read files, may-change paths, must-run tests, docs-to-update, PR checklist, forbidden-without-approval). See [`AGENTS.md`](../AGENTS.md) §1 and [`.claude/contract/README.md`](../.claude/contract/README.md).

### Intent Classification (Decision Table)

| Signal You Observe | Primary Intent | Must-Read Starts With |
|---|---|---|
| Adding read endpoint | `add-get-endpoint` | `docs/API.md`, `.claude/rules/api-controllers.md` |
| Adding/modifying mutation endpoint | `add-write-endpoint` | `docs/API.md`, `.claude/rules/api-controllers.md`, `auth-patterns` |
| Adding HAL affordance / link-based button | `add-hal-link` | `.claude/rules/api-hateoas.md`, `auth-patterns` |
| New MediatR command/query | `add-cqrs-handler` | `cqrs-mediatr-guidelines`, `.claude/rules/application-layer.md` |
| New EF Core migration | `add-ef-migration` | `dotnet-efcore-guidelines`, `.claude/rules/efcore-migrations.md` |
| Repository query change | `update-repository-query` | `dotnet-efcore-guidelines`, `.claude/rules/efcore-persistence.md` |
| Blazor component affordance gated by HAL links | `blazor-component-affordance` | `blazor-ui-conventions`, `blazor-bff-patterns` |
| 401/403 BFF/API auth issue | `bff-auth-bug` | `auth-patterns`, `blazor-bff-patterns`, `docs/SECURITY-MODEL.md` |
| Cerbos policy change | `cerbos-policy-change` | `docs/AUTHORIZATION_PATTERNS.md`, `auth-patterns` |
| OpenAPI contract / breaking change | `openapi-contract-change` | `docs/API.md`, `docs/API_CHANGELOG.md` |
| Setup-time external provider or infrastructure bootstrap | `external-infrastructure-bootstrap` | `docs/SECURITY-MODEL.md`, `docs/SECRETS.md`, `auth-patterns` |

If no intent matches, stop and propose a new one per `.claude/contract/README.md`. Do not improvise.

### Path → Rule File (Auto-Loaded)

| File You Edit | Rule File |
|---|---|
| `Explore.API/Controllers/**/*.cs` | [`.claude/rules/api-controllers.md`](../.claude/rules/api-controllers.md) |
| `Explore.API/Hateoas/**/*.cs` | [`.claude/rules/api-hateoas.md`](../.claude/rules/api-hateoas.md) |
| `Explore.Application/**/*.cs` | [`.claude/rules/application-layer.md`](../.claude/rules/application-layer.md) |
| `Explore.Domain/**/*.cs` | [`.claude/rules/domain.md`](../.claude/rules/domain.md) |
| `Explore.Persistence/**/*.cs` (non-migration) | [`.claude/rules/efcore-persistence.md`](../.claude/rules/efcore-persistence.md) |
| `Explore.Persistence/Migrations/**/*.cs` | [`.claude/rules/efcore-migrations.md`](../.claude/rules/efcore-migrations.md) |
| `Explore.Blazor/**/*` (BFF) | [`.claude/rules/blazor-server.md`](../.claude/rules/blazor-server.md) |
| `Explore.Blazor.Client/**/*` (WASM) | [`.claude/rules/blazor-client.md`](../.claude/rules/blazor-client.md) |
| `**/*Tests/**/*.cs`, `**/*UnitTests/**/*.cs`, `**/*IntegrationTests/**/*.cs` | [`.claude/rules/tests.md`](../.claude/rules/tests.md) |

### Context Maintenance

- Benchmark scenarios live in `.claude/benchmarks/cold-start-tasks.yaml` to measure cold-start agent success.

If a rule in `.claude/rules/` appears to conflict with `QUICK_REFERENCE.md` or this file, the canonical doc wins and the rule file must be fixed per [`AGENTS.md`](../AGENTS.md) §4.
