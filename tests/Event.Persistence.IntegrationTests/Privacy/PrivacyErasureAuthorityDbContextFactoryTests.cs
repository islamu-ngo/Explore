// ABOUTME: Verifies authority EF tooling ignores ambient durable database targets.
// ABOUTME: Locks the design-time factory to its fixed inert scaffolding connection.

using System.Data;
using Explore.Persistence.Privacy.ErasureAuthority;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace Event.Persistence.IntegrationTests.Privacy;

[NotInParallel]
public sealed class PrivacyErasureAuthorityDbContextFactoryTests
{
    [Test]
    public async Task CreateDbContext_IgnoresAmbientAuthorityConnectionString()
    {
        const string key = "ConnectionStrings__PrivacyErasureAuthority";
        string? previousValue = Environment.GetEnvironmentVariable(key);

        try
        {
            Environment.SetEnvironmentVariable(
                key,
                "Host=127.0.0.1;Port=2;Database=hostile_ambient;Username=canary;Password=canary;Timeout=1");

            await using PrivacyErasureAuthorityDbContext context =
                new PrivacyErasureAuthorityDbContextFactory().CreateDbContext([]);
            var target = new NpgsqlConnectionStringBuilder(context.Database.GetConnectionString());

            await Assert.That(target.Host).IsEqualTo("127.0.0.1");
            await Assert.That(target.Port).IsEqualTo(1);
            await Assert.That(target.Database).IsEqualTo("privacy_erasure_authority_design_time");
            await Assert.That(context.Database.GetDbConnection().State).IsEqualTo(ConnectionState.Closed);
        }
        finally
        {
            Environment.SetEnvironmentVariable(key, previousValue);
        }
    }

    [Test]
    public async Task CreateDbContext_WithExplicitConnection_UsesValidatedClosedTarget()
    {
        const string connection =
            "Host=127.0.0.1;Port=3;Database=explicit_authority_canary;Username=operator;Password=secret";

        await using PrivacyErasureAuthorityDbContext context =
            new PrivacyErasureAuthorityDbContextFactory().CreateDbContext(["--connection", connection]);
        var target = new NpgsqlConnectionStringBuilder(context.Database.GetConnectionString());

        await Assert.That(target.Database).IsEqualTo("explicit_authority_canary");
        await Assert.That(context.Database.GetDbConnection().State).IsEqualTo(ConnectionState.Closed);
    }

    [Test]
    public async Task CreateDbContext_RejectsMissingOrBlankExplicitConnection()
    {
        string[][] invalidArguments =
        [
            ["--connection"],
            ["--connection", ""],
            ["--connection", "   "],
            ["--connection", "--verbose"],
        ];

        foreach (string[] args in invalidArguments)
        {
            ArgumentException exception = Assert.Throws<ArgumentException>(() =>
                new PrivacyErasureAuthorityDbContextFactory().CreateDbContext(args));

            await Assert.That(exception.Message).Contains("--connection");
        }
    }

    [Test]
    public async Task CreateDbContext_RejectsDuplicateExplicitConnection()
    {
        ArgumentException exception = Assert.Throws<ArgumentException>(() =>
            new PrivacyErasureAuthorityDbContextFactory().CreateDbContext(
            [
                "--connection",
                "Host=127.0.0.1;Database=first;Username=operator",
                "--connection",
                "Host=127.0.0.1;Database=second;Username=operator",
            ]));

        await Assert.That(exception.Message).Contains("--connection");
        await Assert.That(exception.Message).DoesNotContain("first");
        await Assert.That(exception.Message).DoesNotContain("second");
    }

    [Test]
    public async Task CreateDbContext_RejectsMalformedExplicitConnectionWithoutEchoingIt()
    {
        const string secretCanary = "do-not-echo-this-secret";

        ArgumentException exception = Assert.Throws<ArgumentException>(() =>
            new PrivacyErasureAuthorityDbContextFactory().CreateDbContext(
                ["--connection", $"Password={secretCanary};UnsupportedKeyword=value"]));

        await Assert.That(exception.Message).Contains("--connection");
        await Assert.That(exception.Message).DoesNotContain(secretCanary);
        await Assert.That(exception.Message).DoesNotContain("UnsupportedKeyword");
    }

    [Test]
    public async Task CreateDbContext_RejectsExplicitConnectionMissingRequiredIdentity()
    {
        string[] invalidConnections =
        [
            "Database=authority;Username=operator",
            "Host=127.0.0.1;Username=operator",
            "Host=127.0.0.1;Database=authority",
        ];

        foreach (string connection in invalidConnections)
        {
            ArgumentException exception = Assert.Throws<ArgumentException>(() =>
                new PrivacyErasureAuthorityDbContextFactory().CreateDbContext(
                    ["--connection", connection]));

            await Assert.That(exception.Message).Contains("--connection");
            await Assert.That(exception.Message).DoesNotContain("authority");
            await Assert.That(exception.Message).DoesNotContain("operator");
        }
    }

    [Test]
    public async Task CreateDbContext_IgnoresUnrelatedArgumentsAndAmbientAuthorityConnectionString()
    {
        const string key = "ConnectionStrings__PrivacyErasureAuthority";
        string? previousValue = Environment.GetEnvironmentVariable(key);

        try
        {
            Environment.SetEnvironmentVariable(
                key,
                "Host=127.0.0.1;Port=2;Database=hostile_ambient;Username=canary;Password=canary");

            await using PrivacyErasureAuthorityDbContext context =
                new PrivacyErasureAuthorityDbContextFactory().CreateDbContext(
                    ["--environment", "Production", "--verbose"]);
            var target = new NpgsqlConnectionStringBuilder(context.Database.GetConnectionString());

            await Assert.That(target.Port).IsEqualTo(1);
            await Assert.That(target.Database).IsEqualTo("privacy_erasure_authority_design_time");
            await Assert.That(context.Database.GetDbConnection().State).IsEqualTo(ConnectionState.Closed);
        }
        finally
        {
            Environment.SetEnvironmentVariable(key, previousValue);
        }
    }
}
