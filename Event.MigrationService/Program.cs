using Event.MigrationService.Extensions;
using Explore.Persistence;
using Npgsql.EntityFrameworkCore.PostgreSQL;
using Aspire.Npgsql.EntityFrameworkCore.PostgreSQL;
using Microsoft.EntityFrameworkCore;

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
