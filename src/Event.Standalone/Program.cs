// ABOUTME: Composes the API and Blazor owning host modules into one optional web process.
// ABOUTME: Owns one Combined profile, shutdown state, and ordered startup sequence.

using Explore.API.Hosting;
using Explore.Blazor.Extensions;
using Explore.Blazor.Hosting;

const BlazorHostProfile hostProfile = BlazorHostProfile.Combined;
var shutdownState = new GracefulShutdownState();
using var shutdownCts = shutdownState.CancellationTokenSource;
var builder = WebApplication.CreateBuilder(args);
var apiHost = builder.AddApiHostServices(() => shutdownState.IsShuttingDown);
builder.AddBlazorHostServices(hostProfile, shutdownState);

var app = builder.Build();
await app.RunApiHostStartupAsync(
    apiHost,
    shutdownCts,
    () => shutdownState.IsShuttingDown = true);
await app.InitializeBlazorHostAsync(hostProfile);
app.Run();

partial class Program;
