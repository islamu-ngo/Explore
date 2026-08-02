// ABOUTME: Regression tests for the TickerQ design-time DbContext factory.
// ABOUTME: Ensures it binds structured database settings instead of hardcoded localhost defaults.

using Explore.API.Scheduling;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using TUnit.Core;

namespace ApiIntegrationTests.Scheduling;

[NotInParallel("TickerQDesignTimeFactory")]
public sealed class ApiTickerQDbContextFactoryTests
{
    private static readonly string[] Keys =
    [
        "Database__Provider",
        "Database__Host",
        "Database__Database",
        "Database__Runtime__Username",
        "Database__Runtime__Password",
        "Database__Runtime__Port",
    ];

    [Test]
    public void CreateDbContext_UsesStructuredDatabaseSettings()
    {
        try
        {
            Environment.SetEnvironmentVariable("Database__Provider", "PostgreSql");
            Environment.SetEnvironmentVariable("Database__Host", "tickerq-db.example.test");
            Environment.SetEnvironmentVariable("Database__Database", "tickerq_design_time");
            Environment.SetEnvironmentVariable("Database__Runtime__Username", "tickerq_user");
            Environment.SetEnvironmentVariable("Database__Runtime__Password", "tickerq_secret");
            Environment.SetEnvironmentVariable("Database__Runtime__Port", "5434");

            var factory = new ApiTickerQDbContextFactory();
            using var context = factory.CreateDbContext([]);

            var parsed = new NpgsqlConnectionStringBuilder(context.Database.GetConnectionString());
            parsed.Host.Should().Be("tickerq-db.example.test");
            parsed.Port.Should().Be(5434);
            parsed.Database.Should().Be("tickerq_design_time");
            parsed.Username.Should().Be("tickerq_user");
            parsed.Password.Should().Be("tickerq_secret");
            parsed.Host.Should().NotBe("localhost");
        }
        finally
        {
            foreach (var key in Keys)
            {
                Environment.SetEnvironmentVariable(key, null);
            }
        }
    }

    [Test]
    public void CreateDbContext_RejectsNonPostgreSqlWithoutLeakingCredentials()
    {
        const string password = "tickerq-design-time-gate-password";

        try
        {
            Environment.SetEnvironmentVariable("Database__Provider", "SqlServer");
            Environment.SetEnvironmentVariable("Database__Host", "sql.example.test");
            Environment.SetEnvironmentVariable("Database__Database", "tickerq_design_time");
            Environment.SetEnvironmentVariable("Database__Runtime__Username", "tickerq_user");
            Environment.SetEnvironmentVariable("Database__Runtime__Password", password);

            Action act = () => new ApiTickerQDbContextFactory().CreateDbContext([]);

            var exception = act.Should().Throw<InvalidOperationException>().Which;
            exception.Message.Should().Contain("Database:Provider=PostgreSql")
                .And.Contain("EmailDispatchProcessor:Mode=HostedService")
                .And.NotContain(password);
        }
        finally
        {
            foreach (var key in Keys)
            {
                Environment.SetEnvironmentVariable(key, null);
            }
        }
    }
}
