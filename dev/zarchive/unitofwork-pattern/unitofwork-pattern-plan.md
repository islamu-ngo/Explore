# Plan: UnitOfWork Pattern — Enterprise-Grade for the Whole API

Last Updated: 2026-03-22

## Executive Summary

`IUnitOfWork` is a **transaction boundary coordinator**, not a replacement for EF Core's built-in change tracking. EF Core's `DbContext` is already the real Unit of Work. Our explicit abstraction exists solely to:

1. Force all multi-step writes into a single atomic transaction scope
2. Satisfy `NpgsqlRetryingExecutionStrategy`, which forbids user-initiated transactions outside `ExecuteAsync`
3. Keep post-commit side effects (cache, events, etc.) cleanly separated from database writes

The `ExecuteInTransactionAsync` lambda pattern is the correct and final API shape for this codebase. This plan documents the canonical pattern, all required hardening points (retry safety, nested guard, concurrency, testing, outbox), and a full handler audit checklist.

---

## Current State Analysis

### What is Correct (Do Not Change)

| Item | State | Notes |
|------|-------|-------|
| `IUnitOfWork.ExecuteInTransactionAsync` lambda API | ✅ Correct | Npgsql-compatible; avoids Begin/Commit/Rollback exposure |
| `EfCoreUnitOfWork` — `CreateExecutionStrategy().ExecuteAsync()` | ✅ Correct | Required wrapper for retry strategy |
| InMemory bypass in `EfCoreUnitOfWork` | ✅ Correct | Integration tests use InMemory; bypass is necessary |
| `IUnitOfWork` registered as `AddScoped<IUnitOfWork, EfCoreUnitOfWork>()` | ✅ Correct | Scoped to match `ExploreDbContext` lifetime |
| All repos share the same scoped `ExploreDbContext` | ✅ Correct | EF Core DI pattern; all changes are in the same context |
| `CompleteInstanceOnboardingCommandHandler` using UoW | ✅ Done | Reference implementation |

### What Needs Action

| Item | State | Action |
|------|-------|--------|
| Nested transaction guard in `EfCoreUnitOfWork` | ❌ Missing | Add `CurrentTransaction != null` detect + throw |
| Generic `<T>` overload on `IUnitOfWork` | ❌ Missing | Avoids closure-capture boilerplate |
| Handlers with multi-step writes NOT using UoW | ⚠️ Unaudited | Phase 1 audit |
| Retry/idempotency documentation | ❌ Missing | Critical — delegate may run more than once |
| Real Postgres transaction tests | ❌ Missing | InMemory does not validate rollback/constraints |

### Why Per-Operation `SaveChangesAsync` is Acceptable

Each of the 75+ repositories calls `SaveChangesAsync()` after each write. Within a transaction, these intermediate saves emit SQL within that transaction scope and are rolled back atomically if anything throws. There is no correctness issue with this approach. Switching to a fully deferred single `SaveChangesAsync()` at the end would require restructuring every repository — high cost, near-zero ROI. **Do not change this.**

---

## Canonical Pattern

### Interface (`Explore.Application/Contracts/Persistence/IUnitOfWork.cs`)

```csharp
namespace Explore.Application.Contracts.Persistence;

public interface IUnitOfWork
{
    /// <summary>
    /// Executes the operation inside a single database transaction, compatible with
    /// NpgsqlRetryingExecutionStrategy. The delegate must be idempotent — it may run
    /// more than once if the execution strategy retries on transient failures.
    /// </summary>
    Task ExecuteInTransactionAsync(Func<CancellationToken, Task> operation, CancellationToken ct = default);

    /// <summary>
    /// Variant that returns a value. Use this to avoid closure boilerplate when the
    /// transaction produces a result (e.g., a created entity's ID).
    /// </summary>
    Task<T> ExecuteInTransactionAsync<T>(Func<CancellationToken, Task<T>> operation, CancellationToken ct = default);
}
```

