// ABOUTME: Contract tests for structured primary database options and native builders.
// ABOUTME: Exercise runtime/migrator roles, validation matrices, redaction, and provider-specific connection-string output.

using Explore.Persistence;
using Explore.Persistence.Database;
using Explore.Secrets.Bootstrap;
using Explore.Secrets.Database;
using FluentAssertions;
using Microsoft.Data.SqlClient;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using MySqlConnector;
using Npgsql;
using TUnit.Core;

namespace Event.Persistence.IntegrationTests.Database;

public sealed class PrimaryDatabaseConfigurationTests
{
    private static IConfiguration BuildConfiguration(IDictionary<string, string?> values) =>
        new ConfigurationBuilder().AddInMemoryCollection(values).Build();

    [Test]
    public void BindRuntime_StructuredSchemaOverridesEnvironmentAlias()
    {
        var configuration = BuildConfiguration(new Dictionary<string, string?>
        {
            ["Database:Provider"] = "PostgreSql",
            ["Database:Host"] = "pg.example.test",
            ["Database:Database"] = "event_db",
            ["Database:Schema"] = "structured_event",
            ["DATABASE_SCHEMA"] = "alias_event",
            ["Database:Runtime:Username"] = "app_user",
            ["Database:Runtime:Password"] = "runtime-secret",
        });

        PrimaryDatabaseConfiguration.BindRuntime(configuration).Schema.Should().Be("structured_event");
    }

    [Test]
    public void BindRuntime_UsesSchemaAliasAndDefault()
    {
        var values = new Dictionary<string, string?>
        {
            ["Database:Provider"] = "PostgreSql",
            ["Database:Host"] = "pg.example.test",
            ["Database:Database"] = "event_db",
            ["Database:Runtime:Username"] = "app_user",
            ["Database:Runtime:Password"] = "runtime-secret",
        };

        PrimaryDatabaseConfiguration.BindRuntime(BuildConfiguration(values)).Schema
            .Should().Be(PrimaryDatabaseConnectionOptions.DefaultSchema);
        values["DATABASE_SCHEMA"] = "alias_event";
        PrimaryDatabaseConfiguration.BindRuntime(BuildConfiguration(values)).Schema.Should().Be("alias_event");
    }

    [Test]
    public void BindRuntime_RejectsUnsupportedPrefixAlias()
    {
        var configuration = BuildConfiguration(new Dictionary<string, string?>
        {
            ["Database:Provider"] = "PostgreSql",
            ["Database:Host"] = "pg.example.test",
            ["Database:Database"] = "event_db",
            ["DATABASE_PREFIX"] = "ie_custom",
            ["Database:Runtime:Username"] = "app_user",
            ["Database:Runtime:Password"] = "runtime-secret",
        });

        Action act = () => PrimaryDatabaseConfiguration.BindRuntime(configuration);

        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*Prefix overrides are not supported*");
    }

    [Test]
    public void BindRuntime_RejectsRuntimePrefixEnvironmentAlias()
    {
        var configuration = BuildConfiguration(new Dictionary<string, string?>
        {
            ["Database:Provider"] = "PostgreSql",
            ["Database:Host"] = "pg.example.test",
            ["Database:Database"] = "event_db",
            ["DATABASE_RUNTIME_PREFIX"] = "ie_custom",
            ["Database:Runtime:Username"] = "app_user",
            ["Database:Runtime:Password"] = "runtime-secret",
        });

        Action act = () => PrimaryDatabaseConfiguration.BindRuntime(configuration);

        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*Prefix overrides are not supported*");
    }

    [Test]
    public void BindRuntime_RejectsStructuredPrefixAlias()
    {
        var configuration = BuildConfiguration(new Dictionary<string, string?>
        {
            ["Database:Provider"] = "PostgreSql",
            ["Database:Host"] = "pg.example.test",
            ["Database:Database"] = "event_db",
            ["Database:Prefix"] = "ie_custom",
            ["Database:Runtime:Username"] = "app_user",
            ["Database:Runtime:Password"] = "runtime-secret",
        });

        Action act = () => PrimaryDatabaseConfiguration.BindRuntime(configuration);

        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*Prefix overrides are not supported*");
    }

