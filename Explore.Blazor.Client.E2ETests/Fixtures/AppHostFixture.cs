// ABOUTME: Aspire AppHost fixture for E2E tests starting the full application stack.
// ABOUTME: Provides the Blazor frontend URL to Playwright tests.

using Aspire.Hosting;
using Aspire.Hosting.ApplicationModel;
using Aspire.Hosting.Testing;
using Microsoft.Extensions.DependencyInjection;

namespace Explore.Blazor.Client.E2ETests.Fixtures;

public sealed class AppHostFixture : IAsyncInitializer, IAsyncDisposable
{
    private DistributedApplication? _app;

    public string BlazorBaseUrl => _app?.GetEndpoint("explore-blazor", "https")?.ToString().TrimEnd('/')
        ?? throw new InvalidOperationException("Blazor app not started");

    public async Task InitializeAsync()
    {
        var builder = await DistributedApplicationTestingBuilder.CreateAsync<Projects.Explore_AppHost>();

        builder.Services.ConfigureHttpClientDefaults(c =>
            c.ConfigureHttpClient(h => h.BaseAddress = null));

        _app = await builder.BuildAsync();
        var resourceNotificationService = _app.Services.GetRequiredService<ResourceNotificationService>();

        await _app.StartAsync();

        await resourceNotificationService.WaitForResourceHealthyAsync(
            "explore-blazor",
            new CancellationTokenSource(TimeSpan.FromMinutes(3)).Token);
    }

    public async ValueTask DisposeAsync()
    {
        if (_app is not null)
        {
            await _app.DisposeAsync();
        }
    }
}
