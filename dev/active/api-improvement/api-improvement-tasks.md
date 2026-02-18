# Tasks: Enterprise-Grade API Improvement

**Last Updated**: 2026-02-18

---

## Phase 1: Security Hardening (Week 1) — Priority: Critical — ✅ COMPLETE

- [x] **1.1** Add `SecurityHeadersMiddleware` — `X-Content-Type-Options`, `X-Frame-Options`, `Referrer-Policy`, `Permissions-Policy`, `CSP` | **S** | File: `Explore.API/Middleware/SecurityHeadersMiddleware.cs`
- [x] **1.2** Harden CORS policies — Replace `AllowAnyOrigin` with config-driven allowlists on Internal/External policies | **S** | File: `Explore.API/Program.cs`
- [x] **1.3** Tiered rate limiting — Global (200 tokens/IP token bucket), Authenticated (200/min/user sliding window), Write (30/min/user fixed window) with `Retry-After` and `X-RateLimit-*` headers | **M** | File: `Explore.API/Extensions/RateLimitingExtensions.cs`
  - Supports X-Forwarded-For for ngrok/Cloudflare reverse proxy
  - Loopback IP bypass for local development
  - No-op limiters in "Testing" environment
  - Config-driven limits via `RateLimiting:*` section

---

## Phase 2: Observability & Debugging (Week 1-2) — Priority: High — ✅ COMPLETE

- [x] **2.1** Structured request logging middleware — Method, path, status, duration, user, tenant, correlation ID | **S** | File: `Explore.API/Middleware/RequestLoggingMiddleware.cs`
- [x] **2.2** Correlation ID propagation — Incoming parse + generate + Serilog LogContext + response header | **S** | File: `Explore.API/Middleware/CorrelationIdMiddleware.cs`
- [x] **2.3** Custom business metrics (OTel) — `events.created`, `registrations.created`, `organizations.created`, `authorization.decisions` counters with tenant tags | **M** | File: `Explore.Application/Telemetry/BusinessMetrics.cs`
  - Meter registered in `Explore.ServiceDefaults/Extensions.cs` as `Explore.Business`

---

## Phase 3: API Design & Contract (Week 2-3) — Priority: High — PARTIALLY COMPLETE

- [x] **3.1** Formal API versioning — `Asp.Versioning.Mvc` 8.1.0, **media type versioning** (`Accept: application/json;v=1.0`), `[ApiVersion("1.0")]` on all 42 controllers | **M** | Files: `Directory.Packages.props`, `Explore.API/Extensions/ApiVersioningExtensions.cs`, all controllers
  - **NOTE**: User chose media type versioning (not URL versioning as originally planned)
  - Default version 1.0 when unspecified, keeps URLs clean
- [x] **3.2** ETag / conditional requests — SHA256 body hash weak ETag, `If-None-Match` → 304 Not Modified for GET/HEAD with JSON content types | **M** | File: `Explore.API/Middleware/ETagMiddleware.cs`
- [ ] **3.3** Idempotency keys — `Idempotency-Key` header on POST/PUT, `IdempotencyRecord` table, HybridCache fast path | **L** | Files: `Explore.API/Middleware/IdempotencyMiddleware.cs`, migration
- [ ] **3.4** Cursor-based pagination — `CursorPaginatedResult<T>`, keyset pagination, optional on event list | **M** | Files: `Explore.Application/Responses/CursorPaginatedResult.cs`, repos

---

## Phase 4: Performance Optimization (Week 3-4) — Priority: Medium

- [ ] **4.1** Compiled EF Core queries — Top 5 hot-path repository methods: EventById, EventList, CategoryById, OrgById, ActiveEventsByTenant | **M** | Files: `Explore.Persistence/Repositories/`
- [ ] **4.2** AsNoTracking audit — Ensure all read-only repository methods use `.AsNoTracking()` | **S** | File: `Explore.Persistence/Repositories/GenericRepository.cs`
- [ ] **4.3** Response payload optimization — `JsonIgnore(WhenWritingNull)`, minimal list DTOs, STJ source generators for hot DTOs | **M** | Files: `Explore.Application/DTOs/`

---

## Phase 5: Resilience & Reliability (Week 4-5) — Priority: Medium

- [ ] **5.1** Request timeout per endpoint — Default 30s, lookups 10s, complex 60s, 504 on timeout | **S** | File: `Explore.API/Program.cs`
- [ ] **5.2** Optimistic concurrency — `RowVersion` on Event/Organization/EventSession, handle `DbUpdateConcurrencyException` → 409 Conflict | **M** | Files: Domain entities, EF configs, update handlers
- [ ] **5.3** Circuit breaker for DB — Polly circuit breaker on EF Core operations, 503 + Retry-After when open | **M** | File: `Explore.Persistence/PersistenceServicesRegistration.cs`

---

## Phase 6: Testing & Documentation (Week 5-6) — Priority: High

- [ ] **6.1** Performance benchmark suite — Serialization, compiled queries, caching, pagination benchmarks | **M** | File: `Event.Benchmarks/`
- [ ] **6.2** Integration tests for new middleware — Security headers, rate limiting, correlation ID, ETag, idempotency, timeout | **L** | File: `Event.API.IntegrationTests/Middleware/`
- [ ] **6.3** Architecture tests for new conventions — `[ApiVersion]`, `[OutputCache]`, `AsNoTracking`, cache invalidation | **S** | File: `Event.Architecture.Tests/ApiConventionTests.cs`
- [ ] **6.4** API changelog — `docs/API_CHANGELOG.md` with v1.0 baseline, linked from Scalar UI | **S** | File: `docs/API_CHANGELOG.md`

---

## Ad-Hoc / Bug Fixes Completed

- [x] Fixed pre-existing build error: `GetTenantOnboardingStatusQueryHandler.cs` missing `using Explore.Application.Contracts.Identity;`
- [x] Fixed rate limiting 429 failures in integration tests by adding `IHostEnvironment` parameter and no-op limiters for "Testing" environment
- [x] Fixed `RateLimitPartition.GetNoLimiter` generic type parameter in test-environment policy registration

---

## Remaining Configuration TODO

- [ ] Add `RateLimiting:*` section to `appsettings.json` with explicit default values
- [ ] Add `Cors:AllowedOrigins` section to `appsettings.json`
- [ ] Document media type versioning usage in `docs/API.md`
- [ ] Document rate limiting headers in `docs/API.md`
- [ ] Document ETag support in `docs/API.md`

---

## Summary Progress

| Phase | Status | Tasks Done | Tasks Total |
|-------|--------|------------|-------------|
| Phase 1: Security | ✅ Complete | 3/3 | 3 |
| Phase 2: Observability | ✅ Complete | 3/3 | 3 |
| Phase 3: API Design | 🔶 Partial | 2/4 | 4 |
| Phase 4: Performance | ⬜ Not Started | 0/3 | 3 |
| Phase 5: Resilience | ⬜ Not Started | 0/3 | 3 |
| Phase 6: Testing & Docs | ⬜ Not Started | 0/4 | 4 |
| **Total** | | **8/20** | **20** |
