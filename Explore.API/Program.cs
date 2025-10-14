using Microsoft.IdentityModel.Tokens;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();

var authority = builder.Configuration["Keycloak:Authority"];
var realm = builder.Configuration["Keycloak:Realm"];
var audience = builder.Configuration["Keycloak:Audience"];

// Add services to the container.

builder.Services.AddControllers();
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();

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

app.MapDefaultEndpoints();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();
