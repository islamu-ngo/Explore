// ABOUTME: API host composition root for services, middleware, OpenAPI, and development utilities.
// ABOUTME: Wires Clean Architecture layers, tenant-aware HTTP pipeline, and migration/admin endpoints.

using System.IO.Compression;
using System.Net;
using Explore.API.BackgroundServices;
using Explore.API.Configuration;
using Explore.API.Extensions;
using Explore.API.HealthChecks;
using Explore.API.Mcp;
using Explore.API.Middleware;
using Explore.API.OpenApi;
using Explore.API.Services.Calendar;
using Explore.Application;
using Explore.Application.Contracts.Infrastructure;
using Explore.Application.Contracts.Services;
using Explore.Application.Telemetry;
using Explore.Infrastructure;
using Explore.Infrastructure.HealthChecks;
using Explore.Infrastructure.Messaging;
using Explore.Persistence;
using Explore.Persistence.Schema;
using Explore.Persistence.Seed;
using Explore.Secrets.Extensions;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.ResponseCompression;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Options;
using Microsoft.IO;
using Microsoft.OpenApi;
using ModelContextProtocol.AspNetCore;
using ModelContextProtocol.Server;
using OpenFeature;
using OpenFeature.Hosting.Providers.Memory;
using Scalar.AspNetCore;
using Serilog;
using static Microsoft.AspNetCore.Http.StatusCodes;

// Graceful shutdown tracking for zero-downtime deployments
// SIGTERM: 25 second grace period (health returns 503, still accepts requests)
// SIGINT: Immediate shutdown via host StopApplication()
var shutdownCts = new CancellationTokenSource();
const int GracefulShutdownSeconds = 25;

var builder = WebApplication.CreateBuilder(args);
var isOpenApiGeneration = OpenApiGenerationMode.IsBuildTimeGeneration;

// Configure shutdown timeout for graceful termination
builder.WebHost.ConfigureKestrel(options =>
{
    options.Limits.KeepAliveTimeout = TimeSpan.FromSeconds(GracefulShutdownSeconds + 5);
});

// Set host shutdown timeout
builder.Host.ConfigureHostOptions(options =>
{
    options.ShutdownTimeout = TimeSpan.FromSeconds(GracefulShutdownSeconds + 5);
});

builder.AddServiceDefaults();
builder.AddRedisDistributedCache(connectionName: "cache");
builder.AddDistributedCacheReadinessCheck();
builder.AddOidcDiscoveryReadinessCheck();
builder.Configuration.AddInfisicalCompatibility();

// Add secret management services (refresh service, health checks, metrics, audit logging)
// This adds observability and background refresh for secrets loaded via AddInfisicalCompatibility
builder.Services.AddSecretManagement(
    builder.Configuration,
    enableAuditing: true,
    enableRefreshService: !isOpenApiGeneration);

builder.Services.AddHttpContextAccessor();

var forwardedHeadersTrust = builder.Configuration
    .GetSection(ForwardedHeadersTrustOptions.SectionName)
    .Get<ForwardedHeadersTrustOptions>() ?? new ForwardedHeadersTrustOptions();

if (builder.Environment.IsEnvironment("Testing") &&
    builder.Configuration[$"{ForwardedHeadersTrustOptions.SectionName}:TrustLoopbackProxy"] is null &&
    !forwardedHeadersTrust.TrustLoopbackProxy &&
    forwardedHeadersTrust.KnownProxies.Count == 0 &&
    forwardedHeadersTrust.KnownNetworks.Count == 0)
{
    forwardedHeadersTrust.TrustLoopbackProxy = true;
}

builder.Services.Configure<ForwardedHeadersOptions>(options =>
{
    forwardedHeadersTrust.ApplyTo(options);
});

// Performance: Response compression (Brotli + Gzip)
builder.Services.AddResponseCompression(options =>
{
    options.EnableForHttps = true;
    options.Providers.Add<BrotliCompressionProvider>();
    options.Providers.Add<GzipCompressionProvider>();
    options.MimeTypes = ResponseCompressionDefaults.MimeTypes.Concat(["application/json", "application/hal+json"]);
});
builder.Services.Configure<BrotliCompressionProviderOptions>(options => options.Level = CompressionLevel.Fastest);
builder.Services.Configure<GzipCompressionProviderOptions>(options => options.Level = CompressionLevel.Fastest);

