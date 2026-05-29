// ABOUTME: Registers TickerQ as the API host scheduler and operations surface.
// ABOUTME: Uses PostgreSQL-backed scheduler state while keeping business outbox truth in Explore.Persistence.

using Explore.API.Configuration;
using Explore.API.Scheduling;
using Explore.Application.Contracts.Infrastructure;
using Explore.Secrets.Bootstrap;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;
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

        services.AddOptions<TickerQSchedulerOptions>()
            .Bind(configuration.GetSection(TickerQSchedulerOptions.SectionName))
            .ValidateOnStart();
        services.AddSingleton<IValidateOptions<TickerQSchedulerOptions>, TickerQSchedulerOptionsValidator>();

        if (!enabled || !options.Enabled || environment.IsEnvironment("Testing"))
        {
            return services;
        }

        var connectionString = ResolvePostgresConnectionString(configuration);

        services.RemoveAll<IScheduledEmailDispatchTrigger>();
        services.AddScoped<IScheduledEmailDispatchTrigger, TickerQScheduledEmailDispatchTrigger>();

        services.AddTickerQ<TimeTickerEntity, CronTickerEntity>(tickerOptions =>
        {
            tickerOptions.ConfigureScheduler(scheduler =>
            {
                scheduler.MaxConcurrency = Math.Max(1, options.MaxConcurrency);
                scheduler.NodeIdentifier = string.IsNullOrWhiteSpace(options.NodeIdentifier)
                    ? Environment.MachineName
                    : options.NodeIdentifier;
            });

            tickerOptions.AddOperationalStore(efOptions =>
            {
                efOptions.SetSchema(ApiTickerQDbContext.Schema);
                efOptions.UseTickerQDbContext<ApiTickerQDbContext>(dbOptions =>
                {
                    dbOptions.UseNpgsql(connectionString);
                });
            });

            if (options.DashboardEnabled)
            {
                tickerOptions.AddDashboard(dashboard =>
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

            tickerOptions.AddOpenTelemetryInstrumentation();
        });

        return services;
    }

    public static IApplicationBuilder UseApiTickerQScheduler(this WebApplication app)
    {
        var schedulerOptions = app.Configuration.GetSection(TickerQSchedulerOptions.SectionName).Get<TickerQSchedulerOptions>()
            ?? new TickerQSchedulerOptions();

        if (schedulerOptions.DashboardEnabled)
        {
            var dashboardPath = string.IsNullOrWhiteSpace(schedulerOptions.DashboardPath)
                ? "/admin/scheduler"
                : schedulerOptions.DashboardPath;
            var dashboardPolicy = string.IsNullOrWhiteSpace(schedulerOptions.DashboardAuthorizationPolicy)
                ? TickerQSchedulerOptions.InstanceAdminPolicyName
                : schedulerOptions.DashboardAuthorizationPolicy;

            app.Use(async (context, next) =>
            {
                if (!context.Request.Path.StartsWithSegments(dashboardPath))
                {
                    await next();
                    return;
                }

                var authorizationService = context.RequestServices.GetRequiredService<IAuthorizationService>();
                var authorizationResult = await authorizationService.AuthorizeAsync(
                    context.User,
                    resource: null,
                    dashboardPolicy);

                if (authorizationResult.Succeeded)
                {
                    await next();
                    return;
                }

                if (context.User.Identity?.IsAuthenticated == true)
                {
                    await context.ForbidAsync();
                    return;
                }

                await context.ChallengeAsync();
            });
        }

        app.UseTickerQ();
        return app;
    }

    public static async Task MigrateTickerQSchedulerAsync(this WebApplication app)
    {
        if (!IsTickerQSchedulerEnabled(app.Configuration, app.Environment))
        {
            return;
        }

        await using var scope = app.Services.CreateAsyncScope();
        var dbFactory = scope.ServiceProvider.GetRequiredService<IDbContextFactory<ApiTickerQDbContext>>();
        await using var db = await dbFactory.CreateDbContextAsync();
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