    [Test]
    public void BindRuntime_RejectsRuntimeStructuredPrefixAlias()
    {
        var configuration = BuildConfiguration(new Dictionary<string, string?>
        {
            ["Database:Provider"] = "PostgreSql",
            ["Database:Host"] = "pg.example.test",
            ["Database:Database"] = "event_db",
            ["Database:Runtime:Prefix"] = "ie_custom",
            ["Database:Runtime:Username"] = "app_user",
            ["Database:Runtime:Password"] = "runtime-secret",
        });

        Action act = () => PrimaryDatabaseConfiguration.BindRuntime(configuration);

        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*Prefix overrides are not supported*");
    }

    [Test]
    public void BindMigrator_RejectsUnsupportedPrefixAlias()
    {
        var configuration = BuildConfiguration(new Dictionary<string, string?>
        {
            ["Database:Provider"] = "PostgreSql",
            ["Database:Host"] = "pg.example.test",
            ["Database:Database"] = "event_db",
            ["DATABASE_PREFIX"] = "ie_custom",
            ["Database:Migrator:Username"] = "migrator_user",
            ["Database:Migrator:Password"] = "migrator-secret",
        });

        Action act = () => PrimaryDatabaseConfiguration.BindMigrator(configuration);

        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*Prefix overrides are not supported*");
    }

    [Test]
    public void BindMigrator_RejectsMigratorPrefixEnvironmentAlias()
    {
        var configuration = BuildConfiguration(new Dictionary<string, string?>
        {
            ["Database:Provider"] = "PostgreSql",
            ["Database:Host"] = "pg.example.test",
            ["Database:Database"] = "event_db",
            ["DATABASE_MIGRATOR_PREFIX"] = "ie_custom",
            ["Database:Migrator:Username"] = "migrator_user",
            ["Database:Migrator:Password"] = "migrator-secret",
        });

        Action act = () => PrimaryDatabaseConfiguration.BindMigrator(configuration);

        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*Prefix overrides are not supported*");
    }

    [Test]
    public void BindMigrator_RejectsStructuredPrefixAlias()
    {
        var configuration = BuildConfiguration(new Dictionary<string, string?>
        {
            ["Database:Provider"] = "PostgreSql",
            ["Database:Host"] = "pg.example.test",
            ["Database:Database"] = "event_db",
            ["Database:Prefix"] = "ie_custom",
            ["Database:Migrator:Username"] = "migrator_user",
            ["Database:Migrator:Password"] = "migrator-secret",
        });

        Action act = () => PrimaryDatabaseConfiguration.BindMigrator(configuration);

        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*Prefix overrides are not supported*");
    }

    [Test]
    public void BindMigrator_RejectsMigratorStructuredPrefixAlias()
    {
        var configuration = BuildConfiguration(new Dictionary<string, string?>
        {
            ["Database:Provider"] = "PostgreSql",
            ["Database:Host"] = "pg.example.test",
            ["Database:Database"] = "event_db",
            ["Database:Migrator:Prefix"] = "ie_custom",
            ["Database:Migrator:Username"] = "migrator_user",
            ["Database:Migrator:Password"] = "migrator-secret",
        });

        Action act = () => PrimaryDatabaseConfiguration.BindMigrator(configuration);

        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*Prefix overrides are not supported*");
    }

    [Test]
    public void BuildConnectionString_PostgreSqlCustomSchemaSetsSessionSearchPath()
    {
        var options = new PrimaryDatabaseConnectionOptions
        {
            Role = PrimaryDatabaseRole.Runtime,
            Provider = PrimaryDatabaseProvider.PostgreSql,
            Host = "pg.example.test",
            Database = "event_db",
            Schema = "custom_event",
            Username = "app_user",
            Password = "runtime-secret",
            TlsMode = PrimaryDatabaseTlsMode.Disabled,
        };

        var result = PrimaryDatabaseConfiguration.BuildConnectionString(options);

        new NpgsqlConnectionStringBuilder(result.ConnectionString).SearchPath
            .Should().Be("custom_event");
    }

    [Test]
    public void BuildConnectionString_PostgreSqlMigratorSearchPathKeepsPublicBootstrapFallback()
    {
        var options = new PrimaryDatabaseConnectionOptions
        {
            Role = PrimaryDatabaseRole.Migrator,
            Provider = PrimaryDatabaseProvider.PostgreSql,
            Host = "pg.example.test",
            Database = "event_db",
            Schema = "custom_event",
            Username = "migrator_user",
            Password = "migrator-secret",
            TlsMode = PrimaryDatabaseTlsMode.Disabled,
        };

        var result = PrimaryDatabaseConfiguration.BuildConnectionString(options);

        new NpgsqlConnectionStringBuilder(result.ConnectionString).SearchPath
            .Should().Be("custom_event, public");
    }

