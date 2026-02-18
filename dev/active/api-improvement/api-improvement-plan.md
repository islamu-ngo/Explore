# Plan: Enterprise-Grade API Improvement

**Last Updated**: 2026-02-18 (plan created) | Implementation started same day

---

## Executive Summary

The ISLAMU Event API (`Explore.API`) is already a strong implementation with Clean Architecture, CQRS, HATEOAS/HAL+JSON, dual-layer caching (OutputCache + HybridCache), Cerbos authorization, OpenTelemetry observability, and response compression. This plan identifies the remaining gaps to bring the API to a fully enterprise-grade, highly scalable implementation following best practices for .NET 10.

The improvements are categorized into 6 phases, ordered by impact and dependency. The plan prioritizes hardening, performance, and developer experience while respecting YAGNI and the repo's existing conventions.

---

## Current State Analysis

### Already Implemented (Strengths)

| Feature | Status | Notes |
|---------|--------|-------|
| Clean Architecture + CQRS | Complete | MediatR, AutoMapper, strict layer separation |
| Keycloak JWT + BFF | Complete | Multi-audience validation, BFF pattern |
| Cerbos Authorization | Complete | `AuthorizationBehavior`, dual-provider with circuit breaker fallback |
| HATEOAS/HAL+JSON | Complete | 17 assemblers, 17 link policies, `Prefer: return=minimal` |
| OutputCache (3 policies) | Complete | Lookup 1h, List 30s, Detail 60s |
| HybridCache (L1+L2) | Complete | Used in ~15+ handlers, stampede protection |
| Response Compression | Complete | Brotli + Gzip |
| OpenTelemetry | Complete | Traces, metrics, logs, Prometheus, OTLP |
| Health Checks | Complete | Database, secrets, shutdown-aware |
| RFC 7807 ProblemDetails | Complete | Global + validation exception handlers |
| Specification Pattern | Complete | Composable filters/sorts for events |
| Graceful Shutdown | Complete | 25s SIGTERM grace period |
| Pagination | Complete | Offset-based, max 100, HATEOAS nav links |
| Architecture Tests | Complete | NetArchTest layer validation |

### Gaps Identified

| Gap | Impact | Effort |
|-----|--------|--------|
| No API versioning (hardcoded `v1`) | High | M |
| No security headers (CSP, X-Content-Type-Options, etc.) | High | S |
| Rate limiting only on 1 endpoint | High | M |
| No ETag / conditional requests | Medium | M |
| No idempotency keys for POST/PUT | Medium | L |
| No structured request/response logging middleware | Medium | S |
| Correlation ID only outgoing, not incoming | Medium | S |
| No compiled queries on hot paths | Medium | M |
| CORS policies overly permissive | Medium | S |
| No cursor-based pagination option | Low | M |
| No request timeout per endpoint | Low | S |
| No API changelog / versioned docs | Low | S |

---

## Proposed Future State

A fully enterprise-grade API with:
- **Formal API versioning** via `Asp.Versioning` with **media type strategy** (`Accept: application/json;v=1.0`) — *changed from URL path per user request*
- **Comprehensive security headers** via middleware
- **Tiered rate limiting** (global, per-user, per-endpoint)
- **ETag-based conditional requests** for bandwidth optimization on GET endpoints
- **Idempotency keys** for safe POST/PUT retries
- **Structured request logging** with correlation ID propagation through Serilog
- **Compiled EF Core queries** on hot-path repository methods
- **Production-hardened CORS** with explicit origin allowlists
- **Cursor-based pagination** as an alternative for real-time feeds
- **Per-endpoint request timeouts** for long-running operations

---

## Phase 1: Security Hardening (Week 1)
**Priority: Critical** | **Risk: Low** | **Dependencies: None**

