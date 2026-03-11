// ABOUTME: Component tests for AnalyticsInitializer bootstrap behavior and graceful degradation paths.
// ABOUTME: Verifies initialization occurs once and safely no-ops when settings are missing or service throws.

namespace Explore.Blazor.Client.Tests.Components;

public class AnalyticsInitializerTests : IDisposable
{
    private const string NavigationSourceProperty = "navigation_source";
    private const string TenantIdProperty = "tenant_id";
    private const string PageReferrerProperty = "page_referrer";

    private readonly BlazorTestContext _ctx;
    private readonly Type _analyticsInitializerType;

    public AnalyticsInitializerTests()
    {
        _ctx = new BlazorTestContext();
        _analyticsInitializerType = typeof(IPublicExperienceService).Assembly.GetTypes()
            .First(x => x.Name == "AnalyticsInitializer");

        // Register services added by the consent state machine rewrite
        _ctx.Services.AddSingleton(Substitute.For<ICookieConsentInterop>());
        _ctx.Services.AddSingleton(new CookieConsentStateService());
    }

    public void Dispose()
    {
        _ctx.Dispose();
    }

    private static bool MatchesInitialPageViewProperties(IDictionary<string, object>? properties, Guid tenantId)
    {
        return MatchesPageViewProperties(properties, "initial_load", tenantId.ToString(), null, 2);
    }

    private static bool MatchesProgrammaticPageViewProperties(IDictionary<string, object>? properties, Guid tenantId, string pageReferrer)
    {
        return MatchesPageViewProperties(properties, "programmatic_navigation", tenantId.ToString(), pageReferrer, 3);
    }

    private static bool MatchesPageViewProperties(
        IDictionary<string, object>? properties,
        string expectedNavigationSource,
        string? expectedTenantId,
        string? expectedPageReferrer,
        int expectedCount)
    {
        if (properties is null || properties.Count != expectedCount)
        {
            return false;
        }

        if (!TryGetString(properties, NavigationSourceProperty, out var navigationSource)
            || !string.Equals(navigationSource, expectedNavigationSource, StringComparison.Ordinal))
        {
            return false;
        }

        if (expectedTenantId is null)
        {
            if (properties.ContainsKey(TenantIdProperty))
            {
                return false;
            }
        }
        else if (!TryGetString(properties, TenantIdProperty, out var tenantId)
            || !string.Equals(tenantId, expectedTenantId, StringComparison.Ordinal))
        {
            return false;
        }

        if (expectedPageReferrer is null)
        {
            return !properties.ContainsKey(PageReferrerProperty);
        }

        return TryGetString(properties, PageReferrerProperty, out var pageReferrer)
            && string.Equals(pageReferrer, expectedPageReferrer, StringComparison.Ordinal);
    }

    private static bool TryGetString(IDictionary<string, object> properties, string key, out string? value)
    {
        value = null;

        if (!properties.TryGetValue(key, out var rawValue))
        {
            return false;
        }

        value = rawValue?.ToString();
        return !string.IsNullOrWhiteSpace(value);
    }

    private IRenderedFragmentBase RenderAnalyticsInitializer()
    {
        var method = typeof(Bunit.TestContext)
            .GetMethods()
            .First(x => x.Name == "RenderComponent" && x.IsGenericMethod && x.GetParameters().Length == 1);

        return (IRenderedFragmentBase)method.MakeGenericMethod(_analyticsInitializerType)
            .Invoke(_ctx, new object?[] { Array.Empty<ComponentParameter>() })!;
    }