builder.Services.AddApiCaching();
builder.Services.AddSingleton<IEventCalendarFileBuilder, IcalNetEventCalendarFileBuilder>();

builder.Services.AddRouting(options =>
{
    options.LowercaseUrls = true;
});

// Add services to the container.

builder.Services.ConfigureApplicationServices();
builder.Services.ConfigureInfrastructureServices(builder.Configuration);
builder.Services.Configure<CerbosPolicyBootSyncOptions>(
    builder.Configuration.GetSection(CerbosPolicyBootSyncOptions.SectionName));
builder.Services.PostConfigure<McpAdapterSettings>(settings =>
{
    if (string.IsNullOrWhiteSpace(settings.EndpointPath))
    {
        return;
    }

    var endpointPath = settings.EndpointPath.Trim();
    settings.EndpointPath = endpointPath.StartsWith("/", StringComparison.Ordinal)
        ? endpointPath
        : $"/{endpointPath}";
});
builder.Services.AddOptions<McpAdapterSettings>()
    .Bind(builder.Configuration.GetSection(McpAdapterSettings.SectionName))
    .ValidateOnStart();
builder.Services.AddSingleton<IValidateOptions<McpAdapterSettings>, McpAdapterSettingsValidator>();
builder.Services.AddScoped<IMcpRuntimeStateService, McpRuntimeStateService>();
var mcpAdapterSettings = builder.Configuration
    .GetSection(McpAdapterSettings.SectionName)
    .Get<McpAdapterSettings>() ?? new McpAdapterSettings();
var emailDispatchProcessorSettings = builder.Configuration
    .GetSection(EmailDispatchProcessorSettings.SectionName)
    .Get<EmailDispatchProcessorSettings>() ?? new EmailDispatchProcessorSettings();
var emailDispatchRabbitMqSettings = builder.Configuration
    .GetSection(EmailDispatchRabbitMqSettings.SectionName)
    .Get<EmailDispatchRabbitMqSettings>() ?? new EmailDispatchRabbitMqSettings();
var useTickerQEmailDispatch = emailDispatchProcessorSettings.Enabled
    && emailDispatchProcessorSettings.Mode == EmailDispatchProcessorMode.TickerQ;
builder.Services.AddApiTickerQScheduler(
    builder.Configuration,
    builder.Environment,
    enabled: useTickerQEmailDispatch && !isOpenApiGeneration);

// Skip DbContext registration if running in Testing environment (integration tests register their own)
// or build-time OpenAPI generation (the endpoint graph is needed, not live persistence).
var skipDbContext = builder.Environment.IsEnvironment("Testing") || isOpenApiGeneration;
builder.Services.ConfigurePersistenceServices(
    builder.Configuration,
    skipDbContextRegistration: skipDbContext,
    skipLookupCacheInitializer: isOpenApiGeneration,
    environmentName: builder.Environment.EnvironmentName);

// Register shared tenant context; API middleware is authoritative for normal tenant resolution.
builder.Services.AddScoped<ITenantResolverService, Explore.Infrastructure.Services.TenantResolverService>();
builder.Services.AddScoped<ITenantContext, Explore.Infrastructure.Services.TenantContext>();

// Register HATEOAS infrastructure and resource assemblers
builder.Services.AddHateoas();
builder.Services.AddHateoasAssemblers();

// API versioning: media type strategy (Accept: application/json;v=1.0)
builder.Services.AddApiMediaTypeVersioning();

// Business metrics (OpenTelemetry)
builder.Services.AddSingleton<BusinessMetrics>();
builder.Services.AddSingleton<TranslationMetrics>();
builder.Services.AddSingleton<ProjectionMetrics>();

// Pooled memory streams for ETag middleware — eliminates per-request MemoryStream allocations
builder.Services.AddSingleton<RecyclableMemoryStreamManager>();


builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.TypeInfoResolverChain.Add(
            Explore.Application.Serialization.ExploreJsonContext.Default);
        options.JsonSerializerOptions.PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase;
        options.JsonSerializerOptions.DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull;
        options.JsonSerializerOptions.UnmappedMemberHandling = System.Text.Json.Serialization.JsonUnmappedMemberHandling.Disallow;
        // Serialize all enums as strings by default — matches client DTO expectations
        // (e.g. DeploymentModeDto.Mode) and industry best practice for public APIs.
        options.JsonSerializerOptions.Converters.Add(
            new System.Text.Json.Serialization.JsonStringEnumConverter());
    });
