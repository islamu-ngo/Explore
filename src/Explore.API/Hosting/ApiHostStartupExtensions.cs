// ABOUTME: Runs reusable API lifetime registration and ordered pre-start initialization.
// ABOUTME: Preserves migrations, privacy gating, setup-secret initialization, and 25-second shutdown behavior.

using Explore.API.BackgroundServices;
using Explore.API.Extensions;
using Explore.Application.Contracts.Services;
using Explore.Persistence;
using Explore.Persistence.Schema;
using Explore.Persistence.Seed;
using Microsoft.EntityFrameworkCore;

namespace Explore.API.Hosting;

public static class ApiHostStartupExtensions
{
    public const int GracefulShutdownSeconds = 25;

    public static async Task RunApiHostStartupAsync(
        this WebApplication app,
        ApiHostCompositionState state,
        CancellationTokenSource shutdownCts,
        Action markShuttingDown)
    {
        ArgumentNullException.ThrowIfNull(app);
        ArgumentNullException.ThrowIfNull(state);
        ArgumentNullException.ThrowIfNull(shutdownCts);
        ArgumentNullException.ThrowIfNull(markShuttingDown);

        var appLifetime = app.Lifetime;
        var appLogger = app.Logger;
        appLifetime.ApplicationStopping.Register(() =>
        {
            markShuttingDown();
            appLogger.LogInformation(
                "SIGTERM received. Starting graceful shutdown. Health checks return 503. " +
                "Accepting requests for {Seconds} more seconds...",
                GracefulShutdownSeconds);
        });

        Console.CancelKeyPress += (_, eventArgs) =>
        {
            appLogger.LogWarning("SIGINT received. Initiating graceful shutdown...");
            eventArgs.Cancel = true;
            shutdownCts.Cancel();

            try
            {
                appLifetime.StopApplication();
            }
            catch (ObjectDisposedException)
            {
            }
        };

        if (!app.Environment.IsEnvironment("Testing") && !state.IsOpenApiGeneration)
        {
            using var scope = app.Services.CreateScope();
            var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();

            try
            {
                if (app.Environment.IsDevelopment())
                {
                    var db = scope.ServiceProvider.GetRequiredService<ExploreDbContext>();
                    if (db.Database.IsRelational())
                    {
                        logger.LogInformation("Applying database migrations...");
                        await ExploreDatabaseMigrator.MigrateAsync(db, app.Configuration);
                        await PostgresModelConstraintApplier.ApplyAsync(db);
                        logger.LogInformation("Database migrations completed successfully.");
                    }
                    else
                    {
                        logger.LogInformation(
                            "Skipping database migrations because provider {ProviderName} is non-relational.",
                            db.Database.ProviderName ?? "(unknown)");
                    }

                    var seedDevelopmentData =
                        !app.Configuration.GetValue<bool>("Testing:DisableDevelopmentDataSeed");
                    await DatabaseSeeder.SeedAsync(
                        db,
                        app.Environment,
                        seedDevelopmentData,
                        app.Configuration);
                    logger.LogInformation("Database seeding completed.");
                }
                else
                {
                    logger.LogInformation(
                        "Application and Data Protection migrations are owned by Event.MigrationService outside Development.");
                }

                if (state.UseTickerQEmailDispatch)
                {
                    logger.LogInformation("Applying TickerQ scheduler migrations...");
                    await app.MigrateTickerQSchedulerAsync();
                    logger.LogInformation("TickerQ scheduler migrations completed successfully.");
                }
            }
            catch (Exception exception)
            {
                logger.LogCritical(exception, "Database migration failed. Application cannot start.");
                throw;
            }
        }

        if ((!app.Environment.IsEnvironment("Testing") ||
             app.Configuration.GetValue<bool>("Testing:EnablePrivacyErasureStartupGate")) &&
            !state.IsOpenApiGeneration)
        {
            await PrivacyErasureStartupGate.RunAsync(app.Services, shutdownCts.Token);
        }

        if (!state.IsOpenApiGeneration)
        {
            var setupSecretProvider = app.Services.GetRequiredService<ISetupSecretProvider>();
            await setupSecretProvider.InitializeAsync();
            if (setupSecretProvider.IsSetupModeActive)
            {
                if (!setupSecretProvider.IsSetupSecretRequired)
                {
                    app.Logger.LogInformation(
                        "[SetupSecret] Interactive setup-secret validation disabled by trusted managed provisioning configuration. Setup endpoints still reject anonymous setup-secret access.");
                }
                else if (setupSecretProvider.IsFromEnvironmentVariable)
                {
                    app.Logger.LogInformation("[SetupSecret] SETUP_SECRET loaded from environment variable.");
                }
                else
                {
                    if (setupSecretProvider.GeneratedSecretFilePath is not null)
                    {
                        app.Logger.LogWarning(
                            "[SetupMode] Instance is unclaimed. Retrieve the generated setup secret from the Docker host with: " +
                            "docker cp <container-name>:{SetupSecretFilePath} ./setup-secret",
                            setupSecretProvider.GeneratedSecretFilePath);
                    }
                    else
                    {
                        app.Logger.LogWarning(
                            "[SetupMode] Instance is unclaimed. Auto-generated setup secret active. " +
                            "Configure SETUP_SECRET and restart before visiting /setup.");
                    }
                }
            }
            else
            {
                app.Logger.LogInformation(
                    "[SetupSecret] Instance onboarding already completed. Setup mode inactive.");
            }
        }
    }
}
