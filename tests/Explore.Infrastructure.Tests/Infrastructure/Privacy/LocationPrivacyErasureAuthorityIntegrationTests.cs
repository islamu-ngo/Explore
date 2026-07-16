// ABOUTME: PostgreSQL integration tests for the independently retained location-erasure authority.
// ABOUTME: Proves idempotency, monotonic replay, PII-free append-only storage, and app-restore isolation.

using Explore.Application.Contracts.LocationPrivacy;
using Explore.Domain;
using Explore.Domain.Enums;
using Explore.Infrastructure.Privacy.ErasureAuthority;
using Explore.Infrastructure.Tests.Fixtures;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Npgsql;
using Testcontainers.PostgreSql;
using TUnit.Core.Interfaces;

namespace Explore.Infrastructure.Tests.Infrastructure.Privacy;

[Category(InfrastructureTestCategories.Runtime)]
[NotInParallel("LocationPrivacyErasureAuthorityPostgreSql")]
public sealed class LocationPrivacyErasureAuthorityIntegrationTests : IAsyncInitializer, IAsyncDisposable
{
    private const string RuntimeUsername = "location_privacy_runtime_test";
    private const string RuntimePassword = "location-privacy-runtime-test";
    private const string ApplicationDatabaseName = "event_application_restore_tests";
    private const long BlockingInsertAdvisoryLockKey = 7_160_525;

    private readonly PostgreSqlContainer _authorityDatabase = new PostgreSqlBuilder("postgres:18-alpine")
        .WithDatabase("event_location_privacy_authority_tests")
        .WithUsername("postgres")
        .WithPassword("postgres")
        .Build();

    private readonly PostgreSqlContainer _applicationDatabase = new PostgreSqlBuilder("postgres:18-alpine")
        .WithDatabase(ApplicationDatabaseName)
        .WithUsername("postgres")
        .WithPassword("postgres")
        .Build();

    private PostgreSqlLocationPrivacyErasureAuthority? _client;
    private string _runtimeConnectionString = string.Empty;

    public async Task InitializeAsync()
    {
        await Task.WhenAll(
            _authorityDatabase.StartAsync(),
            _applicationDatabase.StartAsync());

        await using (var connection = new NpgsqlConnection(_authorityDatabase.GetConnectionString()))
        {
            await connection.OpenAsync();
            await using var command = connection.CreateCommand();
            command.CommandText = LocationPrivacyErasureAuthoritySchema.ReadProvisioningSql();
            await command.ExecuteNonQueryAsync();

            await using var roleCommand = connection.CreateCommand();
            roleCommand.CommandText =
                $"""
                CREATE ROLE {RuntimeUsername} LOGIN PASSWORD '{RuntimePassword}';
                GRANT location_privacy_authority_runtime TO {RuntimeUsername};
                """;
            await roleCommand.ExecuteNonQueryAsync();
        }

        _runtimeConnectionString = new NpgsqlConnectionStringBuilder(_authorityDatabase.GetConnectionString())
        {
            Username = RuntimeUsername,
            Password = RuntimePassword
        }.ConnectionString;
        _client = new PostgreSqlLocationPrivacyErasureAuthority(
            Options.Create(new LocationPrivacyErasureAuthorityOptions
            {
                ConnectionString = _runtimeConnectionString
            }));
    }

    [Test]
    [Timeout(180_000)]
    public async Task AppendAsync_DuplicateUuidV7Intent_ReturnsSameAuthorityFactWithoutMutation()
    {
        var intent = CreateIntent();

        var first = await Client.AppendAsync(intent);
        var duplicate = await Client.AppendAsync(intent);
        var next = await Client.AppendAsync(CreateIntent());
        var retainedCount = await CountIntentAsync(intent.IntentId);

        await Assert.That(duplicate.AuthoritySequence).IsEqualTo(first.AuthoritySequence);
        await Assert.That(duplicate.RecordedAtUtc).IsEqualTo(first.RecordedAtUtc);
        await Assert.That(duplicate.LocationIds).IsEquivalentTo(first.LocationIds);
        await Assert.That(next.AuthoritySequence).IsEqualTo(first.AuthoritySequence + 1);
        await Assert.That(retainedCount).IsEqualTo(1L);
        await Assert.That(first.AuthoritySequence).IsGreaterThan(0L);
        await Assert.That(first.RequestedAtUtc.Kind).IsEqualTo(DateTimeKind.Utc);
        await Assert.That(first.RecordedAtUtc.Kind).IsEqualTo(DateTimeKind.Utc);
        await Assert.That(first.RecordedAtUtc).IsLessThanOrEqualTo(DateTime.UtcNow);
    }

