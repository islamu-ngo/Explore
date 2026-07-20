// ABOUTME: Verifies the dedicated authority migration on fresh and populated raw PostgreSQL schemas.
// ABOUTME: Proves non-destructive adoption, guarded rollback, and the function-only runtime boundary.

using System.Runtime.CompilerServices;
using Event.Persistence.IntegrationTests.Fixtures;
using Explore.Persistence.Privacy.ErasureAuthority;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql;
using NpgsqlTypes;

namespace Event.Persistence.IntegrationTests.Migrations;

[ClassDataSource<RecipientDeliveryMigrationContainerFixture>(Shared = SharedType.PerAssembly)]
[NotInParallel("RecipientDeliveryMigrationDb")]
public sealed class LocationPrivacyAuthorityMigrationTests(
    RecipientDeliveryMigrationContainerFixture fixture)
{
    private const string MigrationName = "InitialLocationPrivacyAuthority";
    private const string RuntimeUsername = "orea140_runtime_test";
    private const string RuntimePassword = "orea140-runtime-test";
    private static readonly (string Table, string Name, string Expression)[] CanonicalChecks =
    [
        ("erasure_intents", "ck_location_privacy_erasure_intents_sequence", "authority_sequence > 0"),
        ("erasure_intents", "ck_location_privacy_erasure_intents_intent_uuid_v7", "substring(intent_id::text from 15 for 1) = '7'"),
        ("erasure_intents", "ck_location_privacy_erasure_intents_intent_rfc4122_variant", "substring(intent_id::text from 20 for 1) IN ('8', '9', 'a', 'b')"),
        ("erasure_intents", "ck_location_privacy_erasure_intents_owner_nonempty", "owner_user_id <> '00000000-0000-0000-0000-000000000000'::uuid"),
        ("erasure_intents", "ck_location_privacy_erasure_intents_location_ids_no_empty_uuid", "array_position(location_ids, '00000000-0000-0000-0000-000000000000'::uuid) IS NULL"),
        ("erasure_intents", "ck_location_privacy_erasure_intents_location_ids_no_nulls", "array_position(location_ids, NULL) IS NULL"),
        ("erasure_intents", "ck_location_privacy_erasure_intents_reason", "reason BETWEEN 1 AND 3"),
        ("erasure_intents", "ck_location_privacy_erasure_intents_server_time_order", "recorded_at_utc >= requested_at_utc"),
        ("authority_counter", "ck_location_privacy_authority_counter_singleton", "singleton"),
        ("authority_counter", "ck_location_privacy_authority_counter_nonnegative", "last_sequence >= 0")
    ];

    [Test]
    public async Task DedicatedAuthorityMigration_IsPresentAndOwnsOnlyAuthorityModel()
    {
        await using PrivacyErasureAuthorityDbContext context = CreateDbContext();
        IMigrationsAssembly migrations = context.GetService<IMigrationsAssembly>();

        await Assert.That(migrations.Migrations.Keys)
            .Contains(id => id.EndsWith($"_{MigrationName}", StringComparison.Ordinal));
        await Assert.That(context.Model.GetEntityTypes().Select(entity => entity.GetTableName()!))
            .IsEquivalentTo(["authority_counter", "erasure_intents"]);
        await Assert.That(migrations.ModelSnapshot!.Model.GetEntityTypes()
            .Select(entity => entity.GetTableName()!))
            .IsEquivalentTo(["authority_counter", "erasure_intents"]);
    }

    [Test]
    public async Task FreshMigration_RepeatsAndRollsBackOnlyWhileEmpty()
    {
        await ResetDatabaseAsync();
        try
        {
            await using PrivacyErasureAuthorityDbContext context = CreateDbContext();
            IMigrator migrator = context.GetService<IMigrator>();
            string target = TargetMigration(context);

            await migrator.MigrateAsync(target);
            await migrator.MigrateAsync(target);

            await Assert.That(await RelationExistsAsync("location_privacy_authority.erasure_intents")).IsTrue();
            await Assert.That(await RelationExistsAsync("location_privacy_authority.authority_counter")).IsTrue();
            await Assert.That(await ReadCounterAsync()).IsEqualTo(0L);
            await Assert.That(await HistoryContainsAsync(target)).IsTrue();

            await migrator.MigrateAsync(Migration.InitialDatabase);

            await Assert.That(await RelationExistsAsync("location_privacy_authority.erasure_intents")).IsFalse();
            await Assert.That(await RelationExistsAsync("location_privacy_authority.authority_counter")).IsFalse();
            await Assert.That(await HistoryContainsAsync(target)).IsFalse();

            await migrator.MigrateAsync(target);
            await Assert.That(await RelationExistsAsync("location_privacy_authority.erasure_intents")).IsTrue();
            await Assert.That(await ReadCounterAsync()).IsEqualTo(0L);
        }
        finally
        {
            await ResetDatabaseAsync();
        }
    }

    [Test]
    public async Task PopulatedRawSchema_IsAdoptedWithoutChangingFactsOrCounter()
    {
        await ResetDatabaseAsync();
        try
        {
            await PrepareLegacyRawSchemaAsync();
            await AppendAsAdminAsync(Guid.CreateVersion7(), Guid.CreateVersion7(), [Guid.CreateVersion7()], 1);
            await AppendAsAdminAsync(Guid.CreateVersion7(), Guid.CreateVersion7(),
                [Guid.CreateVersion7(), Guid.CreateVersion7()], 2);
            string before = await ReadAuthorityValueBytesAsync();

            await using PrivacyErasureAuthorityDbContext context = CreateDbContext();
            IMigrator migrator = context.GetService<IMigrator>();
            string target = TargetMigration(context);
            await migrator.MigrateAsync(target);
            string after = await ReadAuthorityValueBytesAsync();
            await migrator.MigrateAsync(target);

            await Assert.That(after).IsEqualTo(before);
            await Assert.That(await ReadAuthorityValueBytesAsync()).IsEqualTo(before);
            await Assert.That(await HistoryContainsAsync(target)).IsTrue();
            await Assert.That(await ReadCounterAsync()).IsEqualTo(2L);
        }
        finally
        {
            await ResetDatabaseAsync();
        }
    }

    [Test]
    public async Task FrozenHistoricalSql_IsExecutedDirectlyAndAdoptedByteIdentically()
    {
        await ResetDatabaseAsync();
        try
        {
            await ExecuteAdminAsync(await ReadFrozenHistoricalSqlAsync());
            await AppendAsAdminAsync(Guid.CreateVersion7(), Guid.CreateVersion7(), [Guid.CreateVersion7()], 1);
            await AppendAsAdminAsync(Guid.CreateVersion7(), Guid.CreateVersion7(), [Guid.CreateVersion7()], 2);
            await ExecuteAdminAsync(
                "UPDATE location_privacy_authority.authority_counter SET last_sequence = 41 WHERE singleton");
            string before = await ReadAuthorityValueBytesAsync();

            await MigrateAuthorityAsync();

            await Assert.That(await ReadAuthorityValueBytesAsync()).IsEqualTo(before);
            await Assert.That(await ReadCounterAsync()).IsEqualTo(41L);
            await Assert.That(await AuthorityMigrationHistoryCountAsync()).IsEqualTo(1);
        }
        finally
        {
            await ResetDatabaseAsync();
        }
    }

    [Test]
    public async Task ValidFreshAndFrozenAdoption_ApplyWithExactHistoryAndValuePreservation()
    {
        await ResetDatabaseAsync();
        try
        {
            await MigrateAuthorityAsync();

            await Assert.That(await AuthorityMigrationHistoryCountAsync()).IsEqualTo(1);
            await Assert.That(await ReadCounterAsync()).IsEqualTo(0L);

            await ResetDatabaseAsync();
            await ExecuteAdminAsync(await ReadFrozenHistoricalSqlAsync());
            await AppendAsAdminAsync(
                Guid.CreateVersion7(), Guid.CreateVersion7(), [Guid.CreateVersion7()], 1);
            await ExecuteAdminAsync(
                "UPDATE location_privacy_authority.authority_counter SET last_sequence = 41 WHERE singleton");
            string before = await ReadAuthorityValueBytesAsync();

            await MigrateAuthorityAsync();

            await Assert.That(await AuthorityMigrationHistoryCountAsync()).IsEqualTo(1);
            await Assert.That(await ReadCounterAsync()).IsEqualTo(41L);
            await Assert.That(await ReadAuthorityValueBytesAsync()).IsEqualTo(before);
        }
        finally
        {
            await ResetDatabaseAsync();
        }
    }

    [Test]
    public async Task FreshAndFrozenAdoption_ConvergeToExactDefaultFunctionAclContract()
    {
        await ResetDatabaseAsync();
        try
        {
            await MigrateAuthorityAsync();

            await Assert.That(await ReadAuthorityDefaultAclContractAsync())
                .IsEqualTo(
                    "location_privacy_authority_owner:location_privacy_authority:f:" +
                    "location_privacy_authority_owner:EXECUTE:false");

            await ResetDatabaseAsync();
            await ExecuteAdminAsync(await ReadFrozenHistoricalSqlAsync());
            await AppendAsAdminAsync(
                Guid.CreateVersion7(), Guid.CreateVersion7(), [Guid.CreateVersion7()], 1);
            string before = await ReadAuthorityValueBytesAsync();

            await MigrateAuthorityAsync();

            await Assert.That(await ReadAuthorityDefaultAclContractAsync())
                .IsEqualTo(
                    "location_privacy_authority_owner:location_privacy_authority:f:" +
                    "location_privacy_authority_owner:EXECUTE:false");
            await Assert.That(await ReadAuthorityValueBytesAsync()).IsEqualTo(before);
        }
        finally
        {
            await ResetDatabaseAsync();
        }
    }

    [Test]
    public async Task PrivilegedUpdateAndDelete_ReachTriggerAndLeaveAuthorityStateByteIdentical()
    {
        await ResetDatabaseAsync();
        try
        {
            await ExecuteAdminAsync(await ReadFrozenHistoricalSqlAsync());
            await MigrateAuthorityAsync();
            await AppendAsAdminAsync(Guid.CreateVersion7(), Guid.CreateVersion7(), [Guid.CreateVersion7()], 1);
            string before = await ReadAuthorityStateBytesAsync();

            PostgresException update = (await Assert.ThrowsAsync<PostgresException>(() =>
                ExecuteAdminAsync(
                    "UPDATE location_privacy_authority.erasure_intents SET reason = 2")))!;
            await Assert.That(update.SqlState).IsEqualTo(PostgresErrorCodes.ObjectNotInPrerequisiteState);
            await Assert.That(await ReadAuthorityStateBytesAsync()).IsEqualTo(before);

            PostgresException delete = (await Assert.ThrowsAsync<PostgresException>(() =>
                ExecuteAdminAsync("DELETE FROM location_privacy_authority.erasure_intents")))!;
            await Assert.That(delete.SqlState).IsEqualTo(PostgresErrorCodes.ObjectNotInPrerequisiteState);
            await Assert.That(await ReadAuthorityStateBytesAsync()).IsEqualTo(before);
        }
        finally
        {
            await ResetDatabaseAsync();
        }
    }

    [Test]
    public async Task ExistingPrimaryKeyOnWrongColumn_IsRejectedWith55000()
    {
        await ResetDatabaseAsync();
        try
        {
            await PrepareLegacyRawSchemaAsync();
            await ExecuteAdminAsync(
                "ALTER TABLE location_privacy_authority.erasure_intents " +
                "DROP CONSTRAINT erasure_intents_pkey, " +
                "ADD CONSTRAINT erasure_intents_pkey PRIMARY KEY (intent_id)");

            Exception failure = (await Assert.ThrowsAsync<Exception>(() => MigrateAuthorityAsync()))!;

            await Assert.That(FindPostgresException(failure)!.SqlState)
                .IsEqualTo(PostgresErrorCodes.ObjectNotInPrerequisiteState);
            await Assert.That(await AuthorityMigrationHistoryCountAsync()).IsEqualTo(0);
        }
        finally
        {
            await ResetDatabaseAsync();
        }
    }

    [Test]
    public async Task ExpectedIndexNameWithoutUniqueness_IsRejectedWith55000()
    {
        await ResetDatabaseAsync();
        try
        {
            await PrepareLegacyRawSchemaAsync();
            await ExecuteAdminAsync(
                "ALTER TABLE location_privacy_authority.erasure_intents " +
                "DROP CONSTRAINT erasure_intents_intent_id_key; " +
                "CREATE INDEX ix_erasure_intents_intent_id " +
                "ON location_privacy_authority.erasure_intents (intent_id)");

            Exception failure = (await Assert.ThrowsAsync<Exception>(() => MigrateAuthorityAsync()))!;

            await Assert.That(FindPostgresException(failure)!.SqlState)
                .IsEqualTo(PostgresErrorCodes.ObjectNotInPrerequisiteState);
            await Assert.That(await AuthorityMigrationHistoryCountAsync()).IsEqualTo(0);
        }
        finally
        {
            await ResetDatabaseAsync();
        }
    }

    [Test]
    public async Task ExpectedUniqueIndexNameOnWrongColumn_IsRejectedWith55000()
    {
        await ResetDatabaseAsync();
        try
        {
            await PrepareLegacyRawSchemaAsync();
            await ExecuteAdminAsync(
                "ALTER TABLE location_privacy_authority.erasure_intents " +
                "DROP CONSTRAINT erasure_intents_intent_id_key; " +
                "CREATE UNIQUE INDEX ix_erasure_intents_intent_id " +
                "ON location_privacy_authority.erasure_intents (owner_user_id)");

            Exception failure = (await Assert.ThrowsAsync<Exception>(() => MigrateAuthorityAsync()))!;

            await Assert.That(FindPostgresException(failure)!.SqlState)
                .IsEqualTo(PostgresErrorCodes.ObjectNotInPrerequisiteState);
            await Assert.That(await AuthorityMigrationHistoryCountAsync()).IsEqualTo(0);
        }
        finally
        {
            await ResetDatabaseAsync();
        }
    }

    [Test]
    public async Task ExpectedUniqueIndexNameWithInvalidEnforcementState_IsRejectedWith55000()
    {
        await ResetDatabaseAsync();
        try
        {
            await PrepareLegacyRawSchemaAsync();
            await AppendAsAdminAsync(
                Guid.CreateVersion7(), Guid.CreateVersion7(), [Guid.CreateVersion7()], 1);
            string before = await ReadAuthorityValueBytesAsync();
            await ExecuteAdminAsync(
                "ALTER TABLE location_privacy_authority.erasure_intents " +
                "RENAME CONSTRAINT erasure_intents_intent_id_key TO ix_erasure_intents_intent_id; " +
                "SET allow_system_table_mods = on; " +
                "UPDATE pg_catalog.pg_index " +
                "SET indisvalid = false, indisready = false, indislive = false " +
                "WHERE indexrelid = 'location_privacy_authority.ix_erasure_intents_intent_id'::regclass");

            Exception failure = (await Assert.ThrowsAsync<Exception>(() => MigrateAuthorityAsync()))!;

            await Assert.That(FindPostgresException(failure)!.SqlState)
                .IsEqualTo(PostgresErrorCodes.ObjectNotInPrerequisiteState);
            await Assert.That(await AuthorityMigrationHistoryCountAsync()).IsEqualTo(0);
            await Assert.That(await ReadAuthorityValueBytesAsync()).IsEqualTo(before);
        }
        finally
        {
            await ResetDatabaseAsync();
        }
    }

    [Test]
    public async Task PrimaryKeyBackingIndexes_WithInvalidReadyOrLiveState_AreRejectedWith55000()
    {
        foreach (string table in new[] { "erasure_intents", "authority_counter" })
        {
            foreach (string stateColumn in new[] { "indisvalid", "indisready", "indislive" })
            {
                await AssertCatalogRejectedWithoutStateChangeAsync(
                    $"""
                    SET allow_system_table_mods = on;
                    UPDATE pg_catalog.pg_index
                    SET {stateColumn} = false
                    WHERE indexrelid = (
                        SELECT primary_key.conindid
                        FROM pg_catalog.pg_constraint AS primary_key
                        WHERE primary_key.conrelid =
                                  'location_privacy_authority.{table}'::regclass
                          AND primary_key.contype = 'p');
                    """);
            }
        }
    }

    [Test]
    public async Task CanonicalChecks_WithTautologicalDefinitions_AreRejectedWith55000()
    {
        foreach ((string table, string name, _) in CanonicalChecks)
        {
            await AssertCanonicalCheckRejectedAsync(
                $"""
                ALTER TABLE location_privacy_authority.{table} DROP CONSTRAINT {name};
                ALTER TABLE location_privacy_authority.{table}
                    ADD CONSTRAINT {name} CHECK (true);
                """);
        }
    }

    [Test]
    public async Task CanonicalChecks_WhenUnvalidated_AreRejectedWith55000()
    {
        foreach ((string table, string name, string expression) in CanonicalChecks)
        {
            await AssertCanonicalCheckRejectedAsync(
                $"""
                ALTER TABLE location_privacy_authority.{table} DROP CONSTRAINT {name};
                ALTER TABLE location_privacy_authority.{table}
                    ADD CONSTRAINT {name} CHECK ({expression}) NOT VALID;
                """);
        }
    }

    [Test]
    public async Task CanonicalChecks_WhenUnenforced_AreRejectedWith55000()
    {
        foreach ((string table, string name, string expression) in CanonicalChecks)
        {
            await AssertCanonicalCheckRejectedAsync(
                $"""
                ALTER TABLE location_privacy_authority.{table} DROP CONSTRAINT {name};
                ALTER TABLE location_privacy_authority.{table}
                    ADD CONSTRAINT {name} CHECK ({expression}) NOT ENFORCED;
                """);
        }
    }

    [Test]
    public async Task UnexpectedAuthorityCheck_IsRejectedWith55000()
    {
        await AssertCatalogRejectedWithoutStateChangeAsync(
            """
            ALTER TABLE location_privacy_authority.erasure_intents
                ADD CONSTRAINT ck_location_privacy_erasure_intents_unexpected CHECK (true);
            """);
    }

    [Test]
    public async Task UnrelatedAclGrantee_IsRejectedWith55000()
    {
        const string legacyRole = "orea140_legacy_acl_grantee";
        await ResetDatabaseAsync();
        try
        {
            await ExecuteAdminAsync($"CREATE ROLE {legacyRole} NOLOGIN");
            await ExecuteAdminAsync(await ReadFrozenHistoricalSqlAsync());
            await ExecuteAdminAsync(
                $"""
                GRANT USAGE ON SCHEMA location_privacy_authority TO {legacyRole};
                GRANT SELECT ON TABLE location_privacy_authority.erasure_intents TO {legacyRole};
                GRANT EXECUTE ON FUNCTION
                    location_privacy_authority.read_erasure_intents_after(bigint, integer)
                    TO {legacyRole};
                """);
            string before = await ReadAuthorityValueBytesAsync();

            await AssertMigrationRejectedAsync(before);
        }
        finally
        {
            await ResetDatabaseAsync();
            await ExecuteAdminAsync($"DROP ROLE IF EXISTS {legacyRole}");
        }
    }

    [Test]
    public async Task UnknownDefaultFunctionAclGrantee_IsRejectedWithoutAuthorityMutation()
    {
        const string legacyRole = "orea140_legacy_default_acl_grantee";
        await ResetDatabaseAsync();
        try
        {
            await ExecuteAdminAsync($"CREATE ROLE {legacyRole} NOLOGIN");
            await ExecuteAdminAsync(await ReadFrozenHistoricalSqlAsync());
            await AppendAsAdminAsync(
                Guid.CreateVersion7(), Guid.CreateVersion7(), [Guid.CreateVersion7()], 1);
            await ExecuteAdminAsync(
                "UPDATE location_privacy_authority.authority_counter SET last_sequence = 41 WHERE singleton");
            await ExecuteAdminAsync(
                $"""
                ALTER DEFAULT PRIVILEGES
                    FOR ROLE location_privacy_authority_owner
                    IN SCHEMA location_privacy_authority
                    GRANT EXECUTE ON FUNCTIONS TO {legacyRole};
                """);
            string before = await ReadAuthorityValueBytesAsync();
            string defaultAclBefore = await ReadDefaultFunctionAclAsync(legacyRole);

            Exception? failure = null;
            try
            {
                await MigrateAuthorityAsync();
            }
            catch (Exception exception)
            {
                failure = exception;
            }

            string outcome =
                $"{FindPostgresException(failure!)?.SqlState ?? "success"}|" +
                $"history={await AuthorityMigrationHistoryCountAsync()}|" +
                $"values-unchanged={await ReadAuthorityValueBytesAsync() == before}|" +
                $"poison-unchanged={await ReadDefaultFunctionAclAsync(legacyRole) == defaultAclBefore}";
            await Assert.That(outcome)
                .IsEqualTo("55000|history=0|values-unchanged=True|poison-unchanged=True");
        }
        finally
        {
            await ResetDatabaseAsync();
            await ExecuteAdminAsync($"DROP ROLE IF EXISTS {legacyRole}");
        }
    }

    [Test]
    public async Task RuntimeMembershipWithAdminOption_IsRejectedWith55000()
    {
        const string memberRole = "orea140_runtime_admin_member";
        await ResetDatabaseAsync();
        try
        {
            await ExecuteAdminAsync($"CREATE ROLE {memberRole} NOLOGIN");
            await ExecuteAdminAsync(await ReadFrozenHistoricalSqlAsync());
            await ExecuteAdminAsync(
                $"GRANT location_privacy_authority_runtime TO {memberRole} WITH ADMIN OPTION");
            string before = await ReadAuthorityValueBytesAsync();

            await AssertMigrationRejectedAsync(before);
        }
        finally
        {
            await ResetDatabaseAsync();
            await ExecuteAdminAsync(
                $"REVOKE location_privacy_authority_runtime FROM {memberRole}; DROP ROLE IF EXISTS {memberRole}");
        }
    }

    [Test]
    public async Task PopulatedCanonicalRawSchema_ConvergesCatalogWithoutChangingCounterAheadOfFacts()
    {
        await ResetDatabaseAsync();
        try
        {
            await PrepareLegacyRawSchemaAsync();
            await AppendAsAdminAsync(Guid.CreateVersion7(), Guid.CreateVersion7(), [Guid.CreateVersion7()], 1);
            await AppendAsAdminAsync(Guid.CreateVersion7(), Guid.CreateVersion7(), [Guid.CreateVersion7()], 2);
            await ExecuteAdminAsync(
                "UPDATE location_privacy_authority.authority_counter SET last_sequence = 41 WHERE singleton");
            string before = await ReadAuthorityValueBytesAsync();

            await MigrateAuthorityAsync();

            await Assert.That(await ReadAuthorityValueBytesAsync()).IsEqualTo(before);
            await Assert.That(await ReadCounterAsync()).IsEqualTo(41L);
            await Assert.That(await ReadPrimaryKeyNameAsync("erasure_intents"))
                .IsEqualTo("pk_erasure_intents");
            await Assert.That(await ReadPrimaryKeyNameAsync("authority_counter"))
                .IsEqualTo("pk_authority_counter");
            await Assert.That(await ReadIntentIdUniqueIndexNamesAsync())
                .IsEquivalentTo(["ix_erasure_intents_intent_id"]);
        }
        finally
        {
            await ResetDatabaseAsync();
        }
    }

    [Test]
    public async Task UnsafeNamedRoleAttributes_AreNormalizedToLeastPrivilege()
    {
        await ResetDatabaseAsync();
        await EnsureAuthorityRolesAsync();
        try
        {
            await ExecuteAdminAsync(
                "ALTER ROLE location_privacy_authority_owner WITH LOGIN SUPERUSER CREATEDB CREATEROLE INHERIT REPLICATION BYPASSRLS; " +
                "ALTER ROLE location_privacy_authority_runtime WITH LOGIN SUPERUSER CREATEDB CREATEROLE INHERIT REPLICATION BYPASSRLS;");

            await MigrateAuthorityAsync();

            await Assert.That(await ReadAuthorityRoleAttributesAsync())
                .IsEqualTo(
                    "location_privacy_authority_owner:false:false:false:false:false:false:false|" +
                    "location_privacy_authority_runtime:false:false:false:false:false:false:false");
        }
        finally
        {
            await NormalizeAuthorityRolesAsync();
            await ResetDatabaseAsync();
        }
    }

    [Test]
    public async Task RuntimeMembershipInOwnerRole_IsRejectedWith55000()
    {
        await ResetDatabaseAsync();
        await EnsureAuthorityRolesAsync();
        try
        {
            await ExecuteAdminAsync(
                "GRANT location_privacy_authority_owner TO location_privacy_authority_runtime");

            Exception failure = (await Assert.ThrowsAsync<Exception>(() => MigrateAuthorityAsync()))!;

            await Assert.That(FindPostgresException(failure)!.SqlState)
                .IsEqualTo(PostgresErrorCodes.ObjectNotInPrerequisiteState);
            await Assert.That(await AuthorityMigrationHistoryCountAsync()).IsEqualTo(0);
        }
        finally
        {
            await ExecuteAdminAsync(
                "REVOKE location_privacy_authority_owner FROM location_privacy_authority_runtime");
            await NormalizeAuthorityRolesAsync();
            await ResetDatabaseAsync();
        }
    }

    [Test]
    public async Task ExistingRuntimeFunctionPrivileges_AreNormalizedToIntendedBoundary()
    {
        await ResetDatabaseAsync();
        try
        {
            await PrepareLegacyRawSchemaAsync();
            await ExecuteAdminAsync(
                "GRANT EXECUTE ON FUNCTION " +
                "location_privacy_authority.append_erasure_intent(uuid, uuid, uuid[], smallint) " +
                "TO location_privacy_authority_runtime WITH GRANT OPTION; " +
                "GRANT EXECUTE ON FUNCTION " +
                "location_privacy_authority.read_erasure_intents_after(bigint, integer) " +
                "TO location_privacy_authority_runtime WITH GRANT OPTION; " +
                "GRANT EXECUTE ON FUNCTION " +
                "location_privacy_authority.reject_erasure_intent_mutation() " +
                "TO location_privacy_authority_runtime");

            await MigrateAuthorityAsync();

            await Assert.That(await ReadRuntimeFunctionPrivilegesAsync())
                .IsEqualTo(
                    "append_erasure_intent:EXECUTE:false|" +
                    "read_erasure_intents_after:EXECUTE:false");
        }
        finally
        {
            await ResetDatabaseAsync();
        }
    }

    [Test]
    public async Task RuntimeFunctions_EnforceAppendReadIdempotencyConcurrencyAndTableDenial()
    {
        await ResetDatabaseAsync();
        try
        {
            await using PrivacyErasureAuthorityDbContext context = CreateDbContext();
            IMigrator migrator = context.GetService<IMigrator>();
            await migrator.MigrateAsync(TargetMigration(context));
            await CreateRuntimeLoginAsync();

            Guid intentId = Guid.CreateVersion7();
            Guid ownerId = Guid.CreateVersion7();
            Guid firstLocationId = Guid.CreateVersion7();
            Guid secondLocationId = Guid.CreateVersion7();
            long first = await AppendAsRuntimeAsync(
                intentId, ownerId, [secondLocationId, firstLocationId, firstLocationId], 1);
            long duplicate = await AppendAsRuntimeAsync(
                intentId, ownerId, [firstLocationId, secondLocationId], 1);

            await Assert.That(duplicate).IsEqualTo(first);
            PostgresException mismatch = (await Assert.ThrowsAsync<PostgresException>(() =>
                AppendAsRuntimeAsync(intentId, Guid.CreateVersion7(), [firstLocationId], 1)))!;
            await Assert.That(mismatch.SqlState).IsEqualTo(PostgresErrorCodes.InvalidParameterValue);

            const int concurrentCount = 8;
            long[] sequences = await Task.WhenAll(Enumerable.Range(0, concurrentCount).Select(_ =>
                AppendAsRuntimeAsync(
                    Guid.CreateVersion7(),
                    Guid.CreateVersion7(),
                    [Guid.CreateVersion7()],
                    2)));
            await Assert.That(sequences.Order().ToArray())
                .IsEquivalentTo(Enumerable.Range(1, concurrentCount).Select(offset => first + offset));
            await Assert.That(await CountRuntimeReadAsync(0, 2)).IsEqualTo(2);

            PostgresException boundedRead = (await Assert.ThrowsAsync<PostgresException>(() =>
                CountRuntimeReadAsync(0, 501)))!;
            PostgresException directRead = (await Assert.ThrowsAsync<PostgresException>(() =>
                ExecuteRuntimeAsync("SELECT COUNT(*) FROM location_privacy_authority.erasure_intents")))!;
            PostgresException directInsert = (await Assert.ThrowsAsync<PostgresException>(() =>
                ExecuteRuntimeAsync(
                    "INSERT INTO location_privacy_authority.authority_counter (singleton, last_sequence) VALUES (true, 0)")))!;
            PostgresException directUpdate = (await Assert.ThrowsAsync<PostgresException>(() =>
                ExecuteRuntimeAsync("UPDATE location_privacy_authority.erasure_intents SET reason = 3")))!;
            PostgresException directDelete = (await Assert.ThrowsAsync<PostgresException>(() =>
                ExecuteRuntimeAsync("DELETE FROM location_privacy_authority.erasure_intents")))!;

            await Assert.That(boundedRead.SqlState).IsEqualTo(PostgresErrorCodes.InvalidParameterValue);
            foreach (PostgresException denial in new[] { directRead, directInsert, directUpdate, directDelete })
            {
                await Assert.That(denial.SqlState).IsEqualTo(PostgresErrorCodes.InsufficientPrivilege);
            }

            string beforeRollback = await ReadAuthorityValueBytesAsync();
            Exception rollback = (await Assert.ThrowsAsync<Exception>(() =>
                migrator.MigrateAsync(Migration.InitialDatabase)))!;
            await Assert.That(FindPostgresException(rollback)!.SqlState)
                .IsEqualTo(PostgresErrorCodes.ObjectNotInPrerequisiteState);
            await Assert.That(await ReadAuthorityValueBytesAsync()).IsEqualTo(beforeRollback);
        }
        finally
        {
            await DropRuntimeLoginAsync();
            await ResetDatabaseAsync();
        }
    }

    private async Task PrepareLegacyRawSchemaAsync() =>
        await ExecuteAdminAsync(await ReadFrozenHistoricalSqlAsync());

    private static Task<string> ReadFrozenHistoricalSqlAsync(
        [CallerFilePath] string sourceFile = "") =>
        File.ReadAllTextAsync(Path.Combine(
            Path.GetDirectoryName(sourceFile)!,
            "Fixtures",
            "LocationPrivacyAuthorityHistorical.sql"));

    private async Task AssertCanonicalCheckRejectedAsync(string poisonSql) =>
        await AssertCatalogRejectedWithoutStateChangeAsync(
            $"""
            ALTER TABLE location_privacy_authority.erasure_intents
                ADD CONSTRAINT ck_location_privacy_erasure_intents_sequence
                CHECK (authority_sequence > 0);
            {poisonSql}
            """);

    private async Task AssertCatalogRejectedWithoutStateChangeAsync(string poisonSql)
    {
        await ResetDatabaseAsync();
        try
        {
            await ExecuteAdminAsync(await ReadFrozenHistoricalSqlAsync());
            await AppendAsAdminAsync(Guid.CreateVersion7(), Guid.CreateVersion7(), [Guid.CreateVersion7()], 1);
            await ExecuteAdminAsync(
                "UPDATE location_privacy_authority.authority_counter SET last_sequence = 41 WHERE singleton");
            await ExecuteAdminAsync(poisonSql);
            string before = await ReadAuthorityValueBytesAsync();

            await AssertMigrationRejectedAsync(before);
        }
        finally
        {
            await ResetDatabaseAsync();
        }
    }

    private async Task AssertMigrationRejectedAsync(string before)
    {
        Exception failure = (await Assert.ThrowsAsync<Exception>(() => MigrateAuthorityAsync()))!;
        PostgresException postgres = FindPostgresException(failure)
            ?? throw new InvalidOperationException("Expected a PostgreSQL migration failure.", failure);

        await Assert.That(postgres.SqlState)
            .IsEqualTo(PostgresErrorCodes.ObjectNotInPrerequisiteState);
        await Assert.That(await AuthorityMigrationHistoryCountAsync()).IsEqualTo(0);
        await Assert.That(await ReadAuthorityValueBytesAsync()).IsEqualTo(before);
    }

    private PrivacyErasureAuthorityDbContext CreateDbContext()
    {
        var databaseIdentity = new NpgsqlConnectionStringBuilder(fixture.ConnectionString);
        if (databaseIdentity.Database?.StartsWith("recipient_delivery_migration_", StringComparison.Ordinal) is not true ||
            databaseIdentity.Host is not ("127.0.0.1" or "localhost"))
        {
            throw new InvalidOperationException("Refusing to use a non-disposable PostgreSQL database.");
        }

        var options = new DbContextOptionsBuilder<PrivacyErasureAuthorityDbContext>()
            .UseNpgsql(fixture.ConnectionString)
            .UseSnakeCaseNamingConvention()
            .Options;
        return new PrivacyErasureAuthorityDbContext(options);
    }

    private static string TargetMigration(PrivacyErasureAuthorityDbContext context) =>
        context.GetService<IMigrationsAssembly>().Migrations.Keys.Single(
            id => id.EndsWith($"_{MigrationName}", StringComparison.Ordinal));

    private async Task MigrateAuthorityAsync()
    {
        await using PrivacyErasureAuthorityDbContext context = CreateDbContext();
        await context.GetService<IMigrator>().MigrateAsync(TargetMigration(context));
    }

    private async Task ResetDatabaseAsync() => await ExecuteAdminAsync(
        "DROP SCHEMA IF EXISTS location_privacy_authority CASCADE; " +
        "DROP SCHEMA public CASCADE; CREATE SCHEMA public;");

    private async Task ExecuteAdminAsync(string sql)
    {
        await using var connection = new NpgsqlConnection(fixture.ConnectionString);
        await connection.OpenAsync();
        await using var command = new NpgsqlCommand(sql, connection);
        await command.ExecuteNonQueryAsync();
    }

    private async Task<bool> RelationExistsAsync(string qualifiedName)
    {
        await using var connection = new NpgsqlConnection(fixture.ConnectionString);
        await connection.OpenAsync();
        await using var command = new NpgsqlCommand("SELECT to_regclass(@name) IS NOT NULL", connection);
        command.Parameters.AddWithValue("name", qualifiedName);
        return (bool)(await command.ExecuteScalarAsync())!;
    }

    private async Task<bool> HistoryContainsAsync(string migrationId)
    {
        if (!await RelationExistsAsync("public.\"__EFMigrationsHistory\""))
        {
            return false;
        }

        await using var connection = new NpgsqlConnection(fixture.ConnectionString);
        await connection.OpenAsync();
        await using var command = new NpgsqlCommand(
            "SELECT EXISTS (SELECT 1 FROM \"__EFMigrationsHistory\" WHERE migration_id = @id)",
            connection);
        command.Parameters.AddWithValue("id", migrationId);
        return (bool)(await command.ExecuteScalarAsync())!;
    }

    private async Task<long> ReadCounterAsync()
    {
        await using var connection = new NpgsqlConnection(fixture.ConnectionString);
        await connection.OpenAsync();
        await using var command = new NpgsqlCommand(
            "SELECT last_sequence FROM location_privacy_authority.authority_counter WHERE singleton",
            connection);
        return (long)(await command.ExecuteScalarAsync())!;
    }

    private async Task<int> AuthorityMigrationHistoryCountAsync()
    {
        if (!await RelationExistsAsync("public.\"__EFMigrationsHistory\""))
        {
            return 0;
        }

        await using var connection = new NpgsqlConnection(fixture.ConnectionString);
        await connection.OpenAsync();
        await using var command = new NpgsqlCommand(
            "SELECT COUNT(*)::integer FROM \"__EFMigrationsHistory\" WHERE migration_id LIKE '%_InitialLocationPrivacyAuthority'",
            connection);
        return (int)(await command.ExecuteScalarAsync())!;
    }

    private async Task<string> ReadPrimaryKeyNameAsync(string tableName)
    {
        await using var connection = new NpgsqlConnection(fixture.ConnectionString);
        await connection.OpenAsync();
        await using var command = new NpgsqlCommand(
            """
            SELECT constraint_entry.conname
            FROM pg_catalog.pg_constraint AS constraint_entry
            JOIN pg_catalog.pg_class AS table_entry ON table_entry.oid = constraint_entry.conrelid
            JOIN pg_catalog.pg_namespace AS schema_entry ON schema_entry.oid = table_entry.relnamespace
            WHERE schema_entry.nspname = 'location_privacy_authority'
              AND table_entry.relname = @table_name
              AND constraint_entry.contype = 'p'
            """,
            connection);
        command.Parameters.AddWithValue("table_name", tableName);
        return (string?)await command.ExecuteScalarAsync() ?? "<none>";
    }

    private async Task<string[]> ReadIntentIdUniqueIndexNamesAsync()
    {
        await using var connection = new NpgsqlConnection(fixture.ConnectionString);
        await connection.OpenAsync();
        await using var command = new NpgsqlCommand(
            """
            SELECT index_entry.relname
            FROM pg_catalog.pg_index AS index_catalog
            JOIN pg_catalog.pg_class AS table_entry ON table_entry.oid = index_catalog.indrelid
            JOIN pg_catalog.pg_namespace AS schema_entry ON schema_entry.oid = table_entry.relnamespace
            JOIN pg_catalog.pg_class AS index_entry ON index_entry.oid = index_catalog.indexrelid
            WHERE schema_entry.nspname = 'location_privacy_authority'
              AND table_entry.relname = 'erasure_intents'
              AND index_catalog.indisunique
              AND NOT index_catalog.indisprimary
              AND index_catalog.indpred IS NULL
              AND index_catalog.indexprs IS NULL
              AND ARRAY(
                    SELECT attribute_entry.attname
                    FROM unnest(index_catalog.indkey::smallint[]) WITH ORDINALITY AS key_entry(attnum, ordinal)
                    JOIN pg_catalog.pg_attribute AS attribute_entry
                      ON attribute_entry.attrelid = table_entry.oid
                     AND attribute_entry.attnum = key_entry.attnum
                    ORDER BY key_entry.ordinal) = ARRAY['intent_id']::name[]
            ORDER BY index_entry.relname
            """,
            connection);
        await using NpgsqlDataReader reader = await command.ExecuteReaderAsync();
        var names = new List<string>();
        while (await reader.ReadAsync())
        {
            names.Add(reader.GetString(0));
        }

        return names.ToArray();
    }

    private async Task<string> ReadAuthorityRoleAttributesAsync()
    {
        await using var connection = new NpgsqlConnection(fixture.ConnectionString);
        await connection.OpenAsync();
        await using var command = new NpgsqlCommand(
            """
            SELECT string_agg(
                role_entry.rolname || ':' || role_entry.rolcanlogin || ':' || role_entry.rolsuper || ':' ||
                role_entry.rolcreatedb || ':' || role_entry.rolcreaterole || ':' || role_entry.rolinherit || ':' ||
                role_entry.rolreplication || ':' || role_entry.rolbypassrls,
                '|' ORDER BY role_entry.rolname)
            FROM pg_catalog.pg_roles AS role_entry
            WHERE role_entry.rolname IN (
                'location_privacy_authority_owner',
                'location_privacy_authority_runtime')
            """,
            connection);
        return (string)(await command.ExecuteScalarAsync())!;
    }

    private async Task<string> ReadRuntimeFunctionPrivilegesAsync()
    {
        await using var connection = new NpgsqlConnection(fixture.ConnectionString);
        await connection.OpenAsync();
        await using var command = new NpgsqlCommand(
            """
            SELECT string_agg(
                function_entry.proname || ':' || privilege_entry.privilege_type || ':' ||
                privilege_entry.is_grantable,
                '|' ORDER BY function_entry.proname)
            FROM pg_catalog.pg_proc AS function_entry
            JOIN pg_catalog.pg_namespace AS schema_entry
              ON schema_entry.oid = function_entry.pronamespace
            CROSS JOIN LATERAL pg_catalog.aclexplode(
                COALESCE(
                    function_entry.proacl,
                    pg_catalog.acldefault('f', function_entry.proowner))) AS privilege_entry
            JOIN pg_catalog.pg_roles AS grantee_role
              ON grantee_role.oid = privilege_entry.grantee
            WHERE schema_entry.nspname = 'location_privacy_authority'
              AND grantee_role.rolname = 'location_privacy_authority_runtime'
            """,
            connection);
        return (string)(await command.ExecuteScalarAsync())!;
    }

    private async Task<string> ReadAuthorityDefaultAclContractAsync()
    {
        await using var connection = new NpgsqlConnection(fixture.ConnectionString);
        await connection.OpenAsync();
        await using var command = new NpgsqlCommand(
            """
            SELECT string_agg(
                owner_role.rolname || ':' ||
                COALESCE(schema_entry.nspname, '<global>') || ':' ||
                default_acl.defaclobjtype::text || ':' ||
                CASE
                    WHEN privilege_entry.grantee = 0 THEN 'PUBLIC'
                    ELSE grantee_role.rolname
                END || ':' ||
                privilege_entry.privilege_type || ':' || privilege_entry.is_grantable,
                '|' ORDER BY owner_role.rolname, schema_entry.nspname,
                    default_acl.defaclobjtype, privilege_entry.grantee,
                    privilege_entry.privilege_type)
            FROM pg_catalog.pg_default_acl AS default_acl
            JOIN pg_catalog.pg_roles AS owner_role ON owner_role.oid = default_acl.defaclrole
            LEFT JOIN pg_catalog.pg_namespace AS schema_entry
              ON schema_entry.oid = default_acl.defaclnamespace
            CROSS JOIN LATERAL pg_catalog.aclexplode(default_acl.defaclacl) AS privilege_entry
            LEFT JOIN pg_catalog.pg_roles AS grantee_role ON grantee_role.oid = privilege_entry.grantee
            WHERE owner_role.rolname IN (
                    'location_privacy_authority_owner',
                    'location_privacy_authority_runtime')
               OR schema_entry.nspname = 'location_privacy_authority'
            """,
            connection);
        return (string)(await command.ExecuteScalarAsync())!;
    }

    private async Task<string> ReadDefaultFunctionAclAsync(string grantee)
    {
        await using var connection = new NpgsqlConnection(fixture.ConnectionString);
        await connection.OpenAsync();
        await using var command = new NpgsqlCommand(
            """
            SELECT string_agg(
                owner_role.rolname || ':' || schema_entry.nspname || ':' ||
                default_acl.defaclobjtype::text || ':' || grantee_role.rolname || ':' ||
                privilege_entry.privilege_type || ':' || privilege_entry.is_grantable,
                '|' ORDER BY grantee_role.rolname, privilege_entry.privilege_type)
            FROM pg_catalog.pg_default_acl AS default_acl
            JOIN pg_catalog.pg_roles AS owner_role ON owner_role.oid = default_acl.defaclrole
            JOIN pg_catalog.pg_namespace AS schema_entry ON schema_entry.oid = default_acl.defaclnamespace
            CROSS JOIN LATERAL pg_catalog.aclexplode(default_acl.defaclacl) AS privilege_entry
            JOIN pg_catalog.pg_roles AS grantee_role ON grantee_role.oid = privilege_entry.grantee
            WHERE owner_role.rolname = 'location_privacy_authority_owner'
              AND schema_entry.nspname = 'location_privacy_authority'
              AND default_acl.defaclobjtype = 'f'
              AND grantee_role.rolname = @grantee
            """,
            connection);
        command.Parameters.AddWithValue("grantee", grantee);
        return (string)(await command.ExecuteScalarAsync())!;
    }

    private async Task<string> ReadAuthorityValueBytesAsync()
    {
        await using var connection = new NpgsqlConnection(fixture.ConnectionString);
        await connection.OpenAsync();
        await using var command = new NpgsqlCommand(
            """
            SELECT encode(convert_to(jsonb_build_object(
                'facts', COALESCE((
                    SELECT jsonb_agg(to_jsonb(retained) ORDER BY retained.authority_sequence)
                    FROM location_privacy_authority.erasure_intents AS retained), '[]'::jsonb),
                'counter', (
                    SELECT to_jsonb(counter)
                    FROM location_privacy_authority.authority_counter AS counter
                    WHERE counter.singleton))::text, 'UTF8'), 'hex')
            """,
            connection);
        return (string)(await command.ExecuteScalarAsync())!;
    }

    private async Task<string> ReadAuthorityStateBytesAsync()
    {
        await using var connection = new NpgsqlConnection(fixture.ConnectionString);
        await connection.OpenAsync();
        await using var command = new NpgsqlCommand(
            """
            SELECT encode(convert_to(jsonb_build_object(
                'facts', COALESCE((
                    SELECT jsonb_agg(to_jsonb(retained) ORDER BY retained.authority_sequence)
                    FROM location_privacy_authority.erasure_intents AS retained), '[]'::jsonb),
                'counter', (
                    SELECT to_jsonb(counter)
                    FROM location_privacy_authority.authority_counter AS counter
                    WHERE counter.singleton),
                'history', COALESCE((
                    SELECT jsonb_agg(to_jsonb(history) ORDER BY history.migration_id)
                    FROM "__EFMigrationsHistory" AS history), '[]'::jsonb))::text, 'UTF8'), 'hex')
            """,
            connection);
        return (string)(await command.ExecuteScalarAsync())!;
    }

    private async Task<long> AppendAsAdminAsync(
        Guid intentId,
        Guid ownerId,
        Guid[] locationIds,
        short reason) => await AppendAsync(fixture.ConnectionString, intentId, ownerId, locationIds, reason);

    private async Task<long> AppendAsRuntimeAsync(
        Guid intentId,
        Guid ownerId,
        Guid[] locationIds,
        short reason) => await AppendAsync(RuntimeConnectionString(), intentId, ownerId, locationIds, reason);

    private static async Task<long> AppendAsync(
        string connectionString,
        Guid intentId,
        Guid ownerId,
        Guid[] locationIds,
        short reason)
    {
        await using var connection = new NpgsqlConnection(connectionString);
        await connection.OpenAsync();
        await using var command = new NpgsqlCommand(
            "SELECT authority_sequence FROM location_privacy_authority.append_erasure_intent(@intent_id, @owner_id, @location_ids, @reason)",
            connection);
        command.Parameters.AddWithValue("intent_id", NpgsqlDbType.Uuid, intentId);
        command.Parameters.AddWithValue("owner_id", NpgsqlDbType.Uuid, ownerId);
        command.Parameters.AddWithValue("location_ids", NpgsqlDbType.Array | NpgsqlDbType.Uuid, locationIds);
        command.Parameters.AddWithValue("reason", NpgsqlDbType.Smallint, reason);
        return (long)(await command.ExecuteScalarAsync())!;
    }

    private async Task<int> CountRuntimeReadAsync(long after, int limit)
    {
        await using var connection = new NpgsqlConnection(RuntimeConnectionString());
        await connection.OpenAsync();
        await using var command = new NpgsqlCommand(
            "SELECT COUNT(*)::integer FROM location_privacy_authority.read_erasure_intents_after(@after, @limit)",
            connection);
        command.Parameters.AddWithValue("after", after);
        command.Parameters.AddWithValue("limit", limit);
        return (int)(await command.ExecuteScalarAsync())!;
    }

    private async Task ExecuteRuntimeAsync(string sql)
    {
        await using var connection = new NpgsqlConnection(RuntimeConnectionString());
        await connection.OpenAsync();
        await using var command = new NpgsqlCommand(sql, connection);
        await command.ExecuteNonQueryAsync();
    }

    private async Task CreateRuntimeLoginAsync() => await ExecuteAdminAsync(
        $"""
        DO $block$
        BEGIN
            IF NOT EXISTS (SELECT 1 FROM pg_roles WHERE rolname = '{RuntimeUsername}') THEN
                CREATE ROLE {RuntimeUsername} LOGIN PASSWORD '{RuntimePassword}';
            ELSE
                ALTER ROLE {RuntimeUsername} LOGIN PASSWORD '{RuntimePassword}';
            END IF;
        END;
        $block$;
        GRANT location_privacy_authority_runtime TO {RuntimeUsername};
        """);

    private async Task EnsureAuthorityRolesAsync() => await ExecuteAdminAsync(
        """
        DO $block$
        BEGIN
            IF NOT EXISTS (SELECT 1 FROM pg_roles WHERE rolname = 'location_privacy_authority_owner') THEN
                CREATE ROLE location_privacy_authority_owner NOLOGIN;
            END IF;
            IF NOT EXISTS (SELECT 1 FROM pg_roles WHERE rolname = 'location_privacy_authority_runtime') THEN
                CREATE ROLE location_privacy_authority_runtime NOLOGIN;
            END IF;
        END;
        $block$;
        """);

    private async Task NormalizeAuthorityRolesAsync() => await ExecuteAdminAsync(
        """
        ALTER ROLE location_privacy_authority_owner
            WITH NOLOGIN NOSUPERUSER NOCREATEDB NOCREATEROLE NOINHERIT NOREPLICATION NOBYPASSRLS;
        ALTER ROLE location_privacy_authority_runtime
            WITH NOLOGIN NOSUPERUSER NOCREATEDB NOCREATEROLE NOINHERIT NOREPLICATION NOBYPASSRLS;
        """);

    private async Task DropRuntimeLoginAsync()
    {
        await ExecuteAdminAsync(
            $"""
            DO $block$
            BEGIN
                IF EXISTS (SELECT 1 FROM pg_roles WHERE rolname = '{RuntimeUsername}') THEN
                    REVOKE location_privacy_authority_runtime FROM {RuntimeUsername};
                    DROP ROLE {RuntimeUsername};
                END IF;
            END;
            $block$;
            """);
    }

    private string RuntimeConnectionString() => new NpgsqlConnectionStringBuilder(fixture.ConnectionString)
    {
        Username = RuntimeUsername,
        Password = RuntimePassword
    }.ConnectionString;

    private static PostgresException? FindPostgresException(Exception exception)
    {
        for (Exception? current = exception; current is not null; current = current.InnerException)
        {
            if (current is PostgresException postgres)
            {
                return postgres;
            }
        }

        return null;
    }
}
