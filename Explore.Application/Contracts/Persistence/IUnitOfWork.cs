// ABOUTME: Wraps a database transaction to allow multiple repository writes to commit atomically.
// ABOUTME: Uses the execution strategy pattern required by Npgsql's retrying strategy.

namespace Explore.Application.Contracts.Persistence;

public interface IUnitOfWork
{
    /// <summary>
    /// Executes the given operation inside a single database transaction.
    /// Handles begin, commit, and rollback — compatible with NpgsqlRetryingExecutionStrategy.
    /// </summary>
    Task ExecuteInTransactionAsync(Func<CancellationToken, Task> operation, CancellationToken ct = default);
}
