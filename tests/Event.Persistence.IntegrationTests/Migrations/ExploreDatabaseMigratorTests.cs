// ABOUTME: Verifies the application migration seam against a disposable PostgreSQL database.
// ABOUTME: Proves ordinary EF migration applies the current set without staged configuration and is retry-safe.

using Event.Persistence.IntegrationTests.Fixtures;
using Explore.Persistence;
using Explore.Persistence.Schema;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.Extensions.Configuration;
using Npgsql;

namespace Event.Persistence.IntegrationTests.Migrations;

[ClassDataSource<RecipientDeliveryMigrationContainerFixture>(Shared = SharedType.PerAssembly)]
[NotInParallel("RecipientDeliveryMigrationDb")]
[Property("Category", "ExploreDatabaseMigrator")]
public sealed class ExploreDatabaseMigratorTests(RecipientDeliveryMigrationContainerFixture fixture)
{
    [Test]
    public async Task MigrateAsync_AppliesCurrentMigrationSetWithoutStageConfiguration_AndIsRetrySafe()
    {
        string databaseName = $"ordinary_migrator_{Guid.NewGuid():N}";
        string connectionString = await CreateDatabaseAsync(databaseName);

        try
        {
            await using ExploreDbContext context = CreateContext(connectionString);
            var configuration = new ConfigurationManager();

            await ExploreDatabaseMigrator.MigrateAsync(context, configuration);
            string[] firstHistory = (await context.Database.GetAppliedMigrationsAsync()).ToArray();
            bool probeTableExists = await context.Database.SqlQueryRaw<bool>(
                    """SELECT to_regclass('public.task4_migration_probe') IS NOT NULL AS "Value" """)
                .SingleAsync();

            await ExploreDatabaseMigrator.MigrateAsync(context, configuration);
            string[] secondHistory = (await context.Database.GetAppliedMigrationsAsync()).ToArray();

            await Assert.That(firstHistory).IsEquivalentTo([Task4MigrationProbe.MigrationId]);
            await Assert.That(probeTableExists).IsTrue();
            await Assert.That(secondHistory).IsEquivalentTo(firstHistory);
        }
        finally
        {
            await DropDatabaseAsync(databaseName);
        }
    }

    private async Task<string> CreateDatabaseAsync(string databaseName)
    {
        var builder = new NpgsqlConnectionStringBuilder(fixture.ConnectionString)
        {
            Database = "postgres",
            Pooling = false
        };
        await using var connection = new NpgsqlConnection(builder.ConnectionString);
        await connection.OpenAsync();
        await using var command = new NpgsqlCommand($"CREATE DATABASE \"{databaseName}\"", connection);
        await command.ExecuteNonQueryAsync();
        builder.Database = databaseName;
        return builder.ConnectionString;
    }

    private async Task DropDatabaseAsync(string databaseName)
    {
        NpgsqlConnection.ClearAllPools();
        var builder = new NpgsqlConnectionStringBuilder(fixture.ConnectionString)
        {
            Database = "postgres",
            Pooling = false
        };
        await using var connection = new NpgsqlConnection(builder.ConnectionString);
        await connection.OpenAsync();
        await using var terminate = new NpgsqlCommand(
            "SELECT pg_terminate_backend(pid) FROM pg_stat_activity WHERE datname = @database AND pid <> pg_backend_pid()",
            connection);
        terminate.Parameters.AddWithValue("database", databaseName);
        await terminate.ExecuteNonQueryAsync();
        await using var drop = new NpgsqlCommand($"DROP DATABASE IF EXISTS \"{databaseName}\"", connection);
        await drop.ExecuteNonQueryAsync();
    }

    private static ExploreDbContext CreateContext(string connectionString)
    {
        var options = new DbContextOptionsBuilder<ExploreDbContext>()
            .UseNpgsql(
                connectionString,
                postgres => postgres.MigrationsAssembly(typeof(Task4MigrationProbe).Assembly.FullName))
            .UseSnakeCaseNamingConvention()
            .ConfigureWarnings(warnings => warnings.Ignore(RelationalEventId.PendingModelChangesWarning))
            .Options;
        return new Task4MigrationProbeContext(options);
    }
}

public sealed class Task4MigrationProbeContext(DbContextOptions<ExploreDbContext> options)
    : ExploreDbContext(options);

[DbContext(typeof(Task4MigrationProbeContext))]
[Migration(MigrationId)]
public sealed class Task4MigrationProbe : Migration
{
    public const string MigrationId = "20260720000000_Task4MigrationProbe";

    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "task4_migration_probe",
            columns: table => new
            {
                id = table.Column<int>(type: "integer", nullable: false)
            },
            constraints: table => table.PrimaryKey("pk_task4_migration_probe", x => x.id));
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable("task4_migration_probe");
    }
}
