// ABOUTME: .NET Aspire AppHost for local development orchestration.
// Each project resolves Postgres via BootstrapSecretLoader (Infisical /postgresql -> POSTGRESQL_* env -> Postgresql:* config).
// AppHost only orchestrates startup order + service references; bootstrap credentials come from user-secrets or the shell env.

using Aspire.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;

var builder = DistributedApplication.CreateBuilder(args);
var repositoryRoot = FindRepositoryRoot(Directory.GetCurrentDirectory());
var cerbosPolicyPackagePath = Path.Combine(repositoryRoot, "cerbos", "policies");


Console.WriteLine("===========================================");
Console.WriteLine("Explore AppHost - Local Development Orchestrator");
Console.WriteLine("Postgres via BootstrapSecretLoader; other secrets via per-project Infisical/env");
Console.WriteLine("===========================================");

// Delayed health check for startup sequencing
var startAfter = DateTime.Now.AddSeconds(20);
builder.Services.AddHealthChecks().AddCheck("startup-delay", () =>
    DateTime.Now > startAfter ? HealthCheckResult.Healthy() : HealthCheckResult.Unhealthy());

// Redis for distributed cache (deployment mode, settings)
var cache = builder.AddRedis("cache");

// Optional RabbitMQ Dispatch Mode transport for EmailDispatch pointer messages.
// Basic Dispatch Mode remains API + PostgreSQL + SMTP and does not require this resource outside AppHost.
var messaging = builder.AddRabbitMQ("messaging")
    .WithManagementPlugin();

// Migration service runs first
// It loads its own connection string via AddInfisicalCompatibility() in its Program.cs
var migrations = builder.AddProject<Projects.Event_MigrationService>("event-migrationservice");

// Explore API - loads its own secrets via AddInfisicalCompatibility()
// No need to pass environment variables - it reads from Infisical using bootstrap credentials in user secrets
var exploreAPI = builder.AddProject<Projects.Explore_API>("explore-api")
    .WithReference(migrations)
    .WaitForCompletion(migrations)
    .WithReference(cache)
    .WaitFor(cache)
    .WithReference(messaging)
    .WaitFor(messaging)
    .WithEnvironment("EmailDispatchRabbitMq__Enabled", "true")
    .WithEnvironment("Cerbos__PolicyPackagePath", cerbosPolicyPackagePath);

// Explore Blazor - loads its own secrets via AddInfisicalCompatibility()
// Service discovery (via WithReference) automatically resolves the API URL at runtime.
// Do NOT hardcode ExploreAPI__BaseUrl here — Aspire assigns dynamic ports.
var exploreBlazor = builder.AddProject<Projects.Explore_Blazor>("explore-blazor")
    .WithReference(migrations)
    .WaitForCompletion(migrations)
    .WithReference(exploreAPI)
    .WaitFor(exploreAPI)
    .WithReference(cache)
    .WaitFor(cache);

await builder.Build().RunAsync();

static string FindRepositoryRoot(string startDirectory)
{
    var current = new DirectoryInfo(startDirectory);

    while (current is not null)
    {
        if (File.Exists(Path.Combine(current.FullName, "Explore.sln"))
            && Directory.Exists(Path.Combine(current.FullName, "cerbos", "policies")))
        {
            return current.FullName;
        }

        current = current.Parent;
    }

    return startDirectory;
}