    [Test]
    [Arguments("bad-schema")]
    [Arguments("9bad")]
    [Arguments("schema;drop")]
    [Arguments("aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa")]
    public void BindRuntime_RejectsUnsafeSchema(string schema)
    {
        var configuration = BuildConfiguration(new Dictionary<string, string?>
        {
            ["Database:Provider"] = "PostgreSql",
            ["Database:Host"] = "pg.example.test",
            ["Database:Database"] = "event_db",
            ["Database:Schema"] = schema,
            ["Database:Runtime:Username"] = "app_user",
            ["Database:Runtime:Password"] = "runtime-secret",
        });

        var action = () => PrimaryDatabaseConfiguration.BindRuntime(configuration);
        action.Should().Throw<OptionsValidationException>();
    }

    [Test]
    public void BindRuntime_WithPostgreSqlAndSharedEndpoint_ComposesRuntimeOptions()
    {
        var configuration = BuildConfiguration(new Dictionary<string, string?>
        {
            ["Database:Provider"] = "PostgreSql",
            ["Database:Host"] = "pg.example.test",
            ["Database:Database"] = "event_db",
            ["Database:Runtime:Username"] = "app_user",
            ["Database:Runtime:Password"] = "runtime-secret",
            ["Database:Runtime:TlsMode"] = "Required",
            ["Database:Runtime:TrustServerCertificate"] = "true",
        });

        var options = PrimaryDatabaseConfiguration.BindRuntime(configuration);
        var result = PrimaryDatabaseConfiguration.BuildConnectionString(options);

        options.Role.Should().Be(PrimaryDatabaseRole.Runtime);
        options.Provider.Should().Be(PrimaryDatabaseProvider.PostgreSql);
        options.Port.Should().BeNull();
        options.TlsMode.Should().Be(PrimaryDatabaseTlsMode.Required);
        options.TrustServerCertificate.Should().BeTrue();

        var parsed = new NpgsqlConnectionStringBuilder(result.ConnectionString);
        parsed.Host.Should().Be("pg.example.test");
        parsed.Port.Should().Be(5432);
        parsed.Database.Should().Be("event_db");
        parsed.Username.Should().Be("app_user");
        parsed.Password.Should().Be("runtime-secret");
        parsed.SslMode.Should().Be(SslMode.Require);
        result.RedactedConnectionString.Should().NotContain("runtime-secret");
        result.SafeSummary.Should().Contain("Runtime:PostgreSql");
        result.SafeSummary.Should().Contain("tls=Required");
    }

    [Test]
    public void BindMigrator_WithDistinctCredentials_ComposesMigratorOptions()
    {
        var configuration = BuildConfiguration(new Dictionary<string, string?>
        {
            ["Database:Provider"] = "PostgreSql",
            ["Database:Host"] = "pg.example.test",
            ["Database:Database"] = "event_db",
            ["Database:Migrator:Username"] = "migrator_user",
            ["Database:Migrator:Password"] = "migrator-secret",
        });

        var options = PrimaryDatabaseConfiguration.BindMigrator(configuration);
        var result = PrimaryDatabaseConfiguration.BuildConnectionString(options);

        options.Role.Should().Be(PrimaryDatabaseRole.Migrator);
        new NpgsqlConnectionStringBuilder(result.ConnectionString).Username.Should().Be("migrator_user");
        result.RedactedConnectionString.Should().NotContain("migrator-secret");
    }

    [Test]
    public void BindRuntime_WithSqlite_UsesLocalFilePath()
    {
        var configuration = BuildConfiguration(new Dictionary<string, string?>
        {
            ["Database:Provider"] = "Sqlite",
            ["Database:Database"] = "/tmp/event-primary.db",
        });

        var options = PrimaryDatabaseConfiguration.BindRuntime(configuration);
        var result = PrimaryDatabaseConfiguration.BuildConnectionString(options);

        options.Provider.Should().Be(PrimaryDatabaseProvider.Sqlite);
        options.Role.Should().Be(PrimaryDatabaseRole.Runtime);
        options.Database.Should().Be("/tmp/event-primary.db");

        var parsed = new SqliteConnectionStringBuilder(result.ConnectionString);
        parsed.DataSource.Should().Be("/tmp/event-primary.db");
        parsed.Mode.Should().Be(SqliteOpenMode.ReadWriteCreate);
        parsed.DefaultTimeout.Should().Be(30);
        result.RedactedConnectionString.Should().NotContain("runtime-secret");
    }