### Task 1.1: Add Security Headers Middleware
- **File**: `Explore.API/Middleware/SecurityHeadersMiddleware.cs`
- **Action**: Create middleware that adds `X-Content-Type-Options: nosniff`, `X-Frame-Options: DENY`, `Referrer-Policy: strict-origin-when-cross-origin`, `Permissions-Policy`, `Content-Security-Policy` (for API: `default-src 'none'`)
- **Register**: In `Program.cs` pipeline before `UseRouting()`
- **Acceptance Criteria**:
  - [ ] All responses include security headers
  - [ ] CSP configured appropriately for API (not Blazor)
  - [ ] Integration test verifying headers present
- **Effort**: S
- **Related Skill**: `error-tracking`

### Task 1.2: Harden CORS Policies
- **File**: `Explore.API/Program.cs` (CORS section)
- **Action**: Replace `AllowAnyOrigin` on `InternalAppPolicy` and `ExternalAppPolicy` with explicit origin allowlists from configuration. Keep `DevPolicy` permissive for development only.
- **Acceptance Criteria**:
  - [ ] Dev policy remains permissive
  - [ ] Internal/External policies use `WithOrigins()` from config
  - [ ] Configuration section `Cors:AllowedOrigins` added to `appsettings.json`
  - [ ] Integration test verifying CORS behavior
- **Effort**: S
- **Related Skill**: `auth-patterns`

### Task 1.3: Tiered Rate Limiting
- **Files**: `Explore.API/Program.cs`, new `Explore.API/RateLimiting/` directory
- **Action**: Add tiered rate limiting:
  - **Global**: Fixed window, 1000 req/min per IP (configurable)
  - **Authenticated**: Token bucket, 200 req/min per user
  - **Write**: Fixed window, 30 req/min per user for POST/PUT/DELETE
  - **Admin**: Separate higher limits
- **Configuration**: `appsettings.json` section `RateLimiting:*`
- **Acceptance Criteria**:
  - [ ] Global rate limit on all endpoints
  - [ ] Per-user rate limit on authenticated endpoints
  - [ ] Stricter limit on write operations
  - [ ] 429 Too Many Requests with `Retry-After` header
  - [ ] Rate limit headers (`X-RateLimit-Limit`, `X-RateLimit-Remaining`)
  - [ ] Integration test for rate limiting behavior
- **Effort**: M
- **Related Skill**: `auth-patterns`

---

## Phase 2: Observability & Debugging (Week 1-2)
**Priority: High** | **Risk: Low** | **Dependencies: None**

### Task 2.1: Structured Request/Response Logging Middleware
- **File**: `Explore.API/Middleware/RequestLoggingMiddleware.cs`
- **Action**: Create middleware that logs every request with: HTTP method, path, status code, duration (ms), request size, response size, user ID (if authenticated), tenant ID, correlation ID. Use Serilog structured logging.
- **Register**: In `Program.cs` pipeline early (after exception handler)
- **Acceptance Criteria**:
  - [ ] All requests logged with structured properties
  - [ ] Sensitive data (auth headers, body) NOT logged
  - [ ] Duration measured accurately
  - [ ] Correlation ID included in log context
  - [ ] Unit test for middleware behavior
- **Effort**: S
- **Related Skill**: `error-tracking`

### Task 2.2: Correlation ID Propagation (Incoming + Outgoing)
- **File**: `Explore.API/Middleware/CorrelationIdMiddleware.cs`
- **Action**: Create middleware that:
  1. Reads `X-Correlation-ID` or `X-Request-ID` from incoming request headers
  2. Generates a new one if absent (using `Activity.Current?.Id ?? Guid.NewGuid()`)
  3. Pushes to Serilog `LogContext` as `CorrelationId`
  4. Adds to response headers
  5. Existing `CorrelationIdDelegatingHandler` continues to propagate on outgoing HTTP
- **Acceptance Criteria**:
  - [ ] Incoming correlation ID respected
  - [ ] New ID generated when absent
  - [ ] All Serilog logs include CorrelationId property
  - [ ] Response includes `X-Correlation-ID` header
  - [ ] Integration test verifying propagation
- **Effort**: S
- **Related Skill**: `error-tracking`

