# Context: Enterprise-Grade API Improvement

**Last Updated**: 2026-02-18

---

## SESSION PROGRESS

### 2026-02-18: Initial Planning
- **Completed**: Full codebase analysis, gap identification, plan creation
- **State**: Plan created, awaiting review. No code changes yet.

### 2026-02-18: Phase 1-3 Implementation (Security, Observability, API Design)
- **Completed**: Phases 1, 2, and 3 (partial: 3.1 + 3.2 done; 3.3 + 3.4 deferred)
- **State**: All implemented middleware, rate limiting, versioning, ETags working. All 1,429 tests passing.
- **Key choices**:
  - User requested **media type versioning** (not URL versioning as originally planned). Accept header: `application/json;v=1.0`
  - Rate limiting uses **token bucket** for global (IP-based) and **sliding window** for authenticated users
  - Rate limiting disabled in "Testing" environment to avoid 429s during parallel test execution
  - Fixed pre-existing build error: missing `using` in `GetTenantOnboardingStatusQueryHandler.cs`
- **Next**: Phases 3.3, 3.4, 4, 5, 6 remain (idempotency, cursor pagination, performance, resilience, testing/docs)

---

## Key Files

### New Files Created This Session

| File | Purpose |
|------|---------|
| `Explore.API/Middleware/SecurityHeadersMiddleware.cs` | X-Content-Type-Options, X-Frame-Options, Referrer-Policy, Permissions-Policy, CSP headers |
| `Explore.API/Middleware/CorrelationIdMiddleware.cs` | Incoming/outgoing correlation ID propagation with Serilog LogContext |
| `Explore.API/Middleware/RequestLoggingMiddleware.cs` | Structured logging: method, path, status, duration, user, tenant, correlation ID |
| `Explore.API/Middleware/ETagMiddleware.cs` | Weak ETag from SHA256 hash of response body, 304 Not Modified for GET/HEAD |
| `Explore.API/Extensions/RateLimitingExtensions.cs` | Tiered rate limiting: Global (IP token bucket), Authenticated (sliding window), Write (fixed window), SetupSecret (fixed window) |
| `Explore.API/Extensions/ApiVersioningExtensions.cs` | Media type versioning via Accept header (`application/json;v=1.0`) |
| `Explore.Application/Telemetry/BusinessMetrics.cs` | OpenTelemetry custom meter `Explore.Business` with counters for events, registrations, organizations, auth |

### Modified Files This Session

| File | Changes |
|------|---------|
| `Directory.Packages.props` | Added `Asp.Versioning.Mvc` 8.1.0, `Asp.Versioning.Mvc.ApiExplorer` 8.1.0 |
| `Explore.API/Explore.API.csproj` | Added PackageReferences for Asp.Versioning |
| `Explore.API/Program.cs` | Registered all new services + middleware. Updated CORS to config-driven origins. Middleware pipeline: SecurityHeaders -> CorrelationId -> RequestLogging -> (existing) -> ETag after OutputCache |
| `Explore.ServiceDefaults/Extensions.cs` | Added `.AddMeter("Explore.Business")` to OTel metrics |
| `Explore.Application/Features/TenantOnboarding/Handlers/Queries/GetTenantOnboardingStatusQueryHandler.cs` | Fixed pre-existing bug: added missing `using Explore.Application.Contracts.Identity;` |
| **All 42 controllers** in `Explore.API/Controllers/` | Added `using Asp.Versioning;` and `[ApiVersion("1.0")]` attribute |