    [Test]
    public void BindRuntime_WithSqlServer_UsesDefaultPortAndNativeBuilder()
    {
        var configuration = BuildConfiguration(new Dictionary<string, string?>
        {
            ["Database:Provider"] = "SqlServer",
            ["Database:Host"] = "sql.example.test",
            ["Database:Database"] = "event_db",
            ["Database:Runtime:Username"] = "sql_user",
            ["Database:Runtime:Password"] = "sql-secret",
            ["Database:Runtime:TlsMode"] = "Required",
        });

        var result = PrimaryDatabaseConfiguration.BuildConnectionString(PrimaryDatabaseConfiguration.BindRuntime(configuration));
        var parsed = new SqlConnectionStringBuilder(result.ConnectionString);

        parsed.ConnectionString.Should().Contain("Data Source=sql.example.test");
        parsed.InitialCatalog.Should().Be("event_db");
        parsed.UserID.Should().Be("sql_user");
        parsed.Password.Should().Be("sql-secret");
        parsed.ConnectionString.Should().Contain("Encrypt=True");
        result.SafeSummary.Should().Contain("port=1433");
        result.RedactedConnectionString.Should().NotContain("sql-secret");
    }

    [Test]
    public void BindRuntime_WithMariaDb_RequiresFlavorAndVersion()
    {
        var configuration = BuildConfiguration(new Dictionary<string, string?>
        {
            ["Database:Provider"] = "MariaDb",
            ["Database:Host"] = "mariadb.example.test",
            ["Database:Database"] = "event_db",
            ["Database:Runtime:Username"] = "db_user",
            ["Database:Runtime:Password"] = "db-secret",
            ["Database:Runtime:ServerFlavor"] = "MariaDb",
            ["Database:Runtime:ServerVersion"] = "10.11",
            ["Database:Runtime:TlsMode"] = "Required",
        });

        var options = PrimaryDatabaseConfiguration.BindRuntime(configuration);
        var result = PrimaryDatabaseConfiguration.BuildConnectionString(options);
        var parsed = new MySqlConnectionStringBuilder(result.ConnectionString);

        options.ServerFlavor.Should().Be(PrimaryDatabaseServerFlavor.MariaDb);
        options.ServerVersion.Should().Be(new Version(10, 11));
        parsed.Server.Should().Be("mariadb.example.test");
        parsed.Port.Should().Be(3306);
        parsed.UserID.Should().Be("db_user");
        parsed.Password.Should().Be("db-secret");
        result.SafeSummary.Should().Contain("MariaDb");
        result.SafeSummary.Should().Contain("port=3306");
    }

    [Test]
    public void BindRuntime_WithMySql_RequiresFlavorAndVersion()
    {
        var configuration = BuildConfiguration(new Dictionary<string, string?>
        {
            ["Database:Provider"] = "MySql",
            ["Database:Host"] = "mysql.example.test",
            ["Database:Database"] = "event_db",
            ["Database:Runtime:Username"] = "db_user",
            ["Database:Runtime:Password"] = "db-secret",
            ["Database:Runtime:ServerFlavor"] = "MySql",
            ["Database:Runtime:ServerVersion"] = "8.0",
            ["Database:Runtime:TlsMode"] = "Prefer",
        });

        var options = PrimaryDatabaseConfiguration.BindRuntime(configuration);
        var result = PrimaryDatabaseConfiguration.BuildConnectionString(options);
        var parsed = new MySqlConnectionStringBuilder(result.ConnectionString);

        options.ServerFlavor.Should().Be(PrimaryDatabaseServerFlavor.MySql);
        options.ServerVersion.Should().Be(new Version(8, 0));
        parsed.Server.Should().Be("mysql.example.test");
        parsed.Port.Should().Be(3306);
        parsed.UserID.Should().Be("db_user");
        parsed.Password.Should().Be("db-secret");
        result.SafeSummary.Should().Contain("MySql");
    }

