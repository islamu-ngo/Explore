// ABOUTME: Verifies structured privacy-erasure authority binding, precedence, and redaction.
// ABOUTME: Prevents raw connection strings and non-PostgreSQL providers from entering the authority boundary.

using Explore.Secrets.Database;
using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;
using Npgsql;

namespace Explore.Secrets.UnitTests.Database;

public sealed class PrivacyErasureAuthorityDatabaseConfigurationTests
{
    [Test]
    public void StructuredRolesBuildDistinctNativePostgreSqlConnections()
    {
        IConfiguration configuration = BuildConfiguration(new Dictionary<string, string?>
        {
            ["PrivacyErasureAuthorityDatabase:Provider"] = "PostgreSql",
            ["PrivacyErasureAuthorityDatabase:Host"] = "authority.example.test",
            ["PrivacyErasureAuthorityDatabase:Port"] = "6543",
            ["PrivacyErasureAuthorityDatabase:Database"] = "privacy_authority",
            ["PrivacyErasureAuthorityDatabase:TlsMode"] = "Required",
            ["PrivacyErasureAuthorityDatabase:TrustServerCertificate"] = "false",
            ["PrivacyErasureAuthorityDatabase:Runtime:Username"] = "runtime_role",
            ["PrivacyErasureAuthorityDatabase:Runtime:Password"] = "runtime-secret",
            ["PrivacyErasureAuthorityDatabase:Migrator:Username"] = "migrator_role",
            ["PrivacyErasureAuthorityDatabase:Migrator:Password"] = "migrator-secret",
        });

        var runtime = PrivacyErasureAuthorityDatabaseConfiguration.ResolveRuntimeConnectionString(configuration);
        var migrator = PrivacyErasureAuthorityDatabaseConfiguration.ResolveMigratorConnectionString(configuration);
        var runtimeTarget = new NpgsqlConnectionStringBuilder(runtime.ConnectionString);
        var migratorTarget = new NpgsqlConnectionStringBuilder(migrator.ConnectionString);

        runtimeTarget.Username.Should().Be("runtime_role");
        migratorTarget.Username.Should().Be("migrator_role");
        runtimeTarget.Host.Should().Be("authority.example.test");
        runtimeTarget.Port.Should().Be(6543);
        runtime.RedactedConnectionString.Should().NotContain("runtime-secret");
        runtime.SafeSummary.Should().NotContain("runtime-secret");
    }

    [Test]
    public void ExplicitStructuredValuesOutrankDiscreteSecrets()
    {
        IConfiguration configuration = BuildConfiguration(new Dictionary<string, string?>
        {
            ["PRIVACY_ERASURE_AUTHORITY_HOST"] = "secret-host",
            ["PRIVACY_ERASURE_AUTHORITY_DATABASE"] = "secret_database",
            ["PRIVACY_ERASURE_AUTHORITY_RUNTIME_USERNAME"] = "secret_user",
            ["PRIVACY_ERASURE_AUTHORITY_RUNTIME_PASSWORD"] = "secret-password",
            ["PrivacyErasureAuthorityDatabase:Provider"] = "PostgreSql",
            ["PrivacyErasureAuthorityDatabase:Host"] = "explicit-host",
            ["PrivacyErasureAuthorityDatabase:Database"] = "explicit_database",
            ["PrivacyErasureAuthorityDatabase:Runtime:Username"] = "explicit_user",
            ["PrivacyErasureAuthorityDatabase:Runtime:Password"] = "explicit-password",
        });

        var options = PrivacyErasureAuthorityDatabaseConfiguration.BindRuntime(configuration);

        options.Host.Should().Be("explicit-host");
        options.Database.Should().Be("explicit_database");
        options.Username.Should().Be("explicit_user");
        options.Password.Should().Be("explicit-password");
    }

    [Test]
    public void DiscreteSecretsProjectIntoCanonicalSection()
    {
        var builder = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["PRIVACY_ERASURE_AUTHORITY_HOST"] = "authority",
            ["PRIVACY_ERASURE_AUTHORITY_PORT"] = "5433",
            ["PRIVACY_ERASURE_AUTHORITY_DATABASE"] = "privacy",
            ["PRIVACY_ERASURE_AUTHORITY_RUNTIME_USERNAME"] = "runtime",
            ["PRIVACY_ERASURE_AUTHORITY_RUNTIME_PASSWORD"] = "runtime-secret",
            ["PRIVACY_ERASURE_AUTHORITY_MIGRATOR_USERNAME"] = "migrator",
            ["PRIVACY_ERASURE_AUTHORITY_MIGRATOR_PASSWORD"] = "migrator-secret",
            ["PRIVACY_ERASURE_AUTHORITY_TLS_MODE"] = "Required",
            ["PRIVACY_ERASURE_AUTHORITY_TRUST_SERVER_CERTIFICATE"] = "false",
        });

        PrivacyErasureAuthorityDatabaseConfiguration.ProjectDiscreteConfiguration(builder);
        IConfiguration configuration = builder.Build();

        configuration["PrivacyErasureAuthorityDatabase:Provider"].Should().Be("PostgreSql");
        configuration["PrivacyErasureAuthorityDatabase:Host"].Should().Be("authority");
        configuration["PrivacyErasureAuthorityDatabase:Runtime:Username"].Should().Be("runtime");
        configuration["PrivacyErasureAuthorityDatabase:Migrator:Username"].Should().Be("migrator");
    }

    [Test]
    public void NonPostgreSqlProviderFailsClosed()
    {
        IConfiguration configuration = BuildConfiguration(new Dictionary<string, string?>
        {
            ["PrivacyErasureAuthorityDatabase:Provider"] = "Sqlite",
            ["PrivacyErasureAuthorityDatabase:Database"] = "authority.db",
        });

        Action act = () => PrivacyErasureAuthorityDatabaseConfiguration.BindRuntime(configuration);

        act.Should().Throw<OptionsValidationException>()
            .WithMessage("*Provider must be PostgreSql*");
    }

    private static IConfiguration BuildConfiguration(Dictionary<string, string?> values) =>
        new ConfigurationBuilder().AddInMemoryCollection(values).Build();
}