    [Test]
    [Timeout(180_000)]
    public async Task AppendAsync_DistinctIntents_AssignsStrictlyMonotonicAuthoritySequences()
    {
        var first = await Client.AppendAsync(CreateIntent());
        var second = await Client.AppendAsync(CreateIntent());
        var third = await Client.AppendAsync(CreateIntent());

        await Assert.That(second.AuthoritySequence).IsEqualTo(first.AuthoritySequence + 1);
        await Assert.That(third.AuthoritySequence).IsEqualTo(second.AuthoritySequence + 1);
    }

    [Test]
    [Timeout(180_000)]
    public async Task ReadAfterAsync_ReturnsOrderedBoundedCheckpointBatch()
    {
        var baseline = await GetHighWatermarkAsync();
        var expected = new[]
        {
            await Client.AppendAsync(CreateIntent()),
            await Client.AppendAsync(CreateIntent()),
            await Client.AppendAsync(CreateIntent())
        };

        var batch = await Client.ReadAfterAsync(baseline, 2);

        await Assert.That(batch.Count).IsEqualTo(2);
        await Assert.That(batch[0].IntentId).IsEqualTo(expected[0].IntentId);
        await Assert.That(batch[1].IntentId).IsEqualTo(expected[1].IntentId);
        await Assert.That(batch[1].AuthoritySequence).IsGreaterThan(batch[0].AuthoritySequence);
        await Assert.ThrowsAsync<ArgumentOutOfRangeException>(() =>
            Client.ReadAfterAsync(baseline, PostgreSqlLocationPrivacyErasureAuthority.MaximumReadBatchSize + 1));
    }

    [Test]
    [Timeout(180_000)]
    public async Task SchemaAndApplicationSurface_ContainOnlyPiiFreeFactFields()
    {
        var expectedColumns = new[]
        {
            "authority_sequence",
            "intent_id",
            "owner_user_id",
            "location_ids",
            "reason",
            "requested_at_utc",
            "recorded_at_utc"
        };
        var actualColumns = await ReadAuthorityColumnNamesAsync();
        var exposedPropertyNames = typeof(LocationPrivacyErasureIntent)
            .GetProperties()
            .Concat(typeof(LocationPrivacyErasureAuthorityIntent).GetProperties())
            .Select(property => property.Name)
            .Distinct(StringComparer.Ordinal)
            .ToArray();
        var forbiddenFragments = new[]
        {
            "name", "address", "postcode", "latitude", "longitude", "coordinate", "instruction", "secret"
        };

        await Assert.That(actualColumns.Count).IsEqualTo(expectedColumns.Length);
        for (var index = 0; index < expectedColumns.Length; index++)
        {
            await Assert.That(actualColumns[index]).IsEqualTo(expectedColumns[index]);
        }
        await Assert.That(exposedPropertyNames.Any(name =>
            forbiddenFragments.Any(fragment => name.Contains(fragment, StringComparison.OrdinalIgnoreCase)))).IsFalse();
        await Assert.That(typeof(LocationPrivacyErasureIntent).GetProperties()
            .Any(property => property.PropertyType == typeof(DateTime) ||
                             property.PropertyType == typeof(DateTimeOffset))).IsFalse();
        await Assert.That(typeof(ILocationPrivacyErasureAuthority).GetMethods()
            .Any(method => method.Name.Contains("Update", StringComparison.OrdinalIgnoreCase) ||
                           method.Name.Contains("Delete", StringComparison.OrdinalIgnoreCase))).IsFalse();
    }