### Task 2.3: Custom Business Metrics (OTel Meters)
- **File**: `Explore.Application/Telemetry/BusinessMetrics.cs`
- **Action**: Create custom `Meter` with business-relevant counters and histograms:
  - `events.created` (counter, tags: tenant_id, event_type)
  - `events.published` (counter, tags: tenant_id)
  - `registrations.created` (counter, tags: tenant_id, approval_status)
  - `organizations.created` (counter, tags: tenant_id)
  - `api.request.duration` (histogram, tags: endpoint, method, status)
  - Register meter in `Program.cs` with `AddMeter("Explore.Business")`
- **Acceptance Criteria**:
  - [ ] Metrics exposed on `/metrics` Prometheus endpoint
  - [ ] Counters incremented in relevant command handlers
  - [ ] Dimensional tags for multi-tenant analytics
  - [ ] Unit test for metric recording
- **Effort**: M
- **Related Skill**: `error-tracking`

---

## Phase 3: API Design & Contract (Week 2-3)
**Priority: High** | **Risk: Medium** | **Dependencies: None**

### Task 3.1: Formal API Versioning
- **Package**: `Asp.Versioning.Http` (add to `Directory.Packages.props`)
- **Files**: `Explore.API/Program.cs`, all controllers
- **Action**:
  - Register `AddApiVersioning()` with URL segment reader (preserving `api/v{version}/` pattern)
  - Add `[ApiVersion("1.0")]` to all existing controllers
  - Configure default version to 1.0
  - Add version info to OpenAPI docs
  - Keep route templates as `[Route("api/v{version:apiVersion}/[controller]")]`
- **Acceptance Criteria**:
  - [ ] All controllers decorated with `[ApiVersion("1.0")]`
  - [ ] `Asp.Versioning` package registered
  - [ ] OpenAPI docs show version info
  - [ ] Existing `api/v1/` URLs continue to work identically
  - [ ] Architecture test validating all controllers have `[ApiVersion]`
  - [ ] Integration test for version negotiation
- **Effort**: M
- **Related Skill**: `clean-architecture-rules`

### Task 3.2: ETag / Conditional Requests for GET Endpoints
- **Files**: `Explore.API/Middleware/ETagMiddleware.cs`, `Explore.Application/Contracts/Persistence/IGenericRepository.cs`
- **Action**:
  - Create `ETagMiddleware` that computes `ETag` from response body hash (SHA256 truncated) for GET responses with 200 status
  - Handle `If-None-Match` header → return 304 Not Modified when match
  - Apply only to `GET` requests returning `application/json` or `application/hal+json`
  - For entity-level ETags: Add `RowVersion`/`ConcurrencyToken` (EF Core) to major entities (Event, Organization) for optimistic concurrency + ETag source
- **Acceptance Criteria**:
  - [ ] GET responses include `ETag` header
  - [ ] `If-None-Match` returns 304 when content unchanged
  - [ ] OutputCache interacts correctly with ETags
  - [ ] Integration test for 304 behavior
- **Effort**: M

### Task 3.3: Idempotency Keys for Write Operations
- **Files**: `Explore.API/Middleware/IdempotencyMiddleware.cs`, `Explore.Persistence/Configurations/IdempotencyKeyConfiguration.cs`
- **Action**:
  - Accept `Idempotency-Key` header on POST/PUT requests
  - Store request hash + response in `IdempotencyRecord` table (key, request_hash, response_body, status_code, created_at, expires_at)
  - If duplicate key detected, return cached response without re-executing
  - TTL: 24 hours (configurable)
  - Use HybridCache for fast lookup, DB as persistence
- **Acceptance Criteria**:
  - [ ] Duplicate POST with same key returns same response
  - [ ] Different body with same key returns 422 Unprocessable
  - [ ] Expired keys are cleaned up
  - [ ] EF migration for `IdempotencyRecord` table
  - [ ] Integration test for idempotency behavior
- **Effort**: L
- **Related Skill**: `dotnet-efcore-guidelines`

