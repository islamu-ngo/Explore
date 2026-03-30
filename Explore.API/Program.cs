using System.IO.Compression;
using System.Net;
using System.Net.Http.Headers;
using System.Security.Claims;
using System.Text.RegularExpressions;
using System.Threading.RateLimiting;
using Explore.API.Authentication;
using Explore.API.BackgroundServices;
using Explore.API.Configuration;
using Explore.API.Extensions;
using Explore.API.Middleware;
using Explore.API.Services;
using Explore.Application;
using Explore.Application.Constants;
using Explore.Application.Contracts.Infrastructure;
using Explore.Application.Contracts.Services;
using Explore.Application.Telemetry;
using Explore.Infrastructure;
using Explore.Persistence;
using Explore.Persistence.Seed;
using Explore.Secrets.Extensions;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.AspNetCore.ResponseCompression;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Hybrid;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.IdentityModel.Tokens;
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

var authority = builder.Configuration["Keycloak:Authority"];
var realm = builder.Configuration["Keycloak:Realm"];
var audience = builder.Configuration["Keycloak:Audience"]; // Should be "explore-api"

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

// Performance: Output caching for read endpoints
builder.Services.AddOutputCache(options =>
{
    options.AddBasePolicy(builder => builder.NoCache());
    // PublicData: truly public lookup endpoints (categories, tags, languages) — no auth variance needed
    options.AddPolicy("PublicData", builder => builder
        .Expire(TimeSpan.FromHours(1))
        .SetVaryByHeader(TenantHeaderNames.TenantSlug, "Host")
        .Tag("lookup-data"));
    // LookupData: kept for backward compatibility, same as PublicData
    options.AddPolicy("LookupData", builder => builder
        .Expire(TimeSpan.FromHours(1))
        .SetVaryByHeader(TenantHeaderNames.TenantSlug, "Host")
        .Tag("lookup-data"));
    // ListData: varies by Authorization for auth-aware HATEOAS links
    options.AddPolicy("ListData", builder => builder
        .Expire(TimeSpan.FromSeconds(30))
        .SetVaryByHeader(TenantHeaderNames.TenantSlug, "Host", "Authorization")
        .SetVaryByQuery("pageNumber", "pageSize")
        .Tag("list-data"));
    // DetailData: varies by Authorization for auth-aware HATEOAS links
    options.AddPolicy("DetailData", builder => builder
        .Expire(TimeSpan.FromSeconds(60))
        .SetVaryByHeader(TenantHeaderNames.TenantSlug, "Host", "Authorization")
        .SetVaryByRouteValue("id")
        .Tag("detail-data"));
    // TenantNav: tenant navigation links — short expiry, evicted on write by "tenant-nav" tag
    options.AddPolicy("TenantNav", builder => builder
        .Expire(TimeSpan.FromMinutes(5))
        .SetVaryByHeader(TenantHeaderNames.TenantSlug, "Host")
        .Tag("tenant-nav"));
});

// Performance: HybridCache (L1 in-memory + optional L2 distributed)
builder.Services.AddHybridCache(options =>
{
    options.MaximumPayloadBytes = 1024 * 1024 * 10; // 10MB
    options.MaximumKeyLength = 512;
    options.DefaultEntryOptions = new HybridCacheEntryOptions
    {
        Expiration = TimeSpan.FromMinutes(30),
        LocalCacheExpiration = TimeSpan.FromMinutes(5)
    };
});

builder.Services.AddRouting(options =>
{
    options.LowercaseUrls = true;
});

// Add services to the container.

builder.Services.ConfigureApplicationServices();
builder.Services.ConfigureInfrastructureServices(builder.Configuration);

// Skip DbContext registration if running in Testing environment (Integration tests register their own)
var skipDbContext = builder.Environment.IsEnvironment("Testing");
builder.Services.CongfigurePersistenceServices(builder.Configuration, skipDbContextRegistration: skipDbContext);

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

// Pooled memory streams for ETag middleware — eliminates per-request MemoryStream allocations
builder.Services.AddSingleton<RecyclableMemoryStreamManager>();


builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.TypeInfoResolverChain.Add(
            Explore.Application.Serialization.ExploreJsonContext.Default);
        options.JsonSerializerOptions.PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase;
        options.JsonSerializerOptions.DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull;
    });

builder.Services.AddApiExceptionHandling();

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
});

// Add HttpClient for OpenAPI export service
builder.Services.AddHttpClient();

// Register OpenAPI export service (exports swagger.json at startup in Development)
builder.Services.AddHostedService<OpenApiExportService>();

// Register PDS sync background worker for AT Protocol federation
builder.Services.AddHostedService<PdsSyncWorker>();

// Register generic outbox processor for reliable side-effect delivery
builder.Services.AddHostedService<OutboxProcessor>();

// CORS: hardened policies with configurable allowed origins
// Dev policy remains permissive; production policies use explicit origin allowlists
var corsAllowedOrigins = builder.Configuration.GetSection("Cors:AllowedOrigins")
    .Get<string[]>() ?? ["https://iloveibadah.app"];

builder.Services.AddCors(options =>
{
    options.AddPolicy("InternalAppPolicy",
        policy => policy.WithOrigins(corsAllowedOrigins)
            .AllowAnyMethod()
            .AllowAnyHeader()
            .AllowCredentials());

    options.AddPolicy("ExternalAppPolicy",
        policy => policy.WithOrigins(corsAllowedOrigins)
            .WithMethods("GET", "POST", "PUT", "DELETE", "OPTIONS")
            .AllowAnyHeader());

    options.AddPolicy("InternalWebsitePolicy",
        policy => policy.WithOrigins(corsAllowedOrigins)
            .AllowAnyHeader()
            .AllowAnyMethod()
            .AllowCredentials());

    options.AddPolicy("ExternalWebsitePolicy",
        policy => policy.WithOrigins(corsAllowedOrigins)
            .WithMethods("GET", "OPTIONS")
            .WithHeaders("Accept", "Content-Type", "Authorization", "X-Tenant-Slug"));

    options.AddPolicy("DevPolicy",
        policy => policy.AllowAnyOrigin()
            .AllowAnyHeader()
            .AllowAnyMethod());
});

builder.Host.UseSerilog((ctx, services, lc) =>
    lc.ReadFrom.Configuration(ctx.Configuration)
      .ReadFrom.Services(services)
      .Enrich.FromLogContext(),
    writeToProviders: true);