builder.Services.Configure<Microsoft.AspNetCore.Mvc.ApiBehaviorOptions>(options =>
{
    options.InvalidModelStateResponseFactory =
        Explore.API.ExceptionHandling.ApiValidationProblemDetailsFactory.CreateInvalidModelStateResponse;
    options.ClientErrorMapping[StatusCodes.Status415UnsupportedMediaType].Title = "Unsupported media type";
    options.ClientErrorMapping[StatusCodes.Status415UnsupportedMediaType].Link =
        "https://tools.ietf.org/html/rfc9110#section-15.5.16";
});

builder.Services.AddApiExceptionHandling();

// ──────────────────────────────────────────────
// Localization — CultureRegistry is the compile-time allowlist.
// Runtime governance (enabled_languages / kill-switches) is enforced higher up.
// ──────────────────────────────────────────────
builder.Services.AddLocalization();
builder.Services.Configure<Microsoft.AspNetCore.Builder.RequestLocalizationOptions>(options =>
{
    var cultures = Explore.Domain.Common.Localization.CultureRegistry.GetAll()
        .Select(entry => new System.Globalization.CultureInfo(entry.Code))
        .ToArray();

    options.SupportedCultures = cultures;
    options.SupportedUICultures = cultures;
    options.DefaultRequestCulture = new Microsoft.AspNetCore.Localization.RequestCulture("en");
    options.RequestCultureProviders.Clear();
    options.RequestCultureProviders.Insert(0, new Microsoft.AspNetCore.Localization.CookieRequestCultureProvider());
    options.RequestCultureProviders.Insert(1, new Microsoft.AspNetCore.Localization.AcceptLanguageHeaderRequestCultureProvider());
});

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGenWithAuth(builder.Configuration);
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi

// Configure native OpenAPI (for /openapi/event-api.json endpoint)
// Register document transformer to add missing DTO schemas that are hidden inside HAL wrappers
builder.Services.AddOpenApi("event-api", options =>
{
    options.ShouldInclude = (description) => true;
    options.AddDocumentTransformer((document, _, _) =>
    {
        document.Info.Title = "Event API";
        document.Info.Version = "v0.1";
        return Task.CompletedTask;
    });
    options.AddDocumentTransformer<Explore.API.OpenApi.KeycloakOpenApiSecurityTransformer>();
    options.AddDocumentTransformer<Explore.API.OpenApi.OpenApiStringEnumDocumentTransformer>();
    options.AddDocumentTransformer<Explore.API.OpenApi.HalDtoSchemaTransformer>();
    options.AddDocumentTransformer<Explore.API.OpenApi.OperationIdInvariantTransformer>();
    options.AddOperationTransformer<Explore.API.OpenApi.EndpointClassificationTransformer>();
});

// Register PDS sync background worker for AT Protocol federation
if (!isOpenApiGeneration)
{
    builder.Services.AddHostedService<PdsSyncWorker>();
}

// Register generic outbox processor for reliable side-effect delivery
if (!isOpenApiGeneration)
{
    builder.Services.AddHostedService<OutboxProcessor>();
    if (!builder.Environment.IsEnvironment("Testing"))
    {
        builder.Services.AddHostedService<IdempotencyCleanupProcessor>();
        builder.Services.AddHostedService<AiRetentionCleanupProcessor>();
        builder.Services.AddHostedService<StorageReconciliationProcessor>();
    }

    if (emailDispatchProcessorSettings.Enabled &&
        emailDispatchProcessorSettings.Mode == EmailDispatchProcessorMode.HostedService)
    {
        builder.Services.AddHostedService<EmailDispatchProcessor>();
    }

    if (emailDispatchRabbitMqSettings.Enabled)
    {
        builder.Services.AddHostedService<EmailDispatchRabbitMqConsumerService>();
        if (emailDispatchRabbitMqSettings.DeadLetterReplayEnabled)
        {
            builder.Services.AddHostedService<EmailDispatchRabbitMqDeadLetterReplayService>();
        }
    }
}

// Register zero-touch Cerbos policy package boot synchronization.
// Testing hosts manage policy publishing explicitly to keep integration startup deterministic.
if (!isOpenApiGeneration && !builder.Environment.IsEnvironment("Testing"))
{
    builder.Services.AddSingleton<CerbosPolicyBootSyncRunner>();
    builder.Services.AddHostedService<CerbosPolicyBootSyncWorker>();
}