### Task 3.4: Cursor-Based Pagination (Alternative to Offset)
- **Files**: `Explore.Application/Responses/CursorPaginatedResult.cs`, `Explore.Application/Contracts/Persistence/IGenericRepository.cs`
- **Action**:
  - Add `CursorPaginatedResult<T>` with `Cursor`, `HasMore`, `Items`, `NextCursor`
  - Add `GetByCursorAsync()` to `IGenericRepository` (keyset pagination using `Id > cursor ORDER BY Id`)
  - Support both pagination styles: `?pageNumber=1&pageSize=20` (offset) and `?cursor=xxx&limit=20` (cursor)
  - Apply to endpoints where real-time consistency matters (event lists, registrations)
- **Acceptance Criteria**:
  - [ ] `CursorPaginatedResult<T>` response type created
  - [ ] Cursor pagination available on event list endpoint
  - [ ] HATEOAS links include `next` with cursor parameter
  - [ ] Integration test for cursor pagination
- **Effort**: M

---

## Phase 4: Performance Optimization (Week 3-4)
**Priority: Medium** | **Risk: Low** | **Dependencies: Phase 3.2 for ETag**

### Task 4.1: Compiled EF Core Queries on Hot Paths
- **Files**: `Explore.Persistence/Repositories/EventRepository.cs`, `Explore.Persistence/Repositories/CategoryRepository.cs`, `Explore.Persistence/Repositories/OrganizationRepository.cs`
- **Action**:
  - Identify top 5 most-called repository methods (via benchmark data in `Event.Benchmarks/`)
  - Convert to `EF.CompileAsyncQuery()` for:
    - `GetEventById` (detail page)
    - `GetEventListWithDetails` (listing page)
    - `GetCategoryById` (lookup)
    - `GetOrganizationById` (detail page)
    - `GetActiveEventsByTenant` (dashboard)
  - Maintain non-compiled versions for complex dynamic queries (specification pattern)
- **Acceptance Criteria**:
  - [ ] Top 5 hot-path queries compiled
  - [ ] Benchmark comparison showing improvement
  - [ ] Existing tests pass without changes
  - [ ] No regression in specification-pattern queries
- **Effort**: M
- **Related Skill**: `dotnet-efcore-guidelines`

### Task 4.2: AsNoTracking for Read-Only Queries
- **Files**: `Explore.Persistence/Repositories/GenericRepository.cs`, all query handlers
- **Action**:
  - Audit all `GET` query handlers to ensure `.AsNoTracking()` is used
  - Add `.AsNoTracking()` to `GenericRepository` read methods by default
  - Verify tracking is only enabled where explicitly needed (update handlers)
- **Acceptance Criteria**:
  - [ ] All read-only repository methods use `AsNoTracking()`
  - [ ] Write operations still use tracked queries
  - [ ] Architecture test: query handlers must not call tracked queries (optional)
- **Effort**: S
- **Related Skill**: `dotnet-efcore-guidelines`

### Task 4.3: Response Payload Optimization
- **Files**: Various DTOs in `Explore.Application/DTOs/`
- **Action**:
  - Audit all DTOs for fields that are never used by clients (overfetching)
  - Add `[JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]` to nullable optional fields
  - Ensure list DTOs (`*ListDto`) are truly minimal (no nested objects)
  - Add `System.Text.Json` source generators for hot-path DTOs (AOT-friendly serialization)
- **Acceptance Criteria**:
  - [ ] List DTOs contain only essential fields
  - [ ] Null fields omitted from JSON responses
  - [ ] Source generators for top 5 most-serialized DTOs
  - [ ] Benchmark comparison for serialization performance
- **Effort**: M

---

## Phase 5: Resilience & Reliability (Week 4-5)
**Priority: Medium** | **Risk: Medium** | **Dependencies: Phase 2.2**

### Task 5.1: Request Timeout Per Endpoint
- **File**: `Explore.API/Program.cs`
- **Action**:
  - Add `RequestTimeoutOptions` middleware from ASP.NET Core 10
  - Default timeout: 30 seconds
  - Configure shorter timeouts for simple lookups (10s)
  - Configure longer timeouts for complex operations (60s: search, file upload)
  - Return 504 Gateway Timeout on expiration
