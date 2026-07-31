// ABOUTME: Calls the retained authority's fixed SECURITY DEFINER append and read functions through its DbContext connection.
// ABOUTME: Preserves the runtime role's function-only boundary and never performs ordinary DbSet table access.

using Explore.Application.Configuration;
using Explore.Application.Contracts.PrivacyErasure;
using Explore.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Npgsql;
using NpgsqlTypes;

namespace Explore.Persistence.Privacy.ErasureAuthority.Repositories;

public sealed class EfCorePrivacyErasureAuthorityRepository(
    PrivacyErasureAuthorityDbContext dbContext,
    IOptions<PrivacyErasureOptions> options) : IPrivacyErasureAuthority
{
    public const int MaximumReadBatchSize = 500;

    public async Task<PrivacyErasureIntent> AppendAsync(
        PrivacyErasureRequest intent,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(intent);
        await dbContext.Database.OpenConnectionAsync(cancellationToken);
        try
        {
            await using NpgsqlCommand command = CreateCommand(
                $"SELECT authority_sequence, intent_id, subject_kind, subject_id, reason_code, policy_version, requested_at_utc, recorded_at_utc, retention_expires_at_utc FROM {PrivacyErasureAuthorityDatabaseContract.AppendFunctionSql}(@intent_id, @subject_kind, @subject_id, @reason_code, @policy_version, @authority_retention)");
            command.Parameters.AddWithValue("intent_id", NpgsqlDbType.Uuid, intent.IntentId);
            command.Parameters.AddWithValue("subject_kind", NpgsqlDbType.Smallint, (short)intent.SubjectKind);
            command.Parameters.AddWithValue("subject_id", NpgsqlDbType.Uuid, intent.SubjectId);
            command.Parameters.AddWithValue("reason_code", NpgsqlDbType.Smallint, (short)intent.ReasonCode);
            command.Parameters.AddWithValue("policy_version", NpgsqlDbType.Integer, intent.PolicyVersion);
            command.Parameters.AddWithValue("authority_retention", NpgsqlDbType.Interval, options.Value.AuthorityRetention);
            try
            {
                await using NpgsqlDataReader reader = await command.ExecuteReaderAsync(cancellationToken);
                if (await reader.ReadAsync(cancellationToken))
                {
                    return ReadFact(reader);
                }
            }
            catch (PostgresException exception) when (exception.SqlState == PostgresErrorCodes.InvalidParameterValue)
            {
                throw new InvalidOperationException("The erasure authority rejected the append payload for this IntentId.");
            }

            throw new InvalidOperationException("The erasure-authority append did not return a retained fact.");
        }
        finally
        {
            await dbContext.Database.CloseConnectionAsync();
        }
    }

    public async Task<IReadOnlyList<PrivacyErasureIntent>> ReadAfterAsync(
        long authoritySequence,
        int limit,
        CancellationToken cancellationToken = default)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(authoritySequence);
        if (limit is < 1 or > MaximumReadBatchSize)
        {
            throw new ArgumentOutOfRangeException(nameof(limit));
        }

        await dbContext.Database.OpenConnectionAsync(cancellationToken);
        try
        {
            await using NpgsqlCommand command = CreateCommand(
                $"SELECT authority_sequence, intent_id, subject_kind, subject_id, reason_code, policy_version, requested_at_utc, recorded_at_utc, retention_expires_at_utc FROM {PrivacyErasureAuthorityDatabaseContract.ReadFunctionSql}(@authority_sequence, @limit)");
            command.Parameters.AddWithValue("authority_sequence", NpgsqlDbType.Bigint, authoritySequence);
            command.Parameters.AddWithValue("limit", NpgsqlDbType.Integer, limit);
            var facts = new List<PrivacyErasureIntent>(limit);
            await using NpgsqlDataReader reader = await command.ExecuteReaderAsync(cancellationToken);
            while (await reader.ReadAsync(cancellationToken))
            {
                facts.Add(ReadFact(reader));
            }

            return facts;
        }
        finally
        {
            await dbContext.Database.CloseConnectionAsync();
        }
    }

    private NpgsqlCommand CreateCommand(string sql)
    {
        var connection = (NpgsqlConnection)dbContext.Database.GetDbConnection();
        return new NpgsqlCommand(sql, connection);
    }

    private static PrivacyErasureIntent ReadFact(NpgsqlDataReader reader) =>
        PrivacyErasureIntent.Record(
            reader.GetGuid(1),
            reader.GetInt64(0),
            (PrivacyErasureSubjectKind)reader.GetInt16(2),
            reader.GetGuid(3),
            (PrivacyErasureReasonCode)reader.GetInt16(4),
            reader.GetInt32(5),
            DateTime.SpecifyKind(reader.GetDateTime(6), DateTimeKind.Utc),
            DateTime.SpecifyKind(reader.GetDateTime(7), DateTimeKind.Utc),
            DateTime.SpecifyKind(reader.GetDateTime(8), DateTimeKind.Utc));
}