builder.Services.AddApiCors(builder.Configuration);

builder.Host.UseSerilog((ctx, services, lc) =>
    lc.ReadFrom.Configuration(ctx.Configuration)
      .ReadFrom.Services(services)
      .Enrich.FromLogContext(),
    writeToProviders: true);

builder.Services.AddApiAuthentication(
    builder.Configuration,
    builder.Environment,
    skipAuthorityWarmup: isOpenApiGeneration || builder.Configuration.GetValue<bool>("Testing:SkipJwtAuthorityWarmup"));

builder.Services.AddHsts(options =>
{
    options.Preload = true;
    options.IncludeSubDomains = true;
    options.MaxAge = TimeSpan.FromDays(365);
    //options.ExcludedHosts.Add("example.com");
    //options.ExcludedHosts.Add("www.example.com");
});

// En dev, votre HTTPS local est sur 7039; en prod, laissez null (443 par d�faut)
builder.Services.AddHttpsRedirection(options =>
{
    options.RedirectStatusCode = StatusCodes.Status308PermanentRedirect;
    if (builder.Environment.IsDevelopment())
    {
        options.HttpsPort = 7039;
    }
});

// Shutdown-aware health check for zero-downtime deployments (Coolify rolling updates)
// When SIGTERM is received, health checks return unhealthy so load balancer stops routing traffic
builder.Services.AddHealthChecks()
    .AddCheck("shutdown", () =>
    {
        if (isShuttingDown)
            return HealthCheckResult.Unhealthy("Application is shutting down");
        return HealthCheckResult.Healthy();
    }, tags: ["live", "ready"])
    .AddDbContextCheck<ExploreDbContext>("database", tags: ["ready"])
    .AddCheck<SmtpHealthCheck>(
        "smtp",
        failureStatus: HealthStatus.Unhealthy,
        tags: ["ready", "smtp", "infrastructure"])
    .AddCheck<EmailDispatchHealthCheck>(
        "email-dispatch",
        failureStatus: HealthStatus.Unhealthy,
        tags: ["ready", "email", "dispatch", "infrastructure"])
    .AddCheck<EmailDispatchRabbitMqHealthCheck>(
        "email-dispatch-rabbitmq",
        failureStatus: HealthStatus.Unhealthy,
        tags: ["ready", "email", "dispatch", "rabbitmq", "infrastructure"])
    .AddCheck<IdempotencyCleanupHealthCheck>(
        "idempotency-cleanup",
        failureStatus: HealthStatus.Unhealthy,
        tags: ["ready", "idempotency", "cleanup", "infrastructure"])
    .AddCheck<AiRetentionCleanupHealthCheck>(
        "ai-retention-cleanup",
        failureStatus: HealthStatus.Unhealthy,
        tags: ["ready", "ai", "retention", "cleanup", "infrastructure"])
    .AddCheck<McpAdapterHealthCheck>(
        "mcp-adapter",
        failureStatus: HealthStatus.Unhealthy,
        tags: ["ready", "ai", "mcp", "infrastructure"])
    .AddCheck<StorageReadinessHealthCheck>(
        "storage",
        failureStatus: HealthStatus.Unhealthy,
        tags: ["ready", "storage", "infrastructure"])
    .AddCheck<StorageReconciliationHealthCheck>(
        "storage-reconciliation",
        failureStatus: HealthStatus.Unhealthy,
        tags: ["ready", "storage", "reconciliation", "infrastructure"])
    .AddCheck<CerbosReadinessHealthCheck>(
        "cerbos",
        failureStatus: HealthStatus.Unhealthy,
        tags: ["ready", "cerbos", "infrastructure"])
    .AddCheck<AiProviderHealthCheck>(
        "ai-provider",
        failureStatus: HealthStatus.Unhealthy,
        tags: ["ready", "ai", "provider", "infrastructure"]);

// Request timeouts: default 30s, lookups 10s, complex 60s
builder.Services.AddApiRequestTimeouts(builder.Configuration);

if (!isOpenApiGeneration)
{
    builder.Services
        .AddMcpServer()
        .WithHttpTransport(options =>
        {
            options.Stateless = mcpAdapterSettings.Stateless;
        })
        .AddAuthorizationFilters()
        .WithTools<AiToolRegistryMcpTools>()
        .WithTools<AiAssistantMcpTools>()
        .WithResources<AiAssistantMcpResources>()
        .WithPrompts<AiAssistantMcpPrompts>();

    builder.Services.AddSingleton<IConfigureOptions<McpServerOptions>, AiMcpProjectedToolOptionsSetup>();
}

