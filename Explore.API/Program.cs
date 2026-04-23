using System.IO.Compression;
using System.Net;
using Explore.API.BackgroundServices;
using Explore.API.Configuration;
using Explore.API.Extensions;
using Explore.API.Middleware;
using Explore.API.Services;
using Explore.Application;
using Explore.Application.Contracts.Infrastructure;
using Explore.Application.Contracts.Services;
using Explore.Application.Telemetry;
using Explore.Infrastructure;
using Explore.Persistence;
using Explore.Persistence.Seed;
using Explore.Secrets.Extensions;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.ResponseCompression;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.IO;
using Microsoft.OpenApi;
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
builder.Configuration.AddInfisicalCompatibility();

// Add secret management services (refresh service, health checks, metrics, audit logging)
// This adds observability and background refresh for secrets loaded via AddInfisicalCompatibility
builder.Services.AddSecretManagement(builder.Configuration);

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

builder.Services.AddRouting(options =>
{
    options.LowercaseUrls = true;
});

// Add services to the container.

builder.Services.ConfigureApplicationServices();
builder.Services.ConfigureInfrastructureServices(builder.Configuration);

// Skip DbContext registration if running in Testing environment (Integration tests register their own)
var skipDbContext = builder.Environment.IsEnvironment("Testing");
builder.Services.ConfigurePersistenceServices(builder.Configuration, skipDbContextRegistration: skipDbContext);

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
        // Serialize all enums as strings by default — matches client DTO expectations
        // (e.g. DeploymentModeDto.Mode) and industry best practice for public APIs.
        options.JsonSerializerOptions.Converters.Add(
            new System.Text.Json.Serialization.JsonStringEnumConverter());
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
    options.AddDocumentTransformer<Explore.API.OpenApi.HalDtoSchemaTransformer>();
    options.AddDocumentTransformer<Explore.API.OpenApi.OperationIdInvariantTransformer>();
    options.AddOperationTransformer<Explore.API.OpenApi.EndpointClassificationTransformer>();
});

// Add HttpClient for OpenAPI export service
builder.Services.AddHttpClient();

// Register OpenAPI export service (exports swagger.json at startup in Development)
builder.Services.AddHostedService<OpenApiExportService>();

// Register PDS sync background worker for AT Protocol federation
builder.Services.AddHostedService<PdsSyncWorker>();

// Register generic outbox processor for reliable side-effect delivery
builder.Services.AddHostedService<OutboxProcessor>();

builder.Services.AddApiCors(builder.Configuration);

builder.Host.UseSerilog((ctx, services, lc) =>
    lc.ReadFrom.Configuration(ctx.Configuration)
      .ReadFrom.Services(services)
      .Enrich.FromLogContext(),
    writeToProviders: true);

builder.Services.AddApiAuthentication(builder.Configuration, builder.Environment);

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
    .AddDbContextCheck<ExploreDbContext>("database", tags: ["ready"]);

// Request timeouts: default 30s, lookups 10s, complex 60s
builder.Services.AddApiRequestTimeouts(builder.Configuration);

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

// Register graceful shutdown handlers for zero-downtime deployments
// SIGTERM: Start graceful shutdown with 25 second grace period
// SIGINT (Ctrl+C): Immediate shutdown
app.Lifetime.ApplicationStopping.Register(() =>
{
    isShuttingDown = true;
    app.Logger.LogInformation(
        "SIGTERM received. Starting graceful shutdown. Health checks return 503. " +
        "Accepting requests for {Seconds} more seconds...",
        GracefulShutdownSeconds);
});

// Handle SIGINT — delegate to host for graceful drain
Console.CancelKeyPress += (sender, e) =>
{
    app.Logger.LogWarning("SIGINT received. Initiating graceful shutdown...");
    e.Cancel = true; // Prevent immediate CLR termination; let the host drain
    shutdownCts.Cancel();
    app.Lifetime.StopApplication();
};


// Apply database migrations before starting the application
// EF Core 9+ has built-in locking for concurrent migration protection (safe for multiple replicas)
// Migrations run before API accepts traffic - if they fail, the container doesn't start
if (!builder.Environment.IsEnvironment("Testing"))
{
    using var scope = app.Services.CreateScope();
    var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();
    var db = scope.ServiceProvider.GetRequiredService<ExploreDbContext>();

    try
    {
        logger.LogInformation("Applying database migrations...");
        await db.Database.MigrateAsync();
        logger.LogInformation("Database migrations completed successfully.");

        // Run seeding (lookup tables in all environments, dev data in Development)
        await DatabaseSeeder.SeedAsync(db, app.Environment);
        logger.LogInformation("Database seeding completed.");
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
var setupSecretProvider = app.Services.GetRequiredService<Explore.Application.Contracts.Services.ISetupSecretProvider>();
await setupSecretProvider.InitializeAsync();
string? setupSecretForStartupReminder = null;
if (setupSecretProvider.IsSetupModeActive)
{
    if (setupSecretProvider.IsFromEnvironmentVariable)
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
if (app.Environment.IsDevelopment() || app.Environment.IsEnvironment("Testing"))
{
    app.MapOpenApi();
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
                logger.LogInformation(builder.Configuration["ConnectionStrings:DefaultConnection"]);
                await context.Database.MigrateAsync();
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
app.UseRequestLocalization();
app.UseMiddleware<IdempotencyMiddleware>();
app.UseRateLimiter();
app.UseAuthorization();
app.UseOutputCache();

// Performance: ETag / conditional requests (after output cache)
app.UseETag();

app.MapControllers();

// Map health check endpoints for Coolify/container orchestration
app.MapDefaultEndpoints();

app.Run();

// Static volatile field for thread-safe shutdown signaling across health check threads
partial class Program
{
    private static volatile bool isShuttingDown;
}
