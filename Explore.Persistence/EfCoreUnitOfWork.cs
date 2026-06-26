// ABOUTME: EF Core implementation of IUnitOfWork using CreateExecutionStrategy for Npgsql retry compatibility.
// ABOUTME: Wraps the transaction and all operations inside the retrying strategy's ExecuteAsync scope.

using Explore.Application.Contracts.Persistence;
using Explore.Application.Exceptions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;

namespace Explore.Persistence;

public sealed class EfCoreUnitOfWork : IUnitOfWork
{
    private readonly ExploreDbContext _dbContext;

    public EfCoreUnitOfWork(ExploreDbContext dbContext) => _dbContext = dbContext;

    // Void overload delegates to generic — single execution path, no duplication
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
                "Nested transactions are not supported. Ensure only one UoW transaction scope per handler.");

        // InMemory provider does not support transactions or execution strategies — run directly
        if (_dbContext.Database.ProviderName == "Microsoft.EntityFrameworkCore.InMemory")
        {
            try
            {
                return await operation(ct);
            }
            catch (DbUpdateConcurrencyException ex)
            {
                throw TranslateConcurrencyException(ex);
            }
        }

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
            catch (DbUpdateConcurrencyException ex)
            {
                await RollbackBestEffortAsync(transaction, ct);
                throw TranslateConcurrencyException(ex);
            }
            catch
            {
                await RollbackBestEffortAsync(transaction, ct);
                throw;
            }
        });
    }

    private static async Task RollbackBestEffortAsync(IDbContextTransaction transaction, CancellationToken ct)
    {
        try
        {
            await transaction.RollbackAsync(ct);
        }
        catch (ObjectDisposedException)
        {
            // Preserve the original operation exception when EF/Npgsql already disposed the failed transaction.
        }
        catch (InvalidOperationException)
        {
            // Preserve the original operation exception when the transaction is no longer rollback-ready.
        }
    }

    private static ConcurrencyConflictException TranslateConcurrencyException(DbUpdateConcurrencyException ex)
    {
        var entry = ex.Entries.Count > 0 ? ex.Entries[0] : null;
        var entityType = entry?.Entity.GetType().Name;
        string? entityId = null;
        if (entry?.Metadata.FindPrimaryKey() is { } pk)
        {
            entityId = string.Join(":", pk.Properties.Select(p =>
                entry.Property(p.Name).CurrentValue?.ToString() ?? string.Empty));
        }

        return new ConcurrencyConflictException(
            ConcurrencyConflictException.ConcurrentUpdate,
            $"The {entityType ?? "entity"} was modified by another request. Reload and retry.",
            entityType,
            entityId,
            ex);
    }
}
