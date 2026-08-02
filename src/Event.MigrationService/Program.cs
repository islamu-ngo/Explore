// ABOUTME: Migration service composition root for database bootstrap, DbContexts, and hosted migration work.
// ABOUTME: Configures runtime EF Core behavior separately from design-time migration generation.

using Event.MigrationService.Extensions;
using Explore.Application.Configuration;
using Explore.Persistence;
using Explore.Persistence.Database;
using Explore.Persistence.Privacy.ErasureAuthority;
using Explore.Secrets.Database;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Npgsql.EntityFrameworkCore.PostgreSQL;

namespace Event.MigrationService;

public class Program
{
    public static void Main(string[] args)
    {
        var builder = Host.CreateApplicationBuilder(args);

        var databaseOptions = builder.Configuration.AddPrimaryDatabaseBootstrap();

        builder.AddServiceDefaults();
        builder.Services.AddHostedService<Worker>();

        //builder.Services.AddOpenTelemetry()
        //  .WithTracing(tracing => tracing.AddSource(Worker.ActivitySourceName));

        builder.Services.AddDbContext<ExploreDbContext>(options =>
            {
                PrimaryDatabaseProviderComposition.ConfigureApplication(options, databaseOptions);

                if (builder.Environment.IsDevelopment())
                {
                    options.ConfigureWarnings(warnings =>
                        warnings.Ignore(RelationalEventId.PendingModelChangesWarning));
                }
            });

        builder.Services.AddDbContext<DataProtectionKeyContext>(options =>
            PrimaryDatabaseProviderComposition.ConfigureDataProtection(options, databaseOptions));

        PrivacyErasureAuthorityTopology erasureTopology =
            PrivacyErasureDurabilityOptions.GetTopology(builder.Configuration);
        builder.Services.AddOptions<PrivacyErasureDurabilityOptions>()
            .Configure(options => options.Topology = erasureTopology);
        if (erasureTopology == PrivacyErasureAuthorityTopology.ExternalDatabase)
        {
            var authorityDatabase = PrivacyErasureAuthorityDatabaseConfiguration
                .ResolveMigratorConnectionString(builder.Configuration);
            builder.Services.AddDbContext<PrivacyErasureAuthorityDbContext>(options =>
                options.UseNpgsql(authorityDatabase.ConnectionString, npgsql => npgsql
                        .MigrationsAssembly(typeof(PrivacyErasureAuthorityDbContext).Assembly.FullName)
                        .MigrationsHistoryTable(
                            PrivacyErasureAuthorityDatabaseConfiguration.MigrationsHistoryTable))
                    .UseSnakeCaseNamingConvention());
        }
        else
        {
            EmbeddedPrivacyErasureAuthorityOptions embedded =
                EmbeddedPrivacyErasureAuthorityOptions.Bind(builder.Configuration);
            builder.Services.AddSingleton(embedded);
            builder.Services.AddSingleton<EmbeddedPrivacyErasureAuthorityStorage>();
            builder.Services.AddDbContext<EmbeddedPrivacyErasureAuthorityDbContext>(options =>
                EmbeddedPrivacyErasureAuthorityDbContextFactory.Configure(options, embedded));
        }

        var host = builder.Build();
        host.Run();
    }
}
