// ABOUTME: EF Core implementation of IUnitOfWork using CreateExecutionStrategy for Npgsql retry compatibility.
// ABOUTME: Clears failed-attempt tracking before retry and preserves original errors during rollback cleanup.

using System.Data;
using System.Data.Common;
using Explore.Application.Contracts.Persistence;
using Explore.Application.Exceptions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;

namespace Explore.Persistence;

public sealed class EfCoreUnitOfWork : IUnitOfWork
{
    private readonly ExploreDbContext _dbContext;
    private readonly Func<IExecutionStrategy> _createExecutionStrategy;
    private readonly Func<CancellationToken, Task<IDbContextTransaction>> _beginTransaction;

    public EfCoreUnitOfWork(ExploreDbContext dbContext)
        : this(
            dbContext,
            dbContext.Database.CreateExecutionStrategy,
            dbContext.Database.BeginTransactionAsync)
    {
    }

    internal EfCoreUnitOfWork(
        ExploreDbContext dbContext,
        Func<IExecutionStrategy> createExecutionStrategy,
        Func<CancellationToken, Task<IDbContextTransaction>> beginTransaction)
    {
        _dbContext = dbContext;
        _createExecutionStrategy = createExecutionStrategy;
        _beginTransaction = beginTransaction;
    }

    // Void overload delegates to generic — single execution path, no duplication
    public Task ExecuteInTransactionAsync(Func<CancellationToken, Task> operation, CancellationToken ct = default)
        => ExecuteInTransactionAsync<object?>(async innerCt =>
        {
            await operation(innerCt);
            return null;
        }, ct);

    public async Task<T> ExecuteInTransactionAsync<T>(Func<CancellationToken, Task<T>> operation, CancellationToken ct = default)
    {
        return await ExecuteCoreAsync(operation, isolationLevel: null, ct);
    }

    public async Task<T> ExecuteSerializableAsync<T>(
        Func<CancellationToken, Task<T>> operation,
        CancellationToken ct = default)
    {
        return await ExecuteCoreAsync(operation, IsolationLevel.Serializable, ct);
    }

    private async Task<T> ExecuteCoreAsync<T>(
        Func<CancellationToken, Task<T>> operation,
        IsolationLevel? isolationLevel,
        CancellationToken ct)
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

        var strategy = _createExecutionStrategy();
        return await strategy.ExecuteAsync(async () =>
        {
            await using var transaction = isolationLevel.HasValue
                ? await _dbContext.Database.BeginTransactionAsync(isolationLevel.Value, ct)
                : await _beginTransaction(ct);
            try
            {
                var result = await operation(ct);
                await transaction.CommitAsync(ct);
                return result;
            }
            catch (DbUpdateConcurrencyException ex)
            {
                var translatedException = TranslateConcurrencyException(ex);
                await RollbackAndClearTrackingAsync(transaction);
                throw translatedException;
            }
            catch
            {
                await RollbackAndClearTrackingAsync(transaction);
                throw;
            }
        });
    }

    private async Task RollbackAndClearTrackingAsync(IDbContextTransaction transaction)
    {
        try
        {
            await RollbackBestEffortAsync(transaction);
        }
        finally
        {
            _dbContext.ChangeTracker.Clear();
        }
    }

    private static async Task RollbackBestEffortAsync(IDbContextTransaction transaction)
    {
        try
        {
            await transaction.RollbackAsync(CancellationToken.None);
        }
        catch (ObjectDisposedException)
        {
            // Preserve the original operation exception when EF/Npgsql already disposed the failed transaction.
        }
        catch (InvalidOperationException)
        {
            // Preserve the original operation exception when the transaction is no longer rollback-ready.
        }
        catch (DbException)
        {
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
