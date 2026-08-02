// ABOUTME: Design-time factory for generating TickerQ operational-store EF migrations.
// ABOUTME: Resolves the same structured runtime database contract instead of hardcoded localhost settings.

using Explore.Secrets.Database;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Configuration;

namespace Explore.API.Scheduling;

public sealed class ApiTickerQDbContextFactory : IDesignTimeDbContextFactory<ApiTickerQDbContext>
{
    public ApiTickerQDbContext CreateDbContext(string[] args)
    {
        var configuration = new ConfigurationBuilder()
            .AddUserSecrets<ApiTickerQDbContextFactory>(optional: true)
            .AddEnvironmentVariables()
            .Build();

        var database = PrimaryDatabaseConfiguration.BindRuntime(configuration);
        if (database.Provider != PrimaryDatabaseProvider.PostgreSql)
        {
            throw new InvalidOperationException(
                $"TickerQ requires Database:Provider=PostgreSql, but '{database.Provider}' is selected. " +
                "Set EmailDispatchProcessor:Mode=HostedService for portable email dispatch.");
        }

        var connectionString = PrimaryDatabaseConfiguration.BuildConnectionString(database).ConnectionString;

        var options = new DbContextOptionsBuilder<ApiTickerQDbContext>()
            .UseNpgsql(
                connectionString,
                npgsqlOptions =>
                {
                    npgsqlOptions.MigrationsHistoryTable(
                            ApiTickerQDbContext.MigrationsHistoryTable,
                            ApiTickerQDbContext.Schema);
                })
            .ConfigureWarnings(warnings =>
                warnings.Ignore(RelationalEventId.PendingModelChangesWarning))
            .Options;

        return new ApiTickerQDbContext(options);
    }
}