// Tiered rate limiting: global (IP), authenticated (user), write (stricter)
// Supports X-Forwarded-For for reverse proxy deployments (ngrok, Cloudflare)
builder.Services.AddApiRateLimiting(builder.Configuration, builder.Environment);

// Feature flags: OpenFeature with in-memory provider as default.
// Swap to FeatBit, Unleash, or PostHog by replacing the provider registration.
builder.Services.AddOpenFeature(featureBuilder =>
{
    featureBuilder.AddInMemoryProvider(flags => { });
});

var app = builder.Build();
var appLifetime = app.Lifetime;
var appLogger = app.Logger;

// WebApplicationFactory can spin multiple in-process hosts inside the same test run.
// Reset the static shutdown flag on each fresh host so liveness checks do not inherit
// a prior host's termination state.
isShuttingDown = false;

// Register graceful shutdown handlers for zero-downtime deployments
// SIGTERM: Start graceful shutdown with 25 second grace period
// SIGINT (Ctrl+C): Immediate shutdown
appLifetime.ApplicationStopping.Register(() =>
{
    isShuttingDown = true;
    appLogger.LogInformation(
        "SIGTERM received. Starting graceful shutdown. Health checks return 503. " +
        "Accepting requests for {Seconds} more seconds...",
        GracefulShutdownSeconds);
});

// Handle SIGINT — delegate to host for graceful drain
Console.CancelKeyPress += (sender, e) =>
{
    appLogger.LogWarning("SIGINT received. Initiating graceful shutdown...");
    e.Cancel = true; // Prevent immediate CLR termination; let the host drain
    shutdownCts.Cancel();

    try
    {
        appLifetime.StopApplication();
    }
    catch (ObjectDisposedException)
    {
        // The host may already be disposing when the test runner forwards SIGINT.
        // Treat repeated shutdown signals as idempotent so cancellation does not
        // turn a clean test abort into an unhandled process crash.
    }
};


// Apply database migrations before starting the application
// EF Core 9+ has built-in locking for concurrent migration protection (safe for multiple replicas)
// Migrations run before API accepts traffic - if they fail, the container doesn't start
if (!builder.Environment.IsEnvironment("Testing") && !isOpenApiGeneration)
{
    using var scope = app.Services.CreateScope();
    var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();
    var db = scope.ServiceProvider.GetRequiredService<ExploreDbContext>();

    try
    {
        if (db.Database.IsRelational())
        {
            logger.LogInformation("Applying database migrations...");
            await db.Database.MigrateAsync();
            await PostgresModelConstraintApplier.ApplyAsync(db);
            logger.LogInformation("Database migrations completed successfully.");
        }
        else
        {
            logger.LogInformation(
                "Skipping database migrations because provider {ProviderName} is non-relational.",
                db.Database.ProviderName ?? "(unknown)");
        }

        // Run seeding (lookup tables in all environments, dev data in Development)
        await DatabaseSeeder.SeedAsync(db, app.Environment);
        logger.LogInformation("Database seeding completed.");

        if (useTickerQEmailDispatch)
        {
            logger.LogInformation("Applying TickerQ scheduler migrations...");
            await app.MigrateTickerQSchedulerAsync();
            logger.LogInformation("TickerQ scheduler migrations completed successfully.");
        }
    }
    catch (Exception ex)
    {
        logger.LogCritical(ex, "Database migration failed. Application cannot start.");
        throw; // Prevent app from starting with failed migration
    }
}

// Setup secret bootstrap logging — resolve provider and log the secret for first-run setup.
// Console.WriteLine guarantees visibility in all environments (bypasses Serilog log-level filters).
// Matches established Infisical bootstrap pattern (InfisicalConfigurationProvider.cs).
string? setupSecretForStartupReminder = null;
if (!isOpenApiGeneration)
{
    var setupSecretProvider = app.Services.GetRequiredService<Explore.Application.Contracts.Services.ISetupSecretProvider>();
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
            var secretForLog = setupSecretProvider.GetSecretForLogging();
            setupSecretForStartupReminder = secretForLog;
            app.Logger.LogWarning(
                "[SetupMode] Instance is unclaimed. Auto-generated setup secret active. " +
                "Visit /setup to claim. Secret: {SetupSecret}",
                secretForLog);
            // Console output for terminal visibility when SSH'd into a container
            Console.WriteLine($"[SetupMode] Setup secret: {secretForLog}");
        }
    }
    else
    {
        app.Logger.LogInformation("[SetupSecret] Instance onboarding already completed. Setup mode inactive.");
    }
}

