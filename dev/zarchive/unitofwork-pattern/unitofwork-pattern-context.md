# Context: UnitOfWork Pattern

Last Updated: 2026-03-22

## Key Files

| File | Purpose |
|------|---------|
| `Explore.Application/Contracts/Persistence/IUnitOfWork.cs` | Contract — void + generic overloads |
| `Explore.Persistence/EfCoreUnitOfWork.cs` | Implementation — `CreateExecutionStrategy` wrapper + nested guard |
| `Explore.Persistence/PersistenceServicesRegistration.cs` | DI registration — `AddScoped<IUnitOfWork, EfCoreUnitOfWork>()` (line 76) |
| `Explore.Persistence/Repositories/GenericRepository.cs` | Base repo — each CRUD calls `SaveChangesAsync()` individually (intentional) |
| `Explore.Persistence/ExploreDbContext.cs` | Shared scoped context — all repos receive same instance via DI |
| `Explore.Application/Features/InstanceOnboarding/Handlers/Commands/CompleteInstanceOnboardingCommandHandler.cs` | Reference implementation of the full canonical UoW pattern |

## Core Interface Signatures

```csharp
// Explore.Application/Contracts/Persistence/IUnitOfWork.cs
namespace Explore.Application.Contracts.Persistence;

public interface IUnitOfWork
{
    /// <summary>
    /// Executes the operation inside a single database transaction.
    /// Compatible with NpgsqlRetryingExecutionStrategy.
    /// The delegate MUST be idempotent — it may be retried on transient failures.
    /// </summary>
    Task ExecuteInTransactionAsync(Func<CancellationToken, Task> operation, CancellationToken ct = default);

    /// <summary>
    /// Returns-value variant. Preferred over the void overload to avoid closure boilerplate.
    /// </summary>
    Task<T> ExecuteInTransactionAsync<T>(Func<CancellationToken, Task<T>> operation, CancellationToken ct = default);
}
```

## Final Implementation (Target State)

```csharp
// Explore.Persistence/EfCoreUnitOfWork.cs
public sealed class EfCoreUnitOfWork : IUnitOfWork
{
    private readonly ExploreDbContext _dbContext;
    public EfCoreUnitOfWork(ExploreDbContext dbContext) => _dbContext = dbContext;

    // Void overload delegates to generic — single implementation point
    public Task ExecuteInTransactionAsync(Func<CancellationToken, Task> operation, CancellationToken ct = default)
        => ExecuteInTransactionAsync<object?>(async innerCt =>
        {
            await operation(innerCt);
            return null;
        }, ct);

    public async Task<T> ExecuteInTransactionAsync<T>(Func<CancellationToken, Task<T>> operation, CancellationToken ct = default)
    {
        // Nested transaction guard — fail fast with a deterministic error
        if (_dbContext.Database.CurrentTransaction != null)
            throw new InvalidOperationException(
                "ExecuteInTransactionAsync cannot be called while a transaction is already active. " +
                "Nested transactions are not supported.");

        // InMemory bypass — transactions and execution strategies are not supported by InMemory provider
        if (_dbContext.Database.ProviderName == "Microsoft.EntityFrameworkCore.InMemory")
            return await operation(ct);

        var strategy = _dbContext.Database.CreateExecutionStrategy();
        return await strategy.ExecuteAsync(async () =>
        {
            await using var tx = await _dbContext.Database.BeginTransactionAsync(ct);
            try
            {
                var result = await operation(ct);
                await tx.CommitAsync(ct);
                return result;
            }
            catch
            {
                await tx.RollbackAsync(ct);
                throw;
            }
        });
    }
}
```

## DbContext Registration

```csharp
// PersistenceServicesRegistration.cs
services.AddPooledDbContextFactory<ExploreDbContext>(options =>
{
    options.UseNpgsql(connectionString, npgsqlOptions =>
    {
        npgsqlOptions.EnableRetryOnFailure(maxRetryCount: 3, maxRetryDelay: TimeSpan.FromSeconds(5), null);
        npgsqlOptions.CommandTimeout(30);
        npgsqlOptions.UseQuerySplittingBehavior(QuerySplittingBehavior.SplitQuery);
    }).UseSnakeCaseNamingConvention();
});

// Scoped ExploreDbContext shared by all repositories in a single request
services.AddScoped(sp => sp.GetRequiredService<IDbContextFactory<ExploreDbContext>>().CreateDbContext());

// Transaction boundary coordinator
services.AddScoped<IUnitOfWork, EfCoreUnitOfWork>();
```

## Critical Design Decisions

### Why Lambda API (not Begin/Commit/Rollback)

`NpgsqlRetryingExecutionStrategy` requires that `BeginTransaction` itself be inside `strategy.ExecuteAsync()`. The lambda pattern enforces this structurally. Exposing `BeginTransaction` directly on the interface makes it possible (and easy) to call it outside the strategy scope, which throws at runtime:

> `NpgsqlRetryingExecutionStrategy does not support user-initiated transactions.`

### Why No Deferred SaveChanges

