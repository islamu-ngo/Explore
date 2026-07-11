// ABOUTME: PostgreSQL-backed smoke tests for TickerQ scheduler operational persistence.
// ABOUTME: Proves scheduler migrations use a separate schema and remain outside EmailDispatch business state.

using Explore.API.Configuration;
using Explore.API.Extensions;
using Explore.API.Scheduling;
using FluentAssertions;
using Microsoft.AspNetCore.Hosting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Hosting;
using Testcontainers.PostgreSql;
using TUnit.Core;

namespace ApiIntegrationTests.Features;

public sealed class TickerQSchedulerOperationalStoreTests
{
    [Test]
    public async Task AddApiTickerQSchedulerUsesSeparatePostgreSqlSchemaForOperationalStore()
    {
        await using var container = new PostgreSqlBuilder("postgres:18-alpine")
            .WithDatabase("tickerq_scheduler_test")
            .WithUsername("postgres")
            .WithPassword("postgres")
            .Build();
        await container.StartAsync();

        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:DefaultConnection"] = container.GetConnectionString(),
                [$"{TickerQSchedulerOptions.SectionName}:Enabled"] = "true",
                [$"{TickerQSchedulerOptions.SectionName}:Schema"] = "ticker",
                [$"{TickerQSchedulerOptions.SectionName}:DashboardEnabled"] = "false",
                [$"{TickerQSchedulerOptions.SectionName}:MaxConcurrency"] = "1",
                [$"{TickerQSchedulerOptions.SectionName}:NodeIdentifier"] = "tickerq-test-node"
            })
            .Build();
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddApiTickerQScheduler(
            configuration,
            new TestWebHostEnvironment(),
            enabled: true);

        await using var provider = services.BuildServiceProvider();
        await using var scope = provider.CreateAsyncScope();
        var dbFactory = scope.ServiceProvider.GetRequiredService<IDbContextFactory<ApiTickerQDbContext>>();
        await using var db = await dbFactory.CreateDbContextAsync();

        await db.Database.ExecuteSqlRawAsync("""
            create table public."__EFMigrationsHistory" (
                migration_id varchar(150) not null primary key,
                product_version varchar(32) not null
            );
            """);
        await db.Database.MigrateAsync();

        var connection = db.Database.GetDbConnection();
        await connection.OpenAsync();
        await using var command = connection.CreateCommand();
        command.CommandText = """
            select exists (
                select 1
                from information_schema.schemata
                where schema_name = 'ticker'
            );
            """;
        var schemaExists = (bool)(await command.ExecuteScalarAsync() ?? false);

        schemaExists.Should().BeTrue("TickerQ operational tables must live in their own scheduler schema");

        command.CommandText = """
            select exists (
                select 1
                from information_schema.tables
                where table_schema = 'ticker'
                  and table_name = '__EFMigrationsHistory'
            );
            """;
        var schedulerHistoryExists = (bool)(await command.ExecuteScalarAsync() ?? false);

        schedulerHistoryExists.Should().BeTrue(
            "TickerQ must not reuse the primary application's snake_case EF migrations history table");
    }

    private sealed class TestWebHostEnvironment : IWebHostEnvironment
    {
        public string EnvironmentName { get; set; } = Environments.Development;
        public string ApplicationName { get; set; } = "Explore.API";
        public string WebRootPath { get; set; } = Directory.GetCurrentDirectory();
        public IFileProvider WebRootFileProvider { get; set; } = new NullFileProvider();
        public string ContentRootPath { get; set; } = Directory.GetCurrentDirectory();
        public IFileProvider ContentRootFileProvider { get; set; } = new NullFileProvider();
    }
}
