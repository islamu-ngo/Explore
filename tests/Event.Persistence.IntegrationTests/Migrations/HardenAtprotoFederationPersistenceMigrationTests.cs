// ABOUTME: Exercises the hardened AT Protocol federation migration's empty-state and fail-closed transition guards.
// ABOUTME: Proves Up/Down/Up recovery plus rejection of legacy and hardened rows that cannot be converted safely.

using Event.Persistence.IntegrationTests.Fixtures;
using Explore.Persistence;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql;

namespace Event.Persistence.IntegrationTests.Migrations;

[ClassDataSource<PostgreSqlContainerFixture>(Shared = SharedType.PerAssembly)]
[NotInParallel("PersistenceDb")]
public sealed class HardenAtprotoFederationPersistenceMigrationTests(PostgreSqlContainerFixture fixture)
{
    private const string PreviousMigration = "20260718205141_ProtectAtprotoOAuthSessions";
    private const string TargetMigration = "20260718210538_HardenAtprotoFederationPersistence";

    [Test]
    public async Task EmptyAndPopulatedTransitionsFailClosedWithoutLosingSchemaRecoverability()
    {
        await fixture.ResetAsync();
        await using var context = fixture.CreateDbContext();
        var migrator = context.GetService<IMigrator>();
        try
        {
            await migrator.MigrateAsync(PreviousMigration);
            await migrator.MigrateAsync(TargetMigration);
            await migrator.MigrateAsync(PreviousMigration);
            await migrator.MigrateAsync(TargetMigration);
            await migrator.MigrateAsync(PreviousMigration);

            await ExecuteAsync(
                "INSERT INTO atproto_records (id, did, collection, record_key) " +
                "VALUES (uuidv7(), 'did:plc:legacy', 'community.lexicon.calendar.event', 'legacy')");
            var recordFailure = await Assert.That(async () => await migrator.MigrateAsync(TargetMigration))
                .Throws<PostgresException>();
            await Assert.That(recordFailure!.MessageText).Contains("requires atproto_records and pds_sync_outbox to be empty");
            await ExecuteAsync("DELETE FROM atproto_records");

            await ExecuteAsync(
                "INSERT INTO pds_sync_outbox " +
                "(id, did, collection, record_key, operation, status, created_at, retry_count, max_retries) " +
                "VALUES (uuidv7(), 'did:plc:legacy', 'community.lexicon.calendar.event', 'legacy', 1, 1, NOW(), 0, 10)");
            var outboxFailure = await Assert.That(async () => await migrator.MigrateAsync(TargetMigration))
                .Throws<PostgresException>();
            await Assert.That(outboxFailure!.MessageText).Contains("requires atproto_records and pds_sync_outbox to be empty");
            await ExecuteAsync("DELETE FROM pds_sync_outbox");

            await migrator.MigrateAsync(TargetMigration);
            await ExecuteAsync(
                "INSERT INTO atproto_jetstream_consumer_states " +
                "(id, service, cursor, lease_fence, updated_at) " +
                "VALUES (uuidv7(), 'wss://jetstream.example/subscribe', 0, 0, NOW())");
            var downgradeFailure = await Assert.That(async () => await migrator.MigrateAsync(PreviousMigration))
                .Throws<PostgresException>();
            await Assert.That(downgradeFailure!.MessageText).Contains("Cannot downgrade HardenAtprotoFederationPersistence");
            await ExecuteAsync("DELETE FROM atproto_jetstream_consumer_states");

            await migrator.MigrateAsync(PreviousMigration);
            await migrator.MigrateAsync(TargetMigration);
        }
        finally
        {
            await ExecuteAsync("DELETE FROM atproto_jetstream_quarantines");
            await ExecuteAsync("DELETE FROM atproto_record_tenant_presentations");
            await ExecuteAsync("DELETE FROM atproto_outbound_record_ownerships");
            await ExecuteAsync("DELETE FROM pds_sync_outbox");
            await ExecuteAsync("DELETE FROM atproto_records");
            await ExecuteAsync("DELETE FROM atproto_jetstream_consumer_states");
            await migrator.MigrateAsync(TargetMigration);
        }
    }

    private async Task ExecuteAsync(string sql)
    {
        await using var connection = new NpgsqlConnection(fixture.ConnectionString);
        await connection.OpenAsync();
        await using var command = new NpgsqlCommand(sql, connection);
        await command.ExecuteNonQueryAsync();
    }
}
