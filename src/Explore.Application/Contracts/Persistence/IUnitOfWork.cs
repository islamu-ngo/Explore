// ABOUTME: Transaction boundary coordinator — wraps multi-step write workflows in a single atomic scope.
// ABOUTME: Uses the execution strategy pattern required by NpgsqlRetryingExecutionStrategy; DbContext is the real UoW.

namespace Explore.Application.Contracts.Persistence;

public interface IUnitOfWork
{
    /// <summary>
    /// Executes the operation inside a single database transaction.
    /// Compatible with NpgsqlRetryingExecutionStrategy.
    /// The delegate MUST be idempotent — it may be retried on transient Postgres failures.
    /// Never place emails, HTTP calls, or broker publishes inside the delegate.
    /// </summary>
    Task ExecuteInTransactionAsync(Func<CancellationToken, Task> operation, CancellationToken ct = default);

    /// <summary>
    /// Returns-value variant. Prefer this over the void overload to avoid closure-capture boilerplate.
    /// </summary>
    Task<T> ExecuteInTransactionAsync<T>(Func<CancellationToken, Task<T>> operation, CancellationToken ct = default);
}