    [Test]
    public void Validate_RejectsSqliteWithServerFields()
    {
        var configuration = BuildConfiguration(new Dictionary<string, string?>
        {
            ["Database:Provider"] = "Sqlite",
            ["Database:Database"] = "/tmp/event.db",
            ["Database:Runtime:Host"] = "not-allowed",
            ["Database:Runtime:Username"] = "not-allowed",
            ["Database:Runtime:Password"] = "not-allowed",
        });

        Action act = () => PrimaryDatabaseConfiguration.BindRuntime(configuration);

        act.Should().Throw<OptionsValidationException>();
    }

    [Test]
    public void Validate_RejectsUnknownProvider()
    {
        var configuration = BuildConfiguration(new Dictionary<string, string?>
        {
            ["Database:Provider"] = "Oracle",
        });

        Action act = () => PrimaryDatabaseConfiguration.BindRuntime(configuration);

        act.Should().Throw<InvalidOperationException>();
    }

    [Test]
    [Arguments("1")]
    [Arguments("999")]
    public void BindRuntime_RejectsNumericProviderValues(string provider)
    {
        var configuration = BuildConfiguration(new Dictionary<string, string?>
        {
            ["Database:Provider"] = provider,
        });

        Action act = () => PrimaryDatabaseConfiguration.BindRuntime(configuration);

        act.Should().Throw<InvalidOperationException>();
    }

    [Test]
    [Arguments("1")]
    [Arguments("999")]
    public void BindRuntime_RejectsNumericTlsModeValues(string tlsMode)
    {
        var configuration = BuildConfiguration(new Dictionary<string, string?>
        {
            ["Database:Provider"] = "PostgreSql",
            ["Database:Host"] = "pg.example.test",
            ["Database:Database"] = "event_db",
            ["Database:Runtime:Username"] = "app_user",
            ["Database:Runtime:Password"] = "app-secret",
            ["Database:Runtime:TlsMode"] = tlsMode,
        });

        Action act = () => PrimaryDatabaseConfiguration.BindRuntime(configuration);

        act.Should().Throw<InvalidOperationException>();
    }

    [Test]
    [Arguments("1")]
    [Arguments("999")]
    public void BindRuntime_RejectsNumericServerFlavorValues(string serverFlavor)
    {
        var configuration = BuildConfiguration(new Dictionary<string, string?>
        {
            ["Database:Provider"] = "MySql",
            ["Database:Host"] = "mysql.example.test",
            ["Database:Database"] = "event_db",
            ["Database:Runtime:Username"] = "app_user",
            ["Database:Runtime:Password"] = "app-secret",
            ["Database:Runtime:ServerFlavor"] = serverFlavor,
            ["Database:Runtime:ServerVersion"] = "8.0",
        });

        Action act = () => PrimaryDatabaseConfiguration.BindRuntime(configuration);

        act.Should().Throw<InvalidOperationException>();
    }

    [Test]
    [Arguments(":memory:")]
    [Arguments("file::memory:")]
    [Arguments("file::memory:?cache=shared")]
    [Arguments("file:shared?mode=memory&cache=shared")]
    [Arguments("file:shared?cache=shared&mode=memory")]
    public void BindRuntime_RejectsNonPersistedSqliteDatabase(string database)
    {
        var configuration = BuildConfiguration(new Dictionary<string, string?>
        {
            ["Database:Provider"] = "Sqlite",
            ["Database:Database"] = database,
        });

        Action act = () => PrimaryDatabaseConfiguration.BindRuntime(configuration);

        act.Should().Throw<OptionsValidationException>();
    }

    [Test]
    [Arguments("//database-server/event.db")]
    [Arguments("\\\\database-server\\share\\event.db")]
    [Arguments("file:///app/data/event.db")]
    [Arguments("/app/data/privacy_erasure_authority.db")]
    [Arguments("privacy_erasure_authority.db")]
    public void BindRuntime_RejectsNonLocalOrReservedSqliteDatabase(string database)
    {
        var configuration = BuildConfiguration(new Dictionary<string, string?>
        {
            ["Database:Provider"] = "Sqlite",
            ["Database:Database"] = database,
        });

        Action act = () => PrimaryDatabaseConfiguration.BindRuntime(configuration);

        act.Should().Throw<OptionsValidationException>();
    }

