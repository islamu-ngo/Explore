using Explore.API.Extensions;
using Explore.API.Middleware;
using Explore.API.Services;
using Explore.Application;
using Explore.Application.Contracts.Infrastructure;
using Explore.Infrastructure;
using Explore.Persistence;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.IdentityModel.Tokens;
//using Microsoft.OpenApi.Models;
using Scalar.AspNetCore;
using Serilog;
using System.Net.Http.Headers;
using System.Security.Claims;
using System.Text.RegularExpressions;
using Microsoft.EntityFrameworkCore;
using Microsoft.OpenApi;
using Polly.Bulkhead;
using static Microsoft.AspNetCore.Http.StatusCodes;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();

builder.Services.AddHttpContextAccessor();

var authority = builder.Configuration["Keycloak:Authority"];
var realm = builder.Configuration["Keycloak:Realm"];
var audience = builder.Configuration["Keycloak:Audience"];

// 1. Lire les variables d’environnement S3
var s3Region = builder.Configuration["ISLAMU_EVENT_REGION"]; // si tu en as une
var s3BucketName = builder.Configuration["ISLAMU_EVENT_PRIVATE_BUCKET_NAME"];
var s3AccessKeyId = builder.Configuration["ISLAMU_EVENT_PRIVATE_ACCESS_KEY_ID"];
var s3SecretAccessKey = builder.Configuration["ISLAMU_EVENT_PRIVATE_SECRET_ACCESS_KEY_ID"];
var s3Endpoint = builder.Configuration["ISLAMU_EVENT_S3_ENDPOINT"];

// 2. Ajouter une source de configuration en mémoire qui expose une section "S3Settings"
var s3SettingsDict = new Dictionary<string, string?>
{
    ["S3Settings:Region"] = s3Region,
    ["S3Settings:BucketName"] = s3BucketName,
    ["S3Settings:AccessKeyId"] = s3AccessKeyId,
    ["S3Settings:SecretAccessKey"] = s3SecretAccessKey,
    ["S3Settings:Endpoint"] = s3Endpoint
};

builder.Configuration.AddInMemoryCollection(
    s3SettingsDict.Where(kv => !string.IsNullOrEmpty(kv.Value))
        .ToDictionary(kv => kv.Key, kv => kv.Value)!
);

//AddSwaggerDoc(builder.Services); moved to AddSwaggerGenWithAuth extension method

// Add services to the container.

builder.Services.ConfigureApplicationServices();
builder.Services.ConfigureInfrastructureServices(builder.Configuration);

// Skip DbContext registration if running in Testing environment (Integration tests register their own)
var skipDbContext = builder.Environment.IsEnvironment("Testing");
builder.Services.CongfigurePersistenceServices(builder.Configuration, skipDbContextRegistration: skipDbContext);

// Register tenant context for single-tenant mode
builder.Services.AddScoped<ITenantContext, TenantContext>();

builder.Services.AddControllers();

builder.Services.AddEndpointsApiExplorer();
//builder.Services.AddSwaggerGen(); // moved to AddSwaggerGenWithAuth extension method
builder.Services.AddSwaggerGenWithAuth(builder.Configuration);
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi

// The build tool needs this to generate the JSON file
builder.Services.AddOpenApi("explore-api");

// Add HttpClient for OpenAPI export service
builder.Services.AddHttpClient();

// Register OpenAPI export service (exports swagger.json at startup in Development)
builder.Services.AddHostedService<OpenApiExportService>();

builder.Services.AddCors(options =>
{
    options.AddPolicy("InternalAppPolicy", // ISLAMU!
        builder => builder.AllowAnyOrigin()
            .AllowAnyMethod()
            .AllowAnyHeader());

    options.AddPolicy("ExternalAppPolicy", // for external apps or scripts that need to access the API for community
        builder => builder.AllowAnyOrigin()
            .AllowAnyMethod()
            .AllowAnyHeader());

    options.AddPolicy("InternalWebsitePolicy", // only my website can access some API enpoints, so even if they have the token they cannot access it
        builder => builder.WithOrigins("https://iloveibadah.app") // specify the allowed origin(s) here
            .AllowAnyHeader()
            .AllowAnyMethod());

    options.AddPolicy("ExternalWebsitePolicy", // for external apps or scripts that need to access the API for community
        builder => builder.AllowAnyOrigin()
            .WithMethods()
            .WithHeaders());

    options.AddPolicy("DevPolicy", // for development purposes only
        builder => builder.AllowAnyOrigin()
            .AllowAnyHeader()
            .AllowAnyMethod());
});

builder.Host.UseSerilog((ctx, lc) =>
    lc.WriteTo.Console().ReadFrom.Configuration(ctx.Configuration));

// JWT Bearer Authentication for Keycloak
// Using standard AddJwtBearer instead of AddKeycloakJwtBearer for better control
// over token validation when using external Keycloak (not Aspire-hosted)
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
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
            NameClaimType = "preferred_username",
            RoleClaimType = "roles"
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

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseDeveloperExceptionPage();
    Microsoft.IdentityModel.Logging.IdentityModelEventSource.ShowPII = true;

    //Microsoft.IdentityModel.Tokens.JsonWebTokenHandler.DefaultMapInboundClaims = false;
    app.MapOpenApi();
    app.UseSwagger();
    app.UseSwaggerUI(c => c.SwaggerEndpoint("/swagger/v1/swagger.json", "Explore API v1"));
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
    app.UseExceptionHandler("/Error");
    app.UseHsts();
}

//app.UseForwardedHeaders(new ForwardedHeadersOptions
//{
//    ForwardedHeaders = ForwardedHeaders.XForwardedProto | ForwardedHeaders.XForwardedFor,
//    // En environnement conteneur/proxy, on nettoie pour accepter les proxies dynamiques
//    KnownNetworks = { }, // vide
//    KnownProxies = { }   // vide
//});

app.UseHttpsRedirection();
app.UseRouting();
app.UseAuthentication();
app.UseAuthorization();
app.UseMiddleware<ExceptionMiddleware>();
app.MapControllers();

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
