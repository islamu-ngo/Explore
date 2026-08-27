// ABOUTME: Verifies structured privacy-erasure authority binding, precedence, and redaction.
// ABOUTME: Prevents raw connection strings and non-PostgreSQL providers from entering the authority boundary.

using Explore.Secrets.Database;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;
using Npgsql;

namespace Explore.Secrets.UnitTests.Database;

public sealed class PrivacyErasureAuthorityDatabaseConfigurationTests
{
    [Test]
    public async Task StructuredRolesBuildDistinctNativePostgreSqlConnections()
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

        await Assert.That(runtimeTarget.Username).IsEqualTo("runtime_role");
        await Assert.That(migratorTarget.Username).IsEqualTo("migrator_role");
        await Assert.That(runtimeTarget.Host).IsEqualTo("authority.example.test");
        await Assert.That(runtimeTarget.Port).IsEqualTo(6543);
        await Assert.That(runtime.RedactedConnectionString).DoesNotContain("runtime-secret");
        await Assert.That(runtime.SafeSummary).DoesNotContain("runtime-secret");
    }

    [Test]
    public async Task ExplicitStructuredValuesOutrankDiscreteSecrets()
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

        await Assert.That(options.Host).IsEqualTo("explicit-host");
        await Assert.That(options.Database).IsEqualTo("explicit_database");
        await Assert.That(options.Username).IsEqualTo("explicit_user");
        await Assert.That(options.Password).IsEqualTo("explicit-password");
    }

    [Test]
    public async Task DiscreteSecretsProjectIntoCanonicalSection()
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

        await Assert.That(configuration["Database:Erasure:Provider"]).IsEqualTo("PostgreSql");
        await Assert.That(configuration["Database:Erasure:Host"]).IsEqualTo("authority");
        await Assert.That(configuration["Database:Erasure:Runtime:Username"]).IsEqualTo("runtime");
        await Assert.That(configuration["Database:Erasure:Migrator:Username"]).IsEqualTo("migrator");
        await Assert.That(configuration["PrivacyErasureAuthorityDatabase:Provider"]).IsEqualTo("PostgreSql");
        await Assert.That(configuration["PrivacyErasureAuthorityDatabase:Host"]).IsEqualTo("authority");
        await Assert.That(configuration["PrivacyErasureAuthorityDatabase:Runtime:Username"]).IsEqualTo("runtime");
        await Assert.That(configuration["PrivacyErasureAuthorityDatabase:Migrator:Username"]).IsEqualTo("migrator");
    }

    [Test]
    public async Task DatabaseErasureCanonicalSectionBindsDirectly()
    {
        IConfiguration configuration = BuildConfiguration(new Dictionary<string, string?>
        {
            ["Database:Erasure:Provider"] = "PostgreSql",
            ["Database:Erasure:Host"] = "erasure-db",
            ["Database:Erasure:Port"] = "5432",
            ["Database:Erasure:Database"] = "erasure_ledger",
            ["Database:Erasure:Runtime:Username"] = "erasure_user",
            ["Database:Erasure:Runtime:Password"] = "erasure_pass",
            ["Database:Erasure:Migrator:Username"] = "erasure_admin",
            ["Database:Erasure:Migrator:Password"] = "erasure_admin_pass",
        });

        var runtime = PrivacyErasureAuthorityDatabaseConfiguration.BindRuntime(configuration);
        var migrator = PrivacyErasureAuthorityDatabaseConfiguration.BindMigrator(configuration);

        await Assert.That(runtime.Host).IsEqualTo("erasure-db");
        await Assert.That(runtime.Database).IsEqualTo("erasure_ledger");
        await Assert.That(runtime.Username).IsEqualTo("erasure_user");
        await Assert.That(runtime.Password).IsEqualTo("erasure_pass");
        await Assert.That(migrator.Username).IsEqualTo("erasure_admin");
        await Assert.That(migrator.Password).IsEqualTo("erasure_admin_pass");
    }

    [Test]
    public async Task NonPostgreSqlProviderFailsClosed()
    {
        IConfiguration configuration = BuildConfiguration(new Dictionary<string, string?>
        {
            ["PrivacyErasureAuthorityDatabase:Provider"] = "Sqlite",
            ["PrivacyErasureAuthorityDatabase:Database"] = "authority.db",
        });

        Action act = () => PrivacyErasureAuthorityDatabaseConfiguration.BindRuntime(configuration);

        await Assert.That(act).Throws<OptionsValidationException>()
            .WithMessageContaining("Provider must be PostgreSql");
    }

    [Test]
    public async Task SharedRuntimeAndMigratorUsernameFailsClosed()
    {
        IConfiguration configuration = BuildConfiguration(new Dictionary<string, string?>
        {
            ["PrivacyErasureAuthorityDatabase:Provider"] = "PostgreSql",
            ["PrivacyErasureAuthorityDatabase:Host"] = "authority.example.test",
            ["PrivacyErasureAuthorityDatabase:Database"] = "privacy_authority",
            ["PrivacyErasureAuthorityDatabase:Runtime:Username"] = "shared_role",
            ["PrivacyErasureAuthorityDatabase:Runtime:Password"] = "runtime-secret",
            ["PrivacyErasureAuthorityDatabase:Migrator:Username"] = "SHARED_ROLE",
            ["PrivacyErasureAuthorityDatabase:Migrator:Password"] = "migrator-secret",
        });

        Action act = () => PrivacyErasureAuthorityDatabaseConfiguration.BindRuntime(configuration);

        await Assert.That(act).Throws<OptionsValidationException>()
            .WithMessageContaining("Runtime and Migrator usernames must be distinct");
    }

    private static IConfiguration BuildConfiguration(Dictionary<string, string?> values) =>
        new ConfigurationBuilder().AddInMemoryCollection(values).Build();
}