### Implementation (`Explore.Persistence/EfCoreUnitOfWork.cs`)

```csharp
// ABOUTME: EF Core implementation of IUnitOfWork using CreateExecutionStrategy for Npgsql retry compatibility.
// ABOUTME: Wraps the transaction and all operations inside the retrying strategy's ExecuteAsync scope.

using Explore.Application.Contracts.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Explore.Persistence;

public sealed class EfCoreUnitOfWork : IUnitOfWork
{
    private readonly ExploreDbContext _dbContext;

    public EfCoreUnitOfWork(ExploreDbContext dbContext) => _dbContext = dbContext;

    public Task ExecuteInTransactionAsync(Func<CancellationToken, Task> operation, CancellationToken ct = default)
        => ExecuteInTransactionAsync<object?>(async innerCt =>
        {
            await operation(innerCt);
            return null;
        }, ct);

    public async Task<T> ExecuteInTransactionAsync<T>(Func<CancellationToken, Task<T>> operation, CancellationToken ct = default)
    {
        // Nested transactions are not supported — detect and fail fast with a clear message
        if (_dbContext.Database.CurrentTransaction != null)
            throw new InvalidOperationException(
                "ExecuteInTransactionAsync cannot be called while a transaction is already active. " +
                "Nested transactions are not supported. Ensure only one UoW transaction scope per handler.");

        // InMemory provider does not support transactions or execution strategies — run directly
        if (_dbContext.Database.ProviderName == "Microsoft.EntityFrameworkCore.InMemory")
            return await operation(ct);

        var strategy = _dbContext.Database.CreateExecutionStrategy();
        return await strategy.ExecuteAsync(async () =>
        {
            await using var transaction = await _dbContext.Database.BeginTransactionAsync(ct);
            try
            {
                var result = await operation(ct);
                await transaction.CommitAsync(ct);
                return result;
            }
            catch
            {
                await transaction.RollbackAsync(ct);
                throw;
            }
        });
    }
}
```

---

## Rules for All Handlers

### 1. Transactional need = multi-step write workflow, not repository count

**Incorrect rule (retired):**
> Single-repo writes do NOT need UoW. Handlers that write to 2+ repos need UoW.

**Correct rule:**
> Any command handler performing a multi-step write workflow, multi-entity mutation, or all-or-nothing business operation must use `ExecuteInTransactionAsync`, **regardless of repository count**.

Examples that need UoW even with one repo:
- Looping over 5 settings keys in one settings repository
- Creating an entity and then updating it within the same handler
- Any sequence of writes where partial completion produces an invalid system state

### 2. The delegate must be retry-safe (idempotency)

`CreateExecutionStrategy().ExecuteAsync()` will **retry the entire delegate** on transient Postgres failures (connection drops, deadlocks, etc.). This means the lambda body may run more than once.

**Never place inside the delegate:**
- Email sends, SMS, push notifications
- HTTP calls to external services or webhooks
- Message broker publishes (Kafka, RabbitMQ, etc.)
- Anything with observable side effects outside the database

**Protect replay-sensitive writes with:**
- Database `UNIQUE` constraints (duplicate insert on retry → idempotent via constraint)
- Client-generated deterministic IDs (`Guid.NewGuid()` called **before** the lambda, captured via closure — not inside)
- Idempotent upsert patterns (`GetOrCreate` logic)

```csharp
// CORRECT — ID generated outside, before retry scope
var newId = Guid.NewGuid();
var result = await _unitOfWork.ExecuteInTransactionAsync(async ct =>
{
    return await _repo.Create(new Entity { Id = newId, ... });
}, cancellationToken);

// WRONG — ID generated inside; each retry produces a different ID
var result = await _unitOfWork.ExecuteInTransactionAsync(async ct =>
{
    return await _repo.Create(new Entity { Id = Guid.NewGuid(), ... }); // different each retry
}, cancellationToken);
```

