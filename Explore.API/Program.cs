using Explore.API.Extensions;
using Explore.API.Middleware;
using Explore.Application;
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
using Microsoft.OpenApi;
using static Microsoft.AspNetCore.Http.StatusCodes;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();

builder.Services.AddHttpContextAccessor();

var authority = builder.Configuration["Keycloak:Authority"];
var realm = builder.Configuration["Keycloak:Realm"];
var audience = builder.Configuration["Keycloak:Audience"];

//AddSwaggerDoc(builder.Services); moved to AddSwaggerGenWithAuth extension method

// Add services to the container.

builder.Services.ConfigureApplicationServices();
builder.Services.ConfigureInfrastructureServices(builder.Configuration);
builder.Services.CongfigurePersistenceServices(builder.Configuration);

builder.Services.AddControllers();

builder.Services.AddEndpointsApiExplorer();
//builder.Services.AddSwaggerGen(); // moved to AddSwaggerGenWithAuth extension method
builder.Services.AddSwaggerGenWithAuth(builder.Configuration);
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
//builder.Services.AddOpenApi(opt =>
//{
//    opt.AddDocumentTransformer((document, context, CancellationToken) =>
//    {
//        document.Info.Title = "ISLAMU Explore API";
//        document.Info.Contact = new OpenApiContact()
//        {
//            Name = "Amir",
//            Email = "contact@openislamu.org"
//        };

//        // Add JWT Bearer security scheme
//        document.Components ??= new();
//        document.Components.SecuritySchemes["Bearer"] = new OpenApiSecurityScheme
//        {
//            Type = SecuritySchemeType.Http,
//            Scheme = "bearer",
//            BearerFormat = "JWT",
//            Description = "Enter JWT Bearer token"
//        };

//        // Add global security requirement
//        document.SecurityRequirements.Add(new OpenApiSecurityRequirement
//        {
//            {
//                new OpenApiSecurityScheme
//                {
//                    Reference = new OpenApiReference
//                    {
//                        Type = ReferenceType.SecurityScheme,
//                        Id = "Bearer"
//                    }
//                },
//                Array.Empty<string>()
//            }
//        });

//        return Task.CompletedTask;
//    });
//});

builder.Services.AddCors(options =>
{
    options.AddPolicy("InternalAppPolicy", // ISLAMU!
        builder => builder.AllowAnyOrigin()
            .WithMethods()
            .WithHeaders());

    options.AddPolicy("ExternalAppPolicy", // for external apps or scripts that need to access the API for community
        builder => builder.AllowAnyOrigin()
            .WithMethods()
            .WithHeaders());

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

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddKeycloakJwtBearer(JwtBearerDefaults.AuthenticationScheme, realm: realm, options => {
    //.AddJwtBearer(options => {
        // will require when in prod... needs to fix this hardcoded value later...
        //options.RequireHttpsMetadata = false;

        // Sets options.RequireHttpsMetadata to true if the config value is "true", otherwise false
        options.RequireHttpsMetadata = string.Equals(
            builder.Configuration["Keycloak:RequireHttpsMetadata"],
            "true",
            StringComparison.OrdinalIgnoreCase
        );

        options.Authority = authority;
        options.Audience = audience;
        options.MetadataAddress = builder.Configuration["Keycloak:MetadataAddress"];
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateAudience = true,
            ValidateIssuer = true,
            ValidIssuer = authority,
            // it only checks if envirement variable isnullorempty... I need audience to contain identity-api !!
            //ValidateAudience = !string.IsNullOrEmpty(builder.Configuration["Keycloak:Audience"]),
            ValidAudiences = new[] { "explore-api" },
            ValidateLifetime = true,
            NameClaimType = "preferred_username",
            RoleClaimType = "roles" // see mapper note below
        };

        options.BackchannelHttpHandler = new HttpClientHandler
        {
            ServerCertificateCustomValidationCallback =
                HttpClientHandler.DangerousAcceptAnyServerCertificateValidator
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

// En dev, votre HTTPS local est sur 7039; en prod, laissez null (443 par défaut)
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