// ABOUTME: Regression tests for the TickerQ design-time DbContext factory.
// ABOUTME: Ensures it binds structured database settings instead of hardcoded localhost defaults.

using Explore.API.Scheduling;
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
    public async Task CreateDbContext_UsesStructuredDatabaseSettings()
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
            await Assert.That(parsed.Host).IsEqualTo("tickerq-db.example.test");
            await Assert.That(parsed.Port).IsEqualTo(5434);
            await Assert.That(parsed.Database).IsEqualTo("tickerq_design_time");
            await Assert.That(parsed.Username).IsEqualTo("tickerq_user");
            await Assert.That(parsed.Password).IsEqualTo("tickerq_secret");
            await Assert.That(parsed.Host).IsNotEqualTo("localhost");
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
    public async Task CreateDbContext_RejectsNonPostgreSqlWithoutLeakingCredentials()
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

            var exception = Assert.Throws<InvalidOperationException>(act);
            await Assert.That(exception.Message).Contains("Database:Provider=PostgreSql");
            await Assert.That(exception.Message).Contains("EmailDispatchProcessor:Mode=HostedService");
            await Assert.That(exception.Message).DoesNotContain(password);
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
