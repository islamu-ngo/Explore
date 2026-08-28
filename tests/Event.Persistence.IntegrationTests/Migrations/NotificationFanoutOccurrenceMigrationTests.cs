// ABOUTME: Verifies fanout-occurrence schema in the rebased PostgreSQL baseline.
// ABOUTME: Proves model parity, tenant-safe foreign keys, and the recipient uniqueness guard.

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
public sealed class NotificationFanoutOccurrenceMigrationTests(
    RecipientDeliveryMigrationContainerFixture fixture)
{
    [Test]
    public async Task CurrentBaseline_CreatesOccurrenceSchemaAndMatchesModel()
    {
        await ResetSharedMigrationDatabaseAsync();
        await using var context = CreateDbContext();
        IMigrator migrator = context.GetService<IMigrator>();

        try
        {
            await migrator.MigrateAsync();
            await Assert.That(ReadPendingModelOperations(context)).IsEmpty();
            await AssertSchemaAsync(expected: true);
        }
        finally
        {
            await ResetSharedMigrationDatabaseAsync();
        }
    }

    private Task ResetSharedMigrationDatabaseAsync() => fixture.ResetAsync();

    private ExploreDbContext CreateDbContext()
    {
        var builder = new DbContextOptionsBuilder<ExploreDbContext>()
            .UseNpgsql(fixture.ConnectionString)
            .UseSnakeCaseNamingConvention()
            .ConfigureWarnings(warnings =>
            {
                warnings.Ignore(RelationalEventId.PendingModelChangesWarning);
                warnings.Log(CoreEventId.ManyServiceProvidersCreatedWarning);
            });
        builder.EnableServiceProviderCaching(false);
        return new ExploreDbContext(builder.Options);
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
        AddUniqueConstraintOperation value => $"AddUnique:{value.Table}.{value.Name}",
        DropUniqueConstraintOperation value => $"DropUnique:{value.Table}.{value.Name}",
        AddCheckConstraintOperation value => $"AddCheck:{value.Table}.{value.Name}",
        DropCheckConstraintOperation value => $"DropCheck:{value.Table}.{value.Name}",
        _ => operation.GetType().Name
    };

    private async Task AssertSchemaAsync(bool expected)
    {
        await using var connection = new NpgsqlConnection(fixture.ConnectionString);
        await connection.OpenAsync();

        await Assert.That(await ExistsAsync(connection, """
            SELECT EXISTS (
                SELECT 1 FROM information_schema.tables
                WHERE table_schema = current_schema() AND table_name = 'notification_fanout_occurrences')
            """)).IsEqualTo(expected);
        await Assert.That(await ExistsAsync(connection, """
            SELECT EXISTS (
                SELECT 1 FROM information_schema.columns
                WHERE table_schema = current_schema() AND table_name = 'notification_intents'
                  AND column_name = 'fanout_occurrence_id')
            """)).IsEqualTo(expected);

        if (!expected)
        {
            return;
        }

        await Assert.That(await ExistsAsync(connection, """
            SELECT EXISTS (
                SELECT 1 FROM pg_constraint
                WHERE conname = 'fk_notification_intents_fanout_occurrence_tenant')
            """)).IsTrue();
        await Assert.That(await ExistsAsync(connection, """
            SELECT EXISTS (
                SELECT 1 FROM pg_indexes
                WHERE schemaname = current_schema()
                  AND indexname = 'ux_notification_intents_tenant_occurrence_recipient')
            """)).IsTrue();
    }

    private static async Task<bool> ExistsAsync(NpgsqlConnection connection, string sql)
    {
        await using var command = new NpgsqlCommand(sql, connection);
        return (bool)(await command.ExecuteScalarAsync()
            ?? throw new InvalidOperationException("Schema existence query returned no value."));
    }
}