// Multi-auth API authentication for direct callers.
// Bearer remains JWT-only. Machine callers use X-API-Key and are dispatched explicitly.
builder.Services.AddAuthentication(options =>
    {
        options.DefaultScheme = ApiAuthenticationSchemeNames.MultiAuth;
        options.DefaultAuthenticateScheme = ApiAuthenticationSchemeNames.MultiAuth;
        options.DefaultChallengeScheme = ApiAuthenticationSchemeNames.MultiAuth;
    })
    .AddPolicyScheme(ApiAuthenticationSchemeNames.MultiAuth, ApiAuthenticationSchemeNames.MultiAuth, options =>
    {
        options.ForwardDefaultSelector = context =>
        {
            if (context.Request.Headers.ContainsKey(ApiAuthenticationHeaderNames.ApiKey))
            {
                return ApiAuthenticationSchemeNames.ApiKey;
            }

            return JwtBearerDefaults.AuthenticationScheme;
        };
    })
    .AddJwtBearer(JwtBearerDefaults.AuthenticationScheme, options =>
    {
        // Sets options.RequireHttpsMetadata based on configuration
        options.RequireHttpsMetadata = string.Equals(
            builder.Configuration["Keycloak:RequireHttpsMetadata"],
            "true",
            StringComparison.OrdinalIgnoreCase
        );

        options.Authority = authority;
        options.MetadataAddress = builder.Configuration["Keycloak:MetadataAddress"];

        // Valid audiences for multi-client support (BFF pattern)
        // Keycloak uses 'azp' (authorized party) in addition to 'aud' for client identification
        var validAudiences = new[]
        {
            "explore-api",           // Direct API access (Swagger, external clients)
            "explore-blazor-server", // Blazor Server BFF pattern (forwards OIDC tokens)
            "account"                // Keycloak account service (common in Keycloak tokens)
        };

        // Token validation parameters for multi-client support (BFF pattern)
        options.TokenValidationParameters = new TokenValidationParameters
        {
            // Custom audience validation that checks both 'aud' and 'azp' claims
            // Keycloak often puts the client ID in 'azp' rather than 'aud'
            ValidateAudience = true,
            AudienceValidator = (audiences, securityToken, validationParameters) =>
            {
                var audienceList = audiences?.ToList() ?? new List<string>();

                // Check standard 'aud' claim
                if (audienceList.Any(aud => validAudiences.Contains(aud)))
                {
                    return true;
                }

                // Check 'azp' (authorized party) claim - Keycloak uses this for the client ID
                if (securityToken is System.IdentityModel.Tokens.Jwt.JwtSecurityToken jwtToken)
                {
                    var azp = jwtToken.Claims.FirstOrDefault(c => c.Type == "azp")?.Value;
                    if (!string.IsNullOrEmpty(azp) && validAudiences.Contains(azp))
                    {
                        return true;
                    }

                }

                return false;
            },

            // Issuer validation
            ValidateIssuer = true,
            ValidIssuer = authority,

            // Lifetime validation
            ValidateLifetime = true,
            ClockSkew = TimeSpan.FromMinutes(5), // Allow 5 minutes clock skew

            // Signature validation (automatic via OIDC discovery)
            ValidateIssuerSigningKey = true,

            // Claim type mappings for Keycloak
            NameClaimType = "preferred_username"
        };

        // Development: Accept self-signed certificates for Keycloak
        if (builder.Environment.IsDevelopment())
        {
            options.BackchannelHttpHandler = new HttpClientHandler
            {
                ServerCertificateCustomValidationCallback =
                    HttpClientHandler.DangerousAcceptAnyServerCertificateValidator
            };
        }

        // JWT Bearer events — minimal production logging
        // PII-safe: only exception messages logged, never token claims or raw values
        options.Events = new JwtBearerEvents
        {
            OnAuthenticationFailed = context =>
            {
                var logger = context.HttpContext.RequestServices.GetRequiredService<ILogger<Program>>();
                logger.LogWarning(
                    "[JWT] Authentication failed for {Method} {Path}: {Error}",
                    context.Request.Method,
                    context.Request.Path,
                    context.Exception?.Message);
                return Task.CompletedTask;
            },

            OnChallenge = context =>
            {
                var logger = context.HttpContext.RequestServices.GetRequiredService<ILogger<Program>>();
                logger.LogDebug(
                    "[JWT] Challenge issued for {Path}. Error: {Error}, Description: {Desc}",
                    context.Request.Path,
                    context.Error,
                    context.ErrorDescription);
                return Task.CompletedTask;
            }
        };
    })
    .AddScheme<ApiKeyAuthenticationOptions, ApiKeyAuthenticationHandler>(
        ApiAuthenticationSchemeNames.ApiKey,
        options =>
        {
            builder.Configuration.GetSection(ApiKeyAuthenticationOptions.SectionName).Bind(options);

            if (string.IsNullOrWhiteSpace(options.HeaderName))
            {
                options.HeaderName = ApiAuthenticationHeaderNames.ApiKey;
            }
        });

builder.Services.AddAuthorizationBuilder();

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
if (app.Environment.IsDevelopment())
{
    Microsoft.IdentityModel.Logging.IdentityModelEventSource.ShowPII = true;

    //Microsoft.IdentityModel.Tokens.JsonWebTokenHandler.DefaultMapInboundClaims = false;
    app.MapOpenApi();
    app.UseSwagger();
    app.UseSwaggerUI(c => c.SwaggerEndpoint("/swagger/v0.1/swagger.json", "Explore API v0.1"));
    app.MapScalarApiReference();
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
        .RequireAuthorization(); // S�curisez cet endpoint !
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
