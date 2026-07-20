// ABOUTME: Pins the rebased ExploreDbContext migration catalog and OREA-owned model drift.
// ABOUTME: Proves the application erasure ledger migrates additively and rejects evidence-destroying rollback.

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
public sealed class ApplicationLocationPrivacyErasureLedgerMigrationTests(
    RecipientDeliveryMigrationContainerFixture fixture)
{
    private const string MigrationName = "AddApplicationDatabaseLocationPrivacyErasureLedger";
    private const string LedgerPredecessorMigrationId = "20260719230250_AddEventReportDecisionExecution";

    private static readonly string[] BaselineMigrationIds =
    [
        "20260719221539_init",
        LedgerPredecessorMigrationId,
        "20260720022047_RestoreEventLocationPrivacyDatabaseGuards",
        "20260720120000_MakeTrustSafetyPreferenceOptional"
    ];

    private static readonly string[] LedgerOperations =
    [
        "CreateIndex:location_privacy_authority.erasure_intents.ix_erasure_intents_intent_id",
        "CreateTable:location_privacy_authority.authority_counter",
        "CreateTable:location_privacy_authority.erasure_intents",
        "EnsureSchema:location_privacy_authority"
    ];

    [Test]
    public async Task Baseline_CurrentCatalogAndOREAOwnedPendingOperationsArePinned()
    {
        await using var context = CreateDbContext();
        IMigrationsAssembly migrationsAssembly = context.GetService<IMigrationsAssembly>();
        string[] catalogWithoutTarget = migrationsAssembly.Migrations.Keys
            .Where(id => !id.EndsWith($"_{MigrationName}", StringComparison.Ordinal))
            .Order(StringComparer.Ordinal)
            .ToArray();

        await Assert.That(catalogWithoutTarget).IsEquivalentTo(BaselineMigrationIds);

        string[] pendingOperations = ReadPendingModelOperations(context);
        bool targetExists = migrationsAssembly.Migrations.Keys.Any(
            id => id.EndsWith($"_{MigrationName}", StringComparison.Ordinal));

        await Assert.That(pendingOperations)
            .IsEquivalentTo(targetExists ? [] : LedgerOperations);
    }

    [Test]
    public async Task DesiredMigration_AppliesAdditivelyAndRejectsDownBeforeErasureEvidenceMutation()
    {
        await using var context = CreateDbContext();
        IMigrationsAssembly migrationsAssembly = context.GetService<IMigrationsAssembly>();
        KeyValuePair<string, System.Reflection.TypeInfo> targetMigration = migrationsAssembly.Migrations
            .SingleOrDefault(item => item.Key.EndsWith($"_{MigrationName}", StringComparison.Ordinal));

        await Assert.That(targetMigration.Key)
            .IsNotNull()
            .Because($"the {MigrationName} migration must exist before its PostgreSQL contract can run");

        string targetMigrationId = targetMigration.Key!;
        Migration migration = migrationsAssembly.CreateMigration(
            targetMigration.Value!,
            context.Database.ProviderName!);
        await Assert.That(migration.UpOperations.Select(DescribeOperation))
            .IsEquivalentTo(LedgerOperations);

        await ResetDatabaseAsync();
        try
        {
            await CreateAdditiveProbeAsync();
            IMigrator migrator = context.GetService<IMigrator>();
            await migrator.MigrateAsync(targetMigrationId);

            await Assert.That(await TableExistsAsync("location_privacy_authority", "authority_counter")).IsTrue();
            await Assert.That(await TableExistsAsync("location_privacy_authority", "erasure_intents")).IsTrue();
            await Assert.That(await ProbeValueAsync()).IsEqualTo("preserved");

            Guid intentId = Guid.CreateVersion7();
            await InsertErasureEvidenceAsync(intentId);
            string[] historyBefore = await ReadMigrationHistoryAsync();

            Exception? rollbackFailure = null;
            try
            {
                await migrator.MigrateAsync(LedgerPredecessorMigrationId);
            }
            catch (Exception exception)
            {
                rollbackFailure = exception;
            }

            bool evidenceTableExists = await TableExistsAsync(
                "location_privacy_authority",
                "erasure_intents");
            long evidenceCount = evidenceTableExists ? await EvidenceCountAsync(intentId) : -1;
            string[] historyAfter = await ReadMigrationHistoryAsync();

            await Assert.That(rollbackFailure).IsNotNull();
            await Assert.That(evidenceTableExists).IsTrue();
            await Assert.That(evidenceCount).IsEqualTo(1);
            await Assert.That(historyAfter).IsEquivalentTo(historyBefore);
            await Assert.That(historyAfter).Contains(targetMigrationId);
            await Assert.That(await ProbeValueAsync()).IsEqualTo("preserved");
        }
        finally
        {
            await ResetDatabaseAsync();
        }
    }

    private ExploreDbContext CreateDbContext()
    {
        var databaseIdentity = new NpgsqlConnectionStringBuilder(fixture.ConnectionString);
        if (databaseIdentity.Database?.StartsWith("recipient_delivery_migration_", StringComparison.Ordinal) is not true ||
            databaseIdentity.Host is not ("127.0.0.1" or "localhost"))
        {
            throw new InvalidOperationException("Refusing to use a non-disposable PostgreSQL database.");
        }

        var options = new DbContextOptionsBuilder<ExploreDbContext>()
            .UseNpgsql(fixture.ConnectionString)
            .UseSnakeCaseNamingConvention()
            .ConfigureWarnings(warnings => warnings.Ignore(RelationalEventId.PendingModelChangesWarning))
            .Options;
        return new ExploreDbContext(options);
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
            .Order(StringComparer.Ordinal)
            .ToArray();
    }

    private static string DescribeOperation(MigrationOperation operation) => operation switch
    {
        EnsureSchemaOperation value => $"EnsureSchema:{value.Name}",
        CreateTableOperation value => $"CreateTable:{value.Schema}.{value.Name}",
        CreateIndexOperation value => $"CreateIndex:{value.Schema}.{value.Table}.{value.Name}",
        _ => operation.GetType().Name
    };

    private async Task ResetDatabaseAsync()
    {
        await using var connection = new NpgsqlConnection(fixture.ConnectionString);
        await connection.OpenAsync();
        await using var command = new NpgsqlCommand(
            "DROP SCHEMA IF EXISTS orea130_probe CASCADE; " +
            "DROP SCHEMA IF EXISTS location_privacy_authority CASCADE; " +
            "DROP SCHEMA public CASCADE; CREATE SCHEMA public;",
            connection);
        await command.ExecuteNonQueryAsync();
    }

    private async Task CreateAdditiveProbeAsync()
    {
        await using var connection = new NpgsqlConnection(fixture.ConnectionString);
        await connection.OpenAsync();
        await using var command = new NpgsqlCommand(
            "CREATE SCHEMA orea130_probe; " +
            "CREATE TABLE orea130_probe.marker (value text NOT NULL); " +
            "INSERT INTO orea130_probe.marker (value) VALUES ('preserved');",
            connection);
        await command.ExecuteNonQueryAsync();
    }

    private async Task InsertErasureEvidenceAsync(Guid intentId)
    {
        await using var connection = new NpgsqlConnection(fixture.ConnectionString);
        await connection.OpenAsync();
        await using var command = new NpgsqlCommand(
            """
            INSERT INTO location_privacy_authority.authority_counter (singleton, last_sequence)
            VALUES (TRUE, 1);

            INSERT INTO location_privacy_authority.erasure_intents
                (authority_sequence, intent_id, owner_user_id, location_ids, reason, requested_at_utc, recorded_at_utc)
            VALUES
                (1, @intent_id, @owner_user_id, @location_ids, 1, @requested_at_utc, @recorded_at_utc);
            """,
            connection);
        DateTime recordedAtUtc = DateTime.UtcNow;
        command.Parameters.AddWithValue("intent_id", intentId);
        command.Parameters.AddWithValue("owner_user_id", Guid.CreateVersion7());
        command.Parameters.AddWithValue("location_ids", new[] { Guid.CreateVersion7() });
        command.Parameters.AddWithValue("requested_at_utc", recordedAtUtc.AddSeconds(-1));
        command.Parameters.AddWithValue("recorded_at_utc", recordedAtUtc);
        await command.ExecuteNonQueryAsync();
    }

    private async Task<bool> TableExistsAsync(string schema, string table)
    {
        await using var connection = new NpgsqlConnection(fixture.ConnectionString);
        await connection.OpenAsync();
        await using var command = new NpgsqlCommand(
            """
            SELECT EXISTS (
                SELECT 1 FROM information_schema.tables
                WHERE table_schema = @schema AND table_name = @table)
            """,
            connection);
        command.Parameters.AddWithValue("schema", schema);
        command.Parameters.AddWithValue("table", table);
        return (bool)(await command.ExecuteScalarAsync())!;
    }

    private async Task<long> EvidenceCountAsync(Guid intentId)
    {
        await using var connection = new NpgsqlConnection(fixture.ConnectionString);
        await connection.OpenAsync();
        await using var command = new NpgsqlCommand(
            "SELECT COUNT(*) FROM location_privacy_authority.erasure_intents WHERE intent_id = @intent_id",
            connection);
        command.Parameters.AddWithValue("intent_id", intentId);
        return (long)(await command.ExecuteScalarAsync())!;
    }

    private async Task<string[]> ReadMigrationHistoryAsync()
    {
        await using var connection = new NpgsqlConnection(fixture.ConnectionString);
        await connection.OpenAsync();
        await using var command = new NpgsqlCommand(
            "SELECT migration_id FROM \"__EFMigrationsHistory\" ORDER BY migration_id",
            connection);
        await using NpgsqlDataReader reader = await command.ExecuteReaderAsync();
        var ids = new List<string>();
        while (await reader.ReadAsync())
        {
            ids.Add(reader.GetString(0));
        }

        return ids.ToArray();
    }

    private async Task<string> ProbeValueAsync()
    {
        await using var connection = new NpgsqlConnection(fixture.ConnectionString);
        await connection.OpenAsync();
        await using var command = new NpgsqlCommand("SELECT value FROM orea130_probe.marker", connection);
        return (string)(await command.ExecuteScalarAsync())!;
    }
}
