using Aspire.Hosting;
using Infisical.Sdk;
using Infisical.Sdk.Model;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;

var builder = DistributedApplication.CreateBuilder(args);

// Infisical (read creds from user-secrets)
var infisicalUrl =
    builder.Configuration["Infisical:Url"]
    ?? "https://infisical.openislamu.org";
var infisicalProjectId = builder.Configuration["Infisical:ProjectId"];
var infisicalEnv = builder.Configuration["Infisical:Environment"] ?? "dev";
var infisicalClientId = builder.Configuration["Infisical:ClientId"];
var infisicalClientSecret = builder.Configuration["Infisical:ClientSecret"];

var infisicalSettings = new InfisicalSdkSettingsBuilder()
    .WithHostUri(infisicalUrl)
    .Build();
var infisical = new InfisicalClient(infisicalSettings);

// login with universal auth
await infisical.Auth()
    .UniversalAuth()
    .LoginAsync(infisicalClientId!, infisicalClientSecret!);

// helper: load secrets from a path
async Task<IDictionary<string, string>> LoadInfisicalEnvAsync(
    string projectId,
    string envSlug,
    string secretPath
)
{
    var opts = new ListSecretsOptions
    {
        ProjectId = projectId,
        EnvironmentSlug = envSlug,
        SecretPath = secretPath,
        ExpandSecretReferences = true,
        Recursive = true,
        ViewSecretValue = true
    };

    var secrets = await infisical.Secrets().ListAsync(opts);
    if (secrets == null)
    {
        throw new Exception("Infisical returned null secrets list.");
    }

    return secrets.ToDictionary(s => s.SecretKey, s => s.SecretValue);
}

// Load keycloak secrets once
var keycloakSecrets = await LoadInfisicalEnvAsync(
    infisicalProjectId!,
    infisicalEnv,
    "/keycloak"
);


// Load api secrets once
var apiSecrets = await LoadInfisicalEnvAsync(
    infisicalProjectId!,
    infisicalEnv,
    "/api"
);

// Load postgresql secrets once
var postgresqlSecrets = await LoadInfisicalEnvAsync(
    infisicalProjectId!,
    infisicalEnv,
    "/postgresql"
);

var startAfter = DateTime.Now.AddSeconds(20); //set the start time later

builder.Services.AddHealthChecks().AddCheck("mycheck", () =>
{
    return DateTime.Now > startAfter ? HealthCheckResult.Healthy() : HealthCheckResult.Unhealthy();
});

string postgresPassword = postgresqlSecrets.TryGetValue("POSTGRESQL_SERVER_PASSWORD", out var pgServerPass) && pgServerPass != null
    ? pgServerPass
    : "defaultpassword";

var passwordParam = builder.AddParameter("postgres-password", postgresPassword);

var ExploreServer = builder.AddPostgres("ExploreServer", password: passwordParam)
    .WithDataVolume(isReadOnly: false)
    .WithHealthCheck("mycheck");
var ExploreDB = ExploreServer.AddDatabase("ExploreDB");

var realm = keycloakSecrets.TryGetValue("KEYCLOAK_REALM", out var r) ? r : "islamu-dev";
var authority = $"https://keycloak.openislamu.org/realms/{realm}";
var metadataAddress = $"{authority}/.well-known/openid-configuration";
var authorizationUrl = $"{authority}/protocol/openid-connect/auth";

var exploreBlazor = builder.AddProject<Projects.Explore_Blazor>("explore-blazor")
    .WithEnvironment("Keycloak__Authority", authority)
    //.WithEnvironment("keycloak__Audience", "explore-api") there is no audiance for the blazor server...
    .WithEnvironment("Keycloak__Realm", realm)
    .WithEnvironment("Keycloak__ClientId", "explore-blazor-server")
    .WithEnvironment("Keycloak__ClientSecret", keycloakSecrets.TryGetValue("EXPLORE_BLAZOR_SERVER_CLIENT_SECRET_COOLIFY", out var n) ? n : "")
    .WithEnvironment("Keycloak__RequireHttpsMetadata", "true")
    .WithEnvironment("ExploreAPI__BaseUrl","https://localhost:7039/")
    .WithReference(ExploreDB)
    .WaitFor(ExploreDB);

// for url accessible from the internet, not needed!
//var tunnel = builder.AddDevTunnel("tunnel", "islamu-dev-tunnel")
//    .WithAnonymousAccess()
//    .WaitFor(exploreBlazor)
//    .WithReference(exploreBlazor);

var exploreAPI = builder.AddProject<Projects.Explore_API>("explore-api")
    .WithEnvironment("Keycloak__Authority", authority)
    .WithEnvironment("Keycloak__Audience", "explore-api")
    .WithEnvironment("Keycloak__Realm", realm)
    .WithEnvironment("Keycloak__MetadataAddress", metadataAddress)
    .WithEnvironment("Keycloak__AuthorizationUrl", authorizationUrl)
    .WithEnvironment("Keycloak__ClientId", "explore-api")
    //.WithEnvironment("Keycloak__ClientSecret", keycloakSecrets.TryGetValue("EXPLORE_API_CLIENT_SECRET_COOLIFY", out var y) ? n : "")
    .WithEnvironment("Keycloak__RequireHttpsMetadata", "true")
    .WithReference(ExploreDB)
    .WaitFor(ExploreDB);

foreach (var kv in apiSecrets)
{
    exploreAPI.WithEnvironment(kv.Key, kv.Value);
}

// Not for now!
//builder.AddRedis("redis");

// Not for now!
//builder.AddRabbitMQ("rabbitmq");

builder.Build().Run();