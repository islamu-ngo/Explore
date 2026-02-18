// ABOUTME: Component tests for AnalyticsInitializer bootstrap behavior and graceful degradation paths.
// ABOUTME: Verifies initialization occurs once and safely no-ops when settings are missing or service throws.

namespace Explore.Blazor.Client.Tests.Components;

public class AnalyticsInitializerTests : IDisposable
{
    private readonly BlazorTestContext _ctx;
    private readonly Type _analyticsInitializerType;

    public AnalyticsInitializerTests()
    {
        _ctx = new BlazorTestContext();
        _analyticsInitializerType = typeof(IPublicExperienceService).Assembly.GetTypes()
            .First(x => x.Name == "AnalyticsInitializer");
    }

    public void Dispose()
    {
        _ctx.Dispose();
    }

    private void RenderAnalyticsInitializer()
    {
        var method = typeof(Bunit.TestContext)
            .GetMethods()
            .First(x => x.Name == "RenderComponent" && x.IsGenericMethod && x.GetParameters().Length == 1);

        method.MakeGenericMethod(_analyticsInitializerType)
            .Invoke(_ctx, new object?[] { Array.Empty<ComponentParameter>() });
    }

    [Test]
    public async Task Renders_WithValidSettings_InitializesInteropOnce()
    {
        var settingsService = Substitute.For<IPublicExperienceService>();
        settingsService.GetSettingsAsync().Returns(new PublicExperienceSettingsModel
        {
            AnalyticsProvider = "posthog",
            AnalyticsEnabled = true,
            AnalyticsPublicApiKey = "public-key",
            AnalyticsEndpointUrl = "https://analytics.example.com"
        });

        var analyticsInterop = Substitute.For<IAnalyticsInterop>();
        _ctx.Services.AddSingleton(settingsService);
        _ctx.Services.AddSingleton(analyticsInterop);

        RenderAnalyticsInitializer();
        await Task.Yield();

        await analyticsInterop.Received(1).InitAsync("posthog", true, "public-key", "https://analytics.example.com");
    }

    [Test]
    public async Task Renders_WithNullSettings_DoesNotInitializeInterop()
    {
        var settingsService = Substitute.For<IPublicExperienceService>();
        settingsService.GetSettingsAsync().Returns((PublicExperienceSettingsModel?)null);

        var analyticsInterop = Substitute.For<IAnalyticsInterop>();
        _ctx.Services.AddSingleton(settingsService);
        _ctx.Services.AddSingleton(analyticsInterop);

        RenderAnalyticsInitializer();
        await Task.Yield();

        await analyticsInterop.DidNotReceiveWithAnyArgs().InitAsync(default, default, default!, default!);
    }

    [Test]
    public async Task Renders_WhenSettingsServiceThrows_DoesNotCrash()
    {
        var settingsService = Substitute.For<IPublicExperienceService>();
        settingsService.GetSettingsAsync().ThrowsAsync(new InvalidOperationException("settings unavailable"));

        var analyticsInterop = Substitute.For<IAnalyticsInterop>();
        _ctx.Services.AddSingleton(settingsService);
        _ctx.Services.AddSingleton(analyticsInterop);

        RenderAnalyticsInitializer();
        await Task.Yield();

        await analyticsInterop.DidNotReceiveWithAnyArgs().InitAsync(default, default, default!, default!);
    }
}