### 3. Pre-validate before the lambda; post-commit side effects after

```csharp
// Step 1: Pre-validate — allows fast return without opening a transaction
var entity = await _repo.GetById(request.Id);
if (entity == null) return response.Fail("Not found");

// Step 2: Atomic writes
var createdId = await _unitOfWork.ExecuteInTransactionAsync(async ct =>
{
    var created = await _repoA.Create(...);
    await _repoB.Update(...);
    return created.Id;
}, cancellationToken);

// Step 3: Post-commit side effects (guaranteed DB has committed before these run)
_cache.Invalidate(createdId);
await _provider.ReloadAsync();
```

**Important caveat:** Pre-validation is an optimization for fast failure, not a concurrency guarantee. Business invariants that matter under concurrent access must be protected at the database level (unique constraints, FK constraints, row versioning, or in-transaction re-checks). A pre-check passes, another request modifies the data, then the transaction runs on stale assumptions (TOCTOU). The database is the last line of defense.

### 4. No parallel async inside the delegate

`ExploreDbContext` is not thread-safe. Do not use `Task.WhenAll(...)` or any parallel execution over repository calls inside the lambda.

```csharp
// WRONG
await _unitOfWork.ExecuteInTransactionAsync(async ct =>
{
    await Task.WhenAll(_repoA.Create(...), _repoB.Update(...)); // DbContext thread violation
}, ct);

// CORRECT — sequential writes
await _unitOfWork.ExecuteInTransactionAsync(async ct =>
{
    await _repoA.Create(...);
    await _repoB.Update(...);
}, ct);
```

### 5. Never nest `ExecuteInTransactionAsync`

`EfCoreUnitOfWork` detects an active transaction and throws immediately. This is by design. If you think you need nesting, the actual solution is to merge both operations into one lambda or extract shared logic into private methods that accept a `CancellationToken`.

### 6. Pass the lambda's `ct`, not the outer `CancellationToken`

The generic overload's lambda receives its own `CancellationToken` parameter. Use it inside the lambda.

### 7. Post-commit cross-process effects belong in the outbox (future)

Cache invalidation and in-process provider reloads are safe post-commit. For distributed effects (email, webhooks, integration events, bus publishes), the manual post-commit call pattern is not reliable:

```
DB commits → app crashes → email never sent → inconsistent state
```

For these cases, write the side-effect intent to an **outbox table** inside the same transaction, then have a background processor dispatch them. This is future-phase work; the immediate rule is: **never call external services inside the lambda.**

---

## Handler Audit Decision Tree

```
Is the handler a command (write operation)?
│
├─ No → no UoW needed
│
└─ Yes → Does the handler's success require multiple write operations
          that must ALL succeed or ALL fail together?
          │
          ├─ No → no UoW needed (single atomic write to one repo)
          │
          └─ Yes → Use ExecuteInTransactionAsync
                    │
                    └─ Can the delegate be retried safely?
                       ├─ Yes → done
                       └─ No → move non-idempotent work outside or to outbox
```

---

## Implementation Phases

### Phase 0 — Baseline (DONE)

- [x] `IUnitOfWork` → `ExecuteInTransactionAsync` lambda API
- [x] `EfCoreUnitOfWork` → `CreateExecutionStrategy().ExecuteAsync()` wrapper
- [x] InMemory bypass for integration tests
- [x] Registered as scoped in `PersistenceServicesRegistration`
- [x] `CompleteInstanceOnboardingCommandHandler` migrated

### Phase 1 — Harden `EfCoreUnitOfWork` (Required)

1. Add generic `Task<T>` overload (eliminates closure boilerplate)
2. Add nested transaction guard (`CurrentTransaction != null` → throw)
3. Refactor void overload to delegate to the generic overload (DRY)

### Phase 2 — Handler Audit (Required)

Identify all command handlers performing multi-step write workflows without `ExecuteInTransactionAsync`. See tasks file for the audit list.