### API Layer (Pre-existing)
| File | Purpose |
|------|---------|
| `Explore.API/Program.cs` | Main DI + middleware pipeline (~630 lines with new additions) |
| `Explore.API/Controllers/` | 42 controllers (thin, MediatR dispatch), all now versioned |
| `Explore.API/Middleware/PreferHeaderMiddleware.cs` | HATEOAS `Prefer: return=minimal` |
| `Explore.API/ExceptionHandling/GlobalExceptionHandler.cs` | RFC 7807 ProblemDetails |
| `Explore.API/ExceptionHandling/ValidationExceptionHandler.cs` | FluentValidation errors |
| `Explore.API/Hateoas/` | 17 assemblers, 17 link policies, RouteNames |
| `Explore.API/Filters/BlockInSingleTenantAttribute.cs` | Multi-tenant endpoint guard |
| `Explore.API/BackgroundServices/PdsSyncWorker.cs` | AT Protocol sync |

### Application Layer
| File | Purpose |
|------|---------|
| `Explore.Application/Behaviors/AuthorizationBehavior.cs` | Cerbos enforcement pipeline |
| `Explore.Application/Behaviors/PerformanceBehavior.cs` | Slow request logging (>500ms) |
| `Explore.Application/Telemetry/BusinessMetrics.cs` | **NEW** - OTel business counters |
| `Explore.Application/Responses/BaseCommandResponse.cs` | Standard command response |
| `Explore.Application/Responses/PaginatedResult.cs` | Offset pagination wrapper |
| `Explore.Application/Specifications/` | Composable query specifications |

### Infrastructure Layer
| File | Purpose |
|------|---------|
| `Explore.Infrastructure/Services/CorrelationIdDelegatingHandler.cs` | Outgoing correlation ID |
| `Explore.Infrastructure/Mail/EmailResiliencePipelines.cs` | Polly retry for SMTP |
| `Explore.Infrastructure/Services/Cerbos*.cs` | Cerbos integration |

### Persistence Layer
| File | Purpose |
|------|---------|
| `Explore.Persistence/Repositories/GenericRepository.cs` | Base CRUD with soft delete |
| `Explore.Persistence/ExploreDbContext.cs` | DbContext with query filters |
| `Explore.Persistence/PersistenceServicesRegistration.cs` | Repository DI |

### Test Projects
| File | Purpose |
|------|---------|
| `Event.API.IntegrationTests/` | 382 tests, HATEOAS, auth, smoke tests |
| `Event.Architecture.Tests/` | 27 tests, layer deps, CQRS patterns, naming |
| `Event.Application.UnitTests/` | 272 tests |
| `Event.Domain.UnitTests/` | 61 tests |
| `Explore.Secrets.UnitTests/` | 190 tests |
| `Explore.Blazor.Client.Tests/` | 495 tests |
| `Event.Persistence.IntegrationTests/` | 2 tests |
| `Event.Benchmarks/` | EF Core query benchmarks |

### Observability
| File | Purpose |
|------|---------|
| `Explore.ServiceDefaults/Extensions.cs` | OpenTelemetry, health checks, resilience, **now includes `Explore.Business` meter** |
| `Explore.Diagnostic/` | Serilog, metrics, tracing extensions |

### Configuration
| File | Purpose |
|------|---------|
| `Directory.Packages.props` | Central NuGet package management |
| `Explore.API/appsettings.json` | API configuration |

---

## Decisions Made

| Decision | Rationale |
|----------|-----------|
| **Media type API versioning** (Accept header: `application/json;v=1.0`) | User explicit request. Keeps URLs clean (no `/` changes). Standard approach for APIs consumed by controlled clients. |
| ETag via response body hash (not entity version) | Works with OutputCache; no entity changes needed for Phase 3.2 |
| Idempotency via HybridCache + DB table | Survives restarts; HybridCache for fast lookup (deferred to future session) |
| Cursor pagination keyset-based (not token) | More efficient for PostgreSQL; no session state (deferred to future session) |
| Security headers as middleware (not filter) | Applies to all responses including non-controller (health, metrics) |
| Rate limiting via built-in ASP.NET Core | Already partially implemented; no external dependency needed |
| **Token bucket for global rate limiter** | Better burst tolerance than fixed window; configurable replenish rate |
| **No-op rate limiter in Testing environment** | Prevents 429s during parallel test execution in `WebApplicationFactory` |
| **Loopback IP bypass in global limiter** | `IPAddress.IsLoopback` -> NoLimiter, supports local development |
| **X-Forwarded-For support** | Required for ngrok/Cloudflare reverse proxy deployments |
| **Rate limiting defaults are config-driven** | `appsettings.json` section `RateLimiting:*` with sensible defaults baked into code |