if (!string.IsNullOrWhiteSpace(setupSecretForStartupReminder))
{
    app.Lifetime.ApplicationStarted.Register(() =>
    {
        Console.WriteLine($"[SetupMode] Startup complete. Setup secret: {setupSecretForStartupReminder} — open /setup to continue onboarding.");
    });
}

// Configure the HTTP request pipeline.
// OpenAPI/Swagger surface is exposed in Development AND Testing so contract-invariant
// integration tests (Event.API.IntegrationTests) can fetch /openapi/event-api.json.
if (app.Environment.IsDevelopment() || app.Environment.IsEnvironment("Testing") || isOpenApiGeneration)
{
    app.MapOpenApi().DisableRequestTimeout();
    app.UseSwagger();
    app.UseSwaggerUI(c => c.SwaggerEndpoint("/swagger/v0.1/swagger.json", "Explore API v0.1"));
    app.MapScalarApiReference();
}

if (app.Environment.IsDevelopment())
{
    Microsoft.IdentityModel.Logging.IdentityModelEventSource.ShowPII = true;

    //Microsoft.IdentityModel.Tokens.JsonWebTokenHandler.DefaultMapInboundClaims = false;
    app.UseCors("DevPolicy"); // for development purposes only

    app.MapPost("/admin/migrate", async (ExploreDbContext context, ILogger<Program> logger) =>
        {
            try
            {
                logger.LogInformation(" Applying database migrations...");
                await context.Database.MigrateAsync();
                await PostgresModelConstraintApplier.ApplyAsync(context);
                logger.LogInformation(" Database migrations applied successfully!");
                return Results.Ok(new { message = "Migrations applied successfully" });
            }
            catch (Exception ex)
            {
                logger.LogError(ex, " An error occurred while migrating the database.");
                return Results.Problem("Migration failed: " + ex.Message);
            }
        })
        .WithName(Explore.API.Hateoas.RouteNames.ApplyDatabaseMigrations)
        .RequireAuthorization();
}
else
{
    app.UseCors("InternalAppPolicy");
    app.UseHsts();
}

app.UseApiExceptionHandling();
app.UseForwardedHeaders();

// Security: add protective headers to all responses
app.UseSecurityHeaders();

// Observability: correlation ID propagation (incoming + outgoing + Serilog)
app.UseCorrelationId();

// Observability: structured request logging (after correlation ID so it's available)
app.UseRequestLogging();

app.UseResponseCompression();
app.UseHttpsRedirection();

// HATEOAS: Process Prefer header for RFC 7240 support (return=minimal)
app.UseHateoas();

app.UseRouting();
app.UseMiddleware<ApiTenantResolutionMiddleware>();
app.UseRequestTimeouts();
app.UseMiddleware<ApiAuthenticationConflictMiddleware>();
app.UseAuthentication();
app.UseMiddleware<ApiTenantPostAuthenticationMiddleware>();
app.UseMiddleware<McpRuntimeGateMiddleware>();
app.UseRequestLocalization();
app.UseMiddleware<IdempotencyMiddleware>();
app.UseRateLimiter();
app.UseAuthorization();
if (!isOpenApiGeneration &&
    useTickerQEmailDispatch &&
    TickerQSchedulerExtensions.IsTickerQSchedulerEnabled(app.Configuration, app.Environment))
{
    app.UseApiTickerQScheduler();
}
app.UseOutputCache();

// Performance: ETag / conditional requests (after output cache)
app.UseETag();

app.MapControllers();

var effectiveMcpAdapterSettings = app.Services.GetRequiredService<IOptions<McpAdapterSettings>>().Value;
if (effectiveMcpAdapterSettings.Enabled && !isOpenApiGeneration)
{
    app.MapMcp(effectiveMcpAdapterSettings.EndpointPath)
        .AllowAnonymous();
}

// Map health check endpoints for Coolify/container orchestration
app.MapDefaultEndpoints();

app.Run();

// Static volatile field for thread-safe shutdown signaling across health check threads
partial class Program
{
    private static volatile bool isShuttingDown;
}