- **Acceptance Criteria**:
  - [ ] Default 30s timeout on all endpoints
  - [ ] Per-policy timeout overrides
  - [ ] 504 response on timeout
  - [ ] Integration test for timeout behavior
- **Effort**: S

### Task 5.2: Optimistic Concurrency (ConcurrencyToken)
- **Files**: `Explore.Domain/Event.cs`, `Explore.Domain/Organization.cs`, EF configurations
- **Action**:
  - Add `uint RowVersion` property to major entities (Event, Organization, EventSession)
  - Configure as `[Timestamp]` in EF Core (PostgreSQL `xmin` system column)
  - Handle `DbUpdateConcurrencyException` in update command handlers → return 409 Conflict with retry guidance
  - Include `RowVersion` in update DTOs for clients to send back
- **Acceptance Criteria**:
  - [ ] `RowVersion` on Event, Organization, EventSession entities
  - [ ] EF Core configured with `IsRowVersion()`
  - [ ] 409 Conflict on concurrent update
  - [ ] Client receives current RowVersion in GET responses
  - [ ] Integration test for concurrent update scenario
- **Effort**: M
- **Related Skill**: `dotnet-efcore-guidelines`

### Task 5.3: Circuit Breaker for Database Access
- **File**: `Explore.Persistence/PersistenceServicesRegistration.cs`
- **Action**:
  - Add Polly circuit breaker around database operations (connection failures)
  - Configuration: Trip after 5 consecutive failures, break for 30 seconds
  - Return 503 Service Unavailable with `Retry-After` header when circuit is open
  - Log circuit state changes
- **Acceptance Criteria**:
  - [ ] Circuit breaker wraps EF Core operations
  - [ ] Trips on consecutive connection failures
  - [ ] Returns 503 with Retry-After when open
  - [ ] Logs state transitions
  - [ ] Unit test for circuit behavior
- **Effort**: M

---

## Phase 6: Testing & Documentation (Week 5-6)
**Priority: High** | **Risk: Low** | **Dependencies: All previous phases**

### Task 6.1: Performance Benchmark Suite
- **File**: `Event.Benchmarks/Benchmarks/ApiPerformanceBenchmarks.cs`
- **Action**:
  - Add BenchmarkDotNet benchmarks for:
    - Serialization: HAL response vs minimal response
    - Repository: Compiled vs non-compiled queries
    - Caching: HybridCache hit vs miss
    - Pagination: Offset vs cursor
  - Document baseline P50/P95/P99 latencies
- **Acceptance Criteria**:
  - [ ] Benchmark suite for key API operations
  - [ ] Baseline results documented
  - [ ] Can be run in CI for regression detection
- **Effort**: M

### Task 6.2: Integration Tests for New Middleware
- **File**: `Event.API.IntegrationTests/Middleware/`
- **Action**:
  - Add integration tests for all new middleware:
    - Security headers verification
    - Rate limiting behavior (429 response)
    - Correlation ID propagation
    - ETag / 304 behavior
    - Idempotency key behavior
    - Request timeout behavior
- **Acceptance Criteria**:
  - [ ] Each middleware has at least 3 test cases (happy path, edge case, error case)
  - [ ] Tests use `WebApplicationFactory`
  - [ ] All tests green
- **Effort**: L

### Task 6.3: Architecture Tests for New Conventions
- **File**: `Event.Architecture.Tests/ApiConventionTests.cs`
- **Action**:
  - Add architecture tests for:
    - All controllers must have `[ApiVersion]` attribute
    - All GET endpoints must have `[OutputCache]` or explicit opt-out comment
    - All write endpoints must have `[Authorize]`
    - All query handlers must use `AsNoTracking()` queries
    - All command handlers must invalidate relevant caches
- **Acceptance Criteria**:
  - [ ] Architecture tests enforce new conventions
  - [ ] Tests pass on current codebase
  - [ ] Clear failure messages for violations
