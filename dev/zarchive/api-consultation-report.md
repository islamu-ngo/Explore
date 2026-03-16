ABOUTME: Architecture consultation report for the API-centric clean architecture layers.
ABOUTME: Captures technical debt, improvement areas, and a pragmatic enterprise-grade hardening path.

# API Consultation Report

## Executive Assessment

The API has a strong architectural base already. Clean Architecture boundaries are explicitly enforced in `Event.Architecture.Tests/CleanArchitectureTests.cs:27`, the API uses a documented middleware and CQRS flow in `Explore.API/Program.cs:618`, and central tenancy plus authorization abstractions are real implementation choices rather than slideware in `Explore.Persistence/ExploreDbContext.cs:48` and `Explore.Infrastructure/Services/RuntimeAuthorizationProvider.cs:26`.

This is not a rewrite case.

The biggest gap between the current state and an enterprise-grade API is runtime hardening:

1. cache correctness and invalidation,
2. authorization consistency,
3. protection policies that exist but are not fully applied,
4. startup and operational ownership,
5. response and validation consistency.

## What Is Already Strong

### 1. Clean Architecture foundations are real

- Layer dependency rules are enforced by architecture tests in `Event.Architecture.Tests/CleanArchitectureTests.cs:97`.
- Application stays separate from API and Persistence concerns in structure and project references, matching `docs/ARCHITECTURE.md:14`.
- Repositories return entities and handlers map to DTOs, which aligns with project rules and the current repository/handler split, for example `Explore.Persistence/Repositories/GenericRepository.cs:20` and `Explore.API/Controllers/EventController.cs:109`.

### 2. Multi-tenancy is centrally enforced

- Tenant isolation is enforced by named global query filters in `Explore.Persistence/ExploreDbContext.cs:52`.
- Tenant resolution is centralized in middleware instead of scattered through handlers and repositories in `Explore.API/Program.cs:637`.

### 3. The API already has serious platform features

- ProblemDetails-based exception handling is centralized in `Explore.API/Extensions/ExceptionHandlingExtensions.cs:10`, `Explore.API/ExceptionHandling/ValidationExceptionHandler.cs:12`, and `Explore.API/ExceptionHandling/GlobalExceptionHandler.cs:10`.
- HybridCache, output caching, ETags, rate limiting, and request timeouts are configured in `Explore.API/Program.cs:98`, `Explore.API/Program.cs:118`, `Explore.API/Program.cs:648`, and `Explore.API/Program.cs:650`.
- OpenTelemetry and health endpoints are wired via `Explore.ServiceDefaults/Extensions.cs:50` and `Explore.ServiceDefaults/Extensions.cs:114`.

### 4. Authorization architecture is advanced enough for enterprise use

- Resource-level authorization exists in the MediatR pipeline in `Explore.Application/Behaviors/AuthorizationBehavior.cs:20`.
- Runtime switching between Cerbos and local fallback exists in `Explore.Infrastructure/Services/RuntimeAuthorizationProvider.cs:74`.
- HATEOAS authorization fails closed through the evaluator pattern already documented and implemented.

## Main Improvement Areas

## 1. Caching correctness is the highest-priority issue

### Current state

- `ListData` output caching only varies by `pageNumber` and `pageSize` in `Explore.API/Program.cs:107`.
- `EventController.GetAll` exposes a large filter surface including tags, categories, dates, aspect filters, sorting, and module-conditional filters in `Explore.API/Controllers/EventController.cs:64`.
- Event cache invalidation is string-based and only removes a single list key such as `events:list:1:20` in `Explore.Application/Features/Events/Handlers/Commands/DeleteEventCommandHandler.cs:124`.
- Categories use the same fragile pattern with `categories:list:1:20` in `Explore.Application/Features/Categories/Handlers/Commands/CreateCategoryCommandHandler.cs:62`.
- A better invalidation pattern already exists only in tenant navigation via `IOutputCacheStore.EvictByTagAsync` in `Explore.API/Controllers/TenantController.cs:178`.

### Why this matters

This can return stale or incorrect responses for filtered list endpoints and gets worse under scale, shared caches, and multiple nodes. This is not a theoretical concern; it is a correctness bug class.

### Enterprise-grade target

- Use endpoint-specific output-cache policies.
- Vary list caches by the real query contract, not only paging.
- Standardize tag-based invalidation for all cacheable collections.
- Align HybridCache keys and output-cache tags around resource families.
- For multi-node deployments, assume L1 invalidation lag and keep local expirations intentionally short for write-heavy data.

### Recommended implementation direction

