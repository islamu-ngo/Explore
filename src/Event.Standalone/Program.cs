// ABOUTME: Composes the API and Blazor owning host modules into one optional web process.
// ABOUTME: Owns one Combined profile, shutdown state, and ordered startup sequence.

using Explore.API.Hosting;
using Explore.Blazor.Extensions;
using Explore.Blazor.Hosting;
using Explore.Persistence;
using Explore.Persistence.Database;
using Explore.Secrets.Database;
using Event.Standalone.Hosting;
using Event.Standalone.Middleware;

const BlazorHostProfile hostProfile = BlazorHostProfile.Combined;
var shutdownState = new GracefulShutdownState();
using var shutdownCts = shutdownState.CancellationTokenSource;
var builder = WebApplication.CreateBuilder(args);
var apiHost = builder.AddApiHostServices(() => shutdownState.IsShuttingDown);
builder.AddBlazorHostServices(hostProfile, shutdownState);
builder.Services.AddCombinedApiBridge();

var app = builder.Build();
var primaryDatabase = PrimaryDatabaseConfiguration.BindRuntime(app.Configuration);
if (primaryDatabase.Provider == PrimaryDatabaseProvider.Sqlite &&
    app.Configuration.GetValue("Hosting:ReplicaCount", 1) != 1)
{
    throw new InvalidOperationException(
        "Hosting:ReplicaCount must be 1 when Database:Provider=Sqlite. Event.Standalone local SQLite storage supports exactly one application replica.");
}

await using (var scope = app.Services.CreateAsyncScope())
{
    var database = scope.ServiceProvider.GetRequiredService<ExploreDbContext>();
    await SqliteDatabaseInitializer.InitializeAsync(database, shutdownCts.Token);
}

await app.RunApiHostStartupAsync(
    apiHost,
    shutdownCts,
    () => shutdownState.IsShuttingDown = true);
await app.InitializeBlazorHostAsync(hostProfile);
app.UseStandaloneHostMiddleware(apiHost, hostProfile, shutdownState);
app.MapStandaloneHostEndpoints(apiHost, hostProfile);
app.BindStandaloneInternalApiTransport();
app.Run();

partial class Program;
