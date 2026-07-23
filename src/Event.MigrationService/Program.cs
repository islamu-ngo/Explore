// ABOUTME: Migration service composition root for database bootstrap, DbContexts, and hosted migration work.
// ABOUTME: Configures runtime EF Core behavior separately from design-time migration generation.

using Aspire.Npgsql.EntityFrameworkCore.PostgreSQL;
using Event.MigrationService.Extensions;
using Explore.Application.Configuration;
using Explore.Persistence;
using Explore.Persistence.Privacy.ErasureAuthority;
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

        PrivacyErasureAuthorityTopology erasureTopology =
            PrivacyErasureDurabilityOptions.GetTopology(builder.Configuration);
        builder.Services.AddOptions<PrivacyErasureDurabilityOptions>()
            .Configure(options => options.Topology = erasureTopology);
        if (erasureTopology == PrivacyErasureAuthorityTopology.ExternalDatabase)
        {
            string connectionString = PrivacyErasureDurabilityOptions
                .GetExternalDatabaseMigratorConnectionString(builder.Configuration);
            builder.Services.AddDbContext<PrivacyErasureAuthorityDbContext>(options =>
                options.UseNpgsql(connectionString, npgsql =>
                        npgsql.MigrationsAssembly(typeof(PrivacyErasureAuthorityDbContext).Assembly.FullName))
                    .UseSnakeCaseNamingConvention());
        }

        var host = builder.Build();
        host.Run();
    }
}
