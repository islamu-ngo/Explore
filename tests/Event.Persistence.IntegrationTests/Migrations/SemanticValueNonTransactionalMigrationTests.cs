// ABOUTME: Proves semantic constraints cannot partially install on non-transactional DDL providers.
// ABOUTME: Exercises malformed legacy data, zero-mutation failure, explicit repair, and retry on MariaDB and MySQL.

#nullable enable

using System.Data.Common;
using Event.Persistence.IntegrationTests.Fixtures;
using Explore.Persistence;
using Explore.Persistence.Database;
using Explore.Persistence.Schema;
using Explore.Secrets.Database;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.Extensions.Configuration;

namespace Event.Persistence.IntegrationTests.Migrations;

[ClassDataSource<SemanticValueNonTransactionalProviderFixture>(
    Shared = SharedType.PerClass)]
[NotInParallel("SemanticValueNonTransactionalProviderDb")]
public sealed class SemanticValueNonTransactionalMigrationTests(
    SemanticValueNonTransactionalProviderFixture fixture)
{
    private const string MigrationSuffix = "PersistSemanticValueConstraints";
    private static readonly string[] ConstraintNames =
    [
        "CK_EventAgendaItem_LocalDateRange",
        "CK_EventSession_LocalDateRange",
        "CK_EventTicketType_MoneyNonnegative",
        "CK_LocationPii_CoordinateShape"
    ];

    [Test]
    [Arguments(PrimaryDatabaseProvider.MariaDb)]
    [Arguments(PrimaryDatabaseProvider.MySql)]
    public async Task MalformedLegacyMoney_FailsBeforeDdlAndIsRetryable(
        PrimaryDatabaseProvider provider)
    {
        await using ExploreDbContext context = CreateContext(
            fixture.CreateOptions(provider));
        SemanticMigrationCatalog catalog = FindSemanticMigration(context);
        await CreatePreSemanticDatabaseAsync(context, catalog);
        Guid ticketId = Guid.CreateVersion7();
        await ExecuteAsync(
            context,
            """
            INSERT INTO ie_event_ticket_types (id, fixed_price_minor)
            VALUES (@ticket_id, -1)
            """,
            ("ticket_id", ticketId));

        InvalidOperationException? exception = await Assert.That(async () =>
                await ExploreDatabaseMigrator.MigrateAsync(
                    context,
                    new ConfigurationManager()))
            .Throws<InvalidOperationException>();
        await Assert.That(exception!.Message)
            .Contains("semantic value", StringComparison.OrdinalIgnoreCase);
        await Assert.That(await CountSemanticConstraintsAsync(context))
            .IsEqualTo(0);
        await Assert.That((await context.Database.GetAppliedMigrationsAsync())
                .Contains(catalog.MigrationId, StringComparer.Ordinal))
            .IsFalse();

        await ExecuteAsync(
            context,
            """
            UPDATE ie_event_ticket_types
            SET fixed_price_minor = 0
            WHERE id = @ticket_id
            """,
            ("ticket_id", ticketId));

        await ExploreDatabaseMigrator.MigrateAsync(
            context,
            new ConfigurationManager());

        await Assert.That(await CountSemanticConstraintsAsync(context))
            .IsEqualTo(ConstraintNames.Length);
        await Assert.That((await context.Database.GetAppliedMigrationsAsync())
                .Count(id => string.Equals(
                    id,
                    catalog.MigrationId,
                    StringComparison.Ordinal)))
            .IsEqualTo(1);
    }

    private static ExploreDbContext CreateContext(
        PrimaryDatabaseConnectionOptions options)
    {
        var builder = new DbContextOptionsBuilder<ExploreDbContext>();
        PrimaryDatabaseProviderComposition.ConfigureApplication(builder, options);
        return new ExploreDbContext(builder.Options);
    }

    private static SemanticMigrationCatalog FindSemanticMigration(
        ExploreDbContext context)
    {
        string[] migrations = context.GetService<IMigrationsAssembly>()
            .Migrations
            .Keys
            .ToArray();
        int semanticIndex = Array.FindIndex(
            migrations,
            id => id.EndsWith(MigrationSuffix, StringComparison.Ordinal));
        if (semanticIndex <= 0)
        {
            throw new InvalidOperationException(
                "The semantic migration requires generated predecessors.");
        }

        return new SemanticMigrationCatalog(
            migrations[semanticIndex],
            migrations[..semanticIndex]);
    }

    private static async Task CreatePreSemanticDatabaseAsync(
        ExploreDbContext context,
        SemanticMigrationCatalog catalog)
    {
        await ExecuteAsync(
            context,
            """
            CREATE TABLE ie_event_ticket_types (
                id char(36) NOT NULL PRIMARY KEY,
                fixed_price_minor bigint NULL,
                minimum_price_minor bigint NULL,
                suggested_price_minor bigint NULL);
            """);
        await ExecuteAsync(
            context,
            """
            CREATE TABLE ie_location_pii (
                location_id char(36) NOT NULL PRIMARY KEY,
                latitude double NULL,
                longitude double NULL);
            """);
        await ExecuteAsync(
            context,
            """
            CREATE TABLE ie_event_agenda_items (
                id char(36) NOT NULL PRIMARY KEY,
                local_start_date date NULL,
                local_end_date date NULL);
            """);
        await ExecuteAsync(
            context,
            """
            CREATE TABLE ie_event_sessions (
                id char(36) NOT NULL PRIMARY KEY,
                local_start_date date NULL,
                local_end_date date NULL);
            """);

        IHistoryRepository history =
            context.GetService<IHistoryRepository>();
        await ExecuteAsync(context, history.GetCreateScript());
        foreach (string migrationId in catalog.PreviousMigrationIds)
        {
            await ExecuteAsync(
                context,
                history.GetInsertScript(
                    new HistoryRow(migrationId, ProductInfo.GetVersion())));
        }
    }

    private static async Task<int> CountSemanticConstraintsAsync(
        ExploreDbContext context)
    {
        object? result = await ExecuteScalarAsync(
            context,
            """
            SELECT COUNT(*)
            FROM information_schema.table_constraints
            WHERE constraint_schema = DATABASE()
              AND constraint_name IN (
                  'CK_EventAgendaItem_LocalDateRange',
                  'CK_EventSession_LocalDateRange',
                  'CK_EventTicketType_MoneyNonnegative',
                  'CK_LocationPii_CoordinateShape')
            """);
        return Convert.ToInt32(
            result,
            System.Globalization.CultureInfo.InvariantCulture);
    }

    private static async Task ExecuteAsync(
        ExploreDbContext context,
        string sql,
        params (string Name, object Value)[] parameters)
    {
        await using DbCommand command = CreateCommand(context, sql, parameters);
        await EnsureOpenAsync(command.Connection!);
        await command.ExecuteNonQueryAsync();
    }

    private static async Task<object?> ExecuteScalarAsync(
        ExploreDbContext context,
        string sql)
    {
        await using DbCommand command = CreateCommand(context, sql, []);
        await EnsureOpenAsync(command.Connection!);
        return await command.ExecuteScalarAsync();
    }

    private static DbCommand CreateCommand(
        ExploreDbContext context,
        string sql,
        (string Name, object Value)[] parameters)
    {
        DbCommand command =
            context.Database.GetDbConnection().CreateCommand();
        command.CommandText = sql;
        foreach ((string name, object value) in parameters)
        {
            DbParameter parameter = command.CreateParameter();
            parameter.ParameterName = name;
            parameter.Value = value;
            command.Parameters.Add(parameter);
        }

        return command;
    }

    private static async Task EnsureOpenAsync(DbConnection connection)
    {
        if (connection.State != System.Data.ConnectionState.Open)
        {
            await connection.OpenAsync();
        }
    }

    private sealed record SemanticMigrationCatalog(
        string MigrationId,
        string[] PreviousMigrationIds);
}
