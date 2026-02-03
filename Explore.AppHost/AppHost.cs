// ABOUTME: .NET Aspire AppHost for local development orchestration.
// Simplified orchestrator - each project loads its own secrets via AddSecretManagement()/AddInfisicalCompatibility().
// AppHost only orchestrates startup order and service references, no secret passing required.

using Aspire.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;

var builder = DistributedApplication.CreateBuilder(args);

// Log environment info
Console.WriteLine("===========================================");
Console.WriteLine("Explore AppHost - Local Development Orchestrator");
Console.WriteLine("Each project loads its own secrets via Infisical");
Console.WriteLine("===========================================");

// Delayed health check for startup sequencing
var startAfter = DateTime.Now.AddSeconds(20);
builder.Services.AddHealthChecks().AddCheck("startup-delay", () =>
    DateTime.Now > startAfter ? HealthCheckResult.Healthy() : HealthCheckResult.Unhealthy());

// Migration service runs first
// It loads its own connection string via AddInfisicalCompatibility() in its Program.cs
var migrations = builder.AddProject<Projects.Event_MigrationService>("event-migrationservice");

// Explore API - loads its own secrets via AddInfisicalCompatibility()
// No need to pass environment variables - it reads from Infisical using bootstrap credentials in user secrets
var exploreAPI = builder.AddProject<Projects.Explore_API>("explore-api")
    .WithReference(migrations)
    .WaitForCompletion(migrations);

// Explore Blazor - loads its own secrets via AddInfisicalCompatibility()
// Only pass the API URL since that's orchestration-specific (localhost during development)
var exploreBlazor = builder.AddProject<Projects.Explore_Blazor>("explore-blazor")
    .WithEnvironment("ExploreAPI__BaseUrl", "https://localhost:7039/")
    .WithReference(migrations)
    .WaitForCompletion(migrations)
    .WithReference(exploreAPI)
    .WaitFor(exploreAPI);

await builder.Build().RunAsync();