    [Test]
    [Timeout(180_000)]
    public async Task RuntimeRole_RawTableMutationAndRead_AreRejected()
    {
        var fact = await Client.AppendAsync(CreateIntent());

        var updateFailure = await ExecuteForbiddenMutationAsync(
            "UPDATE location_privacy_authority.erasure_intents SET reason = 1 WHERE intent_id = @intent_id",
            fact.IntentId);
        var deleteFailure = await ExecuteForbiddenMutationAsync(
            "DELETE FROM location_privacy_authority.erasure_intents WHERE intent_id = @intent_id",
            fact.IntentId);
        var readFailure = await ExecuteForbiddenScalarAsync(
            "SELECT COUNT(*) FROM location_privacy_authority.erasure_intents");
        var metadataOverrideFailure = await ExecuteForbiddenMetadataOverrideAsync();

        await Assert.That(updateFailure.SqlState).IsEqualTo(PostgresErrorCodes.InsufficientPrivilege);
        await Assert.That(deleteFailure.SqlState).IsEqualTo(PostgresErrorCodes.InsufficientPrivilege);
        await Assert.That(readFailure.SqlState).IsEqualTo(PostgresErrorCodes.InsufficientPrivilege);
        await Assert.That(metadataOverrideFailure.SqlState).IsEqualTo(PostgresErrorCodes.InsufficientPrivilege);
        await Assert.That(await CountIntentAsync(fact.IntentId)).IsEqualTo(1L);
    }

    [Test]
    [Timeout(180_000)]
    public async Task AppendFunction_RolledBackTransaction_DoesNotConsumeAuthoritySequence()
    {
        var baseline = await GetHighWatermarkAsync();
        var rolledBackIntent = CreateIntent();

        await using (var connection = new NpgsqlConnection(_runtimeConnectionString))
        {
            await connection.OpenAsync();
            await using var transaction = await connection.BeginTransactionAsync();
            await using var command = CreateAppendFunctionCommand(connection, rolledBackIntent);
            command.Transaction = transaction;
            var allocated = (long)(await command.ExecuteScalarAsync() ?? 0L);
            await Assert.That(allocated).IsEqualTo(baseline + 1);
            await transaction.RollbackAsync();
        }

        var retained = await Client.AppendAsync(CreateIntent());

        await Assert.That(retained.AuthoritySequence).IsEqualTo(baseline + 1);
        await Assert.That(await CountIntentAsync(rolledBackIntent.IntentId)).IsEqualTo(0L);
    }

    [Test]
    [Timeout(180_000)]
    public async Task AppendFunction_FailedInsert_DoesNotConsumeAuthoritySequence()
    {
        var baseline = await GetHighWatermarkAsync();
        var rejectedIntent = CreateIntent();
        await InstallRejectingInsertTriggerAsync(rejectedIntent.IntentId);

        try
        {
            await Assert.ThrowsAsync<PostgresException>(() => Client.AppendAsync(rejectedIntent));
        }
        finally
        {
            await RemoveRejectingInsertTriggerAsync();
        }

        var retained = await Client.AppendAsync(CreateIntent());

        await Assert.That(retained.AuthoritySequence).IsEqualTo(baseline + 1);
        await Assert.That(await CountIntentAsync(rejectedIntent.IntentId)).IsEqualTo(0L);
    }

    [Test]
    [Timeout(180_000)]
    public async Task AppendAsync_CancelledAfterCounterAllocation_DoesNotConsumeAuthoritySequence()
    {
        var baseline = await GetHighWatermarkAsync();
        var cancelledIntent = CreateIntent();
        using var cancellation = new CancellationTokenSource();
        await using var blockerConnection = new NpgsqlConnection(_authorityDatabase.GetConnectionString());
        await blockerConnection.OpenAsync();
        await using var blockerTransaction = await blockerConnection.BeginTransactionAsync();
        await AcquireBlockingInsertLockAsync(blockerConnection, blockerTransaction);
        await InstallBlockingInsertTriggerAsync();
        Task<LocationPrivacyErasureAuthorityIntent> appendTask =
            Client.AppendAsync(cancelledIntent, cancellation.Token);

        try
        {
            await WaitForBlockedAppendAsync();
            cancellation.Cancel();
            await Assert.ThrowsAsync<OperationCanceledException>(() => appendTask);
        }
        finally
        {
            cancellation.Cancel();
            await blockerTransaction.RollbackAsync();
            if (!appendTask.IsCompleted)
            {
                await Assert.ThrowsAsync<OperationCanceledException>(() => appendTask);
            }
            await RemoveBlockingInsertTriggerAsync();
        }

        var retained = await Client.AppendAsync(CreateIntent());

        await Assert.That(retained.AuthoritySequence).IsEqualTo(baseline + 1);
        await Assert.That(await CountIntentAsync(cancelledIntent.IntentId)).IsEqualTo(0L);
    }

