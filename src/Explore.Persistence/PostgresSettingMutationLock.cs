// ABOUTME: PostgreSQL implementation of per-setting transaction-scoped mutation locking.
// ABOUTME: Uses stable advisory keys and reuses an active unit-of-work transaction when present.

namespace Explore.Persistence;

using System.Buffers.Binary;
using System.Data;
using System.Data.Common;
using System.Security.Cryptography;
using System.Text;
using Explore.Application.Contracts.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;

public sealed class PostgresSettingMutationLock(
    ExploreDbContext dbContext,
    IUnitOfWork unitOfWork) : ISettingMutationLock
{
    public Task<T> ExecuteAsync<T>(
        string canonicalSettingKey,
        Func<CancellationToken, Task<T>> operation,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(canonicalSettingKey);

        return ExecuteManyAsync([canonicalSettingKey], operation, cancellationToken);
    }

    public Task<T> ExecuteManyAsync<T>(
        IEnumerable<string> canonicalSettingKeys,
        Func<CancellationToken, Task<T>> operation,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(canonicalSettingKeys);
        string[] orderedKeys = NormalizeCanonicalKeys(canonicalSettingKeys);
        if (orderedKeys.Length == 0)
        {
            throw new ArgumentException("At least one canonical setting key is required.", nameof(canonicalSettingKeys));
        }

        return dbContext.Database.CurrentTransaction is not null
            ? ExecuteInsideTransactionAsync(orderedKeys, operation, cancellationToken)
            : unitOfWork.ExecuteInTransactionAsync(
                token => ExecuteInsideTransactionAsync(orderedKeys, operation, token),
                cancellationToken);
    }

    private async Task<T> ExecuteInsideTransactionAsync<T>(
        IReadOnlyList<string> canonicalSettingKeys,
        Func<CancellationToken, Task<T>> operation,
        CancellationToken cancellationToken)
    {
        if (dbContext.Database.ProviderName == "Npgsql.EntityFrameworkCore.PostgreSQL")
        {
            foreach (string canonicalSettingKey in canonicalSettingKeys)
            {
                await AcquirePostgresLockAsync(canonicalSettingKey, cancellationToken);
            }
        }

        return await operation(cancellationToken);
    }

    private async Task AcquirePostgresLockAsync(string canonicalSettingKey, CancellationToken cancellationToken)
    {
        DbConnection connection = dbContext.Database.GetDbConnection();
        if (connection.State != ConnectionState.Open)
        {
            await connection.OpenAsync(cancellationToken);
        }

        await using DbCommand command = connection.CreateCommand();
        command.CommandText = "SELECT pg_advisory_xact_lock(@key)";
        command.Transaction = dbContext.Database.CurrentTransaction?.GetDbTransaction()
            ?? throw new InvalidOperationException("A PostgreSQL setting mutation lock requires an active transaction.");

        DbParameter parameter = command.CreateParameter();
        parameter.ParameterName = "key";
        parameter.Value = ComputeStableLockKey(canonicalSettingKey);
        command.Parameters.Add(parameter);

        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    internal static long ComputeStableLockKey(string canonicalSettingKey)
    {
        string normalized = canonicalSettingKey.Trim().ToLowerInvariant();
        byte[] bytes = Encoding.UTF8.GetBytes($"explore:setting-mutation:{normalized}");
        byte[] hash = SHA256.HashData(bytes);
        return BinaryPrimitives.ReadInt64BigEndian(hash);
    }

    internal static string[] NormalizeCanonicalKeys(IEnumerable<string> canonicalSettingKeys)
    {
        return canonicalSettingKeys
            .Select(key =>
            {
                ArgumentException.ThrowIfNullOrWhiteSpace(key);
                return key.Trim().ToLowerInvariant();
            })
            .Distinct(StringComparer.Ordinal)
            .OrderBy(key => key, StringComparer.Ordinal)
            .ToArray();
    }
}
