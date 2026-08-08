// ABOUTME: Verifies PostgreSQL model constraints against the real post-migration application schema.
// ABOUTME: Guards schema-qualified preflight and catalog lookup behavior after namespace cutovers.

using Explore.Persistence;
using Explore.Persistence.Database;
using Explore.Persistence.Schema;
using Explore.Secrets.Database;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using Testcontainers.PostgreSql;

namespace Event.Persistence.IntegrationTests.Database;

public sealed class PostgresModelConstraintApplierTests
{
    [Test]
    [Arguments("islamu_event")]
    [Arguments("custom_event")]
    public async Task ApplyAsync_AfterNamespaceCutover_CreatesConstraintInApplicationSchema(string schema)
    {
        await using var database = new PostgreSqlBuilder("postgres:18-alpine")
            .WithDatabase("constraint_applier_test")
            .WithUsername("postgres")
            .WithPassword("postgres")
            .Build();
        await database.StartAsync();

        var container = new NpgsqlConnectionStringBuilder(database.GetConnectionString());
        var options = new PrimaryDatabaseConnectionOptions
        {
            Role = PrimaryDatabaseRole.Migrator,
            Provider = PrimaryDatabaseProvider.PostgreSql,
            Host = container.Host,
            Port = container.Port,
            Database = container.Database,
            Schema = schema,
            Username = container.Username,
            Password = container.Password,
            TlsMode = PrimaryDatabaseTlsMode.Disabled,
        };
        var optionsBuilder = new DbContextOptionsBuilder<ExploreDbContext>();
        PrimaryDatabaseConnectionResult configured =
            PrimaryDatabaseProviderComposition.ConfigureApplication(optionsBuilder, options);
        await using var context = new ExploreDbContext(optionsBuilder.Options);
        await context.Database.MigrateAsync();

        await using var connection = new NpgsqlConnection(configured.ConnectionString);
        await connection.OpenAsync();
        await using (var stateCommand = new NpgsqlCommand(
                         "SELECT current_setting('search_path'), to_regclass(@qualified_table)::text, to_regclass('public.event_sessions')::text",
                         connection))
        {
            stateCommand.Parameters.AddWithValue("qualified_table", $"{schema}.event_sessions");
            await using var reader = await stateCommand.ExecuteReaderAsync();
            await reader.ReadAsync();
            await Assert.That(reader.GetString(0)).IsEqualTo($"{schema}, public");
            await Assert.That(reader.GetString(1)).IsEqualTo("event_sessions");
            await Assert.That(reader.IsDBNull(2)).IsTrue();
        }

        var appliedMigrations = (await context.Database.GetAppliedMigrationsAsync()).ToArray();
        await Assert.That(appliedMigrations).HasSingleItem();
        await Assert.That(appliedMigrations[0]).EndsWith("_InitialPostgreSqlApplication");

        await PostgresModelConstraintApplier.ApplyAsync(context);

        await using var constraintCommand = new NpgsqlCommand(
            """
            SELECT count(*)::integer
            FROM pg_catalog.pg_constraint AS constraint_entry
            JOIN pg_catalog.pg_class AS table_entry ON table_entry.oid = constraint_entry.conrelid
            JOIN pg_catalog.pg_namespace AS schema_entry ON schema_entry.oid = table_entry.relnamespace
            WHERE schema_entry.nspname = @schema
              AND table_entry.relname = 'event_sessions'
              AND constraint_entry.conname = 'EX_EventSession_RoomNoOverlap'
              AND constraint_entry.contype = 'x'
            """,
            connection);
        constraintCommand.Parameters.AddWithValue("schema", schema);
        await Assert.That((int)(await constraintCommand.ExecuteScalarAsync())!).IsEqualTo(1);

        await PostgresModelConstraintApplier.ApplyAsync(context);
        await Assert.That((int)(await constraintCommand.ExecuteScalarAsync())!).IsEqualTo(1);

        await using var checkConstraintCommand = new NpgsqlCommand(
            """
            SELECT count(*)::integer
            FROM pg_catalog.pg_constraint AS constraint_entry
            JOIN pg_catalog.pg_class AS table_entry ON table_entry.oid = constraint_entry.conrelid
            JOIN pg_catalog.pg_namespace AS schema_entry ON schema_entry.oid = table_entry.relnamespace
            WHERE schema_entry.nspname = @schema
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
        checkConstraintCommand.Parameters.AddWithValue("schema", schema);
        await Assert.That((int)(await checkConstraintCommand.ExecuteScalarAsync())!).IsEqualTo(7);
    }
}
