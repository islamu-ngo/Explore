// ABOUTME: Rehearses fanout audience execution schema changes in an isolated PostgreSQL Testcontainer.
// ABOUTME: Proves registration cutoff backfill, fenced-run constraints, filtered uniqueness, and reversibility.

using Event.Persistence.IntegrationTests.Fixtures;
using Explore.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Migrations.Operations;
using Npgsql;

namespace Event.Persistence.IntegrationTests.Migrations;

[ClassDataSource<RecipientDeliveryMigrationContainerFixture>(Shared = SharedType.PerAssembly)]
[NotInParallel("RecipientDeliveryMigrationDb")]
public sealed class NotificationFanoutAudienceMigrationTests(
    RecipientDeliveryMigrationContainerFixture fixture)
{
    private const string PreviousMigration = "20260717160935_AddNotificationFanoutOccurrences";
    private const string TargetMigration = "20260717165523_AddNotificationFanoutAudienceExecution";
    private static readonly Guid TenantId = Guid.Parse("019f6d35-9000-7000-8000-000000000001");
    private static readonly Guid UserId = Guid.Parse("019f6d35-9000-7000-8000-000000000002");
    private static readonly Guid ActorId = Guid.Parse("019f6d35-9000-7000-8000-000000000003");
    private static readonly Guid EventId = Guid.Parse("019f6d35-9000-7000-8000-000000000004");
    private static readonly Guid SessionId = Guid.Parse("019f6d35-9000-7000-8000-000000000005");
    private static readonly Guid RegistrationId = Guid.Parse("019f6d35-9000-7000-8000-000000000006");
    private static readonly Guid OccurrenceId = Guid.Parse("019f6d35-9000-7000-8000-000000000007");
    private static readonly Guid OtherOccurrenceId = Guid.Parse("019f6d35-9000-7000-8000-000000000008");
    private static readonly DateTime RegistrationCreatedAt = new(2026, 7, 17, 14, 30, 0, DateTimeKind.Utc);

    [Test]
    public async Task UpDownUp_BackfillsCoverageAndEnforcesFanoutRunExecutionShape()
    {
        var databaseIdentity = new NpgsqlConnectionStringBuilder(fixture.ConnectionString);
        await Assert.That(databaseIdentity.Database).StartsWith("recipient_delivery_migration_");
        await Assert.That(databaseIdentity.Host is "127.0.0.1" or "localhost").IsTrue();

        await using var context = CreateDbContext();
        IMigrator migrator = context.GetService<IMigrator>();

        try
        {
            await migrator.MigrateAsync(PreviousMigration);
            await SeedPreMigrationGraphAsync();

            await migrator.MigrateAsync(TargetMigration);
            await Assert.That(ReadPendingModelOperations(context)).IsEmpty();
            await AssertUpShapeAsync();
            await AssertFanoutRunConstraintsAsync();
            await SeedDowngradeFanoutRunsAsync();

            await migrator.MigrateAsync(PreviousMigration);
            await AssertDownShapeAsync();

            await migrator.MigrateAsync(TargetMigration);
            await AssertUpShapeAsync();
            await AssertDowngradeLegacyRunPreservedAsync();
        }
        finally
        {
            await ResetSharedMigrationDatabaseAsync();
        }
    }

    private ExploreDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<ExploreDbContext>()
            .UseNpgsql(fixture.ConnectionString)
            .UseSnakeCaseNamingConvention()
            .ConfigureWarnings(warnings => warnings.Ignore(RelationalEventId.PendingModelChangesWarning))
            .Options;
        return new ExploreDbContext(options);
    }

    private async Task SeedPreMigrationGraphAsync()
    {
        await using var connection = new NpgsqlConnection(fixture.ConnectionString);
        await connection.OpenAsync();
        await using var command = connection.CreateCommand();
        command.CommandText = $"""
            INSERT INTO tenant_statuses (id, master_code, full_name, is_active_state)
            VALUES (1, 'ACTIVE', 'Active', true)
            ON CONFLICT (id) DO NOTHING;
            INSERT INTO tenants (id, tenant_status_id, full_name, slug, created_at)
            VALUES ('{TenantId}', 1, 'Fanout migration tenant', 'fanout-audience-migration', '{RegistrationCreatedAt:O}');

            INSERT INTO users (id, concurrency_stamp, created_at, is_deleted, email_verified)
            VALUES ('{UserId}', '019f6d35-9000-7000-8000-000000000009', '{RegistrationCreatedAt:O}', false, true);
            INSERT INTO actor_types (id, master_code, full_name)
            VALUES (1, 'USER', 'User')
            ON CONFLICT (id) DO NOTHING;
            INSERT INTO actors
                (id, actor_type_id, concurrency_stamp, created_at, is_deleted, tenant_id, user_id)
            VALUES
                ('{ActorId}', 1, '019f6d35-9000-7000-8000-00000000000a',
                 '{RegistrationCreatedAt:O}', false, '{TenantId}', '{UserId}');

            INSERT INTO event_formats (id, master_code, full_name)
            VALUES (1, 'IN_PERSON', 'In person')
            ON CONFLICT (id) DO NOTHING;
            INSERT INTO event_statuses (id, master_code, full_name)
            VALUES (1, 'DRAFT', 'Draft')
            ON CONFLICT (id) DO NOTHING;
            INSERT INTO visibility_types (id, master_code, full_name)
            VALUES (1, 'PUBLIC', 'Public')
            ON CONFLICT (id) DO NOTHING;
            INSERT INTO events
                (id, actor_id, concurrency_stamp, created_at, event_format_id, event_status_id,
                 is_deleted, is_registration_required, public_code, tenant_id, title, visibility_type_id)
            VALUES
                ('{EventId}', '{ActorId}', '019f6d35-9000-7000-8000-00000000000b',
                 '{RegistrationCreatedAt:O}', 1, 1, false, true, 'FANOUT01', '{TenantId}',
                 'Fanout migration event', 1);

            INSERT INTO event_session_statuses (id, master_code, full_name)
            VALUES (1, 'DRAFT', 'Draft')
            ON CONFLICT (id) DO NOTHING;
            INSERT INTO event_sessions
                (id, concurrency_stamp, created_at, event_id, event_session_status_id,
                 is_deleted, sort_order, tenant_id)
            VALUES
                ('{SessionId}', '019f6d35-9000-7000-8000-00000000000c',
                 '{RegistrationCreatedAt:O}', '{EventId}', 1, false, 1, '{TenantId}');
            INSERT INTO event_registrations
                (id, concurrency_stamp, event_id, user_id, event_session_id, tenant_id, created_at, is_deleted)
            VALUES
                ('{RegistrationId}', '019f6d35-9000-7000-8000-00000000000d',
                 '{EventId}', '{UserId}', '{SessionId}', '{TenantId}', '{RegistrationCreatedAt:O}', false);

            INSERT INTO notification_entity_types (id, master_code, full_name)
            VALUES (1, 'EVENT', 'Event')
            ON CONFLICT (id) DO NOTHING;
            INSERT INTO notification_fanout_occurrences
                (id, tenant_id, event_id, occurred_at, audience_cutoff_at, aggregate_version,
                 change_set_json, safe_before_snapshot_json, safe_after_snapshot_json,
                 template_key, template_version, delivery_policy_id, policy_version, priority,
                 not_before, source_type, source_id, coalescing_key, state)
            VALUES
                ('{OccurrenceId}', '{TenantId}', '{EventId}', '{RegistrationCreatedAt:O}',
                 '{RegistrationCreatedAt:O}', '019f6d35-9000-7000-8000-00000000000e',
                 'null', 'null', 'null', 'migration.fanout', 1, 1, 1, 10,
                 '{RegistrationCreatedAt:O}', 'migration_test', '{EventId}', 'migration-fanout-1', 1),
                ('{OtherOccurrenceId}', '{TenantId}', '{EventId}', '{RegistrationCreatedAt:O}',
                 '{RegistrationCreatedAt:O}', '019f6d35-9000-7000-8000-00000000000f',
                 'null', 'null', 'null', 'migration.fanout', 1, 1, 1, 10,
                 '{RegistrationCreatedAt:O}', 'migration_test', '{EventId}', 'migration-fanout-2', 1);
            """;
        await command.ExecuteNonQueryAsync();
    }

    private async Task AssertUpShapeAsync()
    {
        await using var connection = new NpgsqlConnection(fixture.ConnectionString);
        await connection.OpenAsync();

        await Assert.That(await ReadScalarAsync<DateTime>(connection, $"""
            SELECT coverage_established_at
            FROM event_registrations
            WHERE id = '{RegistrationId}'
            """)).IsEqualTo(RegistrationCreatedAt);
        await Assert.That(await ExistsAsync(connection, """
            SELECT EXISTS (
                SELECT 1
                FROM information_schema.columns
                WHERE table_schema = 'public'
                  AND table_name = 'event_registrations'
                  AND column_name = 'coverage_established_at'
                  AND is_nullable = 'NO'
                  AND column_default = 'now()')
            """)).IsTrue();
        await Assert.That(await ConstraintExistsAsync(connection, "fk_fanout_runs_occurrence_tenant")).IsTrue();
        await Assert.That(await ConstraintExistsAsync(connection, "ck_notification_fanout_runs_cursor_pair")).IsTrue();
        await Assert.That(await ConstraintExistsAsync(connection, "ck_notification_fanout_runs_occurrence_lease")).IsTrue();
        await Assert.That(await IndexExistsAsync(connection, "ux_notification_fanout_runs_occurrence")).IsTrue();

        string sourceIndex = await ReadScalarAsync<string>(connection, """
            SELECT pg_get_indexdef(indexrelid)
            FROM pg_index
            WHERE indexrelid = 'ux_notification_fanout_runs_source'::regclass
            """);
        await Assert.That(sourceIndex).Contains("WHERE (fanout_occurrence_id IS NULL)");
    }

    private async Task AssertDownShapeAsync()
    {
        await using var connection = new NpgsqlConnection(fixture.ConnectionString);
        await connection.OpenAsync();

        await Assert.That(await ColumnExistsAsync(connection, "event_registrations", "coverage_established_at")).IsFalse();
        foreach (string column in new[]
                 {
                     "fanout_occurrence_id",
                     "cursor_first_eligible_registration_created_at",
                     "cursor_user_id",
                     "processing_lease_owner",
                     "processing_lease_token",
                     "processing_lease_expires_at",
                     "processing_generation",
                     "processing_fence",
                     "heartbeat_at"
                 })
        {
            await Assert.That(await ColumnExistsAsync(connection, "notification_fanout_runs", column)).IsFalse();
        }

        await Assert.That(await ConstraintExistsAsync(connection, "fk_fanout_runs_occurrence_tenant")).IsFalse();
        await Assert.That(await ConstraintExistsAsync(connection, "ck_notification_fanout_runs_cursor_pair")).IsFalse();
        await Assert.That(await ConstraintExistsAsync(connection, "ck_notification_fanout_runs_occurrence_lease")).IsFalse();
        await Assert.That(await IndexExistsAsync(connection, "ux_notification_fanout_runs_occurrence")).IsFalse();

        string sourceIndex = await ReadScalarAsync<string>(connection, """
            SELECT pg_get_indexdef(indexrelid)
            FROM pg_index
            WHERE indexrelid = 'ux_notification_fanout_runs_source'::regclass
            """);
        await Assert.That(sourceIndex).DoesNotContain("WHERE");
        await AssertDowngradeLegacyRunPreservedAsync();
    }

    private async Task SeedDowngradeFanoutRunsAsync()
    {
        await using var connection = new NpgsqlConnection(fixture.ConnectionString);
        await connection.OpenAsync();
        await using var command = connection.CreateCommand();
        command.CommandText = $"""
            INSERT INTO notification_fanout_runs
                (id, tenant_id, fanout_kind, notification_entity_type_id, entity_id, source_actor_id,
                 status, processed_count, created_notification_count, created_at, concurrency_stamp,
                 fanout_occurrence_id)
            VALUES
                ('019f6d35-9000-7000-8000-000000000023', '{TenantId}', 'downgrade_source',
                 1, '{EventId}', '{ActorId}', 'pending', 0, 0, '{RegistrationCreatedAt:O}',
                 '019f6d35-9000-7000-8000-000000000024', NULL),
                ('019f6d35-9000-7000-8000-000000000025', '{TenantId}', 'downgrade_source',
                 1, '{EventId}', '{ActorId}', 'pending', 0, 0, '{RegistrationCreatedAt:O}',
                 '019f6d35-9000-7000-8000-000000000026', '{OccurrenceId}'),
                ('019f6d35-9000-7000-8000-000000000027', '{TenantId}', 'downgrade_source',
                 1, '{EventId}', '{ActorId}', 'pending', 0, 0, '{RegistrationCreatedAt:O}',
                 '019f6d35-9000-7000-8000-000000000028', '{OtherOccurrenceId}');
            """;
        await Assert.That(await command.ExecuteNonQueryAsync()).IsEqualTo(3);
    }

    private async Task AssertDowngradeLegacyRunPreservedAsync()
    {
        await using var connection = new NpgsqlConnection(fixture.ConnectionString);
        await connection.OpenAsync();
        await Assert.That(await ReadScalarAsync<long>(connection, """
            SELECT COUNT(*)
            FROM notification_fanout_runs
            WHERE fanout_kind = 'downgrade_source'
            """)).IsEqualTo(1);
        await Assert.That(await ReadScalarAsync<Guid>(connection, """
            SELECT id
            FROM notification_fanout_runs
            WHERE fanout_kind = 'downgrade_source'
            """)).IsEqualTo(Guid.Parse("019f6d35-9000-7000-8000-000000000023"));
    }

    private async Task AssertFanoutRunConstraintsAsync()
    {
        await using var connection = new NpgsqlConnection(fixture.ConnectionString);
        await connection.OpenAsync();

        string baseColumns = """
            id, tenant_id, fanout_kind, notification_entity_type_id, entity_id, source_actor_id,
            status, processed_count, created_notification_count, created_at, concurrency_stamp
            """;
        string sourceTuple = $"'event_change', 1, '{EventId}', '{ActorId}'";

        await AssertViolationAsync(
            connection,
            $"""
             INSERT INTO notification_fanout_runs
                 ({baseColumns}, cursor_first_eligible_registration_created_at)
             VALUES
                 ('019f6d35-9000-7000-8000-000000000010', '{TenantId}', {sourceTuple},
                  'pending', 0, 0, '{RegistrationCreatedAt:O}',
                  '019f6d35-9000-7000-8000-000000000011', '{RegistrationCreatedAt:O}');
             """,
            PostgresErrorCodes.CheckViolation,
            "ck_notification_fanout_runs_cursor_pair");

        await AssertViolationAsync(
            connection,
            $"""
             INSERT INTO notification_fanout_runs
                 ({baseColumns}, fanout_occurrence_id)
             VALUES
                 ('019f6d35-9000-7000-8000-000000000012', '{TenantId}', {sourceTuple},
                  'processing', 0, 0, '{RegistrationCreatedAt:O}',
                  '019f6d35-9000-7000-8000-000000000013', '{OccurrenceId}');
             """,
            PostgresErrorCodes.CheckViolation,
            "ck_notification_fanout_runs_occurrence_lease");

        await AssertViolationAsync(
            connection,
            $"""
             INSERT INTO notification_fanout_runs
                 ({baseColumns}, fanout_occurrence_id)
             VALUES
                 ('019f6d35-9000-7000-8000-000000000014', '{TenantId}', {sourceTuple},
                  'pending', 0, 0, '{RegistrationCreatedAt:O}',
                  '019f6d35-9000-7000-8000-000000000015',
                  '019f6d35-9000-7000-8000-0000000000ff');
             """,
            PostgresErrorCodes.ForeignKeyViolation,
            "fk_fanout_runs_occurrence_tenant");

        await AssertViolationAsync(
            connection,
            $"""
             INSERT INTO notification_fanout_runs
                 ({baseColumns}, fanout_occurrence_id)
             VALUES
                 ('019f6d35-9000-7000-8000-000000000016', '{TenantId}', {sourceTuple},
                  'pending', 0, 0, '{RegistrationCreatedAt:O}',
                  '019f6d35-9000-7000-8000-000000000017', '{OccurrenceId}'),
                 ('019f6d35-9000-7000-8000-000000000018', '{TenantId}',
                  'event_change_2', 1, '019f6d35-9000-7000-8000-000000000019', '{ActorId}',
                  'pending', 0, 0, '{RegistrationCreatedAt:O}',
                  '019f6d35-9000-7000-8000-00000000001a', '{OccurrenceId}');
             """,
            PostgresErrorCodes.UniqueViolation,
            "ux_notification_fanout_runs_occurrence");

        await AssertSucceedsAndRollbackAsync(
            connection,
            $"""
             INSERT INTO notification_fanout_runs
                 ({baseColumns}, fanout_occurrence_id)
             VALUES
                 ('019f6d35-9000-7000-8000-00000000001b', '{TenantId}', {sourceTuple},
                  'pending', 0, 0, '{RegistrationCreatedAt:O}',
                  '019f6d35-9000-7000-8000-00000000001c', '{OccurrenceId}'),
                 ('019f6d35-9000-7000-8000-00000000001d', '{TenantId}', {sourceTuple},
                  'pending', 0, 0, '{RegistrationCreatedAt:O}',
                  '019f6d35-9000-7000-8000-00000000001e', '{OtherOccurrenceId}');
             """);

        await AssertViolationAsync(
            connection,
            $"""
             INSERT INTO notification_fanout_runs ({baseColumns})
             VALUES
                 ('019f6d35-9000-7000-8000-00000000001f', '{TenantId}', {sourceTuple},
                  'pending', 0, 0, '{RegistrationCreatedAt:O}',
                  '019f6d35-9000-7000-8000-000000000020'),
                 ('019f6d35-9000-7000-8000-000000000021', '{TenantId}', {sourceTuple},
                  'pending', 0, 0, '{RegistrationCreatedAt:O}',
                  '019f6d35-9000-7000-8000-000000000022');
             """,
            PostgresErrorCodes.UniqueViolation,
            "ux_notification_fanout_runs_source");
    }

    private static async Task AssertViolationAsync(
        NpgsqlConnection connection,
        string sql,
        string sqlState,
        string constraintName)
    {
        await using NpgsqlTransaction transaction = await connection.BeginTransactionAsync();
        await using NpgsqlCommand command = connection.CreateCommand();
        command.Transaction = transaction;
        command.CommandText = sql;
        PostgresException violation = await Assert.ThrowsAsync<PostgresException>(() => command.ExecuteNonQueryAsync());
        await Assert.That(violation.SqlState).IsEqualTo(sqlState);
        await Assert.That(violation.ConstraintName).IsEqualTo(constraintName);
        await transaction.RollbackAsync();
    }

    private static async Task AssertSucceedsAndRollbackAsync(NpgsqlConnection connection, string sql)
    {
        await using NpgsqlTransaction transaction = await connection.BeginTransactionAsync();
        await using NpgsqlCommand command = connection.CreateCommand();
        command.Transaction = transaction;
        command.CommandText = sql;
        await Assert.That(await command.ExecuteNonQueryAsync()).IsEqualTo(2);
        await transaction.RollbackAsync();
    }

    private async Task ResetSharedMigrationDatabaseAsync()
    {
        var databaseIdentity = new NpgsqlConnectionStringBuilder(fixture.ConnectionString);
        if (!databaseIdentity.Database.StartsWith("recipient_delivery_migration_", StringComparison.Ordinal))
        {
            throw new InvalidOperationException("Refusing to reset a non-test migration database.");
        }

        await using var connection = new NpgsqlConnection(fixture.ConnectionString);
        await connection.OpenAsync();
        await using var command = new NpgsqlCommand("DROP SCHEMA public CASCADE; CREATE SCHEMA public;", connection);
        await command.ExecuteNonQueryAsync();
    }

    private static string[] ReadPendingModelOperations(ExploreDbContext context)
    {
        IMigrationsAssembly migrationsAssembly = context.GetService<IMigrationsAssembly>();
        IMigrationsModelDiffer modelDiffer = context.GetService<IMigrationsModelDiffer>();
        IModel runtimeModel = context.GetService<IDesignTimeModel>().Model;
        IModel rawSnapshotModel = migrationsAssembly.ModelSnapshot?.Model
            ?? throw new InvalidOperationException("ExploreDbContext migration snapshot was not found.");
        IModel snapshotModel = context.GetService<IModelRuntimeInitializer>()
            .Initialize(rawSnapshotModel, designTime: true, validationLogger: null);

        return modelDiffer
            .GetDifferences(snapshotModel.GetRelationalModel(), runtimeModel.GetRelationalModel())
            .Select(DescribeOperation)
            .ToArray();
    }

    private static string DescribeOperation(MigrationOperation operation) => operation switch
    {
        AddColumnOperation value => $"AddColumn:{value.Table}.{value.Name}",
        AlterColumnOperation value => $"AlterColumn:{value.Table}.{value.Name}",
        DropColumnOperation value => $"DropColumn:{value.Table}.{value.Name}",
        CreateIndexOperation value => $"CreateIndex:{value.Table}.{value.Name}",
        DropIndexOperation value => $"DropIndex:{value.Table}.{value.Name}",
        AddForeignKeyOperation value => $"AddForeignKey:{value.Table}.{value.Name}",
        DropForeignKeyOperation value => $"DropForeignKey:{value.Table}.{value.Name}",
        AddCheckConstraintOperation value => $"AddCheck:{value.Table}.{value.Name}",
        DropCheckConstraintOperation value => $"DropCheck:{value.Table}.{value.Name}",
        _ => operation.GetType().Name
    };

    private static Task<bool> ColumnExistsAsync(NpgsqlConnection connection, string table, string column) =>
        ExistsAsync(connection, $"""
            SELECT EXISTS (
                SELECT 1 FROM information_schema.columns
                WHERE table_schema = 'public' AND table_name = '{table}' AND column_name = '{column}')
            """);

    private static Task<bool> ConstraintExistsAsync(NpgsqlConnection connection, string constraint) =>
        ExistsAsync(connection, $"SELECT EXISTS (SELECT 1 FROM pg_constraint WHERE conname = '{constraint}')");

    private static Task<bool> IndexExistsAsync(NpgsqlConnection connection, string index) =>
        ExistsAsync(connection, $"SELECT to_regclass('public.{index}') IS NOT NULL");

    private static async Task<bool> ExistsAsync(NpgsqlConnection connection, string sql) =>
        await ReadScalarAsync<bool>(connection, sql);

    private static async Task<T> ReadScalarAsync<T>(NpgsqlConnection connection, string sql)
    {
        await using var command = new NpgsqlCommand(sql, connection);
        object? value = await command.ExecuteScalarAsync();
        return value is T typed
            ? typed
            : throw new InvalidOperationException($"Schema query returned no {typeof(T).Name} value.");
    }
}
