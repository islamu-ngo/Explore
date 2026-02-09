using Aspire.Npgsql.EntityFrameworkCore.PostgreSQL;
using Event.MigrationService.Extensions;
using Explore.Persistence;
using Microsoft.EntityFrameworkCore;
using Npgsql.EntityFrameworkCore.PostgreSQL;

namespace Event.MigrationService;

public class Program
{
    public static void Main(string[] args)
    {
        var builder = Host.CreateApplicationBuilder(args);

        // Load secrets from Infisical and map to connection string
        builder.Configuration.AddInfisicalMigrationCompatibility();

        builder.AddServiceDefaults();
        builder.Services.AddHostedService<Worker>();

        //builder.Services.AddOpenTelemetry()
        //  .WithTracing(tracing => tracing.AddSource(Worker.ActivitySourceName));

        builder.AddNpgsqlDbContext<ExploreDbContext>(
            "EventMigrationService", configureDbContextOptions: options =>
                options.UseSnakeCaseNamingConvention());

        var host = builder.Build();
        host.Run();
    }
}
