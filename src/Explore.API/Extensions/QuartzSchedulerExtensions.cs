// ABOUTME: Registers Quartz.NET as the API host scheduler, persistence, and operator status surface.
// ABOUTME: Confines every Quartz dependency to the API layer so Application contracts stay scheduler-neutral.

using System.Globalization;
using Explore.API.Configuration;
using Explore.API.Scheduling;
using Explore.Application.Contracts.Infrastructure;
using Explore.Application.Contracts.Scheduling;
using Explore.Secrets.Database;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;
using Quartz;
using Quartz.AspNetCore;

namespace Explore.API.Extensions;

public static class QuartzSchedulerExtensions
{
    /// <summary>
    /// Registers the scheduler. Unlike the EF Core-backed scheduler it replaces, the ADO job store works on
    /// every supported primary database provider, so standalone SQLite deployments get durable scheduling too.
    /// </summary>
    public static IServiceCollection AddApiQuartzScheduler(
        this IServiceCollection services,
        IConfiguration configuration,
        IWebHostEnvironment environment,
        bool enabled)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);
        ArgumentNullException.ThrowIfNull(environment);

        var settings = configuration.GetSection(QuartzSchedulerSettings.SectionName).Get<QuartzSchedulerSettings>()
            ?? new QuartzSchedulerSettings();

        services.AddOptions<QuartzSchedulerSettings>()
            .Bind(configuration.GetSection(QuartzSchedulerSettings.SectionName))
            .ValidateOnStart();
        services.AddSingleton<IValidateOptions<QuartzSchedulerSettings>, QuartzSchedulerSettingsValidator>();

        if (!enabled || !settings.Enabled || environment.IsEnvironment("Testing"))
        {
            return services;
        }

        var runtimeDatabase = PrimaryDatabaseConfiguration.BindRuntime(configuration);
        var connectionString = PrimaryDatabaseConfiguration.BuildConnectionString(runtimeDatabase).ConnectionString;

        services.RemoveAll<IScheduledEmailDispatchTrigger>();
        services.AddScoped<IScheduledEmailDispatchTrigger, QuartzScheduledEmailDispatchTrigger>();
        services.AddSingleton<QuartzSchemaInitializer>();

        services.AddQuartz(quartz =>
        {
            quartz.SchedulerName = settings.SchedulerName;
            quartz.SchedulerId = settings.InstanceId;
            quartz.UseDefaultThreadPool(Math.Max(1, settings.MaxConcurrency));
            quartz.InterruptJobsOnShutdownWithWait = true;

            if (settings.UsePersistentStore)
            {
                ConfigurePersistentStore(quartz, settings, runtimeDatabase.Provider, connectionString);
            }
            else
            {
                quartz.UseInMemoryStore();
            }

            RegisterRecurringJobs(quartz, settings);
            RegisterOnDemandJobs(quartz);
        });

        // WaitForJobsToComplete keeps a mid-flight dispatch batch from being torn off during container shutdown.
        services.AddQuartzServer(quartzServer =>
        {
            quartzServer.WaitForJobsToComplete = true;
            quartzServer.AwaitApplicationStarted = true;
        });

        return services;
    }

    /// <summary>
    /// Maps the read-only scheduler status surface behind the instance-admin policy. It is intentionally not
    /// part of the public API contract, so it stays out of OpenAPI generation and HAL link policies.
    /// </summary>
    public static WebApplication UseApiQuartzScheduler(this WebApplication app)
    {
        ArgumentNullException.ThrowIfNull(app);

        var settings = app.Configuration.GetSection(QuartzSchedulerSettings.SectionName).Get<QuartzSchedulerSettings>()
            ?? new QuartzSchedulerSettings();

        if (!settings.StatusEndpointEnabled)
        {
            return app;
        }

        var statusPath = string.IsNullOrWhiteSpace(settings.StatusEndpointPath)
            ? QuartzSchedulerSettings.DefaultStatusEndpointPath
            : settings.StatusEndpointPath;
        var statusPolicy = string.IsNullOrWhiteSpace(settings.StatusEndpointAuthorizationPolicy)
            ? QuartzSchedulerSettings.InstanceAdminPolicyName
            : settings.StatusEndpointAuthorizationPolicy;

        // Authorization runs as explicit middleware rather than endpoint metadata so an unauthenticated caller
        // is challenged before any scheduler state is read, matching the guardrail the previous scheduler had.
        app.Use(async (context, next) =>
        {
            if (!context.Request.Path.StartsWithSegments(statusPath))
            {
                await next();
                return;
            }

            var authorizationService = context.RequestServices.GetRequiredService<IAuthorizationService>();
            var authorizationResult = await authorizationService.AuthorizeAsync(
                context.User,
                resource: null,
                statusPolicy);

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

        return app;
    }

    /// <summary>Maps the scheduler status endpoint itself; separated so hosts control endpoint ordering.</summary>
    public static WebApplication MapApiQuartzSchedulerEndpoints(this WebApplication app)
    {
        ArgumentNullException.ThrowIfNull(app);

        var settings = app.Configuration.GetSection(QuartzSchedulerSettings.SectionName).Get<QuartzSchedulerSettings>()
            ?? new QuartzSchedulerSettings();

        if (!settings.StatusEndpointEnabled)
        {
            return app;
        }

        var statusPath = string.IsNullOrWhiteSpace(settings.StatusEndpointPath)
            ? QuartzSchedulerSettings.DefaultStatusEndpointPath
            : settings.StatusEndpointPath;
        var statusPolicy = string.IsNullOrWhiteSpace(settings.StatusEndpointAuthorizationPolicy)
            ? QuartzSchedulerSettings.InstanceAdminPolicyName
            : settings.StatusEndpointAuthorizationPolicy;

        app.MapGet(statusPath, QuartzSchedulerStatusEndpoint.HandleAsync)
            .RequireAuthorization(statusPolicy)
            .ExcludeFromDescription();

        return app;
    }

    public static async Task ApplyQuartzSchedulerSchemaAsync(this WebApplication app, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(app);

        if (!IsQuartzSchedulerEnabled(app.Configuration, app.Environment))
        {
            return;
        }

        var initializer = app.Services.GetService<QuartzSchemaInitializer>();
        if (initializer is null)
        {
            return;
        }

        var runtimeDatabase = PrimaryDatabaseConfiguration.BindRuntime(app.Configuration);
        await initializer.ApplyAsync(runtimeDatabase.Provider, cancellationToken);
    }

    public static bool IsQuartzSchedulerEnabled(IConfiguration configuration, IWebHostEnvironment environment)
    {
        ArgumentNullException.ThrowIfNull(configuration);
        ArgumentNullException.ThrowIfNull(environment);

        if (environment.IsEnvironment("Testing"))
        {
            return false;
        }

        var settings = configuration.GetSection(QuartzSchedulerSettings.SectionName).Get<QuartzSchedulerSettings>()
            ?? new QuartzSchedulerSettings();
        return settings.Enabled;
    }

    private static void ConfigurePersistentStore(
        IServiceCollectionQuartzConfigurator quartz,
        QuartzSchedulerSettings settings,
        PrimaryDatabaseProvider provider,
        string connectionString)
    {
        quartz.UsePersistentStore(store =>
        {
            // Pointer-only payloads are plain strings, so property storage avoids any binary serialization
            // of application types into scheduler rows.
            store.UseProperties = true;
            store.UseSystemTextJsonSerializer();

            void ConfigureAdo(SchedulerBuilder.AdoProviderOptions ado)
            {
                ado.ConnectionString = connectionString;
                ado.TablePrefix = settings.TablePrefix;
            }

            switch (provider)
            {
                case PrimaryDatabaseProvider.PostgreSql:
                    store.UsePostgres(ConfigureAdo);
                    break;
                case PrimaryDatabaseProvider.Sqlite:
                    store.UseMicrosoftSQLite(ConfigureAdo);
                    break;
                case PrimaryDatabaseProvider.SqlServer:
                    store.UseSqlServer(ConfigureAdo);
                    break;
                case PrimaryDatabaseProvider.MariaDb:
                case PrimaryDatabaseProvider.MySql:
                    store.UseMySqlConnector(ConfigureAdo);
                    break;
                default:
                    throw new NotSupportedException(
                        string.Create(
                            CultureInfo.InvariantCulture,
                            $"Quartz persistent store is not configured for database provider '{provider}'."));
            }

            if (settings.ClusteringEnabled)
            {
                store.UseClustering(clustering => clustering.CheckinInterval =
                    TimeSpan.FromSeconds(Math.Max(1, settings.ClusterCheckinIntervalSeconds)));
            }
        });
    }

    private static void RegisterRecurringJobs(
        IServiceCollectionQuartzConfigurator quartz,
        QuartzSchedulerSettings settings)
    {
        AddCronJob<EmailDispatchDrainJob>(
            quartz,
            QuartzSchedulerKeys.EmailDispatchDrain,
            QuartzSchedulerKeys.EmailDispatchDrainCron,
            "Claims due EmailDispatchOutbox rows and executes approved dispatch transports.");

        AddCronJob<EmailDispatchRecoveryScanJob>(
            quartz,
            QuartzSchedulerKeys.EmailDispatchRecoveryScan,
            QuartzSchedulerKeys.EmailDispatchRecoveryScanCron,
            "Marks stale EmailDispatchOutbox processing leases as Unknown for operator review.");

        _ = settings;
    }

    /// <summary>
    /// The reminder job is stored durably with no trigger of its own; runtime code attaches one-off triggers
    /// to it through <see cref="QuartzScheduledEmailDispatchTrigger"/>.
    /// </summary>
    private static void RegisterOnDemandJobs(IServiceCollectionQuartzConfigurator quartz)
    {
        quartz.AddJob<EventReminderDispatchJob>(job => job
            .WithIdentity(QuartzSchedulerKeys.EventReminderDispatch)
            .WithDescription("Wakes a pre-persisted event reminder EmailDispatchOutbox row at its scheduled time.")
            .StoreDurably());
    }

    private static void AddCronJob<TJob>(
        IServiceCollectionQuartzConfigurator quartz,
        JobKey jobKey,
        string cronExpression,
        string description)
        where TJob : IJob
    {
        quartz.AddJob<TJob>(job => job
            .WithIdentity(jobKey)
            .WithDescription(description)
            .StoreDurably());

        quartz.AddTrigger(trigger => trigger
            .WithIdentity(QuartzSchedulerKeys.RecurringTriggerFor(jobKey))
            .ForJob(jobKey)
            .WithCronSchedule(cronExpression, schedule => schedule
                .InTimeZone(TimeZoneInfo.Utc)
                // A restart after downtime should resume the normal cadence, not replay every missed occurrence:
                // the durable outbox already holds the backlog and the next pass drains it.
                .WithMisfireHandlingInstructionDoNothing()));
    }
}
