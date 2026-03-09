using System.IO.Compression;
using System.Net.Http.Headers;
using System.Security.Claims;
using System.Text.RegularExpressions;
using System.Threading.RateLimiting;
using Explore.API.Authentication;
using Explore.API.BackgroundServices;
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
using Microsoft.OpenApi;
using Scalar.AspNetCore;
using Serilog;
using static Microsoft.AspNetCore.Http.StatusCodes;

// Graceful shutdown tracking for zero-downtime deployments
// SIGTERM: 25 second grace period (health returns 503, still accepts requests)
// SIGINT: Immediate shutdown
var isShuttingDown = false;
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
builder.Configuration.AddInfisicalCompatibility();

// Add secret management services (refresh service, health checks, metrics, audit logging)
// This adds observability and background refresh for secrets loaded via AddInfisicalCompatibility
builder.Services.AddSecretManagement(builder.Configuration);

var authority = builder.Configuration["Keycloak:Authority"];
var realm = builder.Configuration["Keycloak:Realm"];
var audience = builder.Configuration["Keycloak:Audience"]; // Should be "explore-api"

builder.Services.AddHttpContextAccessor();

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
    options.AddPolicy("LookupData", builder => builder
        .Expire(TimeSpan.FromHours(1))
        .SetVaryByHeader(TenantHeaderNames.TenantSlug, "X-Forwarded-Host", "Host")
        .Tag("lookup-data"));
    options.AddPolicy("ListData", builder => builder
        .Expire(TimeSpan.FromSeconds(30))
        .SetVaryByHeader(TenantHeaderNames.TenantSlug, "X-Forwarded-Host", "Host")
        .SetVaryByQuery("pageNumber", "pageSize")
        .Tag("list-data"));
    options.AddPolicy("DetailData", builder => builder
        .Expire(TimeSpan.FromSeconds(60))
        .SetVaryByHeader(TenantHeaderNames.TenantSlug, "X-Forwarded-Host", "Host")
        .SetVaryByRouteValue("id")
        .Tag("detail-data"));
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