    [Test]
    public async Task SqliteInitializer_PersistsWalAndConfiguredBusyTimeoutAcrossReopen()
    {
        var databasePath = Path.Combine(Path.GetTempPath(), $"event-primary-{Guid.NewGuid():N}.db");
        var result = PrimaryDatabaseConfiguration.BuildConnectionString(new PrimaryDatabaseConnectionOptions
        {
            Role = PrimaryDatabaseRole.Runtime,
            Provider = PrimaryDatabaseProvider.Sqlite,
            Database = databasePath,
        });

        try
        {
            await using (var context = new DbContext(new DbContextOptionsBuilder()
                .UseSqlite(result.ConnectionString)
                .Options))
            {
                await SqliteDatabaseInitializer.InitializeAsync(context, CancellationToken.None);
            }

            await using var connection = new SqliteConnection(result.ConnectionString);
            await connection.OpenAsync(CancellationToken.None);
            await using var command = connection.CreateCommand();
            command.CommandTimeout.Should().Be(30);
            command.CommandText = "PRAGMA journal_mode;";
            (await command.ExecuteScalarAsync(CancellationToken.None)).Should().Be("wal");
        }
        finally
        {
            File.Delete(databasePath);
            File.Delete($"{databasePath}-shm");
            File.Delete($"{databasePath}-wal");
        }
    }

    [Test]
    public async Task SqliteInitializer_WithNonSqliteProvider_DoesNotExecuteSqliteSql()
    {
        await using var context = new DbContext(new DbContextOptionsBuilder()
            .UseInMemoryDatabase($"sqlite-initializer-{Guid.NewGuid():N}")
            .Options);

        var act = () => SqliteDatabaseInitializer.InitializeAsync(context, CancellationToken.None);

        await act.Should().NotThrowAsync();
    }

    [Test]
    public void BuildConnectionString_PostgreSqlRequiredTlsWithoutTrustBypass_VerifiesServerIdentity()
    {
        var configuration = BuildConfiguration(new Dictionary<string, string?>
        {
            ["Database:Provider"] = "PostgreSql",
            ["Database:Host"] = "pg.example.test",
            ["Database:Database"] = "event_db",
            ["Database:Runtime:Username"] = "app_user",
            ["Database:Runtime:Password"] = "app-secret",
            ["Database:Runtime:TlsMode"] = "Required",
        });

        var result = PrimaryDatabaseConfiguration.ResolveRuntimeConnectionString(configuration);

        new NpgsqlConnectionStringBuilder(result.ConnectionString).SslMode.Should().Be(SslMode.VerifyFull);
    }

    [Test]
    public void BuildConnectionString_PostgreSqlDefaultTls_VerifiesServerIdentity()
    {
        var configuration = BuildConfiguration(new Dictionary<string, string?>
        {
            ["Database:Provider"] = "PostgreSql",
            ["Database:Host"] = "pg.example.test",
            ["Database:Database"] = "event_db",
            ["Database:Runtime:Username"] = "app_user",
            ["Database:Runtime:Password"] = "app-secret",
        });

        var result = PrimaryDatabaseConfiguration.ResolveRuntimeConnectionString(configuration);

        new NpgsqlConnectionStringBuilder(result.ConnectionString).SslMode.Should().Be(SslMode.VerifyFull);
    }

    [Test]
    public void BuildConnectionString_PostgreSqlRequiredTlsWithExplicitTrustBypass_RequiresEncryptionWithoutVerification()
    {
        var configuration = BuildConfiguration(new Dictionary<string, string?>
        {
            ["Database:Provider"] = "PostgreSql",
            ["Database:Host"] = "pg.example.test",
            ["Database:Database"] = "event_db",
            ["Database:Runtime:Username"] = "app_user",
            ["Database:Runtime:Password"] = "app-secret",
            ["Database:Runtime:TlsMode"] = "Required",
            ["Database:Runtime:TrustServerCertificate"] = "true",
        });

        var result = PrimaryDatabaseConfiguration.ResolveRuntimeConnectionString(configuration);

        new NpgsqlConnectionStringBuilder(result.ConnectionString).SslMode.Should().Be(SslMode.Require);
    }

