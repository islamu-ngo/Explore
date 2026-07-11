// ABOUTME: Unit tests for BootstrapSecretLoader covering discrete POSTGRESQL_* resolution.
// Verifies env-over-config precedence, NpgsqlConnectionStringBuilder composition, missing-field errors, and port parsing.

using Explore.Secrets.Bootstrap;
using FluentAssertions;
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
    public void LoadPostgresConnectionString_WithAllConfigValues_ComposesCorrectConnectionString()
    {
        ClearEnv();

        var config = BuildConfig(new Dictionary<string, string?>
        {
            [HostKey] = "db.example.com",
            [PortKey] = "6543",
            [DbKey] = "events",
            [UserKey] = "svc_events",
            [PassKey] = "p@ss!word",
        });

        var credentials = BootstrapSecretLoader.LoadPostgresConnectionString(config);

        var parsed = new NpgsqlConnectionStringBuilder(credentials.ConnectionString);
        parsed.Host.Should().Be("db.example.com");
        parsed.Port.Should().Be(6543);
        parsed.Database.Should().Be("events");
        parsed.Username.Should().Be("svc_events");
        parsed.Password.Should().Be("p@ss!word");
        parsed.SslMode.Should().Be(SslMode.Prefer);
        parsed.TrustServerCertificate.Should().BeTrue();

        credentials.Source.Should().Contain("Config");
    }

    [Test]
    public void LoadPostgresConnectionString_WithoutPort_UsesDefault5432()
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

        new NpgsqlConnectionStringBuilder(credentials.ConnectionString).Port.Should().Be(5432);
    }

    [Test]
    public void LoadPostgresConnectionString_WithMalformedPort_FallsBackToDefault()
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

        new NpgsqlConnectionStringBuilder(credentials.ConnectionString).Port.Should().Be(5432);
    }

    #endregion

    #region Environment Variable Resolution

    [Test]
    public void LoadPostgresConnectionString_WithEnvironmentVariables_ComposesCorrectConnectionString()
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
            parsed.Host.Should().Be("env-host");
            parsed.Port.Should().Be(7777);
            parsed.Database.Should().Be("env_db");
            parsed.Username.Should().Be("env_user");
            parsed.Password.Should().Be("env_pass");

            credentials.Source.Should().Contain("Env");
        }
        finally
        {
            ClearEnv();
        }
    }

    [Test]
    public void LoadPostgresConnectionString_EnvWinsOverConfig()
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
            parsed.Host.Should().Be("env-host");
            parsed.Database.Should().Be("env_db");
            parsed.Username.Should().Be("env_user");
            parsed.Password.Should().Be("env_pass");
        }
        finally
        {
            ClearEnv();
        }
    }

    [Test]
    public void LoadPostgresConnectionString_MixedSources_LabelsSourceAsMixed()
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

            credentials.Source.Should().Contain("Mixed");
            credentials.Source.Should().Contain("Env");
            credentials.Source.Should().Contain("Config");
        }
        finally
        {
            ClearEnv();
        }
    }

    #endregion

    #region Missing Field Errors

    [Test]
    public void LoadPostgresConnectionString_MissingHost_ThrowsInvalidOperationException()
    {
        ClearEnv();

        var config = BuildConfig(new Dictionary<string, string?>
        {
            [DbKey] = "db",
            [UserKey] = "u",
            [PassKey] = "p",
        });

        var act = () => BootstrapSecretLoader.LoadPostgresConnectionString(config);

        act.Should().Throw<InvalidOperationException>()
           .WithMessage("*Host*");
    }

    [Test]
    public void LoadPostgresConnectionString_MissingDatabase_ThrowsInvalidOperationException()
    {
        ClearEnv();

        var config = BuildConfig(new Dictionary<string, string?>
        {
            [HostKey] = "localhost",
            [UserKey] = "u",
            [PassKey] = "p",
        });

        var act = () => BootstrapSecretLoader.LoadPostgresConnectionString(config);

        act.Should().Throw<InvalidOperationException>()
           .WithMessage("*Database*");
    }

    [Test]
    public void LoadPostgresConnectionString_MissingUsername_ThrowsInvalidOperationException()
    {
        ClearEnv();

        var config = BuildConfig(new Dictionary<string, string?>
        {
            [HostKey] = "localhost",
            [DbKey] = "db",
            [PassKey] = "p",
        });

        var act = () => BootstrapSecretLoader.LoadPostgresConnectionString(config);

        act.Should().Throw<InvalidOperationException>()
           .WithMessage("*Username*");
    }

    [Test]
    public void LoadPostgresConnectionString_MissingPassword_ThrowsInvalidOperationException()
    {
        ClearEnv();

        var config = BuildConfig(new Dictionary<string, string?>
        {
            [HostKey] = "localhost",
            [DbKey] = "db",
            [UserKey] = "u",
        });

        var act = () => BootstrapSecretLoader.LoadPostgresConnectionString(config);

        act.Should().Throw<InvalidOperationException>()
           .WithMessage("*Password*");
    }

    [Test]
    public void LoadPostgresConnectionString_AllMissing_ErrorListsEveryMissingField()
    {
        ClearEnv();

        var config = BuildConfig(new Dictionary<string, string?>());

        var act = () => BootstrapSecretLoader.LoadPostgresConnectionString(config);

        var ex = act.Should().Throw<InvalidOperationException>().Which;
        ex.Message.Should().Contain("Host");
        ex.Message.Should().Contain("Database");
        ex.Message.Should().Contain("Username");
        ex.Message.Should().Contain("Password");
    }

    #endregion
}
