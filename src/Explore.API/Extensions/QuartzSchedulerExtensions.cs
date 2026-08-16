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
using Explore.Application.Configuration;
using Explore.Infrastructure;
using Explore.Infrastructure.Webhooks;
using Explore.Application.Features.OrganizerPaymentConnections;

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

        // The administration policy is registered unconditionally so the operator surface can always explain why
        // it is unavailable. Without it a host with scheduling disabled would fail dependency resolution instead.
        services.TryAddSingleton<ISchedulerAdminPolicy, SchedulerAdminPolicy>();

        // Registered unconditionally alongside the policy: a refused action on a scheduler-less host is still an
        // attempted privileged operation and must leave a record.
        services.TryAddSingleton<ISchedulerAdminAuditSink, LoggingSchedulerAdminAuditSink>();

        if (!enabled || !settings.Enabled || environment.IsEnvironment("Testing"))
        {
            services.TryAddSingleton<ISchedulerOperations, UnavailableSchedulerOperations>();
            return services;
        }

        var runtimeDatabase = PrimaryDatabaseConfiguration.BindRuntime(configuration);
        var connectionString = PrimaryDatabaseConfiguration.BuildConnectionString(runtimeDatabase).ConnectionString;

        services.RemoveAll<IScheduledEmailDispatchTrigger>();
        services.AddScoped<IScheduledEmailDispatchTrigger, QuartzScheduledEmailDispatchTrigger>();
        services.AddSingleton<QuartzSchemaInitializer>();
        services.TryAddSingleton<ISchedulerOperations, QuartzSchedulerOperations>();

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
            RegisterMaintenanceSweeps(quartz, configuration);
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

    /// <summary>
    /// Registers the periodic maintenance sweeps that previously each ran as a hand-rolled
    /// <c>BackgroundService</c> timer loop. Enablement and cadence stay in each feature's own settings section
    /// so operator-facing configuration keys are unchanged; only the mechanism moved.
    /// </summary>
    private static void RegisterMaintenanceSweeps(
        IServiceCollectionQuartzConfigurator quartz,
        IConfiguration configuration)
    {
        var idempotency = Bind<IdempotencyCleanupSettings>(configuration, IdempotencyCleanupSettings.SectionName);
        AddSweepJob<IdempotencyCleanupJob>(
            quartz,
            QuartzSchedulerKeys.IdempotencyCleanup,
            "Removes expired idempotency replay-cache rows.",
            idempotency.Enabled,
            idempotency.InitialDelaySeconds,
            idempotency.PollingIntervalMinutes);

        var aiRetention = Bind<AiRetentionCleanupSettings>(configuration, AiRetentionCleanupSettings.SectionName);
        AddSweepJob<AiRetentionCleanupJob>(
            quartz,
            QuartzSchedulerKeys.AiRetentionCleanup,
            "Applies AI conversation retention policy across tenants.",
            aiRetention.Enabled,
            aiRetention.InitialDelaySeconds,
            aiRetention.PollingIntervalMinutes);

        var emailRetention = Bind<EmailDispatchRetentionSettings>(configuration, EmailDispatchRetentionSettings.SectionName);
        AddSweepJob<EmailDispatchRetentionCleanupJob>(
            quartz,
            QuartzSchedulerKeys.EmailDispatchRetentionCleanup,
            "Applies email dispatch outbox and receipt retention policy.",
            emailRetention.Enabled,
            emailRetention.InitialDelaySeconds,
            emailRetention.PollingIntervalMinutes);

        var webhookRetention = Bind<WebhookRetentionSettings>(configuration, WebhookRetentionSettings.SectionName);
        AddSweepJob<WebhookRetentionCleanupJob>(
            quartz,
            QuartzSchedulerKeys.WebhookRetentionCleanup,
            "Applies webhook message and delivery-attempt retention policy across tenants.",
            webhookRetention.Enabled,
            webhookRetention.InitialDelaySeconds,
            webhookRetention.PollingIntervalMinutes);

        var storage = Bind<StorageReconciliationSettings>(configuration, StorageReconciliationSettings.SectionName);
        AddSweepJob<StorageReconciliationJob>(
            quartz,
            QuartzSchedulerKeys.StorageReconciliation,
            "Reconciles storage object state against the configured provider.",
            storage.Enabled,
            storage.InitialDelaySeconds,
            storage.PollingIntervalMinutes);

        // Registration retention has no settings section: its cadence is fixed by the immutable-deadline
        // policy rather than being operator-tunable, so the schedule is stated here rather than bound.
        AddSweepJob<RegistrationRetentionCleanupJob>(
            quartz,
            QuartzSchedulerKeys.RegistrationRetentionCleanup,
            "Applies registration answer and PII retention deadlines for every active tenant.",
            enabled: true,
            initialDelaySeconds: 300,
            TimeSpan.FromDays(1));

        var organizerPayment = Bind<OrganizerPaymentReadinessReconciliationOptions>(
            configuration,
            OrganizerPaymentReadinessReconciliationOptions.SectionName);
        AddSweepJob<OrganizerPaymentReadinessReconciliationJob>(
            quartz,
            QuartzSchedulerKeys.OrganizerPaymentReadinessReconciliation,
            "Refreshes stale organizer payment provider readiness state.",
            organizerPayment.Enabled,
            organizerPayment.InitialDelaySeconds,
            TimeSpan.FromSeconds(Math.Max(5, organizerPayment.PollingIntervalSeconds)));

        // Privacy erasure credential cleanup keeps its own settings shape: its cadence is expressed as a
        // TimeSpan poll interval shared with the provider lease machinery rather than as whole minutes.
        var privacyErasure = Bind<PrivacyErasureOptions>(configuration, PrivacyErasureOptions.SectionName);
        AddSweepJob<PrivacyErasureCredentialCleanupJob>(
            quartz,
            QuartzSchedulerKeys.PrivacyErasureCredentialCleanup,
            "Expires privacy-erasure provider credentials and locators past their retention horizon.",
            privacyErasure.RetentionCleanupEnabled,
            initialDelaySeconds: 0,
            privacyErasure.ProviderPollingInterval);
    }

    private static TSettings Bind<TSettings>(IConfiguration configuration, string sectionName)
        where TSettings : new()
        => configuration.GetSection(sectionName).Get<TSettings>() ?? new TSettings();

    private static void AddSweepJob<TJob>(
        IServiceCollectionQuartzConfigurator quartz,
        JobKey jobKey,
        string description,
        bool enabled,
        int initialDelaySeconds,
        int intervalMinutes)
        where TJob : IJob
        => AddSweepJob<TJob>(quartz, jobKey, description, enabled, initialDelaySeconds, TimeSpan.FromMinutes(Math.Max(1, intervalMinutes)));

    /// <summary>
    /// A disabled sweep is simply not registered, which keeps a turned-off feature out of the persistent
    /// store entirely instead of leaving a dormant trigger for operators to puzzle over.
    /// </summary>
    private static void AddSweepJob<TJob>(
        IServiceCollectionQuartzConfigurator quartz,
        JobKey jobKey,
        string description,
        bool enabled,
        int initialDelaySeconds,
        TimeSpan interval)
        where TJob : IJob
    {
        if (!enabled)
        {
            return;
        }

        quartz.AddJob<TJob>(job => job
            .WithIdentity(jobKey)
            .WithDescription(description)
            .StoreDurably());

        quartz.AddTrigger(trigger => trigger
            .WithIdentity(QuartzSchedulerKeys.RecurringTriggerFor(jobKey))
            .ForJob(jobKey)
            .StartAt(DateTimeOffset.UtcNow.AddSeconds(Math.Max(0, initialDelaySeconds)))
            .WithSimpleSchedule(schedule => schedule
                .WithInterval(interval)
                .RepeatForever()
                // Maintenance sweeps are idempotent and horizon-based, so a backlog of missed runs collapses
                // into the next pass rather than firing once per skipped interval after downtime.
                .WithMisfireHandlingInstructionNextWithRemainingCount()));
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