---

## Dependencies

### NuGet Packages Added

| Package | Version | Purpose |
|---------|---------|---------|
| `Asp.Versioning.Mvc` | 8.1.0 | Media type API versioning |
| `Asp.Versioning.Mvc.ApiExplorer` | 8.1.0 | OpenAPI version docs |

All other features use packages already in `Directory.Packages.props`.

---

## Middleware Pipeline Order (Program.cs)

```
SecurityHeaders -> CorrelationId -> RequestLogging -> ResponseCompression -> HTTPS Redirect -> Hateoas -> Routing -> Authentication -> RateLimiter -> Authorization -> OutputCache -> ETag -> Controllers
```

---

## Rate Limiting Configuration Defaults

| Policy | Type | Default | Key |
|--------|------|---------|-----|
| Global | Token Bucket | 200 tokens, replenish 40/10s | IP address (X-Forwarded-For aware) |
| Authenticated | Sliding Window | 200/min, 4 segments | User identity |
| Write | Fixed Window | 30/min | User identity |
| SetupSecret | Fixed Window | 5/min | IP address |

Configurable via `appsettings.json`:
```json
"RateLimiting": {
  "Global": { "TokenLimit": 200, "ReplenishPeriodSeconds": 10, "TokensPerPeriod": 40 },
  "Authenticated": { "PermitLimit": 200, "WindowSeconds": 60, "SegmentsPerWindow": 4 },
  "Write": { "PermitLimit": 30, "WindowSeconds": 60 }
}
```

---

## Test Results (2026-02-18)

| Project | Tests | Status |
|---------|-------|--------|
| Event.Application.UnitTests | 272 | Pass |
| Event.Domain.UnitTests | 61 | Pass |
| Event.Architecture.Tests | 27 | Pass |
| Explore.Secrets.UnitTests | 190 | Pass |
| Explore.Blazor.Client.Tests | 495 | Pass |
| Event.Persistence.IntegrationTests | 2 | Pass |
| Event.API.IntegrationTests | 382 | Pass |
| **Total** | **1,429** | **All Pass** |

---

## Bugs Found & Fixed

1. **Pre-existing**: `GetTenantOnboardingStatusQueryHandler.cs` missing `using Explore.Application.Contracts.Identity;` for `IAdminContext`. Fixed by adding the using statement.
2. **Rate limiting in tests**: Global token bucket (200 tokens) exhausted during parallel test execution causing 156 test failures (429). Fixed by adding `IHostEnvironment` parameter to `AddApiRateLimiting` and returning no-op limiters when `environment.EnvironmentName == "Testing"`.
3. **Generic type error**: `RateLimitPartition.GetNoLimiter` in `AddPolicy` lambdas needs `<string>` not `<HttpContext, string>` type parameter. Fixed.

---

## Quick Resume

To resume work on this task:
1. Read this context file
2. Read `api-improvement-tasks.md` for progress
3. Check `api-improvement-plan.md` for phase details
4. Start with the next unchecked task in `api-improvement-tasks.md`
5. Build first: `dotnet build --configuration Release --verbosity quiet`
6. All 1,429 tests should pass before starting new work

## Context Reset Session Update (2026-02-23 18:12 Europe/Brussels)

- Current implementation state: No new implementation changes in this session for this track.
- Key decisions made this session: Priority focused on admin consolidation handoff in navbar customization track.
- Files modified and why: None in this track during this session.
- Blockers/issues discovered: None newly discovered for this track.
- Next immediate steps: Continue from highest-priority unchecked items in this task file.
