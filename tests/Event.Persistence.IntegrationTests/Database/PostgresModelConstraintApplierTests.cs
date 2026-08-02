// ABOUTME: Verifies PostgreSQL model constraints against the real post-migration application schema.
// ABOUTME: Guards schema-qualified preflight and catalog lookup behavior after namespace cutovers.

using Explore.Persistence;
using Explore.Persistence.Schema;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Npgsql;
using Testcontainers.PostgreSql;

namespace Event.Persistence.IntegrationTests.Database;

public sealed class PostgresModelConstraintApplierTests
{
    [Test]
    public async Task ApplyAsync_AfterNamespaceCutover_CreatesConstraintInApplicationSchema()
    {
        await using var database = new PostgreSqlBuilder("postgres:18-alpine")
            .WithDatabase("constraint_applier_test")
            .WithUsername("postgres")
            .WithPassword("postgres")
            .Build();
        await database.StartAsync();

        var options = new DbContextOptionsBuilder<ExploreDbContext>()
            .UseNpgsql(database.GetConnectionString())
            .UseSnakeCaseNamingConvention()
            .ConfigureWarnings(warnings => warnings.Ignore(RelationalEventId.PendingModelChangesWarning))
            .Options;
        await using var context = new ExploreDbContext(options);
        await context.Database.MigrateAsync();

        await using var connection = new NpgsqlConnection(database.GetConnectionString());
        await connection.OpenAsync();
        await using (var stateCommand = new NpgsqlCommand(
                         "SELECT current_setting('search_path'), to_regclass('islamu_event.event_sessions')::text, to_regclass('public.event_sessions')::text",
                         connection))
        await using (var reader = await stateCommand.ExecuteReaderAsync())
        {
            await reader.ReadAsync();
            await Assert.That(reader.GetString(0)).IsEqualTo("\"$user\", public");
            await Assert.That(reader.GetString(1)).IsEqualTo("islamu_event.event_sessions");
            await Assert.That(reader.IsDBNull(2)).IsTrue();
        }

        var appliedMigrations = (await context.Database.GetAppliedMigrationsAsync()).ToArray();
        await Assert.That(appliedMigrations.Any(migration =>
            migration.EndsWith("_Phase82TypedRegistrationAnswersInPublic", StringComparison.Ordinal))).IsTrue();
        await Assert.That(appliedMigrations.Last()).EndsWith("_AdoptIslamuEventNamespace");

        await PostgresModelConstraintApplier.ApplyAsync(context);

        await using var constraintCommand = new NpgsqlCommand(
            """
            SELECT count(*)::integer
            FROM pg_catalog.pg_constraint AS constraint_entry
            JOIN pg_catalog.pg_class AS table_entry ON table_entry.oid = constraint_entry.conrelid
            JOIN pg_catalog.pg_namespace AS schema_entry ON schema_entry.oid = table_entry.relnamespace
            WHERE schema_entry.nspname = 'islamu_event'
              AND table_entry.relname = 'event_sessions'
              AND constraint_entry.conname = 'EX_EventSession_RoomNoOverlap'
              AND constraint_entry.contype = 'x'
            """,
            connection);
        await Assert.That((int)(await constraintCommand.ExecuteScalarAsync())!).IsEqualTo(1);

        await PostgresModelConstraintApplier.ApplyAsync(context);
        await Assert.That((int)(await constraintCommand.ExecuteScalarAsync())!).IsEqualTo(1);

        await using var checkConstraintCommand = new NpgsqlCommand(
            """
            SELECT count(*)::integer
            FROM pg_catalog.pg_constraint AS constraint_entry
            JOIN pg_catalog.pg_class AS table_entry ON table_entry.oid = constraint_entry.conrelid
            JOIN pg_catalog.pg_namespace AS schema_entry ON schema_entry.oid = table_entry.relnamespace
            WHERE schema_entry.nspname = 'islamu_event'
              AND table_entry.relname = 'event_sessions'
              AND constraint_entry.conname IN (
                  'CK_EventSession_EndAfterStart',
                  'CK_EventSession_EndTimeTypeState',
                  'CK_EventSession_LocalStartMinuteRange',
                  'CK_EventSession_LocalEndMinuteRange',
                  'CK_EventSession_LocalStartMinuteMatchesTime',
                  'CK_EventSession_LocalEndMinuteMatchesTime',
                  'CK_EventSession_RoomRequiresLocation')
              AND constraint_entry.contype = 'c'
            """,
            connection);
        await Assert.That((int)(await checkConstraintCommand.ExecuteScalarAsync())!).IsEqualTo(7);
    }
}
