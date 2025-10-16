using Explore.API.Middleware;
using Explore.Application;
using Explore.Infrastructure;
using Explore.Persistence;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using Scalar.AspNetCore;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();

builder.Services.AddHttpContextAccessor();

var authority = builder.Configuration["Keycloak:Authority"];
var realm = builder.Configuration["Keycloak:Realm"];
var audience = builder.Configuration["Keycloak:Audience"];

AddSwaggerDoc(builder.Services);

// Add services to the container.

builder.Services.ConfigureApplicationServices();
builder.Services.ConfigureInfrastructureServices(builder.Configuration);
builder.Services.CongfigurePersistenceServices(builder.Configuration);

builder.Services.AddControllers();

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi(opt =>
{
    opt.AddDocumentTransformer((document, context, CancellationToken) =>
    {
        document.Info.Title = "ISLAMU Explore API";
        document.Info.Contact = new OpenApiContact()
        {
            Name = "Amir",
            Email = "contact@openislamu.org"
        };

        // Add JWT Bearer security scheme
        document.Components ??= new();
        document.Components.SecuritySchemes["Bearer"] = new OpenApiSecurityScheme
        {
            Type = SecuritySchemeType.Http,
            Scheme = "bearer",
            BearerFormat = "JWT",
            Description = "Enter JWT Bearer token"
        };

        // Add global security requirement
        document.SecurityRequirements.Add(new OpenApiSecurityRequirement
        {
            {
                new OpenApiSecurityScheme
                {
                    Reference = new OpenApiReference
                    {
                        Type = ReferenceType.SecurityScheme,
                        Id = "Bearer"
                    }
                },
                Array.Empty<string>()
            }
        });

        return Task.CompletedTask;
    });
});

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


builder.Services.AddAuthentication()
    .AddKeycloakJwtBearer("keycloak", realm: realm, options =>
    {
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

        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            // it only checks if envirement variable isnullorempty... I need audience to contain identity-api !!
            //ValidateAudience = !string.IsNullOrEmpty(builder.Configuration["Keycloak:Audience"]),
            ValidAudiences = new[] { "explore-api" },
            ValidateLifetime = true,
            NameClaimType = "preferred_username",
            RoleClaimType = "roles" // see mapper note below
        };
    });

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseDeveloperExceptionPage();
    app.MapOpenApi();
    app.UseSwagger();
    app.UseSwaggerUI(c => c.SwaggerEndpoint("/swagger/v1/swagger.json", "Explore API v1"));
    app.MapScalarApiReference();
    app.UseCors("DevPolicy"); // for development purposes only
}
else
{
    app.UseCors("InternalAppPolicy");
}

app.UseHttpsRedirection();
app.UseRouting();
app.UseAuthentication();
app.UseAuthorization();
app.UseMiddleware<ExceptionMiddleware>();
app.MapControllers();

app.UseEndpoints(endpoints =>
{
    endpoints.MapControllers();
});

app.Run();

void AddSwaggerDoc(IServiceCollection services)
{
    services.AddSwaggerGen(c =>
    {
        c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
        {
            Description = @"JWT Authorization header using the Bearer scheme. 
                            Enter 'Bearer' [space] and then your token in the text input below.
                            Example: 'Bearer 12345abcdef'",
            Name = "Authorization",
            In = ParameterLocation.Header,
            Type = SecuritySchemeType.ApiKey,
            Scheme = "Bearer"
        });

        c.AddSecurityRequirement(new OpenApiSecurityRequirement()
        {
            {
                new OpenApiSecurityScheme
                {
                    Reference = new OpenApiReference
                    {
                        Type = ReferenceType.SecurityScheme,
                        Id = "Bearer"
                    },
                    Scheme = "oauth2",
                    Name = "Bearer",
                    In = ParameterLocation.Header,
                },
                new List<string>()
            }
        });

        c.SwaggerDoc("v1", new OpenApiInfo { Title = "Explore API", Version = "v1" });
    });
}