All 75+ repositories call `SaveChangesAsync()` after each write. Within a transaction these are intermediate flushes, all rolled back atomically on failure. Changing to deferred would require restructuring every repository — not worth it.

### Why Generic Overload

Closure-based result capture works but is verbose and error-prone at scale:

```csharp
// Without generic overload — closure boilerplate
Guid? createdId = null;
await _unitOfWork.ExecuteInTransactionAsync(async ct =>
{
    var e = await _repo.Create(entity);
    createdId = e.Id; // easy to forget
}, ct);
// createdId might still be null if an error path was hit

// With generic overload — clean and type-safe
var createdId = await _unitOfWork.ExecuteInTransactionAsync(async ct =>
{
    var e = await _repo.Create(entity);
    return e.Id;
}, ct);
```

### Why Nested Transaction Guard Throws

Nested transactions are a common accidental introduction during refactoring (e.g., a service calls a method that internally uses UoW, and the caller also uses UoW). Without a guard, the behavior is undefined. An explicit `InvalidOperationException` at the point of nesting is better than silent correctness bugs.

Nest detection: `_dbContext.Database.CurrentTransaction != null`

### Why `ITransactionalCommand<T>` is Future Work

A MediatR pipeline behavior that auto-wraps transactional commands is appealing but removes explicit visibility of what is inside each transaction. At current team scale, the per-handler explicit pattern is more debuggable. Revisit when 10+ handlers apply the same pattern.

## Retry-Safety Contract

Because `strategy.ExecuteAsync()` may retry the entire delegate on transient Postgres errors:

| Placement | Rule |
|-----------|------|
| `Guid.NewGuid()`, random seeds, timestamps used as IDs | **Before** the lambda (captured via closure) |
| Email, webhook, HTTP call to external service | **Never** inside the lambda |
| Message broker publish | **Never** inside the lambda — use outbox |
| In-process cache invalidation | **After** `ExecuteInTransactionAsync` returns |
| `SaveChangesAsync` calls in repos | ✅ Fine — EF Core handles idempotency at write level |

## Concurrency / TOCTOU Note

Pre-validation outside the transaction is for fast rejection. It is not a concurrency guarantee. Another request can modify or delete the data between the pre-check and when the transaction opens. Critical invariants must be enforced by:

- Database `UNIQUE` constraints
- Foreign key constraints
- Row version / concurrency tokens (`[ConcurrencyCheck]` / `IsRowVersion()`)
- In-transaction re-checks for high-contention scenarios

## Reference Implementation

`CompleteInstanceOnboardingCommandHandler` — full canonical example:

```csharp
// Pre-validate BEFORE transaction — allows early return
var existingUser = await _userRepository.GetById(request.UserId);
if (existingUser == null && string.IsNullOrWhiteSpace(request.Email))
    return response.Fail("User identity data is required.");

// IDs and values needed inside lambda — generated BEFORE, captured via closure
var deploymentMode = request.Settings.DeploymentMode;
Guid? defaultTenantId = null;

// Atomic writes — all or nothing
await _unitOfWork.ExecuteInTransactionAsync(async ct =>
{
    if (isSingleTenant)
    {
        var tenant = await EnsureDefaultTenantAsync();
        defaultTenantId = tenant.Id;
    }
    var user = existingUser ?? await CreateOnboardingUserAsync(request, defaultTenantId);
    await PersistDeploymentModeSettingAsync(deploymentMode);
    // ... more writes ...
}, cancellationToken);

// Post-commit side effects — DB has committed before this line
_adminCacheInvalidator.InvalidateUser(request.UserId);
await _deploymentModeProvider.InvalidateCacheAsync();
_setupSecretProvider.Lock();
```

## Unit Test Mock Pattern (NSubstitute)

```csharp
// Void overload — makes the mock execute the lambda so inner repo logic runs in tests
_unitOfWork
    .ExecuteInTransactionAsync(Arg.Any<Func<CancellationToken, Task>>(), Arg.Any<CancellationToken>())
    .Returns(callInfo =>
    {
        var op = callInfo.Arg<Func<CancellationToken, Task>>();
        return op(CancellationToken.None);
    });

// Generic overload mock (Guid example — use the actual return type)
_unitOfWork
    .ExecuteInTransactionAsync(Arg.Any<Func<CancellationToken, Task<Guid>>>(), Arg.Any<CancellationToken>())
    .Returns(callInfo =>
    {
        var op = callInfo.Arg<Func<CancellationToken, Task<Guid>>>();
        return op(CancellationToken.None);
    });
```

## Dependencies

- **EF Core 10** — `CreateExecutionStrategy()`, `Database.CurrentTransaction`
- **Npgsql.EntityFrameworkCore.PostgreSQL** — `NpgsqlRetryingExecutionStrategy`
- **MediatR** — handler pattern consuming `IUnitOfWork`
- **Testcontainers.PostgreSql** — Postgres-backed transactional correctness tests (Phase 4)
- **xUnit + InMemory** — fast handler-flow unit/integration tests (InMemory bypass in `EfCoreUnitOfWork`)