1. Split public list endpoints into dedicated cache policies.
2. Add `SetVaryByQuery(...)` for each endpoint based on its actual filters.
3. Introduce shared cache-tag conventions like `events`, `events:{id}`, `organizations`, `categories`.
4. Replace hard-coded single-key invalidation with tag eviction where supported and deterministic key families where not.
5. Audit every `[OutputCache]` endpoint in `Explore.API/Controllers` before enabling longer-lived caching.

## 2. Authorization is strong architecturally but inconsistent operationally

### Current state

- Authorization behavior is centralized in `Explore.Application/Behaviors/AuthorizationBehavior.cs:20`.
- Runtime provider fallback is centralized in `Explore.Infrastructure/Services/RuntimeAuthorizationProvider.cs:26`.
- But handlers still contain bespoke permission logic, for example event command handlers perform extra checks instead of relying solely on the pipeline.
- User identity extraction is inconsistent across controllers. Some follow `sub -> nameidentifier -> sid`, while others only use `ClaimTypes.NameIdentifier`, for example `Explore.API/Controllers/OrganizationMemberController.cs:42`.
- There are explicit TODOs in auth-related behavior such as `Explore.API/Controllers/UserController.cs:228`.

### Why this matters

When authorization is split between pipeline rules, controller checks, and handler-specific logic, drift becomes inevitable. Enterprise APIs need one clearly authoritative authorization model.

### Enterprise-grade target

- Pipeline-driven resource authorization as the default.
- Handlers only enforce business invariants, not transport/user-role branching.
- Shared user identity resolution helper used consistently across API endpoints.
- Explicit policy coverage for admin-only and delegated access cases.

### Recommended implementation direction

1. Audit commands and queries for `IAuthorizedRequest`, `[AuthorizeResource]`, and `ISecureRequest` coverage.
2. Move duplicate permission logic out of handlers where the pipeline can own it.
3. Standardize current-user resolution into one reusable helper/service for API controllers.
4. Close the missing admin visibility logic in `Explore.API/Controllers/UserController.cs:228`.
5. Add coverage tests for authorization edge cases, especially fallback behavior when Cerbos fails.

## 3. Validation architecture is internally inconsistent

### Current state

- `ValidationBehavior` is globally registered in `Explore.Application/ApplicationServicesRegistration.cs:22`.
- Validators are also auto-registered from assembly in `Explore.Application/ApplicationServicesRegistration.cs:25`.
- But the codebase rules say validators are manually instantiated, and some handlers still inject `IValidator<T>` directly, for example `Explore.Application/Features/StorageObjects/Handlers/Commands/UpdateStorageObjectCommandHandler.cs:16` and `Explore.Application/Features/ActorKeyStores/Handlers/Commands/UpdateActorKeyStoreCommandHandler.cs:17`.

### Why this matters

This creates unclear validation ownership. In enterprise systems, validation must be predictable and systematic.

### Enterprise-grade target

Choose one approach:

- either pipeline-driven FluentValidation with DI registration,
- or explicit manual validation in handlers.

Using both without a clear rule increases maintenance cost and makes behavior harder to reason about.

### Recommended implementation direction

The better enterprise choice is pipeline validation for request objects, with domain/business validation remaining in handlers where necessary.

If project constraints require manual instantiation, remove or scope `ValidationBehavior` accordingly so the architecture is explicit, not contradictory.

## 4. Error response contracts are not yet fully standardized

### Current state

- Central ProblemDetails handling is present and well-structured in `Explore.API/Extensions/ExceptionHandlingExtensions.cs:12`.
- But many controllers still return ad hoc anonymous payloads such as `return NotFound(new { error = ... })`, for example in `Explore.API/Controllers/NotificationController.cs:65` and `Explore.API/Controllers/ExternalApiKeyController.cs:51`.
- Some endpoints return `Problem(...)`, some anonymous objects, some command envelopes.

### Why this matters

Enterprise API consumers need consistent failure contracts, especially for SDK generation, contract testing, operational debugging, and cross-team adoption.

### Enterprise-grade target

- Centralized ProblemDetails for all non-success responses.
- Business failure envelopes only where they are part of the explicit contract.
- No anonymous `{ error = ... }` response bodies.

### Recommended implementation direction

1. Standardize all 4xx/5xx responses onto ProblemDetails.
2. Keep command response envelopes for successful mutation workflows where needed.
3. Refactor controller-local error payloads into exception or helper-driven flows.
4. Document the final error contract in OpenAPI.

## 5. Startup responsibilities are duplicated and too risky for mature deployments

### Current state

- The API host runs migrations and seeding during startup in `Explore.API/Program.cs:510`.
- A dedicated migration worker already exists in `Event.MigrationService/Worker.cs:18`.

