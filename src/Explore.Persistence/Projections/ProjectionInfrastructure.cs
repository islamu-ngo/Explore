// ABOUTME: Shared infrastructure for custom-property projection updaters: advisory locks, FNV hashing, and chunking.
// ABOUTME: Used by both Event and EventSession updaters to avoid duplicating pure-infrastructure plumbing.

using System.Data;
using System.Data.Common;
using System.Globalization;
using Explore.Persistence.Database;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;

namespace Explore.Persistence.Projections;

internal static class ProjectionInfrastructure
{
    internal static async Task<bool> TryAcquireAdvisoryLockAsync(
        ExploreDbContext dbContext,
        int projectionLockKey,
        Guid tenantId,
        bool exclusive,
        CancellationToken cancellationToken)
    {
        string providerName = dbContext.Database.ProviderName
            ?? throw new InvalidOperationException("Projection locks require a configured relational provider.");

        return providerName switch
        {
            RelationalNamedLock.PostgreSqlProvider => await TryAcquirePostgreSqlLockAsync(
                dbContext,
                projectionLockKey,
                tenantId,
                exclusive,
                cancellationToken),
            RelationalNamedLock.SqlServerProvider or RelationalNamedLock.MySqlProvider =>
                await TryAcquireServerLockAsync(
                    dbContext,
                    providerName,
                    projectionLockKey,
                    tenantId,
                    exclusive,
                    cancellationToken),
            RelationalNamedLock.SqliteProvider => await TryAcquireSqliteLockAsync(
                dbContext,
                projectionLockKey,
                tenantId,
                exclusive,
                cancellationToken),
            _ => throw new InvalidOperationException(
                $"Unsupported projection lock provider '{providerName}'."),
        };
    }

    private static async Task<bool> TryAcquirePostgreSqlLockAsync(
        ExploreDbContext dbContext,
        int projectionLockKey,
        Guid tenantId,
        bool exclusive,
        CancellationToken cancellationToken)
    {
        var tenantKey = ComputeStableKey(tenantId.ToString("N"));
        var sql = exclusive
            ? "SELECT pg_try_advisory_xact_lock(@key1, @key2)"
            : "SELECT pg_try_advisory_xact_lock_shared(@key1, @key2)";

        var connection = dbContext.Database.GetDbConnection();
        if (connection.State != ConnectionState.Open)
        {
            await connection.OpenAsync(cancellationToken);
        }

        await using var command = connection.CreateCommand();
        command.CommandText = sql;
        if (dbContext.Database.CurrentTransaction is { } currentTransaction)
        {
            command.Transaction = currentTransaction.GetDbTransaction();
        }

        var key1 = command.CreateParameter();
        key1.ParameterName = "@key1";
        key1.Value = projectionLockKey;
        command.Parameters.Add(key1);

        var key2 = command.CreateParameter();
        key2.ParameterName = "@key2";
        key2.Value = tenantKey;
        command.Parameters.Add(key2);

        var result = await command.ExecuteScalarAsync(cancellationToken);
        return result is bool acquired && acquired;
    }

    private static async Task<bool> TryAcquireServerLockAsync(
        ExploreDbContext dbContext,
        string providerName,
        int projectionLockKey,
        Guid tenantId,
        bool exclusive,
        CancellationToken cancellationToken)
    {
        if (exclusive && dbContext.Database.CurrentTransaction is null)
        {
            throw new InvalidOperationException("Exclusive projection locks require an active transaction.");
        }

        IDbContextTransaction? localTransaction = null;
        if (dbContext.Database.CurrentTransaction is null)
        {
            localTransaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);
        }

