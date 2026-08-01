// ABOUTME: Verifies generated application/data-protection init artifacts and the retained authority chain.
// ABOUTME: Pins runtime lookup/schema application, exact moderation linkage, rollback safety, and authority ACLs.

using Event.Persistence.IntegrationTests.Fixtures;
using Explore.Application.Contracts.PrivacyErasure;
using Explore.Persistence;
using Explore.Persistence.Privacy.ErasureAuthority;
using Explore.Persistence.Schema;
using Explore.Persistence.Seed;
using Microsoft.AspNetCore.DataProtection.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Migrations.Operations;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Npgsql;

namespace Event.Persistence.IntegrationTests.Migrations;

[ClassDataSource<RecipientDeliveryMigrationContainerFixture>(Shared = SharedType.PerAssembly)]
[NotInParallel("Task6GeneratedInitDb")]
public sealed class GeneratedInitMigrationBehaviorTests(
    RecipientDeliveryMigrationContainerFixture fixture)
{
    [Test]
    public async Task GeneratedCatalogs_ContainOneApplicationInitOneDataProtectionInitAndRetainedAuthorityChain()
    {
        await using ExploreDbContext explore = CreateExploreContext();
        await using DataProtectionKeyContext dataProtection = CreateDataProtectionContext();
        await using PrivacyErasureAuthorityDbContext authority = CreateAuthorityContext();

        await Assert.That(MigrationIds(explore)).HasSingleItem();
        await Assert.That(MigrationIds(dataProtection)).HasSingleItem();
        await Assert.That(MigrationIds(authority).Length).IsEqualTo(2);
        await Assert.That(MigrationIds(explore)[0]).EndsWith("_init");
        await Assert.That(MigrationIds(dataProtection)[0]).EndsWith("_init");
        await Assert.That(MigrationIds(authority)[0]).EndsWith("_init");
        await Assert.That(MigrationIds(authority)[1]).EndsWith("_AddFiniteAuthorityRetention");

        Migration dataProtectionInit = InitMigration(dataProtection);
        CreateTableOperation table = dataProtectionInit.UpOperations
            .OfType<CreateTableOperation>()
            .Single();
        await Assert.That(table.Name).IsEqualTo("data_protection_keys");
        await Assert.That(table.Columns.Select(column => column.Name))
            .IsEquivalentTo(["id", "friendly_name", "xml"]);
        await Assert.That(dataProtectionInit.UpOperations.Count).IsEqualTo(1);
    }

    [Test]
    public async Task ExploreInit_WithRuntimeCatalogAndSchemaApplication_IsReversibleFromEmpty()
    {
        await ResetDatabaseAsync();
        try
        {
            await using ExploreDbContext context = CreateExploreContext();
            IMigrator migrator = context.GetService<IMigrator>();
            string migrationId = MigrationIds(context).Single();

            await migrator.MigrateAsync(migrationId);
            await PostgresModelConstraintApplier.ApplyAsync(context);
            await LookupTableSeeder.SeedAsync(context);

            await Assert.That(await ScalarIntAsync(
                """
                SELECT
                    (SELECT count(*) FROM location_kinds
                     WHERE (id, master_code) IN (
                         (1, 'UNCLASSIFIED'), (2, 'COMMERCIAL_VENUE'),
                         (3, 'PUBLIC_SPACE'), (4, 'COMMUNITY_VENUE'), (5, 'PRIVATE_HOME')))
                  + (SELECT count(*) FROM location_privacy_states
                     WHERE (id, master_code) IN (
                         (1, 'NOT_PROVIDED'), (2, 'ACTIVE'), (3, 'ERASED')))
                  + (SELECT count(*) FROM location_disclosure_audiences
                     WHERE (id, master_code) IN (
                         (1, 'NEVER'), (2, 'ANY_CURRENT_REGISTRANT'),
                         (3, 'CONFIRMED_PARTICIPANT')))
                """)).IsEqualTo(11);
            await Assert.That(await ScalarIntAsync(
                """
                SELECT count(*)::integer
                FROM pg_catalog.pg_proc AS function_entry
                JOIN pg_catalog.pg_namespace AS schema_entry
                  ON schema_entry.oid = function_entry.pronamespace
                WHERE schema_entry.nspname = 'public'
                  AND function_entry.proname LIKE 'elp_%'
                """)).IsEqualTo(0);
            await Assert.That(await ScalarIntAsync(
                """
                SELECT count(*)::integer
                FROM pg_catalog.pg_trigger
                WHERE NOT tgisinternal AND tgname LIKE 'tr_elp_%'
                """)).IsEqualTo(0);
            await Assert.That(await ScalarIntAsync(
                """
                SELECT count(*)::integer
                FROM pg_catalog.pg_constraint AS constraint_entry
                JOIN pg_catalog.pg_class AS table_entry
                  ON table_entry.oid = constraint_entry.conrelid
                WHERE table_entry.relname = 'event_report_decision_executions'
                  AND constraint_entry.contype = 'f'
                  AND ARRAY(
                        SELECT attribute_entry.attname
                        FROM unnest(constraint_entry.conkey) WITH ORDINALITY AS key_entry(attnum, ordinal)
                        JOIN pg_catalog.pg_attribute AS attribute_entry
                          ON attribute_entry.attrelid = table_entry.oid
                         AND attribute_entry.attnum = key_entry.attnum
                        ORDER BY key_entry.ordinal)
                      = ARRAY['tenant_id', 'report_id', 'decision_id', 'moderation_record_id']::name[]
                """)).IsEqualTo(1);
            await Assert.That(await ScalarIntAsync(
                """
                SELECT count(*)::integer
                FROM pg_catalog.pg_constraint
                WHERE conname = 'EX_EventSession_RoomNoOverlap'
                  AND conrelid = 'event_sessions'::regclass
                """)).IsEqualTo(1);

            await migrator.MigrateAsync(Migration.InitialDatabase);
            await migrator.MigrateAsync(migrationId);
            await PostgresModelConstraintApplier.ApplyAsync(context);
            await LookupTableSeeder.SeedAsync(context);
            await Assert.That(await ScalarIntAsync(
                "SELECT count(*)::integer FROM \"__EFMigrationsHistory\" WHERE migration_id = @id",
                new NpgsqlParameter("id", migrationId)))
                .IsEqualTo(1);
        }
        finally
        {
            await ResetDatabaseAsync();
        }
    }

    [Test]
    public async Task ExploreInit_LeavesLookupOwnershipToTheRuntimeSeeder()
    {
        await ResetDatabaseAsync();
        try
        {
            await using ExploreDbContext context = CreateExploreContext();
            IMigrator migrator = context.GetService<IMigrator>();
            await migrator.MigrateAsync(MigrationIds(context).Single());

            await Assert.That(await ScalarIntAsync(
                "SELECT count(*)::integer FROM location_kinds")).IsEqualTo(0);
            await LookupTableSeeder.SeedAsync(context);
            await Assert.That(await ScalarIntAsync(
                "SELECT count(*)::integer FROM location_kinds")).IsGreaterThanOrEqualTo(5);
        }
        finally
        {
            await ResetDatabaseAsync();
        }
    }

    [Test]
    public async Task AuthorityInit_IsFunctionOnlyAndProtectsRetainedEvidenceRollback()
    {
        await ResetDatabaseAsync();
        try
        {
            await using PrivacyErasureAuthorityDbContext context = CreateAuthorityContext();
            IMigrator migrator = context.GetService<IMigrator>();
            string migrationId = MigrationIds(context).First();

            await migrator.MigrateAsync(migrationId);
            await migrator.MigrateAsync(migrationId);

            await Assert.That(await ScalarIntAsync(
                """
                SELECT count(*)::integer FROM pg_catalog.pg_roles
                WHERE rolname IN (
                    'privacy_erasure_authority_owner',
                    'privacy_erasure_authority_migrator',
                    'privacy_erasure_authority_runtime')
                  AND NOT rolcanlogin AND NOT rolsuper AND NOT rolcreatedb
                  AND NOT rolcreaterole AND NOT rolinherit AND NOT rolreplication
                  AND NOT rolbypassrls
                """)).IsEqualTo(3);
            await Assert.That(await ScalarIntAsync(
                """
                SELECT count(*)::integer
                FROM pg_catalog.pg_proc AS function_entry
                JOIN pg_catalog.pg_namespace AS schema_entry
                  ON schema_entry.oid = function_entry.pronamespace
                WHERE schema_entry.nspname = 'privacy_erasure_authority'
                  AND function_entry.proname IN (
                      'reject_erasure_intent_mutation',
                      'append_erasure_intent',
                      'read_erasure_intents_after')
                """)).IsEqualTo(3);
            await Assert.That(await ScalarIntAsync(
                """
                SELECT count(*)::integer
                FROM information_schema.role_routine_grants
                WHERE grantee = 'privacy_erasure_authority_runtime'
                  AND routine_schema = 'privacy_erasure_authority'
                  AND privilege_type = 'EXECUTE'
                  AND routine_name IN ('append_erasure_intent', 'read_erasure_intents_after')
                """)).IsEqualTo(2);
            await Assert.That(await ScalarIntAsync(
                """
                SELECT count(*)::integer
                FROM information_schema.role_table_grants
                WHERE grantee = 'privacy_erasure_authority_runtime'
                  AND table_schema = 'privacy_erasure_authority'
                """)).IsEqualTo(0);

            await migrator.MigrateAsync(Migration.InitialDatabase);
            await migrator.MigrateAsync(migrationId);

            await ScalarLongAsync(
                """
                SELECT authority_sequence
                FROM privacy_erasure_authority.append_erasure_intent(
                    uuidv7(), 1::smallint, uuidv7(), 1::smallint, 1)
                """);

            Exception rollback = (await Assert.ThrowsAsync<Exception>(
                () => migrator.MigrateAsync(Migration.InitialDatabase)))!;
            PostgresException postgres = FindPostgresException(rollback)
                ?? throw new InvalidOperationException("Expected a PostgreSQL rollback guard.", rollback);
            await Assert.That(postgres.SqlState).IsEqualTo(PostgresErrorCodes.ObjectNotInPrerequisiteState);
            await Assert.That(await ScalarIntAsync(
                "SELECT count(*)::integer FROM privacy_erasure_authority.erasure_intents"))
                .IsEqualTo(1);
        }
        finally
        {
            await ResetDatabaseAsync();
        }
    }

    [Test]
    public async Task CancelledDedicatedMigration_LeavesNoAuthoritySchemaOrHistory()
    {
        await ResetDatabaseAsync();
        try
        {
            await using PrivacyErasureAuthorityDbContext context = CreateAuthorityContext();
            IMigrator migrator = context.GetService<IMigrator>();
            using var cancellation = new CancellationTokenSource();
            cancellation.Cancel();

            await Assert.ThrowsAsync<OperationCanceledException>(() =>
                migrator.MigrateAsync(MigrationIds(context).Last(), cancellation.Token));

            await Assert.That(await ScalarIntAsync(
                "SELECT count(*)::integer FROM pg_catalog.pg_namespace WHERE nspname = 'privacy_erasure_authority'"))
                .IsEqualTo(0);
            await Assert.That(await ScalarIntAsync(
                "SELECT count(*)::integer FROM information_schema.tables WHERE table_schema = 'public' AND table_name = '__EFMigrationsHistory'"))
                .IsEqualTo(0);
        }
        finally
        {
            await ResetDatabaseAsync();
        }
    }

    [Test]
    public async Task SuccessfulApplicationMigration_DoesNotAuthorizeDedicatedMigrationOnSameTarget()
    {
        await ResetDatabaseAsync();
        try
        {
            await using ExploreDbContext context = CreateExploreContext();
            await context.GetService<IMigrator>().MigrateAsync(MigrationIds(context).Single());
            await Assert.That(await ScalarIntAsync(
                "SELECT count(*)::integer FROM information_schema.tables WHERE table_schema = 'privacy_erasure_authority'"))
                .IsEqualTo(2);
            await Assert.That(await ScalarIntAsync(
                """
                SELECT count(*)::integer
                FROM pg_catalog.pg_proc AS function_entry
                JOIN pg_catalog.pg_namespace AS schema_entry
                  ON schema_entry.oid = function_entry.pronamespace
                WHERE schema_entry.nspname = 'privacy_erasure_authority'
                  AND function_entry.proname IN ('append_erasure_intent', 'read_erasure_intents_after')
                """))
                .IsEqualTo(0);

            var authorityTarget = new NpgsqlConnectionStringBuilder(fixture.ConnectionString)
            {
                ApplicationName = "authority-migrator-canary"
            };
            IConfiguration configuration = new ConfigurationBuilder().AddInMemoryCollection(
                new Dictionary<string, string?>
                {
                    ["PrivacyErasure:Authority:Topology"] = "ExternalDatabase",
                    ["ConnectionStrings:DefaultConnection"] = fixture.ConnectionString,
                    ["ConnectionStrings:PrivacyErasureAuthority"] = authorityTarget.ConnectionString
                }).Build();
            var services = new ServiceCollection();

            OptionsValidationException? exception = await Assert.That(() =>
                    services.ConfigurePersistenceServices(
                        configuration,
                        skipDbContextRegistration: true,
                        skipLookupCacheInitializer: true))
                .Throws<OptionsValidationException>();

            await Assert.That(exception!.Message)
                .Contains("different physical PostgreSQL database", StringComparison.OrdinalIgnoreCase);
            await Assert.That(services.Any(descriptor =>
                descriptor.ServiceType == typeof(IPrivacyErasureAuthority))).IsFalse();
        }
        finally
        {
            await ResetDatabaseAsync();
        }
    }

    private ExploreDbContext CreateExploreContext()
    {
        var options = new DbContextOptionsBuilder<ExploreDbContext>()
            .UseNpgsql(fixture.ConnectionString)
            .UseSnakeCaseNamingConvention()
            .Options;
        return new ExploreDbContext(options);
    }

    private DataProtectionKeyContext CreateDataProtectionContext()
    {
        var options = new DbContextOptionsBuilder<DataProtectionKeyContext>()
            .UseNpgsql(fixture.ConnectionString)
            .UseSnakeCaseNamingConvention()
            .Options;
        return new DataProtectionKeyContext(options);
    }

    private PrivacyErasureAuthorityDbContext CreateAuthorityContext()
    {
        var options = new DbContextOptionsBuilder<PrivacyErasureAuthorityDbContext>()
            .UseNpgsql(fixture.ConnectionString)
            .UseSnakeCaseNamingConvention()
            .Options;
        return new PrivacyErasureAuthorityDbContext(options);
    }

    private static string[] MigrationIds(DbContext context) =>
        context.GetService<IMigrationsAssembly>().Migrations.Keys.Order().ToArray();

    private static Migration InitMigration(DbContext context)
    {
        IMigrationsAssembly assembly = context.GetService<IMigrationsAssembly>();
        KeyValuePair<string, System.Reflection.TypeInfo> item = assembly.Migrations
            .Single(entry => entry.Key.EndsWith("_init", StringComparison.Ordinal));
        return assembly.CreateMigration(item.Value, context.Database.ProviderName!);
    }

    private async Task ResetDatabaseAsync()
    {
        await using var connection = new NpgsqlConnection(fixture.ConnectionString);
        await connection.OpenAsync();
        await using var command = new NpgsqlCommand(
            """
            DO $reset$
            DECLARE schema_name text;
            BEGIN
                FOR schema_name IN
                    SELECT nspname
                    FROM pg_catalog.pg_namespace
                    WHERE nspname NOT IN ('pg_catalog', 'information_schema', 'pg_toast')
                      AND nspname NOT LIKE 'pg_temp_%'
                      AND nspname NOT LIKE 'pg_toast_temp_%'
                LOOP
                    EXECUTE format('DROP SCHEMA %I CASCADE', schema_name);
                END LOOP;
                CREATE SCHEMA public;
            END
            $reset$;
            """,
            connection);
        await command.ExecuteNonQueryAsync();
    }

    private async Task ExecuteAsync(string sql)
    {
        await using var connection = new NpgsqlConnection(fixture.ConnectionString);
        await connection.OpenAsync();
        await using var command = new NpgsqlCommand(sql, connection);
        await command.ExecuteNonQueryAsync();
    }

    private async Task<int> ScalarIntAsync(string sql, params NpgsqlParameter[] parameters)
    {
        await using var connection = new NpgsqlConnection(fixture.ConnectionString);
        await connection.OpenAsync();
        await using var command = new NpgsqlCommand(sql, connection);
        command.Parameters.AddRange(parameters);
        return Convert.ToInt32(await command.ExecuteScalarAsync(), System.Globalization.CultureInfo.InvariantCulture);
    }

    private async Task<long> ScalarLongAsync(string sql)
    {
        await using var connection = new NpgsqlConnection(fixture.ConnectionString);
        await connection.OpenAsync();
        await using var command = new NpgsqlCommand(sql, connection);
        return (long)(await command.ExecuteScalarAsync())!;
    }

    private static PostgresException? FindPostgresException(Exception exception)
    {
        for (Exception? current = exception; current is not null; current = current.InnerException)
        {
            if (current is PostgresException postgres)
            {
                return postgres;
            }
        }

        return null;
    }
}
