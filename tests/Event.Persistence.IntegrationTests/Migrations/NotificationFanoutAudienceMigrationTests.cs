// ABOUTME: Verifies fanout audience execution schema in the rebased PostgreSQL baseline.
// ABOUTME: Proves registration coverage, fenced-run constraints, and filtered uniqueness.

using Event.Persistence.IntegrationTests.Fixtures;
using Explore.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql;

namespace Event.Persistence.IntegrationTests.Migrations;

[ClassDataSource<RecipientDeliveryMigrationContainerFixture>(Shared = SharedType.PerAssembly)]
[NotInParallel("RecipientDeliveryMigrationDb")]
public sealed class NotificationFanoutAudienceMigrationTests(
    RecipientDeliveryMigrationContainerFixture fixture)
{
    [Test]
    public async Task CurrentBaseline_EnforcesFanoutRunExecutionShape()
    {
        var databaseIdentity = new NpgsqlConnectionStringBuilder(fixture.ConnectionString);
        await Assert.That(databaseIdentity.Database).StartsWith("recipient_delivery_migration_");
        await Assert.That(databaseIdentity.Host is "127.0.0.1" or "localhost").IsTrue();

        await ResetSharedMigrationDatabaseAsync();
        await using var context = CreateDbContext();
        await context.GetService<IMigrator>().MigrateAsync("20260801192258_init");

        await using var connection = new NpgsqlConnection(fixture.ConnectionString);
        await connection.OpenAsync();
        await Assert.That(await ColumnExistsAsync(
            connection,
            "event_registrations",
            "coverage_established_at")).IsTrue();
        await Assert.That(await ConstraintExistsAsync(connection, "fk_fanout_runs_occurrence_tenant")).IsTrue();
        await Assert.That(await ConstraintExistsAsync(connection, "ck_notification_fanout_runs_cursor_pair")).IsTrue();
        await Assert.That(await ConstraintExistsAsync(connection, "ck_notification_fanout_runs_occurrence_lease")).IsTrue();
        await Assert.That(await IndexExistsAsync(connection, "ux_notification_fanout_runs_occurrence")).IsTrue();
        await Assert.That(await FilteredIndexContainsAsync(
            connection,
            "ux_notification_fanout_runs_source",
            "fanout_occurrence_id IS NULL")).IsTrue();
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

    private Task ResetSharedMigrationDatabaseAsync() => fixture.ResetAsync();

    private static Task<bool> ColumnExistsAsync(
        NpgsqlConnection connection,
        string table,
        string column) =>
        ExistsAsync(
            connection,
            """
            SELECT EXISTS (
                SELECT 1 FROM information_schema.columns
                WHERE table_schema = 'public' AND table_name = @table AND column_name = @name)
            """,
            table,
            column);

    private static Task<bool> ConstraintExistsAsync(NpgsqlConnection connection, string constraint) =>
        ExistsAsync(
            connection,
            "SELECT EXISTS (SELECT 1 FROM pg_constraint WHERE conname = @name)",
            string.Empty,
            constraint);

    private static Task<bool> IndexExistsAsync(NpgsqlConnection connection, string index) =>
        ExistsAsync(
            connection,
            "SELECT EXISTS (SELECT 1 FROM pg_indexes WHERE schemaname = 'public' AND indexname = @name)",
            string.Empty,
            index);

    private static async Task<bool> ExistsAsync(
        NpgsqlConnection connection,
        string sql,
        string table,
        string name)
    {
        await using var command = new NpgsqlCommand(sql, connection);
        command.Parameters.AddWithValue("table", table);
        command.Parameters.AddWithValue("name", name);
        return (bool)(await command.ExecuteScalarAsync())!;
    }

    private static async Task<bool> FilteredIndexContainsAsync(
        NpgsqlConnection connection,
        string index,
        string expected)
    {
        await using var command = new NpgsqlCommand(
            "SELECT indexdef FROM pg_indexes WHERE schemaname = 'public' AND indexname = @name",
            connection);
        command.Parameters.AddWithValue("name", index);
        string definition = (string)(await command.ExecuteScalarAsync())!;
        return definition.Contains(expected, StringComparison.OrdinalIgnoreCase);
    }
}
