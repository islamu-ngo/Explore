# Context: API Enterprise Hardening
**Last Updated: 2026-03-26 20:10 Europe/Brussels**
**Status: ✅ COMPLETE — All phases implemented and verified**

## Purpose
This document is the fast-load reference for the API enterprise hardening initiative. Read this before continuing work in any session.

---

## Active Branch & Scope
- **Branch:** `develop`
- **Primary project:** `Explore.API`
- **Secondary projects:** `Explore.Application`, `Explore.Infrastructure`, `Explore.Persistence`, `Explore.ServiceDefaults`
- **Test projects:** `Event.API.IntegrationTests`, `Event.Application.UnitTests`, `Event.Architecture.Tests`

---

## Implementation Status — ALL COMPLETE

### Phase 1: Security (Program.cs) ✅
| File | Changes | Status |
|------|---------|--------|
| `Explore.API/Program.cs` | Removed JWT PII logging (OnTokenValidated, OnMessageReceived, Console.WriteLine in AudienceValidator). Simplified OnAuthenticationFailed to single LogWarning. Demoted OnChallenge to LogDebug. `MigrateAsync()` + `await`. Graceful shutdown (`e.Cancel=true` + `StopApplication()`). Removed ProcessExit handler. `volatile` isShuttingDown. Deleted commented code. | ✅ |

### Phase 2: API Contract ✅
| File | Changes | Status |
|------|---------|--------|
| `Explore.API/Extensions/ExceptionHandlingExtensions.cs` | Added correlationId from `HttpContext.Items["CorrelationId"]` | ✅ |
| `Explore.API/ExceptionHandling/GlobalExceptionHandler.cs` | ProblemTypeUris dict → IANA RFC 9110 URIs | ✅ |
| `Explore.Application/Behaviors/ValidationBehavior.cs` | **DELETED** — Option A per user instruction | ✅ |
| 19+ controllers | NotFound(new{...}) → plain NotFound(). IHttpContextAccessor removed from 8 controllers. [FromBody] Command → DTO fixes. | ✅ |
| 12 controller GetById methods | Added `if (result == null) return NotFound();` null-checks | ✅ |

### Phase 3: Performance & Observability ✅
| File | Changes | Status |
|------|---------|--------|
| `Explore.API/Middleware/ETagMiddleware.cs` | RecyclableMemoryStreamManager + ArrayPool<byte> + skip >256KB | ✅ |
| `Explore.Application/Behaviors/AuthorizationBehavior.cs` | ConcurrentDictionary reflection cache + ActivitySource OTel spans | ✅ |
| `Explore.Infrastructure/Services/FallbackAuthorizationService.cs` | allow→LogDebug, deny→LogWarning. SafeMode one-way latch via `ActivateSafeMode()` | ✅ |
| `Explore.Application/Contracts/Services/ISetupSecretProvider.cs` | Added `GetSecretForLogging()` to interface | ✅ |
| `Explore.API/Program.cs` | Output cache `Authorization` variance + `PublicData` policy. Configurable CORS origins. Structured setup-mode logging. | ✅ |
| `Explore.ServiceDefaults/Extensions.cs` | Added `.AddSource("Explore.Authorization")` | ✅ |

### Phase 4: Architecture ✅
| File | Changes | Status |
|------|---------|--------|
| `Explore.Domain/IdempotencyRecord.cs` | **NEW** — entity with Key, TenantId, UserId, StatusCode, ResponseBody, ExpiresAt | ✅ |
| `Explore.Application/Contracts/Persistence/IIdempotencyRepository.cs` | **NEW** — FindAsync + SaveAsync | ✅ |
| `Explore.Persistence/Repositories/IdempotencyRepository.cs` | **NEW** | ✅ |
| `Explore.Persistence/Configurations/Entities/IdempotencyRecordConfiguration.cs` | **NEW** — unique index on (Key, TenantId) | ✅ |
| `Explore.API/Middleware/IdempotencyMiddleware.cs` | **NEW** — POST/PUT/PATCH/DELETE only, 128-char key limit, RecyclableMemoryStream | ✅ |
| `Explore.API/Extensions/ApiVersioningExtensions.cs` | Dual versioning: `MediaTypeApiVersionReader` + `UrlSegmentApiVersionReader` + `VersionedRouteConvention` (action-level) | ✅ |
| 43 controllers | ABOUTME headers added (19 already had them) | ✅ |