- **Effort**: S

### Task 6.4: API Changelog
- **File**: `docs/API_CHANGELOG.md`
- **Action**:
  - Create API changelog documenting all breaking/non-breaking changes
  - Format: Keep a Changelog (https://keepachangelog.com)
  - Include current v1 API as baseline entry
  - Add section in OpenAPI description linking to changelog
- **Acceptance Criteria**:
  - [ ] Changelog file created with v1.0 baseline
  - [ ] Format follows Keep a Changelog
  - [ ] Linked from Scalar/Swagger UI
- **Effort**: S

---

## Risk Assessment and Mitigation

| Risk | Probability | Impact | Mitigation |
|------|-------------|--------|------------|
| API versioning breaks existing clients | Medium | High | Apply `[ApiVersion("1.0")]` only; existing URLs unchanged |
| ETag computation adds latency | Low | Medium | Use efficient hashing; OutputCache handles most cases |
| Idempotency table grows unbounded | Medium | Medium | TTL cleanup + background job to purge expired records |
| Compiled queries reduce flexibility | Low | Medium | Keep non-compiled versions for dynamic/specification queries |
| Rate limiting blocks legitimate traffic | Medium | High | Start with generous limits; monitor before tightening |
| Concurrency tokens break existing updates | Medium | High | Make `RowVersion` optional in DTOs initially; gradual adoption |

---

## Success Metrics

| Metric | Current | Target |
|--------|---------|--------|
| Security headers score (securityheaders.com) | D/F | A |
| P95 API response time (hot path) | ~50ms (estimated) | <30ms |
| Cache hit ratio (HybridCache) | Unknown | >80% |
| 304 Not Modified ratio | 0% | >30% of repeated GETs |
| Rate limit false positive rate | N/A | <0.1% |
| Test coverage for middleware | 0% | >90% |
| Architecture test violations | 0 | 0 (maintained) |

---

## Effort Estimates Summary

| Phase | Effort | Duration |
|-------|--------|----------|
| Phase 1: Security Hardening | S+S+M = M | ~3-4 days |
| Phase 2: Observability & Debugging | S+S+M = M | ~3-4 days |
| Phase 3: API Design & Contract | M+M+L+M = XL | ~8-10 days |
| Phase 4: Performance Optimization | M+S+M = M | ~4-5 days |
| Phase 5: Resilience & Reliability | S+M+M = M | ~4-5 days |
| Phase 6: Testing & Documentation | M+L+S+S = L | ~6-7 days |
| **Total** | | **~28-35 days** |

---

## Critique: Potential Risks & Unknowns

**Most likely failure point**: **Task 3.3 (Idempotency Keys)** is the highest-complexity task. The interaction between `IdempotencyMiddleware`, `HybridCache`, and the database persistence layer is non-trivial. Edge cases include: partial failures (request processed but response not cached), concurrent identical requests racing, and serialization of diverse response types. The TTL cleanup also needs a background service. **Recommendation**: Start with a simpler in-memory-only idempotency check (using `HybridCache` alone, no DB table) and add persistence later if needed.

**Second risk area**: **Task 3.1 (API Versioning)** has a deceptively large blast radius. Every controller file needs modification, and the route templates change from `[Route("api/v1/[controller]")]` to `[Route("api/v{version:apiVersion}/[controller]")]`. This touches 40+ controllers. While the change is mechanical, any typo breaks the endpoint. **Mitigation**: Write the architecture test first (Task 6.3), then apply changes systematically.

**Third risk area**: **Task 5.2 (Optimistic Concurrency)** could break existing update flows if clients don't send the `RowVersion` back. PostgreSQL's `xmin` is auto-managed but requires EF Core-specific configuration with the Npgsql provider. This needs careful testing with the existing integration test suite.

**Unknown**: The compiled queries (Task 4.1) performance gain depends on query complexity. Simple key lookups may see <5% improvement. The benchmark suite (Task 6.1) should be completed first to identify the actual hot paths worth optimizing. Don't pre-optimize all queries.