    [Test]
    [Timeout(180_000)]
    public async Task AppendAsync_AfterAmbiguousAcknowledgement_RecoversRetainedFactOnRetry()
    {
        var baseline = await GetHighWatermarkAsync();
        var intent = CreateIntent();
        await using (var connection = new NpgsqlConnection(_runtimeConnectionString))
        {
            await connection.OpenAsync();
            await using var unacknowledgedAppend = CreateAppendFunctionCommand(connection, intent);
            await unacknowledgedAppend.ExecuteNonQueryAsync();
        }

        var recovered = await Client.AppendAsync(intent);
        var next = await Client.AppendAsync(CreateIntent());

        await Assert.That(recovered.AuthoritySequence).IsEqualTo(baseline + 1);
        await Assert.That(recovered.IntentId).IsEqualTo(intent.IntentId);
        await Assert.That(recovered.OwnerUserId).IsEqualTo(intent.OwnerUserId);
        await Assert.That(recovered.LocationIds).IsEquivalentTo(intent.LocationIds);
        await Assert.That(next.AuthoritySequence).IsEqualTo(baseline + 2);
        await Assert.That(await CountIntentAsync(intent.IntentId)).IsEqualTo(1L);
    }

    [Test]
    [Timeout(180_000)]
    public async Task RuntimeRole_CounterAndSequenceOperations_AreRejected()
    {
        var counterRead = await ExecuteForbiddenScalarAsync(
            "SELECT last_sequence FROM location_privacy_authority.authority_counter");
        var nextValue = await ExecuteForbiddenScalarAsync(
            "SELECT nextval('location_privacy_authority.erasure_intents_authority_sequence_seq')");
        var setValue = await ExecuteForbiddenScalarAsync(
            "SELECT setval('location_privacy_authority.erasure_intents_authority_sequence_seq', 1, false)");
        var sequenceCount = await CountAuthoritySequencesAsync();

        await Assert.That(counterRead.SqlState).IsEqualTo(PostgresErrorCodes.InsufficientPrivilege);
        await Assert.That(nextValue.SqlState).IsEqualTo(PostgresErrorCodes.UndefinedTable);
        await Assert.That(setValue.SqlState).IsEqualTo(PostgresErrorCodes.UndefinedTable);
        await Assert.That(sequenceCount).IsEqualTo(0L);
    }

    [Test]
    [Timeout(180_000)]
    public async Task AppendAsync_ConcurrentDistinctIntents_AreContiguousAndReplayable()
    {
        const int appendCount = 12;
        var baseline = await GetHighWatermarkAsync();
        var intents = Enumerable.Range(0, appendCount).Select(_ => CreateIntent()).ToArray();

        var appended = await Task.WhenAll(intents.Select(intent => Client.AppendAsync(intent)));
        var ordered = appended.OrderBy(fact => fact.AuthoritySequence).ToArray();
        var replay = await Client.ReadAfterAsync(baseline, appendCount);

        for (var index = 0; index < appendCount; index++)
        {
            await Assert.That(ordered[index].AuthoritySequence).IsEqualTo(baseline + index + 1);
            await Assert.That(replay[index].AuthoritySequence).IsEqualTo(ordered[index].AuthoritySequence);
            await Assert.That(replay[index].IntentId).IsEqualTo(ordered[index].IntentId);
        }
    }

    [Test]
    [Timeout(180_000)]
    public async Task AppendAsync_DuplicateIntent_NormalizesAndRejectsMismatchedPayloads()
    {
        var firstLocationId = Guid.CreateVersion7();
        var secondLocationId = Guid.CreateVersion7();
        var intent = new LocationPrivacyErasureIntent(
            Guid.CreateVersion7(),
            Guid.CreateVersion7(),
            [secondLocationId, firstLocationId, firstLocationId],
            LocationPrivacyErasureReasonEnum.AccountDeletion);
        var retained = await Client.AppendAsync(intent);
        var normalizedRetry = await Client.AppendAsync(intent with
        {
            LocationIds = [firstLocationId, secondLocationId]
        });

        await Assert.That(normalizedRetry.AuthoritySequence).IsEqualTo(retained.AuthoritySequence);
        await Assert.ThrowsAsync<InvalidOperationException>(() => Client.AppendAsync(intent with
        {
            OwnerUserId = Guid.CreateVersion7()
        }));
        await Assert.ThrowsAsync<InvalidOperationException>(() => Client.AppendAsync(intent with
        {
            LocationIds = [firstLocationId, Guid.CreateVersion7()]
        }));
        await Assert.ThrowsAsync<InvalidOperationException>(() => Client.AppendAsync(intent with
        {
            Reason = LocationPrivacyErasureReasonEnum.OwnerErasureRequest
        }));

        var next = await Client.AppendAsync(CreateIntent());
        await Assert.That(next.AuthoritySequence).IsEqualTo(retained.AuthoritySequence + 1);
        await Assert.That(await CountIntentAsync(intent.IntentId)).IsEqualTo(1L);
    }

