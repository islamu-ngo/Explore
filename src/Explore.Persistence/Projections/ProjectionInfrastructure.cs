// ABOUTME: Shared infrastructure for custom-property projection updaters: advisory locks, FNV hashing, and chunking.
// ABOUTME: Used by both Event and EventSession updaters to avoid duplicating pure-infrastructure plumbing.

using System.Data;
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
