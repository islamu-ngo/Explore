// ABOUTME: Registers TickerQ as the API host scheduler and operations surface.
// ABOUTME: Uses PostgreSQL-backed scheduler state while keeping business outbox truth in Explore.Persistence.

using Explore.API.Configuration;
using Explore.Secrets.Bootstrap;
using Microsoft.EntityFrameworkCore;
using TickerQ.Dashboard.DependencyInjection;
using TickerQ.DependencyInjection;
using TickerQ.EntityFrameworkCore.DbContextFactory;
using TickerQ.EntityFrameworkCore.DependencyInjection;
using TickerQ.Instrumentation.OpenTelemetry;
using TickerQ.Utilities.Entities;

namespace Explore.API.Extensions;

public static class TickerQSchedulerExtensions
{
    public static IServiceCollection AddApiTickerQScheduler(
        this IServiceCollection services,
        IConfiguration configuration,
        IWebHostEnvironment environment,
        bool enabled)
    {
        var options = configuration.GetSection(TickerQSchedulerOptions.SectionName).Get<TickerQSchedulerOptions>()
            ?? new TickerQSchedulerOptions();

        services.Configure<TickerQSchedulerOptions>(configuration.GetSection(TickerQSchedulerOptions.SectionName));

        if (!enabled || !options.Enabled || environment.IsEnvironment("Testing"))
        {
            return services;
        }

        var connectionString = ResolvePostgresConnectionString(configuration);

        var tickerBuilder = services.AddTickerQ<TimeTickerEntity, CronTickerEntity>(tickerOptions =>
        {
            tickerOptions.ConfigureScheduler(scheduler =>
            {
                scheduler.MaxConcurrency = Math.Max(1, options.MaxConcurrency);
                scheduler.NodeIdentifier = string.IsNullOrWhiteSpace(options.NodeIdentifier)
                    ? Environment.MachineName
                    : options.NodeIdentifier;
            });
        });

        tickerBuilder.AddOperationalStore(efOptions =>
        {
            efOptions.UseTickerQDbContext<TickerQDbContext>(dbOptions =>
            {
                dbOptions.UseNpgsql(connectionString);
            });
            efOptions.SetSchema(string.IsNullOrWhiteSpace(options.Schema) ? "ticker" : options.Schema);
        });

        if (options.DashboardEnabled)
        {
            tickerBuilder.AddDashboard(dashboard =>
            {
                dashboard.SetBasePath(string.IsNullOrWhiteSpace(options.DashboardPath)
                    ? "/admin/scheduler"
                    : options.DashboardPath);
                dashboard.WithHostAuthentication(string.IsNullOrWhiteSpace(options.DashboardAuthorizationPolicy)
                    ? TickerQSchedulerOptions.InstanceAdminPolicyName
                    : options.DashboardAuthorizationPolicy);
                dashboard.WithSessionTimeout(Math.Max(1, options.DashboardSessionTimeoutMinutes));
            });
        }

        tickerBuilder.AddOpenTelemetryInstrumentation();

        return services;
    }

    public static async Task MigrateTickerQSchedulerAsync(this WebApplication app)
    {
        if (!IsTickerQSchedulerEnabled(app.Configuration, app.Environment))
        {
            return;
        }

        await using var scope = app.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<TickerQDbContext>();
        if (db.Database.IsRelational())
        {
            await db.Database.MigrateAsync();
        }
    }

    public static bool IsTickerQSchedulerEnabled(IConfiguration configuration, IWebHostEnvironment environment)
    {
        if (environment.IsEnvironment("Testing"))
        {
            return false;
        }

        var schedulerOptions = configuration.GetSection(TickerQSchedulerOptions.SectionName).Get<TickerQSchedulerOptions>()
            ?? new TickerQSchedulerOptions();
        return schedulerOptions.Enabled;
    }

    private static string ResolvePostgresConnectionString(IConfiguration configuration)
    {
        var connectionString = configuration["ConnectionStrings:DefaultConnection"];
        return string.IsNullOrWhiteSpace(connectionString)
            ? BootstrapSecretLoader.LoadPostgresConnectionString(configuration).ConnectionString
            : connectionString;
    }
}