    [Test]
    [Timeout(180_000)]
    public async Task AppendAsync_Version7ShapeWithNonRfcVariant_IsRejectedByClientAndDatabase()
    {
        var nonRfcVariantIntent = CreateIntent() with
        {
            IntentId = Guid.Parse("018e4e5c-7f00-7000-0000-000000000001")
        };

        await Assert.ThrowsAsync<ArgumentException>(() => Client.AppendAsync(nonRfcVariantIntent));
        var databaseFailure = await ExecuteAppendFunctionFailureAsync(nonRfcVariantIntent);

        await Assert.That(databaseFailure.SqlState).IsEqualTo(PostgresErrorCodes.InvalidParameterValue);
        await Assert.That(await CountIntentAsync(nonRfcVariantIntent.IntentId)).IsEqualTo(0L);
    }

    [Test]
    [Timeout(180_000)]
    public async Task AuthorityFunctions_AreSecurityDefinerWithFixedSearchPathAndDedicatedOwner()
    {
        var functionSecurity = await ReadFunctionSecurityAsync();

        await Assert.That(functionSecurity.Count).IsEqualTo(2);
        foreach (var function in functionSecurity)
        {
            await Assert.That(function.IsSecurityDefiner).IsTrue();
            await Assert.That(function.Owner).IsEqualTo("location_privacy_authority_owner");
            await Assert.That(function.Configuration).Contains("search_path=pg_catalog, location_privacy_authority");
        }
    }

    [Test]
    [Timeout(180_000)]
    public async Task RecreateApplicationDatabase_DoesNotEraseIndependentlyRetainedAuthorityFact()
    {
        var authorityFact = await Client.AppendAsync(CreateIntent());
        await CreateApplicationMarkerAsync();

        await RecreateApplicationDatabaseAsync();

        var replay = await Client.ReadAfterAsync(authorityFact.AuthoritySequence - 1, 1);
        await Assert.That(replay.Single().IntentId).IsEqualTo(authorityFact.IntentId);
        await Assert.That(await ApplicationMarkerExistsAsync()).IsFalse();
    }