### Phase 3 — Migrate Flagged Handlers (Required for Production Safety)

For each handler identified in Phase 2, apply the canonical pattern: pre-validate → lambda → post-commit.

### Phase 4 — Transactional Correctness Tests (Required)

Add Testcontainers-backed Postgres integration tests for:
- Rollback on mid-workflow failure (partial write leaves no trace)
- Unique constraint violation handled correctly (idempotent replay)
- Multi-step handler atomicity

### Phase 5 — Architecture Enforcement (Recommended)

**Short term:** Code review checklist for "multi-step writes use UoW."

**Long term:** Introduce `ITransactionalCommand<TResult>` marker interface + MediatR pipeline behavior that automatically wraps transactional commands:

```csharp
// Marker — command explicitly declares transactional intent
public interface ITransactionalCommand<TResult> : IRequest<TResult> { }

// Pipeline behavior — auto-wraps in transaction
public class TransactionPipelineBehavior<TRequest, TResponse>
    : IPipelineBehavior<TRequest, TResponse>
    where TRequest : ITransactionalCommand<TResponse>
{
    private readonly IUnitOfWork _unitOfWork;
    // ...
    public async Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken ct)
        => await _unitOfWork.ExecuteInTransactionAsync(innerCt => next(), ct);
}
```

> **Do not implement this pipeline immediately.** It inverts control in a way that removes visibility of exactly what is inside each transaction. The explicit per-handler pattern is safer and more debuggable at current team scale. Revisit when 10+ handlers consistently apply the same pattern.

### Phase 6 — Outbox Pattern (Future / When Cross-Process Side Effects Are Needed)

When reliable delivery of emails, webhooks, or integration events is required, implement:
1. `OutboxMessage` entity + EF configuration
2. Write outbox entries inside the transaction lambda alongside domain writes
3. `OutboxProcessor` background service polling and dispatching
4. Mark messages processed after delivery

This eliminates the "commit succeeded, side effect lost on crash" class of bugs.

---

## Risk Assessment

| Risk | Likelihood | Impact | Mitigation |
|------|-----------|--------|-----------|
| Handler does multi-step write without UoW → partial write on crash | Medium | High | Phase 2 audit + Phase 3 migration |
| Delegate generates non-deterministic ID on each retry → duplicate entities | Medium | High | Generate IDs before lambda; use DB unique constraints |
| External call inside delegate retried → duplicate email/webhook | Medium | High | Rule #2: no side effects inside lambda; outbox for cross-process |
| TOCTOU: pre-check passes but data changes before transaction | Low | Medium | DB unique/FK constraints are the real guard |
| Nested `ExecuteInTransactionAsync` → silent data correctness issue | Low | High | Explicit throw in `EfCoreUnitOfWork` (Phase 1) |
| Parallel writes inside lambda → DbContext thread violation | Low | Medium | Rule #4 in team docs + code review |

---

## Terminology Clarification

> `IUnitOfWork` in this codebase is a **transaction boundary coordinator**, not a true Unit of Work in the Evans/Fowler sense.
> EF Core's `DbContext` is the actual UoW (change tracking, identity map, etc.).
> Our `IUnitOfWork` wraps only the transaction lifecycle in a way that is compatible with `NpgsqlRetryingExecutionStrategy`.
> This distinction should be documented in code comments to prevent conceptual drift.

---

## Potential Risks & Unknowns

The highest-risk item is **idempotency under retry**: the execution strategy silently retries the lambda, and any non-database side effect or non-deterministic write (e.g., `Guid.NewGuid()` inside the lambda) will produce duplicate data or inconsistent state without any error. This is invisible during development because transient Postgres errors only appear under production load or network instability. The second risk is the **Phase 2 audit gap** — handlers with multi-step writes that were never wrapped are already in production paths; the only discovery mechanism before migration is a crash between writes. These two risks together make Phase 1 hardening and Phase 2 audit the highest-priority work.
