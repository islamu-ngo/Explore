// ABOUTME: Migration service composition root for database bootstrap, DbContexts, and hosted migration work.
// ABOUTME: Configures runtime EF Core behavior separately from design-time migration generation.

using Aspire.Npgsql.EntityFrameworkCore.PostgreSQL;
using Event.MigrationService.Extensions;
using Explore.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Npgsql.EntityFrameworkCore.PostgreSQL;

namespace Event.MigrationService;

public class Program
{
    public static void Main(string[] args)
    {
        var builder = Host.CreateApplicationBuilder(args);

        builder.Configuration.AddDiscretePostgresBootstrap();

        builder.AddServiceDefaults();
        builder.Services.AddHostedService<Worker>();

        //builder.Services.AddOpenTelemetry()
        //  .WithTracing(tracing => tracing.AddSource(Worker.ActivitySourceName));

        builder.AddNpgsqlDbContext<ExploreDbContext>(
            "EventMigrationService", configureDbContextOptions: options =>
            {
                options.UseSnakeCaseNamingConvention();

                if (builder.Environment.IsDevelopment())
                {
                    options.ConfigureWarnings(warnings =>
                        warnings.Ignore(RelationalEventId.PendingModelChangesWarning));
                }
            });

        builder.AddNpgsqlDbContext<DataProtectionKeyContext>(
            "EventMigrationService", configureDbContextOptions: options =>
                options.UseSnakeCaseNamingConvention());

        var host = builder.Build();
        host.Run();
    }
}
