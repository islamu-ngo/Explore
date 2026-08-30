// ABOUTME: Unit tests for BootstrapSecretLoader covering discrete POSTGRESQL_* resolution.
// ABOUTME: Verifies source precedence, structured projection, validation errors, and native connection composition.

using Explore.Secrets.Bootstrap;
using Explore.Secrets.Database;
using Microsoft.Extensions.Configuration;
using Npgsql;
using TUnit.Core;

namespace Explore.Secrets.UnitTests.Bootstrap;

// All tests in this class run sequentially because they manipulate process environment variables,
// which are global state. Running in parallel would cause cross-test contamination.
[NotInParallel]
public class BootstrapSecretLoaderTests
{
    private const string HostKey = "Postgresql:Host";
    private const string PortKey = "Postgresql:Port";
    private const string DbKey = "Postgresql:Database";
    private const string UserKey = "Postgresql:Username";
    private const string PassKey = "Postgresql:Password";

    private const string HostEnv = "POSTGRESQL_HOST";
    private const string PortEnv = "POSTGRESQL_PORT";
    private const string DbEnv = "POSTGRESQL_DATABASE";
    private const string UserEnv = "POSTGRESQL_USERNAME";
    private const string PassEnv = "POSTGRESQL_PASSWORD";

    private static readonly string[] AllEnvVars =
    [
        HostEnv, PortEnv, DbEnv, UserEnv, PassEnv,
    ];

    private static IConfiguration BuildConfig(IDictionary<string, string?> values) =>
        new ConfigurationBuilder().AddInMemoryCollection(values).Build();

    private static void ClearEnv()
    {
        foreach (var key in AllEnvVars)
        {
            Environment.SetEnvironmentVariable(key, null);
        }
    }

    #region Config Resolution

    [Test]
    public async Task LoadPostgresConnectionString_WithAllConfigValues_ComposesCorrectConnectionString()
    {
        ClearEnv();
        string password = SecretsTestValues.CreateSecret();

        var config = BuildConfig(new Dictionary<string, string?>
        {
            [HostKey] = "db.example.com",
            [PortKey] = "6543",
            [DbKey] = "events",
            [UserKey] = "svc_events",
            [PassKey] = password,
        });

        var credentials = BootstrapSecretLoader.LoadPostgresConnectionString(config);

        var parsed = new NpgsqlConnectionStringBuilder(credentials.ConnectionString);
        await Assert.That(parsed.Host).IsEqualTo("db.example.com");
        await Assert.That(parsed.Port).IsEqualTo(6543);
        await Assert.That(parsed.Database).IsEqualTo("events");
        await Assert.That(parsed.Username).IsEqualTo("svc_events");
        await Assert.That(parsed.Password).IsEqualTo(password);
        await Assert.That(parsed.SslMode).IsEqualTo(SslMode.Prefer);

        await Assert.That(credentials.Source).Contains("Config");
        await Assert.That(credentials.LoadedAt).IsNotEqualTo(default);
        await Assert.That(credentials.LoadedAt.Offset).IsEqualTo(TimeSpan.Zero);
    }

    [Test]
    public async Task LoadPostgresConnectionString_WithoutPort_UsesDefault5432()
    {
        ClearEnv();

        var config = BuildConfig(new Dictionary<string, string?>
        {
            [HostKey] = "localhost",
            [DbKey] = "db",
            [UserKey] = "u",
            [PassKey] = "p",
        });

        var credentials = BootstrapSecretLoader.LoadPostgresConnectionString(config);

        await Assert.That(new NpgsqlConnectionStringBuilder(credentials.ConnectionString).Port).IsEqualTo(5432);
    }

    [Test]
    public async Task LoadPostgresConnectionString_WithMalformedPort_FallsBackToDefault()
    {
        ClearEnv();

        var config = BuildConfig(new Dictionary<string, string?>
        {
            [HostKey] = "localhost",
            [PortKey] = "abc123",
            [DbKey] = "db",
            [UserKey] = "u",
            [PassKey] = "p",
        });

        var credentials = BootstrapSecretLoader.LoadPostgresConnectionString(config);

        await Assert.That(new NpgsqlConnectionStringBuilder(credentials.ConnectionString).Port).IsEqualTo(5432);
    }

    [Test]
    public async Task ProjectPostgresConfiguration_WithDiscreteFields_BindsMigratorRole()
    {
        ClearEnv();
        string password = SecretsTestValues.CreateSecret();
        var builder = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
        {
            [HostKey] = "migration-db.example.test",
            [PortKey] = "6543",
            [DbKey] = "event_db",
            [UserKey] = "migrator_user",
            [PassKey] = password,
        });

        BootstrapSecretLoader.ProjectPostgresConfiguration(
            builder,
            PrimaryDatabaseRole.Migrator,
            infisicalAlreadyLoaded: true);
        var options = PrimaryDatabaseConfiguration.BindMigrator(builder.Build());