### Why this matters

In enterprise deployments, schema ownership should be explicit. Running migrations from the API host increases startup coupling, rollback risk, and scale-out ambiguity.

### Enterprise-grade target

- One deployment path owns migrations.
- API startup should fail fast on dependency readiness, not mutate schema.
- Seeding should be environment-specific and tightly controlled.

### Recommended implementation direction

1. Move schema migration ownership to `Event.MigrationService`.
2. Remove migration execution from the API host after deployment automation is ready.
3. Restrict seeding to controlled environments or bootstrap flows.
4. Make health/readiness clearly reflect schema readiness without performing migration work.

## 6. Logging and telemetry exist, but the signal is uneven

### Current state

- OpenTelemetry and metrics are wired in `Explore.ServiceDefaults/Extensions.cs:58`.
- `BusinessMetrics` is defined in `Explore.Application/Telemetry/BusinessMetrics.cs:13`.
- Performance behavior logs only requests slower than 500ms in `Explore.Application/Behaviors/PerformanceBehavior.cs:30`.
- Request logging depends on user claims but is placed before authentication in the main pipeline in `Explore.API/Program.cs:639` and `Explore.API/Program.cs:644`.
- JWT debug logging is very verbose in `Explore.API/Program.cs:338` onward.
- `Console.WriteLine` remains in production API code and integration tests, for example `Explore.API/Program.cs:549` and `Explore.API/Controllers/UserController.cs:234`.

### Why this matters

Enterprise observability needs structured, low-noise, policy-safe telemetry. Too little signal hides incidents; too much noisy or sensitive signal damages operator experience and can expose internals.

### Enterprise-grade target

- Structured logging only.
- Metrics tied to actual decision points and business flows.
- Auth logging that is useful but not overly verbose or sensitive.
- Performance thresholds with warning and critical levels.

### Recommended implementation direction

1. Move request logging after authentication if user identity must be included, or explicitly accept anonymous-only request logging fields.
2. Replace `Console.WriteLine` with structured logs or remove it.
3. Use `BusinessMetrics.RecordAuthorizationDecision(...)` and similar counters at key decision points.
4. Add tracing/metrics around cache hits, cache misses, auth fallback, and slow queries.
5. Revisit JWT event logging in non-development environments.

## 7. Protection mechanisms are configured, but not fully applied

### Current state

- Rate-limit policies are defined in `Explore.API/Extensions/RateLimitingExtensions.cs`.
- Timeout tiers are defined in `Explore.API/Extensions/RequestTimeoutExtensions.cs`.
- Explicit usage appears limited compared to the available policy set.

### Why this matters

Enterprise platforms do not get full value from cross-cutting protections unless those protections are intentionally assigned to endpoints by workload profile.

### Enterprise-grade target

- Lookup endpoints use lookup timeout and caching policies.
- expensive list/search/export endpoints use complex timeout policies.
- mutation endpoints use stricter write rate limits.
- authenticated endpoints use authenticated quotas where appropriate.

### Recommended implementation direction

1. Audit controllers for endpoint-level rate-limit and timeout usage.
2. Assign policies by endpoint category: lookup, list, mutation, onboarding, admin.
3. Add tests for rate-limit headers and rejection semantics.
4. Publish endpoint classes and expected policy usage in docs.

## 8. Repository and query shape can be improved incrementally

### Current state

- `EventRepository` contains heavy include-based query methods and some compiled-query usage in `Explore.Persistence/Repositories/EventRepository.cs:11`.
- Complex event querying is built on a solid specification pattern, but list/detail reads still rely heavily on loaded entity graphs in `Explore.Persistence/Repositories/EventRepository.cs:164`.

### Why this matters

This is acceptable today, but under enterprise-scale read traffic, large include graphs become expensive, harder to evolve, and harder to tune than explicit projections.

### Enterprise-grade target

- Keep the current specification pattern.
- Gradually move the hottest read paths toward projection-based query models.
- Use compiled queries and `AsNoTracking()` systematically on hot paths.

### Recommended implementation direction

1. Identify the top three hot GET endpoints by traffic/latency.
2. Introduce projection-based query handlers for those endpoints first.
3. Keep aggregate-loading behavior for mutation workflows and richer detail endpoints.
4. Add SQL/query-plan verification for the hottest endpoints.

## Technical Debt Inventory

### High severity

- Output-cache vary keys are too coarse for filterable endpoints in `Explore.API/Program.cs:107`.
- Cache invalidation is inconsistent and often string-based rather than tag-driven in `Explore.Application/Features/Events/Handlers/Commands/DeleteEventCommandHandler.cs:124`.
- Authorization logic is split across pipeline and handlers.
- API host still owns migrations despite dedicated migration service presence.

