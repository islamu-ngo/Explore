// ABOUTME: Verifies deleted ownerless Actor tombstones and their fail-closed migration downgrade contract.
// ABOUTME: Proves live ownership remains strict and downgrade never invents or deletes retained Actor owners.

using Event.Persistence.IntegrationTests.Fixtures;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql;

namespace Event.Persistence.IntegrationTests.Migrations;

[ClassDataSource<PostgreSqlContainerFixture>(Shared = SharedType.PerAssembly)]
[NotInParallel("PersistenceDb")]
public sealed class ActorPrivacyTombstoneMigrationTests(PostgreSqlContainerFixture fixture)
{
    private const string CurrentMigration =
        "20260730204755_AllowOwnerlessDeletedActorTombstones";
    private const string PreviousMigration =
        "20260730200905_AddCapacityHoldPolicyLookup";

    [Test]
    public async Task Downgrade_WithOwnerlessDeletedActor_FailsClosedWithoutChangingHistory()
    {
        await fixture.ResetAsync();
        await ExecuteAsync(
            """
            INSERT INTO actors
                (id, actor_type_id, is_suspended, created_at, is_deleted, deleted_at, concurrency_stamp)
            VALUES
                (@id, 1, FALSE, NOW(), TRUE, NOW(), @stamp)
            """,
            ("id", Guid.CreateVersion7()),
            ("stamp", Guid.CreateVersion7()));
        await using var context = fixture.CreateDbContext();

        var exception = await Assert.That(async () =>
                await context.GetService<IMigrator>().MigrateAsync(PreviousMigration))
            .Throws<PostgresException>();

        await Assert.That(exception!.MessageText)
            .IsEqualTo("Cannot downgrade while deleted ownerless Actor tombstones exist.");
        await Assert.That(await context.Database.GetAppliedMigrationsAsync())
            .Contains(CurrentMigration);
    }

    [Test]
    public async Task Downgrade_WithoutOwnerlessDeletedActor_RestoresPriorConstraint()
    {
        await fixture.ResetAsync();
        await using var context = fixture.CreateDbContext();
        IMigrator migrator = context.GetService<IMigrator>();

        try
        {
            await migrator.MigrateAsync(PreviousMigration);

            string definition = await ReadConstraintDefinitionAsync();
            await Assert.That(definition).Contains("num_nonnulls");
            await Assert.That(definition).DoesNotContain("is_deleted");
            await Assert.That(await context.Database.GetAppliedMigrationsAsync())
                .DoesNotContain(CurrentMigration);
        }
        finally
        {
            await migrator.MigrateAsync(CurrentMigration);
        }
    }

    private async Task<string> ReadConstraintDefinitionAsync()
    {
        await using var connection = new NpgsqlConnection(fixture.ConnectionString);
        await connection.OpenAsync();
        await using var command = new NpgsqlCommand(
            """
            SELECT pg_get_constraintdef(oid)
            FROM pg_constraint
            WHERE conname = 'ck_actors_exactly_one_owner'
            """,
            connection);
        return (string)(await command.ExecuteScalarAsync())!;
    }

    private async Task ExecuteAsync(string sql, params (string Name, object Value)[] parameters)
    {
        await using var connection = new NpgsqlConnection(fixture.ConnectionString);
        await connection.OpenAsync();
        await using var command = new NpgsqlCommand(sql, connection);
        foreach ((string name, object value) in parameters)
        {
            command.Parameters.AddWithValue(name, value);
        }

        await command.ExecuteNonQueryAsync();
    }
}