    [Test]
    public async Task Renders_WithValidSettings_InitializesInteropAndTracksInitialPageView()
    {
        var tenantId = Guid.NewGuid();
        var settingsService = Substitute.For<IPublicExperienceService>();
        settingsService.GetSettingsAsync().Returns(new PublicExperienceSettingsModel
        {
            TenantId = tenantId,
            AnalyticsProvider = "posthog",
            AnalyticsEnabled = true,
            AnalyticsConsentMode = "identified",
            AnalyticsTransportMode = "relay",
            AnalyticsAllowIdentify = true,
            AnalyticsPublicApiKey = "public-key",
            AnalyticsEndpointUrl = "https://analytics.example.com"
        });

        var analyticsInterop = Substitute.For<IAnalyticsInterop>();
        _ctx.Services.AddSingleton(settingsService);
        _ctx.Services.AddSingleton(analyticsInterop);

        var cut = RenderAnalyticsInitializer();
        await Task.Yield();

        cut.WaitForAssertion(() =>
        {
            analyticsInterop.Received(1).InitAsync("posthog", true, "identified", "relay", true, "public-key", "https://analytics.example.com");
            analyticsInterop.Received(1).PageViewAsync(
                "/",
                Arg.Is<IDictionary<string, object>>(properties => MatchesInitialPageViewProperties(properties, tenantId)));
        });
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

        await analyticsInterop.DidNotReceiveWithAnyArgs().InitAsync(default!, default, default!, default!, default, default!, default!);
    }

    [Test]
    public async Task Renders_WithAnalyticsDisabled_DoesNotTrackPageViews()
    {
        var settingsService = Substitute.For<IPublicExperienceService>();
        settingsService.GetSettingsAsync().Returns(new PublicExperienceSettingsModel
        {
            AnalyticsProvider = "none",
            AnalyticsEnabled = false,
            AnalyticsConsentMode = "anonymous",
            AnalyticsTransportMode = "direct",
            AnalyticsAllowIdentify = false
        });

        var analyticsInterop = Substitute.For<IAnalyticsInterop>();
        _ctx.Services.AddSingleton(settingsService);
        _ctx.Services.AddSingleton(analyticsInterop);

        var cut = RenderAnalyticsInitializer();
        await Task.Yield();

        cut.WaitForAssertion(() =>
        {
            analyticsInterop.DidNotReceiveWithAnyArgs().InitAsync(default!, default, default!, default!, default, default!, default!);
            analyticsInterop.DidNotReceiveWithAnyArgs().PageViewAsync(default!, default!);
        });
    }

    [Test]
    public async Task Navigates_ToNewRoute_TracksProgrammaticPageViewWithReferrer()
    {
        var tenantId = Guid.NewGuid();
        var settingsService = Substitute.For<IPublicExperienceService>();
        settingsService.GetSettingsAsync().Returns(new PublicExperienceSettingsModel
        {
            TenantId = tenantId,
            AnalyticsProvider = "posthog",
            AnalyticsEnabled = true,
            AnalyticsConsentMode = "pseudonymous",
            AnalyticsTransportMode = "relay",
            AnalyticsAllowIdentify = false,
            AnalyticsEndpointUrl = "/api/a/t"
        });

        var analyticsInterop = Substitute.For<IAnalyticsInterop>();
        _ctx.Services.AddSingleton(settingsService);
        _ctx.Services.AddSingleton(analyticsInterop);
        var navigationManager = _ctx.Services.GetRequiredService<FakeNavigationManager>();

        var cut = RenderAnalyticsInitializer();
        await Task.Yield();

        cut.WaitForAssertion(() =>
        {
            analyticsInterop.Received(1).PageViewAsync(
                "/",
                Arg.Is<IDictionary<string, object>>(properties => MatchesInitialPageViewProperties(properties, tenantId)));
        });

        navigationManager.NavigateTo("/events");

        cut.WaitForAssertion(() =>
        {
            analyticsInterop.Received(1).PageViewAsync(
                "/events",
                Arg.Is<IDictionary<string, object>>(properties => MatchesProgrammaticPageViewProperties(properties, tenantId, "/")));
        });
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

        await analyticsInterop.DidNotReceiveWithAnyArgs().InitAsync(default!, default, default!, default!, default, default!, default!);
    }
}
