// ABOUTME: Verifies recipient-delivery ledger schema in the rebased PostgreSQL baseline.
// ABOUTME: Proves model parity and required recipient constraints without deleted migration boundaries.

using Event.Persistence.IntegrationTests.Fixtures;
using Explore.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql;

namespace Event.Persistence.IntegrationTests.Migrations;

[ClassDataSource<RecipientDeliveryMigrationContainerFixture>(Shared = SharedType.PerAssembly)]
[NotInParallel("RecipientDeliveryMigrationDb")]
public sealed class RecipientNotificationDeliveryMigrationTests(
    RecipientDeliveryMigrationContainerFixture fixture)
{
    [Test]
    public async Task CurrentBaseline_MatchesModelAndEnforcesRecipientDeliveryShape()
    {
        var connectionIdentity = new NpgsqlConnectionStringBuilder(fixture.ConnectionString);
        await Assert.That(connectionIdentity.Database).StartsWith("recipient_delivery_migration_");
        await Assert.That(connectionIdentity.Host is "127.0.0.1" or "localhost").IsTrue();

        await ResetSharedMigrationDatabaseAsync();
        await using var context = CreateDbContext();
        await context.GetService<IMigrator>().MigrateAsync("20260801192258_init");
        await Assert.That(HasPendingModelChanges(context)).IsFalse();

        await using var connection = new NpgsqlConnection(fixture.ConnectionString);
        await connection.OpenAsync();
        await Assert.That(await ConstraintExistsAsync(
            connection,
            "fk_email_dispatch_outbox_recipient_matches_intent")).IsTrue();
        await Assert.That(await ConstraintExistsAsync(
            connection,
            "fk_notification_deliveries_notification_tenant")).IsTrue();
        await Assert.That(await IndexExistsAsync(
            connection,
            "ux_notification_deliveries_tenant_intent_channel")).IsTrue();
        await Assert.That(await IsColumnRequiredAsync(connection, "notification_intents", "recipient_user_id")).IsTrue();
        await Assert.That(await IsColumnRequiredAsync(connection, "email_dispatch_outbox", "recipient_user_id")).IsTrue();
        await Assert.That(await IsColumnRequiredAsync(connection, "email_dispatch_outbox", "notification_intent_id")).IsTrue();
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

    private static bool HasPendingModelChanges(ExploreDbContext context)
    {
        IMigrationsAssembly migrationsAssembly = context.GetService<IMigrationsAssembly>();
        IMigrationsModelDiffer modelDiffer = context.GetService<IMigrationsModelDiffer>();
        IModel runtimeModel = context.GetService<IDesignTimeModel>().Model;
        IModel rawSnapshotModel = migrationsAssembly.ModelSnapshot?.Model
            ?? throw new InvalidOperationException("ExploreDbContext migration snapshot was not found.");
        IModel snapshotModel = context.GetService<IModelRuntimeInitializer>()
            .Initialize(rawSnapshotModel, designTime: true, validationLogger: null);

        return modelDiffer.HasDifferences(
            snapshotModel.GetRelationalModel(),
            runtimeModel.GetRelationalModel());
    }

    private static Task<bool> ConstraintExistsAsync(NpgsqlConnection connection, string constraint) =>
        ExistsAsync(connection, "SELECT EXISTS (SELECT 1 FROM pg_constraint WHERE conname = @name)", constraint);

    private static Task<bool> IndexExistsAsync(NpgsqlConnection connection, string index) =>
        ExistsAsync(connection, "SELECT EXISTS (SELECT 1 FROM pg_indexes WHERE indexname = @name)", index);

    private static async Task<bool> ExistsAsync(
        NpgsqlConnection connection,
        string sql,
        string name)
    {
        await using var command = new NpgsqlCommand(sql, connection);
        command.Parameters.AddWithValue("name", name);
        return (bool)(await command.ExecuteScalarAsync())!;
    }

    private static async Task<bool> IsColumnRequiredAsync(
        NpgsqlConnection connection,
        string table,
        string column)
    {
        await using var command = new NpgsqlCommand(
            """
            SELECT is_nullable = 'NO'
            FROM information_schema.columns
            WHERE table_schema = 'public' AND table_name = @table AND column_name = @column
            """,
            connection);
        command.Parameters.AddWithValue("table", table);
        command.Parameters.AddWithValue("column", column);
        return (bool)(await command.ExecuteScalarAsync())!;
    }
}