    [Test]
    public async Task ConfigureInfrastructureServices_RegistersAuthorityClientAgainstDedicatedOptionsSection()
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                [$"{LocationPrivacyErasureAuthorityOptions.SectionName}:ConnectionString"] = _runtimeConnectionString
            })
            .Build();
        var services = new ServiceCollection();
        services.AddLogging();
        services.ConfigureInfrastructureServices(configuration);

        await using var provider = services.BuildServiceProvider();
        var authority = provider.GetRequiredService<ILocationPrivacyErasureAuthority>();

        await Assert.That(authority).IsTypeOf<PostgreSqlLocationPrivacyErasureAuthority>();
    }

    public async ValueTask DisposeAsync()
    {
        if (_client is not null)
        {
            await _client.DisposeAsync();
        }

        await _authorityDatabase.DisposeAsync();
        await _applicationDatabase.DisposeAsync();
    }

    private PostgreSqlLocationPrivacyErasureAuthority Client =>
        _client ?? throw new InvalidOperationException("The authority client has not been initialized.");

    private static LocationPrivacyErasureIntent CreateIntent() => new(
        Guid.CreateVersion7(),
        Guid.CreateVersion7(),
        [Guid.CreateVersion7(), Guid.CreateVersion7()],
        LocationPrivacyErasureReasonEnum.AccountDeletion);

    private async Task<long> CountIntentAsync(Guid intentId)
    {
        await using var connection = new NpgsqlConnection(_authorityDatabase.GetConnectionString());
        await connection.OpenAsync();
        await using var command = connection.CreateCommand();
        command.CommandText =
            "SELECT COUNT(*) FROM location_privacy_authority.erasure_intents WHERE intent_id = @intent_id";
        command.Parameters.AddWithValue("intent_id", intentId);
        return (long)(await command.ExecuteScalarAsync() ?? 0L);
    }

    private async Task<long> GetHighWatermarkAsync()
    {
        await using var connection = new NpgsqlConnection(_authorityDatabase.GetConnectionString());
        await connection.OpenAsync();
        await using var command = connection.CreateCommand();
        command.CommandText =
            "SELECT COALESCE(MAX(authority_sequence), 0) FROM location_privacy_authority.erasure_intents";
        return (long)(await command.ExecuteScalarAsync() ?? 0L);
    }

    private async Task<IReadOnlyList<string>> ReadAuthorityColumnNamesAsync()
    {
        await using var connection = new NpgsqlConnection(_authorityDatabase.GetConnectionString());
        await connection.OpenAsync();
        await using var command = connection.CreateCommand();
        command.CommandText =
            """
            SELECT column_name
            FROM information_schema.columns
            WHERE table_schema = 'location_privacy_authority'
              AND table_name = 'erasure_intents'
            ORDER BY ordinal_position
            """;
        var columns = new List<string>();
        await using var reader = await command.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            columns.Add(reader.GetString(0));
        }

        return columns;
    }

    private async Task<PostgresException> ExecuteForbiddenMutationAsync(string sql, Guid intentId)
    {
        await using var connection = new NpgsqlConnection(_runtimeConnectionString);
        await connection.OpenAsync();
        await using var command = connection.CreateCommand();
        command.CommandText = sql;
        command.Parameters.AddWithValue("intent_id", intentId);
        return await Assert.ThrowsAsync<PostgresException>(() => command.ExecuteNonQueryAsync())
            ?? throw new InvalidOperationException("Expected PostgreSQL to reject the table mutation.");
    }

    private async Task<PostgresException> ExecuteForbiddenScalarAsync(string sql)
    {
        await using var connection = new NpgsqlConnection(_runtimeConnectionString);
        await connection.OpenAsync();
        await using var command = connection.CreateCommand();
        command.CommandText = sql;
        return await Assert.ThrowsAsync<PostgresException>(() => command.ExecuteScalarAsync())
            ?? throw new InvalidOperationException("Expected PostgreSQL to reject the scalar operation.");
    }

    private async Task<PostgresException> ExecuteForbiddenMetadataOverrideAsync()
    {
        await using var connection = new NpgsqlConnection(_runtimeConnectionString);
        await connection.OpenAsync();
        await using var command = connection.CreateCommand();
        command.CommandText =
            """
            INSERT INTO location_privacy_authority.erasure_intents
                (authority_sequence, intent_id, owner_user_id, location_ids, reason,
                 requested_at_utc, recorded_at_utc)
            VALUES (1, @intent_id, @owner_user_id, ARRAY[]::uuid[], 1,
                    TIMESTAMPTZ '2000-01-01 00:00:00+00', TIMESTAMPTZ '2000-01-01 00:00:00+00')
            """;
        command.Parameters.AddWithValue("intent_id", Guid.CreateVersion7());
        command.Parameters.AddWithValue("owner_user_id", Guid.CreateVersion7());
        return await Assert.ThrowsAsync<PostgresException>(() => command.ExecuteNonQueryAsync())
            ?? throw new InvalidOperationException("Expected PostgreSQL to reject metadata override.");
    }

    private static NpgsqlCommand CreateAppendFunctionCommand(
        NpgsqlConnection connection,
        LocationPrivacyErasureIntent intent)
    {
        var command = connection.CreateCommand();
        command.CommandText =
            """
            SELECT authority_sequence
            FROM location_privacy_authority.append_erasure_intent(
                @intent_id, @owner_user_id, @location_ids, @reason)
            """;
        command.Parameters.AddWithValue("intent_id", NpgsqlTypes.NpgsqlDbType.Uuid, intent.IntentId);
        command.Parameters.AddWithValue("owner_user_id", NpgsqlTypes.NpgsqlDbType.Uuid, intent.OwnerUserId);
        command.Parameters.AddWithValue(
            "location_ids",
            NpgsqlTypes.NpgsqlDbType.Array | NpgsqlTypes.NpgsqlDbType.Uuid,
            intent.LocationIds.ToArray());
        command.Parameters.AddWithValue(
            "reason",
            NpgsqlTypes.NpgsqlDbType.Smallint,
            (short)intent.Reason);
        return command;
    }

    private async Task<PostgresException> ExecuteAppendFunctionFailureAsync(
        LocationPrivacyErasureIntent intent)
    {
        await using var connection = new NpgsqlConnection(_runtimeConnectionString);
        await connection.OpenAsync();
        await using var command = CreateAppendFunctionCommand(connection, intent);
        return await Assert.ThrowsAsync<PostgresException>(() => command.ExecuteScalarAsync())
            ?? throw new InvalidOperationException("Expected PostgreSQL to reject the append payload.");
    }

    private async Task InstallRejectingInsertTriggerAsync(Guid rejectedIntentId)
    {
        await using var connection = new NpgsqlConnection(_authorityDatabase.GetConnectionString());
        await connection.OpenAsync();
        await using var command = connection.CreateCommand();
        command.CommandText =
            $"""
            CREATE OR REPLACE FUNCTION location_privacy_authority.reject_test_insert()
            RETURNS trigger
            LANGUAGE plpgsql
            AS $function$
            BEGIN
                IF NEW.intent_id = '{rejectedIntentId:D}'::uuid THEN
                    RAISE EXCEPTION 'forced integration-test insert failure';
                END IF;
                RETURN NEW;
            END;
            $function$;

            CREATE TRIGGER tr_erasure_intents_reject_test_insert
            BEFORE INSERT ON location_privacy_authority.erasure_intents
            FOR EACH ROW
            EXECUTE FUNCTION location_privacy_authority.reject_test_insert();
            """;
        await command.ExecuteNonQueryAsync();
    }

    private async Task RemoveRejectingInsertTriggerAsync()
    {
        await using var connection = new NpgsqlConnection(_authorityDatabase.GetConnectionString());
        await connection.OpenAsync();
        await using var command = connection.CreateCommand();
        command.CommandText =
            """
            DROP TRIGGER IF EXISTS tr_erasure_intents_reject_test_insert
                ON location_privacy_authority.erasure_intents;
            DROP FUNCTION IF EXISTS location_privacy_authority.reject_test_insert();
            """;
        await command.ExecuteNonQueryAsync();
    }

    private static async Task AcquireBlockingInsertLockAsync(
        NpgsqlConnection connection,
        NpgsqlTransaction transaction)
    {
        await using var command = connection.CreateCommand();
        command.Transaction = transaction;
        command.CommandText = "SELECT pg_advisory_xact_lock(@lock_key)";
        command.Parameters.AddWithValue("lock_key", BlockingInsertAdvisoryLockKey);
        await command.ExecuteNonQueryAsync();
    }

    private async Task InstallBlockingInsertTriggerAsync()
    {
        await using var connection = new NpgsqlConnection(_authorityDatabase.GetConnectionString());
        await connection.OpenAsync();
        await using var command = connection.CreateCommand();
        command.CommandText =
            $"""
            CREATE OR REPLACE FUNCTION location_privacy_authority.block_test_insert()
            RETURNS trigger
            LANGUAGE plpgsql
            AS $function$
            BEGIN
                PERFORM pg_advisory_xact_lock({BlockingInsertAdvisoryLockKey});
                RETURN NEW;
            END;
            $function$;

            CREATE TRIGGER tr_erasure_intents_block_test_insert
            BEFORE INSERT ON location_privacy_authority.erasure_intents
            FOR EACH ROW
            EXECUTE FUNCTION location_privacy_authority.block_test_insert();
            """;
        await command.ExecuteNonQueryAsync();
    }

    private async Task WaitForBlockedAppendAsync()
    {
        using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(10));
        await using var connection = new NpgsqlConnection(_authorityDatabase.GetConnectionString());
        await connection.OpenAsync(timeout.Token);
        await using var command = connection.CreateCommand();
        command.CommandText =
            """
            SELECT EXISTS
            (
                SELECT 1
                FROM pg_locks
                WHERE locktype = 'advisory'
                  AND granted = false
                  AND classid = 0
                  AND objid::bigint = @lock_key
                  AND objsubid = 1
            )
            """;
        command.Parameters.AddWithValue("lock_key", BlockingInsertAdvisoryLockKey);

        try
        {
            while (!(bool)(await command.ExecuteScalarAsync(timeout.Token) ?? false))
            {
                await Task.Delay(10, timeout.Token);
            }
        }
        catch (OperationCanceledException) when (timeout.IsCancellationRequested)
        {
            throw new TimeoutException("The authority append did not reach the blocking insert trigger.");
        }
    }

    private async Task RemoveBlockingInsertTriggerAsync()
    {
        await using var connection = new NpgsqlConnection(_authorityDatabase.GetConnectionString());
        await connection.OpenAsync();
        await using var command = connection.CreateCommand();
        command.CommandText =
            """
            DROP TRIGGER IF EXISTS tr_erasure_intents_block_test_insert
                ON location_privacy_authority.erasure_intents;
            DROP FUNCTION IF EXISTS location_privacy_authority.block_test_insert();
            """;
        await command.ExecuteNonQueryAsync();
    }

    private async Task<long> CountAuthoritySequencesAsync()
    {
        await using var connection = new NpgsqlConnection(_authorityDatabase.GetConnectionString());
        await connection.OpenAsync();
        await using var command = connection.CreateCommand();
        command.CommandText =
            """
            SELECT COUNT(*)
            FROM pg_class AS relation
            INNER JOIN pg_namespace AS schema ON schema.oid = relation.relnamespace
            WHERE schema.nspname = 'location_privacy_authority'
              AND relation.relkind = 'S'
            """;
        return (long)(await command.ExecuteScalarAsync() ?? 0L);
    }

    private async Task<IReadOnlyList<(bool IsSecurityDefiner, string Owner, string Configuration)>>
        ReadFunctionSecurityAsync()
    {
        await using var connection = new NpgsqlConnection(_authorityDatabase.GetConnectionString());
        await connection.OpenAsync();
        await using var command = connection.CreateCommand();
        command.CommandText =
            """
            SELECT function.prosecdef,
                   owner.rolname,
                   array_to_string(function.proconfig, ';')
            FROM pg_proc AS function
            INNER JOIN pg_namespace AS schema ON schema.oid = function.pronamespace
            INNER JOIN pg_roles AS owner ON owner.oid = function.proowner
            WHERE schema.nspname = 'location_privacy_authority'
              AND function.proname IN ('append_erasure_intent', 'read_erasure_intents_after')
            ORDER BY function.proname
            """;
        var functions = new List<(bool, string, string)>();
        await using var reader = await command.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            functions.Add((reader.GetBoolean(0), reader.GetString(1), reader.GetString(2)));
        }

        return functions;
    }

    private async Task CreateApplicationMarkerAsync()
    {
        await using var connection = new NpgsqlConnection(_applicationDatabase.GetConnectionString());
        await connection.OpenAsync();
        await using var command = connection.CreateCommand();
        command.CommandText = "CREATE TABLE application_restore_marker (id integer PRIMARY KEY); INSERT INTO application_restore_marker VALUES (1);";
        await command.ExecuteNonQueryAsync();
    }

    private async Task RecreateApplicationDatabaseAsync()
    {
        NpgsqlConnection.ClearAllPools();
        var administrativeConnectionString = new NpgsqlConnectionStringBuilder(
            _applicationDatabase.GetConnectionString())
        {
            Database = "postgres"
        }.ConnectionString;
        await using var connection = new NpgsqlConnection(administrativeConnectionString);
        await connection.OpenAsync();
        await using (var drop = connection.CreateCommand())
        {
            drop.CommandText = $"DROP DATABASE IF EXISTS \"{ApplicationDatabaseName}\" WITH (FORCE)";
            await drop.ExecuteNonQueryAsync();
        }

        await using var create = connection.CreateCommand();
        create.CommandText = $"CREATE DATABASE \"{ApplicationDatabaseName}\"";
        await create.ExecuteNonQueryAsync();
    }

    private async Task<bool> ApplicationMarkerExistsAsync()
    {
        await using var connection = new NpgsqlConnection(_applicationDatabase.GetConnectionString());
        await connection.OpenAsync();
        await using var command = connection.CreateCommand();
        command.CommandText = "SELECT to_regclass('public.application_restore_marker') IS NOT NULL";
        return (bool)(await command.ExecuteScalarAsync() ?? false);
    }
}
