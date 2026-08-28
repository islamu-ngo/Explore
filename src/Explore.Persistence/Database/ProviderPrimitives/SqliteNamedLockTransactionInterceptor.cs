// ABOUTME: Holds SQLite process named locks until their caller-owned EF transaction completes.
// ABOUTME: Releases tracked semaphores on commit, rollback, failure, or connection cleanup.

using System.Collections.Concurrent;
using System.Data.Common;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace Explore.Persistence.Database;

internal sealed class SqliteNamedLockTransactionInterceptor
    : DbTransactionInterceptor, IDbConnectionInterceptor
{
    private readonly ConcurrentDictionary<DbTransaction, TransactionLocks> _locks = new();

    public static SqliteNamedLockTransactionInterceptor Instance { get; } = new();

    public bool IsTracked(DbTransaction transaction, string resource) =>
        _locks.TryGetValue(transaction, out TransactionLocks? locks)
        && locks.Contains(resource);

    public void Track(
        DbTransaction transaction,
        string resource,
        SemaphoreSlim semaphore)
    {
        TransactionLocks locks = _locks.GetOrAdd(
            transaction,
            static _ => new TransactionLocks());
        if (!locks.TryAdd(resource, semaphore))
        {
            throw new InvalidOperationException(
                $"SQLite named lock '{resource}' is already tracked by this transaction.");
        }
    }

    public override void TransactionCommitted(
        DbTransaction transaction,
        TransactionEndEventData eventData) => Release(transaction);

    public override void TransactionRolledBack(
        DbTransaction transaction,
        TransactionEndEventData eventData) => Release(transaction);

    public override void TransactionFailed(
        DbTransaction transaction,
        TransactionErrorEventData eventData) => Release(transaction);

    public override Task TransactionCommittedAsync(
        DbTransaction transaction,
        TransactionEndEventData eventData,
        CancellationToken cancellationToken = default)
    {
        Release(transaction);
        return Task.CompletedTask;
    }

    public override Task TransactionRolledBackAsync(
        DbTransaction transaction,
        TransactionEndEventData eventData,
        CancellationToken cancellationToken = default)
    {
        Release(transaction);
        return Task.CompletedTask;
    }

    public override Task TransactionFailedAsync(
        DbTransaction transaction,
        TransactionErrorEventData eventData,
        CancellationToken cancellationToken = default)
    {
        Release(transaction);
        return Task.CompletedTask;
    }

    public InterceptionResult ConnectionClosing(
        DbConnection connection,
        ConnectionEventData eventData,
        InterceptionResult result)
    {
        ReleaseTransactionsFor(connection);
        return result;
    }

    public ValueTask<InterceptionResult> ConnectionClosingAsync(
        DbConnection connection,
        ConnectionEventData eventData,
        InterceptionResult result)
    {
        ReleaseTransactionsFor(connection);
        return ValueTask.FromResult(result);
    }

    private void ReleaseTransactionsFor(DbConnection connection)
    {
        foreach (DbTransaction transaction in _locks.Keys.Where(candidate =>
                     ReferenceEquals(candidate.Connection, connection)))
        {
            Release(transaction);
        }
    }

    private void Release(DbTransaction transaction)
    {
        if (!_locks.TryRemove(transaction, out TransactionLocks? locks))
        {
            return;
        }

        foreach (SemaphoreSlim semaphore in locks.TakeInReverseOrder())
        {
            semaphore.Release();
        }
    }

    private sealed class TransactionLocks
    {
        private readonly object _gate = new();
        private readonly Dictionary<string, SemaphoreSlim> _semaphores =
            new(StringComparer.Ordinal);

        public bool Contains(string resource)
        {
            lock (_gate)
            {
                return _semaphores.ContainsKey(resource);
            }
        }

        public bool TryAdd(string resource, SemaphoreSlim semaphore)
        {
            lock (_gate)
            {
                return _semaphores.TryAdd(resource, semaphore);
            }
        }

        public SemaphoreSlim[] TakeInReverseOrder()
        {
            lock (_gate)
            {
                return _semaphores
                    .OrderByDescending(pair => pair.Key, StringComparer.Ordinal)
                    .Select(pair => pair.Value)
                    .ToArray();
            }
        }
    }
}