//AddSwaggerDoc(builder.Services); moved to AddSwaggerGenWithAuth extension method

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
//builder.Services.AddSwaggerGen(); // moved to AddSwaggerGenWithAuth extension method
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
        policy => policy.WithOrigins("https://iloveibadah.app")
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

                    // Log the audience validation failure for debugging
                    Console.WriteLine($"[JWT AudienceValidator] Token audiences: [{string.Join(", ", audienceList)}], azp: {azp ?? "(null)"}, valid audiences: [{string.Join(", ", validAudiences)}]");
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

        // JWT Bearer events for debugging and logging
        options.Events = new JwtBearerEvents
        {
            OnAuthenticationFailed = context =>
            {
                var logger = context.HttpContext.RequestServices.GetRequiredService<ILogger<Program>>();
                logger.LogWarning("[JWT] Authentication failed: {Error}", context.Exception?.Message);

                // Log detailed exception info for debugging
                if (context.Exception is SecurityTokenValidationException stve)
                {
                    logger.LogWarning("[JWT] Token validation error details: {Details}", stve.Message);
                }
                if (context.Exception?.InnerException != null)
                {
                    logger.LogWarning("[JWT] Inner exception: {Inner}", context.Exception.InnerException.Message);
                }

                // Log token details for debugging audience issues
                var authHeader = context.Request.Headers["Authorization"].FirstOrDefault();
                if (!string.IsNullOrEmpty(authHeader) && authHeader.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
                {
                    try
                    {
                        var token = authHeader.Substring(7);
                        var handler = new System.IdentityModel.Tokens.Jwt.JwtSecurityTokenHandler();
                        if (handler.CanReadToken(token))
                        {
                            var jwt = handler.ReadJwtToken(token);
                            var aud = jwt.Audiences?.ToList() ?? new List<string>();
                            var azp = jwt.Claims.FirstOrDefault(c => c.Type == "azp")?.Value;
                            var iss = jwt.Issuer;
                            var exp = jwt.ValidTo;

                            logger.LogWarning("[JWT] Token details - Issuer: {Issuer}, Audiences: [{Audiences}], Azp: {Azp}, Expires: {Exp}",
                                iss, string.Join(", ", aud), azp ?? "(null)", exp);
                        }
                    }
                    catch (Exception ex)
                    {
                        logger.LogWarning("[JWT] Could not parse token for debugging: {Error}", ex.Message);
                    }
                }

                return Task.CompletedTask;
            },

            OnTokenValidated = context =>
            {
                var logger = context.HttpContext.RequestServices.GetRequiredService<ILogger<Program>>();
                var claims = context.Principal?.Claims.Select(c => $"{c.Type}={c.Value}");
                logger.LogInformation("[JWT] Token validated successfully. Claims: {Claims}",
                    string.Join(", ", claims ?? Array.Empty<string>()));
                return Task.CompletedTask;
            },

            OnChallenge = context =>
            {
                var logger = context.HttpContext.RequestServices.GetRequiredService<ILogger<Program>>();
                logger.LogWarning("[JWT] Challenge issued. Error: {Error}, ErrorDescription: {Desc}",
                    context.Error, context.ErrorDescription);
                return Task.CompletedTask;
            },

            OnMessageReceived = context =>
            {
                var logger = context.HttpContext.RequestServices.GetRequiredService<ILogger<Program>>();
                var hasAuth = context.Request.Headers.ContainsKey("Authorization");
                var authHeader = hasAuth ? context.Request.Headers["Authorization"].ToString() : null;
                var tokenPreview = !string.IsNullOrEmpty(authHeader) && authHeader.Length > 20
                    ? $"{authHeader[..20]}..."
                    : authHeader;

                logger.LogInformation("[JWT] Message received. Path: {Path}, Has Authorization: {HasAuth}, Header: {Token}",
                    context.Request.Path, hasAuth, tokenPreview);
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
//builder.Services.AddAuthorization();

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

// Handle SIGINT for immediate shutdown
Console.CancelKeyPress += (sender, e) =>
{
    app.Logger.LogWarning("SIGINT received. Initiating immediate shutdown...");
    e.Cancel = false; // Allow the process to terminate immediately
    shutdownCts.Cancel();
    Environment.Exit(0);
};

// Handle SIGTERM with grace period (Unix systems)
AppDomain.CurrentDomain.ProcessExit += (sender, e) =>
{
    if (!isShuttingDown)
    {
        isShuttingDown = true;
        app.Logger.LogInformation(
            "Process exit signal received. Graceful shutdown with {Seconds} second grace period...",
            GracefulShutdownSeconds);

        // Wait for grace period to allow in-flight requests to complete
        Thread.Sleep(TimeSpan.FromSeconds(GracefulShutdownSeconds));
    }
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
        db.Database.Migrate();
        logger.LogInformation("Database migrations completed successfully.");

        // Run seeding (lookup tables in all environments, dev data in Development)
        DatabaseSeeder.SeedAsync(db, app.Environment).GetAwaiter().GetResult();
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
        app.Logger.LogWarning("[SetupSecret] No SETUP_SECRET env var found. Auto-generated secret for bootstrap.");
        var secretForLog = ((Explore.Infrastructure.Services.SetupSecretProvider)setupSecretProvider).GetSecretForLogging();
        setupSecretForStartupReminder = secretForLog;
        Console.WriteLine();
        Console.WriteLine("+=============================================================+");
        Console.WriteLine("| SETUP SECRET (auto-generated, not persisted across restarts |");
        Console.WriteLine("| unless you set the SETUP_SECRET environment variable):      |");
        Console.WriteLine("|                                                             |");
        Console.WriteLine($"|  {secretForLog,-55} |");
        Console.WriteLine("|                                                             |");
        Console.WriteLine("| Use this at /setup to claim this instance.                  |");
        Console.WriteLine("+=============================================================+");
        Console.WriteLine();
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
        Console.WriteLine();
        Console.WriteLine("+=============================================================+");
        Console.WriteLine("| STARTUP COMPLETE — SETUP SECRET                             |");
        Console.WriteLine("|                                                             |");
        Console.WriteLine($"|  {setupSecretForStartupReminder,-55} |");
        Console.WriteLine("|                                                             |");
        Console.WriteLine("| Open /setup in Blazor to continue onboarding.               |");
        Console.WriteLine("+=============================================================+");
        Console.WriteLine();
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
app.UseAuthentication();
app.UseMiddleware<ApiTenantPostAuthenticationMiddleware>();
app.UseRateLimiter();
app.UseAuthorization();
app.UseOutputCache();

// Performance: ETag / conditional requests (after output cache)
app.UseETag();

app.MapControllers();

// Map health check endpoints for Coolify/container orchestration
app.MapDefaultEndpoints();

//app.MapGet("users/me", (ClaimsPrincipal claimsPrincipal) =>
//{
//    return claimsPrincipal.Claims.ToDictionary(c => c.Type, c => c.Value);
//}).RequireAuthorization();

app.Run();

//void AddSwaggerDoc(IServiceCollection services)
//{
//    services.AddSwaggerGen(c =>
//    {
//        c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
//        {
//            Description = @"JWT Authorization header using the Bearer scheme.
//                            Enter 'Bearer' [space] and then your token in the text input below.
//                            Example: 'Bearer 12345abcdef'",
//            Name = "Authorization",
//            In = ParameterLocation.Header,
//            Type = SecuritySchemeType.ApiKey,
//            Scheme = "Bearer"
//        });

//        c.AddSecurityRequirement(new OpenApiSecurityRequirement()
//        {
//            {
//                new OpenApiSecurityScheme
//                {
//                    Reference = new OpenApiReference
//                    {
//                        Type = ReferenceType.SecurityScheme,
//                        Id = "Bearer"
//                    },
//                    Scheme = "oauth2",
//                    Name = "Bearer",
//                    In = ParameterLocation.Header,
//                },
//                new List<string>()
//            }
//        });

//        c.SwaggerDoc("v1", new OpenApiInfo { Title = "Explore API", Version = "v1" });
//    });
//}