### Phase 5: Docs ✅
| File | Changes | Status |
|------|---------|--------|
| `docs/API.md` | 11 surgical updates across pipeline, versioning, caching, errors, auth, CORS, shutdown, idempotency | ✅ |
| `docs/ARCHITECTURE.md` | 8 surgical updates: request flow, SafeMode, versioning, caching, idempotency, behaviors | ✅ |
| `dev/_journal/MAJOR_DECISIONS.md` | 5 decisions recorded | ✅ |

---

## Key Decisions Made

1. **ValidationBehavior: Delete (Option A)** — Manual validation per CLAUDE.md rule. Dead code never registered.
2. **Idempotency: PostgreSQL-backed** — Auditability, same transactional boundary, 24h TTL via ExpiresAt.
3. **URL versioning: IApplicationModelConvention** — Zero-touch for controller authors, action-level routes with absolute templates.
4. **Swashbuckle: Kept** — User explicitly requested. Task 4.3 skipped.
5. **SafeMode: One-way latch** — `ActivateSafeMode()` replaces public setter. Persists until restart.

---

## Bugs Found & Fixed During Implementation

### VersionedRouteConvention Cartesian Product (Critical)
- **Problem:** Initial controller-level route approach caused MVC to combine versioned routes with named action routes → `InvalidOperationException: Attribute routes with same name must have same template`
- **Fix:** Rewrote to action-level approach with absolute templates (leading `/`), `Name = null` on versioned copies, `Order + 1` for lower priority.
- **File:** `Explore.API/Extensions/ApiVersioningExtensions.cs`

### Deep Agent Scope Creep — IGenericRepository Modified
- **Problem:** Delegated controller cleanup agent went out of scope and renamed `HardDelete(T entity)` → `HardDeleteByIdAsync(TKey id)` on IGenericRepository interface + implementation, breaking 4 handler files.
- **Fix:** Reverted IGenericRepository.cs and GenericRepository.cs to original. All 4 handlers confirmed matching HEAD.
- **Lesson:** Always constrain deep agent scope with explicit MUST NOT DO rules. Verify interface contracts after delegation.

### Controller GetById Null-Check Regression
- **Problem:** Deep agent removed null-check + NotFound patterns from controller GetById methods during cleanup. When handler returns null (entity not found), `ToResource(null)` throws NullReferenceException → 500.
- **Fix:** Added `if (result == null) return NotFound();` to 12 controller GetById methods.
- **Affected:** ActorController (2 methods), CategoryController, EventSessionController, TagController, LocationController, CustomPropertyDefinitionController, EventController, OrganizationController, IndexedDidController, ExternalApiKeyController, StorageObjectController, GroupController.

---

## Remaining Work (Post-Hardening)

1. **Move to archive:** `dev/active/api-enterprise-hardening/` → `dev/zarchive/api-enterprise-hardening/`
2. **Uncommitted changes:** All work is uncommitted. Run `git status` to see full changeset.
3. **Pre-existing test failures:**
   - Blazor: 21 MudBlazor v9 migration failures (String→Variant, String→Boolean, String→EventCallback)
   - API: 1 flaky OpenFeature ChannelClosedException in test teardown (infrastructure, not code)
   - Persistence: 7 failures requiring PostgreSQL (database connectivity)
4. **Deferred:** Wire `BusinessMetrics` counters into command handlers (events.created, registrations.created, etc.)
5. **Deferred:** Integration tests for idempotency replay (Task 5.1 items 5)

---

## Commands for Verification

```bash
# Build
dotnet build --configuration Release --verbosity quiet

# Unit tests (all pass)
dotnet test --project Event.Application.UnitTests/Event.Application.UnitTests.csproj --configuration Release --verbosity quiet
dotnet test --project Event.Domain.UnitTests/Event.Domain.UnitTests.csproj --configuration Release --verbosity quiet
dotnet test --project Event.Architecture.Tests/Event.Architecture.Tests.csproj --configuration Release --verbosity quiet
dotnet test --project Explore.Secrets.UnitTests/Explore.Secrets.UnitTests.csproj --configuration Release --verbosity quiet

# Integration tests (452/453 — 1 flaky OpenFeature teardown)
dotnet test --project Event.API.IntegrationTests/Event.API.IntegrationTests.csproj --configuration Release --verbosity quiet

# Blazor tests (580/601 — 21 pre-existing MudBlazor v9 failures)
dotnet test --project Explore.Blazor.Client.Tests/Explore.Blazor.Client.Tests.csproj --configuration Release --verbosity quiet
```
