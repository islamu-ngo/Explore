// ABOUTME: Serializes each ATProto OAuth refresh across application instances with a PostgreSQL session lock.
// ABOUTME: Holds the scoped DbContext connection without a retryable transaction while remote rotation runs.

using System.Buffers.Binary;
using System.Data;
using System.Data.Common;
using System.Security.Cryptography;
using System.Text;
using Explore.Application.Contracts.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Explore.Persistence;

public sealed class PostgresAtprotoSessionRefreshLock(ExploreDbContext dbContext)
    : IAtprotoSessionRefreshLock
{
    public async Task<IAsyncDisposable> AcquireAsync(
        Guid tenantId,
        Guid userId,
        string provider,
        string subjectDid,
        CancellationToken cancellationToken = default)
    {
        if (tenantId == Guid.Empty || userId == Guid.Empty)
        {
            throw new ArgumentException("ATProto refresh scope is invalid.");
        }

        ArgumentException.ThrowIfNullOrWhiteSpace(provider);
        ArgumentException.ThrowIfNullOrWhiteSpace(subjectDid);
        if (dbContext.Database.ProviderName != "Npgsql.EntityFrameworkCore.PostgreSQL")
        {
            return NoopLease.Instance;
        }

        DbConnection connection = dbContext.Database.GetDbConnection();
        bool ownsConnection = connection.State != ConnectionState.Open;
        if (ownsConnection)
        {
            await connection.OpenAsync(cancellationToken).ConfigureAwait(false);
        }

        long key = ComputeStableLockKey(tenantId, userId, provider, subjectDid);
        try
        {
            await ExecuteAsync(connection, "SELECT pg_advisory_lock(@key)", key, cancellationToken)
                .ConfigureAwait(false);
            return new PostgresLease(connection, key, ownsConnection);
        }
        catch
        {
            if (ownsConnection)
            {
                await connection.CloseAsync().ConfigureAwait(false);
            }

            throw;
        }
    }

    internal static long ComputeStableLockKey(
        Guid tenantId,
        Guid userId,
        string provider,
        string subjectDid)
    {
        string scope = string.Create(
            System.Globalization.CultureInfo.InvariantCulture,
            $"explore:atproto-refresh:{tenantId:D}:{userId:D}:{provider}:{subjectDid}");
        byte[] hash = SHA256.HashData(Encoding.UTF8.GetBytes(scope));
        return BinaryPrimitives.ReadInt64BigEndian(hash);
    }

    private static async Task ExecuteAsync(
        DbConnection connection,
        string sql,
        long key,
        CancellationToken cancellationToken)
    {
        await using DbCommand command = connection.CreateCommand();
        command.CommandText = sql;
        DbParameter parameter = command.CreateParameter();
        parameter.ParameterName = "key";
        parameter.Value = key;
        command.Parameters.Add(parameter);
        await command.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
    }

    private sealed class PostgresLease(DbConnection connection, long key, bool ownsConnection)
        : IAsyncDisposable
    {
        private int _disposed;

        public async ValueTask DisposeAsync()
        {
            if (Interlocked.Exchange(ref _disposed, 1) != 0)
            {
                return;
            }

            try
            {
                using var unlockTimeout = new CancellationTokenSource(TimeSpan.FromSeconds(5));
                await ExecuteAsync(
                    connection,
                    "SELECT pg_advisory_unlock(@key)",
                    key,
                    unlockTimeout.Token).ConfigureAwait(false);
            }
            catch
            {
                await connection.CloseAsync().ConfigureAwait(false);
                throw;
            }
            finally
            {
                if (ownsConnection && connection.State != ConnectionState.Closed)
                {
                    await connection.CloseAsync().ConfigureAwait(false);
                }
            }
        }
    }

    private sealed class NoopLease : IAsyncDisposable
    {
        public static NoopLease Instance { get; } = new();

        public ValueTask DisposeAsync() => ValueTask.CompletedTask;
    }
}