    [Test]
    public void BindRuntime_RejectsTrustBypassWithoutRequiredTls()
    {
        var configuration = BuildConfiguration(new Dictionary<string, string?>
        {
            ["Database:Provider"] = "PostgreSql",
            ["Database:Host"] = "pg.example.test",
            ["Database:Database"] = "event_db",
            ["Database:Runtime:Username"] = "app_user",
            ["Database:Runtime:Password"] = "app-secret",
            ["Database:Runtime:TlsMode"] = "Prefer",
            ["Database:Runtime:TrustServerCertificate"] = "true",
        });

        Action act = () => PrimaryDatabaseConfiguration.BindRuntime(configuration);

        act.Should().Throw<OptionsValidationException>();
    }

    [Test]
    public void Validate_RejectsSqlServerMissingDatabase()
    {
        var configuration = BuildConfiguration(new Dictionary<string, string?>
        {
            ["Database:Provider"] = "SqlServer",
            ["Database:Host"] = "sql.example.test",
            ["Database:Runtime:Username"] = "sql_user",
            ["Database:Runtime:Password"] = "sql-secret",
        });

        Action act = () => PrimaryDatabaseConfiguration.BindRuntime(configuration);

        act.Should().Throw<OptionsValidationException>();
    }

    [Test]
    [Arguments("Development")]
    [Arguments("Testing")]
    [Arguments(null)]
    [Arguments("Production")]
    public void ResolveRuntimeConnectionString_WithLegacyConnectionString_RejectsEveryEnvironment(
        string? environmentName)
    {
        var configuration = BuildConfiguration(new Dictionary<string, string?>
        {
            ["ConnectionStrings:DefaultConnection"] = "Host=localhost;Database=legacy;Username=postgres;Password=secret",
            ["ASPNETCORE_ENVIRONMENT"] = environmentName,
        });

        Action act = () => PrimaryDatabaseConfiguration.ResolveRuntimeConnectionString(configuration);

        act.Should().Throw<InvalidOperationException>();
    }

    [Test]
    public void ConfigurePersistenceServices_WhenDatabaseRegistrationIsSkipped_DoesNotRequireDatabaseConfiguration()
    {
        var configuration = BuildConfiguration(new Dictionary<string, string?>());
        var services = new ServiceCollection();

        Action act = () => services.ConfigurePersistenceServices(
            configuration,
            skipDbContextRegistration: true,
            skipLookupCacheInitializer: true,
            environmentName: "Production");

        act.Should().NotThrow();
    }

    [Test]
    public void ConfigurePersistenceServices_WhenDatabaseRegistrationIsActive_RequiresStructuredConfiguration()
    {
        var configuration = BuildConfiguration(new Dictionary<string, string?>());
        var services = new ServiceCollection();

        Action act = () => services.ConfigurePersistenceServices(
            configuration,
            skipDbContextRegistration: false,
            skipLookupCacheInitializer: true,
            environmentName: "Production");

        act.Should().Throw<InvalidOperationException>();
    }

    [Test]
    public void ConfigurePersistenceServices_WithProjectedDiscretePostgres_UsesSharedStructuredBinder()
    {
        var builder = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["Postgresql:Host"] = "pg.example.test",
            ["Postgresql:Port"] = "5432",
            ["Postgresql:Database"] = "event_db",
            ["Postgresql:Username"] = "app_user",
            ["Postgresql:Password"] = "app-secret",
        });
        BootstrapSecretLoader.ProjectPostgresConfiguration(
            builder,
            PrimaryDatabaseRole.Runtime,
            infisicalAlreadyLoaded: true);
        var services = new ServiceCollection();

        Action act = () => services.ConfigurePersistenceServices(
            builder.Build(),
            skipDbContextRegistration: false,
            skipLookupCacheInitializer: true,
            environmentName: "Production");

        act.Should().NotThrow();
    }

    [Test]
    public void BuildConnectionString_RedactsSensitiveValues()
    {
        var configuration = BuildConfiguration(new Dictionary<string, string?>
        {
            ["Database:Provider"] = "PostgreSql",
            ["Database:Host"] = "pg.example.test",
            ["Database:Database"] = "event_db",
            ["Database:Runtime:Username"] = "app_user",
            ["Database:Runtime:Password"] = "MDB_SECRET_SENTINEL",
        });

        var result = PrimaryDatabaseConfiguration.BuildConnectionString(PrimaryDatabaseConfiguration.BindRuntime(configuration));

        result.RedactedConnectionString.Should().NotContain("MDB_SECRET_SENTINEL");
        result.SafeSummary.Should().NotContain("MDB_SECRET_SENTINEL");
    }
}