        try
        {
            DbTransaction transaction = dbContext.Database.CurrentTransaction!.GetDbTransaction();
            string resource = RelationalNamedLock.NormalizeProviderResource(
                providerName,
                $"custom-property-projection:{projectionLockKey}:{tenantId:N}");
            await using DbCommand command = CreateServerTryAcquireCommand(
                dbContext.Database.GetDbConnection(),
                transaction,
                providerName,
                resource,
                exclusive);
            object? result = await command.ExecuteScalarAsync(cancellationToken);
            int resultCode = result is null or DBNull
                ? int.MinValue
                : Convert.ToInt32(result, CultureInfo.InvariantCulture);
            bool acquired = providerName == RelationalNamedLock.SqlServerProvider
                ? resultCode >= 0
                : resultCode == 1;

            if (acquired && providerName == RelationalNamedLock.MySqlProvider)
            {
                MySqlNamedLockTransactionInterceptor.Instance.Track(transaction, resource);
            }

            if (localTransaction is not null)
            {
                await localTransaction.CommitAsync(cancellationToken);
            }

            return acquired;
        }
        finally
        {
            if (localTransaction is not null)
            {
                await localTransaction.DisposeAsync();
            }
        }
    }

    internal static DbCommand CreateServerTryAcquireCommand(
        DbConnection connection,
        DbTransaction? transaction,
        string providerName,
        string resource,
        bool exclusive)
    {
        DbCommand command = connection.CreateCommand();
        command.Transaction = transaction;
        command.CommandText = providerName switch
        {
            RelationalNamedLock.SqlServerProvider =>
                "DECLARE @result int; EXEC @result = sys.sp_getapplock "
                + "@Resource = @resource, @LockMode = @lockMode, "
                + "@LockOwner = 'Transaction', @LockTimeout = 0; SELECT @result;",
            RelationalNamedLock.MySqlProvider => "SELECT GET_LOCK(@resource, 0)",
            _ => throw new InvalidOperationException(
                $"Unsupported server projection lock provider '{providerName}'."),
        };

        AddParameter(command, "resource", resource);
        if (providerName == RelationalNamedLock.SqlServerProvider)
        {
            AddParameter(command, "lockMode", exclusive ? "Exclusive" : "Shared");
        }

        return command;
    }

    private static Task<bool> TryAcquireSqliteLockAsync(
        ExploreDbContext dbContext,
        int projectionLockKey,
        Guid tenantId,
        bool exclusive,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (exclusive && dbContext.Database.CurrentTransaction is null)
        {
            throw new InvalidOperationException("Exclusive projection locks require an active transaction.");
        }

        string resource = $"custom-property-projection:{projectionLockKey}:{tenantId:N}";
        if (dbContext.Database.CurrentTransaction is { } currentTransaction)
        {
            return Task.FromResult(SqliteProjectionLockTransactionInterceptor.Instance.TryAcquire(
                currentTransaction.GetDbTransaction(),
                resource));
        }

        return Task.FromResult(SqliteProjectionLockTransactionInterceptor.Instance.TryProbe(resource));
    }

    private static void AddParameter(DbCommand command, string name, object value)
    {
        DbParameter parameter = command.CreateParameter();
        parameter.ParameterName = name;
        parameter.Value = value;
        command.Parameters.Add(parameter);
    }

    internal static int ComputeStableKey(string value)
    {
        unchecked
        {
            const int fnvOffsetBasis = unchecked((int)2166136261);
            const int fnvPrime = 16777619;
            var hash = fnvOffsetBasis;
            foreach (var c in value)
            {
                hash ^= c;
                hash *= fnvPrime;
            }
            return hash;
        }
    }

    internal static IEnumerable<IReadOnlyList<T>> Chunk<T>(IReadOnlyList<T> source, int size)
    {
        if (size <= 0)
        {
            yield return source;
            yield break;
        }

        for (var i = 0; i < source.Count; i += size)
        {
            var end = Math.Min(i + size, source.Count);
            var chunk = new List<T>(end - i);
            for (var j = i; j < end; j++)
            {
                chunk.Add(source[j]);
            }
            yield return chunk;
        }
    }
}