### Medium severity

- Validation model is inconsistent between pipeline registration and handler-level validator injection.
- Error response shapes are inconsistent across controllers.
- Logging placement and verbosity are not yet tuned for mature operations.
- Identity extraction is inconsistent across controllers.

### Low severity but real

- TODO markers in API-facing code such as `Explore.API/Controllers/OrganizationController.cs:243` and `Explore.API/Controllers/UserController.cs:228`.
- Console debug output in tests and some API code.
- Package warnings from baseline build including deprecated and vulnerable dependencies, notably `MimeKit` vulnerability warnings and deprecated `Microsoft.Extensions.ApiDescription.Client` usage from the build output.

## Recommended Enterprise-Grade Target State

The target state should preserve the existing architecture while tightening the runtime platform around it.

### Architecture

- Keep Clean Architecture and CQRS.
- Keep repository returns as entities.
- Keep API-authoritative tenancy.
- Keep runtime-switchable authorization provider.

### Platform hardening

- Endpoint-specific caching policies and tag invalidation.
- One standardized authorization enforcement model.
- One validation strategy.
- One error response contract.
- One migration owner.
- Better policy attachment for timeouts and rate limits.

### Read path optimization

- Projection-first hot reads.
- systematic `AsNoTracking()`.
- more selective compiled queries.
- observability around DB and cache cost.

## Pragmatic Roadmap From Current State To Enterprise-Grade

## Phase 1 - Correctness and Security First

1. Fix output-cache vary rules for all filterable endpoints.
2. Standardize tag-based cache invalidation patterns.
3. Close authorization inconsistencies and missing admin checks.
4. Normalize user ID extraction across API controllers.
5. Remove ad hoc anonymous error payloads in favor of ProblemDetails.

## Phase 2 - Operational Hardening

1. Move migration ownership to `Event.MigrationService`.
2. Tune request logging placement and JWT logging verbosity.
3. Add missing business/security metrics.
4. Explicitly assign timeout and rate-limit policies per endpoint class.
5. Clean up package warnings and known vulnerable dependencies.

## Phase 3 - Performance and Maintainability

1. Convert the highest-traffic reads to projection-based handlers.
2. Expand compiled query usage where measurements justify it.
3. Add contract tests for ProblemDetails and policy behavior.
4. Expand architecture tests to cover caching/auth conventions where practical.

## Concrete Recommendations

### Immediate actions

- Fix `ListData` vary keys for event and similar list endpoints.
- Replace hard-coded list invalidation keys with tag-based conventions.
- Implement the missing delete path in `Explore.API/Controllers/OrganizationController.cs:243`.
- Implement admin-aware access logic in `Explore.API/Controllers/UserController.cs:228`.
- Remove `Console.WriteLine` from API runtime code.

### Near-term actions

- Decide and document the validation strategy.
- Standardize all controller non-success responses.
- shift migration execution out of the API host.
- add authorization and cache-behavior integration tests.

### Strategic actions

- Introduce projection-based read models for the busiest endpoints.
- Strengthen defense in depth with future PostgreSQL RLS once current tenant-filter behavior is stable.
- formalize API governance rules for endpoint annotations, caching, rate limiting, timeouts, and error contracts.

## Final Conclusion

This API is already architected like a serious platform, but it is not yet operated like a fully enterprise-grade one.

The codebase does not need a reinvention. It needs disciplined hardening of the runtime platform around the architecture it already has.

If only one theme is prioritized, it should be this:

**make runtime behavior deterministic and policy-driven across caching, authorization, error contracts, and operations.**

That is the shortest path from the current strong foundation to a reliably enterprise-grade API.

## Evidence Sources Used

- Internal architecture and docs: `docs/ARCHITECTURE.md`, `docs/API.md`, `docs/SECURITY.md`, `docs/CODEBASE_STRUCTURE.md`
- Key implementation files: `Explore.API/Program.cs`, `Explore.Application/Behaviors/AuthorizationBehavior.cs`, `Explore.Application/Behaviors/PerformanceBehavior.cs`, `Explore.Persistence/ExploreDbContext.cs`, `Explore.Persistence/Repositories/EventRepository.cs`, `Explore.Infrastructure/Services/RuntimeAuthorizationProvider.cs`
- Test and guardrail files: `Event.Architecture.Tests/CleanArchitectureTests.cs`
- External guidance: Microsoft ASP.NET Core docs, EF Core docs, MediatR documentation through Context7, and broader enterprise guidance via Tavily research