        await Assert.That(options.Role).IsEqualTo(PrimaryDatabaseRole.Migrator);
        await Assert.That(options.Provider).IsEqualTo(PrimaryDatabaseProvider.PostgreSql);
        await Assert.That(options.Host).IsEqualTo("migration-db.example.test");
        await Assert.That(options.Port).IsEqualTo(6543);
        await Assert.That(options.Username).IsEqualTo("migrator_user");
        await Assert.That(options.Password).IsEqualTo(password);
    }

    #endregion

    #region Environment Variable Resolution

    [Test]
    public async Task LoadPostgresConnectionString_WithEnvironmentVariables_ComposesCorrectConnectionString()
    {
        ClearEnv();
        try
        {
            Environment.SetEnvironmentVariable(HostEnv, "env-host");
            Environment.SetEnvironmentVariable(PortEnv, "7777");
            Environment.SetEnvironmentVariable(DbEnv, "env_db");
            Environment.SetEnvironmentVariable(UserEnv, "env_user");
            Environment.SetEnvironmentVariable(PassEnv, "env_pass");

            var config = BuildConfig(new Dictionary<string, string?>());

            var credentials = BootstrapSecretLoader.LoadPostgresConnectionString(config);

            var parsed = new NpgsqlConnectionStringBuilder(credentials.ConnectionString);
            await Assert.That(parsed.Host).IsEqualTo("env-host");
            await Assert.That(parsed.Port).IsEqualTo(7777);
            await Assert.That(parsed.Database).IsEqualTo("env_db");
            await Assert.That(parsed.Username).IsEqualTo("env_user");
            await Assert.That(parsed.Password).IsEqualTo("env_pass");

            await Assert.That(credentials.Source).Contains("Env");
        }
        finally
        {
            ClearEnv();
        }
    }

    [Test]
    public async Task LoadPostgresConnectionString_EnvWinsOverConfig()
    {
        ClearEnv();
        try
        {
            Environment.SetEnvironmentVariable(HostEnv, "env-host");
            Environment.SetEnvironmentVariable(PortEnv, "6543");
            Environment.SetEnvironmentVariable(DbEnv, "env_db");
            Environment.SetEnvironmentVariable(UserEnv, "env_user");
            Environment.SetEnvironmentVariable(PassEnv, "env_pass");

            var config = BuildConfig(new Dictionary<string, string?>
            {
                [HostKey] = "config-host",
                [PortKey] = "5432",
                [DbKey] = "config_db",
                [UserKey] = "config_user",
                [PassKey] = "config_pass",
            });

            var credentials = BootstrapSecretLoader.LoadPostgresConnectionString(config);

            var parsed = new NpgsqlConnectionStringBuilder(credentials.ConnectionString);
            await Assert.That(parsed.Host).IsEqualTo("env-host");
            await Assert.That(parsed.Database).IsEqualTo("env_db");
            await Assert.That(parsed.Username).IsEqualTo("env_user");
            await Assert.That(parsed.Password).IsEqualTo("env_pass");
        }
        finally
        {
            ClearEnv();
        }
    }

    [Test]
    public async Task LoadPostgresConnectionString_MixedSources_LabelsSourceAsMixed()
    {
        ClearEnv();
        try
        {
            Environment.SetEnvironmentVariable(HostEnv, "env-host");

            var config = BuildConfig(new Dictionary<string, string?>
            {
                [PortKey] = "5432",
                [DbKey] = "db",
                [UserKey] = "u",
                [PassKey] = "p",
            });

            var credentials = BootstrapSecretLoader.LoadPostgresConnectionString(config);

            await Assert.That(credentials.Source).Contains("Mixed");
            await Assert.That(credentials.Source).Contains("Env");
            await Assert.That(credentials.Source).Contains("Config");
        }
        finally
        {
            ClearEnv();
        }
    }

    #endregion

    #region Missing Field Errors

    [Test]
    public async Task LoadPostgresConnectionString_MissingHost_ThrowsInvalidOperationException()
    {
        ClearEnv();

        var config = BuildConfig(new Dictionary<string, string?>
        {
            [DbKey] = "db",
            [UserKey] = "u",
            [PassKey] = "p",
        });

        var act = () => BootstrapSecretLoader.LoadPostgresConnectionString(config);

        await Assert.That(act).Throws<InvalidOperationException>()
            .WithMessageContaining("Host");
    }

    [Test]
    public async Task LoadPostgresConnectionString_MissingDatabase_ThrowsInvalidOperationException()
    {
        ClearEnv();

        var config = BuildConfig(new Dictionary<string, string?>
        {
            [HostKey] = "localhost",
            [UserKey] = "u",
            [PassKey] = "p",
        });

        var act = () => BootstrapSecretLoader.LoadPostgresConnectionString(config);

        await Assert.That(act).Throws<InvalidOperationException>()
            .WithMessageContaining("Database");
    }

    [Test]
    public async Task LoadPostgresConnectionString_MissingUsername_ThrowsInvalidOperationException()
    {
        ClearEnv();

        var config = BuildConfig(new Dictionary<string, string?>
        {
            [HostKey] = "localhost",
            [DbKey] = "db",
            [PassKey] = "p",
        });

        var act = () => BootstrapSecretLoader.LoadPostgresConnectionString(config);

        await Assert.That(act).Throws<InvalidOperationException>()
            .WithMessageContaining("Username");
    }

    [Test]
    public async Task LoadPostgresConnectionString_MissingPassword_ThrowsInvalidOperationException()
    {
        ClearEnv();

        var config = BuildConfig(new Dictionary<string, string?>
        {
            [HostKey] = "localhost",
            [DbKey] = "db",
            [UserKey] = "u",
        });

        var act = () => BootstrapSecretLoader.LoadPostgresConnectionString(config);

        await Assert.That(act).Throws<InvalidOperationException>()
            .WithMessageContaining("Password");
    }

    [Test]
    public async Task LoadPostgresConnectionString_AllMissing_ErrorListsEveryMissingField()
    {
        ClearEnv();

        var config = BuildConfig(new Dictionary<string, string?>());

        var act = () => BootstrapSecretLoader.LoadPostgresConnectionString(config);

        var ex = (await Assert.That(act).Throws<InvalidOperationException>())!;
        await Assert.That(ex.Message).Contains("Host");
        await Assert.That(ex.Message).Contains("Database");
        await Assert.That(ex.Message).Contains("Username");
        await Assert.That(ex.Message).Contains("Password");
    }

    #endregion
}
