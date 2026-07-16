// ABOUTME: Appends and streams immutable facts from the retained PostgreSQL erasure authority.
// ABOUTME: Uses transactional database allocation, normalized UUIDv7 idempotency, and bounded ordered reads.

using Explore.Application.Contracts.LocationPrivacy;
using Explore.Domain;
using Explore.Domain.Enums;
using Microsoft.Extensions.Options;
using Npgsql;
using NpgsqlTypes;

namespace Explore.Infrastructure.Privacy.ErasureAuthority;

public sealed class PostgreSqlLocationPrivacyErasureAuthority :
    ILocationPrivacyErasureAuthority,
    IAsyncDisposable
{
    public const int MaximumReadBatchSize = 500;

    private readonly NpgsqlDataSource _dataSource;

    public PostgreSqlLocationPrivacyErasureAuthority(
        IOptions<LocationPrivacyErasureAuthorityOptions> options)
    {
        var connectionString = options.Value.ConnectionString?.Trim();
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            throw new InvalidOperationException(
                $"{LocationPrivacyErasureAuthorityOptions.SectionName}:ConnectionString is required.");
        }

        _dataSource = NpgsqlDataSource.Create(connectionString);
    }

    public async Task<LocationPrivacyErasureAuthorityIntent> AppendAsync(
        LocationPrivacyErasureIntent intent,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(intent);
        ValidateIntent(intent);

        await using var connection = await _dataSource.OpenConnectionAsync(cancellationToken);
        await using var append = connection.CreateCommand();
        append.CommandText =
            """
            SELECT authority_sequence, intent_id, owner_user_id, location_ids, reason,
                   requested_at_utc, recorded_at_utc
            FROM location_privacy_authority.append_erasure_intent(
                @intent_id, @owner_user_id, @location_ids, @reason)
            """;
        AddIntentParameters(append, intent);

        try
        {
            await using var reader = await append.ExecuteReaderAsync(cancellationToken);
            if (await reader.ReadAsync(cancellationToken))
            {
                return ReadFact(reader);
            }
        }
        catch (PostgresException exception)
            when (exception.SqlState == PostgresErrorCodes.InvalidParameterValue)
        {
            throw new InvalidOperationException(
                "The erasure authority rejected the append payload for this IntentId.");
        }

        throw new InvalidOperationException("The erasure-authority append did not return a retained fact.");
    }

    public async Task<IReadOnlyList<LocationPrivacyErasureAuthorityIntent>> ReadAfterAsync(
        long authoritySequence,
        int limit,
        CancellationToken cancellationToken = default)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(authoritySequence);
        if (limit is < 1 or > MaximumReadBatchSize)
        {
            throw new ArgumentOutOfRangeException(
                nameof(limit),
                limit,
                $"Read limit must be between 1 and {MaximumReadBatchSize}.");
        }

        await using var connection = await _dataSource.OpenConnectionAsync(cancellationToken);
        await using var command = connection.CreateCommand();
        command.CommandText =
            """
            SELECT authority_sequence, intent_id, owner_user_id, location_ids, reason,
                   requested_at_utc, recorded_at_utc
            FROM location_privacy_authority.read_erasure_intents_after(
                @authority_sequence, @limit)
            """;
        command.Parameters.AddWithValue("authority_sequence", NpgsqlDbType.Bigint, authoritySequence);
        command.Parameters.AddWithValue("limit", NpgsqlDbType.Integer, limit);

        var facts = new List<LocationPrivacyErasureAuthorityIntent>(limit);
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            facts.Add(ReadFact(reader));
        }

        return facts;
    }

    public ValueTask DisposeAsync() => _dataSource.DisposeAsync();

    private static void ValidateIntent(LocationPrivacyErasureIntent intent)
    {
        if (intent.IntentId == Guid.Empty ||
            intent.IntentId.Version != 7 ||
            intent.IntentId.Variant is < 8 or > 11)
        {
            throw new ArgumentException("IntentId must be an RFC 4122 UUIDv7 value.", nameof(intent));
        }

        if (intent.OwnerUserId == Guid.Empty)
        {
            throw new ArgumentException("OwnerUserId must be an opaque non-empty identifier.", nameof(intent));
        }

        ArgumentNullException.ThrowIfNull(intent.LocationIds);
        if (intent.LocationIds.Any(locationId => locationId == Guid.Empty))
        {
            throw new ArgumentException("LocationIds cannot contain an empty identifier.", nameof(intent));
        }

        if (!Enum.IsDefined(intent.Reason))
        {
            throw new ArgumentOutOfRangeException(nameof(intent), "Reason must be a defined erasure reason.");
        }
    }

    private static void AddIntentParameters(NpgsqlCommand command, LocationPrivacyErasureIntent intent)
    {
        command.Parameters.AddWithValue("intent_id", NpgsqlDbType.Uuid, intent.IntentId);
        command.Parameters.AddWithValue("owner_user_id", NpgsqlDbType.Uuid, intent.OwnerUserId);
        command.Parameters.AddWithValue(
            "location_ids",
            NpgsqlDbType.Array | NpgsqlDbType.Uuid,
            intent.LocationIds.Distinct().Order().ToArray());
        command.Parameters.AddWithValue("reason", NpgsqlDbType.Smallint, (short)intent.Reason);
    }

    private static LocationPrivacyErasureAuthorityIntent ReadFact(NpgsqlDataReader reader)
    {
        var requestedAtUtc = DateTime.SpecifyKind(reader.GetDateTime(5), DateTimeKind.Utc);
        var recordedAtUtc = DateTime.SpecifyKind(reader.GetDateTime(6), DateTimeKind.Utc);
        return LocationPrivacyErasureAuthorityIntent.Record(
            reader.GetGuid(1),
            reader.GetInt64(0),
            reader.GetGuid(2),
            reader.GetFieldValue<Guid[]>(3),
            (LocationPrivacyErasureReasonEnum)reader.GetInt16(4),
            requestedAtUtc,
            recordedAtUtc);
    }
}
